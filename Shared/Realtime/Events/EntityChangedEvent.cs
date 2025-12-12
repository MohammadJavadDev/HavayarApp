namespace Shared.Realtime.Events;

/// <summary>
/// Describes a CRUD change that occurred in the system.
/// </summary>
public sealed class EntityChangedEvent
{
    public required string Operation { get; init; } // Create | Update | Delete
    public required string EntityName { get; init; }
    public required string EntityId { get; init; }
    public required string ViewPath { get; init; }
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    public Dictionary<string, string>? Metadata { get; init; }
}




