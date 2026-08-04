namespace BlazorPersonaPlexDemo;

public static class CodeSnippets
{
    public const string Registration = """
        using BlazorPersonaPlexDemo.Components;
        using BlazorPersonaPlexDemo.Demo;

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddSingleton<FakePersonaSessionFactory>();

        var app = builder.Build();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
        """;

    public const string SpeechToSpeechPanel = """
        @inject FakePersonaSessionFactory SessionFactory

        <SpeechToSpeechPanel Session="@session"
                             ShowAvatar="true"
                             PushToTalk="false"
                             OnTurnCompleted="HandleTurnCompleted" />

        @code {
            private FakePersonaSession session = default!;

            protected override void OnInitialized()
                => session = SessionFactory.CreateLiveSession("Demo host", VoicePreset.NATF2);

            private Task HandleTurnCompleted(ConversationTurn turn)
            {
                Console.WriteLine($"Completed turn #{turn.Index}: {turn.PersonaResponse}");
                return Task.CompletedTask;
            }
        }
        """;

    public const string PersonaStatusCard = """
        @inject FakePersonaSessionFactory SessionFactory

        <PersonaStatusCard Session="@session"
                           ShowVramUsage="true"
                           ShowSessionTimer="true" />

        @code {
            private FakePersonaSession session = default!;

            protected override void OnInitialized()
                => session = SessionFactory.CreateLiveSession("Status demo", VoicePreset.NATM2);
        }
        """;

    public const string ConversationTurnVisualizer = """
        @inject FakePersonaSessionFactory SessionFactory

        <ConversationTurnVisualizer Turns="@session.Turns"
                                    ShowTranscript="true"
                                    MaxTurns="6" />

        @code {
            private FakePersonaSession session = default!;

            protected override void OnInitialized()
                => session = SessionFactory.CreateSeededSession("History demo", VoicePreset.VARF1);
        }
        """;

    public const string AudioWaveform = """
        @inject FakePersonaSessionFactory SessionFactory

        <AudioWaveform Source="AudioSource.Microphone"
                       Samples="@session.MicrophoneLevels"
                       Height="88"
                       BarColor="#0d6efd"
                       BarWidth="6" />

        @code {
            private FakePersonaSession session = default!;

            protected override void OnInitialized()
                => session = SessionFactory.CreateLiveSession("Waveform demo", VoicePreset.VARM0);
        }
        """;
}
