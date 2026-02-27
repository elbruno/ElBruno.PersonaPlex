"""
Export PersonaPlex-7B-v1 (Moshi architecture) components to ONNX format.

Usage:
    python export_onnx.py --component encoder --output onnx_exports/
    python export_onnx.py --component lm --output onnx_exports/ --quantize fp16
    python export_onnx.py --component decoder --output onnx_exports/
    python export_onnx.py --component embeddings --output onnx_exports/
    python export_onnx.py --component all --output onnx_exports/

Prerequisites:
    - Install dependencies: pip install -r requirements.txt
    - Accept model license: https://huggingface.co/nvidia/personaplex-7b-v1
    - Set HF_TOKEN environment variable
    - Clone NVIDIA/personaplex repo and point --moshi-path to its moshi/ dir

The model consists of:
  1. Mimi Encoder  – SEANet + Transformer → audio-to-tokens (~100 MB)
  2. Mimi Decoder  – tokens-to-audio (~100 MB)
  3. LM Backbone   – 7B-param Transformer + Depformer (~14 GB fp16)
  4. Voice Embeds   – pre-computed .pt → .npy files
"""

import argparse
import os
import sys
import logging
from pathlib import Path

import numpy as np
import torch
import onnx

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
log = logging.getLogger(__name__)

SAMPLE_RATE = 24000
FRAME_RATE = 12.5
HF_REPO = "nvidia/personaplex-7b-v1"


def _ensure_moshi_importable(moshi_path: str):
    """Add moshi source to sys.path so we can import without pip install."""
    p = str(Path(moshi_path).resolve())
    if p not in sys.path:
        sys.path.insert(0, p)

    # Disable torch.compile (requires C++ compiler not available in all envs)
    import torch._dynamo
    torch._dynamo.config.suppress_errors = True

    # Disable moshi's own compile wrapper
    try:
        import moshi.utils.compile as mc
        mc._compile_disabled = True
    except ImportError:
        pass


def _download_weights(cache_dir: str):
    """Download model weights from HuggingFace if not already cached."""
    from huggingface_hub import hf_hub_download

    files = [
        "tokenizer-e351c8d8-checkpoint125.safetensors",  # Mimi
        "model.safetensors",  # LM
        "tokenizer_spm_32k_3.model",  # SentencePiece
        "voices.tgz",  # Voice embeddings
    ]
    paths = {}
    for f in files:
        log.info("Ensuring %s is downloaded...", f)
        paths[f] = hf_hub_download(repo_id=HF_REPO, filename=f, cache_dir=cache_dir)
    return paths


# ---------------------------------------------------------------------------
# Mimi Encoder wrapper (audio → codes)
# ---------------------------------------------------------------------------
class MimiEncoderWrapper(torch.nn.Module):
    """Wraps MimiModel.encode() for ONNX export (no streaming state)."""

    def __init__(self, mimi):
        super().__init__()
        self.encoder = mimi.encoder
        self.encoder_transformer = mimi.encoder_transformer
        self.quantizer = mimi.quantizer
        self.downsample = mimi.downsample if hasattr(mimi, "downsample") else None

    def forward(self, audio: torch.Tensor) -> torch.Tensor:
        """
        Args:
            audio: [B, 1, T] float32 waveform at 24kHz
        Returns:
            codes: [B, K, T'] int64 discrete tokens
        """
        emb = self.encoder(audio)
        if self.encoder_transformer is not None:
            (emb,) = self.encoder_transformer(emb)
        if self.downsample is not None:
            emb = self.downsample(emb)
        codes = self.quantizer.encode(emb)
        return codes


# ---------------------------------------------------------------------------
# Mimi Decoder wrapper (codes → audio)
# ---------------------------------------------------------------------------
class MimiDecoderWrapper(torch.nn.Module):
    """Wraps MimiModel.decode() for ONNX export (no streaming state)."""

    def __init__(self, mimi):
        super().__init__()
        self.quantizer = mimi.quantizer
        self.upsample = mimi.upsample if hasattr(mimi, "upsample") else None
        self.decoder_transformer = mimi.decoder_transformer
        self.decoder = mimi.decoder

    def forward(self, codes: torch.Tensor) -> torch.Tensor:
        """
        Args:
            codes: [B, K, T'] int64 discrete tokens
        Returns:
            audio: [B, 1, T] float32 waveform
        """
        emb = self.quantizer.decode(codes)
        if self.upsample is not None:
            emb = self.upsample(emb)
        if self.decoder_transformer is not None:
            (emb,) = self.decoder_transformer(emb)
        audio = self.decoder(emb)
        return audio


