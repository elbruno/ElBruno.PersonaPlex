namespace ElBruno.PersonaPlex.BlazorComponents.Options;

/// <summary>
/// Configures server-backed behavior for the PersonaPlex Blazor components package.
/// </summary>
public sealed class PersonaPlexBlazorComponentsOptions
{
    /// <summary>
    /// Controls the underlying PersonaPlex pipeline.
    /// </summary>
    public PersonaPlexOptions PipelineOptions { get; } = new();

    /// <summary>
    /// Server-side working directory used for turn input/output WAV files.
    /// </summary>
    public string WorkingDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "personaplex-ui");

    /// <summary>
    /// Optional callback that can customize the ONNX Runtime session options for pipeline creation.
    /// </summary>
    public Func<Microsoft.ML.OnnxRuntime.SessionOptions>? SessionOptionsFactory { get; set; }

    /// <summary>
    /// Optional session factory used by tests or advanced hosts to provide a custom <see cref="Sessions.IPersonaSession"/>.
    /// </summary>
    public Func<string, CancellationToken, Task<Sessions.IPersonaSession>>? SessionFactory { get; set; }
}
