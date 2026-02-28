# Project Context

- **Owner:** Bruno Capuano
- **Project:** ElBruno.PersonaPlex — C# library wrapping NVIDIA PersonaPlex-7B-v1 for ONNX Runtime inference
- **Stack:** C#, .NET, ONNX Runtime, Python (model export), xUnit, GitHub Actions, NuGet
- **Created:** 2026-02-27

## Learnings

### 2026-02-27: Codebase Audit Findings (23 Issues Identified)

**Build & Tests:**
- Clean build (0 warnings/errors in core library; 38 CS1591 warnings in test project only)
- All 34 tests passing (17 test cases × 2 frameworks: net8.0, net10.0)
- Project structure: Well-organized (Pipeline, Audio, ModelManager separation)
- Documentation generation enabled; XML comments comprehensive for public APIs

**Key Issues (Ranked by Impact):**
1. **HIGH:** Path traversal validation incomplete (ValidatePath checks original path, not normalized fullPath)
2. **HIGH:** Missing disposed session checks in async encode/decode (race condition risk)
3. **HIGH:** Enum value mismatch: Voice embeddings not actually loaded (7B LM export blocking full implementation)
4. **MEDIUM:** Test project missing XML suppression; 38 warnings on CS1591
5. **MEDIUM:** Audio resampling naive (linear interpolation, no anti-aliasing)
6. **MEDIUM:** No input validation on audio file size (OOM risk on corrupt files)
7. **MEDIUM:** 3D array copy ops inefficient (nested loops vs SIMD)
8. **MEDIUM:** Chunk boundary handling untested with stateful encoder
9. **LOW:** ConversationResult.DurationMs never populated; ResponseText always null
10. **LOW:** README claims "Full-Duplex Capable" but 7B LM not exported yet (misleading)

**Architecture Insights:**
- Current pipeline is **half-duplex** (encode → decode only; no LLM reasoning until 7B ONNX available)
- Voice embedding loading (`GetEmbeddingFileName()`) is dead code until LM export completes
- Dependency versions: Mixed pinning (Downloader @exact 0.5.0, MEDependencyInjection @9.0.*)
- No GPU driver detection; CUDA/DirectML failures silently fall back to CPU
- No performance benchmarks documented

**Quality Baseline:**
- Core library code quality: **High** (clean patterns, proper disposal, good separation of concerns)
- Test coverage: **Good** (basic unit tests for options, voice presets, model manager)
- Documentation: **Mostly complete** but gaps in architecture details, CHANGELOG, benchmarks
- Build system: Clean, multi-target (net8.0 + net10.0), proper NuGet packaging
