using ElBruno.PersonaPlex.Pipeline;
using Microsoft.ML.OnnxRuntime;
using System.Text.RegularExpressions;

namespace ElBruno.PersonaPlex.Sessions;

/// <summary>
/// Adapts the current file-based <see cref="PersonaPlexPipeline"/> to the UI-facing <see cref="IPersonaSession"/> contract.
/// This preserves current runtime behavior while giving downstream UI work a stable session boundary.
/// </summary>
public sealed class FileBasedPersonaSessionAdapter : IFileBackedPersonaSession
{
    private static readonly Regex SessionIdPattern = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly PersonaPlexPipeline _pipeline;
    private readonly string _storageRoot;
    private readonly bool _ownsStorageRoot;
    private readonly List<ConversationTurn> _turns = [];
    private readonly SemaphoreSlim _turnGate = new(1, 1);
    private readonly DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
    private bool _disposed;
    private PersonaSessionStatus _status;

    /// <summary>
    /// Creates an adapter around an already-created pipeline.
    /// </summary>
    public FileBasedPersonaSessionAdapter(PersonaPlexPipeline pipeline, string? sessionId = null, string? storageRoot = null)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        SessionId = NormalizeSessionId(sessionId);
        _storageRoot = InitializeStorageRoot(storageRoot, SessionId);
        _ownsStorageRoot = string.IsNullOrWhiteSpace(storageRoot);
        _status = CreateStatus(GetInitialState(), detail: "Ready for turn-based file processing.");
    }

    /// <summary>
    /// Creates an adapter by constructing the underlying file-based pipeline.
    /// </summary>
    public static async Task<FileBasedPersonaSessionAdapter> CreateAsync(
        string? modelDir = null,
        PersonaPlexOptions? options = null,
        Func<SessionOptions>? sessionOptionsFactory = null,
        IProgress<DownloadProgress>? progress = null,
        string? sessionId = null,
        string? storageRoot = null,
        CancellationToken cancellationToken = default)
    {
        var pipeline = await PersonaPlexPipeline.CreateAsync(
            modelDir,
            options,
            sessionOptionsFactory,
            progress,
            cancellationToken);

        return new FileBasedPersonaSessionAdapter(pipeline, sessionId, storageRoot);
    }

    /// <inheritdoc />
    public string SessionId { get; }

    /// <summary>
    /// Session-scoped storage root used to isolate file-based audio I/O.
    /// </summary>
    public string StorageRoot => _storageRoot;

    /// <inheritdoc />
    public PersonaSessionState State => _status.State;

    /// <inheritdoc />
    public PersonaSessionStatus Status => _status;

    /// <inheritdoc />
    public IReadOnlyList<ConversationTurn> Turns => _turns;

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public async Task<ConversationTurn> ProcessTurnAsync(PersonaTurnRequest request, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.InputAudio);

        var inputAudioPath = request.InputAudio.LocalFilePath;
        if (string.IsNullOrWhiteSpace(inputAudioPath))
            throw new ArgumentException("The current file-based adapter requires InputAudio.LocalFilePath.", nameof(request));

        await _turnGate.WaitAsync(cancellationToken);
        try
        {
            UpdateStatus(PersonaSessionState.ProcessingTurn, detail: "Processing turn.");

            var startedAt = DateTimeOffset.UtcNow;
            var resolvedInputPath = ResolveSessionPath(inputAudioPath, mustExist: true);
            var resolvedOutputPath = ResolveSessionPath(request.OutputAudioPath, mustExist: false);
            Directory.CreateDirectory(Path.GetDirectoryName(resolvedOutputPath)!);
            var result = await _pipeline.ProcessAsync(
                resolvedInputPath,
                request.VoicePreset,
                request.TextPrompt,
                resolvedOutputPath,
                request.Seed,
                cancellationToken);

            var turn = new ConversationTurn
            {
                TurnId = Guid.NewGuid().ToString("N"),
                SequenceNumber = _turns.Count + 1,
                Status = ConversationTurnStatus.Completed,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                InputAudio = request.InputAudio with { LocalFilePath = resolvedInputPath },
                OutputAudio = new PersonaAudioReference
                {
                    Kind = PersonaAudioKind.AssistantOutput,
                    Format = PersonaAudioFormat.Wav24KhzMono16Bit,
                    LocalFilePath = resolvedOutputPath,
                    MediaType = "audio/wav",
                    DurationMs = result.DurationMs
                },
                UserTranscript = request.UserTranscript,
                AssistantTranscript = result.ResponseText,
                InferenceTimeMs = result.InferenceTimeMs,
                VoicePreset = result.VoicePreset,
                TextPrompt = result.TextPrompt,
                Seed = result.Seed
            };

            _turns.Add(turn);
            UpdateStatus(PersonaSessionState.Ready, detail: "Ready for the next turn.");
            return turn;
        }
        catch (OperationCanceledException)
        {
            UpdateStatus(PersonaSessionState.Ready, detail: "Turn processing canceled.");
            throw;
        }
        catch (Exception ex)
        {
            UpdateStatus(PersonaSessionState.Faulted, detail: "Turn processing failed.", errorMessage: ex.Message);
            throw;
        }
        finally
        {
            _turnGate.Release();
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
            return ValueTask.CompletedTask;

        _disposed = true;
        _pipeline.Dispose();
        _turnGate.Dispose();
        DeleteOwnedStorageRoot();
        UpdateStatus(PersonaSessionState.Disposed, detail: "Session disposed.");
        return ValueTask.CompletedTask;
    }

    private PersonaSessionState GetInitialState() =>
        _pipeline.IsEncoderLoaded && _pipeline.IsDecoderLoaded
            ? PersonaSessionState.Ready
            : PersonaSessionState.Faulted;

    private static string InitializeStorageRoot(string? storageRoot, string sessionId)
    {
        var root = string.IsNullOrWhiteSpace(storageRoot)
            ? Path.GetFullPath(Path.Combine(GetDefaultStorageBaseRoot(), sessionId))
            : Path.GetFullPath(storageRoot);

        if (string.IsNullOrWhiteSpace(storageRoot))
        {
            var baseRoot = EnsureTrailingSeparator(GetDefaultStorageBaseRoot());
            if (!root.StartsWith(baseRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The session storage root must stay within the default storage base path.");
        }

        Directory.CreateDirectory(root);
        return root;
    }

    private static string GetDefaultStorageBaseRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ElBruno",
            "PersonaPlex",
            "sessions");

    private static string NormalizeSessionId(string? sessionId)
    {
        var resolvedSessionId = string.IsNullOrWhiteSpace(sessionId)
            ? Guid.NewGuid().ToString("N")
            : sessionId;

        if (!SessionIdPattern.IsMatch(resolvedSessionId))
            throw new ArgumentException("SessionId may contain only letters, numbers, dashes, and underscores.", nameof(sessionId));

        return resolvedSessionId;
    }

    private string ResolveSessionPath(string path, bool mustExist)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var candidate = Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(_storageRoot, path));

        var root = EnsureTrailingSeparator(_storageRoot);
        var candidateWithSeparator = EnsureTrailingSeparator(candidate);
        if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(candidateWithSeparator, root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Audio files must stay within the session storage root.");
        }

        if (mustExist && !File.Exists(candidate))
            throw new FileNotFoundException("Audio file was not found in session storage.", candidate);

        return candidate;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
            return path;

        return path + Path.DirectorySeparatorChar;
    }

    private void DeleteOwnedStorageRoot()
    {
        if (_ownsStorageRoot && Directory.Exists(_storageRoot))
        {
            Directory.Delete(_storageRoot, recursive: true);
        }
    }

    private PersonaSessionStatus CreateStatus(PersonaSessionState state, string? detail = null, string? errorMessage = null) =>
        new()
        {
            State = state,
            IsModelReady = _pipeline.IsEncoderLoaded && _pipeline.IsDecoderLoaded,
            CreatedAt = _createdAt,
            UpdatedAt = DateTimeOffset.UtcNow,
            Detail = detail,
            ErrorMessage = errorMessage
        };

    private void UpdateStatus(PersonaSessionState state, string? detail = null, string? errorMessage = null)
    {
        _status = CreateStatus(state, detail, errorMessage);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
