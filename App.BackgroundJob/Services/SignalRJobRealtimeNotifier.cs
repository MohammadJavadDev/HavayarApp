using App.BackgroundJob.Hubs;
using Microsoft.AspNetCore.SignalR;
using Services.Job;

namespace App.BackgroundJob.Services
{
	public sealed class SignalRJobRealtimeNotifier : IJobRealtimeNotifier
	{
		private readonly IHubContext<JobMonitorHub> _hub;

		public SignalRJobRealtimeNotifier(IHubContext<JobMonitorHub> hub)
		{
			_hub = hub;
		}

		public Task NotifyStatusChangedAsync(int scheduleId, string status, DateTime? lastRun, DateTime? nextRun, CancellationToken cancellationToken = default)
		{
			return _hub.Clients.Group(JobMonitorHub.StatusesGroup).SendAsync(
				"statusChanged",
				new
				{
					scheduleId,
					status,
					lastRun,
					nextRun
				},
				cancellationToken);
		}

		public Task NotifyLogAddedAsync(long historyId, int scheduleId, string level, string message, DateTime timestamp, CancellationToken cancellationToken = default)
		{
			return _hub.Clients.Group(JobMonitorHub.HistoryGroup(historyId)).SendAsync(
				"logAdded",
				new
				{
					historyId,
					scheduleId,
					level,
					message,
					timestamp
				},
				cancellationToken);
		}

		public async Task NotifyLogsBatchAsync(IReadOnlyList<JobLogNotification> logs, CancellationToken cancellationToken = default)
		{
			if (logs == null || logs.Count == 0)
				return;

			foreach (var group in logs.GroupBy(l => l.HistoryId))
			{
				var payload = group.Select(l => new
				{
					historyId = l.HistoryId,
					scheduleId = l.ScheduleId,
					level = l.Level,
					message = l.Message,
					timestamp = l.Timestamp
				}).ToList();

				await _hub.Clients.Group(JobMonitorHub.HistoryGroup(group.Key)).SendAsync(
					"logsBatch",
					payload,
					cancellationToken).ConfigureAwait(false);
			}
		}

		public Task NotifyHistoryCompletedAsync(long historyId, int scheduleId, bool success, DateTime endTime, CancellationToken cancellationToken = default)
		{
			var payload = new
			{
				historyId,
				scheduleId,
				success,
				endTime
			};

			return Task.WhenAll(
				_hub.Clients.Group(JobMonitorHub.HistoryGroup(historyId)).SendAsync("historyCompleted", payload, cancellationToken),
				_hub.Clients.Group(JobMonitorHub.StatusesGroup).SendAsync("historyCompleted", payload, cancellationToken));
		}
	}
}