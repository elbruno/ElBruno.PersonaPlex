# Scenario-04: Blazor + Aspire + Agent Framework — Feasibility Evaluation

> **Evaluators:** Neo (Architecture), Trinity (C#/.NET), Morpheus (ML/Infrastructure)
> **Requested by:** Bruno Capuano
> **Date:** 2026-02-27
> **Verdict:** ⚠️ **CONDITIONAL GO** — feasible with phased approach and known blockers

---

## Executive Summary

Scenario-04 proposes a **real-time conversational AI web application** combining:

1. **Blazor Server** frontend for audio capture/playback
2. **ASP.NET Core** backend hosting PersonaPlex ONNX models
3. **Microsoft.Extensions.AI** for LLM orchestration with Ollama
4. **Ollama** running a local 7B model for text reasoning
5. **.NET Aspire** orchestrating the full stack

**Bottom line:** All five components are individually proven and can integrate. Three blockers require mitigation:

| # | Blocker | Severity | Workaround |
|---|---------|----------|------------|
| 1 | PersonaPlex 7B LM backbone ONNX export incomplete | 🔴 Critical | Use Ollama as LM replacement (Encoder/Decoder only) |
| 2 | PersonaPlex API is file-based (not streaming) | 🟡 High | Accept for MVP; temp files with SignalR byte[] transfer |
| 3 | GPU contention running ONNX Runtime + Ollama | 🟡 High | INT4 quantization + time-slicing on single GPU |

**Recommendation:** Start with Phase 1 (text-only architecture validation), integrate PersonaPlex when the LM export completes.

---

## 1. Architecture Overview

### System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        .NET ASPIRE AppHost                              │
│              (Orchestration · Service Discovery · Dashboard)            │
└──┬──────────────────┬──────────────────┬───────────────────┬───────────┘
   │                  │                  │                   │
   ▼                  ▼                  ▼                   ▼
┌──────────────┐  ┌───────────────┐  ┌──────────────┐  ┌────────────────┐
│  Blazor Web  │  │   API Backend │  │    Ollama     │  │   Service      │
│  (Server)    │  │  (ASP.NET     │  │  (Container)  │  │   Defaults     │
│              │  │   Core)       │  │  llama3.2:7b  │  │ (Telemetry/    │
│  Audio UI    │  │  PersonaPlex  │  │              │  │  Health/Config) │
│  SignalR     │  │  M.E.AI       │  │  REST API    │  │               │
│  Client      │  │  SignalR Hub  │  │  :11434      │  │               │
└──────┬───────┘  └──────┬────────┘  └──────┬───────┘  └────────────────┘
       │                 │                  │
       │  SignalR        │  HTTP/REST       │
       │  (binary audio  │  (chat           │
       │   streaming)    │   completions)   │
       │                 │                  │
       ▼                 ▼                  │
┌──────────────────────────────────────────┘
│
│  ┌─────────────────────────────────────────────────────────┐
│  │                    Browser                              │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │  │ MediaRecorder │  │ Web Audio    │  │ SignalR      │  │
│  │  │ API (capture) │  │ API (play)   │  │ JS Client   │  │
│  │  └──────────────┘  └──────────────┘  └──────────────┘  │
│  └─────────────────────────────────────────────────────────┘
```

### Data Flow: Speech-to-Speech Conversation

```
User speaks into mic
        │
        ▼
[Browser: MediaRecorder API] ─── 24kHz mono PCM ───► [SignalR Hub]
                                                          │
                                                          ▼
                                                  ┌───────────────┐
                                                  │ PersonaPlex   │
                                                  │ Pipeline      │
                                                  ├───────────────┤
                                                  │ Mimi Encoder  │──► audio tokens
                                                  │      ▼        │
                                                  │ 7B LM *       │──► response tokens + text
                                                  │      ▼        │
                                                  │ Mimi Decoder  │──► output audio (24kHz WAV)
                                                  └───────┬───────┘
                                                          │
            [Browser: Web Audio API] ◄── audio bytes ─────┘
                      │
                      ▼
              User hears response

    * 7B LM currently unavailable — Ollama substitutes as reasoning engine
```

### Agent Framework Integration (LLM-Augmented Path)

```
PersonaPlex S2S response ──► ResponseText (transcribed)
                                    │
                                    ▼
                       ┌────────────────────────┐
                       │  Microsoft.Extensions  │
                       │  .AI (IChatClient)     │
                       │                        │
                       │  • Context injection   │
                       │  • Multi-turn memory   │
                       │  • Tool calling        │
                       └────────┬───────────────┘
                                │
                                ▼
                       ┌──────────────────┐
                       │  Ollama Local    │
                       │  (llama3.2:7b)   │
                       └────────┬─────────┘
                                │
                       Enriched Response Text
                                │
                                ▼
                PersonaPlex Decoder → Audio → User
```

---

## 2. Conversation Modes

### Mode A: Direct PersonaPlex (Full-Duplex Native)

```
User Speech → PersonaPlex S2S → AI Speech
```

- Leverages PersonaPlex's Moshi full-duplex capability
- Fastest latency: **~490–690ms** end-to-end
- **Blocked until:** 7B LM ONNX export completes

### Mode B: LLM-Augmented (Hybrid Pipeline)

```
User Speech → PersonaPlex ASR → M.E.AI → Ollama → PersonaPlex TTS → AI Speech
```

- Adds reasoning/context/tools via agent framework + Ollama
- Higher latency: **~1.7–4.3 seconds**
- **Available now:** Only needs Encoder/Decoder + Ollama

### Mode C: Ollama as Primary Reasoner (Workaround)

```
User Audio → PersonaPlex Encoder → audio tokens
           → Transcription (Whisper or fallback)
           → M.E.AI + Ollama → text response
           → PersonaPlex Decoder → output audio → User
```

- Best workaround while 7B LM export is incomplete
- Latency: **~1.8–4.3 seconds** (acceptable)
- Demonstrates full pipeline architecture without the LM backbone

**Decision:** Start with **Mode C** for MVP. Add Mode A when 7B LM export completes. Mode B is opt-in "deep thinking" for complex queries.

---

## 3. Technology Decisions

### 3.1 Blazor Hosting Model: **Blazor Server**

| Model | Verdict | Reason |
|-------|---------|--------|
| **Blazor Server** | ✅ **Recommended** | SignalR built-in, server-side ONNX, no model download to browser, low latency |
| Blazor WebAssembly | ❌ Rejected | ONNX Runtime needs native binaries; 350MB+ models can't ship to browser |
| Blazor Auto | ❌ Rejected | WASM component adds complexity with no benefit for this use case |

**Trade-off:** Requires persistent SignalR connection. Scales by concurrent users (not requests). For scale-out, consider Azure SignalR Service.

### 3.2 Communication Protocol: **SignalR with MessagePack**

| Protocol | Verdict | Reason |
|----------|---------|--------|
| **SignalR** | ✅ **Recommended** | Best Blazor integration, auto-reconnect, binary streaming via `IAsyncEnumerable<byte[]>` |
| gRPC-Web | ❌ Deferred | Requires proxy (Envoy/YARP) for browser; consider for Phase 4 if backpressure is critical |
| Raw WebSocket | ❌ Rejected | Manual reconnection, no compression, higher complexity |

### 3.3 Agent Framework: **Microsoft.Extensions.AI**

| Framework | Verdict | Reason |
|-----------|---------|--------|
| **M.E.AI** | ✅ **Recommended** | Lightweight (~100KB), .NET-native, direct Ollama integration, Aspire-ready |
| Semantic Kernel | 🟡 Phase 4 | Upgrade path if complex orchestration needed (planners, function calling, memory) |
| AutoGen .NET | ❌ Rejected | Overkill — single-agent conversation, not multi-agent coordination |

**Rationale:** PersonaPlex is the primary interface. The LLM is a backend reasoning engine, not a front-channel agent. M.E.AI's `IChatClient` is the right abstraction — lightweight, clean, future-proof.

### 3.4 Target Framework: **net9.0**

| TFM | Verdict | Reason |
|-----|---------|--------|
| **net9.0** | ✅ **Recommended** | Stable (GA Nov 2024), full Aspire 13.x support, easy upgrade to net10.0 later |
| net10.0 | ❌ Deferred | Preview — too early for production; Aspire compatibility uncertain |
| net8.0 | 🟡 Fallback | LTS but no advantage over net9.0 for new projects |

**Note:** The PersonaPlex library targets `net8.0;net10.0`. A net9.0 scenario project will resolve against the net8.0 TFM — fully compatible.

### 3.5 Local LLM: **Ollama with llama3.2:7b-q4**

| Option | Verdict | Reason |
|--------|---------|--------|
| **Ollama** | ✅ **Recommended** | Simpler setup, community models, Aspire integration via CommunityToolkit |
| Azure AI Foundry Local | 🟡 Alternative | Same OpenAI-compatible API; better for enterprise; more setup |

**Model pairing recommendation:**

| Model | Size | Use Case |
|-------|------|----------|
| **llama3.2:7b-q4** | ~4 GB | General chat, balanced performance/quality |
| **phi-4:7b-q4** | ~4 GB | Fastest response, good reasoning |
| **llama3.1:13b-q4** | ~8 GB | Complex reasoning (needs RTX 4090+) |

---

## 4. Latency Budget Analysis

Target: **< 2 seconds** end-to-end for acceptable conversational UX.

### Component Breakdown

| Component | Time (Mode C) | Notes |
|-----------|---------------|-------|
| Browser audio capture → server | ~50–100ms | SignalR binary, small chunks |
| PersonaPlex Mimi Encoder (GPU) | ~30–50ms | INT8, per 1-sec audio chunk |
| Transcription (Whisper/fallback) | ~200ms | If PersonaPlex text unavailable |
| M.E.AI → Ollama (llama3.2:7b-q4) | ~500–1500ms | 30–80ms/token × ~20 tokens |
| PersonaPlex Mimi Decoder (GPU) | ~30–50ms | INT8 |
| Server → browser playback | ~50–100ms | Web Audio API buffering |
| File I/O overhead (MVP) | ~500ms | Temp file write/read (eliminated in Phase 2) |
| **TOTAL (Mode C, MVP)** | **~1.4–2.5s** | ✅ Acceptable for MVP |
| **TOTAL (Mode C, streaming)** | **~0.9–1.9s** | ✅ Good (Phase 2) |
| **TOTAL (Mode A, when available)** | **~0.5–0.7s** | ✅ Excellent |

### Streaming Optimizations (Phase 2)

- **Progressive audio generation:** Start playing audio as first tokens decode (~500-1000ms improvement)
- **Chunked processing:** Process 100ms audio frames instead of full file
- **Eliminate temp files:** Stream-based API removes file I/O overhead

---

## 5. GPU/Memory Requirements

### VRAM Budget: PersonaPlex + Ollama

| Configuration | PersonaPlex | Ollama | Total | Feasible GPUs |
|---------------|-------------|--------|-------|---------------|
| **INT4 + Q4_K_M (7B)** | ~4.5 GB | ~5 GB | **~9.5 GB** | RTX 3060 12GB, RTX 4070 12GB ✅ |
| **INT8 + Q4_K_M (7B)** | ~8 GB | ~5 GB | **~13 GB** | RTX 4080 16GB ✅ |
| **FP16 + Q8 (7B)** | ~15 GB | ~8 GB | **~23 GB** | RTX 4090 24GB ✅ |
| **INT8 + Q4_K_M (13B)** | ~8 GB | ~9 GB | **~17 GB** | RTX 4090 24GB ✅ |

### GPU Coexistence

- **CUDA driver conflicts:** Unlikely — both OnnxRuntime and llama.cpp use the same CUDA runtime
- **VRAM partitioning:** Not directly controllable, but CUDA manages memory pools
- **Mitigation:** Use `OLLAMA_MAX_LOADED_MODELS=1` to prevent multiple Ollama models staying resident

### Hardware Recommendations

| GPU | VRAM | Config | Verdict |
|-----|------|--------|---------|
| **RTX 4080 16GB** | 16 GB | INT8 PersonaPlex + Q4 Ollama 7B | ✅ **Minimum viable** |
| **RTX 4090 24GB** | 24 GB | INT8/FP16 PersonaPlex + Q8 Ollama 13B | ✅✅ **Recommended for dev** |
| **A6000 48GB** | 48 GB | FP16 everything | Enterprise, no compromises |

### Split Execution Fallback

If single GPU is too constrained:
- **PersonaPlex on GPU, Ollama on CPU:** Ollama 5-10× slower but frees all VRAM for PersonaPlex
- **PersonaPlex Encoder/Decoder on CPU, Ollama on GPU:** Encoder/Decoder are lightweight (~200-500ms on CPU), prioritizes fast LLM reasoning

---

## 6. NuGet Package Matrix

### Core Packages

| Package | Version | Purpose |
|---------|---------|---------|
| **Aspire.Hosting.AppHost** | 13.1.2 | Aspire orchestrator SDK |
| **Aspire.ServiceDefaults** | 13.1.2 | Shared telemetry, health checks |
| **CommunityToolkit.Aspire.Hosting.Ollama** | 13.1.2-beta.518 | Ollama container integration |
| **Microsoft.Extensions.AI** | 1.2.0 | Unified AI abstractions (`IChatClient`) |
| **Microsoft.Extensions.AI.Ollama** | 1.2.0 | Ollama connector for `IChatClient` |
| **Microsoft.AspNetCore.SignalR.Client** | 9.0.2 | Client-side SignalR (Blazor) |
| **Microsoft.AspNetCore.SignalR.Protocols.MessagePack** | 9.0.2 | Binary serialization for audio |
| **Microsoft.ML.OnnxRuntime** | 1.24.2 | ONNX model inference (CPU) |
| **Microsoft.ML.OnnxRuntime.Gpu** | 1.24.2 | NVIDIA CUDA acceleration |
| **NAudio** | 2.2.1 | Audio format handling |
| **ElBruno.PersonaPlex** | 0.1.0-preview | Core PersonaPlex library |

### Upgrade Path Packages

| Package | Version | When |
|---------|---------|------|
| **Microsoft.SemanticKernel** | 1.37.0 | Phase 4: If complex orchestration needed |
| **Microsoft.SemanticKernel.Connectors.Ollama** | 1.37.0-alpha | Phase 4: SK + Ollama |

---

## 7. Proposed Project Structure

```
src/samples/scenario-04-blazor-aspire/
│
├── scenario-04.AppHost/                     # Aspire Orchestrator
│   ├── Program.cs                           # Resource definitions
│   └── scenario-04.AppHost.csproj
│       ├── Aspire.Hosting.AppHost (13.1.2)
│       └── CommunityToolkit.Aspire.Hosting.Ollama (13.1.2-beta.518)
│
├── scenario-04.ServiceDefaults/             # Shared Aspire Defaults
│   ├── Extensions.cs                        # OpenTelemetry, health checks
│   └── scenario-04.ServiceDefaults.csproj
│       └── Aspire.ServiceDefaults (13.1.2)
│
├── scenario-04.Api/                         # ASP.NET Core Backend
│   ├── Program.cs                           # Minimal API + SignalR + DI
│   ├── Hubs/
│   │   └── AudioStreamingHub.cs             # SignalR hub for audio streaming
│   ├── Services/
│   │   ├── PersonaPlexService.cs            # Wraps PersonaPlex pipeline
│   │   └── AudioProcessingQueue.cs          # Background queue for inference
│   ├── appsettings.json
│   └── scenario-04.Api.csproj
│       ├── ElBruno.PersonaPlex (0.1.0-preview)
│       ├── Microsoft.AspNetCore.SignalR.Protocols.MessagePack (9.0.2)
│       ├── Microsoft.Extensions.AI.Ollama (1.2.0)
│       └── ProjectRef → ServiceDefaults
│
├── scenario-04.Web/                         # Blazor Server Frontend
│   ├── Program.cs                           # Blazor Server setup
│   ├── Components/
│   │   ├── App.razor
│   │   ├── Pages/
│   │   │   ├── Index.razor                  # Landing page
│   │   │   └── Conversation.razor           # Main conversation UI
│   │   └── Layout/
│   │       └── MainLayout.razor
│   ├── wwwroot/
│   │   ├── js/
│   │   │   ├── audioCapture.js              # MediaRecorder → SignalR
│   │   │   └── audioPlayback.js             # Web Audio API playback
│   │   └── css/app.css
│   └── scenario-04.Web.csproj
│       ├── Microsoft.AspNetCore.SignalR.Client (9.0.2)
│       └── ProjectRef → Shared, ServiceDefaults
│
└── scenario-04.Shared/                      # Shared Contracts
    ├── Models/
    │   ├── AudioChunkDto.cs                 # { byte[] Data, int SequenceNumber }
    │   ├── ConversationStateDto.cs          # { VoicePreset, TextPrompt }
    │   └── ChatMessageDto.cs               # { string Role, string Content }
    └── scenario-04.Shared.csproj
        └── TargetFramework: net9.0
```

---

## 8. Key Code Patterns

### Aspire AppHost (Program.cs)

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Ollama container for text reasoning
var ollama = builder.AddOllama("ollama")
    .WithModel("llama3.2:7b")
    .WithDataVolume()      // Persist model cache across restarts
    .WithOpenWebUI();      // Optional testing UI

// API Backend (PersonaPlex + SignalR + M.E.AI)
var api = builder.AddProject<Projects.scenario_04_Api>("api")
    .WithReference(ollama)
    .WithEnvironment("PERSONAPLEX_MODEL_DIR", "./models")
    .WithExternalHttpEndpoints();

// Blazor Server Frontend
builder.AddProject<Projects.scenario_04_Web>("web")
    .WithReference(api);

builder.Build().Run();
```

### SignalR Audio Hub

```csharp
public class AudioStreamingHub : Hub
{
    private readonly PersonaPlexPipeline _pipeline;

    public async Task<byte[]> ProcessAudioAsync(byte[] inputWav, string voicePreset)
    {
        var tempInput = Path.GetTempFileName() + ".wav";
        var tempOutput = Path.GetTempFileName() + ".wav";
        try
        {
            await File.WriteAllBytesAsync(tempInput, inputWav);
            await _pipeline.ProcessAsync(
                tempInput,
                voicePreset: Enum.Parse<VoicePreset>(voicePreset),
                outputPath: tempOutput);
            return await File.ReadAllBytesAsync(tempOutput);
        }
        finally
        {
            File.Delete(tempInput);
            File.Delete(tempOutput);
        }
    }
}
```

### Blazor Audio Capture (JS Interop)

```javascript
// wwwroot/js/audioCapture.js
export async function startCapture(dotNetRef) {
    const stream = await navigator.mediaDevices.getUserMedia({
        audio: { sampleRate: 24000, channelCount: 1,
                 echoCancellation: true, noiseSuppression: true }
    });
    const audioContext = new AudioContext({ sampleRate: 24000 });
    const source = audioContext.createMediaStreamSource(stream);
    const processor = audioContext.createScriptProcessor(4096, 1, 1);

    processor.onaudioprocess = (e) => {
        const audioData = e.inputBuffer.getChannelData(0);
        dotNetRef.invokeMethodAsync('OnAudioData',
            Array.from(new Uint8Array(audioData.buffer)));
    };
    source.connect(processor);
    processor.connect(audioContext.destination);
}
```

### M.E.AI + Ollama Integration

```csharp
// API Backend DI registration
builder.Services.AddSingleton<IChatClient>(sp =>
{
    var endpoint = sp.GetRequiredService<IConfiguration>()["services:ollama:http:0"];
    return new OllamaChatClient(new Uri(endpoint!), "llama3.2:7b");
});

// Usage in conversation service
public class ConversationService(IChatClient chatClient)
{
    public async Task<string> AugmentResponseAsync(string transcript)
    {
        var response = await chatClient.CompleteAsync(
            $"The user said: '{transcript}'. Provide a thoughtful response.");
        return response.Text;
    }
}
```

---

## 9. Risk Matrix

| # | Risk | Severity | Likelihood | Impact | Mitigation |
|---|------|----------|------------|--------|------------|
| 1 | **7B LM ONNX export incomplete** | 🔴 Critical | High | Cannot run PersonaPlex full pipeline | Use Ollama as LM replacement (Mode C) |
| 2 | **File-based API (no streaming)** | 🟡 High | Confirmed | +500-1000ms latency, temp file overhead | Accept for MVP; refactor to `ProcessStreamAsync` in Phase 2 |
| 3 | **GPU VRAM exhaustion** | 🟡 High | Medium | Models crash or refuse to load | INT4 quantization + `OLLAMA_MAX_LOADED_MODELS=1` |
| 4 | **Latency > 2s in augmented mode** | 🟡 High | Medium | Poor conversational UX | Use smaller Ollama model, streaming, visual feedback |
| 5 | **Browser audio API complexity** | 🟠 Medium | Medium | Glitches, format mismatches | Use proven patterns (RecordRTC.js), fallback to file upload |
| 6 | **Full-duplex not feasible (Phase 1)** | 🟠 Medium | Confirmed | Turn-based only, not real-time overlap | Accept half-duplex for MVP; full-duplex requires streaming refactor |
| 7 | **PersonaPlex 0.1.0-preview instability** | 🟡 Medium | Medium | Breaking API changes | Pin version, monitor releases |
| 8 | **OnnxRuntime + Ollama GPU driver conflict** | 🟢 Low | Low | Performance degradation | Both use same CUDA runtime; no known conflicts |
| 9 | **SignalR connection drops** | 🟢 Low | Low | Session interrupted | Built-in auto-reconnect |

---

## 10. PersonaPlex Library Changes Needed

The current `PersonaPlexPipeline` API is file-path based. For Scenario-04 to reach production quality, the library needs enhancements:

### Phase 1 (MVP — no library changes)
Use the existing `ProcessAsync(inputAudioPath, ..., outputPath)` with temp files. Works but adds ~500ms latency.

### Phase 2 (Stream-based API)
```csharp
// New API surface needed in ElBruno.PersonaPlex
public async Task<ConversationResult> ProcessAsync(
    Stream inputAudio,              // ✅ Stream instead of file path
    VoicePreset? voicePreset,
    string? textPrompt,
    Stream outputAudio,             // ✅ Stream instead of file path
    CancellationToken ct)

public async Task<ConversationResult> ProcessAsync(
    byte[] inputAudioBytes,         // ✅ byte[] convenience overload
    VoicePreset? voicePreset,
    string? textPrompt,
    CancellationToken ct)           // Returns byte[] in ConversationResult
```

### Phase 3 (Chunked streaming)
```csharp
public async IAsyncEnumerable<AudioChunk> ProcessChunksAsync(
    IAsyncEnumerable<byte[]> inputChunks,
    VoicePreset? voicePreset,
    string? textPrompt,
    [EnumeratorCancellation] CancellationToken ct)
```

**Estimated effort:** 2-3 days for Phase 2 (Stream-based). 1-2 weeks for Phase 3 (chunked).

---

## 11. Aspire Dashboard Telemetry

The Aspire dashboard (http://localhost:15888) provides built-in OpenTelemetry visualization.

### Custom Metrics to Expose

```csharp
var meter = new Meter("PersonaPlex.Inference");
var inferenceCounter = meter.CreateCounter<long>("personaplex.inferences");
var inferenceTime = meter.CreateHistogram<double>("personaplex.inference_time_ms");
var ollamaLatency = meter.CreateHistogram<double>("ollama.chat_completion_ms");
```

### Trace Visualization

```
HTTP POST /audiohub/ProcessAudio (2100ms)
  ├─ PersonaPlex.Encoder.Encode (40ms)
  ├─ Ollama.ChatCompletion (1200ms)        ← visible in dashboard
  ├─ PersonaPlex.Decoder.Decode (40ms)
  └─ SignalR.Response (20ms)
```

---

## 12. Implementation Phasing

### Phase 1: Architecture Validation (1–2 weeks)

**Goal:** Validate end-to-end flow without PersonaPlex LM backbone.

- [ ] Create `scenario-04-blazor-aspire/` project structure
- [ ] Set up Aspire AppHost with Ollama container
- [ ] Build Blazor Server conversation UI
- [ ] Implement SignalR AudioStreamingHub (file-based MVP)
- [ ] Integrate M.E.AI + Ollama for text chat
- [ ] Browser audio capture/playback via JS interop
- [ ] Use Mode C: Encoder → transcription → Ollama → Decoder

**Output:** Working prototype with text-only LLM + PersonaPlex audio I/O.

### Phase 2: PersonaPlex Integration (1–2 weeks)

**Prerequisite:** 7B LM ONNX export completed.

- [ ] Refactor PersonaPlex to Stream/byte[] API
- [ ] Integrate full PersonaPlex pipeline (Encoder → LM → Decoder)
- [ ] Performance tuning (GPU, quantization)
- [ ] Latency profiling via Aspire dashboard

**Output:** Real speech-to-speech with PersonaPlex native LM.

### Phase 3: Streaming & UX Polish (2–3 weeks)

- [ ] Chunked audio processing (100ms frames)
- [ ] Progressive audio playback
- [ ] Conversation history with M.E.AI
- [ ] Voice selection UI (switch presets mid-conversation)
- [ ] Error handling and reconnection UX

### Phase 4: Advanced Features (4–6 weeks)

- [ ] Full-duplex (simultaneous listen + speak)
- [ ] Upgrade to Semantic Kernel (if orchestration complexity grows)
- [ ] Function calling / tool use via agent framework
- [ ] Multi-user scaling
- [ ] Azure deployment via Aspire

---

## 13. Go/No-Go Assessment

### ✅ GO Conditions (all met)

| Condition | Status |
|-----------|--------|
| Aspire orchestration feasible | ✅ Aspire 13.x has Ollama support |
| Blazor Server + SignalR proven for real-time | ✅ Binary streaming, auto-reconnect |
| M.E.AI + Ollama integration exists | ✅ Packages available, API clean |
| PersonaPlex Encoder/Decoder exported | ✅ 178MB + 170MB ONNX ready |
| Workaround exists for missing 7B LM | ✅ Ollama as LM replacement |

### ⚠️ CONDITIONS (must be monitored)

| Condition | Risk if unmet |
|-----------|---------------|
| 7B LM ONNX export must complete for Phase 2 | Stuck on Mode C workaround |
| GPU ≥16GB VRAM required for both models | Must use CPU fallback or split execution |
| PersonaPlex library needs Stream API for Phase 2 | Extra library work (2-3 days) |

### 🚫 NO-GO Triggers

**Abandon Scenario-04 if:**
- 7B LM export is blocked indefinitely (>8 weeks) AND Encoder/Decoder alone don't provide useful audio processing
- No CUDA-capable GPU is available (CPU-only makes latency unacceptable)
- Latency consistently exceeds 5 seconds with no optimization path

### Final Verdict

**⚠️ CONDITIONAL GO — proceed with Phase 1 immediately.** The architecture is sound, all components are individually proven, and a clear workaround exists for the incomplete 7B LM export. The phased approach de-risks the critical blockers while delivering incremental value at each stage.

---

## 14. Open Questions for Bruno

1. **PersonaPlex 7B ONNX ETA?** A hard date helps plan Phase 2 timing.
2. **GPU setup?** Single RTX 4090, dual GPU rig, or cloud GPU?
3. **Semantic Kernel later?** Stick with M.E.AI or plan for SK from the start?
4. **Deployment target?** Local dev only, or Azure Container Apps eventually?
5. **Voice selection UI?** Allow users to switch voices mid-conversation?

---

## References

- [NVIDIA PersonaPlex Paper](https://arxiv.org/abs/2602.06053)
- [Moshi Architecture (Full-Duplex Speech)](https://arxiv.org/abs/2410.00037)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [SignalR Binary Streaming](https://learn.microsoft.com/aspnet/core/signalr/streaming)
- [Microsoft.Extensions.AI](https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/)
- [Ollama](https://ollama.ai/)
- [CommunityToolkit.Aspire.Hosting.Ollama](https://github.com/CommunityToolkit/Aspire)
- [PersonaPlex HuggingFace](https://huggingface.co/nvidia/personaplex-7b-v1)
