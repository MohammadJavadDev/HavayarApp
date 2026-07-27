using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.Base.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using Services.NotificationServices;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Hubs;
using WebApp.ViewModels.NotificationViewModels;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.SystemControllers
{

	[ControllerInfoAttribute("اعلانات")]
	[ApiController]
	[ApiResultFilter]
	[Route("System/[controller]")]
	public class NotifyController(IHubContext<NotificationHub> _hubContext, IUnitOfWork _unitOfWork , INotificationService _notificationService) : BaseController
	{
	 
		[HttpPost("Send")]
		[Authorize("admin")]
		public async Task<IActionResult> SendMessage(string message)
		{
			await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
			return Ok(new { success = true });
		}

		[Authorize("AuthenticatedUser")]
		[HttpGet("/panel/{action}")]
		[ActionDisplayName("لیست اعلانات", ActionAccessType.View)]
		public IActionResult ListMyNotifications()
		{
			return View("Views/Panel/System/Notification/List.cshtml");
		}

		[Authorize("AuthenticatedUser")]
		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست اعلانات", ActionAccessType.Api)]
		public async Task<IActionResult> GetMyNotifications(int page = 1, int pageSize = 10, bool? isRead = null, CancellationToken cancellationToken = default)
		{
			if (User?.Identity == null)
				return Unauthorized();

			var userId = User.Identity.GetUserId();

			var query = _unitOfWork.Repository<Notification>()
				.TableNoTracking
				.Where(n => n.OwnerId == userId);

			// فیلتر بر اساس وضعیت خوانده شده
			if (isRead.HasValue)
			{
				query = query.Where(n => n.IsRead == isRead.Value);
			}

			// مرتب‌سازی بر اساس تاریخ ایجاد (جدیدترین‌ها اول)
			query = query.OrderByDescending(n => n.CreatedOnMiladiDateTime);

			// تعداد کل
			var total = await query.CountAsync(cancellationToken);

			var dt = _unitOfWork.Repository<Notification>()
				.TableNoTracking
				.Where(n => n.OwnerId == userId).ToList();

			// صفحه‌بندی
			var notifications = await query
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(n => new
				{
					n.Id,
					n.Title,
					n.Body,
					n.IsRead,
					n.ViewPath,
					n.CreatedOnShamsiDateTime,
					n.CreatedOnMiladiDateTime
				})
				.ToListAsync(cancellationToken);

			return Ok(new
			{
				notifications = notifications,
				total = total,
				page = page,
				pageSize = pageSize,
				hasMore = total > page * pageSize
			});
		}

		[Authorize("AuthenticatedUser")]
		[HttpPost("[action]")]
		[ActionDisplayName("علامت‌گذاری به عنوان خوانده شده", ActionAccessType.Api)]
		public async Task<IActionResult> MarkAsRead([FromBody] long id, CancellationToken cancellationToken)
		{
			if (User?.Identity == null)
				return Unauthorized();

			var userId = User.Identity.GetUserId();

			var notificationsQuery = _unitOfWork.Repository<Notification>()
			    .Table
			    .Where(n => n.Id == id && n.OwnerId == userId);

			 
			var updatedCount = await notificationsQuery.ExecuteUpdateAsync(
			    setters => setters.SetProperty(n => n.IsRead, true),
			    cancellationToken);

		 
			if (updatedCount == 0)
				return Ok();

			return Ok();
		}
		[Authorize("AuthenticatedUser")]
		[HttpPost("[action]")]
		[ActionDisplayName("علامت‌گذاری همه به عنوان خوانده شده", ActionAccessType.Api)]
		public async Task<IActionResult> MarkAllAsRead( CancellationToken cancellationToken)
		{
 			if (User?.Identity is null  || User.Identity.GetUserId() == 0)
				return Unauthorized();

			var userId = User.Identity.GetUserId();

			var notificationsQuery = _unitOfWork.Repository<Notification>()
			    .Table
			    .Where(n => n.OwnerId == userId);

		 
			if (!await notificationsQuery.AnyAsync(cancellationToken))
				return Ok();


			await notificationsQuery.ExecuteUpdateAsync(
			    setters => setters.SetProperty(n => n.IsRead, true),
			    cancellationToken);

			return Ok();
		}
		 

		[Authorize("AuthenticatedUser")]
		[HttpPost("[action]")]
		[ActionDisplayName("حذف اعلان", ActionAccessType.Api)]
		public async Task<IActionResult> Delete([FromBody] long id, CancellationToken cancellationToken)
		{
			if (User?.Identity == null)
				return Unauthorized();

			var userId = User.Identity.GetUserId();

			var notificationsQuery = _unitOfWork.Repository<Notification>()
			    .Table
			    .Where(n => n.Id == id && n.OwnerId == userId);

 			var deletedCount = await notificationsQuery.ExecuteDeleteAsync(cancellationToken);

 			if (deletedCount == 0)
				return NotFound(new { message = "اعلان یافت نشد" });

			return Ok();
		}

		[Authorize("AuthenticatedUser")]
		[HttpPost("[action]")]
		[ActionDisplayName("حذف تمامی اعلان ها", ActionAccessType.Api)]
		public async Task<IActionResult> DeleteAll(CancellationToken cancellationToken)
		{
			if (User?.Identity == null)
				return Unauthorized();

			var userId = User.Identity.GetUserId();
			var notificationRepo = _unitOfWork.Repository<Notification>();

 
			var notificationsQuery = notificationRepo.Table
			    .Where(n => n.OwnerId == userId);

 
			if (!await notificationsQuery.AnyAsync(cancellationToken))
				return Ok();

			var deletedCount = await notificationsQuery.ExecuteDeleteAsync(cancellationToken);

			return Ok();
		}

		

		[Authorize("admin")]
		[HttpGet("/panel/{action}")]
		[ActionDisplayName(" ارسال اعلان", ActionAccessType.View)]
		public IActionResult Send()
		{
			return View("Views/Panel/System/Notification/Send.cshtml");
		}


		[Authorize("admin")]
		[HttpPost("/panel/{action}")]
		[ActionDisplayName("ارسال اعلان", ActionAccessType.Api)]
		public async Task<IActionResult> SendNotification(SendNotificationViewModel model)
		{
			var notification = new Notification()
			{
				Body = model.Body,
				Title = model.Title,
			};
			if (model.SaveNotification)
			{
				if (model.AllUsers)
				{
					await _notificationService.CreateAndSendToAllUsersAsync(notification);
				}
				else
				{
					await _notificationService.CreateAndSendAsync(notification, model.OwnerIds);
				}
			}
			else
			{
				if (model.AllUsers)
				{
					await _notificationService.SendToAllUsersAsync(notification);
				}
				else
				{
					await _notificationService.SendAsync(notification, model.OwnerIds);
 
				}
			}


			return Ok(model);
		}

	}
}
