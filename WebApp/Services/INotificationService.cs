using Aspose.Cells.Charts;
using Common.Utilities;
using Data.Contracts;
using Data.Repositories;
using Entities.Base;
using Entities.Base.Notification;
using Infrastructure.Messaging;
using Infrastructure.NotificationServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using Shared.Realtime.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApp.Hubs;

namespace Services.NotificationServices
{


	public interface INotificationService
	{
		Task SendAsync(Notification notification);
		Task SendAsync(Notification notification, long[] UserIds);
		Notification CreateAsync(Notification notification);
		Task<Notification> CreateAndSendAsync(Notification notification);
		Task CreateAndSendAsync(Notification notification, long[] UserIds);
		Task CreateAndSendToAllUsersAsync(Notification notification);
		Task SendToAllUsersAsync(Notification notification);
	}

	public class NotificationService(IUnitOfWork _unitOfWork, INotificationEventPublisher _eventPublisher, IUserService _userService) : INotificationService
	{

		public async Task SendAsync(Notification notification)
		{
			// ارسال نوتیف از طریق صف برای سرویس Real
			var evt = new NotificationEvent
			{
				Title = notification.Title,
				Body = notification.Body,
				UserIds = new[] { notification.OwnerId }
			};
			await _eventPublisher.PublishAsync(evt);
		}

		public async Task SendAsync(Notification notification, long[] UserIds)
		{
			// ارسال نوتیف از طریق صف برای سرویس Real

			var evt = new NotificationEvent
			{
				Title = notification.Title,
				Body = notification.Body,
				UserIds = UserIds
			};
			await _eventPublisher.PublishAsync(evt);
		}
		public Notification CreateAsync(Notification notification)
		{

			return _unitOfWork.Repository<Notification>().Add(notification);

		}
		public async Task<Notification> CreateAndSendAsync(Notification notification)
		{

			var result = _unitOfWork.Repository<Notification>().Add(notification);

			var evt = new NotificationEvent
			{
				Title = notification.Title,
				Body = notification.Body,
				Id = result.Id,
				ViewPath = result.ViewPath,
				UserIds = new[] { result.OwnerId }
			};
			await _eventPublisher.PublishAsync(evt);

			return result;

		}

		public async Task CreateAndSendAsync(Notification notification, long[] UserIds)
		{
			var listNotifications = UserIds.Select(c =>
			{
				notification.OwnerId = c;
				return notification;

			}).ToList();

			var result = _unitOfWork.Repository<Notification>().AddRange(listNotifications);

			foreach( var r in  result)
			{


				var evt = new NotificationEvent
				{
					Title = notification.Title,
					Body = notification.Body,
					Id = r.Id,
					ViewPath = r.ViewPath,
					UserIds = new[] { r.OwnerId }
				};
				  _eventPublisher.PublishAsync(evt);
			}
		


		}

		public async Task CreateAndSendToAllUsersAsync(Notification notification)
		{
			var allUsersIds = await _userService.TableNoTracking
			    .Where(c => c.IsActive == IsActiveEnum.Active)
			    .Select(c => c.Id)
			    .ToArrayAsync();

			var listNotifications = allUsersIds.Select(c =>
			{
				var newNotification = notification.Clone();
				newNotification.OwnerId = (long)c;
				return newNotification;
			}).ToList();

			var result = _unitOfWork.Repository<Notification>().AddRange(listNotifications);

			var evt = new NotificationEvent
			{
				Title = notification.Title,
				Body = notification.Body,
				UserIds = result.Select(x => x.OwnerId).ToArray()
			};
			await _eventPublisher.PublishAsync(evt);


		}

		public async Task SendToAllUsersAsync(Notification notification)
		{
			var allUsersIds = await _userService.TableNoTracking.Where(c => c.IsActive == IsActiveEnum.Active).Select(c => c.Id)
				.ToArrayAsync();

			var evt = new NotificationEvent
			{
				Title = notification.Title,
				Body = notification.Body,
				UserIds = allUsersIds.Select(x => (long)x).ToArray()
			};
			await _eventPublisher.PublishAsync(evt);
		}
	}
}
