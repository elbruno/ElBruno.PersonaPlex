# Architecture Overview

## Model

PersonaPlex-7B-v1 is based on the [Moshi](https://arxiv.org/abs/2410.00037) architecture — a unified, full-duplex speech-to-speech model that integrates ASR, LLM, and TTS into a single 7B-parameter Transformer.

## Pipeline

The inference pipeline has 3 main ONNX components:

```
Input Audio  --> [Mimi Encoder]   --> audio tokens
             --> [Main LM (7B)]   --> response tokens    (+ text prompt + voice embedding)
             --> [Mimi Decoder]   --> output audio (24kHz WAV)
```

## Model Components

| Component | Description | Size |
|-----------|-------------|------|
| **Mimi Encoder** | ConvNet + Transformer — converts 24kHz audio → discrete tokens | TBD |
| **Main LM** | 7B-param unified Transformer backbone — speech understanding + generation | TBD |
| **Mimi Decoder** | Converts generated tokens → 24kHz audio output | TBD |
| **Voice Embeddings** | Pre-computed .pt files for each voice preset (NATF/NATM/VARF/VARM) | Small |

## Voice Conditioning

PersonaPlex supports two types of conditioning:
1. **Voice prompt** — Audio-based voice conditioning via pre-computed embeddings
2. **Text prompt** — Text-based persona/role definition

## Project Structure

```
ElBruno.PersonaPlex.slnx                     # Solution file
src/ElBruno.PersonaPlex/                      # Core library (NuGet package)
  Pipeline/PersonaPlexPipeline.cs             # Full S2S orchestrator + CreateAsync factory
  Audio/WavWriter.cs                          # WAV file writer (24 kHz, 16-bit PCM)
  ModelManager.cs                             # Auto-download models from HuggingFace
  PersonaPlexOptions.cs                       # Configuration
  ConversationResult.cs                       # Inference result
  VoicePreset.cs                              # Voice preset enum
  ExecutionProvider.cs                        # CPU/CUDA/DirectML
  SessionOptionsHelper.cs                     # ONNX session config
  DownloadProgress.cs                         # Progress reporting
src/ElBruno.PersonaPlex.Tests/                # xUnit tests
src/samples/                                  # Console app samples
python/                                       # ONNX export scripts
docs/                                         # Documentation
```

## References

- [NVIDIA PersonaPlex Paper](https://arxiv.org/abs/2602.06053)
- [Original Moshi Paper](https://arxiv.org/abs/2410.00037)
- [PersonaPlex GitHub](https://github.com/NVIDIA/personaplex)
- [PersonaPlex HuggingFace](https://huggingface.co/nvidia/personaplex-7b-v1)
