using Common.Auth.Enums;
using Entities.Auth;
using Entities.Base;

namespace Services.AccessServices.DTOs;

/// <summary>
/// DTO برای Role بدون circular reference
/// </summary>
public sealed class RoleDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
     public IsActiveEnum IsActive { get; set; }
     public List<RoleAccessDto> RoleAccesses { get; set; } = new();
}

/// <summary>
/// DTO برای AccessPath بدون circular reference
/// </summary>
public sealed class RoleAccessDto
{
    public long Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
     public string EntityName { get; set; } = string.Empty;
     public long RowId { get; set; }
     public ActionAccessType ActionAccessType { get; set; }
     public ActionAccessItemType? ActionAccessItemType { get; set; }
}

/// <summary>
/// Extension methods برای تبدیل بین Entity و DTO
/// </summary>
public static class RoleExtensions
{
    public static RoleDto ToDto(this Role entity)
    {
        return new RoleDto
        {
            Id = (long)entity.Id,
            Name = entity.Name,
            DisplayName = entity.Title,
		   RoleAccesses = entity.RoleAccesses?.Select(a => a.ToDto()).ToList() ?? new List<RoleAccessDto>()
        };
    }

    public static RoleAccessDto ToDto(this RoleAccess entity)
    {
        return new RoleAccessDto
	   {
            Id = (long)entity.Id,
            Path = entity.Path,
            DisplayName = entity.DisplayName,
            ActionAccessItemType = entity.ActionAccessItemType,
            ActionAccessType = entity.ActionAccessType,
            RowId = entity.RoleId,
            EntityName = entity.EntityName
        };
    }

    public static List<RoleDto> ToDtoList(this IEnumerable<Role> entities)
    {
        return entities.Select(e => e.ToDto()).ToList();
    }
}
