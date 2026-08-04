using ElBruno.PersonaPlex;

namespace BlazorPersonaPlexDemo.Demo;

public sealed class FakePersonaSession : IPersonaSession
{
    private readonly List<ConversationTurn> _turns = [];
    private readonly Random _random;

    public FakePersonaSession(
        string sessionName,
        string personaName,
        string personaPrompt,
        VoicePreset voicePreset,
        bool interactive)
    {
        SessionName = sessionName;
        PersonaName = personaName;
        PersonaPrompt = personaPrompt;
        VoicePreset = voicePreset;
        IsInteractive = interactive;
        BackendDescription = interactive
            ? "Interactive server-backed preview"
            : "Canned fake session";
        ModelDirectory = ModelManager.DefaultModelDir;
        _random = new Random(HashCode.Combine(sessionName, personaName, voicePreset));
        StartedAt = DateTimeOffset.UtcNow;
        StatusMessage = interactive
            ? "Waiting to start the preview session."
            : "Loaded canned conversation for design-time review.";
        State = interactive ? PersonaSessionState.NotStarted : PersonaSessionState.Ready;
        VramUsagePercent = interactive ? 18 : 12;
        MicrophoneLevels = BuildWaveform(AudioSource.Microphone);
        OutputLevels = BuildWaveform(AudioSource.PersonaOutput);
    }

    public string SessionName { get; }
    public string PersonaName { get; }
    public string PersonaPrompt { get; }
    public string BackendDescription { get; }
    public string ModelDirectory { get; }
    public bool ModelsPresent => ModelManager.AreModelsPresent(ModelDirectory);
    public VoicePreset VoicePreset { get; }
    public PersonaSessionState State { get; private set; }
    public string StatusMessage { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public double? VramUsagePercent { get; private set; }
    public bool IsInteractive { get; }
    public bool IsBusy => State is PersonaSessionState.Bootstrapping or PersonaSessionState.Capturing or PersonaSessionState.Responding;
    public IReadOnlyList<ConversationTurn> Turns => _turns;
    public IReadOnlyList<double> MicrophoneLevels { get; private set; }
    public IReadOnlyList<double> OutputLevels { get; private set; }

    public event Action? Changed;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (State is PersonaSessionState.Ready or PersonaSessionState.Capturing or PersonaSessionState.Responding)
        {
            return;
        }

        State = PersonaSessionState.Bootstrapping;
        StatusMessage = "Bootstrapping interactive demo session on the server.";
        NotifyChanged();

        await Task.Delay(250, cancellationToken);

        StartedAt = DateTimeOffset.UtcNow;
        State = PersonaSessionState.Ready;
        StatusMessage = "Session ready. Submit a transcript to simulate a duplex turn.";
        VramUsagePercent = 22;
        await RefreshWaveformsAsync(cancellationToken);
    }

    public async Task SubmitUserTurnAsync(string transcript, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return;
        }

        if (State == PersonaSessionState.NotStarted)
        {
            await StartAsync(cancellationToken);
        }

        State = PersonaSessionState.Capturing;
        StatusMessage = "Capturing a simulated microphone turn.";
        MicrophoneLevels = BuildWaveform(AudioSource.Microphone);
        NotifyChanged();

        await Task.Delay(225, cancellationToken);

        State = PersonaSessionState.Responding;
        StatusMessage = "Generating a fake PersonaPlex response on the server.";
        OutputLevels = BuildWaveform(AudioSource.PersonaOutput);
        VramUsagePercent = Math.Min(74, (VramUsagePercent ?? 18) + _random.Next(4, 10));
        NotifyChanged();

        await Task.Delay(350, cancellationToken);

        var turn = new ConversationTurn(
            Index: _turns.Count + 1,
            UserTranscript: transcript.Trim(),
            PersonaResponse: BuildResponse(transcript),
            UserAudioDuration: TimeSpan.FromSeconds(Math.Round(1.1 + transcript.Length / 24d, 1)),
            PersonaAudioDuration: TimeSpan.FromSeconds(Math.Round(1.5 + transcript.Length / 30d, 1)),
            Timestamp: DateTimeOffset.UtcNow,
            PersonaName: PersonaName,
            VoicePreset: VoicePreset);

        _turns.Add(turn);
        State = PersonaSessionState.Ready;
        StatusMessage = $"Completed turn #{turn.Index} using {VoicePreset}.";
        VramUsagePercent = Math.Max(18, (VramUsagePercent ?? 18) - _random.Next(3, 8));
        NotifyChanged();
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _turns.Clear();
        State = IsInteractive ? PersonaSessionState.NotStarted : PersonaSessionState.Ready;
        StatusMessage = IsInteractive
            ? "Interactive preview reset. Start again when ready."
            : "Canned session restored to its seeded state.";
        StartedAt = DateTimeOffset.UtcNow;
        VramUsagePercent = IsInteractive ? 18 : 12;
        MicrophoneLevels = BuildWaveform(AudioSource.Microphone);
        OutputLevels = BuildWaveform(AudioSource.PersonaOutput);
        NotifyChanged();
        return Task.CompletedTask;
    }

    public Task RefreshWaveformsAsync(CancellationToken cancellationToken = default)
    {
        MicrophoneLevels = BuildWaveform(AudioSource.Microphone);
        OutputLevels = BuildWaveform(AudioSource.PersonaOutput);
        NotifyChanged();
        return Task.CompletedTask;
    }

    public void Seed(IEnumerable<ConversationTurn> turns, string statusMessage)
    {
        _turns.Clear();
        _turns.AddRange(turns);
        State = PersonaSessionState.Ready;
        StatusMessage = statusMessage;
        StartedAt = DateTimeOffset.UtcNow.AddMinutes(-6);
        VramUsagePercent = 14;
        MicrophoneLevels = BuildWaveform(AudioSource.Microphone);
        OutputLevels = BuildWaveform(AudioSource.PersonaOutput);
        NotifyChanged();
    }

    public Task SetFaultAsync(string message)
    {
        State = PersonaSessionState.Faulted;
        StatusMessage = message;
        VramUsagePercent = 0;
        NotifyChanged();
        return Task.CompletedTask;
    }

    private string BuildResponse(string transcript)
    {
        var promptLens = PersonaPrompt.Length > 72
            ? $"{PersonaPrompt[..72]}..."
            : PersonaPrompt;

        return $"{PersonaName} ({VoicePreset}) acknowledges \"{transcript.Trim()}\" and answers with a concise follow-up shaped by the prompt: {promptLens}";
    }

    private IReadOnlyList<double> BuildWaveform(AudioSource source)
    {
        var phase = source == AudioSource.Microphone ? 0.55 : 1.1;
        var bars = new double[18];

        for (var i = 0; i < bars.Length; i++)
        {
            var harmonic = Math.Sin((i + 1) * phase + _random.NextDouble());
            var noise = _random.NextDouble() * 0.35;
            var baseValue = source == AudioSource.Microphone ? 0.28 : 0.4;
            bars[i] = Math.Clamp((baseValue + Math.Abs(harmonic) * 0.45 + noise) * 100, 12, 96);
        }

        return bars;
    }

    private void NotifyChanged() => Changed?.Invoke();
}
