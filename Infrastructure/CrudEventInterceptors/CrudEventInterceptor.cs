using Entities.Auth;
using Entities.Base;
using Entities.Base.Menu;
using Entities.Base.Notification;
using Entities.Base.NotifitactionBuilder;
using Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Linq;

namespace Infrastructure.CrudEventInterceptors;

/// <summary>
/// Interceptor برای publish رویدادهای CRUD به RabbitMQ
/// از SavedChangesAsync استفاده می‌کند تا EntityId بعد از generate شدن موجود باشد
/// </summary>
public sealed class CrudEventInterceptor(ICrudEventPublisher publisher) : SaveChangesInterceptor
{
    private readonly List<PendingEvent> _pendingEvents = new();

	private readonly List<Type> _notificationIgnoreTypes = new List<Type>
    {
		typeof(SystemMenu),
		typeof(User),
		typeof(Role),
		typeof(PropertyIdentity),
		typeof(Notification),
		 typeof(NotificationBuidler)
    };

	public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        // ذخیره state entities قبل از SaveChanges
        _pendingEvents.Clear();
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity &&
                   (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToArray();

        foreach (var entry in entries)
        {
               if(_notificationIgnoreTypes.Contains(entry.Entity.GetType()))
               {
                    continue;
               }
            var operation = entry.State switch
            {
                EntityState.Added => "Create",
                EntityState.Modified => "Update",
                EntityState.Deleted => "Delete",
                _ => null
            };

            if (operation != null)
            {
                _pendingEvents.Add(new PendingEvent
                {
                    Entry = entry,
                    Operation = operation,
                    EntityName = entry.Entity.GetType().FullName
                });
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        // بعد از SaveChanges، Id موجود است - حالا publish می‌کنیم
        foreach (var pending in _pendingEvents)
        {
            try
            {
                // حالا می‌توانیم Id را بخوانیم (برای Added state هم generate شده)
                var pkProperty = pending.Entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                var entityId = pkProperty?.CurrentValue?.ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(entityId))
                {
                    // جمع‌آوری metadata اضافی
                    var metadata = new Dictionary<string, string>
                    {
                        ["UserId"] = pending.Entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedById")?.CurrentValue?.ToString() ?? "",
                        ["UserName"] = pending.Entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedByName")?.CurrentValue?.ToString() ?? "",
                        ["Timestamp"] = DateTime.UtcNow.ToString("O")
                    };

                    // Publish event (fire-and-forget)
                    _ = publisher.PublishEntityChangedAsync(
                        operation: pending.Operation,
                        entityName: pending.EntityName,
                        entityId: entityId,
                        metadata: metadata,
                        ct: cancellationToken
                    );
                }
            }
            catch
            {
                // ignore publish errors - don't break the save operation
            }
        }

        _pendingEvents.Clear();
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private sealed class PendingEvent
    {
        public required EntityEntry Entry { get; init; }
        public required string Operation { get; init; }
        public required string EntityName { get; init; }
    }
}


