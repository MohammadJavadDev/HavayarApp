using Data.Contracts;
using Data.Repositories;
using Entities.Base;
using Entities.Base.Enums;
using Entities.Base.NotifitactionBuilder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.NotifitactionBuilderServices;

/// <summary>
/// Cache برای نگهداری NotificationBuilder rules بر اساس (UserId, EntityFullName, Operation)
/// </summary>
public interface INotificationRuleCache
{
    /// <summary>
    /// دریافت تمام rules فعال برای یک entity و operation
    /// </summary>
    Task<List<NotificationBuidler>> GetRulesAsync(string entityFullName, SystemOpertions operation, CancellationToken ct = default);

    /// <summary>
    /// دریافت rules یک کاربر خاص برای entity و operation
    /// </summary>
    Task<NotificationBuidler?> GetUserRuleAsync(long userId, string entityFullName, SystemOpertions operation, CancellationToken ct = default);

    /// <summary>
    /// اضافه یا به‌روزرسانی rule در cache
    /// </summary>
    Task SetRuleAsync(NotificationBuidler rule, CancellationToken ct = default);

    /// <summary>
    /// حذف rule از cache
    /// </summary>
    Task RemoveRuleAsync(long userId, string entityFullName, SystemOpertions operation, CancellationToken ct = default);

    /// <summary>
    /// پاک کردن کل cache (برای reload)
    /// </summary>
    Task ClearAsync(CancellationToken ct = default);

    /// <summary>
    /// Load کردن تمام rules از دیتابیس
    /// </summary>
    Task LoadFromDatabaseAsync(Func<Task<List<NotificationBuidler>>> loader, CancellationToken ct = default);
}

/// <summary>
/// پیاده‌سازی Redis-backed cache برای NotificationBuilder rules
/// </summary>
public sealed class NotificationRuleCache : INotificationRuleCache
{
    private readonly IDistributedCache _redis;
    private readonly string _keyPrefix = "NotificationRules:";
    private readonly TimeSpan _expiration = TimeSpan.FromHours(24);
 
    private readonly IServiceScopeFactory _scopeFactory;

