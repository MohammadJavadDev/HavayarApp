using App.Real.Hubs;
using App.Real.Services;
using Common.Utilities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Realtime.Events;
using Shared.Realtime.Options;
using System.Text;
using System.Text.Json;

public sealed class RabbitMqSubscriberHostedService : BackgroundService
{
	private readonly RabbitMQOptions _options;
	private readonly IHubContext<RealtimeHub> _hub;
	private readonly IEntityChangeNotificationHandler _entityHandler;
	private readonly ILogger<RabbitMqSubscriberHostedService> _logger;

	private IConnection? _connection;
	private IChannel? _channel;

	private const string NotificationQueueName = "notification.queue";
	private const string NotificationRoutingKey = "notification.send";
	private const string EntityQueueName = "entity.changed.queue";
	private const string EntityRoutingKey = "entity.*";

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
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				_logger.LogInformation("RabbitMQ subscriber: shutdown requested, exiting loop.");
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex,
				    "[DIAGNOSTIC] Unhandled error in ConnectAndConsumeAsync. Type={ExType} Message={ExMsg}",
				    ex.GetType().FullName, ex.Message);

				await CleanupConnectionAsync();

				try
				{
					await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
				}
				catch (OperationCanceledException)
				{
					break;
				}
			}
		}

		_logger.LogInformation("RabbitMQ subscriber stopped.");
	}

	private async Task ConnectAndConsumeAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("[DIAGNOSTIC] Creating ConnectionFactory...");

		var factory = new ConnectionFactory
		{
			HostName = _options.HostName,
			Port = _options.Port,
			UserName = _options.UserName,
			Password = _options.Password,
			VirtualHost = _options.VirtualHost,
		 
			AutomaticRecoveryEnabled = false,
			NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
			RequestedHeartbeat = TimeSpan.FromSeconds(60)
		};

		_logger.LogInformation(
		    "[DIAGNOSTIC] Connecting → Host={Host} Port={Port} VHost={VHost} User={User}",
		    _options.HostName, _options.Port, _options.VirtualHost, _options.UserName);

		_connection = await factory.CreateConnectionAsync(cancellationToken: stoppingToken);

		_logger.LogInformation("[DIAGNOSTIC] Connection created. IsOpen={IsOpen}", _connection.IsOpen);

		// رویداد بستن connection را گوش بده
		_connection.ConnectionShutdownAsync += (sender, args) =>
		{
			_logger.LogWarning(
			    "[DIAGNOSTIC] Connection shutdown! Initiator={Initiator} ReplyCode={ReplyCode} ReplyText={ReplyText} Cause={Cause}",
			    args.Initiator, args.ReplyCode, args.ReplyText, args.Cause);
			return Task.CompletedTask;
		};

		_channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

		_logger.LogInformation("[DIAGNOSTIC] Channel created. IsOpen={IsOpen}", _channel.IsOpen);

		// رویداد بستن channel را گوش بده
		_channel.ChannelShutdownAsync += (sender, args) =>
		{
			_logger.LogWarning(
			    "[DIAGNOSTIC] Channel shutdown! Initiator={Initiator} ReplyCode={ReplyCode} ReplyText={ReplyText} Cause={Cause}",
			    args.Initiator, args.ReplyCode, args.ReplyText, args.Cause);
			return Task.CompletedTask;
		};

		_logger.LogInformation("[DIAGNOSTIC] Declaring topology...");
		await DeclareTopologyAsync(stoppingToken);

		_logger.LogInformation("[DIAGNOSTIC] Setting QoS...");
		await _channel.BasicQosAsync(
		    prefetchSize: 0,
		    prefetchCount: 10,
		    global: false,
		    cancellationToken: stoppingToken);

		_logger.LogInformation("[DIAGNOSTIC] Registering consumers...");
		await RegisterConsumersAsync(stoppingToken);

		_logger.LogInformation("[DIAGNOSTIC] All setup done. Waiting for cancellation...");

		var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		await using var reg = stoppingToken.Register(() =>
		{
			_logger.LogInformation("[DIAGNOSTIC] CancellationToken fired → completing TCS.");
			tcs.TrySetResult();
		});
		await tcs.Task;

		_logger.LogInformation("[DIAGNOSTIC] TCS completed. ConnectAndConsumeAsync exiting.");
	}

	private async Task DeclareTopologyAsync(CancellationToken ct)
	{
		_logger.LogInformation("[DIAGNOSTIC] DeclareTopologyAsync → Exchange={Exchange} Type={Type}",
		    _options.Exchange, _options.ExchangeType);

		await _channel!.ExchangeDeclareAsync(
		    exchange: _options.Exchange,
		    type: _options.ExchangeType,
		    durable: _options.ExchangeDurable,
		    autoDelete: _options.ExchangeAutoDelete,
		    arguments: null,
		    cancellationToken: ct);

		_logger.LogInformation("[DIAGNOSTIC] Exchange declared. Declaring notification queue...");

		await _channel.QueueDeclareAsync(
		    queue: NotificationQueueName,
		    durable: true,
		    exclusive: false,
		    autoDelete: false,
		    arguments: null,
		    cancellationToken: ct);

		await _channel.QueueBindAsync(
		    queue: NotificationQueueName,
		    exchange: _options.Exchange,
		    routingKey: NotificationRoutingKey,
		    arguments: null,
		    cancellationToken: ct);

		_logger.LogInformation("[DIAGNOSTIC] Notification queue declared and bound.");

		await _channel.QueueDeclareAsync(
		    queue: EntityQueueName,
		    durable: true,
		    exclusive: false,
		    autoDelete: false,
		    arguments: null,
		    cancellationToken: ct);

		await _channel.QueueBindAsync(
		    queue: EntityQueueName,
		    exchange: _options.Exchange,
		    routingKey: EntityRoutingKey,
		    arguments: null,
		    cancellationToken: ct);

		_logger.LogInformation("[DIAGNOSTIC] Entity queue declared and bound.");
	}

	private async Task RegisterConsumersAsync(CancellationToken ct)
	{
		var notificationConsumer = new AsyncEventingBasicConsumer(_channel!);
		notificationConsumer.ReceivedAsync += OnNotificationReceivedAsync;

		var entityConsumer = new AsyncEventingBasicConsumer(_channel!);
		entityConsumer.ReceivedAsync += OnEntityChangedReceivedAsync;

		await _channel!.BasicConsumeAsync(
		    queue: NotificationQueueName,
		    autoAck: false,
		    consumer: notificationConsumer,
		    cancellationToken: ct);

		_logger.LogInformation("[DIAGNOSTIC] Consuming from '{Queue}'", NotificationQueueName);

		await _channel.BasicConsumeAsync(
		    queue: EntityQueueName,
		    autoAck: false,
		    consumer: entityConsumer,
		    cancellationToken: ct);

		_logger.LogInformation("[DIAGNOSTIC] Consuming from '{Queue}'", EntityQueueName);
	}

	private async Task OnNotificationReceivedAsync(object sender, BasicDeliverEventArgs ea)
	{
		_logger.LogDebug("[DIAGNOSTIC] OnNotificationReceivedAsync called. DeliveryTag={Tag}", ea.DeliveryTag);
		try
		{
			var json = Encoding.UTF8.GetString(ea.Body.Span);
			_logger.LogDebug("Received notification event: {Json}", json);

			var evt = JsonSerializer.Deserialize<NotificationEvent>(json);
			if (evt?.UserIds?.Length > 0)
			{
				foreach (var userId in evt.UserIds)
				{
					await _hub.Clients
					    .User(userId.ToString())
					    .SendAsync("ReceiveNotification", new
					    {
						    Title = evt.Title,
						    Body = evt.Body,
						    ViewPath = evt.ViewPath,
						    Id = evt.Id,
						    IsRead = false,
						    CreatedOnShamsiDateTime = DateTime.Now.ToShamsiDateTime(),
					    }, CancellationToken.None);

					_logger.LogDebug("Notification sent to user {UserId}", userId);
				}
			}

			await AckSafeAsync(ea.DeliveryTag);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex,
			    "[DIAGNOSTIC] Error in OnNotificationReceivedAsync. Type={ExType}", ex.GetType().FullName);
			await NackSafeAsync(ea.DeliveryTag, requeue: true);
		}
	}

	private async Task OnEntityChangedReceivedAsync(object sender, BasicDeliverEventArgs ea)
	{
		_logger.LogDebug("[DIAGNOSTIC] OnEntityChangedReceivedAsync called. DeliveryTag={Tag}", ea.DeliveryTag);
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

				await _entityHandler.HandleAsync(evt, CancellationToken.None);
			}

			await AckSafeAsync(ea.DeliveryTag);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex,
			    "[DIAGNOSTIC] Error in OnEntityChangedReceivedAsync. Type={ExType}", ex.GetType().FullName);
			await NackSafeAsync(ea.DeliveryTag, requeue: true);
		}
	}

	private async Task AckSafeAsync(ulong deliveryTag)
	{
		try
		{
			if (_channel is { IsOpen: true })
			{
				_logger.LogDebug("[DIAGNOSTIC] Acking DeliveryTag={Tag}", deliveryTag);
				await _channel.BasicAckAsync(
				    deliveryTag: deliveryTag,
				    multiple: false,
				    cancellationToken: CancellationToken.None);
			}
			else
			{
				_logger.LogWarning("[DIAGNOSTIC] AckSafe skipped — channel closed. DeliveryTag={Tag}", deliveryTag);
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "[DIAGNOSTIC] AckSafe exception. DeliveryTag={Tag}", deliveryTag);
		}
	}

	private async Task NackSafeAsync(ulong deliveryTag, bool requeue)
	{
		try
		{
			if (_channel is { IsOpen: true })
			{
				_logger.LogDebug("[DIAGNOSTIC] Nacking DeliveryTag={Tag} Requeue={Requeue}", deliveryTag, requeue);
				await _channel.BasicNackAsync(
				    deliveryTag: deliveryTag,
				    multiple: false,
				    requeue: requeue,
				    cancellationToken: CancellationToken.None);
			}
			else
			{
				_logger.LogWarning("[DIAGNOSTIC] NackSafe skipped — channel closed. DeliveryTag={Tag}", deliveryTag);
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "[DIAGNOSTIC] NackSafe exception. DeliveryTag={Tag}", deliveryTag);
		}
	}

	private async Task CleanupConnectionAsync()
	{
		_logger.LogInformation("[DIAGNOSTIC] CleanupConnectionAsync starting...");

		if (_channel != null)
		{
			try
			{
				if (_channel.IsOpen)
				{
					_logger.LogInformation("[DIAGNOSTIC] Closing channel...");
					await _channel.CloseAsync();
				}
				await _channel.DisposeAsync();
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "[DIAGNOSTIC] Error closing channel.");
			}
			finally { _channel = null; }
		}

		if (_connection != null)
		{
			try
			{
				if (_connection.IsOpen)
				{
					_logger.LogInformation("[DIAGNOSTIC] Closing connection...");
					await _connection.CloseAsync();
				}
				await _connection.DisposeAsync();
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "[DIAGNOSTIC] Error closing connection.");
			}
			finally { _connection = null; }
		}

		_logger.LogInformation("[DIAGNOSTIC] CleanupConnectionAsync done.");
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("[DIAGNOSTIC] StopAsync called.");
		await base.StopAsync(cancellationToken);
		await CleanupConnectionAsync();
	}

	public override void Dispose()
	{
		_logger.LogInformation("[DIAGNOSTIC] Dispose called.");
		CleanupConnectionAsync().GetAwaiter().GetResult();
		base.Dispose();
	}
}
