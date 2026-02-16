using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Realtime.Events;
using Shared.Realtime.Options;
using App.Real.Hubs;
using Common.Utilities;

namespace App.Real.Services;

public sealed class RabbitMqSubscriberHostedService : BackgroundService
{
    private readonly RabbitMQOptions _options;
    private readonly IHubContext<RealtimeHub> _hub;
    private readonly IEntityChangeNotificationHandler _entityHandler;
    private readonly ILogger<RabbitMqSubscriberHostedService> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqSubscriberHostedService(
        IOptions<RabbitMQOptions> options,
        IHubContext<RealtimeHub> hub,
        IEntityChangeNotificationHandler entityHandler,
        ILogger<RabbitMqSubscriberHostedService> logger)
    {
        _options = options.Value;
        _hub = hub;
        _entityHandler = entityHandler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMQ subscriber starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndConsumeAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _options.HostName +"111 Error in RabbitMQ subscriber. Reconnecting in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("RabbitMQ subscriber stopped.");
    }

    private async Task ConnectAndConsumeAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedHeartbeat = TimeSpan.FromSeconds(60)
        };

        _logger.LogInformation("Connecting to RabbitMQ at {Host}:{Port}...", _options.HostName, _options.Port);

        // RabbitMQ.Client 7.x: use async methods
        _connection = await factory.CreateConnectionAsync(cancellationToken: stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        _logger.LogInformation("Connected to RabbitMQ. Declaring exchange and queue...");

        // Declare exchange
        await _channel.ExchangeDeclareAsync(
            exchange: _options.Exchange,
            type: _options.ExchangeType,
            durable: _options.ExchangeDurable,
            autoDelete: _options.ExchangeAutoDelete,
            arguments: null,
            cancellationToken: stoppingToken
        );

        // Declare queue for notifications
        var notificationQueueName = "notification.queue";
        await _channel.QueueDeclareAsync(
            queue: notificationQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        // Bind queue to exchange
        await _channel.QueueBindAsync(
            queue: notificationQueueName,
            exchange: _options.Exchange,
            routingKey: "notification.send",
            arguments: null,
            cancellationToken: stoppingToken
        );

        _logger.LogInformation("Queue '{Queue}' bound to exchange '{Exchange}' with routing key 'notification.send'", notificationQueueName, _options.Exchange);

        // Declare queue for entity changes
        var entityQueueName = "entity.changed.queue";
        await _channel.QueueDeclareAsync(
            queue: entityQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        // Bind entity queue to exchange with wildcard routing
        await _channel.QueueBindAsync(
            queue: entityQueueName,
            exchange: _options.Exchange,
            routingKey: "entity.*", // entity.create, entity.update, entity.delete
            arguments: null,
            cancellationToken: stoppingToken
        );

        _logger.LogInformation("Queue '{Queue}' bound to exchange '{Exchange}' with routing key 'entity.*'", entityQueueName, _options.Exchange);

        // Set QoS
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

        // Create async consumer for notifications (RabbitMQ.Client 7.x)
        var notificationConsumer = new AsyncEventingBasicConsumer(_channel);
        notificationConsumer.ReceivedAsync += async (sender, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.Span);
                _logger.LogDebug("Received notification event: {Json}", json);

                var evt = JsonSerializer.Deserialize<NotificationEvent>(json);
                if (evt?.UserIds?.Length > 0)
                {
                    foreach (var userId in evt.UserIds)
                    {
                        await _hub.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
                        {
                            Title = evt.Title,
                            Body = evt.Body,
                            ViewPath = evt.ViewPath,
                            Id = evt.Id,
                            IsRead = false,
                            CreatedOnShamsiDateTime = DateTime.Now.ToShamsiDateTime(),
                        }, stoppingToken);

                        _logger.LogDebug("Notification sent to user {UserId}", userId);
                    }
                }

                // Acknowledge message
                await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification message. Delivery tag: {DeliveryTag}", ea.DeliveryTag);

                // Reject message and requeue
                try
                {
                    await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
                }
                catch (Exception nackEx)
                {
                    _logger.LogError(nackEx, "Error rejecting message");
                }
            }
        };

        // Create async consumer for entity changes
        var entityConsumer = new AsyncEventingBasicConsumer(_channel);
        entityConsumer.ReceivedAsync += async (sender, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.Span);
                _logger.LogInformation("Received entity change event: {Json}", json);

                var evt = JsonSerializer.Deserialize<EntityChangedEvent>(json);
                if (evt != null)
                {
                    _logger.LogInformation(
                        "Entity changed: Operation={Operation}, EntityName={EntityName}, EntityId={EntityId}",
                        evt.Operation, evt.EntityName, evt.EntityId);

                    // استفاده از handler برای پردازش و ارسال notifications
                    await _entityHandler.HandleAsync(evt, stoppingToken);
                }

                // Acknowledge message
                await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing entity change message. Delivery tag: {DeliveryTag}", ea.DeliveryTag);

                // Reject message and requeue
                try
                {
                    await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
                }
                catch (Exception nackEx)
                {
                    _logger.LogError(nackEx, "Error rejecting entity change message");
                }
            }
        };

        // Start consuming from notification queue
        await _channel.BasicConsumeAsync(
            queue: notificationQueueName,
            autoAck: false,
            consumer: notificationConsumer,
            cancellationToken: stoppingToken
        );

        _logger.LogInformation("RabbitMQ subscriber is now consuming messages from queue '{Queue}'", notificationQueueName);

        // Start consuming from entity changes queue
        await _channel.BasicConsumeAsync(
            queue: entityQueueName,
            autoAck: false,
            consumer: entityConsumer,
            cancellationToken: stoppingToken
        );

        _logger.LogInformation("RabbitMQ subscriber is now consuming entity changes from queue '{Queue}'", entityQueueName);

        // Keep alive until cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping RabbitMQ subscriber...");
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _logger.LogInformation("Disposing RabbitMQ subscriber resources...");

        try
        {
            if (_channel != null)
            {
                _channel.CloseAsync().GetAwaiter().GetResult();
                _channel.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing channel");
        }

        try
        {
            if (_connection != null)
            {
                _connection.CloseAsync().GetAwaiter().GetResult();
                _connection.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing connection");
        }

        base.Dispose();
    }
}



