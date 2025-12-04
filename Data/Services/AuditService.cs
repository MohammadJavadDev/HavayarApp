using Common;
using Common.Attributes;
using Common.System;
using Common.Utilities;
using Data.Contracts;
using Data.SystemAuth;
using Entities.Auth;
using Entities.Base;
using Entities.Base.DataTable;
using Entities.Base.Menu;
using Entities.Base.Notification;
using Entities.Base.NotifitactionBuilder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Data.Services;

public class AuditService : IAuditService, IScopedDependency
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ISdk? _sdk;

    private readonly List<Type> _auditIgnoreTypes = new List<Type>
    {
        typeof(SystemMenu),
        typeof(User),
        typeof(Role),
        typeof(PropertyIdentity),
        typeof(Notification),
        typeof(NotificationBuidler)
    };

    public AuditService(IServiceScopeFactory serviceScopeFactory, ISdk? sdk)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _sdk = sdk;
    }

    public async Task SaveAuditAsync<TEntity>(TEntity entity, TEntity? oldEntity, AuditLogType type, CancellationToken cancellationToken = default) 
        where TEntity : BaseEntity
    {
        // Run audit in background to not block main operation
        _ = Task.Run(async () =>
        {
            try
            {
                await SaveAuditInternalAsync(entity, oldEntity, type, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid breaking main operation
                Console.WriteLine($"Audit logging failed: {ex.Message}");
            }
        }, cancellationToken);

        await Task.CompletedTask;
    }

    private async Task SaveAuditInternalAsync<TEntity>(TEntity entity, TEntity? oldEntity, AuditLogType type, CancellationToken cancellationToken) 
        where TEntity : BaseEntity
    {
        // Check if entity type should be audited
        if (_auditIgnoreTypes.Any(c => typeof(TEntity) == c))
        {
            return;
        }

        // Create audit entry
        var auditLog = CreateAuditLog(entity, oldEntity, type);

        if (auditLog == null || !auditLog.AuditLogDetails.Any())
        {
            return;
        }

        // Save in a new scope to avoid transaction issues
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            await dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private AuditLog? CreateAuditLog<TEntity>(TEntity entity, TEntity? oldEntity, AuditLogType type) 
        where TEntity : BaseEntity
    {
        var entityType = typeof(TEntity);
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var auditLog = new AuditLog
        {
            TableName = entityType.Name,
            CreatedById = _sdk?.CurrentUser?.Id,
            CreatedByName = _sdk?.CurrentUser?.FullName,
            CreatedOnMiladiDateTime = DateTime.Now,
            CreatedOnShamsiDateTime = DateTime.Now.ToShamsiDateTime(),
            EntityId = entity.Id ?? 0,
            Type = type,
            AuditLogDetails = new List<AuditLogDetail>()
        };

        foreach (var property in properties)
        {
            // Skip system properties
            var displayInfo = property.GetCustomAttribute<DisplayInfoAttribute>();
            if (displayInfo?.SystemProprty ?? false)
            {
                continue;
            }

            var propertyName = property.Name;
            var propertyTitle = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? propertyName;

            var currentValue = property.GetValue(entity)?.ToString();
            var oldValue = oldEntity != null ? property.GetValue(oldEntity)?.ToString() : null;

            AuditLogDetail? auditDetail = null;

            switch (type)
            {
                case AuditLogType.Add:
                    if (!string.IsNullOrEmpty(currentValue))
                    {
                        auditDetail = new AuditLogDetail
                        {
                            PropertyName = propertyName,
                            PropertyTitle = propertyTitle,
                            NewValue = currentValue,
                            OldValue = null
                        };
                    }
                    break;

                case AuditLogType.Update:
                    if (oldValue != currentValue)
                    {
                        auditDetail = new AuditLogDetail
                        {
                            PropertyName = propertyName,
                            PropertyTitle = propertyTitle,
                            OldValue = oldValue,
                            NewValue = currentValue
                        };
                    }
                    break;

                case AuditLogType.Delete:
                    if (!string.IsNullOrEmpty(currentValue))
                    {
                        auditDetail = new AuditLogDetail
                        {
                            PropertyName = propertyName,
                            PropertyTitle = propertyTitle,
                            OldValue = currentValue,
                            NewValue = null
                        };
                    }
                    break;
            }

            if (auditDetail != null)
            {
                auditLog.AuditLogDetails.Add(auditDetail);
            }
        }

        return auditLog.AuditLogDetails.Any() ? auditLog : null;
    }
}

