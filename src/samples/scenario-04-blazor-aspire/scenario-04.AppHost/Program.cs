// ──────────────────────────────────────────────────────────────────────────
// PersonaPlex Scenario 04 — Aspire AppHost
// ──────────────────────────────────────────────────────────────────────────
// Orchestrates:
//   1. Ollama container    – local LLM for reasoning (phi4-mini by default)
//   2. API Backend         – ASP.NET Core + SignalR + M.E.AI → Ollama
//   3. Blazor Web Frontend – conversation UI
//
// HOW TO RUN:
//   1. Install Docker Desktop (for Ollama container)
//   2. dotnet run --project scenario-04.AppHost
//   3. Open the Aspire dashboard (printed in console, usually http://localhost:15888)
//   4. Click the Blazor Web endpoint to open the conversation UI
//
// The Aspire dashboard gives you:
//   • Service discovery (API ↔ Ollama connection string auto-injected)
//   • OpenTelemetry traces (see latency for each Ollama call)
//   • Health checks for all services
//   • Logs from all containers in one place
// ──────────────────────────────────────────────────────────────────────────

var builder = DistributedApplication.CreateBuilder(args);

// ──────────────────────────────────────────────────────────────
// 1. Ollama — local LLM container
// ──────────────────────────────────────────────────────────────
// This pulls and runs the Ollama Docker container automatically.
// The model is downloaded on first run and cached in a Docker volume.
//
// To use a different model, change the string below.
// Popular options:
//   "phi4-mini"       — 3.8B, fast, good reasoning (~2.5 GB)
//   "llama3.2"        — 3B, balanced                (~2 GB)
//   "llama3.1:8b"     — 8B, higher quality          (~4.7 GB)
//   "phi4"            — 14B, best reasoning         (~9 GB)
//
// NOTE: The model will be downloaded into the Docker volume on first run.
//       This can take a few minutes depending on your connection.
// ──────────────────────────────────────────────────────────────
var ollama = builder.AddOllama("ollama")
    .WithDataVolume("personaplex-ollama-data")
    .AddModel("phi4-mini");

// ──────────────────────────────────────────────────────────────
// 2. API Backend — PersonaPlex + SignalR + Ollama integration
// ──────────────────────────────────────────────────────────────
// The connection string "ollama" is automatically injected by Aspire,
// so the API backend discovers Ollama without hardcoded URLs.
// ──────────────────────────────────────────────────────────────
var api = builder.AddProject<Projects.scenario_04_Api>("api")
    .WithReference(ollama)
    .WaitFor(ollama)
    .WithExternalHttpEndpoints();

// ──────────────────────────────────────────────────────────────
// 3. Blazor Web Frontend — conversation UI
// ──────────────────────────────────────────────────────────────
// References the API backend for SignalR connection.
// Aspire injects the API endpoint as a connection string.
// ──────────────────────────────────────────────────────────────
builder.AddProject<Projects.scenario_04_Web>("web")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
