# Project Context

- **Owner:** Bruno Capuano
- **Project:** ElBruno.PersonaPlex — C# library wrapping NVIDIA PersonaPlex-7B-v1 for ONNX Runtime inference
- **Stack:** C#, .NET, ONNX Runtime, Python, NuGet, GitHub Actions, HuggingFace
- **Focus:** Application security, dependency auditing, supply chain security, input validation, safe model loading, secret management
- **Created:** 2026-02-27

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-02-28: Audit Fixes Review Complete
- Path traversal defense: Always validate against `Path.GetFullPath()` normalized path, not original input (prevents obfuscation)
- Async disposal safety: IDisposable pattern requires `_disposed` flag checks at method entry for fail-fast semantics
- Duration calculation: Hardcoded constants (SampleRate=24000) prevent division-by-zero and overflow; casting to double prevents integer truncation
- Test project suppression: CS1591 warning suppression appropriate only for test projects, never the main library
- Formula safety: For audio duration, `(samples.Length / (double)SampleRate) * 1000.0` is safe — no truncation, no overflow for realistic audio (<24 hours)

### 2026-02-27: Initial security context
- This library downloads ML models from HuggingFace — supply chain security is critical
- ONNX model files can be large and must be validated before deserialization
- NuGet package consumers trust that dependencies are audited
- CI/CD pipelines handle secrets (NuGet API keys, GitHub tokens) — must follow least-privilege
- Input validation matters for any user-provided strings passed to the model
