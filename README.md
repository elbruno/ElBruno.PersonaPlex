# ElBruno.PersonaPlex

[![NuGet](https://img.shields.io/nuget/v/ElBruno.PersonaPlex.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ElBruno.PersonaPlex)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ElBruno.PersonaPlex.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ElBruno.PersonaPlex)
[![Build Status](https://github.com/elbruno/ElBruno.PersonaPlex/actions/workflows/publish.yml/badge.svg)](https://github.com/elbruno/ElBruno.PersonaPlex/actions/workflows/publish.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/elbruno/ElBruno.PersonaPlex?style=social)](https://github.com/elbruno/ElBruno.PersonaPlex)
[![Twitter Follow](https://img.shields.io/twitter/follow/elbruno?style=social)](https://twitter.com/elbruno)

**HuggingFace Models:**
[![PersonaPlex ONNX](https://img.shields.io/badge/🤗%20HuggingFace-PersonaPlex%207B%20v1%20ONNX-yellow?style=flat-square)](https://huggingface.co/elbruno/personaplex-7b-v1-onnx)
[![PersonaPlex Original](https://img.shields.io/badge/🤗%20HuggingFace-NVIDIA%20PersonaPlex%207B%20v1-blue?style=flat-square)](https://huggingface.co/nvidia/personaplex-7b-v1)

Run **NVIDIA PersonaPlex-7B-v1** full-duplex speech-to-speech locally from C# using ONNX Runtime — no Python needed at inference time. Models are downloaded automatically on first run.

Pre-exported ONNX models are hosted on HuggingFace:
[**elbruno/personaplex-7b-v1-onnx**](https://huggingface.co/elbruno/personaplex-7b-v1-onnx)

## Features

- **Local Speech-to-Speech Inference** — Run PersonaPlex entirely on your machine using ONNX Runtime
- **Automatic Model Download** — Models download from HuggingFace on first run
- **Full-Duplex Capable** — Based on NVIDIA's Moshi architecture for simultaneous listen + speak
- **Persona Control** — Text-based role prompts for customizable AI personas
- **Voice Selection** — Multiple pre-packaged voice embeddings (Natural + Variety voices)
- **GPU Acceleration** — Optional CUDA or DirectML support via SessionOptions injection
- **Multi-Language** — Supports conversational interactions in English
- **Shared Model Cache** — Models stored once in `%LOCALAPPDATA%/ElBruno/PersonaPlex`, shared across all apps

---

## Quick Start

### Install via NuGet

```bash
dotnet add package ElBruno.PersonaPlex
```

### Basic usage in C#

```csharp
using ElBruno.PersonaPlex.Pipeline;

// Models download automatically on first run
using var pipeline = await PersonaPlexPipeline.CreateAsync("models");

// Speech-to-speech with persona
await pipeline.ProcessAsync(
    inputAudioPath: "input.wav",
    voicePreset: VoicePreset.NATF2,
    textPrompt: "You are a friendly assistant.",
    outputPath: "output.wav");
```

### GPU Acceleration

```csharp
using ElBruno.PersonaPlex.Pipeline;

// CUDA (NVIDIA)
var pipeline = await PersonaPlexPipeline.CreateAsync(
    sessionOptionsFactory: SessionOptionsHelper.CreateCudaOptions);

// DirectML (any GPU on Windows)
var pipeline = await PersonaPlexPipeline.CreateAsync(
    sessionOptionsFactory: SessionOptionsHelper.CreateDirectMlOptions);
```

## Supported Voices

PersonaPlex includes pre-packaged voice embeddings:

| Category | Voices |
|----------|--------|
| **Natural (Female)** | NATF0, NATF1, NATF2, NATF3 |
| **Natural (Male)** | NATM0, NATM1, NATM2, NATM3 |
| **Variety (Female)** | VARF0, VARF1, VARF2, VARF3, VARF4 |
| **Variety (Male)** | VARM0, VARM1, VARM2, VARM3, VARM4 |

---

## Real-Time Conversation Pipeline

The **ElBruno.PersonaPlex.Realtime** family of packages provides a complete real-time audio conversation pipeline following [Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai) patterns.

### Install

```bash
dotnet add package ElBruno.PersonaPlex.Realtime          # Core abstractions + pipeline
dotnet add package ElBruno.PersonaPlex.Realtime.Whisper   # Local STT (Whisper.net)
dotnet add package ElBruno.PersonaPlex.Realtime.QwenTTS   # Local TTS (QwenTTS)
dotnet add package ElBruno.PersonaPlex.Realtime.SileroVad # Local VAD (Silero)
```

### Quick Start

```csharp
// 1. Configure the pipeline (models auto-download on first use)
builder.Services.AddPersonaPlexRealtime(opts =>
{
    opts.DefaultSystemPrompt = "You are a helpful voice assistant.";
})
.UseWhisperStt("whisper-tiny.en")   // 75MB, or "whisper-base.en" for better accuracy
.UseQwenTts()                       // QwenTTS with multiple voice presets
.UseSileroVad();                    // Voice activity detection

// 2. Register your LLM (any IChatClient provider)
builder.Services.AddChatClient(new OllamaChatClient(
    new Uri("http://localhost:11434"), "phi4-mini"));

// 3. Use the pipeline
var conversation = app.Services.GetRequiredService<IRealtimeConversationClient>();

// One-shot turn
using var audio = File.OpenRead("question.wav");
var turn = await conversation.ProcessTurnAsync(audio);
Console.WriteLine($"User: {turn.UserText}");
Console.WriteLine($"AI: {turn.ResponseText}");

// Streaming full-duplex
await foreach (var evt in conversation.ConverseAsync(microphoneStream))
{
    if (evt.Kind == ConversationEventKind.ResponseTextChunk)
        Console.Write(evt.ResponseText);
}
```

### Packages

| Package | Description |
|---------|-------------|
| `ElBruno.PersonaPlex.Realtime` | Core: `ITextToSpeechClient`, `IVoiceActivityDetector`, `IRealtimeConversationClient`, pipeline orchestration, DI |
| `ElBruno.PersonaPlex.Realtime.Whisper` | `ISpeechToTextClient` (M.E.AI) via Whisper.net — auto-downloads GGML models |
| `ElBruno.PersonaPlex.Realtime.QwenTTS` | `ITextToSpeechClient` via ElBruno.QwenTTS — multiple voice presets |
| `ElBruno.PersonaPlex.Realtime.SileroVad` | `IVoiceActivityDetector` via Silero VAD v5 ONNX — configurable thresholds |

## Samples

See the [**Samples Guide**](docs/samples-guide.md) for detailed instructions on each scenario, including what test content to use and expected output.

| Sample | Description |
|--------|-------------|
| [scenario-01-simple](src/samples/scenario-01-simple/) | Basic speech-to-speech generation |
| [scenario-02-persona](src/samples/scenario-02-persona/) | Custom persona prompts |
| [scenario-03-voice-select](src/samples/scenario-03-voice-select/) | Voice selection demo |
| [scenario-04-blazor-aspire](src/samples/scenario-04-blazor-aspire/) | 🆕 Blazor + Aspire + Ollama real-time conversation with server-side Whisper STT |
| [scenario-05-model-download](src/samples/scenario-05-model-download/) | Model download, progress reporting & custom directory |
| [scenario-06-realtime-console](src/samples/scenario-06-realtime-console/) | 🆕 Real-time console: Audio → STT → LLM → TTS pipeline |
| [scenario-07-realtime-api](src/samples/scenario-07-realtime-api/) | 🆕 ASP.NET Core API with SignalR streaming conversation |

### Run a Sample

```bash
cd src/samples/scenario-01-simple
dotnet run -- myrecording.wav
```

## Documentation

| Document | Description |
|----------|-------------|
| [Getting Started](docs/getting-started.md) | Setup, auto-download, and first run |
| [Samples Guide](docs/samples-guide.md) | Detailed guide for each sample scenario |
| [Architecture](docs/architecture.md) | Pipeline design, model components, project structure |
| [Realtime Architecture](docs/realtime-architecture.md) | 🆕 Real-time conversation pipeline design, M.E.AI integration |
| [Exporting Models](docs/exporting-models.md) | Re-exporting ONNX models from PyTorch weights |
| [GPU Acceleration](docs/gpu-acceleration.md) | CUDA, DirectML, and CPU configuration |
| [Publishing](docs/publishing.md) | NuGet publishing guide (Trusted Publishing / OIDC) |

## Python Tools

The `python/` directory contains tools for **exporting ONNX models from PyTorch weights**. These are only needed if you want to re-export or customize models — they are not required for running the C# pipeline.

### Exported ONNX Models

| Model | Size | Description |
|-------|------|-------------|
| `mimi_encoder.onnx` | 178 MB | Audio → discrete tokens (Mimi audio codec encoder) |
| `mimi_decoder.onnx` | 170 MB | Discrete tokens → audio (Mimi audio codec decoder) |
| `lm_backbone.onnx` + `.data` | 13.3 GB (FP16) | Full 7B-param Transformer backbone (32 attention layers, 4096 dim) |

The **Mimi encoder/decoder** models are hosted on HuggingFace and auto-downloaded by the C# library.

The **LM backbone** (7B parameters) can be exported locally using:

```bash
cd python
pip install -r requirements.txt
python export_onnx.py --component lm --output onnx_exports/ --moshi-path /path/to/personaplex/moshi --quantize fp16
```

> **Note:** The `onnxruntime-genai` model builder does NOT support the PersonaPlex/Moshi architecture (it's not a standard HuggingFace model type). The manual export via `export_onnx.py` works and produces a validated, loadable ONNX model. FP16 inference on CPU takes ~0.4s per step with ~25s model load time.

---

## Building from Source

```bash
git clone https://github.com/elbruno/ElBruno.PersonaPlex.git
cd ElBruno.PersonaPlex
dotnet build
dotnet test
```

## Requirements

- .NET 8.0 or .NET 10.0 SDK
- ONNX Runtime compatible platform (Windows, Linux, macOS)
- Sufficient disk space for model files

---

## Contributing

Contributions are welcome! Here's how to get started:

1. **Fork** the repository
2. **Create a branch** for your feature or fix: `git checkout -b feature/my-feature`
3. **Make your changes** and ensure the solution builds: `dotnet build`
4. **Run tests**: `dotnet test`
5. **Submit a pull request** with a clear description of the changes

Please open an issue first for major changes or new features to discuss the approach.

---

## References

- [NVIDIA PersonaPlex GitHub](https://github.com/NVIDIA/personaplex)
- [Original model (PyTorch)](https://huggingface.co/nvidia/personaplex-7b-v1)
- [PersonaPlex Paper](https://arxiv.org/abs/2602.06053)
- [Pre-exported ONNX models](https://huggingface.co/elbruno/personaplex-7b-v1-onnx)

---

## 👋 About the Author

Hi! I'm **ElBruno** 🧡, a passionate developer and content creator exploring AI, .NET, and modern development practices.

**Made with ❤️ by [ElBruno](https://github.com/elbruno)**

If you like this project, consider following my work across platforms:

- 📻 **Podcast**: [No Tienen Nombre](https://notienenombre.com) — Spanish-language episodes on AI, development, and tech culture
- 💻 **Blog**: [ElBruno.com](https://elbruno.com) — Deep dives on embeddings, RAG, .NET, and local AI
- 📺 **YouTube**: [youtube.com/elbruno](https://www.youtube.com/elbruno) — Demos, tutorials, and live coding
- 🔗 **LinkedIn**: [@elbruno](https://www.linkedin.com/in/elbruno/) — Professional updates and insights
- 𝕏 **Twitter**: [@elbruno](https://www.x.com/in/elbruno/) — Quick tips, releases, and tech news

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

## Related Projects

- [ElBruno.QwenTTS](https://github.com/elbruno/ElBruno.QwenTTS)
- [ElBruno.VibeVoiceTTS](https://github.com/elbruno/ElBruno.VibeVoiceTTS)
- [ElBruno.Text2Image](https://github.com/elbruno/ElBruno.Text2Image)
- [ElBruno.HuggingFace.Downloader](https://github.com/elbruno/ElBruno.HuggingFace.Downloader)
