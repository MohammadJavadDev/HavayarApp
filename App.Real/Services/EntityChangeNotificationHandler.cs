using App.Real.Hubs;
using Azure;
using Azure.Core;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Data.Repositories;
using Entities.Base.Notification;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Services.AccessServices;
using Services.NotifitactionBuilderServices;
using Shared.Realtime.Events;
using System.Text.Json;

namespace App.Real.Services;

/// <summary>
/// سرویس برای پردازش رویدادهای تغییر موجودیت و ارسال notification به کاربران مرتبط
/// </summary>
public interface IEntityChangeNotificationHandler
{
    Task HandleAsync(EntityChangedEvent evt, CancellationToken ct = default);
}

public sealed class EntityChangeNotificationHandler : IEntityChangeNotificationHandler
{
    private readonly IHubContext<RealtimeHub> _hub;
    private readonly ILogger<EntityChangeNotificationHandler> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly string _webAppBaseUrl;
    private readonly IAccessMemoryStorage _accessMemoryStorage;

	public EntityChangeNotificationHandler(
        IHubContext<RealtimeHub> hub,
        ILogger<EntityChangeNotificationHandler> logger,
        IConfiguration configuration,
	   IServiceScopeFactory serviceScopeFactory,
	   IAccessMemoryStorage accessMemoryStorage


	  )
    {
          _hub = hub;
          _logger = logger;
       
		 _webAppBaseUrl = configuration["WebAppBaseUrl"] ?? "https://localhost:7073";
          _serviceScopeFactory = serviceScopeFactory;
          _accessMemoryStorage = accessMemoryStorage;


    }

    public async Task HandleAsync(EntityChangedEvent evt, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Processing entity change: {Operation} on {EntityName} with Id={EntityId}",
            evt.Operation, evt.EntityName, evt.EntityId);

