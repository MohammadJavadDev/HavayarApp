using Data.Contracts;
using Entities.Base.Notification;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace WebApp.Hubs
{
	public class NotificationHub(IUnitOfWork unitOfWork ) : Hub
	{
		public override async Task OnConnectedAsync()
		{

			var userId = Context.UserIdentifier; 

			if (long.TryParse(userId, out var ownerId))
			{
				// دریافت نوتیفیکیشن‌های نخوانده
				var notReadNotifications = await unitOfWork
				    .Repository<Notification>()
				    .TableNoTracking
				    .Where(c => c.OwnerId == ownerId && !c.IsRead)
				    .OrderByDescending(c=>c.CreatedOnMiladiDateTime)
				    .Select(c=>new { c.Title  , c.Body , c.CreatedOnShamsiDateTime , c.Id})
				    .ToListAsync();

				// ارسال لیست نوتیفیکیشن‌ها به خود کاربر
				await Clients.Caller.SendAsync("ReceiveUnreadNotification", notReadNotifications);
			}

			await base.OnConnectedAsync();
		}

		// ارسال اعلان به همه
		public async Task SendNotification(string message)
		{
			await Clients.All.SendAsync("ReceiveNotification", message);
		}

		// ارسال اعلان به کاربر خاص
		public async Task SendToUser(string userId, string message)
		{
			await Clients.User(userId).SendAsync("ReceiveNotification", message);
		}
	}
}