# ---------------------------------------------------------------------------
# LM wrapper for non-streaming (offline) inference
# ---------------------------------------------------------------------------
class LMEmbedCodesWrapper(torch.nn.Module):
    """Wraps LMModel.embed_codes + forward_embeddings for ONNX export."""

    def __init__(self, lm):
        super().__init__()
        self.lm = lm

    def forward(self, codes: torch.Tensor) -> tuple[torch.Tensor, torch.Tensor]:
        """
        Args:
            codes: [B, K, T] int64 — K = n_q + 1 (text + audio codebooks)
        Returns:
            transformer_out: [B, T, dim] — main transformer hidden states
            text_logits: [B, 1, T, text_card] — text predictions
        """
        return self.lm.forward_codes(codes)


class LMDepformerStepWrapper(torch.nn.Module):
    """Wraps a single Depformer step for ONNX export."""

    def __init__(self, lm, cb_index: int):
        super().__init__()
        self.lm = lm
        self.cb_index = cb_index

    def forward(self, sequence: torch.Tensor, transformer_out: torch.Tensor) -> torch.Tensor:
        """
        Args:
            sequence: [B, 1, 1] int64 — previous codebook token
            transformer_out: [B, 1, dim] — from main transformer
        Returns:
            logits: [B, 1, 1, card] — prediction for this codebook
        """
        return self.lm.forward_depformer(self.cb_index, sequence, transformer_out)


# ---------------------------------------------------------------------------
# Export functions
# ---------------------------------------------------------------------------
def export_encoder(output_dir: str, cache_dir: str, device: str = "cpu"):
    """Export Mimi encoder to ONNX."""
    from moshi.models.loaders import get_mimi, MIMI_NAME
    from huggingface_hub import hf_hub_download

    log.info("Downloading Mimi weights...")
    mimi_path = hf_hub_download(repo_id=HF_REPO, filename=MIMI_NAME, cache_dir=cache_dir)

    log.info("Loading Mimi model...")
    mimi = get_mimi(mimi_path, device=device)
    mimi.eval()

    wrapper = MimiEncoderWrapper(mimi).to(device)
    wrapper.eval()

    # 1 second of audio at 24kHz
    dummy_audio = torch.randn(1, 1, SAMPLE_RATE, device=device)
    out_path = os.path.join(output_dir, "mimi_encoder.onnx")

    log.info("Tracing encoder...")
    with torch.no_grad():
        test_codes = wrapper(dummy_audio)
        log.info("Encoder output shape: %s (expected [1, K, T'])", test_codes.shape)

    log.info("Exporting to ONNX: %s", out_path)
    torch.onnx.export(
        wrapper,
        (dummy_audio,),
        out_path,
        input_names=["audio"],
        output_names=["codes"],
        dynamic_axes={
            "audio": {0: "batch", 2: "samples"},
            "codes": {0: "batch", 2: "frames"},
        },
        opset_version=17,
        do_constant_folding=True,
    )

    # Validate
    model = onnx.load(out_path)
    onnx.checker.check_model(model)
    size_mb = os.path.getsize(out_path) / (1024 * 1024)
    log.info("✓ mimi_encoder.onnx exported (%.1f MB)", size_mb)


def export_decoder(output_dir: str, cache_dir: str, device: str = "cpu"):
    """Export Mimi decoder to ONNX."""
    from moshi.models.loaders import get_mimi, MIMI_NAME
    from huggingface_hub import hf_hub_download

    log.info("Downloading Mimi weights...")
    mimi_path = hf_hub_download(repo_id=HF_REPO, filename=MIMI_NAME, cache_dir=cache_dir)

    log.info("Loading Mimi model...")
    mimi = get_mimi(mimi_path, device=device)
    mimi.eval()

    wrapper = MimiDecoderWrapper(mimi).to(device)
    wrapper.eval()

    # Encoder produces K=8 codebooks. 1 sec audio → ~12-13 frames
    n_frames = 13
    n_codebooks = mimi.num_codebooks  # 8
    dummy_codes = torch.randint(0, 2048, (1, n_codebooks, n_frames), device=device, dtype=torch.long)
    out_path = os.path.join(output_dir, "mimi_decoder.onnx")

    log.info("Tracing decoder...")
    with torch.no_grad():
        test_audio = wrapper(dummy_codes)
        log.info("Decoder output shape: %s", test_audio.shape)

    log.info("Exporting to ONNX: %s", out_path)
    torch.onnx.export(
        wrapper,
        (dummy_codes,),
        out_path,
        input_names=["codes"],
        output_names=["audio"],
        dynamic_axes={
            "codes": {0: "batch", 2: "frames"},
            "audio": {0: "batch", 2: "samples"},
        },
        opset_version=17,
        do_constant_folding=True,
    )

    model = onnx.load(out_path)
    onnx.checker.check_model(model)
    size_mb = os.path.getsize(out_path) / (1024 * 1024)
    log.info("✓ mimi_decoder.onnx exported (%.1f MB)", size_mb)


