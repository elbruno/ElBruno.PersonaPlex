# Copilot Instructions

## Application

This repo contains **ElBruno.PersonaPlex** — a C# .NET library for running NVIDIA PersonaPlex-7B-v1 full-duplex speech-to-speech locally using ONNX Runtime. The Core library is published to NuGet as `ElBruno.PersonaPlex`. Pre-exported ONNX models are hosted on HuggingFace at `elbruno/personaplex-7b-v1-ONNX`.

## Build & Test

```bash
dotnet build          # Build all projects
dotnet test           # Run xUnit tests (ElBruno.PersonaPlex.Tests)
dotnet pack src/ElBruno.PersonaPlex/ElBruno.PersonaPlex.csproj -c Release -o artifacts  # Create NuGet package
```

## Project Structure

```
ElBruno.PersonaPlex.slnx                # Solution file
src/ElBruno.PersonaPlex/                 # Core library (NuGet: ElBruno.PersonaPlex, targets net8.0+net10.0)
  Pipeline/PersonaPlexPipeline.cs        # Full S2S orchestrator + CreateAsync factory
  Audio/WavWriter.cs                     # WAV file writer (24 kHz, 16-bit PCM)
  ModelManager.cs                        # Auto-download models from HuggingFace
  PersonaPlexOptions.cs                  # Configuration options
  ConversationResult.cs                  # Inference result model
  VoicePreset.cs                         # Voice preset enum (NATF/NATM/VARF/VARM)
  ExecutionProvider.cs                   # CPU/CUDA/DirectML enum
  SessionOptionsHelper.cs               # ONNX session configuration
  DownloadProgress.cs                    # Download progress reporting
src/ElBruno.PersonaPlex.Tests/           # xUnit unit tests
src/samples/scenario-01-simple/          # Basic speech-to-speech demo
src/samples/scenario-02-persona/         # Custom persona prompts demo
src/samples/scenario-03-voice-select/    # Voice selection demo
docs/                                    # All documentation
python/                                  # ONNX export & HuggingFace tools (optional)
images/                                  # NuGet icon, README images
```

## Key Technical Details

- **Shared model directory**: `ModelManager.DefaultModelDir` → `%LOCALAPPDATA%/ElBruno/PersonaPlex/models` (Windows).
- **PersonaPlexPipeline.CreateAsync()**: Factory method that auto-downloads models if missing.
- **HuggingFace URL pattern**: `https://huggingface.co/{repoId}/resolve/main/{filename}` — no API token needed for public repos.
- **Voice presets**: 18 voices — NATF0-3, NATM0-3, VARF0-4, VARM0-4 (from pre-computed .pt embeddings).

## Documentation Convention

- **Only `README.md` and `LICENSE` live at the repo root.** All other documentation (including CHANGELOG) goes in `docs/`.
- Documentation files use kebab-case naming (e.g., `docs/getting-started.md`, `docs/architecture.md`).
- The main `README.md` should be concise with quick start commands and links to `docs/` for details.
- Do NOT create documentation files at the repo root — always place them in `docs/`.

## NuGet Publishing

- **Package name**: `ElBruno.PersonaPlex` (PackageId in csproj)
- **Workflow**: `.github/workflows/publish.yml` — triggered on GitHub release or manual dispatch
- **Authentication**: OIDC via `NuGet/login@v1`, requires `NUGET_USER` secret in `release` environment
- **Version**: Determined from release tag (`v1.0.0` → `1.0.0`), manual input, or csproj fallback

## Key Conventions

- Target frameworks: net8.0 and net10.0
- Use `async/await` throughout; pipeline follows `CreateAsync` factory pattern
- All public APIs must have XML documentation comments
- Suppress CS1591 for internal/private members only
- Use `IDisposable` for ONNX session lifecycle management
