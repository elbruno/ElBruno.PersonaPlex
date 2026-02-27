using Microsoft.Extensions.AI;

namespace Scenario04.Api.Services;

/// <summary>
/// Manages multi-turn conversation with Ollama via Microsoft.Extensions.AI.
/// Each session maintains its own chat history for context continuity.
/// </summary>
public sealed class ConversationService
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<ConversationService> _logger;

    // Per-session chat history (keyed by session id)
    private readonly Dictionary<string, List<ChatMessage>> _sessions = new();
    private readonly Lock _lock = new();

    private const string SystemPrompt =
        """
        You are a friendly, helpful AI assistant in a real-time voice conversation.
        Keep your responses concise and conversational — aim for 1-3 sentences.
        You are speaking out loud, so avoid markdown, code blocks, or bullet points.
        Be warm and natural, like talking to a friend.
        """;

    public ConversationService(IChatClient chatClient, ILogger<ConversationService> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    /// <summary>
    /// Send a user message and get a streamed response from Ollama.
    /// </summary>
    public async IAsyncEnumerable<string> ChatStreamAsync(
        string sessionId,
        string userMessage,
        string? personaPrompt = null)
    {
        var history = GetOrCreateSession(sessionId, personaPrompt);

        history.Add(new ChatMessage(ChatRole.User, userMessage));
        _logger.LogInformation("[{Session}] User: {Message}", sessionId, userMessage);

        var fullResponse = string.Empty;

        await foreach (var update in _chatClient.GetStreamingResponseAsync(history))
        {
            if (update.Text is { Length: > 0 } text)
            {
                fullResponse += text;
                yield return text;
            }
        }

        // Add assistant response to history for multi-turn context
        history.Add(new ChatMessage(ChatRole.Assistant, fullResponse));
        _logger.LogInformation("[{Session}] Assistant: {Response}", sessionId, fullResponse);
    }

    /// <summary>
    /// Send a user message and get the complete response (non-streaming).
    /// </summary>
    public async Task<string> ChatAsync(string sessionId, string userMessage, string? personaPrompt = null)
    {
        var history = GetOrCreateSession(sessionId, personaPrompt);
        history.Add(new ChatMessage(ChatRole.User, userMessage));

        _logger.LogInformation("[{Session}] User: {Message}", sessionId, userMessage);

        var response = await _chatClient.GetResponseAsync(history);
        var text = response.Text ?? string.Empty;

        history.Add(new ChatMessage(ChatRole.Assistant, text));
        _logger.LogInformation("[{Session}] Assistant: {Response}", sessionId, text);

        return text;
    }

    /// <summary>
    /// Clear a session's conversation history.
    /// </summary>
    public void ClearSession(string sessionId)
    {
        lock (_lock)
        {
            _sessions.Remove(sessionId);
        }
        _logger.LogInformation("[{Session}] Session cleared", sessionId);
    }

    private List<ChatMessage> GetOrCreateSession(string sessionId, string? personaPrompt)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var history))
            {
                var system = personaPrompt is not null
                    ? $"{SystemPrompt}\n\nAdditional persona instructions: {personaPrompt}"
                    : SystemPrompt;

                history = [new ChatMessage(ChatRole.System, system)];
                _sessions[sessionId] = history;
            }

            return history;
        }
    }
}
