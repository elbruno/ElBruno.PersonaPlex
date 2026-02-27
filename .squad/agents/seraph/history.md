# Project Context

- **Owner:** Bruno Capuano
- **Project:** ElBruno.PersonaPlex — C# library wrapping NVIDIA PersonaPlex-7B-v1 for ONNX Runtime inference
- **Stack:** PyTorch, ONNX, ONNX Runtime (C#), Python, quantization (INT4/INT8/FP16)
- **Focus:** ONNX model conversion correctness, optimization, quantization, inference pipeline verification
- **Created:** 2026-02-27

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-02-27: Initial model context
- NVIDIA PersonaPlex-7B-v1 is a 7B parameter model — export and quantization settings matter significantly
- PyTorch → ONNX conversion must preserve model behavior (numerical accuracy within tolerances)
- ONNX Runtime execution providers: CPU (default), CUDA, DirectML — fallback chain configuration is critical
- Quantization options: INT4 (smallest, lossy), INT8 (balanced), FP16 (highest quality, GPU-preferred)
- Dynamic axes must be correctly specified for variable-length inputs (batch size, sequence length)
- C# inference code must match the exact input/output tensor names and shapes from the ONNX export
