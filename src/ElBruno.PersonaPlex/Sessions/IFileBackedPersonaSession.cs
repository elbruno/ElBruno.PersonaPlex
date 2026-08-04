namespace ElBruno.PersonaPlex.Sessions;

/// <summary>
/// Exposes the session-owned storage root used by file-based session implementations.
/// </summary>
public interface IFileBackedPersonaSession : IPersonaSession
{
    /// <summary>
    /// Gets the session-owned storage root for audio exchanged during turn processing.
    /// </summary>
    string StorageRoot { get; }
}
