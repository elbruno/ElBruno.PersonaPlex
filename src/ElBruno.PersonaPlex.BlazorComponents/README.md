# ElBruno.PersonaPlex.BlazorComponents

Server-backed Blazor UI components for **ElBruno.PersonaPlex**.

This package is designed for **half-duplex, turn-based** speech-to-speech experiences:

- the browser records microphone input and plays response audio
- the server owns model loading, ONNX Runtime execution, and WAV file processing
- one turn is captured, sent to the server, processed, and then played back

It does **not** provide browser-side inference, streaming token updates, or full-duplex audio streaming.

## Install

```bash
dotnet add package ElBruno.PersonaPlex.BlazorComponents
```

The components depend on the core runtime package and bring it in automatically through NuGet.

## Expected hosting model

Use this package from a **server-backed Blazor app** such as **Blazor Server** or a Blazor Web App using **Interactive Server** rendering.

Keep PersonaPlex inference on the server. The current runtime contract is file-based and turn-based, not continuous streaming.

## Register services

In `Program.cs`, register the component services and configure the underlying pipeline:

```csharp
using ElBruno.PersonaPlex;
using ElBruno.PersonaPlex.BlazorComponents.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddPersonaPlexBlazorComponents(options =>
{
    options.WorkingDirectory = Path.Combine(builder.Environment.ContentRootPath, "personaplex-ui");
    options.PipelineOptions.ModelDirectory = Path.Combine(builder.Environment.ContentRootPath, "models");
    options.PipelineOptions.TextPrompt = "You are a helpful assistant.";
    options.PipelineOptions.VoicePreset = VoicePreset.NATF2;

    // Optional: customize ONNX Runtime execution
    // options.SessionOptionsFactory = SessionOptionsHelper.CreateCudaOptions;
    // options.SessionOptionsFactory = SessionOptionsHelper.CreateDirectMlOptions;
});
```

## Import the components

Add the namespace to `_Imports.razor`:

```razor
@using ElBruno.PersonaPlex.BlazorComponents.Components
@using ElBruno.PersonaPlex.BlazorComponents.Models
```

## Use the components

```razor
<PersonaStatusCard ShowSessionTimer="true" />

<SpeechToSpeechPanel ShowAvatar="true"
                     PushToTalk="false" />

<ConversationTurnVisualizer ShowTranscript="true"
                            MaxTurns="10" />
```

The default flow uses the built-in `PersonaPlexStateService`:

1. the first render creates a server-backed session
2. microphone audio is captured in the browser
3. the component writes WAV turn files in the configured server working directory
4. `PersonaPlexPipeline` processes the turn on the server
5. the generated WAV response is returned for browser playback

Advanced hosts can also pass their own `IPersonaSession` instance to `SpeechToSpeechPanel` and `PersonaStatusCard`.

## Included components

### `SpeechToSpeechPanel`

Primary turn UI. Records microphone audio, submits one turn to the server, shows busy/error state, and plays the latest response.

### `PersonaStatusCard`

Displays server session state, model directory, working directory, timer, and error details.

### `ConversationTurnVisualizer`

Shows recent turns with user audio, generated audio, transcripts, inference time, and selected voice.

### `AudioWaveform`

Canvas-based waveform for microphone capture or playback audio.

## Limitations

- **Server-backed only**: no local WebAssembly/browser inference.
- **Half-duplex only**: one recorded turn in, one generated response out.
- **File-based runtime**: current processing uses WAV files on the server.
- **No streaming APIs**: no incremental token stream or partial audio response stream.
- **Browser microphone access required**: the host app must run in a context that allows audio capture.

## Related docs

- Root package and runtime overview: repository `README.md`
- Blazor package contract: `docs/blazor-components.md`
- Sample app guide: `docs/samples-guide.md`
