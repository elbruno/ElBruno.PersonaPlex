using ElBruno.PersonaPlex.BlazorComponents.Tests.TestDoubles;

namespace ElBruno.PersonaPlex.BlazorComponents.Tests;

public sealed class FakePersonaPlexSessionTests
{
    [Fact]
    public async Task FakePersonaPlexSession_QueuesCompletedTurnsAndTracksSubmission()
    {
        var workingDirectory = Path.Combine(AppContext.BaseDirectory, "fake-session-tests", Guid.NewGuid().ToString("N"));
        var session = new FakePersonaPlexSession(workingDirectory);
        session.QueueTurn(new ConversationTurn
        {
            TurnId = "turn-1",
            SequenceNumber = 1,
            StartedAt = new DateTimeOffset(2026, 8, 4, 13, 8, 0, TimeSpan.Zero),
            CompletedAt = new DateTimeOffset(2026, 8, 4, 13, 8, 1, TimeSpan.Zero),
            InputAudio = new PersonaAudioReference { Kind = PersonaAudioKind.UserInput, LocalFilePath = "input.wav" },
            OutputAudio = new PersonaAudioReference { Kind = PersonaAudioKind.AssistantOutput, LocalFilePath = "output.wav", MediaType = "audio/wav" },
            AssistantTranscript = "Queued",
            InferenceTimeMs = 150,
            VoicePreset = VoicePreset.NATM0,
            Status = ConversationTurnStatus.Completed
        });

        var turn = await session.ProcessTurnAsync(new PersonaTurnRequest
        {
            InputAudio = new PersonaAudioReference { Kind = PersonaAudioKind.UserInput, LocalFilePath = "input.wav" },
            OutputAudioPath = "output.wav",
            UserTranscript = "hello"
        });

        Assert.Equal(PersonaSessionState.Ready, session.State);
        Assert.Equal(1, session.ProcessTurnCalls);
        Assert.Equal("Queued", turn.AssistantTranscript);
        Assert.Single(session.TurnHistory);

        await session.DisposeAsync();
    }
}
