using ElBruno.PersonaPlex.BlazorComponents.Tests.TestDoubles;

namespace ElBruno.PersonaPlex.BlazorComponents.Tests;

public sealed class PersonaStatusCardTests : TestContext
{
    [Fact]
    public async Task PersonaStatusCard_RendersReadyTimerAndFaultedState()
    {
        var stateService = CreateStateService(out var session, initialStatus: new PersonaSessionStatus
        {
            State = PersonaSessionState.Ready,
            IsModelReady = true,
            CreatedAt = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(2).Add(TimeSpan.FromSeconds(5))),
            UpdatedAt = DateTimeOffset.UtcNow,
            Detail = "Ready"
        });
        await stateService.EnsureReadyAsync();
        Services.AddSingleton(stateService);

        var cut = RenderComponent<PersonaStatusCard>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.ShowVramUsage, true)
            .Add(p => p.ShowSessionTimer, true));

        Assert.Contains("Ready for the next turn.", cut.Markup);
        Assert.Contains("Model directory", cut.Markup);
        Assert.Contains("Current runtime does not expose VRAM telemetry.", cut.Markup);
        Assert.Matches(@"\d{2}:\d{2}", cut.Markup);

        session.Status = session.Status with
        {
            State = PersonaSessionState.Faulted,
            ErrorMessage = "GPU unavailable",
            UpdatedAt = DateTimeOffset.UtcNow
        };

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("The last turn failed.", cut.Markup);
            Assert.Contains("GPU unavailable", cut.Markup);
        });

        await stateService.DisposeAsync();
    }

    [Fact]
    public async Task PersonaStatusCard_RendersLoadingState()
    {
        var stateService = CreateStateService(out var session, initialStatus: new PersonaSessionStatus
        {
            State = PersonaSessionState.Initializing,
            IsModelReady = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Detail = "Loading"
        });
        await stateService.EnsureReadyAsync();
        Services.AddSingleton(stateService);

        var cut = RenderComponent<PersonaStatusCard>(parameters => parameters.Add(p => p.Session, session));

        Assert.Contains("Loading ONNX sessions on the server.", cut.Markup);
        Assert.Contains("Initializing", cut.Markup);

        await stateService.DisposeAsync();
    }

    private static PersonaPlexStateService CreateStateService(out FakePersonaPlexSession session, PersonaSessionStatus? initialStatus = null)
    {
        var workingDirectory = Path.Combine(AppContext.BaseDirectory, "status-card-tests", Guid.NewGuid().ToString("N"));
        var createdSession = new FakePersonaPlexSession(workingDirectory, initialStatus);
        session = createdSession;
        return new PersonaPlexStateService(Microsoft.Extensions.Options.Options.Create(new PersonaPlexBlazorComponentsOptions
        {
            WorkingDirectory = workingDirectory,
            SessionFactory = (_, _) => Task.FromResult<IPersonaSession>(createdSession)
        }));
    }
}
