# Exporting ONNX Models

This guide covers how to export the PersonaPlex-7B-v1 PyTorch model to ONNX format for use with the C# library.

> **Note:** You only need this if you want to re-export or customize models. The C# library downloads pre-exported ONNX models automatically.

## Prerequisites

- Python 3.10+
- PyTorch 2.x
- CUDA-capable GPU (recommended for export)
- HuggingFace account with accepted PersonaPlex model license

## Setup

```bash
cd python/
pip install -r requirements.txt

# Accept the model license at https://huggingface.co/nvidia/personaplex-7b-v1
export HF_TOKEN=<YOUR_HUGGINGFACE_TOKEN>
```

## Export Process

The PersonaPlex model (Moshi architecture) needs to be exported as separate ONNX components:

### 1. Mimi Encoder
Converts 24kHz audio input to discrete tokens.

```bash
python export_onnx.py --component encoder --output onnx_exports/
```

### 2. Main Language Model
The 7B-parameter unified Transformer backbone. This is the largest component and may require quantization.

```bash
python export_onnx.py --component lm --output onnx_exports/ --quantize int8
```

### 3. Mimi Decoder
Converts generated tokens back to 24kHz audio.

```bash
python export_onnx.py --component decoder --output onnx_exports/
```

### 4. Voice Embeddings
Convert `.pt` voice embedding files to a format usable by ONNX Runtime.

```bash
python export_onnx.py --component embeddings --output onnx_exports/
```

## Upload to HuggingFace

```bash
python upload_to_hf.py --source onnx_exports/ --repo elbruno/personaplex-7b-v1-ONNX
```

## Quantization Options

Due to the 7B parameter size, quantization is recommended:

| Mode | Size | Quality | Speed |
|------|------|---------|-------|
| FP32 | ~28 GB | Best | Slowest |
| FP16 | ~14 GB | Very good | Faster |
| INT8 | ~7 GB | Good | Fast |
| INT4 | ~3.5 GB | Acceptable | Fastest |

## Known Limitations

- The Moshi architecture uses custom operations that may need ONNX custom operator support
- Full-duplex streaming mode requires stateful KV-cache management in ONNX
- Export has been tested with opset version 17+
