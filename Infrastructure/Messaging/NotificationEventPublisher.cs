using Infrastructure.Messaging;
using Shared.Realtime.Events;
 

namespace Infrastructure.NotificationServices;

public interface INotificationEventPublisher
{
	Task PublishAsync(NotificationEvent evt, CancellationToken ct = default);
}
public sealed class NotificationEventPublisher(IRabbitMqPublisher publisher) : INotificationEventPublisher
{
    public Task PublishAsync(NotificationEvent evt, CancellationToken ct = default)
        => publisher.PublishAsync(evt, routingKey: "notification.send", ct);
}


