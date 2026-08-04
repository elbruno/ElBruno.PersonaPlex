namespace ElBruno.PersonaPlex.Sessions;

/// <summary>
/// Snapshot of the current session status for UI components such as status cards.
/// </summary>
public sealed record PersonaSessionStatus
{
    /// <summary>
    /// Current lifecycle state.
    /// </summary>
    public PersonaSessionState State { get; init; } = PersonaSessionState.Created;

    /// <summary>
    /// True when the backing runtime has the required models loaded.
    /// </summary>
    public bool IsModelReady { get; init; }

    /// <summary>
    /// Session creation time in UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Last time the session status changed in UTC.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional human-readable detail for UI display.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>
    /// Optional error message when the session is faulted.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// True while the session is actively processing a turn.
    /// </summary>
    public bool IsBusy => State is PersonaSessionState.Initializing or PersonaSessionState.ProcessingTurn;
}
