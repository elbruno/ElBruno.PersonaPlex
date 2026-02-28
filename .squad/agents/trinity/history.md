# Project Context

- **Owner:** Bruno Capuano
- **Project:** ElBruno.PersonaPlex — C# library wrapping NVIDIA PersonaPlex-7B-v1 for ONNX Runtime inference
- **Stack:** C#, .NET, ONNX Runtime, NuGet packaging, async/await patterns
- **Created:** 2026-02-27

## Learnings

### 2026-02-27: Audit Fixes (Release Blockers #1, #2, #4, #8) — COMPLETED

**Trinity fixed 4 critical audit findings:**

1. **Path Traversal Validation (Issue #1):** ValidatePath() was checking original `path` for ".." but validating against `fullPath`. Changed to validate `fullPath` consistently, preventing obfuscated traversal patterns.

2. **Disposed Session Checks (Issue #2):** EncodeSingleChunk() and DecodeSingleChunk() used null-forgiving `!` operators on sessions that could be disposed. Added `if (_disposed) throw new ObjectDisposedException()` checks at method entry to prevent race conditions during concurrent access.

3. **Test Project Build Hygiene (Issue #4):** Added `<NoWarn>$(NoWarn);CS1591</NoWarn>` to ElBruno.PersonaPlex.Tests.csproj, eliminating 38 CS1591 warnings from test classes/methods.

4. **ConversationResult.DurationMs (Issue #8):** Modified DecodeAsync() to return the decoded float array, allowing ProcessAsync() to calculate duration: `(outputSamples.Length / SampleRate) * 1000.0`. Property now correctly populated in ConversationResult.

**Verification:** `dotnet build` succeeded (no warnings). All 34 xUnit tests passed (net8.0 and net10.0).

**Risk Mitigation:** These are purely defensive/correctness fixes with no API-breaking changes. DecodeAsync signature changed (now returns Task<float[]> instead of Task), but this is internal to ProcessAsync workflow. Samples continue to work unchanged.

<!-- Append new learnings below. Each entry is something lasting about the project. -->

