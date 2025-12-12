using Common.Auth.Enums;

namespace Data.SystemAuth;

/// <summary>
/// DTO برای کاربر آنلاین با RoleAccess ها
/// </summary>
public sealed class OnlineUserDto
{
	public long UserId { get; set; }
	public string Username { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string? ProfileImage { get; set; }
	public string[] Roles { get; set; } = Array.Empty<string>();
	public List<long> RoleIds { get; set; } = [];
	public List<RoleAccessDto> RoleAccesses { get; set; } = new();
	public List<string> ConnectionIds { get; set; } = new();
	public DateTime ConnectedAt { get; set; }
	public DateTime LastActivity { get; set; }
}

/// <summary>
/// DTO برای RoleAccess
/// </summary>
public sealed class RoleAccessDto
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

