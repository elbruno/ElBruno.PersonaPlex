using ElBruno.PersonaPlex;

namespace BlazorPersonaPlexDemo.Demo;

public sealed record ConversationTurn(
    int Index,
    string UserTranscript,
    string PersonaResponse,
    TimeSpan UserAudioDuration,
    TimeSpan PersonaAudioDuration,
    DateTimeOffset Timestamp,
    string PersonaName,
    VoicePreset VoicePreset);
