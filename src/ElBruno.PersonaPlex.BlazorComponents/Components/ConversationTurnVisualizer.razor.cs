using ElBruno.PersonaPlex.BlazorComponents.Services;
using ElBruno.PersonaPlex.Sessions;
using Microsoft.AspNetCore.Components;

namespace ElBruno.PersonaPlex.BlazorComponents.Components;

/// <summary>
/// Visualizes recent conversation turns with audio playback and timing metadata.
/// </summary>
public partial class ConversationTurnVisualizer
{
    /// <summary>
    /// Gets or sets the turns to display. Falls back to the injected state service when omitted.
    /// </summary>
    [Parameter]
    public IReadOnlyList<ConversationTurn>? Turns { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether text metadata should be shown.
    /// </summary>
    [Parameter]
    public bool ShowTranscript { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of turns to display.
    /// </summary>
    [Parameter]
    public int MaxTurns { get; set; } = 50;

    [Inject]
    private PersonaPlexStateService StateService { get; set; } = default!;

    /// <summary>
    /// Gets the normalized set of turns rendered by the component.
    /// </summary>
    protected IReadOnlyList<ConversationTurn> VisibleTurns =>
        GetVisibleTurns();

    /// <summary>
    /// Formats a metric value in milliseconds.
    /// </summary>
    protected static string FormatMetric(double? milliseconds) =>
        milliseconds is null ? "—" : $"{milliseconds:F0} ms";

    /// <summary>
    /// Resolves a browser audio source for a turn audio reference.
    /// </summary>
    protected string? GetAudioSource(PersonaAudioReference? audioReference) => StateService.GetAudioSource(audioReference);

    private IReadOnlyList<ConversationTurn> GetVisibleTurns()
    {
        var turns = Turns ?? StateService.Turns;
        var maxTurns = Math.Max(1, MaxTurns);

        return turns.Count <= maxTurns
            ? turns.ToArray()
            : turns.Skip(turns.Count - maxTurns).ToArray();
    }
}
