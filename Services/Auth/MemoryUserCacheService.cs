using Entities.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Auth
{
	public class MemoryUserCacheService : IUserCacheService
	{
		private readonly IMemoryCache _cache;
		private readonly ILogger<MemoryUserCacheService> _logger;

		private static readonly TimeSpan DefaultExpiration = TimeSpan.FromDays(1);

		public MemoryUserCacheService(IMemoryCache cache, ILogger<MemoryUserCacheService> logger)
		{
			_cache = cache;
			_logger = logger;
		}

		public Task<UserRoleAccessesInfo> GetAsync(long userId)
		{
			var key = CacheKeys.UserPermissions(userId);

			if (_cache.TryGetValue(key, out UserRoleAccessesInfo info))
				return Task.FromResult(info);

			return Task.FromResult<UserRoleAccessesInfo>(null);
		}

		public Task SetAsync(UserRoleAccessesInfo info)
		{
			var key = CacheKeys.UserPermissions(info.UserId);

			_cache.Set(
			    key,
			    info,
			    new MemoryCacheEntryOptions
			    {
				    AbsoluteExpirationRelativeToNow = DefaultExpiration,
				    Priority = CacheItemPriority.High,
				    Size = 1
			    });

			_logger.LogInformation("User {UserId} cached.", info.UserId);
			return Task.CompletedTask;
		}

		public Task RemoveAsync(long userId)
		{
			var key = CacheKeys.UserPermissions(userId);
			_cache.Remove(key);

			_logger.LogInformation("User {UserId} cache cleared.", userId);
			return Task.CompletedTask;
		}

		/// <summary>
		/// Refresh user using a loader function (db call)
		/// </summary>
		public async Task RefreshAsync(long userId, Func<Task<UserRoleAccessesInfo>> loader)
		{
			_logger.LogInformation("Refreshing cache for user {UserId}", userId);

			var data = await loader();

			if (data != null)
				await SetAsync(data);
			else
				await RemoveAsync(userId);
		}
	}

}
