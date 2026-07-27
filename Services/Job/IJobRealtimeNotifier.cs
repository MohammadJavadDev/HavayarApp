namespace Services.Job
{
	/// <summary>
	/// Decouples job worker/logger from UI push transport (SignalR, etc.).
	/// </summary>
	public interface IJobRealtimeNotifier
	{
		Task NotifyStatusChangedAsync(int scheduleId, string status, DateTime? lastRun, DateTime? nextRun, CancellationToken cancellationToken = default);

		Task NotifyLogAddedAsync(long historyId, int scheduleId, string level, string message, DateTime timestamp, CancellationToken cancellationToken = default);

		Task NotifyLogsBatchAsync(IReadOnlyList<JobLogNotification> logs, CancellationToken cancellationToken = default);

		Task NotifyHistoryCompletedAsync(long historyId, int scheduleId, bool success, DateTime endTime, CancellationToken cancellationToken = default);
	}

	public sealed class JobLogNotification
	{
		public long HistoryId { get; init; }
		public int ScheduleId { get; init; }
		public string Level { get; init; } = "Info";
		public string Message { get; init; } = string.Empty;
		public DateTime Timestamp { get; init; }
	}
}
