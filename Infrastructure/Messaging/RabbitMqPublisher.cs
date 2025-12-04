using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Shared.Realtime.Options;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Messaging
{
	public interface IRabbitMqPublisher : IAsyncDisposable
	{
		/// <summary>
		/// Publishes a message to the configured exchange with the provided routingKey.
		/// This method will ensure a connection/channel exists (and try reconnect if necessary).
		/// </summary>
		Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default);

		/// <summary>
		/// Optional: call at startup to eagerly initialize connection/channel and fail fast if config wrong.
		/// </summary>
		Task InitializeAsync(CancellationToken cancellationToken = default);
	}

	public sealed class RabbitMqPublisher : IRabbitMqPublisher
	{
		private readonly RabbitMQOptions _options;
		private readonly ILogger<RabbitMqPublisher> _logger;
		private readonly ConnectionFactory _factory;

		private IConnection? _connection;
		private IChannel? _channel;

		// Protect connect/recreate with a semaphore
		private readonly SemaphoreSlim _sync = new(1, 1);
		private bool _disposed;

		public RabbitMqPublisher(IOptions<RabbitMQOptions> options, ILogger<RabbitMqPublisher> logger)
		{
			_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));

			_factory = new ConnectionFactory
			{
				HostName = _options.HostName,
				Port = _options.Port,
				UserName = _options.UserName,
				Password = _options.Password,
				VirtualHost = _options.VirtualHost,

				// Automatic recovery is disabled - we handle reconnection manually for better control
				AutomaticRecoveryEnabled = false,

				// Recommended settings for production
				RequestedHeartbeat = TimeSpan.FromSeconds(60),
				NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
			};
		}

		public async Task InitializeAsync(CancellationToken cancellationToken = default)
		{
			await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
		}

		private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
		{
			if (_connection is not null && _connection.IsOpen && _channel is not null && _channel.IsOpen)
				return;

			await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
			try
			{
				if (_connection is not null && _connection.IsOpen && _channel is not null && _channel.IsOpen)
					return;

				// dispose previous if any (best-effort)
				try
				{
					if (_channel is not null)
					{
						try { await _channel.CloseAsync().ConfigureAwait(false); } catch { }
						try { await _channel.DisposeAsync().ConfigureAwait(false); } catch { }
					}
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Error disposing previous RabbitMQ channel.");
				}

				try
				{
					if (_connection is not null)
					{
						try { await _connection.CloseAsync().ConfigureAwait(false); } catch { }
						try { _connection.Dispose(); } catch { }
					}
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Error disposing previous RabbitMQ connection.");
				}

				// Connect with retry/backoff (exponential backoff strategy)
				var attempts = 0;
				var maxAttempts = Math.Max(1, _options.ConnectRetryCount);
				Exception? lastEx = null;

				while (attempts < maxAttempts && !cancellationToken.IsCancellationRequested)
				{
					attempts++;
					try
					{
						_logger.LogInformation("Connecting to RabbitMQ (attempt {Attempt}/{Max})...", attempts, maxAttempts);

						// Create connection async (recommended in RabbitMQ.Client 7.x+)
						_connection = await _factory.CreateConnectionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

						// Create channel with CreateChannelOptions
						var createChannelOptions = new CreateChannelOptions(
							publisherConfirmationsEnabled: _options.EnablePublisherConfirms,
							publisherConfirmationTrackingEnabled: _options.EnablePublisherConfirmTracking
						);

						_channel = await _connection.CreateChannelAsync(createChannelOptions, cancellationToken).ConfigureAwait(false);

						// Register callback for unroutable messages (when mandatory=true)
						if (_options.HandleBasicReturns)
						{
							_channel.BasicReturnAsync += (sender, ea) =>
							{
								_logger.LogWarning(
									"Message returned from broker - unroutable. Exchange: {Exchange}, RoutingKey: {RoutingKey}, ReplyCode: {ReplyCode}, ReplyText: {ReplyText}",
									ea.Exchange, ea.RoutingKey, ea.ReplyCode, ea.ReplyText);
								return Task.CompletedTask;
							};
						}

						// declare exchange idempotently
						var exchangeType = string.IsNullOrWhiteSpace(_options.ExchangeType) ? ExchangeType.Topic : _options.ExchangeType;
						await _channel.ExchangeDeclareAsync(
							   exchange: _options.Exchange,
							   type: exchangeType,
							   durable: _options.ExchangeDurable,
							   autoDelete: _options.ExchangeAutoDelete,
							   arguments: null,
							   cancellationToken: cancellationToken
						).ConfigureAwait(false);

						// register connection close/shutdown log
						_connection.ConnectionShutdownAsync += (s, e) =>
						{
							_logger.LogWarning("RabbitMQ connection shutdown: {Reason}", e.ReplyText);
							// lazy reconnect will happen on next publish
							return Task.CompletedTask;
						};

						_logger.LogInformation("RabbitMQ connected and channel ready. Exchange: {Exchange}", _options.Exchange);
						return;
					}
					catch (Exception ex)
					{
						lastEx = ex;
						_logger.LogWarning(ex, "Failed to connect to RabbitMQ on attempt {Attempt}", attempts);
						// exponential backoff
						var delay = Math.Min(_options.ConnectRetryBaseDelayMs * (int)Math.Pow(2, attempts - 1), _options.ConnectRetryMaxDelayMs);
						try { await Task.Delay(delay, cancellationToken).ConfigureAwait(false); } catch (TaskCanceledException) { break; }
					}
				}

				throw new InvalidOperationException($"Could not connect to RabbitMQ after {attempts} attempts.", lastEx);
			}
			finally
			{
				_sync.Release();
			}
		}

		public async Task PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default)
		{
			if (_disposed)
				throw new ObjectDisposedException(nameof(RabbitMqPublisher));

			if (string.IsNullOrWhiteSpace(routingKey))
				throw new ArgumentNullException(nameof(routingKey));

			// Ensure connection/channel is available
			await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

			if (_channel is null || !_channel.IsOpen)
				throw new InvalidOperationException("RabbitMQ channel is not available.");

			// Serialize message to JSON bytes
			var body = JsonSerializer.SerializeToUtf8Bytes(message);

			// Create message properties (fixed from _channel.pr())
			var props = new BasicProperties
			{
				DeliveryMode = _options.PersistentMessages ? DeliveryModes.Persistent : DeliveryModes.Transient,
				ContentType = "application/json",
				ContentEncoding = "utf-8",
				Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
				MessageId = Guid.NewGuid().ToString()
			};

			try
			{
				// BasicPublishAsync awaits publisher confirmation when enabled
				// The cancellationToken can be used to set a timeout for the publish operation
				await _channel.BasicPublishAsync(
					exchange: _options.Exchange,
					routingKey: routingKey,
					mandatory: _options.MandatoryPublish,
					basicProperties: props,
					body: body,
					cancellationToken: cancellationToken
				).ConfigureAwait(false);

				_logger.LogDebug(
					"Message published successfully. Exchange: {Exchange}, RoutingKey: {RoutingKey}, MessageId: {MessageId}",
					_options.Exchange, routingKey, props.MessageId);
			}
			catch (PublishException pex)
			{
				// PublishException is thrown when the broker NACKs or returns the message
				_logger.LogError(pex,
					"Publish failed - broker rejected message. RoutingKey: {RoutingKey}, MessageId: {MessageId}",
					routingKey, props.MessageId);
				throw;
			}
			catch (AlreadyClosedException acex)
			{
				_logger.LogWarning(acex,
					"Connection/channel closed during publish. Attempting reconnect and retry. MessageId: {MessageId}",
					props.MessageId);

				// Try a single reconnect and retry
				try
				{
					await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

					if (_channel is null || !_channel.IsOpen)
						throw new InvalidOperationException("Failed to reconnect to RabbitMQ.");

					await _channel.BasicPublishAsync(
						_options.Exchange,
						routingKey,
						_options.MandatoryPublish,
						props,
						body,
						cancellationToken).ConfigureAwait(false);

					_logger.LogInformation(
						"Republish succeeded after reconnect. MessageId: {MessageId}",
						props.MessageId);
				}
				catch (Exception rex)
				{
					_logger.LogError(rex,
						"Republish failed after reconnect. MessageId: {MessageId}",
						props.MessageId);
					throw;
				}
			}
			catch (OperationCanceledException ocex)
			{
				_logger.LogWarning(ocex,
					"Publish operation cancelled (timeout or cancellation requested). MessageId: {MessageId}",
					props.MessageId);
				throw;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex,
					"Unexpected error while publishing message. MessageId: {MessageId}",
					props.MessageId);
				throw;
			}
		}

		public async ValueTask DisposeAsync()
		{
			if (_disposed)
				return;

			_disposed = true;

			// Gracefully close and dispose channel + connection
			try
			{
				// Close channel first
				if (_channel is not null)
				{
					try
					{
						if (_channel.IsOpen)
							await _channel.CloseAsync().ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Error closing RabbitMQ channel during disposal");
					}

					try
					{
						await _channel.DisposeAsync().ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Error disposing RabbitMQ channel");
					}

					_channel = null;
				}

				// Then close connection
				if (_connection is not null)
				{
					try
					{
						if (_connection.IsOpen)
							await _connection.CloseAsync().ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Error closing RabbitMQ connection during disposal");
					}

					try
					{
						_connection.Dispose();
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Error disposing RabbitMQ connection");
					}

					_connection = null;
				}

				_logger.LogInformation("RabbitMQ publisher disposed successfully");
			}
			finally
			{
				_sync.Dispose();
			}

			GC.SuppressFinalize(this);
		}
	}
}
