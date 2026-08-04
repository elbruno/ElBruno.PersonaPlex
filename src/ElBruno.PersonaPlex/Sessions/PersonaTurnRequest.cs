namespace ElBruno.PersonaPlex.Sessions;

/// <summary>
/// Server-side request contract for a single persona turn.
/// </summary>
public sealed record PersonaTurnRequest
{
    /// <summary>
    /// Audio submitted by the caller.
    /// </summary>
    public required PersonaAudioReference InputAudio { get; init; }

    /// <summary>
    /// Output file path used by the current file-based adapter.
    /// Relative values are resolved under the adapter's storage root.
    /// </summary>
    public string OutputAudioPath { get; init; } = "output.wav";

    /// <summary>
    /// Optional transcript captured by the caller before invoking the session.
    /// </summary>
    public string? UserTranscript { get; init; }

    /// <summary>
    /// Optional voice preset override for this turn.
    /// </summary>
    public VoicePreset? VoicePreset { get; init; }

    /// <summary>
    /// Optional persona override for this turn.
    /// </summary>
    public string? TextPrompt { get; init; }

    /// <summary>
    /// Optional deterministic seed override for this turn.
    /// </summary>
    public int? Seed { get; init; }
}
