namespace Shared.Realtime.Events;

/// <summary>
/// Event to deliver a notification to users.
/// </summary>
public sealed class NotificationEvent
{
    public required string Title { get; init; }
    public required string Body { get; init; }
    public long[]? UserIds { get; init; }

    /// <summary>
    /// Optional JSON criteria for dynamic recipients (evaluated by App.Real or WebApp endpoint).
    /// </summary>
    public string? RecipientsCriteriaJson { get; init; }

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}




