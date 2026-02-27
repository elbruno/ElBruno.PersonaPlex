using ElBruno.PersonaPlex;

namespace ElBruno.PersonaPlex.Tests;

public class VoicePresetTests
{
    [Theory]
    [InlineData(VoicePreset.NATF0, "NATF0.pt")]
    [InlineData(VoicePreset.NATM2, "NATM2.pt")]
    [InlineData(VoicePreset.VARF3, "VARF3.pt")]
    [InlineData(VoicePreset.VARM4, "VARM4.pt")]
    public void GetEmbeddingFileName_ReturnsCorrectName(VoicePreset preset, string expected)
    {
        Assert.Equal(expected, preset.GetEmbeddingFileName());
    }

    [Fact]
    public void AllPresets_HaveUniqueEmbeddingFileNames()
    {
        var presets = Enum.GetValues<VoicePreset>();
        var fileNames = presets.Select(p => p.GetEmbeddingFileName()).ToList();
        Assert.Equal(fileNames.Count, fileNames.Distinct().Count());
    }

    [Fact]
    public void VoicePreset_Has18Values()
    {
        // 4 NATF + 4 NATM + 5 VARF + 5 VARM = 18
        Assert.Equal(18, Enum.GetValues<VoicePreset>().Length);
    }
}
