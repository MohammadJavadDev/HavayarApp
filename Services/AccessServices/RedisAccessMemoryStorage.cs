using Entities.Auth;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Services.AccessServices.DTOs;

namespace Services.AccessServices;

/// <summary>
/// Redis-backed implementation برای AccessMemoryStorage
/// </summary>
public sealed class RedisAccessMemoryStorage : IAccessMemoryStorage
{
    private readonly IDistributedCache _redis;
    private readonly string _controllersKey = "Access:Controllers";
    private readonly string _actionsKey = "Access:Actions";
    private readonly string _pathsKey = "Access:Paths";
    private readonly TimeSpan _expiration = TimeSpan.FromHours(24);

    // JsonSerializerOptions ساده (بدون circular reference چون از DTOs استفاده می‌کنیم)
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public RedisAccessMemoryStorage(IDistributedCache redis)
    {
        _redis = redis;
    }

    public List<AccessController> GetAllAccessControllers()
    {
        var json = _redis.GetString(_controllersKey);
        if (string.IsNullOrEmpty(json))
            return new List<AccessController>();

        // Deserialize DTOs و تبدیل به entities
        var dtos = JsonSerializer.Deserialize<List<AccessControllerDto>>(json, JsonOptions);
        if (dtos == null)
            return new List<AccessController>();

        // تبدیل DTOs به entities (ساده‌سازی شده)
        return dtos.Select(dto => new AccessController
        {
            Id = dto.Id,
            Name = dto.Name,
            DisplayName = dto.DisplayName,
            Actions = dto.Actions.Select(a => new AccessAction
            {
                Id = a.Id,
                Name = a.Name,
                DisplayName = a.DisplayName,
                Path = a.Path,
                HttpMethod = a.HttpMethod
            }).ToList()
        }).ToList();
    }

    public void SetAccessControllers(List<AccessController> accessControllers)
    {
        // تبدیل entities به DTOs
        var controllerDtos = accessControllers.ToDtoList();
        var actionDtos = accessControllers.SelectMany(c => c.Actions).ToDtoList();

        // ذخیره controllers (DTOs)
        var controllersJson = JsonSerializer.Serialize(controllerDtos, JsonOptions);
        _redis.SetString(_controllersKey, controllersJson, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _expiration
        });

        // ذخیره actions (DTOs)
        var actionsJson = JsonSerializer.Serialize(actionDtos, JsonOptions);
        _redis.SetString(_actionsKey, actionsJson, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _expiration
        });

        // ذخیره paths
        var paths = actionDtos.Select(c => c.Path).ToList();
        var pathsJson = JsonSerializer.Serialize(paths, JsonOptions);
        _redis.SetString(_pathsKey, pathsJson, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _expiration
        });
    }

    public bool ExistPath(string path)
    {
        var json = _redis.GetString(_pathsKey);
        if (string.IsNullOrEmpty(json))
            return false;

        var paths = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
        return paths?.Any(c => c == path) ?? false;
    }

    public AccessAction? GetAccessAction(string path)
    {
        var json = _redis.GetString(_actionsKey);
        if (string.IsNullOrEmpty(json))
            return null;

        var actionDtos = JsonSerializer.Deserialize<List<AccessActionDto>>(json, JsonOptions);
        var actionDto = actionDtos?.FirstOrDefault(x => x.Path.ToLower().Equals(path.ToLower()));

        if (actionDto == null)
            return null;

        // تبدیل DTO به entity
        return new AccessAction
        {
            Id = actionDto.Id,
            Name = actionDto.Name,
            DisplayName = actionDto.DisplayName,
            Path = actionDto.Path,
            HttpMethod = actionDto.HttpMethod
        };
    }

	AccessController? IAccessMemoryStorage.GetAccessControllerBy(Type type)
	{
		throw new NotImplementedException();
	}

	AccessController? IAccessMemoryStorage.GetAccessControllerBy(string fullName)
	{
		throw new NotImplementedException();
	}
}

