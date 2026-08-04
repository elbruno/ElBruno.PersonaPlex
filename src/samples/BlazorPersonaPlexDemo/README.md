# BlazorPersonaPlexDemo

Server-backed, model-free Blazor Server sample for issue #6.

## What it demonstrates

- Proposed PersonaPlex UI component contracts rendered in a Blazor Server app
- One page each for `SpeechToSpeechPanel`, `PersonaStatusCard`, `ConversationTurnVisualizer`, and `AudioWaveform`
- Interactive server-backed fake sessions plus seeded/canned review states
- Collapsible code panels powered by `CodeSnippets.cs`
- Bootstrap 5.3.x and Bootstrap Icons loaded from CDN in `Components/App.razor`

## Why this sample is model-free

The app references `ElBruno.PersonaPlex` only for shared types such as `VoicePreset` and for model-cache hints. It never creates `PersonaPlexPipeline` during startup, so running the sample does **not** trigger ONNX downloads, GPU checks, or model initialization.

## Run

```bash
dotnet run --project src/samples/BlazorPersonaPlexDemo/BlazorPersonaPlexDemo.csproj
```

Then open the local URL shown in the console and browse the component pages from the left navigation.

## Files of interest

- `Program.cs` — Blazor Server + demo session DI wiring
- `CodeSnippets.cs` — collapsible sample snippets
- `Demo/` — server-backed fake session contracts and implementation
- `Components/PersonaPlex/` — demo component implementations
- `Components/Pages/` — page-per-component sample surface
