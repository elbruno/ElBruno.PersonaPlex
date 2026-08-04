namespace ElBruno.PersonaPlex.Sessions;

/// <summary>
/// Describes audio metadata that UI layers can use without depending on the ONNX pipeline.
/// </summary>
public sealed record PersonaAudioFormat(
    int SampleRate,
    int Channels,
    int BitsPerSample,
    string Container,
    string Encoding)
{
    /// <summary>
    /// Default format used by the current file-based PersonaPlex pipeline.
    /// </summary>
    public static PersonaAudioFormat Wav24KhzMono16Bit { get; } =
        new(24000, 1, 16, "wav", "pcm_s16le");
}
