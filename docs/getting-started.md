# Getting Started

## Prerequisites

- **.NET 8.0**, **.NET 10.0**, or **.NET 11.0** SDK
- ONNX Runtime compatible platform (Windows, Linux, macOS)
- Sufficient disk space for model files (size TBD after ONNX export)

> .NET 8.0 targeting is retained for compatibility, but .NET 8 reached end of support on November 10, 2026. Prefer .NET 10 or .NET 11 for supported deployments. .NET 11 also requires x86/x64 hardware with the x86-64-v2 instruction set; see the [.NET 11 minimum hardware requirements](https://learn.microsoft.com/dotnet/core/compatibility/jit/11/minimum-hardware-requirements).

## Installation

```bash
dotnet add package ElBruno.PersonaPlex
```

## First Run

```csharp
using ElBruno.PersonaPlex.Pipeline;

// Models download automatically on first run
using var pipeline = await PersonaPlexPipeline.CreateAsync("models");

// Process speech-to-speech
var result = await pipeline.ProcessAsync(
    inputAudioPath: "input.wav",
    outputPath: "output.wav");

Console.WriteLine($"Output saved to: {result.OutputAudioPath}");
```

On first run, the pipeline will automatically download the required ONNX model files from HuggingFace. Subsequent runs use the cached models.

## Model Cache

Models are stored by default at:
- **Windows**: `%LOCALAPPDATA%\ElBruno\PersonaPlex\models`
- **Linux/macOS**: `~/.local/share/ElBruno/PersonaPlex/models`

You can override the model directory:
```csharp
using var pipeline = await PersonaPlexPipeline.CreateAsync("/path/to/custom/models");
```

## Next Steps

- [Architecture](architecture.md) — Pipeline design and model components
- [GPU Acceleration](gpu-acceleration.md) — Configure CUDA or DirectML
- [Exporting Models](exporting-models.md) — Re-export ONNX models from PyTorch
