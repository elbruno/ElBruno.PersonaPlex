namespace ElBruno.PersonaPlex.Sessions;

/// <summary>
/// Lifecycle state for a turn-based persona session used by server-backed UI shells.
/// </summary>
public enum PersonaSessionState
{
    Created,
    Initializing,
    Ready,
    ProcessingTurn,
    Faulted,
    Disposed
}
