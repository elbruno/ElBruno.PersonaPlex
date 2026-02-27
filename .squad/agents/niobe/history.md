# Project Context

- **Owner:** Bruno Capuano
- **Project:** ElBruno.PersonaPlex — C# library wrapping NVIDIA PersonaPlex-7B-v1 for ONNX Runtime inference
- **Stack:** C#, .NET, ONNX Runtime, Python, NuGet, GitHub Actions, HuggingFace
- **Focus:** Application security, dependency auditing, supply chain security, input validation, safe model loading, secret management
- **Created:** 2026-02-27

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-02-27: Initial security context
- This library downloads ML models from HuggingFace — supply chain security is critical
- ONNX model files can be large and must be validated before deserialization
- NuGet package consumers trust that dependencies are audited
- CI/CD pipelines handle secrets (NuGet API keys, GitHub tokens) — must follow least-privilege
- Input validation matters for any user-provided strings passed to the model
