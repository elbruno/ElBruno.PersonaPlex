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
2. **Ollama** installed and running locally — [ollama.com](https://ollama.com)
3. **phi4-mini model** pulled: `ollama pull phi4-mini`

## How to Run

```bash
# 1. Start Ollama (if not already running):
ollama serve

# 2. Pull the model (first time only):
ollama pull phi4-mini

# 3. From the repo root:
cd src/samples/scenario-04-blazor-aspire

# 4. Run the Aspire AppHost (starts API + Web):
dotnet run --project scenario-04.AppHost
```

### What happens when you run it:

1. **Aspire starts the API backend** — connects to Ollama at `http://localhost:11434`, exposes SignalR hub
2. **Aspire starts the Blazor frontend** — connects to API via SignalR
3. **Aspire Dashboard opens** — shows all services, logs, traces

### Using Docker-managed Ollama (optional)

If you prefer Aspire to manage Ollama via Docker instead of running it locally, edit `scenario-04.AppHost/Program.cs` and uncomment the Docker-based Ollama section. This requires Docker Desktop to be running.

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
| AI Framework | **Microsoft Agent Framework** | 1.0.0-rc2 | `AIAgent` + `OllamaChatClient` ([docs](https://learn.microsoft.com/agent-framework/agents/providers/ollama)) |
| AI Abstractions | **Microsoft.Extensions.AI.Ollama** | 9.7.0-preview | `OllamaChatClient` as `IChatClient` |
| LLM | **Ollama (phi4-mini)** | latest | 3.8B params, fast, runs locally |
| Orchestration | **.NET Aspire** | 13.1.2 | Service discovery, telemetry, container management |

## Microsoft Agent Framework Integration

This scenario follows the [official Microsoft Agent Framework + Ollama pattern](https://learn.microsoft.com/agent-framework/agents/providers/ollama).

### How it works

**1. Register OllamaChatClient as IChatClient (Program.cs):**

```csharp
// Microsoft.Extensions.AI.Ollama provides OllamaChatClient
builder.Services.AddChatClient(new OllamaChatClient(
        new Uri(ollamaEndpoint), ollamaModel))
    .UseFunctionInvocation()    // Enable function/tool calling
    .UseOpenTelemetry()         // Traces visible in Aspire dashboard
    .UseLogging();              // Log all AI interactions
```

**2. One-shot agent query (Agent Framework pattern):**

```csharp
using Microsoft.Agents.AI;

// Create an AIAgent from the IChatClient — this is the Agent Framework pattern
var agent = chatClient.AsAIAgent(
    instructions: "You are a helpful assistant running locally via Ollama.");

var result = await agent.RunAsync("What is the largest city in France?");
Console.WriteLine(result.Text);
```

**3. Multi-turn streaming conversation (ConversationService):**

```csharp
// For multi-turn chat, we manage history per session and stream tokens
await foreach (var token in chatClient.GetStreamingResponseAsync(chatHistory))
{
    yield return token.Text;  // Stream each token to the Blazor UI via SignalR
}
```

### Packages used

```xml
<PackageReference Include="Microsoft.Extensions.AI" Version="10.3.0" />
<PackageReference Include="Microsoft.Extensions.AI.Ollama" Version="9.7.0-preview.1.25356.2" />
<PackageReference Include="Microsoft.Agents.AI" Version="1.0.0-rc2" />
```

## Changing the Ollama Model

Pull a different model and update `scenario-04.Api/appsettings.json` (or set the `Ollama:Model` config):

```bash
ollama pull llama3.2
```

Then set the model name in the API config or environment variable:

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "Model": "llama3.2"
  }
}
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
