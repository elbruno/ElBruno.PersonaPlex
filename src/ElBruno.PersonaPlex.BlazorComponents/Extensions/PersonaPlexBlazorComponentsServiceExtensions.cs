using ElBruno.PersonaPlex.BlazorComponents.Options;
using ElBruno.PersonaPlex.BlazorComponents.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ElBruno.PersonaPlex.BlazorComponents.Extensions;

/// <summary>
/// Registers the services required by the PersonaPlex Blazor component library.
/// </summary>
public static class PersonaPlexBlazorComponentsServiceExtensions
{
    /// <summary>
    /// Adds the server-backed PersonaPlex Blazor component services.
    /// </summary>
    public static IServiceCollection AddPersonaPlexBlazorComponents(
        this IServiceCollection services,
        Action<PersonaPlexBlazorComponentsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<PersonaPlexBlazorComponentsOptions>();

        if (configure is not null)
        {
            services.Configure(configure);
        }
        services.AddScoped<PersonaPlexStateService>();
        return services;
    }
}
