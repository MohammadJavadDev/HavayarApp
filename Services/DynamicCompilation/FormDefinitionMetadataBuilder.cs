using Common;
using Common.Attributes;
using Common.Entities.EntityMetadatas;
using Common.Utilities;
using Entities.Base;
using Entities.Base.FormBuilder;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Services.DynamicCompilation;

public interface IFormDefinitionMetadataBuilder
{
	EntityMetadata BuildFromType(Type entityType, FormDefinition formDefinition);
}

public sealed class FormDefinitionMetadataBuilder : IFormDefinitionMetadataBuilder, ISingletonDependency
{
	public EntityMetadata BuildFromType(Type entityType, FormDefinition formDefinition)
	{
		var displayAttr = entityType.GetCustomAttribute<DisplayAttribute>();
		var tableAttr = entityType.GetCustomAttribute<TableAttribute>();

		var entityMeta = new EntityMetadata
		{
			EntityName = entityType.Name,
			DisplayName = displayAttr?.Name ?? formDefinition.DisplayName ?? entityType.Name,
			Schema = tableAttr?.Schema ?? formDefinition.Schema ?? "dbo",
			Type = "entity",
			EntityFullName = entityType.FullName ?? entityType.Name,
			TabelName = tableAttr?.Name ?? formDefinition.EntityName
		};

		foreach (var prop in entityType.GetProperties())
		{
			var displayInfo = prop.GetCustomAttribute<DisplayInfoAttribute>();
			if (displayInfo == null)
				continue;

			var displayNameAttr = prop.GetCustomAttribute<DisplayNameAttribute>();
			var displayName = displayNameAttr?.DisplayName.ToLower() ?? prop.Name.ToLower();

			var meta = new PropertyMetadata
			{
				Name = prop.Name,
				DisplayName = displayName,
				Type = GetActualTypeFullName(prop.PropertyType),
				DataType = displayInfo.type.ToString().ToLower(),
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
				ParentEntityTypeName = entityMeta.EntityFullName
			};

			if (meta.SystemType == SystemType.ListEntity &&
			    prop.PropertyType.IsGenericType)
			{
				meta.RelatedEntityTypeFullName = prop.PropertyType.GenericTypeArguments[0].FullName;
				meta.RelatedEntityType = prop.PropertyType.GenericTypeArguments[0].Name;
			}

			if (meta.SystemType == SystemType.Entity)
			{
				var relProp = prop.PropertyType.GetProperties()
					.Where(p => p.GetCustomAttribute<DisplayInfoAttribute>()?.ShowInRelationData == true)
					.Select(p => new
					{
						PropertyName = p.Name
					})
					.FirstOrDefault();

				meta.RelatedEntity = prop.PropertyType.Name;
				meta.RelatedDisplayProp = relProp?.PropertyName;
				meta.SearchPath = relProp != null
					? $"{prop.Name}.{relProp.PropertyName}"
					: $"{prop.Name}.Id";
			}

			if (meta.SystemType == SystemType.Select)
				meta.Options = EnumExtensions.GetEnumValuesFromProperty(prop) ?? [];

			entityMeta.Properties.Add(meta);
		}

		return entityMeta;
	}

	private static string GetActualTypeFullName(Type type)
	{
		var underlyingType = Nullable.GetUnderlyingType(type);
		var actualType = underlyingType ?? type;
		return actualType.FullName ?? actualType.Name;
	}
}
