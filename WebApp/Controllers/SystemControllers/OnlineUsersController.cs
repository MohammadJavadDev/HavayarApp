using Data.SystemAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
 
namespace WebApp.Controllers.SystemControllers;

/// <summary>
/// Controller برای مدیریت کاربران آنلاین
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OnlineUsersController : ControllerBase
{
	private readonly IOnlineUserService _onlineUserService;

	public OnlineUsersController(IOnlineUserService onlineUserService)
	{
		_onlineUserService = onlineUserService;
	}

	/// <summary>
	/// دریافت لیست تمام کاربران آنلاین
	/// GET /api/onlineusers
	/// </summary>
	[HttpGet]
	public async Task<ActionResult<List<OnlineUserDto>>> GetAllOnlineUsers()
	{
		var users = await _onlineUserService.GetAllOnlineUsersAsync();
		return Ok(users);
	}

	/// <summary>
	/// دریافت اطلاعات یک کاربر آنلاین
	/// GET /api/onlineusers/{userId}
	/// </summary>
	[HttpGet("{userId}")]
	public async Task<ActionResult<OnlineUserDto>> GetOnlineUser(long userId)
	{
		var user = await _onlineUserService.GetOnlineUserAsync(userId);
		if (user == null)
			return NotFound($"User {userId} is not online");

		return Ok(user);
	}

	/// <summary>
	/// بررسی اینکه آیا کاربر آنلاین است یا نه
	/// GET /api/onlineusers/{userId}/is-online
	/// </summary>
	[HttpGet("{userId}/is-online")]
	public async Task<ActionResult<bool>> IsUserOnline(long userId)
	{
		var isOnline = await _onlineUserService.IsUserOnlineAsync(userId);
		return Ok(isOnline);
	}

	/// <summary>
	/// دریافت تعداد کاربران آنلاین
	/// GET /api/onlineusers/count
	/// </summary>
	[HttpGet("count")]
	public async Task<ActionResult<int>> GetOnlineUsersCount()
	{
		var count = await _onlineUserService.GetOnlineUsersCountAsync();
		return Ok(count);
	}

	/// <summary>
	/// دریافت RoleAccess های یک کاربر
	/// GET /api/onlineusers/{userId}/role-accesses
	/// </summary>
	[HttpGet("{userId}/role-accesses")]
	public async Task<ActionResult<List<RoleAccessDto>>> GetUserRoleAccesses(long userId)
	{
		var roleAccesses = await _onlineUserService.GetUserRoleAccessesAsync(userId);
		return Ok(roleAccesses);
	}

	/// <summary>
	/// بررسی دسترسی کاربر به یک path
	/// GET /api/onlineusers/{userId}/has-access?path=/api/users
	/// </summary>
	[HttpGet("{userId}/has-access")]
	public async Task<ActionResult<bool>> HasUserAccess(long userId, [FromQuery] string path)
	{
		if (string.IsNullOrEmpty(path))
			return BadRequest("Path is required");

		var hasAccess = await _onlineUserService.HasUserAccessAsync(userId, path);
		return Ok(hasAccess);
	}

	/// <summary>
	/// به‌روزرسانی RoleAccess های یک کاربر (مثلاً بعد از تغییر نقش)
	/// POST /api/onlineusers/{userId}/refresh-role-accesses
	/// </summary>
	[HttpPost("{userId}/refresh-role-accesses")]
	[Authorize(Policy = "admin")] // فقط admin می‌تواند refresh کند
	public async Task<ActionResult> RefreshUserRoleAccesses(long userId)
	{
		await _onlineUserService.RefreshUserRoleAccessesAsync(userId);
		return Ok(new { message = $"Role accesses refreshed for user {userId}" });
	}
}


