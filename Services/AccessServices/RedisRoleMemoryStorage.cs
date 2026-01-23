using Entities.Auth;
using Microsoft.Extensions.Caching.Distributed;
using Services.AccessServices.DTOs;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;

namespace Services.AccessServices;

/// <summary>
/// Redis-backed implementation برای RoleMemoryStorage
/// </summary>
public sealed class RedisRoleMemoryStorage : IRoleMemoryStorage
{
    private readonly IDistributedCache _redis;
    private readonly string _key = "Roles:All";
    private readonly TimeSpan _expiration = TimeSpan.FromHours(24);

    // JsonSerializerOptions ساده (بدون circular reference چون از DTOs استفاده می‌کنیم)
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public RedisRoleMemoryStorage(IDistributedCache redis)
    {
        _redis = redis;
    }

    public void SetRoles(List<Role> roles)
    {
     
        var roleDtos = roles.ToDtoList();
        var json = JsonSerializer.Serialize(roleDtos, JsonOptions);
        _redis.SetString(_key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _expiration
        });
    }

    public List<Role> GetRoles()
    {
        var json = _redis.GetString(_key);
        if (string.IsNullOrEmpty(json))
            return new List<Role>();

        // Deserialize DTOs و تبدیل به entities
        var roleDtos = JsonSerializer.Deserialize<List<RoleDto>>(json, JsonOptions);
        if (roleDtos == null)
            return new List<Role>();

        // تبدیل DTOs به entities
        return roleDtos.Select(dto => new Role
        {
            Id = dto.Id,
            Name = dto.Name,
            Title = dto.DisplayName,
            IsActive = dto.IsActive,

            RoleAccesses = dto.RoleAccesses.Select(a => new RoleAccess
            {
                Id = a.Id,
                Path = a.Path,
                DisplayName = a.DisplayName,
                ActionAccessItemType = a.ActionAccessItemType,
                EntityName = a.EntityName,
                RoleId = a.RowId,
                ActionAccessType = a.ActionAccessType,

            }).ToList()
        }).ToList();
    }

    public bool HaveAccessByRole(string path, string roleName)
    {
        if (roleName == "admin")
            return true;

        var roles = GetRoles();
        return roles?.FirstOrDefault(c => c.Name == roleName)
            ?.RoleAccesses?.Any(c => c.Path == path) ?? false;
    }

	public bool HaveAccessByRole(string path, long roleId)
	{
		if (roleId == 1)
			return true;

		var roles = GetRoles();
		return roles?.FirstOrDefault(c => c.Id == roleId)
		    ?.RoleAccesses?.Any(c => c.Path == path) ?? false;
	}

	public Role GetRoleByName(string roleName)
    {
        var roles = GetRoles();
        return roles.FirstOrDefault(r => r.Name == roleName);
    }

    public Role? GetRoleBy(Func<Role, bool> predicate)
    {
        var roles = GetRoles();
        return roles.FirstOrDefault(predicate);
    }

    public Role GetRoleById(long roleId)
    {
        var roles = GetRoles();
        return roles.FirstOrDefault(r => r.Id == roleId);
    }

    public bool ExistPath(string path)
    {
        var roles = GetRoles();
        return roles.Any(c => c.RoleAccesses.Any(z => z.Path == path));
    }

    public void AddRole(Role role)
    {
        var roles = GetRoles();
        roles.Add(role);
        SetRoles(roles);
    }

    public void UpdateRoleAccessPaths(long roleId, List<string> accessPaths)
    {
        var roles = GetRoles();
        var role = roles.FirstOrDefault(r => r.Id == roleId);

        if (role == null)
            return;

        // لیست مسیرها را نرمال و یکتا می‌کنیم
        var normalizedPaths = accessPaths?
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        // حفظ اطلاعات دسترسی‌های قبلی در صورت وجود مسیر تکراری
        var currentAccesses = role.RoleAccesses ?? new List<RoleAccess>();
        role.RoleAccesses = normalizedPaths
            .Select(path =>
                currentAccesses.FirstOrDefault(a => a.Path?.Equals(path, StringComparison.OrdinalIgnoreCase) == true)
                ?? new RoleAccess { Path = path, RoleId = roleId })
            .ToList();

        SetRoles(roles);
    }
}

