using ElBruno.PersonaPlex.BlazorComponents.Tests.TestDoubles;
using Microsoft.AspNetCore.Components.Web;

namespace ElBruno.PersonaPlex.BlazorComponents.Tests;

public sealed class SpeechToSpeechPanelTests : BunitContext
{
    [Fact]
    public async Task SpeechToSpeechPanel_FirstRender_EnsuresReadyAndLoadsInteropModule()
    {
        var jsRuntime = new RecordingJSRuntime();
        var stateService = CreateGuardedStateService();
        var session = new FakePersonaPlexSession(Path.Combine(AppContext.BaseDirectory, "speech-panel-tests", Guid.NewGuid().ToString("N")));
        Services.AddSingleton<Microsoft.JSInterop.IJSRuntime>(jsRuntime);
        Services.AddSingleton(stateService);

        var cut = Render<SpeechToSpeechPanel>(parameters => parameters.Add(p => p.Session, session));

        Assert.Equal(PersonaSessionState.Ready, session.State);
        Assert.Null(stateService.Session);
        Assert.Contains(jsRuntime.Invocations, invocation => invocation.Identifier == "import");
        Assert.Contains("Start recording", cut.Markup);

        await stateService.DisposeAsync();
        await session.DisposeAsync();
    }

    [Fact]
    public async Task SpeechToSpeechPanel_SuppliedSession_SubmitsTurnWithoutCreatingStateServiceSession()
    {
        var jsRuntime = new RecordingJSRuntime();
        jsRuntime.Module.QueueResult("stopRecording", "data:audio/wav;base64,dGVzdA==");

        var stateService = CreateGuardedStateService();
        var session = new FakePersonaPlexSession(Path.Combine(AppContext.BaseDirectory, "speech-panel-tests", Guid.NewGuid().ToString("N")));
        session.QueueTurn(new ConversationTurn
        {
            TurnId = "turn-1",
            SequenceNumber = 1,
            StartedAt = new DateTimeOffset(2026, 8, 4, 13, 8, 0, TimeSpan.Zero),
            CompletedAt = new DateTimeOffset(2026, 8, 4, 13, 8, 1, TimeSpan.Zero),
            InputAudio = new PersonaAudioReference { Kind = PersonaAudioKind.UserInput, LocalFilePath = "input.wav", MediaType = "audio/wav" },
            UserTranscript = "What is new?",
            OutputAudio = new PersonaAudioReference { Kind = PersonaAudioKind.AssistantOutput, LocalFilePath = "output.wav", MediaType = "audio/wav", DurationMs = 1000 },
            AssistantTranscript = "All good.",
            InferenceTimeMs = 250,
            VoicePreset = VoicePreset.NATF2,
            Status = ConversationTurnStatus.Completed
        });

        var completedTurns = new List<ConversationTurn>();
        Services.AddSingleton<Microsoft.JSInterop.IJSRuntime>(jsRuntime);
        Services.AddSingleton(stateService);

        var cut = Render<SpeechToSpeechPanel>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.OnTurnCompleted, turn => completedTurns.Add(turn)));

        SetPrivateField(cut.Instance, "_turnNote", "What is new?");
        await InvokePrivateAsync(cut.Instance, "ToggleRecordingAsync");
        await InvokePrivateAsync(cut.Instance, "ToggleRecordingAsync");

        Assert.Contains(jsRuntime.Module.Invocations, invocation => invocation.Identifier == "startRecording");
        Assert.Contains(jsRuntime.Module.Invocations, invocation => invocation.Identifier == "stopRecording");
        Assert.Equal(1, session.ProcessTurnCalls);
        Assert.Equal("What is new?", session.LastRequest?.UserTranscript);
        Assert.Equal(session.SessionId, cut.Instance.Session?.SessionId);
        Assert.Equal("All good.", Assert.Single(completedTurns).AssistantTranscript);
        Assert.Null(stateService.Session);
        Assert.Contains("Latest response", cut.Markup);

        await stateService.DisposeAsync();
        await session.DisposeAsync();
    }

    [Fact]
    public async Task SpeechToSpeechPanel_SuppliedSession_ResetIsDisabledAndDoesNotDisposeSuppliedSession()
    {
        var jsRuntime = new RecordingJSRuntime();
        jsRuntime.Module.QueueResult("stopRecording", "data:audio/wav;base64,dGVzdA==");
        var stateService = CreateGuardedStateService();
        var session = new FakePersonaPlexSession(Path.Combine(AppContext.BaseDirectory, "speech-panel-tests", Guid.NewGuid().ToString("N")));

        Services.AddSingleton<Microsoft.JSInterop.IJSRuntime>(jsRuntime);
        Services.AddSingleton(stateService);

        var cut = Render<SpeechToSpeechPanel>(parameters => parameters.Add(p => p.Session, session));
        SetPrivateField(cut.Instance, "_latestResponseAudioUrl", "data:audio/wav;base64,dGVzdA==");
        session.TurnHistory.Add(new ConversationTurn
        {
            TurnId = "turn-1",
            SequenceNumber = 1,
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            InputAudio = new PersonaAudioReference { Kind = PersonaAudioKind.UserInput, LocalFilePath = "input.wav", MediaType = "audio/wav" },
            AssistantTranscript = "All good.",
            Status = ConversationTurnStatus.Completed
        });

        session.QueueTurn(new ConversationTurn
        {
            TurnId = "turn-2",
            SequenceNumber = 2,
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            InputAudio = new PersonaAudioReference { Kind = PersonaAudioKind.UserInput, LocalFilePath = "input-2.wav", MediaType = "audio/wav" },
            OutputAudio = new PersonaAudioReference { Kind = PersonaAudioKind.AssistantOutput, LocalFilePath = "output-2.wav", MediaType = "audio/wav", DurationMs = 1000 },
            AssistantTranscript = "Still usable.",
            Status = ConversationTurnStatus.Completed
        });

        var resetButton = cut.Find("button.secondary-button");
        Assert.True(resetButton.HasAttribute("disabled"));

        await InvokePrivateAsync(cut.Instance, "ResetSessionAsync");
        SetPrivateField(cut.Instance, "_turnNote", "Check session survives reset.");
        await InvokePrivateAsync(cut.Instance, "ToggleRecordingAsync");
        await InvokePrivateAsync(cut.Instance, "ToggleRecordingAsync");

        cut.WaitForAssertion(() =>
        {
            Assert.False(session.DisposeAsyncCalled);
            Assert.Null(stateService.Session);
            Assert.Equal(1, session.ProcessTurnCalls);
            Assert.Equal("Check session survives reset.", session.LastRequest?.UserTranscript);
            Assert.Contains("Latest response", cut.Markup);
        });

        await stateService.DisposeAsync();
        await session.DisposeAsync();
    }

    [Fact]
    public async Task SpeechToSpeechPanel_SuppliedSession_DisposePreservesCallerOwnedTurnAudioFiles()
    {
        var jsRuntime = new RecordingJSRuntime();
        jsRuntime.Module.QueueResult("stopRecording", "data:audio/wav;base64,dGVzdA==");
        var workingDirectory = Path.Combine(AppContext.BaseDirectory, "speech-panel-tests", Guid.NewGuid().ToString("N"));
        var stateService = CreateGuardedStateService(workingDirectory);
        var sessionRoot = Path.Combine(workingDirectory, "supplied-session");
        var session = new FakePersonaPlexSession(sessionRoot);

        Services.AddSingleton<Microsoft.JSInterop.IJSRuntime>(jsRuntime);
        Services.AddSingleton(stateService);

        var cut = Render<SpeechToSpeechPanel>(parameters => parameters.Add(p => p.Session, session));

        SetPrivateField(cut.Instance, "_turnNote", "Preserve session files.");
        await InvokePrivateAsync(cut.Instance, "ToggleRecordingAsync");
        await InvokePrivateAsync(cut.Instance, "ToggleRecordingAsync");

        var turn = Assert.Single(session.TurnHistory);
        var inputPath = AssertFilePathUnderRoot(turn.InputAudio.LocalFilePath, session.StorageRoot);
        var outputPath = AssertFilePathUnderRoot(turn.OutputAudio?.LocalFilePath, session.StorageRoot);
        var legacyPanelStorageRoot = Path.Combine(stateService.WorkingDirectory, "external-sessions", session.SessionId);

        await cut.Instance.DisposeAsync();

        Assert.True(File.Exists(inputPath));
        Assert.True(File.Exists(outputPath));
        Assert.False(Directory.Exists(legacyPanelStorageRoot));

        await stateService.DisposeAsync();
        await session.DisposeAsync();
    }

    [Fact]
    public async Task SpeechToSpeechPanel_SuppliedSession_SwapPreservesPriorTurnAudioFiles()
    {
        var jsRuntime = new RecordingJSRuntime();
        jsRuntime.Module.QueueResult("stopRecording", "data:audio/wav;base64,dGVzdA==");
        var workingDirectory = Path.Combine(AppContext.BaseDirectory, "speech-panel-tests", Guid.NewGuid().ToString("N"));
        var stateService = CreateGuardedStateService(workingDirectory);
        var firstSession = new FakePersonaPlexSession(Path.Combine(workingDirectory, "first-session"));
        var secondSession = new FakePersonaPlexSession(Path.Combine(workingDirectory, "second-session"));

        Services.AddSingleton<Microsoft.JSInterop.IJSRuntime>(jsRuntime);
        Services.AddSingleton(stateService);

        var cut = Render<SpeechToSpeechPanel>(parameters => parameters.Add(p => p.Session, firstSession));

        SetPrivateField(cut.Instance, "_turnNote", "Keep old session files.");
        await InvokePrivateAsync(cut.Instance, "ToggleRecordingAsync");
        await InvokePrivateAsync(cut.Instance, "ToggleRecordingAsync");

        var originalTurn = Assert.Single(firstSession.TurnHistory);
        var originalInputPath = AssertFilePathUnderRoot(originalTurn.InputAudio.LocalFilePath, firstSession.StorageRoot);
        var originalOutputPath = AssertFilePathUnderRoot(originalTurn.OutputAudio?.LocalFilePath, firstSession.StorageRoot);

        cut.Render(parameters => parameters.Add(p => p.Session, secondSession));

        Assert.True(File.Exists(originalInputPath));
        Assert.True(File.Exists(originalOutputPath));

        await stateService.DisposeAsync();
        await firstSession.DisposeAsync();
        await secondSession.DisposeAsync();
    }

    [Fact]
    public async Task SpeechToSpeechPanel_SuppliedNonFileBackedSession_TraversalSessionIdStaysWithinFallbackRoot()
    {
        var jsRuntime = new RecordingJSRuntime();
        jsRuntime.Module.QueueResult("stopRecording", "data:audio/wav;base64,dGVzdA==");
        var workingDirectory = Path.Combine(AppContext.BaseDirectory, "speech-panel-tests", Guid.NewGuid().ToString("N"));
        var stateService = CreateGuardedStateService(workingDirectory);
        var session = new FakeSuppliedPersonaSession(@"..\..\escape");

        Services.AddSingleton<Microsoft.JSInterop.IJSRuntime>(jsRuntime);
        Services.AddSingleton(stateService);

        var cut = Render<SpeechToSpeechPanel>(parameters => parameters.Add(p => p.Session, session));

        await InvokePrivateAsync(cut.Instance, "ToggleRecordingAsync");
        await InvokePrivateAsync(cut.Instance, "ToggleRecordingAsync");

        var fallbackRoot = Path.Combine(stateService.WorkingDirectory, "external-sessions");
        var inputPath = AssertFilePathUnderRoot(session.LastRequest?.InputAudio?.LocalFilePath, fallbackRoot);
        AssertFilePathUnderRoot(session.LastRequest?.OutputAudioPath, fallbackRoot);
        var ownedSessionRoot = Path.GetDirectoryName(inputPath);
        Assert.DoesNotContain($@"{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}", session.LastRequest?.InputAudio?.LocalFilePath, StringComparison.Ordinal);

        await cut.Instance.DisposeAsync();
        Assert.False(Directory.Exists(ownedSessionRoot));
        await stateService.DisposeAsync();
        await session.DisposeAsync();
    }

    [Fact]
    public async Task SpeechToSpeechPanel_SuppliedNonFileBackedSession_RootedSessionIdStaysWithinFallbackRoot()
    {
        var jsRuntime = new RecordingJSRuntime();
        jsRuntime.Module.QueueResult("stopRecording", "data:audio/wav;base64,dGVzdA==");
        var workingDirectory = Path.Combine(AppContext.BaseDirectory, "speech-panel-tests", Guid.NewGuid().ToString("N"));
        var stateService = CreateGuardedStateService(workingDirectory);
        var session = new FakeSuppliedPersonaSession(Path.Combine(Path.GetPathRoot(workingDirectory)!, "rooted-session"));

        Services.AddSingleton<Microsoft.JSInterop.IJSRuntime>(jsRuntime);
        Services.AddSingleton(stateService);

        var cut = Render<SpeechToSpeechPanel>(parameters => parameters.Add(p => p.Session, session));

        await InvokePrivateAsync(cut.Instance, "ToggleRecordingAsync");
        await InvokePrivateAsync(cut.Instance, "ToggleRecordingAsync");

        var fallbackRoot = Path.Combine(stateService.WorkingDirectory, "external-sessions");
        var inputPath = AssertFilePathUnderRoot(session.LastRequest?.InputAudio?.LocalFilePath, fallbackRoot);
        AssertFilePathUnderRoot(session.LastRequest?.OutputAudioPath, fallbackRoot);
        var ownedSessionRoot = Path.GetDirectoryName(inputPath);

        await cut.Instance.DisposeAsync();
        Assert.False(Directory.Exists(ownedSessionRoot));
        await stateService.DisposeAsync();
        await session.DisposeAsync();
    }

    [Fact]
    public async Task SpeechToSpeechPanel_SuppliedSession_ParameterSwapWhileStopIsInFlight_DiscardsStaleAudio()
    {
        var jsRuntime = new RecordingJSRuntime();
        var pendingStop = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        jsRuntime.Module.QueueDeferredResult("stopRecording", pendingStop.Task);

        var workingDirectory = Path.Combine(AppContext.BaseDirectory, "speech-panel-tests", Guid.NewGuid().ToString("N"));
        var stateService = CreateGuardedStateService(workingDirectory);
        var firstSession = new FakeSuppliedPersonaSession("first-session");
        var secondSession = new FakeSuppliedPersonaSession("second-session");

        Services.AddSingleton<Microsoft.JSInterop.IJSRuntime>(jsRuntime);
        Services.AddSingleton(stateService);

        var completedTurns = new List<ConversationTurn>();
        var cut = Render<SpeechToSpeechPanel>(parameters => parameters
            .Add(p => p.Session, firstSession)
            .Add(p => p.OnTurnCompleted, turn => completedTurns.Add(turn)));

        await InvokePrivateAsync(cut.Instance, "StartRecordingAsync");

        var stopTask = InvokePrivateAsync(cut.Instance, "StopRecordingAsync");
        await Task.Yield();

        cut.Render(parameters => parameters.Add(p => p.Session, secondSession));
        pendingStop.SetResult("data:audio/wav;base64,dGVzdA==");
        await stopTask;

        Assert.Equal(0, firstSession.ProcessTurnCalls);
        Assert.Equal(0, secondSession.ProcessTurnCalls);
        Assert.Empty(completedTurns);

        await stateService.DisposeAsync();
        await firstSession.DisposeAsync();
        await secondSession.DisposeAsync();
    }

    [Fact]
    public async Task SpeechToSpeechPanel_PushToTalk_StopsOnMouseLeave()
    {
        var jsRuntime = new RecordingJSRuntime();
        jsRuntime.Module.QueueResult("stopRecording", "data:audio/wav;base64,dGVzdA==");

        var stateService = CreateGuardedStateService();
        var session = new FakePersonaPlexSession(Path.Combine(AppContext.BaseDirectory, "speech-panel-tests", Guid.NewGuid().ToString("N")));
        Services.AddSingleton<Microsoft.JSInterop.IJSRuntime>(jsRuntime);
        Services.AddSingleton(stateService);

        var cut = Render<SpeechToSpeechPanel>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.PushToTalk, true));

        await InvokePrivateAsync(cut.Instance, "StartRecordingAsync");
        await InvokePrivateAsync(cut.Instance, "HandlePointerLeaveAsync");

        Assert.Contains(jsRuntime.Module.Invocations, invocation => invocation.Identifier == "startRecording");
        Assert.Contains(jsRuntime.Module.Invocations, invocation => invocation.Identifier == "stopRecording");
        Assert.Equal(1, session.ProcessTurnCalls);
        Assert.Null(stateService.Session);

        await stateService.DisposeAsync();
        await session.DisposeAsync();
    }

    [Fact]
    public async Task SpeechToSpeechPanel_ResetWhileStopIsInFlight_DiscardsStaleAudio()
    {
        var jsRuntime = new RecordingJSRuntime();
        var pendingStop = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        jsRuntime.Module.QueueDeferredResult("stopRecording", pendingStop.Task);

        var workingDirectory = Path.Combine(AppContext.BaseDirectory, "speech-panel-tests", Guid.NewGuid().ToString("N"));
        var firstSession = new FakePersonaPlexSession(workingDirectory);
        var stateService = CreateGuardedStateService(workingDirectory);

        Services.AddSingleton<Microsoft.JSInterop.IJSRuntime>(jsRuntime);
        Services.AddSingleton(stateService);

        var completedTurns = new List<ConversationTurn>();
        var cut = Render<SpeechToSpeechPanel>(parameters => parameters
            .Add(p => p.Session, firstSession)
            .Add(p => p.OnTurnCompleted, turn => completedTurns.Add(turn)));

        await InvokePrivateAsync(cut.Instance, "StartRecordingAsync");

        var stopTask = InvokePrivateAsync(cut.Instance, "StopRecordingAsync");
        await Task.Yield();

        await InvokePrivateAsync(cut.Instance, "ResetSessionAsync");
        pendingStop.SetResult("data:audio/wav;base64,dGVzdA==");
        await stopTask;

        Assert.Equal(0, firstSession.ProcessTurnCalls);
        Assert.Empty(completedTurns);
        Assert.Contains(jsRuntime.Module.Invocations, invocation => invocation.Identifier == "endMicrophoneCapture");
        Assert.False(firstSession.DisposeAsyncCalled);
        Assert.Null(stateService.Session);

        await stateService.DisposeAsync();
        await firstSession.DisposeAsync();
    }

    [Fact]
    public async Task SpeechToSpeechPanel_RendersLoadingAndErrorStates()
    {
        var jsRuntime = new RecordingJSRuntime();
        var stateService = CreateStateService(out var session, new PersonaSessionStatus
        {
            State = PersonaSessionState.Initializing,
            IsModelReady = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Detail = "Loading"
        });

        Services.AddSingleton<Microsoft.JSInterop.IJSRuntime>(jsRuntime);
        Services.AddSingleton(stateService);

        var cut = Render<SpeechToSpeechPanel>(parameters => parameters.Add(p => p.Session, session));
        Assert.Null(stateService.Session);
        Assert.True(cut.Find("button.talk-button").HasAttribute("disabled"));

        session.Status = session.Status with
        {
            State = PersonaSessionState.Faulted,
            ErrorMessage = "Session failed",
            UpdatedAt = DateTimeOffset.UtcNow
        };

        cut.WaitForAssertion(() => Assert.Contains("Session failed", cut.Markup));

        await stateService.DisposeAsync();
        await session.DisposeAsync();
    }

    private static PersonaPlexStateService CreateStateService(out FakePersonaPlexSession session, PersonaSessionStatus? initialStatus = null)
    {
        var workingDirectory = Path.Combine(AppContext.BaseDirectory, "speech-panel-tests", Guid.NewGuid().ToString("N"));
        var createdSession = new FakePersonaPlexSession(workingDirectory, initialStatus);
        session = createdSession;
        return new PersonaPlexStateService(Microsoft.Extensions.Options.Options.Create(new PersonaPlexBlazorComponentsOptions
        {
            WorkingDirectory = workingDirectory,
            SessionFactory = (_, _) => Task.FromResult<IPersonaSession>(createdSession)
        }));
    }

    private static PersonaPlexStateService CreateGuardedStateService(string? workingDirectory = null) =>
        new(Microsoft.Extensions.Options.Options.Create(new PersonaPlexBlazorComponentsOptions
        {
            WorkingDirectory = workingDirectory ?? Path.Combine(AppContext.BaseDirectory, "speech-panel-tests", Guid.NewGuid().ToString("N")),
            SessionFactory = static (_, _) => throw new InvalidOperationException("State service session should not be used when Session is supplied.")
        }));

    private static async Task InvokePrivateAsync(object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);

        var task = method!.Invoke(target, null);
        switch (task)
        {
            case Task asyncTask:
                await asyncTask;
                break;
            case ValueTask valueTask:
                await valueTask;
                break;
        }
    }

    private static void SetPrivateField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(target, value);
    }

    private static string AssertFilePathUnderRoot(string? filePath, string expectedRoot)
    {
        Assert.False(string.IsNullOrWhiteSpace(filePath));

        var fullPath = Path.GetFullPath(filePath!);
        var normalizedRoot = EnsureTrailingSeparator(Path.GetFullPath(expectedRoot));
        Assert.StartsWith(normalizedRoot, EnsureTrailingSeparator(fullPath), StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(fullPath));
        return fullPath;
    }

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
}
