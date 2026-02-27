# Decisions

> Shared decision log. All agents read this before starting work. Scribe maintains it.

### 2026-02-27: Review gates established
**By:** Squad (Coordinator)
**What:** All work must pass through Niobe (security review) before being considered done. Work touching ONNX export, inference code, or model optimization must additionally pass through Seraph (model correctness review). Reviewer rejection triggers the Reviewer Rejection Protocol — a different agent must revise, not the original author.
**Why:** Security and model correctness are critical for a library that downloads and loads ML models. Two-reviewer gate ensures no code ships without both security and correctness verification.

---

## Scenario-04: Blazor + Aspire + PersonaPlex Architecture Decisions

### 2026-02-27: Scenario-04 Architecture Evaluation — CONDITIONAL GO
**By:** Neo (Architect), Trinity (C# Dev), Morpheus (ML/Python Dev)  
**Date:** 2026-02-27  
**Status:** 🟡 **CONDITIONAL GO** with phased rollout

### Decision Summary

Scenario-04 aims to build a **real-time conversational AI web application** combining:
- **Frontend:** Blazor Server with audio streaming
- **Backend:** ASP.NET Core API with PersonaPlex ONNX models  
- **Orchestration:** .NET Aspire (service discovery, telemetry)
- **AI Framework:** Microsoft.Extensions.AI + Ollama local LLM
- **Target:** .NET 9.0 LTS

**Verdict:** Feasible with significant technical constraints. Proceed with phased approach (Phase 1: text-only MVP, Phase 2: PersonaPlex integration, Phase 3: advanced features).

### Key Architectural Decisions (Merged)

#### Decision 1: Communication Protocol
**Choice:** SignalR (Phase 1)  
**Rationale:** Best Blazor/.NET integration, binary streaming support, Aspire service discovery, built-in reconnection  
**Trade-off:** Less efficient than gRPC; can upgrade in Phase 2  
**Code Pattern:** `SendAsync(byte[])` for audio chunks; MessagePack codec for efficiency  
**Responsibility:** Trinity (validated with Aspire integration patterns)

#### Decision 2: Blazor Hosting Model
**Choice:** Blazor Server (or Blazor Web App server-side mode)  
**Rationale:** Full ONNX Runtime access (GPU), PersonaPlex model files stay server-side (no 350MB+ downloads), low SignalR latency  
**Trade-off:** Server scales by # concurrent users (consider Azure SignalR Service for scale-out)  
**Responsibility:** Trinity (validated with PersonaPlex ONNX constraints)

#### Decision 3: Agent Framework
**Choice:** Microsoft.Extensions.AI (M.E.AI) over Semantic Kernel  
**Rationale:** Native .NET 9+ support, lighter-weight, built-in chat completion + tool calling, simpler API  
**Trade-off:** Less mature plugin ecosystem than Semantic Kernel; AutoGen is Python-first (not suitable)  
**Responsibility:** Morpheus (ML framework comparison)

#### Decision 4: Conversation Modes
**Choice:** Two-mode architecture:
- **Mode A (Primary):** Direct PersonaPlex S2S (~490-690ms latency) ✅ conversational feel
- **Mode B (Secondary):** LLM-augmented (PersonaPlex ASR → Agent Framework → Ollama → PersonaPlex TTS, ~1-2.4s latency) ⚠️ requires "thinking..." UI feedback

**Rationale:** Mode A validates pipeline; Mode B adds reasoning complexity incrementally  
**Recommendation:** Start MVP with Mode A; add Mode B as opt-in "deep thinking" feature  
**Responsibility:** Neo (architecture patterns), Morpheus (latency analysis)

#### Decision 5: Duplex Strategy
**Choice:** Half-duplex (turn-based) for Phase 1  
**Rationale:** Current PersonaPlex API is file-based (no streaming tokens); full-duplex requires architecture refactor  
**Trade-off:** Not true real-time; acceptable for Phase 1 MVP  
**Deferred:** Full-duplex (Phase 2+) requires PersonaPlexPipeline refactor to expose streaming tokens  
**Responsibility:** Trinity (identified API constraint), Morpheus (latency impact)

#### Decision 6: Target Framework
**Choice:** .NET 9.0 LTS  
**Rationale:** Long-term support, native minimal APIs, built-in SignalR + Aspire integration  
**Versions:** `Aspire.Hosting.AppHost` 9.2.0+, `Microsoft.AspNetCore.SignalR` (built-in), `Microsoft.Extensions.AI` 9.0.0+  
**Responsibility:** Trinity (validated .NET ecosystem compatibility)

#### Decision 7: LLM Backend (Phase 1)
**Choice:** Ollama llama3.3-8B  
**Rationale:** ~200-400ms inference latency (fits Mode B budget), local (no cloud dependency), upgrade path to 70B  
**Alternative:** llama3.3-70B for Phase 3 (requires dual-GPU or extended latency budget)  
**Responsibility:** Morpheus (VRAM budget, model performance analysis)

#### Decision 8: GPU Hardware
**Choice:** Dual-GPU setup recommended; single GPU acceptable with queueing  
**Configuration:**
- **Dual GPU (ideal):** RTX 4090 × 2 (GPU 0 = PersonaPlex, GPU 1 = Ollama)
- **Single GPU (acceptable):** RTX 4090 24GB with time-slicing (adds ~100-200ms latency)
- **Minimum:** 16GB VRAM for INT4 quantization of both models

**VRAM Budget:**
- PersonaPlex (Mimi encoder/decoder + 7B LM): ~12-14GB
- Ollama llama3.3-8B (Q4_K_M): ~5GB  
- Total: ~17-19GB (fits single RTX 4090 with time-slicing)

**Responsibility:** Morpheus (GPU coexistence analysis)

### Critical Blockers & Mitigation

| Blocker | Status | Impact | Mitigation | Timeline |
|---------|--------|--------|-----------|----------|
| **PersonaPlex 7B LM ONNX export incomplete** | In Progress | Cannot run full S2S pipeline | Phase 1: Text-only MVP (Azure TTS); Phase 2: Integrate when ready | Blocks Phase 2 (ETA unknown) |
| **File-based API (no streaming)** | Requires refactor | Higher latency, memory overhead, prevents streaming | Refactor PersonaPlexPipeline to memory-based API (byte[] in/out) | 2-3 days before Phase 2 |
| **GPU contention (ONNX + Ollama)** | Manageable | Single GPU time-slicing adds 100-200ms | Dual-GPU or inference queue service | Phase 1 planning |

### Risk Assessment

| Risk | Severity | Likelihood | Mitigation |
|------|----------|------------|-----------|
| 7B ONNX export blocked indefinitely | 🔴 Critical | High | Phase 1 MVP with text-only; escalate if >4 weeks delay |
| File-based API bottleneck in Phase 2 | 🟡 High | High | Schedule refactor before Phase 2 kickoff |
| GPU contention latency penalty | 🟡 High | High | Dual-GPU recommended; queueing as fallback |
| Mode B latency exceeds 1s (poor UX) | 🟡 High | Medium | Use 8B LLM, streaming responses, visual feedback ("thinking...") |
| Browser audio API complexity | 🟠 Medium | Medium | RecordRTC.js for proven library; format normalization |
| Full-duplex not feasible in Phase 1 | 🟠 Medium | High | Accept half-duplex for MVP; full-duplex Phase 2+ |

### Phased Rollout Plan

**Phase 1 (2 weeks): Architecture Validation**
- Aspire AppHost + ServiceDefaults setup
- Blazor Server conversation UI with MediaRecorder + Web Audio APIs
- SignalR ConversationHub with audio streaming
- M.E.AI + Ollama integration (text-only mode as placeholder)
- End-to-end flow validation without PersonaPlex
- Latency profiling via OpenTelemetry in Aspire dashboard

**Phase 2 (1-2 weeks): PersonaPlex Integration**
- PersonaPlexPipeline refactor (memory-based API)
- Full speech-to-speech pipeline (Mimi encoder → 7B LM → Mimi decoder)
- GPU allocation (dual-device or queue service)
- Latency optimization (streaming token analysis)
- Performance benchmarks

**Phase 3 (2+ weeks): Advanced Features**
- Streaming audio responses (early TTS start from LLM tokens)
- Full-duplex exploration (if streaming tokens available)
- RAG integration via M.E.AI plugins
- Multi-persona voice selection
- Conversation memory and context management

### No-Go Scenarios

**Do NOT proceed if:**
- 7B LM ONNX export blocked indefinitely (>4 weeks)
- GPU resources cannot be secured (no CUDA-capable GPU)
- Latency requirements demand <500ms (not achievable with Mode B)
- Project timeline or budget insufficient for 3-phase approach

### Deliverables & Ownership

- **Neo:** Comprehensive architecture evaluation (System diagrams, latency budgets, risk matrix, decision rationale)
- **Trinity:** .NET technology evaluation (Blazor hosting patterns, SignalR streaming, Aspire orchestration, NuGet versions)
- **Morpheus:** ML infrastructure evaluation (GPU/VRAM coexistence, agent framework comparison, latency breakdown)
- **Coordinator:** Synthesis document (`docs/scenario-04-blazor-aspire-evaluation.md`)

### Next Steps

1. ✅ **Circulate architecture evaluation** with Bruno Capuano for approval
2. 🔲 **Schedule PersonaPlex refactor** (2-3 days) before Phase 1 kickoff  
3. 🔲 **Create project structure** in `src/samples/scenario-04-aspire/` (Aspire AppHost, API, Web, Shared projects)
4. 🔲 **Confirm hardware setup** (single vs dual GPU; procurement if needed)
5. 🔲 **Phase 1 kickoff:** Blazor + Aspire + SignalR text-only MVP

---

### Responsibility Matrix

| Team | Role | Decision Authority |
|------|------|-------------------|
| **Neo** | Architect | System design, phasing, risk assessment, go/no-go recommendation |
| **Trinity** | C# Dev | .NET stack decisions, Aspire orchestration, SignalR patterns |
| **Morpheus** | ML/Python Dev | GPU/VRAM budget, agent framework selection, latency analysis |
| **Bruno** | Requester | Approval, timeline, hardware procurement |
| **Scribe** | Memory Manager | Decision logging, cross-agent updates, context propagation |
