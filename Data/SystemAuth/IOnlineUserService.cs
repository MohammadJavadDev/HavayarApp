

using Entities.Auth;

namespace Data.SystemAuth;

/// <summary>
/// سرویس مدیریت کاربران آنلاین و RoleAccess هایشان
/// </summary>
public interface IOnlineUserService
{
	/// <summary>
	/// اضافه کردن کاربر آنلاین
	/// </summary>
	Task AddOnlineUserAsync(long userId, string? connectionId, CancellationToken ct = default);

	/// <summary>
	/// اضافه کردن کاربر آنلاین
	/// </summary>
	OnlineUserDto? AddOnlineUser(long userId, string? connectionId);

	/// <summary>
	/// حذف کاربر آنلاین
	/// </summary>
	Task RemoveOnlineUserAsync(long userId, string connectionId, CancellationToken ct = default);

	/// <summary>
	/// دریافت لیست تمام کاربران آنلاین
	/// </summary>
	Task<List<OnlineUserDto>> GetAllOnlineUsersAsync(CancellationToken ct = default);

	/// <summary>
	/// دریافت اطلاعات یک کاربر آنلاین
	/// </summary>
	Task<OnlineUserDto?> GetOnlineUserAsync(long userId, CancellationToken ct = default);


	/// <summary>
	/// دریافت اطلاعات یک کاربر آنلاین
	/// </summary>
	OnlineUserDto? GetOnlineUser(long userId);
	/// <summary>
	/// بررسی اینکه آیا کاربر آنلاین است یا نه
	/// </summary>
	Task<bool> IsUserOnlineAsync(long userId, CancellationToken ct = default);

	/// <summary>
	/// دریافت تعداد کاربران آنلاین
	/// </summary>
	Task<int> GetOnlineUsersCountAsync(CancellationToken ct = default);

	/// <summary>
	/// دریافت RoleAccess های یک کاربر
	/// </summary>
	Task<List<RoleAccessDto>> GetUserRoleAccessesAsync(long userId, CancellationToken ct = default);

	/// <summary>
	/// دریافت RoleAccess های یک کاربر
	/// </summary>
	Task<string[]> GetUserRoleAsync(long userId, CancellationToken ct = default);



	/// <summary>
	/// دریافت RoleAccess های یک کاربر
	/// </summary>
	string[] GetUserRole(long userId);


	/// <summary>
	/// دریافت RoleAccess های یک کاربر
	/// </summary>
	List<RoleAccessDto> GetUserRoleAccesses(long userId);

	/// <summary>
	/// بررسی دسترسی کاربر به یک path
	/// </summary>
	Task<bool> HasUserAccessAsync(long userId, string path, CancellationToken ct = default);

	/// <summary>
	/// به‌روزرسانی RoleAccess های یک کاربر (مثلاً بعد از تغییر نقش)
	/// </summary>
	Task RefreshUserRoleAccessesAsync(long userId, CancellationToken ct = default);
}

