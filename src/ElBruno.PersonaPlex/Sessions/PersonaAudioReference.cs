namespace ElBruno.PersonaPlex.Sessions;

/// <summary>
/// References audio exchanged by a persona session.
/// The in-box adapter uses local file paths because the current core pipeline is file-based.
/// </summary>
public sealed record PersonaAudioReference
{
    /// <summary>
    /// Whether this audio came from the user or the assistant.
    /// </summary>
    public PersonaAudioKind Kind { get; init; }

    /// <summary>
    /// Audio format metadata.
    /// </summary>
    public PersonaAudioFormat Format { get; init; } = PersonaAudioFormat.Wav24KhzMono16Bit;

    /// <summary>
    /// Session-scoped file path used by the current server-side adapter.
    /// Relative values are resolved under the adapter's storage root; absolute values must already be under that root.
    /// Future adapters can project audio from other storage without changing UI contracts.
    /// </summary>
    public string? LocalFilePath { get; init; }

    /// <summary>
    /// Optional media type for browser-facing consumers.
    /// </summary>
    public string? MediaType { get; init; }

    /// <summary>
    /// Optional clip duration in milliseconds.
    /// </summary>
    public double? DurationMs { get; init; }
}
