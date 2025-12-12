using Common.Entities.EntityMetadatas;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.InMemoryData;

/// <summary>
/// Redis-backed implementation برای EntityMetadataCache
/// </summary>
public sealed class RedisEntityMetadataCache : IEntityMetadataCache
{
    private readonly IDistributedCache _redis;
    private readonly EntityMetadataCache _fallback; // برای build کردن از assembly
    private readonly string _keyPrefix = "EntityMetadata:";
    private readonly string _allEntitiesKey = "EntityMetadata:AllEntities";
    private readonly TimeSpan _expiration = TimeSpan.FromHours(24);

    // JsonSerializerOptions برای حل circular reference
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.Preserve,
        WriteIndented = false
    };

    public RedisEntityMetadataCache(IDistributedCache redis , EntityMetadataCache entityMetadataCache)
    {
        _redis = redis;
        _fallback = entityMetadataCache; // برای build اولیه
    }

    public EntityMetadata? Get(string entityName)
    {
        var key = $"{_keyPrefix}{entityName.ToLower()}";
        var json = _redis.GetString(key);

        if (!string.IsNullOrEmpty(json))
        {
            return JsonSerializer.Deserialize<EntityMetadata>(json, JsonOptions);
        }

        // اگر در Redis نبود، از fallback بگیر و در Redis ذخیره کن
        var metadata = _fallback.Get(entityName);
        if (metadata != null)
        {
            var metadataJson = JsonSerializer.Serialize(metadata, JsonOptions);
            _redis.SetString(key, metadataJson, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _expiration
            });
        }

        return metadata;
    }

    public IReadOnlyCollection<EntityMetadata> GetAll()
    {
        var json = _redis.GetString(_allEntitiesKey);

        if (!string.IsNullOrEmpty(json))
        {
            var cached = JsonSerializer.Deserialize<List<EntityMetadata>>(json, JsonOptions);
            if (cached != null)
                return cached;
        }

        // اگر در Redis نبود، از fallback بگیر و در Redis ذخیره کن
        var all = _fallback.GetAll();
        var allJson = JsonSerializer.Serialize(all, JsonOptions);
        _redis.SetString(_allEntitiesKey, allJson, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _expiration
        });

        return all;
    }

	public void Refresh()
	{
		// refresh fallback
		_fallback.Refresh();

		// write AllEntities
		var all = _fallback.GetAll();
		var allJson = JsonSerializer.Serialize(all, JsonOptions);

		_redis.SetString(_allEntitiesKey, allJson, new DistributedCacheEntryOptions
		{
			AbsoluteExpirationRelativeToNow = _expiration
		});

		// write individual entities (no Remove!)
		foreach (var metadata in all)
		{
              
			var key = $"{_keyPrefix}{metadata.EntityName.ToLower()}";
			var json = JsonSerializer.Serialize(metadata, JsonOptions);

			_redis.SetString(key, json, new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = _expiration
			});
		}
	}
}