def export_lm(output_dir: str, cache_dir: str, quantize: str = "none", device: str = "cpu"):
    """Export LM backbone to ONNX.

    Due to the 7B parameter size, this is exported in fp16 by default and
    can optionally be quantized to int8.  The streaming Depformer is exported
    as separate per-codebook step models.
    """
    from moshi.models.loaders import get_moshi_lm, MOSHI_NAME
    from huggingface_hub import hf_hub_download

    dtype = torch.float16 if quantize in ("fp16", "int8", "int4") else torch.float32

    log.info("Downloading LM weights...")
    lm_path = hf_hub_download(repo_id=HF_REPO, filename=MOSHI_NAME, cache_dir=cache_dir)

    log.info("Loading LM model (dtype=%s, this may take a while)...", dtype)
    lm = get_moshi_lm(lm_path, device=device, dtype=dtype)
    lm.eval()

    # Export main transformer (embed_codes + forward_embeddings)
    n_q = lm.n_q  # 16
    num_codebooks = n_q + 1  # 17 (text + audio)
    seq_len = 10

    backbone_wrapper = LMEmbedCodesWrapper(lm)
    backbone_wrapper.eval()

    dummy_codes = torch.randint(0, 2048, (1, num_codebooks, seq_len), device=device, dtype=torch.long)
    # Text codebook uses range [0, 32000)
    dummy_codes[:, 0, :] = torch.randint(0, 32000, (1, seq_len), device=device, dtype=torch.long)

    out_path = os.path.join(output_dir, "lm_backbone.onnx")

    log.info("Tracing LM backbone...")
    with torch.no_grad():
        transformer_out, text_logits = backbone_wrapper(dummy_codes)
        log.info("LM backbone output: transformer_out=%s, text_logits=%s",
                 transformer_out.shape, text_logits.shape)

    log.info("Exporting LM backbone to ONNX: %s", out_path)
    torch.onnx.export(
        backbone_wrapper,
        (dummy_codes,),
        out_path,
        input_names=["codes"],
        output_names=["transformer_out", "text_logits"],
        dynamic_axes={
            "codes": {0: "batch", 2: "seq_len"},
            "transformer_out": {0: "batch", 1: "seq_len"},
            "text_logits": {0: "batch", 2: "seq_len"},
        },
        opset_version=17,
        do_constant_folding=True,
    )

    model = onnx.load(out_path)
    onnx.checker.check_model(out_path)  # Use path for >2GB models
    size_mb = os.path.getsize(out_path) / (1024 * 1024)
    # Include external data file size if present
    data_file = out_path + ".data"
    if os.path.exists(data_file):
        size_mb += os.path.getsize(data_file) / (1024 * 1024)
    log.info("✓ lm_backbone.onnx exported (%.1f MB total)", size_mb)

    # Export Depformer steps (one per codebook)
    dep_q = lm.dep_q  # 16
    for cb in range(dep_q):
        dep_wrapper = LMDepformerStepWrapper(lm, cb)
        dep_wrapper.eval()

        dummy_seq = torch.randint(0, 2048, (1, 1, 1), device=device, dtype=torch.long)
        dummy_tr_out = torch.randn(1, 1, lm.dim, device=device, dtype=dtype)

        dep_path = os.path.join(output_dir, f"lm_depformer_step_{cb}.onnx")
        log.info("Exporting Depformer step %d/%d...", cb + 1, dep_q)

        with torch.no_grad():
            torch.onnx.export(
                dep_wrapper,
                (dummy_seq, dummy_tr_out),
                dep_path,
                input_names=["sequence", "transformer_out"],
                output_names=["logits"],
                opset_version=17,
                do_constant_folding=True,
            )

    log.info("✓ %d Depformer step models exported", dep_q)

    # Post-export quantization
    if quantize == "int8":
        try:
            from onnxruntime.quantization import quantize_dynamic, QuantType
            q_path = os.path.join(output_dir, "lm_backbone_int8.onnx")
            log.info("Quantizing LM backbone to INT8...")
            quantize_dynamic(out_path, q_path, weight_type=QuantType.QInt8)
            q_size = os.path.getsize(q_path) / (1024 * 1024)
            log.info("✓ lm_backbone_int8.onnx (%.1f MB)", q_size)
        except ImportError:
            log.warning("onnxruntime.quantization not available, skipping INT8 quantization")


