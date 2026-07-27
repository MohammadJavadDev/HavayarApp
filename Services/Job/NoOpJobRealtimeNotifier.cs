namespace Services.Job
{
	/// <summary>
	/// Default no-op notifier so Services.Job has no SignalR dependency.
	/// </summary>
	public sealed class NoOpJobRealtimeNotifier : IJobRealtimeNotifier
	{
		public Task NotifyStatusChangedAsync(int scheduleId, string status, DateTime? lastRun, DateTime? nextRun, CancellationToken cancellationToken = default)
			=> Task.CompletedTask;

		public Task NotifyLogAddedAsync(long historyId, int scheduleId, string level, string message, DateTime timestamp, CancellationToken cancellationToken = default)
			=> Task.CompletedTask;

		public Task NotifyLogsBatchAsync(IReadOnlyList<JobLogNotification> logs, CancellationToken cancellationToken = default)
			=> Task.CompletedTask;

		public Task NotifyHistoryCompletedAsync(long historyId, int scheduleId, bool success, DateTime endTime, CancellationToken cancellationToken = default)
			=> Task.CompletedTask;
	}
}
