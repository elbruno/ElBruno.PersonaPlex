namespace ElBruno.PersonaPlex.Sessions;

/// <summary>
/// Turn-based session contract for server-backed Blazor or ASP.NET UI shells.
/// </summary>
public interface IPersonaSession : IAsyncDisposable
{
    /// <summary>
    /// Session identifier stable for the lifetime of the session instance.
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Current lifecycle state.
    /// </summary>
    PersonaSessionState State { get; }

    /// <summary>
    /// Detailed status snapshot for UI display.
    /// </summary>
    PersonaSessionStatus Status { get; }

    /// <summary>
    /// Conversation history accumulated by this session.
    /// </summary>
    IReadOnlyList<ConversationTurn> Turns { get; }

    /// <summary>
    /// Raised whenever the session state or turn history changes.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// Processes one user turn and returns the completed assistant response.
    /// </summary>
    Task<ConversationTurn> ProcessTurnAsync(PersonaTurnRequest request, CancellationToken cancellationToken = default);
}
