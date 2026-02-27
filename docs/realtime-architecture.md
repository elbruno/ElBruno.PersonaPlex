# Real-Time Conversation Pipeline — Architecture

## Overview

The `ElBruno.PersonaPlex.Realtime` package family provides a pluggable, provider-agnostic pipeline for real-time audio conversations in .NET, following [Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai) patterns.

## Three-Layer Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  Layer 3: ORCHESTRATION                                         │
│  IRealtimeConversationClient                                    │
│  Chains: VAD → STT → LLM → TTS automatically                  │
│  DI: builder.Services.AddPersonaPlexRealtime()                 │
└──────────────────────────┬──────────────────────────────────────┘
                           │ uses
┌──────────────────────────┴──────────────────────────────────────┐
│  Layer 2: COMPONENT ABSTRACTIONS                                │
│                                                                  │
│  ISpeechToTextClient (M.E.AI)  │  ITextToSpeechClient (ours)   │
│  ├─ WhisperSpeechToTextClient  │  ├─ QwenTextToSpeechClient    │
│  └─ (any M.E.AI provider)     │  └─ (pluggable)               │
│                                │                                 │
│  IChatClient (M.E.AI)          │  IVoiceActivityDetector (ours) │
│  ├─ OllamaChatClient           │  ├─ SileroVadDetector          │
│  └─ OpenAIChatClient           │  └─ (pluggable)               │
└─────────────────────────────────────────────────────────────────┘
                           │ uses
┌──────────────────────────┴──────────────────────────────────────┐
│  Layer 1: MODEL ENGINES                                         │
│  Whisper.net (GGML) │ QwenTTS (ONNX) │ Silero VAD (ONNX)      │
│  ONNX Runtime       │ Ollama          │ Microsoft.Extensions.AI │
└─────────────────────────────────────────────────────────────────┘
```

## Microsoft.Extensions.AI Integration

### Interfaces We Implement (from M.E.AI)

- **`ISpeechToTextClient`** — Our `WhisperSpeechToTextClient` implements this experimental M.E.AI interface
- **`IChatClient`** — We consume any registered `IChatClient` (Ollama, OpenAI, Azure, etc.)

### Interfaces We Define (following M.E.AI patterns)

- **`ITextToSpeechClient`** — No official TTS interface exists in M.E.AI yet. Ours follows the same patterns (batch + streaming, options, service discovery)
- **`IVoiceActivityDetector`** — Audio stream → speech segments
- **`IRealtimeConversationClient`** — High-level pipeline orchestration

## Data Flow

### One-Shot Turn (`ProcessTurnAsync`)

```
Audio Stream → ISpeechToTextClient.GetTextAsync()
                    → text
                        → IChatClient.GetResponseAsync()
                            → response text
                                → ITextToSpeechClient.GetSpeechAsync()
                                    → ConversationTurn { UserText, ResponseText, ResponseAudio }
```

### Streaming Conversation (`ConverseAsync`)

```
Audio Chunks → IVoiceActivityDetector.DetectSpeechAsync()
    │              → SpeechSegment
    │                   → ISpeechToTextClient.GetTextAsync()
    │                       → ConversationEvent(TranscriptionComplete)
    │                           → IChatClient.GetStreamingResponseAsync()
    │                               → ConversationEvent(ResponseTextChunk) ×N
    │                                   → ITextToSpeechClient.GetStreamingSpeechAsync()
    │                                       → ConversationEvent(ResponseAudioChunk) ×N
    │                                           → ConversationEvent(ResponseComplete)
    └──(next segment)──►
```

## NuGet Package Dependencies

```
ElBruno.PersonaPlex.Realtime
├── Microsoft.Extensions.AI.Abstractions
└── Microsoft.Extensions.DependencyInjection.Abstractions

ElBruno.PersonaPlex.Realtime.Whisper
├── ElBruno.PersonaPlex.Realtime
├── Whisper.net
└── Whisper.net.AllRuntimes

ElBruno.PersonaPlex.Realtime.QwenTTS
├── ElBruno.PersonaPlex.Realtime
└── ElBruno.QwenTTS

ElBruno.PersonaPlex.Realtime.SileroVad
├── ElBruno.PersonaPlex.Realtime
├── ElBruno.HuggingFace.Downloader
└── Microsoft.ML.OnnxRuntime
```

## Auto-Downloaded Models

All models are downloaded on first use and cached locally:

| Model | Size | Cache Location |
|-------|------|---------------|
| Whisper tiny.en | ~75 MB | `%LOCALAPPDATA%/ElBruno/PersonaPlex/whisper-models/` |
| Whisper base.en | ~142 MB | `%LOCALAPPDATA%/ElBruno/PersonaPlex/whisper-models/` |
| Silero VAD v5 | ~2 MB | `%LOCALAPPDATA%/ElBruno/PersonaPlex/silero-vad/` |
| QwenTTS | ~5.5 GB | QwenTTS default location |
| Ollama phi4-mini | ~2.7 GB | Ollama registry (manual `ollama pull`) |
