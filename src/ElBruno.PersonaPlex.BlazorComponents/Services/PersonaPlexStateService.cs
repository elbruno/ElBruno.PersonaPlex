using ElBruno.PersonaPlex.BlazorComponents.Options;
using ElBruno.PersonaPlex.Sessions;
using Microsoft.Extensions.Options;

namespace ElBruno.PersonaPlex.BlazorComponents.Services;

/// <summary>
/// Creates and tracks the current server-backed <see cref="IPersonaSession"/> for Blazor components.
/// </summary>
public sealed class PersonaPlexStateService : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly PersonaPlexBlazorComponentsOptions _options;
    private readonly Dictionary<string, string> _audioSources = new(StringComparer.OrdinalIgnoreCase);
    private IPersonaSession? _session;
    private string? _sessionStorageRoot;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonaPlexStateService"/> class.
    /// </summary>
    public PersonaPlexStateService(IOptions<PersonaPlexBlazorComponentsOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Raised whenever the active session or its observable state changes.
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// Gets the active session when one has been initialized.
    /// </summary>
    public IPersonaSession? Session => _session;

    /// <summary>
    /// Gets the latest status snapshot when a session exists.
    /// </summary>
    public PersonaSessionStatus? Status => _session?.Status;

    /// <summary>
    /// Gets the current turn history.
    /// </summary>
    public IReadOnlyList<ConversationTurn> Turns => _session?.Turns ?? [];

    /// <summary>
    /// Gets the UTC timestamp when the current session started.
    /// </summary>
    public DateTimeOffset? SessionStartedAt => _session?.Status.CreatedAt;

    /// <summary>
    /// Gets the model directory used by the current session options.
    /// </summary>
    public string ModelDirectory => _options.PipelineOptions.ModelDirectory;

    /// <summary>
    /// Gets the server-side working directory used by the active session.
    /// </summary>
    public string WorkingDirectory => _sessionStorageRoot ?? _options.WorkingDirectory;

    /// <summary>
    /// Gets the latest session duration.
    /// </summary>
    public TimeSpan SessionDuration =>
        SessionStartedAt is null ? TimeSpan.Zero : DateTimeOffset.UtcNow - SessionStartedAt.Value;

    /// <summary>
    /// Gets the latest error message, when present.
    /// </summary>
    public string? ErrorMessage => _session?.Status.ErrorMessage;

    /// <summary>
    /// Gets VRAM usage percentage when the current runtime exposes it.
    /// </summary>
    public double? VramUsagePercent => null;

    /// <summary>
    /// Ensures a file-based server session exists and is ready for turn processing.
    /// </summary>
    public async Task<IPersonaSession> EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        if (_session is not null)
        {
            return _session;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_session is not null)
            {
                return _session;
            }

            Directory.CreateDirectory(_options.WorkingDirectory);
            _sessionStorageRoot = Path.Combine(_options.WorkingDirectory, Guid.NewGuid().ToString("N"));

            _session = _options.SessionFactory is not null
                ? await _options.SessionFactory(_sessionStorageRoot, cancellationToken).ConfigureAwait(false)
                : await FileBasedPersonaSessionAdapter.CreateAsync(
                    modelDir: _options.PipelineOptions.ModelDirectory,
                    options: CloneOptions(_options.PipelineOptions),
                    sessionOptionsFactory: _options.SessionOptionsFactory,
                    storageRoot: _sessionStorageRoot,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            _session.Changed += HandleSessionChanged;
            NotifyStateChanged();
            return _session;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Processes a browser-captured WAV turn through the active session.
    /// </summary>
    public async Task<ConversationTurn> SubmitTurnAsync(
        string audioDataUrl,
        string? userTranscript = null,
        VoicePreset? voicePreset = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audioDataUrl);

        var session = await EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var (audioBytes, mediaType) = DecodeDataUrl(audioDataUrl);
            var turnId = Guid.NewGuid().ToString("N");
            var inputPath = $"input-{turnId}.wav";
            var outputPath = $"output-{turnId}.wav";
            var resolvedInputPath = Path.Combine(_sessionStorageRoot!, inputPath);

            Directory.CreateDirectory(Path.GetDirectoryName(resolvedInputPath)!);
            await File.WriteAllBytesAsync(resolvedInputPath, audioBytes, cancellationToken).ConfigureAwait(false);

            var turn = await session.ProcessTurnAsync(
                new PersonaTurnRequest
                {
                    InputAudio = new PersonaAudioReference
                    {
                        Kind = PersonaAudioKind.UserInput,
                        Format = PersonaAudioFormat.Wav24KhzMono16Bit,
                        LocalFilePath = inputPath,
                        MediaType = mediaType
                    },
                    OutputAudioPath = outputPath,
                    UserTranscript = userTranscript,
                    VoicePreset = voicePreset,
                    TextPrompt = _options.PipelineOptions.TextPrompt,
                    Seed = _options.PipelineOptions.Seed
                },
                cancellationToken).ConfigureAwait(false);

            _audioSources[resolvedInputPath] = audioDataUrl;

            if (!string.IsNullOrWhiteSpace(turn.OutputAudio?.LocalFilePath))
            {
                if (turn.OutputAudio.LocalFilePath.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    _audioSources[turn.OutputAudio.LocalFilePath] = turn.OutputAudio.LocalFilePath;
                }
                else
                {
                    var outputBytes = await File.ReadAllBytesAsync(turn.OutputAudio.LocalFilePath, cancellationToken).ConfigureAwait(false);
                    _audioSources[turn.OutputAudio.LocalFilePath] =
                        $"data:{turn.OutputAudio.MediaType ?? "audio/wav"};base64,{Convert.ToBase64String(outputBytes)}";
                }
            }

            NotifyStateChanged();
            return turn;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Starts a fresh session and clears cached browser audio sources.
    /// </summary>
    public async Task ResetSessionAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var sessionStorageRoot = _sessionStorageRoot;
            if (_session is not null)
            {
                _session.Changed -= HandleSessionChanged;
                await _session.DisposeAsync().ConfigureAwait(false);
            }

            _audioSources.Clear();
            _session = null;
            _sessionStorageRoot = null;
            DeleteOwnedStorageRoot(sessionStorageRoot);
            NotifyStateChanged();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Resolves a browser-playable audio source for the supplied session audio reference.
    /// </summary>
    public string? GetAudioSource(PersonaAudioReference? audioReference)
    {
        var localFilePath = audioReference?.LocalFilePath;
        if (string.IsNullOrWhiteSpace(localFilePath))
        {
            return null;
        }

        if (localFilePath.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return localFilePath;
        }

        var fullPath = Path.IsPathRooted(localFilePath)
            ? Path.GetFullPath(localFilePath)
            : _sessionStorageRoot is null
                ? null
                : Path.GetFullPath(Path.Combine(_sessionStorageRoot, localFilePath));

        return fullPath is not null && _audioSources.TryGetValue(fullPath, out var source)
            ? source
            : null;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await ResetSessionAsync().ConfigureAwait(false);
        _gate.Dispose();
    }

    private void HandleSessionChanged(object? sender, EventArgs e) => NotifyStateChanged();

    private void NotifyStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    private static void DeleteOwnedStorageRoot(string? storageRoot)
    {
        if (!string.IsNullOrWhiteSpace(storageRoot) && Directory.Exists(storageRoot))
        {
            Directory.Delete(storageRoot, recursive: true);
        }
    }

    private static PersonaPlexOptions CloneOptions(PersonaPlexOptions options) =>
        new()
        {
            ExecutionProvider = options.ExecutionProvider,
            HuggingFaceRepoId = options.HuggingFaceRepoId,
            ModelDirectory = options.ModelDirectory,
            Seed = options.Seed,
            TextPrompt = options.TextPrompt,
            VoicePreset = options.VoicePreset
        };

    private static (byte[] AudioBytes, string MediaType) DecodeDataUrl(string dataUrl)
    {
        var separatorIndex = dataUrl.IndexOf(',', StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            throw new InvalidOperationException("Expected a base64 audio data URL.");
        }

        var header = dataUrl[..separatorIndex];
        var payload = dataUrl[(separatorIndex + 1)..];

        if (!header.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            !header.Contains(";base64", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Expected a base64 audio data URL.");
        }

        var mediaType = header[5..header.IndexOf(';')];
        return (Convert.FromBase64String(payload), mediaType);
    }
}
