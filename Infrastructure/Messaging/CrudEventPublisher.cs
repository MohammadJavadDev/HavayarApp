using Shared.Realtime.Events;

namespace Infrastructure.Messaging;

public interface ICrudEventPublisher
{
    Task PublishEntityChangedAsync(string operation, string entityName, string entityId, Dictionary<string, string>? metadata = null, CancellationToken ct = default);
}

public sealed class CrudEventPublisher(IRabbitMqPublisher publisher) : ICrudEventPublisher
{
    public Task PublishEntityChangedAsync(string operation, string entityName, string entityId, Dictionary<string, string>? metadata = null, CancellationToken ct = default)
    {
        var evt = new EntityChangedEvent
        {
            Operation = operation,
            EntityName = entityName,
            EntityId = entityId,
            Metadata = metadata
        };
        return publisher.PublishAsync(evt, routingKey: $"entity.{operation.ToLowerInvariant()}", ct);
    }
}