        try
        {
            // فراخوانی WebApp API برای evaluate کردن notification rules
            var recipients = await EvaluateRulesViaWebAppAsync(evt, ct);

            if (recipients.Count == 0)
            {
                _logger.LogDebug("No users matched notification rules for {EntityName} #{EntityId}",
                    evt.EntityName, evt.EntityId);
                return;
            }
			using var scope = _serviceScopeFactory.CreateScope();

			var _unitofWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

			_logger.LogInformation("Found {Count} recipients for {EntityName} #{EntityId} notification",
                recipients.Count, evt.EntityName, evt.EntityId);

               var viewPath = "";
               var controller = _accessMemoryStorage.GetAccessControllerByRedis(evt.EntityName);

               if(controller != null)
               {
                    var actionEntity = controller.
                         Actions.FirstOrDefault(
                         c=>c.ActionAccessItemType == ActionAccessItemType.Update
                         &&
                         c.ActionAccessType == ActionAccessType.View
                         );

                    if (actionEntity != null)
                    {
                         viewPath = actionEntity.Path+"?id="+evt.EntityId;

				}
			}

		  // ارسال notification به هر کاربر
		  foreach (var recipient in recipients)
            {
                    var newNoti = new Notification()
                    {
                         Body = recipient.Body,
                         Title = recipient.MessageTitle,
                         IsRead = false,
                         EntityId = evt.EntityId.ToLong(),
                         OwnerId = recipient.UserId,
                         ViewPath = viewPath,

				};
			 await _unitofWork.Repository<Notification>().AddAsync(newNoti,ct);

			 await _hub.Clients.User(recipient.UserId.ToString())
                    .SendAsync("ReceiveNotification", new
                    {
                        Title = recipient.MessageTitle,
                        Body = recipient.Body,
                        IsRead = false,
                        CreatedOnShamsiDateTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                        EntityName = evt.EntityName,
                        EntityId = evt.EntityId,
                        Operation = evt.Operation,
			         ViewPath = viewPath
				}, ct);

                _logger.LogDebug("Notification sent to user {UserId}: {Title}",
                    recipient.UserId, recipient.MessageTitle);
            }

            // همچنین یک EntityChanged event برای real-time UI updates بفرست
            //var userIds = recipients.Select(r => r.UserId.ToString()).ToArray();
            //foreach (var userId in userIds)
            //{
            //    await _hub.Clients.User(userId).SendAsync("EntityChanged", new
            //    {
            //        Message = $"{evt.EntityName} با شناسه {evt.EntityId} {GetPersianOperation(evt.Operation)} شد",
            //        Operation = evt.Operation,
            //        EntityName = evt.EntityName,
            //        EntityId = evt.EntityId,
            //        Metadata = evt.Metadata,
            //        Timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
            //    }, ct);
            //}
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling entity change for {EntityName} #{EntityId}",
                evt.EntityName, evt.EntityId);
        }
    }

    /// <summary>
    /// فراخوانی WebApp API برای evaluate کردن rules
    /// </summary>
    private async Task<List<EvaluationRecipient>> EvaluateRulesViaWebAppAsync(
        EntityChangedEvent evt,
        CancellationToken ct)
    {
           

		try
        {
            // تبدیل operation string به enum
            var operation = evt.Operation switch
            {
                "Create" => 0, // SystemOpertions.Create
                "Update" => 1, // SystemOpertions.Edit
                "Delete" => 2, // SystemOpertions.Delete
                _ => 1
            };
		 
               using var scope = _serviceScopeFactory.CreateScope();

               var notificationService = scope.ServiceProvider.GetRequiredService<INotifitactionBuilderService>();

			var recipients = await notificationService.EvaluateEntityChangeAsync(
			  evt.EntityName,
			    (Entities.Base.Enums.SystemOpertions)operation,
			    evt.EntityId
			);
                
			return recipients.Select(r => new EvaluationRecipient
			{
				UserId = r.UserId,
				MessageTitle = r.MessageTitle,
                    Body = r.Body
			}).ToList() ?? new List<EvaluationRecipient>();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error calling WebApp API to evaluate rules");
			return new List<EvaluationRecipient>();
		}
	}

    private string GetPersianOperation(string operation) => operation switch
    {
        "Create" => "ایجاد",
        "Update" => "ویرایش",
        "Delete" => "حذف",
        _ => operation
    };

    /// <summary>
    /// قوانین notification بر اساس نوع موجودیت و عملیات
    /// اینجا می‌توانید business logic خودتان را قرار دهید
    /// </summary>
    private EntityNotification? BuildNotificationForEntity(EntityChangedEvent evt)
    {
        // مثال: User ایجاد شد → به Admins اطلاع بده
        if (evt.EntityName == "User" && evt.Operation == "Create")
        {
            var userName = evt.Metadata?.GetValueOrDefault("UserName") ?? "نامشخص";
            return new EntityNotification
            {
                Message = $"کاربر جدید '{userName}' با شناسه {evt.EntityId} ایجاد شد",
                TargetGroup = "Admins"
            };
        }

        // مثال: Notification ایجاد شد → به صاحب آن بفرست
        if (evt.EntityName == "Notification" && evt.Operation == "Create")
        {
            // Notification خودش را NotificationService می‌فرستد، پس اینجا نیازی نیست
            return null;
        }

        // مثال: موجودیت مهمی حذف شد → به همه اطلاع بده
        if (evt.Operation == "Delete" && IsImportantEntity(evt.EntityName))
        {
            return new EntityNotification
            {
                Message = $"{evt.EntityName} با شناسه {evt.EntityId} حذف شد",
                BroadcastToAll = true
            };
        }

        // TODO: اینجا قوانین خودتان را اضافه کنید:
        // - به کاربران خاص notification بفرستید
        // - بر اساس Role یا Permission فیلتر کنید
        // - از NotifitactionBuilderService برای evaluate کردن rules استفاده کنید

        return null;
    }

    private bool IsImportantEntity(string entityName)
    {
        // لیست موجودیت‌های مهم که حذفشان باید اعلام شود
        var importantEntities = new[] { "User", "Role", "Team", "AccessController" };
        return importantEntities.Contains(entityName);
    }

    // DTOs for WebApp API communication
    private sealed class EvaluateResponse
    {
        public List<RecipientDto>? Recipients { get; set; }
    }

    private sealed class RecipientDto
    {
        public long UserId { get; set; }
        public string MessageTitle { get; set; } = string.Empty;
    }

    private sealed class EvaluationRecipient
    {
        public long UserId { get; set; }
        public string MessageTitle { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    private sealed class EntityNotification
    {
        public required string Message { get; init; }
        public string? TargetGroup { get; init; }
        public long[]? TargetUserIds { get; init; }
        public bool BroadcastToAll { get; init; }
    }
}

