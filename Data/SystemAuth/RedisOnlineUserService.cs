using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Data.SystemAuth;

/// <summary>
/// پیاده‌سازی Redis-backed سرویس کاربران آنلاین با بهینه‌سازی کامل
/// </summary>
public sealed class RedisOnlineUserService : IOnlineUserService
{
	private readonly IDistributedCache _redis;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly string _keyPrefix = "OnlineUsers:";
	private readonly string _userIdsKey = "OnlineUsers:UserIds";
	private readonly TimeSpan _expiration = TimeSpan.FromHours(24);
	private readonly TimeSpan _userExpiration = TimeSpan.FromMinutes(30);

	// برای primitive types و collections ساده
	private static readonly JsonSerializerOptions SimpleJsonOptions = new()
	{
		WriteIndented = false
	};

	// برای اشیاء پیچیده
	private static readonly JsonSerializerOptions ComplexJsonOptions = new()
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = false
	};

	public RedisOnlineUserService(IDistributedCache redis, IServiceScopeFactory scopeFactory)
	{
		_redis = redis ?? throw new ArgumentNullException(nameof(redis));
		_scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
	}

	#region Add/Remove Online Users

	/// <summary>
	/// اضافه کردن کاربر آنلاین (Async)
	/// </summary>
	public async Task AddOnlineUserAsync(long userId, string? connectionId, CancellationToken ct = default)
	{
		try
		{
			var userDto = await GetOrLoadUserAsync(userId, ct);
			if (userDto == null)
				return;

			// اضافه کردن connectionId
			if (!string.IsNullOrWhiteSpace(connectionId) && !userDto.ConnectionIds.Contains(connectionId))
			{
				userDto.ConnectionIds.Add(connectionId);
			}

			// به‌روزرسانی LastActivity
			userDto.LastActivity = DateTime.UtcNow;

			// ذخیره در Redis
			await SaveUserToRedisAsync(userId, userDto, ct);

			// اضافه کردن userId به لیست آنلاین
			await AddUserIdToOnlineListAsync(userId, ct);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error adding online user {userId}: {ex.Message}");
		}
	}

	/// <summary>
	/// اضافه کردن کاربر آنلاین (Sync)
	/// </summary>
	public OnlineUserDto? AddOnlineUser(long userId, string? connectionId)
	{
		try
		{
			var userDto = GetOrLoadUser(userId);
			if (userDto == null)
				return null;

			// اضافه کردن connectionId
			if (!string.IsNullOrWhiteSpace(connectionId) && !userDto.ConnectionIds.Contains(connectionId))
			{
				userDto.ConnectionIds.Add(connectionId);
			}

			// به‌روزرسانی LastActivity
			userDto.LastActivity = DateTime.UtcNow;

			// ذخیره در Redis
			SaveUserToRedis(userId, userDto);

			// اضافه کردن userId به لیست آنلاین
			AddUserIdToOnlineList(userId);

			return userDto;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error adding online user {userId}: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// حذف کاربر آنلاین
	/// </summary>
	public async Task RemoveOnlineUserAsync(long userId, string connectionId, CancellationToken ct = default)
	{
		try
		{
			var userDto = await GetOnlineUserAsync(userId, ct);
			if (userDto == null)
				return;

			// حذف connectionId
			userDto.ConnectionIds.Remove(connectionId);

			// اگر connection نداره، کاربر را حذف کن
			if (userDto.ConnectionIds.Count == 0)
			{
				await _redis.RemoveAsync(GetUserKey(userId), ct);
				await RemoveUserIdFromOnlineListAsync(userId, ct);
			}
			else
			{
				// در غیر این صورت، به‌روزرسانی کن
				await SaveUserToRedisAsync(userId, userDto, ct);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error removing online user {userId}: {ex.Message}");
		}
	}

	#endregion

	#region Get Online Users

	/// <summary>
	/// دریافت لیست تمام کاربران آنلاین
	/// </summary>
	public async Task<List<OnlineUserDto>> GetAllOnlineUsersAsync(CancellationToken ct = default)
	{
		try
		{
			var userIds = await GetOnlineUserIdsAsync(ct);
			if (userIds.Count == 0)
				return new List<OnlineUserDto>();

			// دریافت اطلاعات کاربران به صورت موازی
			var tasks = userIds.Select(userId => GetOnlineUserAsync(userId, ct));
			var users = await Task.WhenAll(tasks);

			return users
			    .Where(u => u != null && u.ConnectionIds.Count > 0)
			    .ToList()!;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error getting all online users: {ex.Message}");
			return new List<OnlineUserDto>();
		}
	}

	/// <summary>
	/// دریافت اطلاعات یک کاربر آنلاین (Async)
	/// </summary>
	public async Task<OnlineUserDto?> GetOnlineUserAsync(long userId, CancellationToken ct = default)
	{
		try
		{
			var userKey = GetUserKey(userId);
			var userJson = await _redis.GetStringAsync(userKey, ct);

			if (string.IsNullOrEmpty(userJson))
				return null;

			return JsonSerializer.Deserialize<OnlineUserDto>(userJson, ComplexJsonOptions);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error getting online user {userId}: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// دریافت اطلاعات یک کاربر آنلاین (Sync)
	/// در صورت عدم وجود در Redis، از دیتابیس لود می‌کند
	/// </summary>
	public OnlineUserDto? GetOnlineUser(long userId)
	{
		try
		{
			var userKey = GetUserKey(userId);
			var userJson = _redis.GetString(userKey);

			if (string.IsNullOrEmpty(userJson))
			{
				// اگر در Redis نبود، از دیتابیس لود کن و به Redis اضافه کن
				return AddOnlineUser(userId, null);
			}

			return JsonSerializer.Deserialize<OnlineUserDto>(userJson, ComplexJsonOptions);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error getting online user {userId}: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// بررسی آنلاین بودن کاربر
	/// </summary>
	public async Task<bool> IsUserOnlineAsync(long userId, CancellationToken ct = default)
	{
		var user = await GetOnlineUserAsync(userId, ct);
		return user != null && user.ConnectionIds.Count > 0;
	}

	/// <summary>
	/// دریافت تعداد کاربران آنلاین
	/// </summary>
	public async Task<int> GetOnlineUsersCountAsync(CancellationToken ct = default)
	{
		var userIds = await GetOnlineUserIdsAsync(ct);
		return userIds.Count;
	}

	#endregion

	#region Role Access Methods

	/// <summary>
	/// دریافت RoleAccess های کاربر (Async)
	/// </summary>
	public async Task<List<RoleAccessDto>> GetUserRoleAccessesAsync(long userId, CancellationToken ct = default)
	{
		var user = await GetOnlineUserAsync(userId, ct);
		return user?.RoleAccesses ?? new List<RoleAccessDto>();
	}

	/// <summary>
	/// دریافت RoleAccess های کاربر (Sync)
	/// </summary>
	public List<RoleAccessDto> GetUserRoleAccesses(long userId)
	{
		var user = GetOnlineUser(userId);
		return user?.RoleAccesses ?? new List<RoleAccessDto>();
	}

	/// <summary>
	/// دریافت نقش‌های کاربر (Async)
	/// </summary>
	public async Task<List<string>> GetUserRoleAsync(long userId, CancellationToken ct = default)
	{
		var user = await GetOnlineUserAsync(userId, ct);
		return user?.Roles ?? new List<string>();
	}

	/// <summary>
	/// دریافت نقش‌های کاربر (Sync)
	/// </summary>
	public List<string> GetUserRole(long userId)
	{
		var user = GetOnlineUser(userId);
		return user?.Roles ?? new List<string>();
	}

	/// <summary>
	/// بررسی دسترسی کاربر به یک path
	/// </summary>
	public async Task<bool> HasUserAccessAsync(long userId, string path, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(path))
			return false;

		var roleAccesses = await GetUserRoleAccessesAsync(userId, ct);
		return roleAccesses.Any(ra => ra.Path?.Equals(path, StringComparison.OrdinalIgnoreCase) == true);
	}

	/// <summary>
	/// به‌روزرسانی RoleAccess های کاربر (Async)
	/// </summary>
	public async Task RefreshUserRoleAccessesAsync(long userId, CancellationToken ct = default)
	{
		try
		{
			var existingUser = await GetOnlineUserAsync(userId, ct);
			if (existingUser == null)
				return; // کاربر آنلاین نیست

			// بارگذاری مجدد از دیتابیس
			var updatedUserDto = await LoadUserFromDatabaseAsync(userId, ct);
			if (updatedUserDto == null)
				return;

			// حفظ اطلاعات connection
			updatedUserDto.ConnectionIds = existingUser.ConnectionIds;
			updatedUserDto.ConnectedAt = existingUser.ConnectedAt;
			updatedUserDto.LastActivity = DateTime.UtcNow;
			updatedUserDto.PagesByConnection = existingUser.PagesByConnection;

			// ذخیره مجدد
			await SaveUserToRedisAsync(userId, updatedUserDto, ct);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error refreshing user role accesses {userId}: {ex.Message}");
		}
	}

	/// <summary>
	/// به‌روزرسانی RoleAccess های کاربر (Sync)
	/// </summary>
	public void RefreshUserRoleAccesses(long userId)
	{
		try
		{
			var existingUser = GetOnlineUser(userId);
			if (existingUser == null)
				return;

			// بارگذاری مجدد از دیتابیس
			var updatedUserDto = LoadUserFromDatabase(userId);
			if (updatedUserDto == null)
				return;

			// حفظ اطلاعات connection
			updatedUserDto.ConnectionIds = existingUser.ConnectionIds;
			updatedUserDto.ConnectedAt = existingUser.ConnectedAt;
			updatedUserDto.LastActivity = DateTime.UtcNow;
			updatedUserDto.PagesByConnection = existingUser.PagesByConnection;

			// ذخیره مجدد
			SaveUserToRedis(userId, updatedUserDto);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error refreshing user role accesses {userId}: {ex.Message}");
		}
	}

	#endregion

	#region Page Management

	/// <summary>
	/// اضافه یا به‌روزرسانی صفحه باز شده کاربر
	/// </summary>
	public async Task AddOrUpdatePageAsync(
	    long userId,
	    string connectionId,
	    string path,
	    string? title,
	    CancellationToken ct = default)
	{
		try
		{
			var user = await GetOnlineUserAsync(userId, ct);
			if (user == null)
				return;

			if (!user.PagesByConnection.ContainsKey(connectionId))
				user.PagesByConnection[connectionId] = new List<OnlineUserPageDto>();

			var pages = user.PagesByConnection[connectionId];
			var page = pages.FirstOrDefault(p => p.Path == path);

			if (page == null)
			{
				pages.Add(new OnlineUserPageDto
				{
					Path = path,
					Title = title,
					OpenedAt = DateTime.UtcNow,
					LastActive = DateTime.UtcNow
				});
			}
			else
			{
				page.LastActive = DateTime.UtcNow;
				page.Title = title;
			}

			user.LastActivity = DateTime.UtcNow;

			await SaveUserToRedisAsync(userId, user, ct);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"AddOrUpdatePageAsync error: {ex.Message}");
		}
	}

	/// <summary>
	/// حذف صفحه باز شده کاربر
	/// </summary>
	public async Task RemovePageAsync(
	    long userId,
	    string connectionId,
	    string path,
	    CancellationToken ct = default)
	{
		try
		{
			var user = await GetOnlineUserAsync(userId, ct);
			if (user == null)
				return;

			if (!user.PagesByConnection.TryGetValue(connectionId, out var pages))
				return;

			pages.RemoveAll(p => p.Path == path);

			if (pages.Count == 0)
				user.PagesByConnection.Remove(connectionId);

			await SaveUserToRedisAsync(userId, user, ct);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"RemovePageAsync error: {ex.Message}");
		}
	}

	#endregion

	#region Private Helper Methods

	/// <summary>
	/// دریافت یا لود کاربر از دیتابیس (Async)
	/// </summary>
	private async Task<OnlineUserDto?> GetOrLoadUserAsync(long userId, CancellationToken ct)
	{
		var existingUser = await GetOnlineUserAsync(userId, ct);
		return existingUser ?? await LoadUserFromDatabaseAsync(userId, ct);
	}

	/// <summary>
	/// دریافت یا لود کاربر از دیتابیس (Sync)
	/// </summary>
	private OnlineUserDto? GetOrLoadUser(long userId)
	{
		var userKey = GetUserKey(userId);
		var userJson = _redis.GetString(userKey);

		if (!string.IsNullOrEmpty(userJson))
		{
			return JsonSerializer.Deserialize<OnlineUserDto>(userJson, ComplexJsonOptions);
		}

		return LoadUserFromDatabase(userId);
	}
	/// <summary>
	/// بارگذاری اطلاعات کاربر از دیتابیس با یک Query بهینه (Async)
	/// </summary>
	private async Task<OnlineUserDto?> LoadUserFromDatabaseAsync(long userId, CancellationToken ct)
	{
		try
		{
			using var scope = _scopeFactory.CreateAsyncScope();
			var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			// مرحله 1: دریافت کاربر
			var user = await db.Users.AsNoTracking()
			    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive == IsActiveEnum.Active, ct);

			if (user == null)
				return null;

			// مرحله 2: دریافت RoleAccess ها (فقط اگر RoleIds وجود داشت)
			var roleAccesses = new List<RoleAccessDto>();

			if (user.RoleIds != null && user.RoleIds.Count > 0)
			{
				// دریافت RoleAccess ها به صورت مستقیم
				roleAccesses = await db.Roles.AsNoTracking()
				    .Where(r => user.RoleIds.Contains((long)r.Id))
				    .SelectMany(r => r.RoleAccesses.Select(ra => new RoleAccessDto
				    {
					    Id = (long)ra.Id!,
					    Path = ra.Path,
					    ActionAccessType = ra.ActionAccessType,
					    ActionAccessItemType = ra.ActionAccessItemType,
					    EntityName = ra.EntityName,
					    DisplayName = ra.DisplayName,
					    EntityId = ra.EntityId,
					    RowId = ra.RowId,
					    RoleId = ra.RoleId,
					    RoleName = r.Name
				    }))
				    .ToListAsync(ct);
			}

			return new OnlineUserDto
			{
				UserId = (long)user.Id!,
				Username = user.Username,
				FullName = user.Name,
				ProfileImage = user.ProfileUrl,
				Roles = user.Roles ?? new List<string>(),
				RoleIds = user.RoleIds ?? new List<long>(),
				RoleAccesses = roleAccesses,
				ConnectionIds = new List<string>(),
				ConnectedAt = DateTime.UtcNow,
				LastActivity = DateTime.UtcNow,
				FullNameFn = user.NameFa,
				OrganizationUnitId = user.OrgUnitId,
				Email = user.Email,
				PagesByConnection = new Dictionary<string, List<OnlineUserPageDto>>()
			};
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error loading user from database {userId}: {ex.Message}");
			return null;
		}
	}
	/// <summary>
	/// بارگذاری اطلاعات کاربر از دیتابیس (Sync)
	/// </summary>
	private OnlineUserDto? LoadUserFromDatabase(long userId)
	{
		return LoadUserFromDatabaseAsync(userId, CancellationToken.None)
		    .GetAwaiter()
		    .GetResult();
	}

	/// <summary>
	/// ذخیره کاربر در Redis (Async)
	/// </summary>
	private async Task SaveUserToRedisAsync(long userId, OnlineUserDto user, CancellationToken ct)
	{
		var userKey = GetUserKey(userId);
		var userJson = JsonSerializer.Serialize(user, ComplexJsonOptions);

		await _redis.SetStringAsync(userKey, userJson, new DistributedCacheEntryOptions
		{
			AbsoluteExpirationRelativeToNow = _userExpiration
		}, ct);
	}

	/// <summary>
	/// ذخیره کاربر در Redis (Sync)
	/// </summary>
	private void SaveUserToRedis(long userId, OnlineUserDto user)
	{
		var userKey = GetUserKey(userId);
		var userJson = JsonSerializer.Serialize(user, ComplexJsonOptions);

		_redis.SetString(userKey, userJson, new DistributedCacheEntryOptions
		{
			AbsoluteExpirationRelativeToNow = _userExpiration
		});
	}

	/// <summary>
	/// دریافت لیست userId های آنلاین
	/// </summary>
	private async Task<HashSet<long>> GetOnlineUserIdsAsync(CancellationToken ct)
	{
		try
		{
			var userIdsJson = await _redis.GetStringAsync(_userIdsKey, ct);

			if (string.IsNullOrEmpty(userIdsJson))
				return new HashSet<long>();

			return JsonSerializer.Deserialize<HashSet<long>>(userIdsJson, SimpleJsonOptions)
				  ?? new HashSet<long>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error getting online user IDs: {ex.Message}");
			return new HashSet<long>();
		}
	}

	/// <summary>
	/// اضافه کردن userId به لیست آنلاین (Async)
	/// </summary>
	private async Task AddUserIdToOnlineListAsync(long userId, CancellationToken ct)
	{
		try
		{
			var userIds = await GetOnlineUserIdsAsync(ct);

			if (userIds.Add(userId)) // فقط اگر اضافه شد
			{
				var updatedJson = JsonSerializer.Serialize(userIds, SimpleJsonOptions);
				await _redis.SetStringAsync(_userIdsKey, updatedJson, new DistributedCacheEntryOptions
				{
					AbsoluteExpirationRelativeToNow = _expiration
				}, ct);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error adding userId to online list: {ex.Message}");
		}
	}

	/// <summary>
	/// اضافه کردن userId به لیست آنلاین (Sync)
	/// </summary>
	private void AddUserIdToOnlineList(long userId)
	{
		try
		{
			var userIdsJson = _redis.GetString(_userIdsKey);

			var userIds = string.IsNullOrEmpty(userIdsJson)
			    ? new HashSet<long>()
			    : JsonSerializer.Deserialize<HashSet<long>>(userIdsJson, SimpleJsonOptions)
				 ?? new HashSet<long>();

			if (userIds.Add(userId))
			{
				var updatedJson = JsonSerializer.Serialize(userIds, SimpleJsonOptions);
				_redis.SetString(_userIdsKey, updatedJson, new DistributedCacheEntryOptions
				{
					AbsoluteExpirationRelativeToNow = _expiration
				});
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error adding userId to online list: {ex.Message}");
		}
	}

	/// <summary>
	/// حذف userId از لیست آنلاین (Async)
	/// </summary>
	private async Task RemoveUserIdFromOnlineListAsync(long userId, CancellationToken ct)
	{
		try
		{
			var userIds = await GetOnlineUserIdsAsync(ct);

			if (userIds.Remove(userId)) // فقط اگر حذف شد
			{
				if (userIds.Count == 0)
				{
					await _redis.RemoveAsync(_userIdsKey, ct);
				}
				else
				{
					var updatedJson = JsonSerializer.Serialize(userIds, SimpleJsonOptions);
					await _redis.SetStringAsync(_userIdsKey, updatedJson, new DistributedCacheEntryOptions
					{
						AbsoluteExpirationRelativeToNow = _expiration
					}, ct);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error removing userId from online list: {ex.Message}");
		}
	}

	/// <summary>
	/// ساخت کلید Redis برای کاربر
	/// </summary>
	private string GetUserKey(long userId) => $"{_keyPrefix}User:{userId}";

	#endregion
}