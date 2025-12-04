using Entities.Auth;

namespace Services.AccessServices.DTOs;

/// <summary>
/// DTO برای AccessController بدون circular reference
/// </summary>
public sealed class AccessControllerDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<AccessActionDto> Actions { get; set; } = new();
}

/// <summary>
/// DTO برای AccessAction بدون circular reference
/// </summary>
public sealed class AccessActionDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
}

/// <summary>
/// Extension methods برای تبدیل بین Entity و DTO
/// </summary>
public static class AccessControllerExtensions
{
    public static AccessControllerDto ToDto(this AccessController entity)
    {
        return new AccessControllerDto
        {
            Id = entity.Id,
            Name = entity.Name,
            DisplayName = entity.DisplayName,
            Actions = entity.Actions?.Select(a => a.ToDto()).ToList() ?? new List<AccessActionDto>()
        };
    }

    public static AccessActionDto ToDto(this AccessAction entity)
    {
        return new AccessActionDto
        {
            Id = entity.Id,
            Name = entity.Name,
            DisplayName = entity.DisplayName,
            Path = entity.Path,
            HttpMethod = entity.HttpMethod
        };
    }

    public static List<AccessControllerDto> ToDtoList(this IEnumerable<AccessController> entities)
    {
        return entities.Select(e => e.ToDto()).ToList();
    }

    public static List<AccessActionDto> ToDtoList(this IEnumerable<AccessAction> entities)
    {
        return entities.Select(e => e.ToDto()).ToList();
    }
}
