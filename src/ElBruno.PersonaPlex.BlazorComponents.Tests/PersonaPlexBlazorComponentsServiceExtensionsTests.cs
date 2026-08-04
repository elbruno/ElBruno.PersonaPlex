namespace ElBruno.PersonaPlex.BlazorComponents.Tests;

public sealed class PersonaPlexBlazorComponentsServiceExtensionsTests
{
    [Fact]
    public async Task AddPersonaPlexBlazorComponents_RegistersOptionsAndSessionServices()
    {
        var services = new ServiceCollection();

        services.AddPersonaPlexBlazorComponents(options =>
        {
            options.WorkingDirectory = "custom-working-directory";
            options.PipelineOptions.ModelDirectory = "custom-model-directory";
        });

        await using var provider = services.BuildServiceProvider();
        var concrete = provider.GetRequiredService<PersonaPlexStateService>();
        var options = provider.GetRequiredService<IOptions<PersonaPlexBlazorComponentsOptions>>();

        Assert.Equal("custom-working-directory", concrete.WorkingDirectory);
        Assert.Equal("custom-model-directory", concrete.ModelDirectory);
        Assert.Equal("custom-working-directory", options.Value.WorkingDirectory);
        Assert.Equal("custom-model-directory", options.Value.PipelineOptions.ModelDirectory);
    }
}
