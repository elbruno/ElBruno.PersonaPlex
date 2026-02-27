using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using ElBruno.PersonaPlex.Audio;

namespace ElBruno.PersonaPlex.Pipeline;

/// <summary>
/// Main pipeline for PersonaPlex audio encoding/decoding using ONNX Runtime.
///
/// Currently supports the Mimi audio codec (encoder + decoder) which converts
/// between 24kHz audio waveforms and discrete token representations.
///
/// The full speech-to-speech pipeline (with 7B LM backbone) is not yet available
/// in ONNX format due to the model's streaming architecture complexity.
/// For reasoning/conversation, pair this with an LLM via Microsoft Agent Framework
/// (see scenario-04-blazor-aspire).
/// </summary>
public class PersonaPlexPipeline : IDisposable
{
    private readonly string _modelDir;
    private readonly PersonaPlexOptions _options;
    private InferenceSession? _encoderSession;
    private InferenceSession? _decoderSession;
    private bool _disposed;

    private PersonaPlexPipeline(
        string modelDir,
        PersonaPlexOptions options,
        InferenceSession? encoder,
        InferenceSession? decoder)
    {
        _modelDir = modelDir;
        _options = options;
        _encoderSession = encoder;
        _decoderSession = decoder;
    }

    /// <summary>
    /// Creates a new PersonaPlex pipeline, downloading models if necessary.
    /// </summary>
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

        // Load ONNX sessions
        var so = sessionOptionsFactory?.Invoke() ?? SessionOptionsHelper.CreateOptions(options.ExecutionProvider);

        InferenceSession? encoder = null;
        InferenceSession? decoder = null;

        var encoderPath = Path.Combine(resolvedDir, "mimi_encoder.onnx");
        var decoderPath = Path.Combine(resolvedDir, "mimi_decoder.onnx");

        if (File.Exists(encoderPath))
            encoder = new InferenceSession(encoderPath, so);

        if (File.Exists(decoderPath))
            decoder = new InferenceSession(decoderPath, so);

        return new PersonaPlexPipeline(resolvedDir, options, encoder, decoder);
    }

    /// <summary>
    /// Encodes a WAV audio file into discrete audio tokens using the Mimi encoder.
    /// </summary>
    /// <param name="inputAudioPath">Path to a 24kHz mono WAV file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audio tokens as a 3D array [batch=1, codebooks=8, frames].</returns>
    public Task<long[,,]> EncodeAsync(string inputAudioPath, CancellationToken cancellationToken = default)
    {
        if (_encoderSession is null)
            throw new InvalidOperationException("Mimi encoder model not loaded. Ensure mimi_encoder.onnx is present.");

        ValidatePath(inputAudioPath, nameof(inputAudioPath));

        var audioData = WavReader.ReadMono24kHz(inputAudioPath);
        var inputTensor = new DenseTensor<float>(audioData, [1, 1, audioData.Length]);
        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("audio", inputTensor) };

        using var results = _encoderSession.Run(inputs);
        var codesTensor = results.First().AsTensor<long>();

        // Copy to managed array
        var dims = codesTensor.Dimensions;
        var codes = new long[dims[0], dims[1], dims[2]];
        for (int b = 0; b < dims[0]; b++)
            for (int k = 0; k < dims[1]; k++)
                for (int t = 0; t < dims[2]; t++)
                    codes[b, k, t] = codesTensor[b, k, t];

        return Task.FromResult(codes);
    }

    /// <summary>
    /// Decodes discrete audio tokens back into a WAV audio file using the Mimi decoder.
    /// </summary>
    /// <param name="codes">Audio tokens [batch=1, codebooks=8, frames].</param>
    /// <param name="outputPath">Output WAV file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task DecodeAsync(long[,,] codes, string outputPath, CancellationToken cancellationToken = default)
    {
        if (_decoderSession is null)
            throw new InvalidOperationException("Mimi decoder model not loaded. Ensure mimi_decoder.onnx is present.");

        ValidatePath(outputPath, nameof(outputPath));

        var d0 = codes.GetLength(0);
        var d1 = codes.GetLength(1);
        var d2 = codes.GetLength(2);
        var flat = new long[d0 * d1 * d2];
        int idx = 0;
        for (int b = 0; b < d0; b++)
            for (int k = 0; k < d1; k++)
                for (int t = 0; t < d2; t++)
                    flat[idx++] = codes[b, k, t];

        var inputTensor = new DenseTensor<long>(flat, [d0, d1, d2]);
        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("codes", inputTensor) };

        using var results = _decoderSession.Run(inputs);
        var audioTensor = results.First().AsTensor<float>();

        // Extract audio samples
        var samples = new float[audioTensor.Dimensions[2]];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = audioTensor[0, 0, i];

        WavWriter.WriteWav(outputPath, samples, 24000);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Processes an input audio file: encode → (placeholder for LM) → decode.
    /// Currently round-trips audio through the Mimi codec (encode then decode).
    /// </summary>
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

        // Step 1: Encode audio → tokens
        var codes = await EncodeAsync(inputAudioPath, cancellationToken);

        // Step 2: LM processing (not yet available in ONNX)
        // The 7B Transformer backbone would go here.
        // For now, the tokens pass through unchanged.

        // Step 3: Decode tokens → audio
        await DecodeAsync(codes, outputPath, cancellationToken);

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
    /// Whether the encoder model is loaded and ready.
    /// </summary>
    public bool IsEncoderLoaded => _encoderSession is not null;

    /// <summary>
    /// Whether the decoder model is loaded and ready.
    /// </summary>
    public bool IsDecoderLoaded => _decoderSession is not null;

    /// <summary>
    /// Gets the model directory path.
    /// </summary>
    public string ModelDirectory => _modelDir;

    /// <summary>
    /// Gets the current pipeline options.
    /// </summary>
    public PersonaPlexOptions Options => _options;

    /// <summary>
    /// Validates that a file path does not contain directory traversal sequences.
    /// </summary>
    private static void ValidatePath(string path, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, paramName);
        var fullPath = Path.GetFullPath(path);
        if (fullPath.Contains("..", StringComparison.Ordinal) || path.Contains('\0'))
            throw new ArgumentException("Path contains invalid characters or traversal sequences.", paramName);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _encoderSession?.Dispose();
        _decoderSession?.Dispose();
        _encoderSession = null;
        _decoderSession = null;

        GC.SuppressFinalize(this);
    }
}
