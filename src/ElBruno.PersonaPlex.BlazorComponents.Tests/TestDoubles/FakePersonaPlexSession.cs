namespace ElBruno.PersonaPlex.BlazorComponents.Tests.TestDoubles;

internal sealed class FakePersonaPlexSession : IFileBackedPersonaSession
{
    private readonly Queue<ConversationTurn> _queuedTurns = new();
    private readonly string _storageRoot;
    private readonly bool _deleteStorageRootOnDispose;
    private PersonaSessionStatus _status;

    public FakePersonaPlexSession(string storageRoot, PersonaSessionStatus? initialStatus = null, bool deleteStorageRootOnDispose = true)
    {
        _storageRoot = storageRoot;
        _deleteStorageRootOnDispose = deleteStorageRootOnDispose;
        _status = initialStatus ?? new PersonaSessionStatus
        {
            State = PersonaSessionState.Ready,
            IsModelReady = true,
            CreatedAt = new DateTimeOffset(2026, 8, 4, 13, 7, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 8, 4, 13, 7, 0, TimeSpan.Zero),
            Detail = "Ready for tests."
        };
    }

    public string SessionId { get; } = Guid.NewGuid().ToString("N");

    public string StorageRoot => _storageRoot;

    public PersonaSessionState State => Status.State;

    public PersonaSessionStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public IReadOnlyList<ConversationTurn> Turns => TurnHistory;

    public List<ConversationTurn> TurnHistory { get; } = [];

    public event EventHandler? Changed;

    public int ProcessTurnCalls { get; private set; }

    public PersonaTurnRequest? LastRequest { get; private set; }

    public bool DisposeAsyncCalled { get; private set; }

    public Task<ConversationTurn> ProcessTurnAsync(PersonaTurnRequest request, CancellationToken cancellationToken = default)
    {
        ProcessTurnCalls++;
        LastRequest = request;
        Status = Status with
        {
            State = PersonaSessionState.ProcessingTurn,
            UpdatedAt = DateTimeOffset.UtcNow,
            Detail = "Processing test turn.",
            ErrorMessage = null
        };

        var turn = _queuedTurns.Count > 0
            ? _queuedTurns.Dequeue()
            : BuildDefaultTurn(request);

        if (!string.IsNullOrWhiteSpace(turn.OutputAudio?.LocalFilePath))
        {
            var outputPath = Path.IsPathRooted(turn.OutputAudio.LocalFilePath)
                ? turn.OutputAudio.LocalFilePath
                : Path.Combine(_storageRoot, turn.OutputAudio.LocalFilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllBytes(outputPath, [1, 2, 3, 4]);
            turn = turn with { OutputAudio = turn.OutputAudio with { LocalFilePath = outputPath } };
        }

        TurnHistory.Add(turn);
        Status = Status with
        {
            State = PersonaSessionState.Ready,
            UpdatedAt = DateTimeOffset.UtcNow,
            Detail = "Ready for the next turn.",
            ErrorMessage = null
        };

        return Task.FromResult(turn);
    }

    public ValueTask DisposeAsync()
    {
        DisposeAsyncCalled = true;
        Status = Status with
        {
            State = PersonaSessionState.Disposed,
            UpdatedAt = DateTimeOffset.UtcNow,
            Detail = "Disposed."
        };

        if (_deleteStorageRootOnDispose && Directory.Exists(_storageRoot))
        {
            Directory.Delete(_storageRoot, recursive: true);
        }

        return ValueTask.CompletedTask;
    }

    public void QueueTurn(ConversationTurn turn) => _queuedTurns.Enqueue(turn);

    private ConversationTurn BuildDefaultTurn(PersonaTurnRequest request)
    {
        var outputPath = Path.Combine(_storageRoot, request.OutputAudioPath);
        return new ConversationTurn
        {
            TurnId = Guid.NewGuid().ToString("N"),
            SequenceNumber = TurnHistory.Count + 1,
            Status = ConversationTurnStatus.Completed,
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            InputAudio = request.InputAudio,
            OutputAudio = new PersonaAudioReference
            {
                Kind = PersonaAudioKind.AssistantOutput,
                Format = PersonaAudioFormat.Wav24KhzMono16Bit,
                LocalFilePath = outputPath,
                MediaType = "audio/wav",
                DurationMs = 1000
            },
            UserTranscript = request.UserTranscript,
            AssistantTranscript = "Synthetic response",
            InferenceTimeMs = 125,
            VoicePreset = request.VoicePreset ?? VoicePreset.NATF2,
            TextPrompt = request.TextPrompt,
            Seed = request.Seed
        };
    }
}
