using Common.Attributes;
using Common.Entities.EntityMetadatas;
using Entities.Base;
using Entities.Base.ImportDefinitions;
using System.Text;

namespace Services.ImportDefinitionServices
{
	public class ImportDefinitionEntityGenerator
	{
		private static readonly HashSet<SystemType> SkippedImportTypes = new()
		{
			SystemType.ListEntity,
			SystemType.ListString,
			SystemType.ListLong,
			SystemType.File,
			SystemType.AutoNumber,
			SystemType.Entity
		};

		private static readonly Dictionary<string, (string ParamName, string? SystemVariable, string? LiteralValue)> AuditColumns = new(StringComparer.OrdinalIgnoreCase)
		{
			["CreatedById"] = ("CurrentUserId", "CurrentUserId", null),
			["ModifiedById"] = ("CurrentUserId", "CurrentUserId", null),
			["CreatedByName"] = ("CurrentUserName", "CurrentUserName", null),
			["ModifiedByName"] = ("CurrentUserName", "CurrentUserName", null),
			["ModifiedDateMiladiDateTime"] = ("CurrentDateTimeMiladi", "CurrentDateTimeMiladi", null),
			["ModifiedDateShamsiDateTime"] = ("CurrentDateTimeShamsi", "CurrentDateTimeShamsi", null),
			["CreatedOnMiladiDateTime"] = ("CurrentDateTimeMiladi", "CurrentDateTimeMiladi", null),
			["CreatedOnShamsiDateTime"] = ("CurrentDateTimeShamsi", "CurrentDateTimeShamsi", null),
			["IsActive"] = ("IsActive", null, "1")
		};

		public static ImportDefinitionGenerateResult Generate(EntityMetadata entity)
		{
			var columns = new List<ImportDefinitionColumn>();
			var sqlColumns = new List<string>();
			var sqlValues = new List<string>();
			var sortOrder = 0;

			foreach (var prop in entity.Properties.Where(p => !p.SystemProperty))
			{
				if (prop.SystemType == SystemType.Entity)
				{
					AddUserColumn(columns, ref sortOrder, sqlColumns, sqlValues,
						$"{prop.Name}Id",
						prop.DisplayName,
						SystemType.Long,
						prop.Required,
						null);
					continue;
				}

				if (SkippedImportTypes.Contains(prop.SystemType))
					continue;

				OptionSetting? optionSettings = null;
				if (prop.SystemType == SystemType.Select && !string.IsNullOrWhiteSpace(prop.Type))
				{
					optionSettings = new OptionSetting
					{
						TypeOption = TypeOptionEnum.System,
						SystemTypeName = prop.Type
					};
				}

				AddUserColumn(columns, ref sortOrder, sqlColumns, sqlValues,
					prop.Name,
					prop.DisplayName,
					prop.SystemType,
					prop.Required,
					optionSettings);
			}

			foreach (var audit in AuditColumns)
			{
				sqlColumns.Add($"[{audit.Key}]");

				if (!string.IsNullOrEmpty(audit.Value.LiteralValue))
				{
					sqlValues.Add(audit.Value.LiteralValue);
					continue;
				}

				sqlValues.Add($"@{audit.Value.ParamName}");
			}

			var sqlQuery = new StringBuilder()
				.AppendLine($"INSERT INTO [{entity.Schema}].[{entity.TabelName}] ({string.Join(", ", sqlColumns)})")
				.AppendLine($"VALUES ({string.Join(", ", sqlValues)})")
				.ToString();

			return new ImportDefinitionGenerateResult
			{
				FullNameEntity = $"{entity.EntityName}Import",
				Title = $"ورود اطلاعات {entity.DisplayName}",
				Description = $"ورود اطلاعات {entity.DisplayName} از فایل اکسل",
				SqlQuery = sqlQuery,
				Columns = columns
			};
		}

		private static void AddUserColumn(
			List<ImportDefinitionColumn> columns,
			ref int sortOrder,
			List<string> sqlColumns,
			List<string> sqlValues,
			string columnName,
			string displayName,
			SystemType dataType,
			bool isRequired,
			OptionSetting? optionSettings)
		{
			sqlColumns.Add($"[{columnName}]");
			sqlValues.Add($"@{columnName}");

			columns.Add(new ImportDefinitionColumn
			{
				ColumnName = columnName,
				DisplayName = displayName,
				DataType = dataType,
				IsSystemVariable = false,
				IsRequired = isRequired,
				SortOrder = sortOrder++,
				OptionSettings = optionSettings
			});
		}
	}

	public class ImportDefinitionGenerateResult
	{
		public string FullNameEntity { get; set; } = "";
		public string Title { get; set; } = "";
		public string Description { get; set; } = "";
		public string SqlQuery { get; set; } = "";
		public List<ImportDefinitionColumn> Columns { get; set; } = new();
	}
}
