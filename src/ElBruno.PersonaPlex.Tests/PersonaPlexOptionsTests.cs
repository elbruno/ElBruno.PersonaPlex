using ElBruno.PersonaPlex;

namespace ElBruno.PersonaPlex.Tests;

public class PersonaPlexOptionsTests
{
    [Fact]
    public void DefaultOptions_HaveExpectedValues()
    {
        var options = new PersonaPlexOptions();

        Assert.Equal("models", options.ModelDirectory);
        Assert.Equal(VoicePreset.NATF2, options.VoicePreset);
        Assert.NotNull(options.TextPrompt);
        Assert.Null(options.Seed);
        Assert.Equal(ExecutionProvider.CPU, options.ExecutionProvider);
        Assert.Equal("elbruno/personaplex-7b-v1-ONNX", options.HuggingFaceRepoId);
    }

    [Fact]
    public void Options_CanBeCustomized()
    {
        var options = new PersonaPlexOptions
        {
            ModelDirectory = "/custom/path",
            VoicePreset = VoicePreset.NATM0,
            TextPrompt = "You are a pirate.",
            Seed = 42,
            ExecutionProvider = ExecutionProvider.CUDA,
            HuggingFaceRepoId = "custom/repo"
        };

        Assert.Equal("/custom/path", options.ModelDirectory);
        Assert.Equal(VoicePreset.NATM0, options.VoicePreset);
        Assert.Equal("You are a pirate.", options.TextPrompt);
        Assert.Equal(42, options.Seed);
        Assert.Equal(ExecutionProvider.CUDA, options.ExecutionProvider);
        Assert.Equal("custom/repo", options.HuggingFaceRepoId);
    }
}
