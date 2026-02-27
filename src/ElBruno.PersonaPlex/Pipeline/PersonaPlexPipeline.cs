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
/// Audio is processed in 1-second chunks to match the ONNX trace size and
/// ensure correct output for any duration of input audio.
///
/// The full speech-to-speech pipeline (with 7B LM backbone) is not yet available
/// in ONNX format due to the model's streaming architecture complexity.
/// For reasoning/conversation, pair this with an LLM via the
/// <see href="https://github.com/elbruno/ElBruno.Realtime">ElBruno.Realtime</see> library.
/// </summary>
public class PersonaPlexPipeline : IDisposable
{
    private const int SampleRate = 24000;
    private const int ChunkSamples = SampleRate; // 1 second, matching ONNX trace

    private readonly string _modelDir;
    private readonly PersonaPlexOptions _options;
    private InferenceSession? _encoderSession;
    private InferenceSession? _decoderSession;
    private bool _disposed;
    private int _framesPerChunk; // detected from first encoder run

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
    /// Long audio is automatically split into 1-second chunks for processing.
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

        if (audioData.Length <= ChunkSamples)
            return Task.FromResult(EncodeSingleChunk(audioData));

        // Chunked encoding for long audio
        var codeChunks = new List<long[,,]>();
        for (int offset = 0; offset < audioData.Length; offset += ChunkSamples)
        {
            var len = Math.Min(ChunkSamples, audioData.Length - offset);
            var chunk = new float[len];
            Array.Copy(audioData, offset, chunk, 0, len);

            var codes = EncodeSingleChunk(chunk);
            codeChunks.Add(codes);
        }

        return Task.FromResult(ConcatenateCodes(codeChunks));
    }

    /// <summary>
    /// Decodes discrete audio tokens back into a WAV audio file using the Mimi decoder.
    /// Long token sequences are automatically split into chunks for processing.
    /// </summary>
    /// <param name="codes">Audio tokens [batch=1, codebooks=8, frames].</param>
    /// <param name="outputPath">Output WAV file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task DecodeAsync(long[,,] codes, string outputPath, CancellationToken cancellationToken = default)
    {
        if (_decoderSession is null)
            throw new InvalidOperationException("Mimi decoder model not loaded. Ensure mimi_decoder.onnx is present.");

        ValidatePath(outputPath, nameof(outputPath));

        var totalFrames = codes.GetLength(2);
        var codebooks = codes.GetLength(1);
        var framesPerChunk = _framesPerChunk > 0 ? _framesPerChunk : totalFrames;

        if (totalFrames <= framesPerChunk)
        {
            var samples = DecodeSingleChunk(codes);
            WavWriter.WriteWav(outputPath, samples, SampleRate);
            return Task.CompletedTask;
        }

        // Chunked decoding for long sequences
        var allSamples = new List<float[]>();
        for (int offset = 0; offset < totalFrames; offset += framesPerChunk)
        {
            var len = Math.Min(framesPerChunk, totalFrames - offset);
            var chunk = ExtractCodeSlice(codes, codebooks, offset, len);
            allSamples.Add(DecodeSingleChunk(chunk));
        }

        // Concatenate all decoded audio
        var totalLen = allSamples.Sum(s => s.Length);
        var output = new float[totalLen];
        int pos = 0;
        foreach (var s in allSamples)
        {
            s.CopyTo(output, pos);
            pos += s.Length;
        }

        WavWriter.WriteWav(outputPath, output, SampleRate);
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

    // ── Single-chunk encode/decode ──────────────────────────────

    private long[,,] EncodeSingleChunk(float[] audio)
    {
        var inputTensor = new DenseTensor<float>(audio, [1, 1, audio.Length]);
        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("audio", inputTensor) };

        using var results = _encoderSession!.Run(inputs);
        var codesTensor = results.First().AsTensor<long>();

        var dims = codesTensor.Dimensions;
        var codes = new long[dims[0], dims[1], dims[2]];
        for (int b = 0; b < dims[0]; b++)
            for (int k = 0; k < dims[1]; k++)
                for (int t = 0; t < dims[2]; t++)
                    codes[b, k, t] = codesTensor[b, k, t];

        // Detect frames-per-chunk from first full-length encode
        if (_framesPerChunk == 0 && audio.Length == ChunkSamples)
            _framesPerChunk = dims[2];

        return codes;
    }

    private float[] DecodeSingleChunk(long[,,] codes)
    {
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

        using var results = _decoderSession!.Run(inputs);
        var audioTensor = results.First().AsTensor<float>();

        var numDims = audioTensor.Dimensions.Length;
        int sampleCount;
        float[] samples;

        if (numDims == 3)
        {
            // Expected shape: [B, 1, T]
            sampleCount = audioTensor.Dimensions[2];
            samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
                samples[i] = audioTensor[0, 0, i];
        }
        else if (numDims == 2)
        {
            // Possible shape: [B, T]
            sampleCount = audioTensor.Dimensions[1];
            samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
                samples[i] = audioTensor[0, i];
        }
        else
        {
            // Fallback: flatten
            sampleCount = (int)audioTensor.Length;
            samples = new float[sampleCount];
            int si = 0;
            foreach (var v in audioTensor)
                samples[si++] = v;
        }

        return samples;
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static long[,,] ConcatenateCodes(List<long[,,]> chunks)
    {
        if (chunks.Count == 1) return chunks[0];

        var codebooks = chunks[0].GetLength(1);
        var totalFrames = chunks.Sum(c => c.GetLength(2));
        var result = new long[1, codebooks, totalFrames];

        int offset = 0;
        foreach (var chunk in chunks)
        {
            var frames = chunk.GetLength(2);
            for (int k = 0; k < codebooks; k++)
                for (int t = 0; t < frames; t++)
                    result[0, k, offset + t] = chunk[0, k, t];
            offset += frames;
        }

        return result;
    }

    private static long[,,] ExtractCodeSlice(long[,,] codes, int codebooks, int startFrame, int length)
    {
        var slice = new long[1, codebooks, length];
        for (int k = 0; k < codebooks; k++)
            for (int t = 0; t < length; t++)
                slice[0, k, t] = codes[0, k, startFrame + t];
        return slice;
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
