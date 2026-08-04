namespace ElBruno.PersonaPlex.BlazorComponents.Tests;

public sealed class ConversationTurnVisualizerTests : TestContext
{
    [Fact]
    public void ConversationTurnVisualizer_ShowsEmptyStateWithoutTurns()
    {
        Services.AddSingleton(new PersonaPlexStateService(Microsoft.Extensions.Options.Options.Create(new PersonaPlexBlazorComponentsOptions())));

        var cut = RenderComponent<ConversationTurnVisualizer>(parameters => parameters
            .Add(p => p.Turns, Array.Empty<ConversationTurn>()));

        Assert.Contains("No conversation turns yet.", cut.Markup);
    }

    [Fact]
    public void ConversationTurnVisualizer_ShowsNewestMaxTurnsInChronologicalOrder()
    {
        Services.AddSingleton(new PersonaPlexStateService(Microsoft.Extensions.Options.Options.Create(new PersonaPlexBlazorComponentsOptions())));

        var turns = new[]
        {
            new ConversationTurn
            {
                TurnId = "turn-1",
                SequenceNumber = 1,
                StartedAt = new DateTimeOffset(2026, 8, 4, 13, 6, 0, TimeSpan.Zero),
                InputAudio = new PersonaAudioReference { Kind = PersonaAudioKind.UserInput, LocalFilePath = "input1.wav" },
                UserTranscript = "Oldest",
                ErrorMessage = "Network failed",
                VoicePreset = VoicePreset.VARM2,
                Status = ConversationTurnStatus.Failed
            },
            new ConversationTurn
            {
                TurnId = "turn-2",
                SequenceNumber = 2,
                StartedAt = new DateTimeOffset(2026, 8, 4, 13, 7, 0, TimeSpan.Zero),
                InputAudio = new PersonaAudioReference { Kind = PersonaAudioKind.UserInput, LocalFilePath = "input2.wav" },
                UserTranscript = "Processing turn",
                VoicePreset = VoicePreset.NATM1,
                Status = ConversationTurnStatus.Processing
            },
            new ConversationTurn
            {
                TurnId = "turn-3",
                SequenceNumber = 3,
                StartedAt = new DateTimeOffset(2026, 8, 4, 13, 8, 0, TimeSpan.Zero),
                InputAudio = new PersonaAudioReference { Kind = PersonaAudioKind.UserInput, LocalFilePath = "input3.wav" },
                UserTranscript = "Newest",
                OutputAudio = new PersonaAudioReference { Kind = PersonaAudioKind.AssistantOutput, LocalFilePath = "output3.wav" },
                AssistantTranscript = "Newest response",
                InferenceTimeMs = 200,
                VoicePreset = VoicePreset.NATF2,
                Status = ConversationTurnStatus.Completed
            }
        };

        var cut = RenderComponent<ConversationTurnVisualizer>(parameters => parameters
            .Add(p => p.Turns, turns)
            .Add(p => p.MaxTurns, 2)
            .Add(p => p.ShowTranscript, true));

        Assert.Equal(2, cut.FindAll(".turn-card").Count);
        Assert.Contains("Newest response", cut.Markup);
        Assert.Contains("Generating response audio...", cut.Markup);
        Assert.DoesNotContain("Oldest", cut.Markup);
        Assert.True(
            cut.Markup.IndexOf("Processing turn", StringComparison.Ordinal) <
            cut.Markup.IndexOf("Newest response", StringComparison.Ordinal));

        cut.SetParametersAndRender(parameters => parameters
            .Add(p => p.Turns, turns)
            .Add(p => p.MaxTurns, 3)
            .Add(p => p.ShowTranscript, false));

        Assert.DoesNotContain("Newest response", cut.Markup);
        Assert.DoesNotContain("Processing turn", cut.Markup);
        Assert.Contains("Network failed", cut.Markup);
    }
}
