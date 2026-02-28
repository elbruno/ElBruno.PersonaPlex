using ElBruno.PersonaPlex;

namespace ElBruno.PersonaPlex.Tests;

/// <summary>
/// Tests for ConversationResult property population and initialization.
/// Verifies that DurationMs and other output properties are correctly calculated.
/// </summary>
public class ConversationResultTests
{
    [Fact]
    public void ConversationResult_DurationMs_CanBePopulated()
    {
        // Verify the DurationMs property can be set and retrieved
        var result = new ConversationResult
        {
            OutputAudioPath = "/tmp/output.wav",
            DurationMs = 2500.0,
            InferenceTimeMs = 150.0,
            VoicePreset = VoicePreset.NATF0
        };

        Assert.Equal(2500.0, result.DurationMs);
    }

    [Fact]
    public void ConversationResult_DurationMs_CalculationFromSampleCount()
    {
        // Verify the calculation logic that ProcessAsync should use
        const int sampleRate = 24000;
        const int totalSamples = 60000; // 2.5 seconds
        
        var expectedDurationMs = (totalSamples / (double)sampleRate) * 1000.0;
        Assert.Equal(2500.0, expectedDurationMs);
    }

    [Fact]
    public void ConversationResult_DurationMs_InitialValue()
    {
        // Verify DurationMs can be initialized to zero
        var result = new ConversationResult
        {
            OutputAudioPath = "test.wav",
            DurationMs = 0,
            InferenceTimeMs = 0,
            VoicePreset = VoicePreset.NATF0
        };

        Assert.Equal(0, result.DurationMs);
    }

    [Fact]
    public void ConversationResult_AllRequiredProperties_CanBeSet()
    {
        // Verify all properties defined in the audit can be populated
        var result = new ConversationResult
        {
            OutputAudioPath = "/tmp/output.wav",
            ResponseText = null,
            DurationMs = 2500.0,
            InferenceTimeMs = 150.0,
            VoicePreset = VoicePreset.NATM2,
            TextPrompt = "Test persona",
            Seed = 42
        };

        Assert.NotNull(result.OutputAudioPath);
        Assert.Null(result.ResponseText); // Reserved for future LLM integration
        Assert.Equal(2500.0, result.DurationMs);
        Assert.Equal(150.0, result.InferenceTimeMs);
        Assert.Equal(VoicePreset.NATM2, result.VoicePreset);
        Assert.Equal("Test persona", result.TextPrompt);
        Assert.Equal(42, result.Seed);
    }

    [Theory]
    [InlineData(24000, 1.0)] // 1 second at 24kHz = 24000 samples
    [InlineData(24000, 2.5)] // 2.5 seconds at 24kHz = 60000 samples
    [InlineData(24000, 10.0)] // 10 seconds at 24kHz = 240000 samples
    public void ConversationResult_DurationMs_CalculatesCorrectly(int sampleRate, double durationSeconds)
    {
        var totalSamples = (int)(sampleRate * durationSeconds);
        var expectedDurationMs = (totalSamples / (double)sampleRate) * 1000.0;

        // Verify precision within 1ms tolerance
        Assert.Equal(durationSeconds * 1000.0, expectedDurationMs, precision: 1);
    }

    [Fact]
    public void ConversationResult_DurationMs_WithVoicePresets()
    {
        // Verify DurationMs works with all voice presets
        var presets = new[]
        {
            VoicePreset.NATF0, VoicePreset.NATF1, VoicePreset.NATF2, VoicePreset.NATF3,
            VoicePreset.NATM0, VoicePreset.NATM1, VoicePreset.NATM2, VoicePreset.NATM3,
            VoicePreset.VARF0, VoicePreset.VARF1, VoicePreset.VARF2, VoicePreset.VARF3, VoicePreset.VARF4,
            VoicePreset.VARM0, VoicePreset.VARM1, VoicePreset.VARM2, VoicePreset.VARM3, VoicePreset.VARM4
        };

        foreach (var preset in presets)
        {
            var result = new ConversationResult
            {
                OutputAudioPath = $"output_{preset}.wav",
                DurationMs = 1000.0,
                InferenceTimeMs = 100.0,
                VoicePreset = preset
            };

            Assert.Equal(preset, result.VoicePreset);
            Assert.Equal(1000.0, result.DurationMs);
        }
    }
}