    // JsonSerializerOptions برای حل circular reference
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.Preserve,
        WriteIndented = false
    };

    public NotificationRuleCache(IDistributedCache redis, IServiceScopeFactory serviceScopeFactory)
    {
        _redis = redis;
 
 
          _scopeFactory = serviceScopeFactory;
    }

    /// <summary>
    /// ساخت cache key
    /// </summary>
    private string GetKey(string entityFullName, SystemOpertions operation)
        => $"{_keyPrefix}{entityFullName.ToLower()}:{operation}";

    private string GetKey(long userId, string entityFullName, SystemOpertions operation)
        => $"{_keyPrefix}user:{userId}:{entityFullName.ToLower()}:{operation}";

    public async Task<List<NotificationBuidler>> GetRulesAsync(string entityFullName, SystemOpertions operation, CancellationToken ct = default)
    {
        var key = GetKey(entityFullName, operation);
        var json = await _redis.GetStringAsync(key, ct);

        if (string.IsNullOrEmpty(json))
            return new List<NotificationBuidler>();

        var rules = JsonSerializer.Deserialize<List<NotificationBuidler>>(json, JsonOptions);
        return rules?.Where(r => r.IsActive == IsActiveEnum.Active).ToList() ?? new List<NotificationBuidler>();
    }

    public async Task<NotificationBuidler?> GetUserRuleAsync(long userId, string entityFullName, SystemOpertions operation, CancellationToken ct = default)
    {
        var key = GetKey(userId, entityFullName, operation);
        var json = await _redis.GetStringAsync(key, ct);

        if (string.IsNullOrEmpty(json))
            return null;

        var rule = JsonSerializer.Deserialize<NotificationBuidler>(json, JsonOptions);
        return rule?.IsActive == IsActiveEnum.Active ? rule : null;
    }

    public async Task SetRuleAsync(NotificationBuidler rule, CancellationToken ct = default)
    {
        // ذخیره rule خاص کاربر
        var userKey = GetKey(rule.UserId, rule.EntityFullName, rule.SystemOpertion);

        if (rule.IsActive == IsActiveEnum.Active)
        {
            var json = JsonSerializer.Serialize(rule, JsonOptions);
            await _redis.SetStringAsync(userKey, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _expiration
            }, ct);
        }
        else
        {
            // غیرفعال شده - حذف از cache
            await _redis.RemoveAsync(userKey, ct);
        }

        // Update کردن لیست کلی rules برای این entity+operation
        await UpdateEntityOperationListAsync(rule.EntityFullName, rule.SystemOpertion, ct);
    }

    public async Task RemoveRuleAsync(long userId, string entityFullName, SystemOpertions operation, CancellationToken ct = default)
    {
        var userKey = GetKey(userId, entityFullName, operation);
        await _redis.RemoveAsync(userKey, ct);

        // Update لیست کلی
        await UpdateEntityOperationListAsync(entityFullName, operation, ct);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        // Redis StackExchange not supports pattern-based delete directly
        // باید با Lua script یا manual iteration
        // برای سادگی، فقط expiration داریم و در reload همه را می‌خوانیم
    }

    public async Task LoadFromDatabaseAsync(Func<Task<List<NotificationBuidler>>> loader, CancellationToken ct = default)
    {
        var allRules = await loader();

        // گروه‌بندی بر اساس entity و operation
        var grouped = allRules
            .Where(r => r.IsActive == IsActiveEnum.Active)
            .GroupBy(r => new { r.EntityFullName, r.SystemOpertion });

        foreach (var group in grouped)
        {
            // ذخیره لیست rules برای این entity+operation
            var key = GetKey(group.Key.EntityFullName, group.Key.SystemOpertion);
            var json = JsonSerializer.Serialize(group.ToList(), JsonOptions);
            await _redis.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _expiration
            }, ct);

            // ذخیره هر rule جداگانه هم برای GetUserRuleAsync
            foreach (var rule in group)
            {
                var userKey = GetKey(rule.UserId, rule.EntityFullName, rule.SystemOpertion);
                var ruleJson = JsonSerializer.Serialize(rule, JsonOptions);
                await _redis.SetStringAsync(userKey, ruleJson, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _expiration
                }, ct);
            }
        }
    }

	 private async Task<List<NotificationBuidler>> DbLoader()
	{
		using var scope = _scopeFactory.CreateAsyncScope();

		var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

		return await unitOfWork.Repository<NotificationBuidler>().TableNoTracking.ToListAsync();
	}

	// dbLoaderFiltered function برای خواندن rules فیلتر شده (بهینه‌تر)
	 private async Task<List<NotificationBuidler>> DbLoaderFiltered(string entityFullName,SystemOpertions operation) 
	{
		using var scope = _scopeFactory.CreateAsyncScope();

		var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
		return await unitOfWork.Repository<NotificationBuidler>().TableNoTracking
		  .Where(r => r.EntityFullName == entityFullName &&
						  r.SystemOpertion == operation &&
						  r.IsActive == IsActiveEnum.Active).ToListAsync();
	}

	/// <summary>
	/// Update کردن لیست کلی rules برای یک entity+operation
	/// </summary>
	private async Task UpdateEntityOperationListAsync(string entityFullName, SystemOpertions operation, CancellationToken ct)
    {
        try
        {
            // حذف لیست قدیمی
            var key = GetKey(entityFullName, operation);
            await _redis.RemoveAsync(key, ct);

            List<NotificationBuidler> filteredRules;

			filteredRules = await DbLoaderFiltered(entityFullName, operation);
                
            // ذخیره لیست جدید در Redis
            if (filteredRules.Any())
            {
                var json = JsonSerializer.Serialize(filteredRules, JsonOptions);
                await _redis.SetStringAsync(key, json, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _expiration
                }, ct);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw - cache invalidation is not critical
            // در production باید logger استفاده کنیم
            Console.WriteLine($"Error updating entity operation list for {entityFullName}:{operation}: {ex.Message}");
        }
    }
}

