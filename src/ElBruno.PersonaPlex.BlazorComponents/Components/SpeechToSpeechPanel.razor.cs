using ElBruno.PersonaPlex.BlazorComponents.Services;
using ElBruno.PersonaPlex.Sessions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Security.Cryptography;
using System.Text;

namespace ElBruno.PersonaPlex.BlazorComponents.Components;

/// <summary>
/// Primary turn-based speech-to-speech panel for server-backed PersonaPlex apps.
/// </summary>
public partial class SpeechToSpeechPanel
{
    private readonly string _noteId = $"personaplex-note-{Guid.NewGuid():N}";
    private readonly Dictionary<string, string> _suppliedSessionAudioSources = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _ownedSuppliedSessionStorageRoots = new(StringComparer.OrdinalIgnoreCase);
    private IPersonaSession? _attachedSession;
    private IJSObjectReference? _module;
    private long _recordingGeneration;
    private IPersonaSession? _recordingSession;
    private string? _turnNote;
    private string? _latestResponseAudioUrl;

    /// <summary>
    /// Gets or sets the current UI session. When omitted, the injected state service session is used.
    /// </summary>
    [Parameter]
    public IPersonaSession? Session { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to render the persona avatar.
    /// </summary>
    [Parameter]
    public bool ShowAvatar { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the record button behaves as push-to-talk.
    /// </summary>
    [Parameter]
    public bool PushToTalk { get; set; }

    /// <summary>
    /// Raised when a turn completes successfully.
    /// </summary>
    [Parameter]
    public EventCallback<ConversationTurn> OnTurnCompleted { get; set; }

    [Inject]
    private PersonaPlexStateService StateService { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Gets a value indicating whether microphone capture is currently active.
    /// </summary>
    protected bool IsRecording { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the panel is loading or processing a turn.
    /// </summary>
    protected bool IsBusy => CurrentSession?.Status.IsBusy ?? StateService.Status?.IsBusy ?? false;

    /// <summary>
    /// Gets a value indicating whether the current session can be cleared safely.
    /// </summary>
    protected bool CanResetSession =>
        Session is null &&
        !IsBusy &&
        !IsRecording &&
        (CurrentSession?.Turns.Count ?? StateService.Turns.Count) > 0;

    /// <summary>
    /// Gets the label for the active turn owner.
    /// </summary>
    protected string CurrentTurnLabel =>
        IsRecording
            ? "User"
            : CurrentSession?.Status.State == PersonaSessionState.ProcessingTurn ? "AI" : "Ready";

    protected string? CurrentErrorMessage => CurrentSession?.Status.ErrorMessage ?? StateService.ErrorMessage;

    private IPersonaSession? CurrentSession => Session ?? StateService.Session;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(_attachedSession, CurrentSession))
        {
            var shouldCancelRecording = InvalidateRecording();
            if (shouldCancelRecording && _module is not null)
            {
                _ = InvokeAsync(EndMicrophoneCaptureAsync);
            }

            if (_attachedSession is not null)
            {
                _attachedSession.Changed -= HandleSessionChanged;
            }

            _attachedSession = CurrentSession;
            _latestResponseAudioUrl = null;
            ResetSuppliedSessionState();

            if (_attachedSession is not null)
            {
                _attachedSession.Changed += HandleSessionChanged;
            }
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _module = await JSRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            "./_content/ElBruno.PersonaPlex.BlazorComponents/personaPlexBlazorComponents.js");

        if (Session is null)
        {
            await StateService.EnsureReadyAsync();
            if (!ReferenceEquals(_attachedSession, CurrentSession))
            {
                OnParametersSet();
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task ToggleRecordingAsync()
    {
        if (IsRecording)
        {
            await StopRecordingAsync();
            return;
        }

        await StartRecordingAsync();
    }

    private async Task StartRecordingAsync()
    {
        if (IsRecording || IsBusy)
        {
            return;
        }

        _module ??= await JSRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            "./_content/ElBruno.PersonaPlex.BlazorComponents/personaPlexBlazorComponents.js");

        await _module.InvokeVoidAsync("startRecording");
        _recordingGeneration++;
        _recordingSession = CurrentSession;
        IsRecording = true;
        await InvokeAsync(StateHasChanged);
    }

    private async Task StopRecordingAsync()
    {
        if (!IsRecording || _module is null)
        {
            return;
        }

        var recordingGeneration = _recordingGeneration;
        var recordingSession = _recordingSession;
        IsRecording = false;
        await InvokeAsync(StateHasChanged);

        var dataUrl = await _module.InvokeAsync<string>("stopRecording");
        if (recordingGeneration != _recordingGeneration ||
            !ReferenceEquals(recordingSession, CurrentSession) ||
            string.IsNullOrWhiteSpace(dataUrl))
        {
            _recordingSession = null;
            return;
        }

        var turn = Session is null
            ? await StateService.SubmitTurnAsync(dataUrl, _turnNote)
            : await SubmitSuppliedSessionTurnAsync(recordingSession!, dataUrl, _turnNote);

        _latestResponseAudioUrl = Session is null
            ? StateService.GetAudioSource(turn.OutputAudio)
            : await GetSuppliedSessionAudioSourceAsync(turn.OutputAudio);

        if (OnTurnCompleted.HasDelegate)
        {
            await OnTurnCompleted.InvokeAsync(turn);
        }

        _turnNote = null;
        _recordingSession = null;
        await InvokeAsync(StateHasChanged);
    }

    private Task HandlePointerLeaveAsync() => PushToTalk && IsRecording ? StopRecordingAsync() : Task.CompletedTask;

    private async Task ResetSessionAsync()
    {
        await CancelRecordingAsync();

        if (Session is null)
        {
            _turnNote = null;
            _latestResponseAudioUrl = null;
            await StateService.ResetSessionAsync();
            await StateService.EnsureReadyAsync();
            OnParametersSet();
            await InvokeAsync(StateHasChanged);
            return;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task CancelRecordingAsync()
    {
        var shouldEndCapture = InvalidateRecording();
        if (!shouldEndCapture && _module is null)
        {
            return;
        }

        if (_module is null)
        {
            await InvokeAsync(StateHasChanged);
            return;
        }

        await InvokeAsync(StateHasChanged);

        try
        {
            await EndMicrophoneCaptureAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private void HandleSessionChanged(object? sender, EventArgs e) => _ = InvokeAsync(StateHasChanged);

    private async Task<ConversationTurn> SubmitSuppliedSessionTurnAsync(
        IPersonaSession session,
        string audioDataUrl,
        string? userTranscript,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audioDataUrl);

        var (audioBytes, mediaType) = DecodeDataUrl(audioDataUrl);
        var storageRoot = GetSuppliedSessionStorageRoot(session);
        var turnId = Guid.NewGuid().ToString("N");
        var inputPath = Path.Combine(storageRoot, $"input-{turnId}.wav");
        var outputPath = Path.Combine(storageRoot, $"output-{turnId}.wav");

        Directory.CreateDirectory(storageRoot);
        await File.WriteAllBytesAsync(inputPath, audioBytes, cancellationToken);

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
                TextPrompt = null
            },
            cancellationToken);

        _suppliedSessionAudioSources[Path.GetFullPath(inputPath)] = audioDataUrl;
        return turn;
    }

    private async Task<string?> GetSuppliedSessionAudioSourceAsync(PersonaAudioReference? audioReference, CancellationToken cancellationToken = default)
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

        var fullPath = Path.GetFullPath(localFilePath);
        if (_suppliedSessionAudioSources.TryGetValue(fullPath, out var source))
        {
            return source;
        }

        if (!File.Exists(fullPath))
        {
            return null;
        }

        source = $"data:{audioReference?.MediaType ?? "audio/wav"};base64,{Convert.ToBase64String(await File.ReadAllBytesAsync(fullPath, cancellationToken))}";
        _suppliedSessionAudioSources[fullPath] = source;
        return source;
    }

    private string GetSuppliedSessionStorageRoot(IPersonaSession session)
    {
        if (session is IFileBackedPersonaSession fileBackedSession)
        {
            return fileBackedSession.StorageRoot;
        }

        var externalSessionsRoot = Path.GetFullPath(Path.Combine(StateService.WorkingDirectory, "external-sessions"));
        var storageRoot = Path.GetFullPath(Path.Combine(
            externalSessionsRoot,
            GetSafeSuppliedSessionDirectoryName(session.SessionId)));

        EnsurePathStaysUnderRoot(storageRoot, externalSessionsRoot);
        _ownedSuppliedSessionStorageRoots.Add(storageRoot);
        return storageRoot;
    }

    private void ResetSuppliedSessionState()
    {
        _suppliedSessionAudioSources.Clear();
        foreach (var storageRoot in _ownedSuppliedSessionStorageRoots)
        {
            DeleteOwnedDirectory(storageRoot);
        }

        _ownedSuppliedSessionStorageRoots.Clear();
    }

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

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        InvalidateRecording();

        if (_attachedSession is not null)
        {
            _attachedSession.Changed -= HandleSessionChanged;
        }

        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("endMicrophoneCapture");
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        ResetSuppliedSessionState();
    }

    private bool InvalidateRecording()
    {
        var shouldEndCapture = IsRecording;
        _recordingGeneration++;
        _recordingSession = null;
        IsRecording = false;
        return shouldEndCapture;
    }

    private static string GetSafeSuppliedSessionDirectoryName(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return "session";
        }

        var builder = new StringBuilder(sessionId.Length);
        foreach (var character in sessionId)
        {
            builder.Append(char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '_');
        }

        var sanitized = builder.ToString().Trim('_');
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "session";
        }

        if (string.Equals(sanitized, sessionId, StringComparison.Ordinal))
        {
            return sanitized;
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sessionId)))[..12];
        return $"{sanitized}-{hash}";
    }

    private static void EnsurePathStaysUnderRoot(string candidatePath, string rootPath)
    {
        var normalizedCandidate = EnsureTrailingSeparator(Path.GetFullPath(candidatePath));
        var normalizedRoot = EnsureTrailingSeparator(Path.GetFullPath(rootPath));
        if (!normalizedCandidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Supplied-session audio files must stay within the external session storage root.");
        }
    }

    private static void DeleteOwnedDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private async Task EndMicrophoneCaptureAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("endMicrophoneCapture");
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
}
