using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.InMemoryData
{
	using Common.Attributes;
	using Common.Entities.EntityMetadatas;
	using Common.Utilities;
	using Entities.Auth;
	using Entities.Base;
	using System.Collections.Concurrent;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.ComponentModel.DataAnnotations.Schema;
	using System.Reflection;

	public interface IEntityMetadataCache
	{
		EntityMetadata? Get(string entityName);
		IReadOnlyCollection<EntityMetadata> GetAll();
		void Refresh();  
	}

	public class EntityMetadataCache : IEntityMetadataCache
	{
		private readonly ConcurrentDictionary<string, EntityMetadata> _cache = new(StringComparer.OrdinalIgnoreCase);
		private readonly object _buildLock = new();

		private bool _isInitialized = false;

		private readonly Assembly _assembly;
		private readonly Type _baseType;

		public EntityMetadataCache()
		{
			_assembly = typeof(BaseEntity).Assembly;
			_baseType = typeof(BaseEntity);
		}

		// 🧠 Lazy Load
		private void EnsureCacheBuilt()
		{
			if (_isInitialized) return;

			lock (_buildLock)
			{
				if (_isInitialized) return;
				BuildCache();
				_isInitialized = true;
			}
		}

		private void BuildCache()
		{
 
			var entityTypes = _assembly
			    .GetTypes()
			    .Where(t => t.IsClass && !t.IsAbstract && _baseType.IsAssignableFrom(t))
			    .ToList();

			entityTypes.Add(typeof(User));

			foreach (var entityType in entityTypes)
			{
				if(entityType.FullName.Contains("User"))
				{
					var d = "sad";
				}
				var displayAttr = entityType.GetCustomAttribute<DisplayAttribute>();
				var entityDisplayName = displayAttr?.Name ?? entityType.Name;
				var tableAttr = entityType.GetCustomAttribute<TableAttribute>();
			

				var entityMeta = new EntityMetadata
				{
					EntityName = entityType.Name,
					DisplayName = entityDisplayName,
					Schema = tableAttr?.Schema ?? "dbo",
					Type = "entity",
					EntityFullName = entityType.FullName,
					TabelName = tableAttr?.Name ?? entityType.Name

				};

				foreach (var prop in entityType.GetProperties())
				{
					var displayInfo = prop.GetCustomAttribute<DisplayInfoAttribute>();
					if (displayInfo == null) continue;

					var displayNameAttr = prop.GetCustomAttribute<DisplayNameAttribute>();
					var displayName = displayNameAttr?.DisplayName.ToLower() ?? prop.Name.ToLower();

					var meta = new PropertyMetadata
					{
						Name = prop.Name,
						DisplayName = displayName,
						Type = prop.PropertyType.Name,
						DataType = displayInfo.type.ToString().ToLower() ?? prop.PropertyType.Name.ToLower(),
						SearchPath = displayInfo.SearchPath ?? prop.Name,
						AddToTable = displayInfo.AddToTable,
						SystemProperty = displayInfo.SystemProprty,
						ShowInRelationData = displayInfo.ShowInRelationData,
						FileTypes = displayInfo.FileTypes ?? "",
						MaxFileSize = displayInfo.MaxFileSize,
						Required = displayInfo.Required,
						Regex = displayInfo.Regex,
						RegexInvalidError = displayInfo.RegexInvalidError,
						Start = displayInfo.Start,
						Step = displayInfo.Step,
						RelatedEntityTypeFullName = prop.PropertyType.FullName,
						RelatedEntityType = prop.PropertyType.Name,
						SystemType = displayInfo.type,
						ParentEntityName = entityMeta.EntityName,
						ParentEntityTypeName =entityMeta.EntityFullName
					};
					if(meta.SystemType == SystemType.ListEntity)
					{
						meta.RelatedEntityTypeFullName = prop.PropertyType.GenericTypeArguments[0].FullName;
						meta.RelatedEntityType = prop.PropertyType.GenericTypeArguments[0].Name;

					}

					// 🔗 تشخیص رابطه‌های entity
					if (meta.SystemType == SystemType.Entity)
					{
						var entityProps = prop.PropertyType.GetProperties();

						var relProp = entityProps
						    .Where(p => p.GetCustomAttribute<DisplayInfoAttribute>()?.ShowInRelationData == true)
						    .Select(p => new
						    {
							    DisplayName = p.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? p.Name,
							    PropertyName = p.Name
						    })
						    .FirstOrDefault();

						meta.RelatedEntity = prop.PropertyType.Name;
						meta.RelatedDisplayProp = relProp?.PropertyName;
						meta.SearchPath = relProp != null
						    ? $"{prop.Name}.{relProp.PropertyName}"
						    : $"{prop.Name}.Id";
					}

					if(meta.SystemType == SystemType.Select)
					{
						meta.Options = EnumExtensions.GetEnumValuesFromProperty(prop) ?? new();
					}

					entityMeta.Properties.Add(meta);
				}

				_cache[entityType.FullName.ToLower()] = entityMeta;
			}
		}

		// 🧩 Public Methods
		public EntityMetadata? Get(string entityName)
		{
 
			EnsureCacheBuilt();
			_cache.TryGetValue(entityName.ToLower(), out var meta);
			return meta;
		}

		public IReadOnlyCollection<EntityMetadata> GetAll()
		{
			EnsureCacheBuilt();
			return (IReadOnlyCollection<EntityMetadata>)_cache.Values;
		}

		public void Refresh()
		{
			lock (_buildLock)
			{
				BuildCache();
				_isInitialized = true;
			}
		}
	}

}
