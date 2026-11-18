using ElBruno.PersonaPlex.BlazorComponents.Models;
using ElBruno.PersonaPlex.BlazorComponents.Tests.TestDoubles;

namespace ElBruno.PersonaPlex.BlazorComponents.Tests;

public sealed class AudioWaveformTests : BunitContext
{
    [Fact]
    public async Task AudioWaveform_MicrophoneSource_ImportsModuleAttachesAndDisposes()
    {
        var jsRuntime = new RecordingJSRuntime();
        Services.AddSingleton<Microsoft.JSInterop.IJSRuntime>(jsRuntime);

        var cut = Render<AudioWaveform>(parameters => parameters
            .Add(p => p.Source, AudioSource.Microphone)
            .Add(p => p.Height, 64)
            .Add(p => p.BarColor, "#0d6efd")
            .Add(p => p.BarWidth, 3)
            .Add(p => p.IsActive, true));

        var canvas = cut.Find("canvas.audio-waveform");
        Assert.Equal("64", canvas.GetAttribute("height"));
        Assert.Equal("import", Assert.Single(jsRuntime.Invocations).Identifier);
        Assert.Equal("attachMicrophoneWaveform", Assert.Single(jsRuntime.Module.Invocations).Identifier);

        await cut.Instance.DisposeAsync();

        Assert.Contains(jsRuntime.Module.Invocations, invocation => invocation.Identifier == "detachWaveform");
        Assert.True(jsRuntime.Module.DisposeAsyncCalled);
    }

    [Fact]
    public void AudioWaveform_PlaybackSource_RendersWaveformForAudioData()
    {
        var jsRuntime = new RecordingJSRuntime();
        Services.AddSingleton<Microsoft.JSInterop.IJSRuntime>(jsRuntime);

        Render<AudioWaveform>(parameters => parameters
            .Add(p => p.Source, AudioSource.Playback)
            .Add(p => p.AudioDataUrl, "data:audio/wav;base64,ZmFrZQ==")
            .Add(p => p.BarColor, "#ff00ff")
            .Add(p => p.BarWidth, 5));

        var playbackInvocation = Assert.Single(jsRuntime.Module.Invocations);
        Assert.Equal("renderPlaybackWaveform", playbackInvocation.Identifier);
        Assert.Equal("data:audio/wav;base64,ZmFrZQ==", playbackInvocation.Arguments[1]);
    }
}
