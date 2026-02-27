# GPU Acceleration

PersonaPlex supports multiple execution providers through ONNX Runtime for hardware-accelerated inference.

## Execution Providers

| Provider | Package | GPU Vendor | Platform |
|----------|---------|-----------|----------|
| **CPU** | `Microsoft.ML.OnnxRuntime` (default) | N/A | All |
| **CUDA** | `Microsoft.ML.OnnxRuntime.Gpu` | NVIDIA | Windows, Linux |
| **DirectML** | `Microsoft.ML.OnnxRuntime.DirectML` | Any (AMD, Intel, NVIDIA) | Windows |

## Setup

### CUDA (NVIDIA GPUs)

1. Install the CUDA NuGet package:
```bash
dotnet add package Microsoft.ML.OnnxRuntime.Gpu
```

2. Use CUDA session options:
```csharp
using ElBruno.PersonaPlex.Pipeline;

var pipeline = await PersonaPlexPipeline.CreateAsync(
    sessionOptionsFactory: SessionOptionsHelper.CreateCudaOptions);
```

### DirectML (Windows, any GPU)

1. Install the DirectML NuGet package:
```bash
dotnet add package Microsoft.ML.OnnxRuntime.DirectML
```

2. Use DirectML session options:
```csharp
using ElBruno.PersonaPlex.Pipeline;

var pipeline = await PersonaPlexPipeline.CreateAsync(
    sessionOptionsFactory: SessionOptionsHelper.CreateDirectMlOptions);
```

### CPU (Default)

No additional packages needed — CPU execution is included by default:
```csharp
var pipeline = await PersonaPlexPipeline.CreateAsync(); // Uses CPU
```

## Memory Requirements

Given the 7B parameter model size, ensure adequate GPU VRAM:

| Quantization | Estimated VRAM |
|-------------|---------------|
| FP32 | ~28 GB |
| FP16 | ~14 GB |
| INT8 | ~7 GB |
| INT4 | ~4 GB |

> **Tip:** If your GPU has insufficient VRAM, use CPU execution or apply more aggressive quantization to the ONNX models.
