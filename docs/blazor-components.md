# Blazor Components (shipped contract)

This document describes the shipped `ElBruno.PersonaPlex.BlazorComponents` Razor Class Library.

It is part of the **1.0.0** release and is included in the NuGet package with a package-specific README.

## Scope

- Host PersonaPlex from a **server-backed** Blazor app.
- Treat the browser as the UI and audio transport layer.
- Keep ONNX Runtime inference on the server.
- Build on the existing `PersonaPlexPipeline`, `PersonaPlexOptions`, `VoicePreset`, `SessionOptionsHelper`, and `ConversationResult` APIs.
- Use a **half-duplex, turn-based** interaction model rather than full-duplex streaming.

## Explicit limitations

- **Half-duplex only**: the current runtime is file-based encode → decode, not true streaming speech-to-speech.
- **No browser-side inference**: Blazor WebAssembly should not be expected to load or run PersonaPlex models locally.
- **No streaming token API**: the current pipeline does not expose incremental LM tokens or audio frames.

## Component contract

### `SpeechToSpeechPanel`

Primary turn-based conversation surface.

Responsibilities:

- capture microphone input from the host app
- submit an audio turn to the server
- show the current turn state
- render the returned audio and metadata

### `PersonaStatusCard`

Pipeline state card for:

- loading
- ready
- error
- cache/model directory information

### `ConversationTurnVisualizer`

Conversation history view for:

- user audio
- transcript or prompt text
- generated audio
- turn duration and inference time

### `AudioWaveform`

Visualizes live microphone input or generated playback audio.

## Hosting model

The expected fit is **Blazor Server** or another server-backed interactive web app.

That keeps model loading, caching, and `PersonaPlexPipeline` execution on the server where the ONNX Runtime dependencies already live.

## Current runtime contract to anchor against

- `PersonaPlexPipeline.CreateAsync(...)` creates the pipeline and downloads models when needed.
- `PersonaPlexPipeline.ProcessAsync(...)` performs the current round-trip audio flow.
- `PersonaPlexOptions` controls model directory, voice preset, text prompt, seed, and execution provider.
- `VoicePreset` currently exposes 18 voice presets.
- `SessionOptionsHelper` provides CPU, CUDA, and DirectML session options.

## References

- [README](../README.md)
- [Samples Guide](samples-guide.md)
- [Architecture](architecture.md)
