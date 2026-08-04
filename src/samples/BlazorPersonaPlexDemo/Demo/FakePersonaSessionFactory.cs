using ElBruno.PersonaPlex;

namespace BlazorPersonaPlexDemo.Demo;

public sealed class FakePersonaSessionFactory
{
    public FakePersonaSession CreateLiveSession(string sessionName, VoicePreset voicePreset) =>
        new(
            sessionName,
            personaName: "Demo Concierge",
            personaPrompt: "You are a friendly concierge who explains PersonaPlex component behavior in short answers.",
            voicePreset,
            interactive: true);

    public FakePersonaSession CreateSeededSession(string sessionName, VoicePreset voicePreset)
    {
        var session = new FakePersonaSession(
            sessionName,
            personaName: "Release Preview Narrator",
            personaPrompt: "You summarize the current UI state like a release demo host.",
            voicePreset,
            interactive: false);

        session.Seed(
        [
            new ConversationTurn(
                1,
                "Give me a quick status update.",
                "The server-backed preview is running without downloading any models.",
                TimeSpan.FromSeconds(1.6),
                TimeSpan.FromSeconds(2.0),
                DateTimeOffset.UtcNow.AddMinutes(-5),
                "Release Preview Narrator",
                voicePreset),
            new ConversationTurn(
                2,
                "What should reviewers look for?",
                "Validate the component contracts, canned interactions, and Bootstrap-based sample layout.",
                TimeSpan.FromSeconds(1.8),
                TimeSpan.FromSeconds(2.4),
                DateTimeOffset.UtcNow.AddMinutes(-4),
                "Release Preview Narrator",
                voicePreset)
        ],
        "Seeded with a canned fake conversation.");

        return session;
    }
}
