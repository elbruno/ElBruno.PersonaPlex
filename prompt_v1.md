Analyze the goal below and create a team and a plan to implement this.

# Goal

Build a new C# .NET library that wraps the [MODEL_URL] model
for local inference using ONNX Runtime — no Python needed at runtime.

## Package Name
- Proposed: `ElBruno.{ModelName}` (e.g., ElBruno.PersonaPlex)

## Requirements

### ONNX Export
- Research whether the model can be exported to ONNX from its PyTorch source
- If the model has multiple components (encoder, decoder, LM backbone, etc.),
  export each as a separate ONNX file
- Apply quantization (INT4/INT8) if the model is large (>1B params)
- Create Python export scripts in a `python/` directory
- Upload exported ONNX models to a new HuggingFace repo: `elbruno/{model-name}-ONNX`

### C# Library
- Core NuGet package with ONNX Runtime inference pipeline
- Auto-download models from HuggingFace on first run (use `ElBruno.HuggingFace.Downloader`)
- Factory pattern: `await Pipeline.CreateAsync("models")` (see reference projects)
- GPU acceleration support: CPU / CUDA / DirectML via SessionOptions injection
- Target frameworks: net8.0 and net10.0
- NuGet metadata: MIT license, package icon, README, tags

### Publishing & CI/CD
- GitHub Actions workflow for NuGet publishing using OIDC Trusted Publishing
- Trigger on release tags + manual workflow_dispatch
- Build → Test → Pack → Push pipeline

### Samples
- `src/samples/scenario-01-simple/` — Basic usage (minimal code to get started)
- `src/samples/scenario-02-{feature}/` — Model-specific feature demo
- `src/samples/scenario-03-{feature}/` — Another key scenario
- Each sample is a standalone console app referencing the core library

### Documentation
- README.md with NuGet badges, quick start, API examples, prerequisites
- `docs/` folder: getting-started, architecture, exporting-models, gpu-acceleration
- CHANGELOG.md

## Team Requirements

Create a team with AT LEAST these specialists:
- **Lead/Architect** — architecture decisions, code review, quality gates
- **ML/Python Dev** — ONNX export, model research, quantization, HuggingFace upload
- **C# Dev** — core library, ONNX Runtime pipeline, NuGet package, samples
- **Tester** — xUnit tests alongside implementation
- **DevOps** — CI/CD, GitHub Actions, solution structure, packaging
- **Security Specialist** — reviews ALL work: supply chain security (NuGet/HF downloads),
  dependency auditing, input validation, safe model loading, secret management in CI/CD
- **ONNX Transformation Specialist** — reviews all ONNX export scripts and C# inference
  code for correctness, quantization accuracy, tensor shapes, execution providers

### Review Pipeline (mandatory)
Every piece of work must pass through:
1. Security review (all work)
2. ONNX review (model-touching work)
3. Lead sign-off
before being considered done.

## Repo Structure (follow this pattern)

```text
{RepoName}.slnx
├── src/
│   ├── ElBruno.{ModelName}/          # Core library (NuGet package)
│   │   ├── Pipeline/                 # Main inference pipeline
│   │   ├── Models/                   # Options, results, presets
│   │   ├── ModelManager.cs           # Auto-download from HuggingFace
│   │   ├── DownloadProgress.cs
│   │   ├── ExecutionProvider.cs
│   │   └── SessionOptionsHelper.cs
│   ├── ElBruno.{ModelName}.Tests/    # xUnit tests
│   └── samples/                      # Console app samples
├── python/                           # ONNX export & upload scripts
├── docs/                             # Documentation
├── images/                           # NuGet icon, README images
├── .github/workflows/publish.yml     # NuGet publishing (OIDC)
├── .gitignore
├── .gitattributes
├── LICENSE (MIT)
├── CHANGELOG.md
├── Directory.Build.props
└── README.md
```

## References

Follow the patterns from these existing libraries:
- https://github.com/elbruno/ElBruno.QwenTTS (best — same ONNX + audio model pattern)
- https://github.com/elbruno/ElBruno.VibeVoiceTTS (voice TTS ONNX pattern)
- https://github.com/elbruno/ElBruno.Text2Image (multi-package NuGet, image generation)
