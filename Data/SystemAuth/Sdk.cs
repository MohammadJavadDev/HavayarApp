using Common.Utilities;
using Data.SystemAuth;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Data.SystemAuth;
public class Sdk : ISdk
{
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly IOnlineUserService _onlineUserService;
	private CurrentUser? _currentUser;
	private bool _isLoaded = false;

	private HashSet<string>? _currentUserRolesSet;
	private HashSet<long>? _currentUserRoleIdsSet;

	public Sdk(IHttpContextAccessor httpContextAccessor,
		    IOnlineUserService onlineUserService)
	{
		_httpContextAccessor = httpContextAccessor;
		_onlineUserService = onlineUserService;
		Authenticated = _httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
	}

	public CurrentUser? CurrentUser
	{
		get
		{
			if (!_isLoaded)
			{
				LoadCurrentUser();
				_isLoaded = true;
			}
			return _currentUser;
		}
		private set => _currentUser = value;
	}

	public bool Authenticated { get; private set; }

	public bool IsAdministrator => CurrentUser?.IsAdministrator ?? false;

	private void LoadCurrentUser()
	{
		try
		{
			var httpContext = _httpContextAccessor?.HttpContext;
			if (httpContext == null)
			{
				Authenticated = false;
				return;
			}
			var userIdentity = httpContext.User?.Identity;
			if (userIdentity == null || !userIdentity.IsAuthenticated)
			{
				Authenticated = false;
				return;
			}
			var userId = userIdentity.GetUserId();
			if (userId <= 0)
			{
				Authenticated = false;
				return;
			}
			var onlineUser = _onlineUserService.GetOnlineUser(userId);
			if (onlineUser == null)
			{
				Authenticated = false;
				return;
			}
			_currentUser = new CurrentUser
			{
				Id = userId,
				Username = onlineUser.Username,
				ProfileImage = onlineUser.ProfileImage,
				FullName = onlineUser.FullName,
				OrganizationUnitId = onlineUser.OrganizationUnitId,
				FullNameFn = onlineUser.FullNameFn,
				Email = onlineUser.Email,
				IpAddress = httpContext.Connection.RemoteIpAddress,
				RoleAccess = onlineUser.RoleAccesses ?? new List<RoleAccessDto>(),
				Roles = onlineUser.Roles ?? new List<string>(),
				RoleIds = onlineUser.RoleIds ?? new List<long>(),
				IsAdministrator = onlineUser.Roles?.Any(c => c == "admin") ?? false,
			};
			Authenticated = true;

			// Clear cached role sets
			_currentUserRolesSet = null;
			_currentUserRoleIdsSet = null;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error loading current user: {ex.Message}");
			Authenticated = false;
		}
	}

	public void RefreshUserRole()
	{
		if (_currentUser == null)
			return;
		_onlineUserService.RefreshUserRoleAccesses(_currentUser.Id);
		// Force reload
		_isLoaded = false;
		_currentUserRolesSet = null;
		_currentUserRoleIdsSet = null;
		LoadCurrentUser();
	}

	#region Role Checking Methods

	private HashSet<string> CurrentUserRolesSet
	{
		get
		{
			if (_currentUserRolesSet == null && CurrentUser?.Roles != null)
			{
				_currentUserRolesSet = new HashSet<string>(
				    CurrentUser.Roles,
				    StringComparer.OrdinalIgnoreCase
				);
			}
			return _currentUserRolesSet ?? new HashSet<string>();
		}
	}

	private HashSet<long> CurrentUserRoleIdsSet
	{
		get
		{
			if (_currentUserRoleIdsSet == null && CurrentUser?.RoleIds != null)
			{
				_currentUserRoleIdsSet = new HashSet<long>(CurrentUser.RoleIds);
			}
			return _currentUserRoleIdsSet ?? new HashSet<long>();
		}
	}

	public bool HasRole(string roleName)
	{
		if (!Authenticated || string.IsNullOrEmpty(roleName))
			return false;
		return CurrentUserRolesSet.Contains(roleName);
	}

	public bool HasRole(long roleId)
	{
		if (!Authenticated)
			return false;
		return CurrentUserRoleIdsSet.Contains(roleId);
	}

	public bool HasAnyRole(params string[] roleNames)
	{
		if (!Authenticated || roleNames == null || roleNames.Length == 0)
			return false;
		return roleNames.Any(roleName => CurrentUserRolesSet.Contains(roleName));
	}

	public bool HasAllRoles(params string[] roleNames)
	{
		if (!Authenticated || roleNames == null || roleNames.Length == 0)
			return false;
		return roleNames.All(roleName => CurrentUserRolesSet.Contains(roleName));
	}

	#endregion
}
public class CurrentUser
{
     public long Id { get; set; }
	public long? OrganizationUnitId { get; set; }
	public string? Username { get; set; } 
     public string? FullName { get; set; }
	public string? FullNameFn { get; set; }
	public string? Email { get; set; }
	public List<string> Roles { get; set; } = [];
	public List<long> RoleIds { get; set; } = [];
     public string? ProfileImage { get; set; }
     public IPAddress? IpAddress { get; set; }
	public List<RoleAccessDto> RoleAccess { get; set; } = new();
     public bool IsAdministrator { get; set; } = false;
}

public static class SdkClientSerializer
{
	// فقط فیلدهایی که واقعاً سمت کلاینت لازم هستن
	public static string SerializeForClient(CurrentUser? user, bool authenticated)
	{
		if (!authenticated || user == null)
		{
			var empty = new { _x = 0, _a = false };
			return Convert.ToBase64String(
			    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(empty))
			);
		}

		// کلیدها عمداً مبهم هستن - هیچ اسم معنی‌داری ندارن
		var payload = new
		{
			_x = user.Id,                        // userId
			_a = user.IsAdministrator,            // isAdmin
			_b = user.Username ?? "",             // username
			_c = user.FullName ?? "",             // fullName
			_d = user.FullNameFn ?? "",           // fullNameFn
			_e = user.Email ?? "",                // email
			_f = user.ProfileImage ?? "",         // profileImage
			_g = user.OrganizationUnitId,         // orgUnitId
			_h = user.Roles ?? [],                // roles
			_i = user.RoleIds ?? [],              // roleIds
										   // RoleAccess: فقط Path و نوع دسترسی - بدون نام‌های معنی‌دار
			_j = (user.RoleAccess ?? []).Select(r => new
			{
				p = r.Path,
				t = (int)r.ActionAccessType,
				it = r.ActionAccessItemType.HasValue ? (int?)r.ActionAccessItemType.Value : null,
				e = r.EntityName,
				ei = r.EntityId,
				ri = r.RowId,
				n = r.RoleName
			}).ToList()
		};

		var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
		{
			PropertyNamingPolicy = null // دقیقاً همون کلیدهای _ prefix
		});

		// Base64 → جلوگیری از خوانایی مستقیم در سورس
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
	}
}