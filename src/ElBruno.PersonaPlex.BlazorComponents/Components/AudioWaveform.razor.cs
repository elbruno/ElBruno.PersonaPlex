using ElBruno.PersonaPlex.BlazorComponents.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ElBruno.PersonaPlex.BlazorComponents.Components;

/// <summary>
/// Canvas-based waveform renderer for microphone capture or generated playback audio.
/// </summary>
public partial class AudioWaveform
{
    private ElementReference _canvas;
    private IJSObjectReference? _module;

    /// <summary>
    /// Gets or sets the waveform source type.
    /// </summary>
    [Parameter]
    public AudioSource Source { get; set; }

    /// <summary>
    /// Gets or sets the waveform height in CSS pixels.
    /// </summary>
    [Parameter]
    public int Height { get; set; } = 60;

    /// <summary>
    /// Gets or sets the waveform bar color.
    /// </summary>
    [Parameter]
    public string BarColor { get; set; } = "#0d6efd";

    /// <summary>
    /// Gets or sets the waveform bar width.
    /// </summary>
    [Parameter]
    public int BarWidth { get; set; } = 3;

    /// <summary>
    /// Gets or sets the playback audio data URL when <see cref="Source"/> is <see cref="AudioSource.Playback"/>.
    /// </summary>
    [Parameter]
    public string? AudioDataUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the waveform is active.
    /// </summary>
    [Parameter]
    public bool IsActive { get; set; } = true;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        _module ??= await JSRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            "./_content/ElBruno.PersonaPlex.BlazorComponents/personaPlexBlazorComponents.js");

        if (Source == AudioSource.Microphone)
        {
            await _module.InvokeVoidAsync(
                "attachMicrophoneWaveform",
                _canvas,
                new { color = BarColor, barWidth = BarWidth, active = IsActive });
        }
        else
        {
            await _module.InvokeVoidAsync(
                "renderPlaybackWaveform",
                _canvas,
                AudioDataUrl,
                new { color = BarColor, barWidth = BarWidth });
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("detachWaveform", _canvas);
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
