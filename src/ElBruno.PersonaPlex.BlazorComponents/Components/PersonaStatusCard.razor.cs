using ElBruno.PersonaPlex.BlazorComponents.Services;
using ElBruno.PersonaPlex.Sessions;
using Microsoft.AspNetCore.Components;

namespace ElBruno.PersonaPlex.BlazorComponents.Components;

/// <summary>
/// Shows server-backed PersonaPlex session state, load status, and timing details.
/// </summary>
public partial class PersonaStatusCard
{
    private IPersonaSession? _attachedSession;
    private Timer? _timer;

    /// <summary>
    /// Gets or sets the current UI session. When omitted, the injected state service session is used.
    /// </summary>
    [Parameter]
    public IPersonaSession? Session { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether VRAM usage should be shown when available.
    /// </summary>
    [Parameter]
    public bool ShowVramUsage { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the session timer should be shown.
    /// </summary>
    [Parameter]
    public bool ShowSessionTimer { get; set; } = true;

    [Inject]
    private PersonaPlexStateService StateService { get; set; } = default!;

    private IPersonaSession? CurrentSession => Session ?? StateService.Session;

    /// <summary>
    /// Gets the card status CSS class.
    /// </summary>
    protected string StatusCssClass =>
        CurrentSession?.Status.State switch
        {
            PersonaSessionState.Ready => "ready",
            PersonaSessionState.ProcessingTurn => "processing",
            PersonaSessionState.Faulted => "faulted",
            PersonaSessionState.Initializing => "loading",
            _ => "idle"
        };

    /// <summary>
    /// Gets the card status text.
    /// </summary>
    protected string StatusText =>
        CurrentSession?.Status.State switch
        {
            PersonaSessionState.Initializing => "Loading ONNX sessions on the server.",
            PersonaSessionState.Ready => "Ready for the next turn.",
            PersonaSessionState.ProcessingTurn => "Processing a turn on the server.",
            PersonaSessionState.Faulted => "The last turn failed.",
            PersonaSessionState.Disposed => "Session disposed.",
            _ => "Waiting for initialization."
        };

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _timer = new Timer(_ => _ = InvokeAsync(StateHasChanged), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(_attachedSession, CurrentSession))
        {
            if (_attachedSession is not null)
            {
                _attachedSession.Changed -= HandleSessionChanged;
            }

            _attachedSession = CurrentSession;
            if (_attachedSession is not null)
            {
                _attachedSession.Changed += HandleSessionChanged;
            }
        }
    }

    /// <summary>
    /// Formats a session duration for display.
    /// </summary>
    protected static string FormatDuration(TimeSpan duration) =>
        duration == TimeSpan.Zero
            ? "00:00"
            : duration.TotalHours >= 1
                ? duration.ToString(@"hh\:mm\:ss")
                : duration.ToString(@"mm\:ss");

    private void HandleSessionChanged(object? sender, EventArgs e) => _ = InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_attachedSession is not null)
        {
            _attachedSession.Changed -= HandleSessionChanged;
        }

        _timer?.Dispose();
    }
}
