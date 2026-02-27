using Microsoft.Extensions.AI;
using Scenario04.Api.Hubs;
using Scenario04.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────────────────────
// Aspire service defaults (OpenTelemetry, health checks, etc.)
// ──────────────────────────────────────────────────────────────
builder.AddServiceDefaults();

// ──────────────────────────────────────────────────────────────
// Ollama via Microsoft.Extensions.AI
// ──────────────────────────────────────────────────────────────
// Aspire injects the connection string as "ConnectionStrings__ollama"
// which resolves to something like "http://localhost:11434".
// We use the OpenAI-compatible endpoint that Ollama exposes.
// ──────────────────────────────────────────────────────────────
var ollamaEndpoint = builder.Configuration.GetConnectionString("ollama")
    ?? builder.Configuration["Ollama:Endpoint"]
    ?? "http://localhost:11434";

var ollamaModel = builder.Configuration["Ollama:Model"] ?? "phi4-mini";

builder.Services.AddChatClient(services =>
    new OpenAI.OpenAIClient(
        new System.ClientModel.ApiKeyCredential("ollama"),
        new OpenAI.OpenAIClientOptions { Endpoint = new Uri($"{ollamaEndpoint}/v1") })
    .GetChatClient(ollamaModel)
    .AsIChatClient())
    .UseFunctionInvocation()
    .UseOpenTelemetry()
    .UseLogging();

// ──────────────────────────────────────────────────────────────
// Application services
// ──────────────────────────────────────────────────────────────
builder.Services.AddSingleton<ConversationService>();

// ──────────────────────────────────────────────────────────────
// SignalR with MessagePack for binary audio streaming
// ──────────────────────────────────────────────────────────────
builder.Services.AddSignalR()
    .AddMessagePackProtocol();

// CORS — allow the Blazor frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.MapDefaultEndpoints();
app.MapHub<ConversationHub>("/hubs/conversation");

// Simple health/info endpoint
app.MapGet("/", () => new
{
    Service = "PersonaPlex Conversation API",
    OllamaEndpoint = ollamaEndpoint,
    OllamaModel = ollamaModel,
    Status = "Running"
});

app.Run();
