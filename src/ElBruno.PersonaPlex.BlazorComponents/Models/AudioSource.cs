namespace ElBruno.PersonaPlex.BlazorComponents.Models;

/// <summary>
/// Identifies which audio stream an <see cref="Components.AudioWaveform"/> should visualize.
/// </summary>
public enum AudioSource
{
    /// <summary>
    /// Live microphone input captured in the browser.
    /// </summary>
    Microphone,

    /// <summary>
    /// Generated or previously recorded audio rendered from a data URL.
    /// </summary>
    Playback
}
