namespace ElBruno.PersonaPlex;

/// <summary>
/// Represents the result of a speech-to-speech inference.
/// </summary>
public class ConversationResult
{
    /// <summary>
    /// Path to the generated output audio file.
    /// </summary>
    public required string OutputAudioPath { get; init; }

    /// <summary>
    /// Transcribed text from the model's response (if available).
    /// </summary>
    public string? ResponseText { get; init; }

    /// <summary>
    /// Duration of the output audio in milliseconds.
    /// </summary>
    public double DurationMs { get; init; }

    /// <summary>
    /// Total inference time in milliseconds.
    /// </summary>
    public double InferenceTimeMs { get; init; }

    /// <summary>
    /// The voice preset used for generation.
    /// </summary>
    public VoicePreset VoicePreset { get; init; }

    /// <summary>
    /// The text prompt (persona) used for generation.
    /// </summary>
    public string? TextPrompt { get; init; }

    /// <summary>
    /// The seed used for generation (if set).
    /// </summary>
    public int? Seed { get; init; }
}
