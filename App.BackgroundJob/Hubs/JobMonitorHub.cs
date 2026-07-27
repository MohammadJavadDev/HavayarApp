using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace App.BackgroundJob.Hubs
{
	[Authorize(Roles = "admin")]
	public sealed class JobMonitorHub : Hub
	{
		public const string StatusesGroup = "statuses";

		public static string HistoryGroup(long historyId) => $"history:{historyId}";

		public Task JoinStatuses() => Groups.AddToGroupAsync(Context.ConnectionId, StatusesGroup);

		public Task LeaveStatuses() => Groups.RemoveFromGroupAsync(Context.ConnectionId, StatusesGroup);

		public Task JoinHistory(long historyId) =>
			Groups.AddToGroupAsync(Context.ConnectionId, HistoryGroup(historyId));

		public Task LeaveHistory(long historyId) =>
			Groups.RemoveFromGroupAsync(Context.ConnectionId, HistoryGroup(historyId));
	}
}
