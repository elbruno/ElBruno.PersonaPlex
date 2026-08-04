using ElBruno.PersonaPlex.Pipeline;
using ElBruno.PersonaPlex.Sessions;
using System.Reflection;

namespace ElBruno.PersonaPlex.Tests;

public sealed class FileBasedPersonaSessionAdapterTests
{
    [Fact]
    public async Task FileBasedPersonaSessionAdapter_DisposeAsync_DeletesDefaultOwnedStorageRoot()
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var adapter = new FileBasedPersonaSessionAdapter(CreatePipeline(), sessionId: sessionId);
        var storageRoot = adapter.StorageRoot;

        Assert.True(Directory.Exists(storageRoot));

        await adapter.DisposeAsync();

        Assert.False(Directory.Exists(storageRoot));
    }

    [Fact]
    public async Task FileBasedPersonaSessionAdapter_DisposeAsync_PreservesCallerProvidedStorageRoot()
    {
        var storageRoot = Path.Combine(AppContext.BaseDirectory, "adapter-tests", Guid.NewGuid().ToString("N"));
        var adapter = new FileBasedPersonaSessionAdapter(CreatePipeline(), storageRoot: storageRoot);

        Assert.True(Directory.Exists(storageRoot));

        await adapter.DisposeAsync();

        Assert.True(Directory.Exists(storageRoot));
        Directory.Delete(storageRoot, recursive: true);
    }

    private static PersonaPlexPipeline CreatePipeline()
    {
        var constructor = typeof(PersonaPlexPipeline).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(string), typeof(PersonaPlexOptions), typeof(Microsoft.ML.OnnxRuntime.InferenceSession), typeof(Microsoft.ML.OnnxRuntime.InferenceSession)],
            modifiers: null);

        Assert.NotNull(constructor);
        return (PersonaPlexPipeline)constructor!.Invoke(
            [AppContext.BaseDirectory, new PersonaPlexOptions(), null, null]);
    }
}
