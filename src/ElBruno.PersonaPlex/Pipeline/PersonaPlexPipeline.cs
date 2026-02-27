using Microsoft.ML.OnnxRuntime;

namespace ElBruno.PersonaPlex.Pipeline;

/// <summary>
/// Main pipeline for PersonaPlex speech-to-speech inference using ONNX Runtime.
/// </summary>
public class PersonaPlexPipeline : IDisposable
{
    private readonly string _modelDir;
    private readonly PersonaPlexOptions _options;
    private readonly Func<SessionOptions>? _sessionOptionsFactory;
    private bool _disposed;

    private PersonaPlexPipeline(string modelDir, PersonaPlexOptions options, Func<SessionOptions>? sessionOptionsFactory)
    {
        _modelDir = modelDir;
        _options = options;
        _sessionOptionsFactory = sessionOptionsFactory;
    }

    /// <summary>
    /// Creates a new PersonaPlex pipeline, downloading models if necessary.
    /// </summary>
    /// <param name="modelDir">Directory for model files. Uses default cache if null.</param>
    /// <param name="options">Pipeline configuration options.</param>
    /// <param name="sessionOptionsFactory">Optional factory for ONNX session options (for GPU acceleration).</param>
    /// <param name="progress">Optional progress callback for model downloads.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An initialized PersonaPlexPipeline.</returns>
    public static async Task<PersonaPlexPipeline> CreateAsync(
        string? modelDir = null,
        PersonaPlexOptions? options = null,
        Func<SessionOptions>? sessionOptionsFactory = null,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new PersonaPlexOptions();
        var resolvedDir = await ModelManager.EnsureModelsAsync(
            modelDir ?? options.ModelDirectory,
            options.HuggingFaceRepoId,
            progress,
            cancellationToken);

        return new PersonaPlexPipeline(resolvedDir, options, sessionOptionsFactory);
    }

    /// <summary>
    /// Processes an input audio file through the PersonaPlex model.
    /// </summary>
    /// <param name="inputAudioPath">Path to the input WAV audio file.</param>
    /// <param name="voicePreset">Voice preset for the response.</param>
    /// <param name="textPrompt">Text prompt defining the persona/role.</param>
    /// <param name="outputPath">Path for the output WAV file.</param>
    /// <param name="seed">Optional random seed for reproducibility.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ConversationResult"/> with details about the generated output.</returns>
    public async Task<ConversationResult> ProcessAsync(
        string inputAudioPath,
        VoicePreset? voicePreset = null,
        string? textPrompt = null,
        string outputPath = "output.wav",
        int? seed = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedVoice = voicePreset ?? _options.VoicePreset;
        var resolvedPrompt = textPrompt ?? _options.TextPrompt;
        var resolvedSeed = seed ?? _options.Seed;

        var startTime = DateTime.UtcNow;

        // TODO: Implement ONNX inference pipeline
        // 1. Load input audio → Mimi Encoder → tokens
        // 2. Feed tokens + text prompt + voice embedding → Main LM
        // 3. Main LM output tokens → Mimi Decoder → output audio
        // 4. Write output WAV file

        await Task.CompletedTask; // Placeholder for async ONNX inference

        var inferenceTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

        return new ConversationResult
        {
            OutputAudioPath = outputPath,
            InferenceTimeMs = inferenceTime,
            VoicePreset = resolvedVoice,
            TextPrompt = resolvedPrompt,
            Seed = resolvedSeed
        };
    }

    /// <summary>
    /// Gets the model directory path.
    /// </summary>
    public string ModelDirectory => _modelDir;

    /// <summary>
    /// Gets the current pipeline options.
    /// </summary>
    public PersonaPlexOptions Options => _options;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // TODO: Dispose ONNX sessions when implemented
        GC.SuppressFinalize(this);
    }
}
