using Data.SystemAuth;
using Microsoft.AspNetCore.SignalR;
 
namespace App.Real.Hubs;

public sealed class RealtimeHub : Hub
{
	private readonly IOnlineUserService _onlineUserService;

	public RealtimeHub(IOnlineUserService onlineUserService)
	{
		_onlineUserService = onlineUserService;
	}

	public override async Task OnConnectedAsync()
	{
		// Connection is already associated with user via IUserIdProvider
		if (long.TryParse(Context.UserIdentifier, out var userId))
		{
			// اضافه کردن کاربر به لیست آنلاین
			await _onlineUserService.AddOnlineUserAsync(userId, Context.ConnectionId);
		}

		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		if (long.TryParse(Context.UserIdentifier, out var userId))
		{
			// حذف کاربر از لیست آنلاین
			await _onlineUserService.RemoveOnlineUserAsync(userId, Context.ConnectionId);
		}

		await base.OnDisconnectedAsync(exception);
	}
}




