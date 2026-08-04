using ElBruno.PersonaPlex;

namespace BlazorPersonaPlexDemo.Demo;

public interface IPersonaSession
{
    string SessionName { get; }
    string PersonaName { get; }
    string PersonaPrompt { get; }
    string BackendDescription { get; }
    string ModelDirectory { get; }
    bool ModelsPresent { get; }
    VoicePreset VoicePreset { get; }
    PersonaSessionState State { get; }
    string StatusMessage { get; }
    DateTimeOffset StartedAt { get; }
    double? VramUsagePercent { get; }
    bool IsBusy { get; }
    IReadOnlyList<ConversationTurn> Turns { get; }
    IReadOnlyList<double> MicrophoneLevels { get; }
    IReadOnlyList<double> OutputLevels { get; }

    event Action? Changed;

    Task StartAsync(CancellationToken cancellationToken = default);
    Task SubmitUserTurnAsync(string transcript, CancellationToken cancellationToken = default);
    Task ResetAsync(CancellationToken cancellationToken = default);
    Task RefreshWaveformsAsync(CancellationToken cancellationToken = default);
}
