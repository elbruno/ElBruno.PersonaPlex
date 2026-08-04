using ElBruno.PersonaPlex.BlazorComponents.Tests.TestDoubles;

namespace ElBruno.PersonaPlex.BlazorComponents.Tests;

public sealed class PersonaPlexStateServiceTests
{
    [Fact]
    public async Task PersonaPlexStateService_UsesCustomSessionFactoryAndCachesAudioSources()
    {
        var workingDirectory = Path.Combine(AppContext.BaseDirectory, "state-service-tests", Guid.NewGuid().ToString("N"));
        var fakeSession = new FakePersonaPlexSession(workingDirectory);
        var service = new PersonaPlexStateService(Microsoft.Extensions.Options.Options.Create(new PersonaPlexBlazorComponentsOptions
        {
            WorkingDirectory = workingDirectory,
            SessionFactory = (_, _) => Task.FromResult<IPersonaSession>(fakeSession)
        }));

        var turn = await service.SubmitTurnAsync("data:audio/wav;base64,AQIDBA==", "hello");
        var outputSource = service.GetAudioSource(turn.OutputAudio);
        var inputSource = service.GetAudioSource(turn.InputAudio);

        Assert.Same(fakeSession, service.Session);
        Assert.Equal(1, fakeSession.ProcessTurnCalls);
        Assert.StartsWith("data:audio/wav;base64,", outputSource);
        Assert.Equal("data:audio/wav;base64,AQIDBA==", inputSource);

        await service.ResetSessionAsync();

        Assert.Null(service.Session);
        Assert.True(fakeSession.DisposeAsyncCalled);
    }

    [Fact]
    public async Task PersonaPlexStateService_ResetSessionAsync_DeletesOwnedStorageRootEvenWhenSessionDoesNot()
    {
        var workingDirectory = Path.Combine(AppContext.BaseDirectory, "state-service-tests", Guid.NewGuid().ToString("N"));
        var fakeSession = new FakePersonaPlexSession(workingDirectory, deleteStorageRootOnDispose: false);
        var service = new PersonaPlexStateService(Microsoft.Extensions.Options.Options.Create(new PersonaPlexBlazorComponentsOptions
        {
            WorkingDirectory = workingDirectory,
            SessionFactory = (_, _) => Task.FromResult<IPersonaSession>(fakeSession)
        }));

        await service.SubmitTurnAsync("data:audio/wav;base64,AQIDBA==", "cleanup");
        var ownedStorageRoot = service.WorkingDirectory;

        Assert.True(Directory.Exists(ownedStorageRoot));

        await service.ResetSessionAsync();

        Assert.True(fakeSession.DisposeAsyncCalled);
        Assert.False(Directory.Exists(ownedStorageRoot));
        Assert.Null(service.Session);
    }
}
