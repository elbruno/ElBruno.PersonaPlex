namespace ElBruno.PersonaPlex;

/// <summary>
/// Configuration options for PersonaPlex inference.
/// </summary>
public class PersonaPlexOptions
{
    /// <summary>
    /// Directory where ONNX models are stored. Models are auto-downloaded if not present.
    /// </summary>
    public string ModelDirectory { get; set; } = "models";

    /// <summary>
    /// The voice preset to use for speech generation.
    /// </summary>
    public VoicePreset VoicePreset { get; set; } = VoicePreset.NATF2;

    /// <summary>
    /// Text prompt defining the persona/role for the conversation.
    /// </summary>
    public string TextPrompt { get; set; } = "You are a wise and friendly teacher. Answer questions or provide advice in a clear and engaging way.";

    /// <summary>
    /// Random seed for reproducible generation.
    /// </summary>
    public int? Seed { get; set; }

    /// <summary>
    /// The execution provider to use for ONNX Runtime inference.
    /// </summary>
    public ExecutionProvider ExecutionProvider { get; set; } = ExecutionProvider.CPU;

    /// <summary>
    /// HuggingFace repository ID for ONNX model downloads.
    /// </summary>
    public string HuggingFaceRepoId { get; set; } = "elbruno/personaplex-7b-v1-ONNX";
}