def export_embeddings(output_dir: str, cache_dir: str):
    """Convert voice embedding .pt files to numpy .npy format."""
    from huggingface_hub import hf_hub_download
    import tarfile

    log.info("Downloading voice embeddings...")
    voices_path = hf_hub_download(repo_id=HF_REPO, filename="voices.tgz", cache_dir=cache_dir)

    emb_dir = os.path.join(output_dir, "voice_embeddings")
    os.makedirs(emb_dir, exist_ok=True)

    log.info("Extracting voices.tgz...")
    with tarfile.open(voices_path, "r:gz") as tar:
        tar.extractall(path=emb_dir, filter="data")

    # Convert .pt files to .npy
    count = 0
    for pt_file in Path(emb_dir).rglob("*.pt"):
        log.info("Converting %s", pt_file.name)
        data = torch.load(pt_file, map_location="cpu", weights_only=True)
        if isinstance(data, torch.Tensor):
            npy_path = pt_file.with_suffix(".npy")
            np.save(npy_path, data.numpy())
            count += 1
        elif isinstance(data, dict):
            for key, val in data.items():
                if isinstance(val, torch.Tensor):
                    npy_path = pt_file.with_suffix(f".{key}.npy")
                    np.save(npy_path, val.numpy())
                    count += 1

    log.info("✓ Converted %d voice embedding files to .npy", count)

    # Also copy the SentencePiece tokenizer
    try:
        sp_path = hf_hub_download(repo_id=HF_REPO, filename="tokenizer_spm_32k_3.model", cache_dir=cache_dir)
        import shutil
        dest = os.path.join(output_dir, "tokenizer_spm_32k_3.model")
        shutil.copy2(sp_path, dest)
        log.info("✓ Copied SentencePiece tokenizer to %s", dest)
    except Exception as e:
        log.warning("Could not copy SentencePiece tokenizer: %s", e)


def main():
    parser = argparse.ArgumentParser(
        description="Export PersonaPlex-7B-v1 (Moshi) components to ONNX"
    )
    parser.add_argument(
        "--component", required=True,
        choices=["encoder", "lm", "decoder", "embeddings", "all"],
        help="Component to export",
    )
    parser.add_argument("--output", default="onnx_exports/", help="Output directory")
    parser.add_argument("--cache-dir", default=None, help="HuggingFace cache directory")
    parser.add_argument(
        "--moshi-path", default=None,
        help="Path to moshi source directory (from NVIDIA/personaplex clone)",
    )
    parser.add_argument(
        "--quantize", default="none",
        choices=["none", "fp16", "int8", "int4"],
        help="Quantization mode (for LM component)",
    )
    parser.add_argument(
        "--device", default="cpu",
        choices=["cpu", "cuda"],
        help="Device for model loading during export",
    )

    args = parser.parse_args()
    os.makedirs(args.output, exist_ok=True)

    if args.moshi_path:
        _ensure_moshi_importable(args.moshi_path)

    exporters = {
        "encoder": lambda: export_encoder(args.output, args.cache_dir, args.device),
        "decoder": lambda: export_decoder(args.output, args.cache_dir, args.device),
        "lm": lambda: export_lm(args.output, args.cache_dir, args.quantize, args.device),
        "embeddings": lambda: export_embeddings(args.output, args.cache_dir),
    }

    if args.component == "all":
        for name, fn in exporters.items():
            log.info("\n=== Exporting %s ===", name)
            fn()
    else:
        exporters[args.component]()

    log.info("Done! ONNX files are in: %s", os.path.abspath(args.output))


if __name__ == "__main__":
    main()
