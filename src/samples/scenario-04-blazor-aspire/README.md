# Scenario 04 — Blazor + Aspire + Ollama Conversation

A real-time conversation app that combines a **Blazor Server** frontend with an **Ollama-powered** AI backend, orchestrated by **.NET Aspire**.

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                  .NET Aspire AppHost                     │
│         (Orchestration · Dashboard · Telemetry)          │
└──┬──────────────────┬──────────────────┬────────────────┘
   │                  │                  │
   ▼                  ▼                  ▼
┌──────────────┐  ┌───────────────┐  ┌──────────────┐
│  Blazor Web  │  │   API Backend │  │    Ollama     │
│  (Server)    │  │  (ASP.NET     │  │  (Container)  │
│              │  │   Core)       │  │  phi4-mini    │
│  Chat UI     │  │  SignalR Hub  │  │              │
│  SignalR     │  │  M.E.AI       │  │  REST API    │
│  Client      │  │  Streaming    │  │  :11434      │
└──────┬───────┘  └──────┬────────┘  └──────┬───────┘
       │                 │                  │
       │  SignalR        │  OpenAI-compat   │
       │  (streaming)    │  HTTP API        │
       └────────────────►└─────────────────►┘
```

### Data Flow

```
User types message
    │
    ▼
Blazor (SignalR) ──► API Hub ──► M.E.AI ──► Ollama (phi4-mini)
                                                │
                                    streaming tokens
                                                │
User sees response ◄── Blazor ◄── SignalR ◄─────┘
```

## Prerequisites

1. **.NET 10 SDK** (or .NET 9 SDK)
2. **Docker Desktop** — for running the Ollama container
3. That's it! Aspire handles everything else automatically.

## How to Run

```bash
# From the repo root:
cd src/samples/scenario-04-blazor-aspire

# Run the Aspire AppHost (starts everything):
dotnet run --project scenario-04.AppHost
```

### What happens when you run it:

1. **Aspire starts the Ollama Docker container** — pulls `phi4-mini` model on first run (~2.5 GB download)
2. **Aspire starts the API backend** — connects to Ollama, exposes SignalR hub
3. **Aspire starts the Blazor frontend** — connects to API via SignalR
4. **Aspire Dashboard opens** — shows all services, logs, traces at `http://localhost:15888`

### First-run note

The first run downloads the Ollama container image AND the phi4-mini model. This can take **5-10 minutes** depending on your internet connection. Subsequent runs start in seconds because everything is cached in Docker volumes.

## Using the App

1. Open the **Blazor Web** endpoint from the Aspire dashboard (or the URL printed in console)
2. Navigate to `/conversation`
3. Type a message and press Enter or click Send
4. Watch the AI response stream in real-time, token by token

### Features

- **Streaming responses** — tokens appear as Ollama generates them
- **Multi-turn conversation** — context is maintained across messages
- **Custom persona** — set a system prompt (e.g., "You are a pirate captain")
- **Session management** — clear history and start fresh
- **Connection status** — visual indicator for SignalR connection health

## Key Technology Choices

| Component | Technology | Version | Why |
|-----------|-----------|---------|-----|
| Frontend | **Blazor Server** | .NET 10 | SignalR built-in, server-side rendering |
| Communication | **SignalR + MessagePack** | 10.0.3 | Binary streaming, auto-reconnect |
| AI Framework | **Microsoft.Extensions.AI** | 10.3.0 | Lightweight `IChatClient`, Ollama-compatible |
| LLM | **Ollama (phi4-mini)** | latest | 3.8B params, fast, runs locally |
| Orchestration | **.NET Aspire** | 13.1.2 | Service discovery, telemetry, container management |

## Changing the Ollama Model

Edit `scenario-04.AppHost/Program.cs`:

```csharp
var ollama = builder.AddOllama("ollama")
    .WithDataVolume("personaplex-ollama-data")
    .AddModel("phi4-mini");       // ← Change this
```

Popular options:
| Model | Size | Speed | Quality |
|-------|------|-------|---------|
| `phi4-mini` | ~2.5 GB | ⚡ Fast | Good |
| `llama3.2` | ~2 GB | ⚡ Fast | Good |
| `llama3.1:8b` | ~4.7 GB | Medium | Better |
| `phi4` | ~9 GB | Slower | Best |

## Project Structure

```
scenario-04-blazor-aspire/
├── scenario-04.AppHost/           # Aspire orchestrator
│   └── Program.cs                 # Ollama + API + Web wiring
├── scenario-04.ServiceDefaults/   # Shared telemetry/health
│   └── Extensions.cs
├── scenario-04.Api/               # ASP.NET Core backend
│   ├── Program.cs                 # DI, SignalR, M.E.AI setup
│   ├── Hubs/
│   │   └── ConversationHub.cs     # SignalR hub (streaming)
│   └── Services/
│       └── ConversationService.cs # Multi-turn chat with Ollama
├── scenario-04.Web/               # Blazor Server frontend
│   ├── Program.cs
│   ├── Components/
│   │   ├── App.razor
│   │   ├── Routes.razor
│   │   ├── Layout/MainLayout.razor
│   │   └── Pages/
│   │       ├── Index.razor        # Home page
│   │       └── Conversation.razor # Chat UI
│   └── wwwroot/css/app.css
└── scenario-04.Shared/            # Shared DTOs
    └── Models/
        ├── AudioChunkDto.cs
        ├── ChatMessageDto.cs
        └── ConversationStateDto.cs
```

## Future: PersonaPlex Audio Integration

When the PersonaPlex ONNX models are fully exported, this scenario will be extended to support:

```
User speaks → Mimi Encoder → Ollama reasoning → Mimi Decoder → AI speaks back
```

The `ConversationHub.ProcessAudio()` method has a placeholder ready for this integration. See the [evaluation document](../../../docs/scenario-04-blazor-aspire-evaluation.md) for the full roadmap.
