using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Data.SystemAuth;

/// <summary>
/// پیاده‌سازی Redis-backed سرویس کاربران آنلاین
/// </summary>
public sealed class RedisOnlineUserService : IOnlineUserService
{
	private readonly IDistributedCache _redis;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly string _keyPrefix = "OnlineUsers:";
	private readonly string _allUsersKey = "OnlineUsers:All";
	private readonly TimeSpan _expiration = TimeSpan.FromHours(24);
	private readonly TimeSpan _userExpiration = TimeSpan.FromMinutes(30); // کاربر بعد از 30 دقیقه غیرفعال، offline می‌شود
 
	// JsonSerializerOptions
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		ReferenceHandler = ReferenceHandler.Preserve,
		WriteIndented = false
	};

	public RedisOnlineUserService(IDistributedCache redis, IServiceScopeFactory scopeFactory)
	{
		_redis = redis;
		_scopeFactory = scopeFactory;
		 
	}

	/// <summary>
	/// اضافه کردن کاربر آنلاین
	/// </summary>
	public async Task AddOnlineUserAsync(long userId, string? connectionId, CancellationToken ct = default)
	{
		try
		{
			// دریافت اطلاعات کاربر از دیتابیس
			var userDto = await LoadUserFromDatabaseAsync(userId, ct);
			if (userDto == null)
				return;

			// اضافه کردن connectionId
			if (!connectionId.IsNullOrEmpty() && !userDto.ConnectionIds.Contains(connectionId))
			{
				userDto.ConnectionIds.Add(connectionId);
			}

			// به‌روزرسانی LastActivity
			userDto.LastActivity = DateTime.UtcNow;

			// ذخیره در Redis
			var userKey = GetUserKey(userId);
			var userJson = JsonSerializer.Serialize(userDto, JsonOptions);
			await _redis.SetStringAsync(userKey, userJson, new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = _userExpiration
			}, ct);

			// اضافه کردن userId به لیست کاربران آنلاین
			await AddUserIdToOnlineListAsync(userId, ct);
		}
		catch (Exception ex)
		{
			// Log error but don't throw
			Console.WriteLine($"Error adding online user {userId}: {ex.Message}");
		}
	}

	/// <summary>
	/// اضافه کردن کاربر آنلاین
	/// </summary>
	public OnlineUserDto? AddOnlineUser(long userId, string? connectionId)
	{
		try
		{
			// دریافت اطلاعات کاربر از دیتابیس
			var userDto =   LoadUserFromDatabase(userId);
			if (userDto == null)
				return null;

			// اضافه کردن connectionId
			if (connectionId.HasValue() && !userDto.ConnectionIds.Contains(connectionId))
			{
				userDto.ConnectionIds.Add(connectionId);
			}

			// به‌روزرسانی LastActivity
			userDto.LastActivity = DateTime.UtcNow;

			// ذخیره در Redis
			var userKey = GetUserKey(userId);
			var userJson = JsonSerializer.Serialize(userDto, JsonOptions);
			  _redis.SetString(userKey, userJson, new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = _userExpiration
			});

			// اضافه کردن userId به لیست کاربران آنلاین
			  AddUserIdToOnlineList(userId);

			return userDto;
		}
		catch (Exception ex)
		{
			// Log error but don't throw
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
			var userKey = GetUserKey(userId);
			var userJson = await _redis.GetStringAsync(userKey, ct);

			if (string.IsNullOrEmpty(userJson))
				return;

			var userDto = JsonSerializer.Deserialize<OnlineUserDto>(userJson, JsonOptions);
			if (userDto == null)
				return;

			// حذف connectionId
			userDto.ConnectionIds.Remove(connectionId);

			// اگر دیگر connectionی نداره، کاربر را حذف کن
			if (userDto.ConnectionIds.Count == 0)
			{
				await _redis.RemoveAsync(userKey, ct);
				await RemoveUserIdFromOnlineListAsync(userId, ct);
			}
			else
			{
				// در غیر این صورت، فقط به‌روزرسانی کن
				var updatedJson = JsonSerializer.Serialize(userDto, JsonOptions);
				await _redis.SetStringAsync(userKey, updatedJson, new DistributedCacheEntryOptions
				{
					AbsoluteExpirationRelativeToNow = _userExpiration
				}, ct);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error removing online user {userId}: {ex.Message}");
		}
	}

	/// <summary>
	/// دریافت لیست تمام کاربران آنلاین
	/// </summary>
	public async Task<List<OnlineUserDto>> GetAllOnlineUsersAsync(CancellationToken ct = default)
	{
		try
		{
			// دریافت لیست userId های آنلاین
			var userIdsJson = await _redis.GetStringAsync("OnlineUsers:UserIds", ct);
			if (string.IsNullOrEmpty(userIdsJson))
				return new List<OnlineUserDto>();

			var userIds = JsonSerializer.Deserialize<List<long>>(userIdsJson, JsonOptions);
			if (userIds == null || userIds.Count == 0)
				return new List<OnlineUserDto>();

			// دریافت اطلاعات هر کاربر
			var users = new List<OnlineUserDto>();
			foreach (var userId in userIds)
			{
				var user = await GetOnlineUserAsync(userId, ct);
				if (user != null && user.ConnectionIds.Count > 0)
				{
					users.Add(user);
				}
			}

			return users;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error getting all online users: {ex.Message}");
			return new List<OnlineUserDto>();
		}
	}

	/// <summary>
	/// دریافت اطلاعات یک کاربر آنلاین
	/// </summary>
	public async Task<OnlineUserDto?> GetOnlineUserAsync(long userId, CancellationToken ct = default)
	{
		try
		{
			var userKey = GetUserKey(userId);
			var userJson = await _redis.GetStringAsync(userKey, ct);
			if (string.IsNullOrEmpty(userJson))
				return null;

			return JsonSerializer.Deserialize<OnlineUserDto>(userJson, JsonOptions);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error getting online user {userId}: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// دریافت اطلاعات یک کاربر آنلاین
	/// </summary>
	public OnlineUserDto? GetOnlineUser(long userId)
	{
		try
		{
			var userKey = GetUserKey(userId);
			var userJson = _redis.GetString(userKey);
			if (string.IsNullOrEmpty(userJson))
				return AddOnlineUser(userId,null);


			return JsonSerializer.Deserialize<OnlineUserDto>(userJson, JsonOptions);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error getting online user {userId}: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// بررسی اینکه آیا کاربر آنلاین است یا نه
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
		var allUsers = await GetAllOnlineUsersAsync(ct);
		return allUsers.Count;
	}

	/// <summary>
	/// دریافت RoleAccess های یک کاربر
	/// </summary>
	public async Task<List<RoleAccessDto>> GetUserRoleAccessesAsync(long userId, CancellationToken ct = default)
	{
		var user = await GetOnlineUserAsync(userId, ct);
		return user?.RoleAccesses ?? new List<RoleAccessDto>();
	}

	/// <summary>
	/// دریافت RoleAccess های یک کاربر
	/// </summary>
	public List<RoleAccessDto> GetUserRoleAccesses(long userId)
	{
		var user = GetOnlineUser(userId);
		return user?.RoleAccesses ?? new List<RoleAccessDto>();
	}

	/// <summary>
	/// بررسی دسترسی کاربر به یک path
	/// </summary>
	public async Task<bool> HasUserAccessAsync(long userId, string path, CancellationToken ct = default)
	{
		var roleAccesses = await GetUserRoleAccessesAsync(userId, ct);
		return roleAccesses.Any(ra => ra.Path == path);
	}

	/// <summary>
	/// به‌روزرسانی RoleAccess های یک کاربر
	/// </summary>
	public async Task RefreshUserRoleAccessesAsync(long userId, CancellationToken ct = default)
	{
		try
		{
			var userKey = GetUserKey(userId);
			var userJson = await _redis.GetStringAsync(userKey, ct);

			if (string.IsNullOrEmpty(userJson))
				return; // کاربر آنلاین نیست

			var userDto = JsonSerializer.Deserialize<OnlineUserDto>(userJson, JsonOptions);
			if (userDto == null)
				return;

			// بارگذاری مجدد RoleAccess ها از دیتابیس
			var updatedUserDto = await LoadUserFromDatabaseAsync(userId, ct);
			if (updatedUserDto == null)
				return;

			// حفظ ConnectionIds و timestamps
			updatedUserDto.ConnectionIds = userDto.ConnectionIds;
			updatedUserDto.ConnectedAt = userDto.ConnectedAt;
			updatedUserDto.LastActivity = DateTime.UtcNow;

			// ذخیره مجدد
			var updatedJson = JsonSerializer.Serialize(updatedUserDto, JsonOptions);
			await _redis.SetStringAsync(userKey, updatedJson, new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = _userExpiration
			}, ct);

			// لیست کلی خودکار به‌روزرسانی می‌شود
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error refreshing user role accesses {userId}: {ex.Message}");
		}
	}

	/// <summary>
	/// بارگذاری اطلاعات کاربر از دیتابیس
	/// </summary>
	private async Task<OnlineUserDto?> LoadUserFromDatabaseAsync(long userId, CancellationToken ct)
	{
		using var scope = _scopeFactory.CreateAsyncScope();

 
		var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
		var _db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		// دریافت کاربر
		var user = await _db.Users.AsNoTracking()
			.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive == IsActiveEnum.Active, ct);

		if (user == null)
			return null;

		// دریافت RoleAccess ها
		var roleAccesses = new List<RoleAccessDto>();

		if (user.Roles != null && user.Roles.Length > 0)
		{
		// دریافت Role ها با RoleAccesses
		var roles = await unitOfWork.Repository<Role>().Table
			.Where(r => user.Roles.Contains(r.Name))
			.Include(r => r.RoleAccesses)
			.AsNoTracking()
			.ToListAsync(ct);

			// تبدیل RoleAccess ها به DTO
			foreach (var role in roles)
			{
				foreach (var roleAccess in role.RoleAccesses ?? new List<RoleAccess>())
				{
					roleAccesses.Add(new RoleAccessDto
					{
						Id = (long)roleAccess.Id!,
						Path = roleAccess.Path,
						ActionAccessType = roleAccess.ActionAccessType,
						ActionAccessItemType = roleAccess.ActionAccessItemType,
						EntityName = roleAccess.EntityName,
						DisplayName = roleAccess.DisplayName,
						EntityId = roleAccess.EntityId,
						RowId = roleAccess.RowId,
						RoleId = roleAccess.RoleId,
						RoleName = role.Name
					});
				}
			}
		}

		return new OnlineUserDto
		{
			UserId = (long)user.Id!,
			Username = user.Username,
			FullName = user.Name,
			ProfileImage = user.ProfileUrl,
			Roles = user.Roles ?? Array.Empty<string>(),
			RoleAccesses = roleAccesses,
			ConnectionIds = new List<string>(),
			ConnectedAt = DateTime.UtcNow,
			LastActivity = DateTime.UtcNow
		};
	}


	/// <summary>
	/// بارگذاری اطلاعات کاربر از دیتابیس
	/// </summary>
	private OnlineUserDto? LoadUserFromDatabase(long userId)
	{
		using var scope = _scopeFactory.CreateScope();
		 
	 
		var _db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		// دریافت کاربر
		var user =  _db.Users.AsNoTracking()
			.FirstOrDefault(u => u.Id == userId && u.IsActive == IsActiveEnum.Active);

		if (user == null)
			return null;

		// دریافت RoleAccess ها
		var roleAccesses = new List<RoleAccessDto>();

		if (user.Roles != null && user.Roles.Length > 0)
		{
			// دریافت Role ها با RoleAccesses
			var roles = _db.Roles.AsNoTracking()
				.Where(r => user.Roles.Contains(r.Name))
				.Include(r => r.RoleAccesses)
				.AsNoTracking()
				.ToList();

			// تبدیل RoleAccess ها به DTO
			foreach (var role in roles)
			{
				foreach (var roleAccess in role.RoleAccesses ?? new List<RoleAccess>())
				{
					roleAccesses.Add(new RoleAccessDto
					{
						Id = (long)roleAccess.Id!,
						Path = roleAccess.Path,
						ActionAccessType = roleAccess.ActionAccessType,
						ActionAccessItemType = roleAccess.ActionAccessItemType,
						EntityName = roleAccess.EntityName,
						DisplayName = roleAccess.DisplayName,
						EntityId = roleAccess.EntityId,
						RowId = roleAccess.RowId,
						RoleId = roleAccess.RoleId,
						RoleName = role.Name
					});
				}
			}
		}

		return new OnlineUserDto
		{
			UserId = (long)user.Id!,
			Username = user.Username,
			FullName = user.Name,
			ProfileImage = user.ProfileUrl,
			Roles = user.Roles ?? Array.Empty<string>(),
			RoleAccesses = roleAccesses,
			ConnectionIds = new List<string>(),
			ConnectedAt = DateTime.UtcNow,
			LastActivity = DateTime.UtcNow
		};
	}
	/// <summary>
	/// اضافه کردن userId به لیست کاربران آنلاین
	/// </summary>
	private async Task AddUserIdToOnlineListAsync(long userId, CancellationToken ct)
	{
		try
		{
			var userIdsKey = "OnlineUsers:UserIds";
			var userIdsJson = await _redis.GetStringAsync(userIdsKey, ct);
			
			var userIds = string.IsNullOrEmpty(userIdsJson)
				? new List<long>()
				: JsonSerializer.Deserialize<List<long>>(userIdsJson, JsonOptions) ?? new List<long>();

			if (!userIds.Contains(userId))
			{
				userIds.Add(userId);
				var updatedJson = JsonSerializer.Serialize(userIds, JsonOptions);
				await _redis.SetStringAsync(userIdsKey, updatedJson, new DistributedCacheEntryOptions
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
	/// اضافه کردن userId به لیست کاربران آنلاین
	/// </summary>
	private void AddUserIdToOnlineList(long userId)
	{
		try
		{
			var userIdsKey = "OnlineUsers:UserIds";
			var userIdsJson =   _redis.GetString(userIdsKey);

			var userIds = string.IsNullOrEmpty(userIdsJson)
				? new List<long>()
				: JsonSerializer.Deserialize<List<long>>(userIdsJson, JsonOptions) ?? new List<long>();

			if (!userIds.Contains(userId))
			{
				userIds.Add(userId);
				var updatedJson = JsonSerializer.Serialize(userIds, JsonOptions);
				  _redis.SetString(userIdsKey, updatedJson, new DistributedCacheEntryOptions
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
	/// حذف userId از لیست کاربران آنلاین
	/// </summary>
	private async Task RemoveUserIdFromOnlineListAsync(long userId, CancellationToken ct)
	{
		try
		{
			var userIdsKey = "OnlineUsers:UserIds";
			var userIdsJson = await _redis.GetStringAsync(userIdsKey, ct);
			
			if (string.IsNullOrEmpty(userIdsJson))
				return;

			var userIds = JsonSerializer.Deserialize<List<long>>(userIdsJson, JsonOptions);
			if (userIds == null)
				return;

			userIds.Remove(userId);

			if (userIds.Count == 0)
			{
				await _redis.RemoveAsync(userIdsKey, ct);
			}
			else
			{
				var updatedJson = JsonSerializer.Serialize(userIds, JsonOptions);
				await _redis.SetStringAsync(userIdsKey, updatedJson, new DistributedCacheEntryOptions
				{
					AbsoluteExpirationRelativeToNow = _expiration
				}, ct);
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
	private string GetUserKey(long userId)
	{
		return $"{_keyPrefix}User:{userId}";
	}

	public async Task<string[]>  GetUserRoleAsync(long userId, CancellationToken ct)
	{
		var user = await GetOnlineUserAsync(userId, ct);
		return user?.Roles ?? new string[0];
	}

	public string[] GetUserRole(long userId)
	{
		var user =   GetOnlineUser(userId);
		return user?.Roles ?? new string[0];
	}
}

