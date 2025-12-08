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
using System.Collections;
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
	   typeof(NotificationBuidler),
	    typeof(SystemDataTableProfile)
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

		var auditLogs = new List<AuditLog>();

		// Create main entity audit
		var mainAuditLog = CreateAuditLog(entity, oldEntity, type);
		if (mainAuditLog != null && mainAuditLog.AuditLogDetails.Any())
		{
			auditLogs.Add(mainAuditLog);
		}

		// Handle collection properties
		var collectionAudits = CreateCollectionAudits(entity, oldEntity, type, entity.Id);
		if (collectionAudits.Any())
		{
			auditLogs.AddRange(collectionAudits);
		}

		if (!auditLogs.Any())
		{
			return;
		}

		// Save in a new scope to avoid transaction issues
		using (var scope = _serviceScopeFactory.CreateScope())
		{
			var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			await dbContext.AuditLogs.AddRangeAsync(auditLogs, cancellationToken);
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

			// Skip collection properties (will be handled separately)
			if (IsCollectionProperty(property))
			{
				continue;
			}

			// Skip navigation properties (Entity type)
			if (typeof(BaseEntity).IsAssignableFrom(property.PropertyType))
			{
				continue;
			}

			var propertyName = property.Name;
			var propertyTitle = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? propertyName;

			var currentValueRaw = property.GetValue(entity);
			var oldValueRaw = oldEntity != null ? property.GetValue(oldEntity) : null;

			var currentValue = GetPropertyDisplayValue(property, currentValueRaw);
			var oldValue = GetPropertyDisplayValue(property, oldValueRaw);

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

	private List<AuditLog> CreateCollectionAudits<TEntity>(TEntity entity, TEntity? oldEntity, AuditLogType type, long? parentId)
	    where TEntity : BaseEntity
	{
		var auditLogs = new List<AuditLog>();
		var entityType = typeof(TEntity);
		var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

		foreach (var property in properties)
		{
			// Check if this is a collection of BaseEntity
			if (!IsCollectionProperty(property))
			{
				continue;
			}

			var propertyTitle = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? property.Name;

			// Get current and old collections
			var currentCollection = property.GetValue(entity) as IEnumerable;
			var oldCollection = oldEntity != null ? property.GetValue(oldEntity) as IEnumerable : null;

			var currentItems = currentCollection?.Cast<BaseEntity>().ToList() ?? new List<BaseEntity>();
			var oldItems = oldCollection?.Cast<BaseEntity>().ToList() ?? new List<BaseEntity>();

			// Handle based on parent operation type
			switch (type)
			{
				case AuditLogType.Add:
					// All current items are new
					foreach (var item in currentItems)
					{
						var itemAudit = CreateCollectionItemAudit(item, null, AuditLogType.Add, propertyTitle, entity);
						if (itemAudit != null)
						{
							auditLogs.Add(itemAudit);
						}
					}
					break;

				case AuditLogType.Update:
					// Compare collections to find added, updated, deleted items
					var addedItems = currentItems.Where(c => c.Id == null || c.Id == 0 || !oldItems.Any(o => o.Id == c.Id)).ToList();
					var deletedItems = oldItems.Where(o => o.Id != null && o.Id != 0 && !currentItems.Any(c => c.Id == o.Id)).ToList();
					var potentiallyUpdatedItems = currentItems.Where(c => c.Id != null && c.Id != 0 && oldItems.Any(o => o.Id == c.Id)).ToList();

					// Added items
					foreach (var item in addedItems)
					{
						var itemAudit = CreateCollectionItemAudit(item, null, AuditLogType.Add, propertyTitle, entity);
						if (itemAudit != null)
						{
							auditLogs.Add(itemAudit);
						}
					}

					// Deleted items
					foreach (var item in deletedItems)
					{
						var itemAudit = CreateCollectionItemAudit(item, null, AuditLogType.Delete, propertyTitle, entity);
						if (itemAudit != null)
						{
							auditLogs.Add(itemAudit);
						}
					}

					// Updated items
					foreach (var currentItem in potentiallyUpdatedItems)
					{
						var oldItem = oldItems.FirstOrDefault(o => o.Id == currentItem.Id);
						if (oldItem != null)
						{
							var itemAudit = CreateCollectionItemAudit(currentItem, oldItem, AuditLogType.Update, propertyTitle, entity);
							if (itemAudit != null)
							{
								auditLogs.Add(itemAudit);
							}
						}
					}
					break;

				case AuditLogType.Delete:
					// All current items are deleted
					foreach (var item in currentItems)
					{
						var itemAudit = CreateCollectionItemAudit(item, null, AuditLogType.Delete, propertyTitle, entity);
						if (itemAudit != null)
						{
							auditLogs.Add(itemAudit);
						}
					}
					break;
			}
		}

		return auditLogs;
	}

	private AuditLog? CreateCollectionItemAudit(BaseEntity item, BaseEntity? oldItem, AuditLogType type, string collectionName, BaseEntity parentEntity)
	{
		var itemType = item.GetType();
		var properties = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

		var auditLog = new AuditLog
		{
			TableName = $"{itemType.Name}",
			CreatedById = _sdk?.CurrentUser?.Id,
			CreatedByName = _sdk?.CurrentUser?.FullName,
			CreatedOnMiladiDateTime = DateTime.Now,
			CreatedOnShamsiDateTime = DateTime.Now.ToShamsiDateTime(),
			EntityId = item.Id ?? 0,
			Type = type,
			ParentId = parentEntity.Id,
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

			// Skip collection properties
			if (IsCollectionProperty(property))
			{
				continue;
			}

			// Skip navigation properties
			if (typeof(BaseEntity).IsAssignableFrom(property.PropertyType))
			{
				continue;
			}

			var propertyName = property.Name;
			var propertyTitle = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? propertyName;

			var currentValueRaw = property.GetValue(item);
			var oldValueRaw = oldItem != null ? property.GetValue(oldItem) : null;

			var currentValue = GetPropertyDisplayValue(property, currentValueRaw);
			var oldValue = GetPropertyDisplayValue(property, oldValueRaw);
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

	private bool IsCollectionProperty(PropertyInfo property)
	{
		if (property.PropertyType == typeof(string))
		{
			return false;
		}

		if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
		{
			var genericArgs = property.PropertyType.GetGenericArguments();
			if (genericArgs.Any() && typeof(BaseEntity).IsAssignableFrom(genericArgs[0]))
			{
				return true;
			}
		}

		return false;
	}
	private string? GetEnumDisplayName(object? enumValue)
	{
		if (enumValue == null)
			return null;

		var enumType = enumValue.GetType();
		if (!enumType.IsEnum)
			return enumValue.ToString();

		var memberInfo = enumType.GetMember(enumValue.ToString() ?? string.Empty);
		if (memberInfo.Length > 0)
		{
			var displayAttribute = memberInfo[0].GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>();
			if (displayAttribute != null)
			{
				return displayAttribute.Name ?? enumValue.ToString();
			}
		}

		return enumValue.ToString();
	}

	private string? GetPropertyDisplayValue(PropertyInfo property, object? value)
	{
		if (value == null)
			return null;

		// Check if property is enum
		var propertyType = property.PropertyType;

		// Handle nullable enum
		if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			propertyType = Nullable.GetUnderlyingType(propertyType);
		}

		if (propertyType != null && propertyType.IsEnum)
		{
			return GetEnumDisplayName(value);
		}

		return value.ToString();
	}
}
