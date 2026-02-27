namespace Scenario04.Shared.Models;

/// <summary>
/// A chat message in the conversation history.
/// </summary>
public record ChatMessageDto
{
    public required string Role { get; init; }
    public required string Content { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public bool HasAudio { get; init; }
}
