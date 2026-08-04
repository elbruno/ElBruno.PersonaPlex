namespace ElBruno.PersonaPlex.Sessions;

/// <summary>
/// Represents one turn in the UI-facing conversation history.
/// The built-in adapter produces one assistant response per submitted user clip.
/// </summary>
public sealed record ConversationTurn
{
    /// <summary>
    /// Stable turn identifier.
    /// </summary>
    public required string TurnId { get; init; }

    /// <summary>
    /// Monotonic turn number within the session.
    /// </summary>
    public int SequenceNumber { get; init; }

    /// <summary>
    /// Current turn status.
    /// </summary>
    public ConversationTurnStatus Status { get; init; } = ConversationTurnStatus.Completed;

    /// <summary>
    /// Turn start time in UTC.
    /// </summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Turn completion time in UTC.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// User-supplied audio for this turn.
    /// </summary>
    public required PersonaAudioReference InputAudio { get; init; }

    /// <summary>
    /// Assistant audio produced for this turn, when available.
    /// </summary>
    public PersonaAudioReference? OutputAudio { get; init; }

    /// <summary>
    /// Optional transcript associated with the user audio.
    /// </summary>
    public string? UserTranscript { get; init; }

    /// <summary>
    /// Optional transcript associated with the assistant audio.
    /// </summary>
    public string? AssistantTranscript { get; init; }

    /// <summary>
    /// Total inference time in milliseconds.
    /// </summary>
    public double? InferenceTimeMs { get; init; }

    /// <summary>
    /// Voice preset used for the assistant response.
    /// </summary>
    public VoicePreset VoicePreset { get; init; }

    /// <summary>
    /// Prompt/persona used for the turn.
    /// </summary>
    public string? TextPrompt { get; init; }

    /// <summary>
    /// Seed used by the current generation.
    /// </summary>
    public int? Seed { get; init; }

    /// <summary>
    /// Optional error message when turn processing fails.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
