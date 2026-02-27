using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;
using Scenario04.Api.Services;
using Scenario04.Shared.Models;

namespace Scenario04.Api.Hubs;

/// <summary>
/// SignalR hub for real-time conversation between the Blazor frontend and the
/// Ollama-powered backend via Microsoft Agent Framework.
///
/// Architecture:
///   Browser ──SignalR──► ConversationHub ──M.E.AI──► Ollama (phi4-mini)
///
/// Uses Microsoft.Extensions.AI.Ollama (OllamaChatClient) as the IChatClient,
/// following the Agent Framework pattern from:
/// https://learn.microsoft.com/agent-framework/agents/providers/ollama
///
/// For multi-turn streaming: ConversationService manages chat history per session.
/// For one-shot agent queries: Uses IChatClient.AsAIAgent() directly.
///
/// The hub uses MessagePack protocol for efficient binary transfer (audio chunks).
/// </summary>
public sealed class ConversationHub : Hub
{
    private readonly ConversationService _conversation;
    private readonly IChatClient _chatClient;
    private readonly ILogger<ConversationHub> _logger;

    public ConversationHub(ConversationService conversation, IChatClient chatClient, ILogger<ConversationHub> logger)
    {
        _conversation = conversation;
        _chatClient = chatClient;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────
    // Text conversation (streaming response)
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Send a text message and receive a streaming response from Ollama.
    /// The response is streamed token-by-token to the client.
    /// </summary>
    public async IAsyncEnumerable<string> SendMessage(string sessionId, string message, string? personaPrompt = null)
    {
        _logger.LogInformation("Hub: SendMessage from {ConnectionId}, session={Session}", Context.ConnectionId, sessionId);

        await foreach (var token in _conversation.ChatStreamAsync(sessionId, message, personaPrompt))
        {
            yield return token;
        }
    }

    /// <summary>
    /// Send a text message and receive a complete (non-streaming) response.
    /// </summary>
    public async Task<string> SendMessageComplete(string sessionId, string message, string? personaPrompt = null)
    {
        _logger.LogInformation("Hub: SendMessageComplete from {ConnectionId}, session={Session}", Context.ConnectionId, sessionId);
        return await _conversation.ChatAsync(sessionId, message, personaPrompt);
    }

    // ──────────────────────────────────────────────────────────
    // Audio conversation (Mode C: Encoder → Ollama → Decoder)
    // Placeholder — will be connected when PersonaPlex ONNX
    // models are integrated into the pipeline.
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Process an audio chunk through the conversation pipeline.
    /// Currently returns a text response; future: returns audio bytes.
    /// </summary>
    public async Task<ChatMessageDto> ProcessAudio(string sessionId, byte[] audioData, string? personaPrompt = null)
    {
        _logger.LogInformation("Hub: ProcessAudio from {ConnectionId}, {Bytes} bytes", Context.ConnectionId, audioData.Length);

        // TODO: When PersonaPlex ONNX models are ready:
        // 1. Mimi Encoder: audioData → audio tokens
        // 2. Transcribe tokens to text (or use separate Whisper model)
        // 3. Send text to Ollama → get response
        // 4. Mimi Decoder: response text → audio tokens → output audio
        //
        // For now, we acknowledge the audio and return a placeholder.

        var response = await _conversation.ChatAsync(
            sessionId,
            "[Audio message received — audio processing will be available when PersonaPlex ONNX models are integrated]",
            personaPrompt);

        return new ChatMessageDto
        {
            Role = "assistant",
            Content = response,
            HasAudio = false
        };
    }

    // ──────────────────────────────────────────────────────────
    // One-shot Agent query (Microsoft Agent Framework pattern)
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Execute a one-shot agent query using the Microsoft Agent Framework pattern.
    /// Creates an AIAgent from the OllamaChatClient with custom instructions.
    ///
    /// This follows the pattern from:
    /// https://learn.microsoft.com/agent-framework/agents/providers/ollama
    ///
    /// <code>
    /// var agent = chatClient.AsAIAgent(instructions: "You are a helpful assistant.");
    /// var result = await agent.RunAsync("What is the largest city in France?");
    /// </code>
    /// </summary>
    public async Task<string> AgentQuery(string question, string? instructions = null)
    {
        _logger.LogInformation("Hub: AgentQuery from {ConnectionId}", Context.ConnectionId);

        var agent = _chatClient.AsAIAgent(
            instructions: instructions ?? "You are a helpful, concise assistant. Keep responses to 1-3 sentences.");

        var result = await agent.RunAsync(question);
        return result.Text ?? string.Empty;
    }

    // ──────────────────────────────────────────────────────────
    // Session management
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Clear the conversation history for a session.
    /// </summary>
    public void ClearSession(string sessionId)
    {
        _conversation.ClearSession(sessionId);
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
