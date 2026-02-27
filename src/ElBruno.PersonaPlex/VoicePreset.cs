namespace ElBruno.PersonaPlex;

/// <summary>
/// Represents the available voice presets for PersonaPlex.
/// </summary>
public enum VoicePreset
{
    // Natural Female
    NATF0,
    NATF1,
    NATF2,
    NATF3,

    // Natural Male
    NATM0,
    NATM1,
    NATM2,
    NATM3,

    // Variety Female
    VARF0,
    VARF1,
    VARF2,
    VARF3,
    VARF4,

    // Variety Male
    VARM0,
    VARM1,
    VARM2,
    VARM3,
    VARM4
}

/// <summary>
/// Extension methods for <see cref="VoicePreset"/>.
/// </summary>
public static class VoicePresetExtensions
{
    /// <summary>
    /// Gets the embedding filename for this voice preset.
    /// </summary>
    public static string GetEmbeddingFileName(this VoicePreset preset) =>
        $"{preset}.pt";
}
