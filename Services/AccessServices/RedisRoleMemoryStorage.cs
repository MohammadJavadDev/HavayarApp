using Data.Repositories;
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
    private readonly TimeSpan _snapshotTtl = TimeSpan.FromMinutes(2);
    private readonly object _sync = new();
    private RoleSnapshot? _snapshot;

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
        roles ??= new List<Role>();
        var roleDtos = roles.ToDtoList();
        var json = JsonSerializer.Serialize(roleDtos, JsonOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _expiration
        };

        lock (_sync)
        {
            _redis.SetString(_key, json, options);
            _snapshot = RoleSnapshot.Create(roles);
        }
    }

    public List<Role> GetRoles()
    {
        return GetSnapshot().Roles;
    }

    public bool HaveAccessByRole(string path, string roleName)
    {
        if (roleName == "admin")
            return true;

        if (string.IsNullOrEmpty(roleName) || string.IsNullOrEmpty(path))
            return false;

        var snapshot = GetSnapshot();
        return snapshot.ByName.TryGetValue(roleName, out var role)
            && role.Id is long roleId
            && snapshot.PathsByRoleId.TryGetValue(roleId, out var paths)
            && paths.Contains(path);
    }

	public bool HaveAccessByRole(string path, long roleId)
	{
		if (roleId == 1)
			return true;

		if (string.IsNullOrEmpty(path))
			return false;

		var snapshot = GetSnapshot();
		return snapshot.PathsByRoleId.TryGetValue(roleId, out var paths)
		    && paths.Contains(path);
	}

	public Role GetRoleByName(string roleName)
    {
        if (string.IsNullOrEmpty(roleName))
            return null;

        GetSnapshot().ByName.TryGetValue(roleName, out var role);
        return role;
    }

    public Role? GetRoleBy(Func<Role, bool> predicate)
    {
        return GetSnapshot().Roles.FirstOrDefault(predicate);
    }

    public Role GetRoleById(long roleId)
    {
        GetSnapshot().ById.TryGetValue(roleId, out var role);
        return role;
    }

    public bool ExistPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        return GetSnapshot().AllPaths.Contains(path);
    }

    private RoleSnapshot GetSnapshot()
    {
        var current = _snapshot;
        if (current != null && DateTime.UtcNow - current.LoadedUtc < _snapshotTtl)
            return current;

        lock (_sync)
        {
            current = _snapshot;
            if (current != null && DateTime.UtcNow - current.LoadedUtc < _snapshotTtl)
                return current;

            current = RoleSnapshot.Create(ReadRolesFromRedis());
            _snapshot = current;
            return current;
        }
    }

    private List<Role> ReadRolesFromRedis()
    {
        var json = _redis.GetString(_key);
        if (string.IsNullOrEmpty(json))
            return new List<Role>();

        var roleDtos = JsonSerializer.Deserialize<List<RoleDto>>(json, JsonOptions);
        if (roleDtos == null)
            return new List<Role>();

        return roleDtos.Select(dto => new Role
        {
            Id = dto.Id,
            Name = dto.Name,
            Title = dto.DisplayName,
            IsActive = dto.IsActive,
            RoleAccesses = (dto.RoleAccesses ?? new List<RoleAccessDto>()).Select(a => new RoleAccess
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

    private sealed class RoleSnapshot
    {
        public required List<Role> Roles { get; init; }
        public required Dictionary<long, Role> ById { get; init; }
        public required Dictionary<string, Role> ByName { get; init; }
        public required Dictionary<long, HashSet<string>> PathsByRoleId { get; init; }
        public required HashSet<string> AllPaths { get; init; }
        public DateTime LoadedUtc { get; init; }

        public static RoleSnapshot Create(List<Role> roles)
        {
            roles ??= new List<Role>();
            var byId = new Dictionary<long, Role>();
            var byName = new Dictionary<string, Role>(StringComparer.Ordinal);
            var pathsByRoleId = new Dictionary<long, HashSet<string>>();
            var allPaths = new HashSet<string>(StringComparer.Ordinal);

            foreach (var role in roles)
            {
                if (role == null)
                    continue;

                if (role.Id is long id && id > 0 && !byId.ContainsKey(id))
                    byId[id] = role;

                if (!string.IsNullOrEmpty(role.Name) && !byName.ContainsKey(role.Name))
                    byName[role.Name] = role;

                if (role.Id is not long roleId || roleId <= 0)
                    continue;

                var paths = new HashSet<string>(StringComparer.Ordinal);
                if (role.RoleAccesses != null)
                {
                    foreach (var access in role.RoleAccesses)
                    {
                        if (string.IsNullOrEmpty(access?.Path))
                            continue;

                        paths.Add(access.Path);
                        allPaths.Add(access.Path);
                    }
                }

                pathsByRoleId[roleId] = paths;
            }

            return new RoleSnapshot
            {
                Roles = roles,
                ById = byId,
                ByName = byName,
                PathsByRoleId = pathsByRoleId,
                AllPaths = allPaths,
                LoadedUtc = DateTime.UtcNow
            };
        }
    }

    public void AddRole(Role role)
    {
        var roles = GetRoles().ToList();
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

