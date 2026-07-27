using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Services.Job
{
	/// <summary>
	/// Batches dense job log notifications (~300ms or 50 items) before pushing to clients.
	/// </summary>
	public sealed class JobLogBroadcastBuffer : IHostedService, IDisposable
	{
		private const int FlushIntervalMs = 300;
		private const int MaxBatchSize = 50;

		private readonly IJobRealtimeNotifier _notifier;
		private readonly ILogger<JobLogBroadcastBuffer> _logger;
		private readonly ConcurrentQueue<JobLogNotification> _queue = new();
		private readonly SemaphoreSlim _flushLock = new(1, 1);
		private Timer? _timer;
		private int _pendingCount;

		public JobLogBroadcastBuffer(IJobRealtimeNotifier notifier, ILogger<JobLogBroadcastBuffer> logger)
		{
			_notifier = notifier;
			_logger = logger;
		}

		public void Enqueue(long historyId, int scheduleId, string level, string message, DateTime timestamp)
		{
			_queue.Enqueue(new JobLogNotification
			{
				HistoryId = historyId,
				ScheduleId = scheduleId,
				Level = level ?? "Info",
				Message = message ?? string.Empty,
				Timestamp = timestamp
			});

			if (Interlocked.Increment(ref _pendingCount) >= MaxBatchSize)
			{
				_ = FlushAsync();
			}
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_timer = new Timer(_ => _ = FlushAsync(), null, FlushIntervalMs, FlushIntervalMs);
			return Task.CompletedTask;
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			_timer?.Change(Timeout.Infinite, 0);
			await FlushAsync().ConfigureAwait(false);
		}

		private async Task FlushAsync()
		{
			if (!await _flushLock.WaitAsync(0).ConfigureAwait(false))
				return;

			try
			{
				var batch = new List<JobLogNotification>(MaxBatchSize);
				while (batch.Count < MaxBatchSize && _queue.TryDequeue(out var item))
				{
					batch.Add(item);
					Interlocked.Decrement(ref _pendingCount);
				}

				if (batch.Count == 0)
					return;

				try
				{
					await _notifier.NotifyLogsBatchAsync(batch).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Failed to broadcast {Count} job log notifications", batch.Count);
				}
			}
			finally
			{
				_flushLock.Release();
			}
		}

		public void Dispose()
		{
			_timer?.Dispose();
			_flushLock.Dispose();
		}
	}
}
