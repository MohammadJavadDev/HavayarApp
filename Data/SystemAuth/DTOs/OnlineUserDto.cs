using Common.Auth.Enums;
using System.Collections.Generic;

namespace Data.SystemAuth;

/// <summary>
/// DTO برای کاربر آنلاین با RoleAccess ها
/// </summary>
public  class OnlineUserDto
{
	public long UserId { get; set; }
	public string Username { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string FullNameFn { get; set; } = string.Empty;
	public string? Email { get; set; }
	public long? OrganizationUnitId { get; set; }
	public string? ProfileImage { get; set; }
	public List<string> Roles { get; set; } = [];
	public List<long> RoleIds { get; set; } = [];
	public List<RoleAccessDto> RoleAccesses { get; set; } = new();
	public List<string> ConnectionIds { get; set; } = new();
	public DateTime ConnectedAt { get; set; }
	public DateTime LastActivity { get; set; }
	public string? CurrentPagePath { get; set; }
	public string? CurrentPageTitle { get; set; }
	public Dictionary<string, List<OnlineUserPageDto>> PagesByConnection { get; set; }
	    = new();
}

public class OnlineUserPageDto
{
	public string Path { get; set; } = default!;
	public string? Title { get; set; }

	public DateTime OpenedAt { get; set; }
	public DateTime LastActive { get; set; }
}

/// <summary>
/// DTO برای RoleAccess
/// </summary>
public  class RoleAccessDto
{
	public long Id { get; set; }
	public string? Path { get; set; }
	public ActionAccessType ActionAccessType { get; set; }
	public ActionAccessItemType? ActionAccessItemType { get; set; }
	public string? EntityName { get; set; }
	public string? DisplayName { get; set; }
	public long? EntityId { get; set; }
	public long? RowId { get; set; }
	public long RoleId { get; set; }
	public string RoleName { get; set; } = string.Empty;
}

