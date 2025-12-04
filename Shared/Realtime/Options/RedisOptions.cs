namespace Shared.Realtime.Options;

/// <summary>
/// تنظیمات Redis برای Caching و SignalR Backplane
/// </summary>
public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; init; } = "localhost:6379";
    public string InstanceName { get; init; } = "HavayarApp:";
    public int DefaultExpirationMinutes { get; init; } = 60;

    // SignalR backplane
    public bool EnableSignalRBackplane { get; init; } = true;

    // Database index برای هر cache
    public int NotificationRuleCacheDb { get; init; } = 0;
    public int EntityMetadataCacheDb { get; init; } = 1;
    public int RoleMemoryStorageDb { get; init; } = 2;
    public int AccessMemoryStorageDb { get; init; } = 3;
    public int MenuBuilderCacheDb { get; init; } = 4;
    public int DataTableProfileCacheDb { get; init; } = 5;
}

