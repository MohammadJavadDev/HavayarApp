using Entities.Base.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.NotifitactionBuilderServices;

namespace WebFramework.Controllers.SystemControllers;

/// <summary>
/// API endpoint برای evaluate کردن notification rules
/// این endpoint توسط App.Real برای تشخیص کاربران دریافت‌کننده notification فراخوانی می‌شود
/// </summary>
[ApiController]
[Route("api/notification-rules")]
public sealed class NotificationRulesController(INotifitactionBuilderService notificationService) : ControllerBase
{
    /// <summary>
    /// Evaluate می‌کند که کدام کاربران باید برای یک entity change notification دریافت کنند
    /// </summary>
    /// <param name="request">اطلاعات entity تغییر یافته</param>
    /// <returns>لیست کاربران و پیام‌های مرتبط</returns>
    [HttpPost("evaluate")]
    public async Task<IActionResult> EvaluateEntityChange([FromBody] EvaluateEntityChangeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EntityFullName) || string.IsNullOrWhiteSpace(request.EntityId))
        {
            return BadRequest(new { error = "EntityFullName and EntityId are required" });
        }

        try
        {
            var recipients = await notificationService.EvaluateEntityChangeAsync(
                request.EntityFullName,
                request.Operation,
                request.EntityId
            );

            return Ok(new
            {
                recipients = recipients.Select(r => new
                {
                    userId = r.UserId,
                    messageTitle = r.MessageTitle
                })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Reload کردن cache از دیتابیس (برای مواقعی که manually تغییر داده شده)
    /// </summary>
    [HttpPost("reload-cache")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ReloadCache()
    {
        try
        {
            await notificationService.InitializeCacheAsync();
            return Ok(new { message = "Cache reloaded successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public sealed class EvaluateEntityChangeRequest
{
    public required string EntityFullName { get; init; }
    public required SystemOpertions Operation { get; init; }
    public required string EntityId { get; init; }
}

