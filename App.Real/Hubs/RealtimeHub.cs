using Data.Contracts;
using Data.Repositories;
using Data.SystemAuth;
using Entities.Base.Notification;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace App.Real.Hubs;

public sealed class RealtimeHub : Hub
{
	private readonly IOnlineUserService _onlineUserService;
	private readonly IUnitOfWork _unitOfWork;

	public RealtimeHub(IOnlineUserService onlineUserService,IUnitOfWork unitOfWork)
	{
		_onlineUserService = onlineUserService;
		_unitOfWork = unitOfWork;
	}

	public override async Task OnConnectedAsync()
	{
 
		if (long.TryParse(Context.UserIdentifier, out var userId))
		{
		 
			await _onlineUserService.AddOnlineUserAsync(userId, Context.ConnectionId);

			 
			var notReadNotifications = await _unitOfWork
			    .Repository<Notification>()
			    .TableNoTracking
			    .Where(c => c.OwnerId == userId && !c.IsRead)
			    .OrderByDescending(c => c.CreatedOnMiladiDateTime)
			    .Select(c => new { c.Title, c.Body, c.CreatedOnShamsiDateTime, c.Id 
			    ,c.ViewPath})
			    .ToListAsync();

 
			await Clients.Caller.SendAsync("ReceiveUnreadNotification", notReadNotifications);
		}

		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		if (long.TryParse(Context.UserIdentifier, out var userId))
		{
		 
			await _onlineUserService.RemoveOnlineUserAsync(userId, Context.ConnectionId);
		}

		await base.OnDisconnectedAsync(exception);
	}
}




