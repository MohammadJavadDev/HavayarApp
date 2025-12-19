using Entities.Auth;
using Microsoft.Extensions.Caching.Distributed;
using Services.AccessServices.DTOs;
using System.Text.Json;

namespace Services.AccessServices;

public class AccessMemoryStorage : IAccessMemoryStorage
{
 
    private List<AccessController> _accessControllers;
    private List<AccessAction> _accessAction;
    private List<string> _accessPaths;

	private readonly IDistributedCache _redis;
	private readonly string _controllersKey = "Access:Controllers";
	private readonly string _actionsKey = "Access:Actions";
	private readonly string _pathsKey = "Access:Paths";
	private readonly string _controllerByFullNamePrefix = "Access:Controller:";
	private readonly TimeSpan _expiration = TimeSpan.FromHours(24);

	// JsonSerializerOptions ساده (بدون circular reference چون از DTOs استفاده می‌کنیم)
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = false
	};

	public AccessMemoryStorage(IDistributedCache redis)
    {
 
        _accessControllers = new List<AccessController>();
        _accessPaths = new List<string>();
        _accessAction = new List<AccessAction>();
		_redis = redis;
	}

 
    public List<AccessController> GetAllAccessControllers()
    {
        return _accessControllers;
    }
 
    public void SetAccessControllers(List<AccessController> accessControllers)
    {
        _accessControllers = accessControllers;
        _accessAction = accessControllers.SelectMany(c => c.Actions).ToList();
        _accessPaths = accessControllers.SelectMany(c => c.Actions).ToList()
            .Select(c=>c.Path).ToList();
    }

    public bool ExistPath(string path)
    {
        
        return _accessPaths.Any(c=>c == path);
    }

    public AccessAction? GetAccessAction(string path)
    {
  
        return _accessAction.FirstOrDefault(x => x.Path.ToLower().Equals(path.ToLower()));
    }

	AccessController? IAccessMemoryStorage.GetAccessControllerBy(Type type)
	{
          return _accessControllers.FirstOrDefault(x => x.EntityType == type);
	}

	AccessController? IAccessMemoryStorage.GetAccessControllerBy(string fullName)
	{
		return _accessControllers.FirstOrDefault(x => x.EntityType.FullName == fullName);
	}


	public List<AccessController> GetAllAccessControllersRedis()
	{
		var json = _redis.GetString(_controllersKey);
		if (string.IsNullOrEmpty(json))
			return new List<AccessController>();

		// Deserialize DTOs و تبدیل به entities
		var dtos = JsonSerializer.Deserialize<List<AccessControllerDto>>(json, JsonOptions);
		if (dtos == null)
			return new List<AccessController>();

		// تبدیل DTOs به entities
		return dtos.Select(dto => DtoToEntity(dto)).ToList();
	}

	public void SetAccessControllersRedis(List<AccessController> accessControllers)
	{
		// تبدیل entities به DTOs
		var controllerDtos = accessControllers.ToDtoList();
		var actionDtos = accessControllers.SelectMany(c => c.Actions).ToDtoList();

		var cacheOptions = new DistributedCacheEntryOptions
		{
			AbsoluteExpirationRelativeToNow = _expiration
		};

		// ذخیره controllers به صورت لیست (برای GetAllAccessControllers)
		var controllersJson = JsonSerializer.Serialize(controllerDtos, JsonOptions);
		_redis.SetString(_controllersKey, controllersJson, cacheOptions);

		// ذخیره هر controller به صورت جداگانه برای دسترسی سریع (بدون نیاز به بارگذاری همه)
		foreach (var dto in controllerDtos)
		{
			if (!string.IsNullOrEmpty(dto.EntityTypeFullName))
			{
				var controllerKey = GetControllerKeyByFullName(dto.EntityTypeFullName);
				var controllerJson = JsonSerializer.Serialize(dto, JsonOptions);
				_redis.SetString(controllerKey, controllerJson, cacheOptions);
			}
		}

		// ذخیره actions (DTOs)
		var actionsJson = JsonSerializer.Serialize(actionDtos, JsonOptions);
		_redis.SetString(_actionsKey, actionsJson, cacheOptions);

		// ذخیره paths
		var paths = actionDtos.Select(c => c.Path).ToList();
		var pathsJson = JsonSerializer.Serialize(paths, JsonOptions);
		_redis.SetString(_pathsKey, pathsJson, cacheOptions);
	}

	public bool ExistPathRedis(string path)
	{
		var json = _redis.GetString(_pathsKey);
		if (string.IsNullOrEmpty(json))
			return false;

		var paths = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
		return paths?.Any(c => c == path) ?? false;
	}

	public AccessAction? GetAccessActionRedis(string path)
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

	AccessController? IAccessMemoryStorage.GetAccessControllerByRedis(Type type)
	{
		if (type == null)
			return null;

		return ((IAccessMemoryStorage)this).GetAccessControllerByRedis(type.FullName ?? string.Empty);
	}

	AccessController? IAccessMemoryStorage.GetAccessControllerByRedis(string fullName)
	{
		if (string.IsNullOrEmpty(fullName))
			return null;

		// دریافت مستقیم از Redis بدون نیاز به بارگذاری همه controllers
		var controllerKey = GetControllerKeyByFullName(fullName);
		var json = _redis.GetString(controllerKey);

		if (string.IsNullOrEmpty(json))
			return null;

		var dto = JsonSerializer.Deserialize<AccessControllerDto>(json, JsonOptions);
		if (dto == null)
			return null;

		return DtoToEntity(dto);
	}

	private string GetControllerKeyByFullName(string fullName)
	{
		return $"{_controllerByFullNamePrefix}{fullName}";
	}

	private static AccessController DtoToEntity(AccessControllerDto dto)
	{
		Type? entityType = null;
		if (!string.IsNullOrEmpty(dto.EntityTypeFullName))
		{
			// تلاش برای یافتن Type از طریق FullName
			entityType = Type.GetType(dto.EntityTypeFullName);

			// اگر پیدا نشد، در تمام اسمبلی‌های لود شده جستجو کن
			if (entityType == null)
			{
				entityType = AppDomain.CurrentDomain.GetAssemblies()
				    .SelectMany(assembly =>
				    {
					    try
					    {
						    return assembly.GetTypes();
					    }
					    catch
					    {
						    return Array.Empty<Type>();
					    }
				    })
				    .FirstOrDefault(t => t.FullName == dto.EntityTypeFullName);
			}
		}

		return new AccessController
		{
			Id = dto.Id,
			Name = dto.Name,
			DisplayName = dto.DisplayName,
			Path = dto.Path,
			EntityType = entityType,
			Actions = dto.Actions.Select(a => new AccessAction
			{
				Id = a.Id,
				Name = a.Name,
				DisplayName = a.DisplayName,
				Path = a.Path,
				HttpMethod = a.HttpMethod,
				ActionAccessType =a.ActionAccessType,
				ActionAccessItemType = a.ActionAccessItemType
			}).ToList()
		};
	}
}