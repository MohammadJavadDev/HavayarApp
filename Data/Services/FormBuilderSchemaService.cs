using Common;
using Common.Attributes;
using Data.Contracts;
using Entities.Base.FormBuilder;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Data.Services;

public interface IFormBuilderSchemaService
{
	Task EnsureFormDefinitionPublishColumnsAsync(CancellationToken cancellationToken = default);
	Task EnsureSchemaAsync(FormDefinition formDefinition, CancellationToken cancellationToken = default);
	Task SyncSchemaAsync(FormDefinition previousDefinition, FormDefinition formDefinition, CancellationToken cancellationToken = default);
}

public class FormBuilderSchemaService(ApplicationDbContext dbContext) : IFormBuilderSchemaService, IScopedDependency
{
	private static readonly HashSet<SystemType> SkippedColumnTypes = new()
	{
		SystemType.ListEntity,
		SystemType.ListString,
		SystemType.ListLong,
		SystemType.File
	};

	private static readonly (string Name, string SqlType)[] BaseEntityColumns =
	[
		("Id", "bigint IDENTITY(1,1) NOT NULL"),
		("CreatedById", "bigint NULL"),
		("ModifiedById", "bigint NULL"),
		("CreatedByName", "nvarchar(150) NULL"),
		("ModifiedByName", "nvarchar(150) NULL"),
		("ModifiedDateMiladiDateTime", "datetime2 NULL"),
		("ModifiedDateShamsiDateTime", "nvarchar(30) NULL"),
		("CreatedOnMiladiDateTime", "datetime2 NULL"),
		("CreatedOnShamsiDateTime", "nvarchar(30) NULL"),
		("IsActive", "int NULL")
	];

	public async Task EnsureFormDefinitionPublishColumnsAsync(CancellationToken cancellationToken = default)
	{
		var columns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["IsPublished"] = "bit NOT NULL CONSTRAINT DF_FormDefinition_IsPublished DEFAULT 0",
			["PublishedAt"] = "datetime2 NULL",
			["PublishVersion"] = "int NOT NULL CONSTRAINT DF_FormDefinition_PublishVersion DEFAULT 0",
			["LastPublishError"] = "nvarchar(4000) NULL",
			["ControllerRoutePrefix"] = "nvarchar(200) NULL"
		};

		foreach (var column in columns)
		{
			var sql = $@"
IF NOT EXISTS (
	SELECT 1 FROM sys.columns c
	INNER JOIN sys.tables t ON c.object_id = t.object_id
	INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
	WHERE s.name = N'System' AND t.name = N'FormDefinition' AND c.name = N'{column.Key}'
)
ALTER TABLE [System].[FormDefinition] ADD [{column.Key}] {column.Value};";

			await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
		}
	}

	public async Task EnsureSchemaAsync(FormDefinition formDefinition, CancellationToken cancellationToken = default)
	{
		var schema = EscapeIdentifier(formDefinition.Schema ?? "dbo");
		var table = EscapeIdentifier(formDefinition.EntityName);

		await EnsureSchemaExistsAsync(schema, cancellationToken);

		if (await TableExistsAsync(schema, table, cancellationToken))
			return;

		var columns = BuildUserColumns(formDefinition);
		var userColumnSql = columns.Count > 0
			? ",\n\t" + string.Join(",\n\t", columns.Select(c => $"[{c.Name}] {c.SqlType}"))
			: string.Empty;
		var baseColumns = string.Join(",\n\t", BaseEntityColumns.Select(c => $"[{c.Name}] {c.SqlType}"));

		var createSql = $@"
CREATE TABLE [{schema}].[{table}] (
	{baseColumns}{userColumnSql},
	CONSTRAINT [PK_{table}] PRIMARY KEY CLUSTERED ([Id] ASC)
);";

		await dbContext.Database.ExecuteSqlRawAsync(createSql, cancellationToken);
	}

	public async Task SyncSchemaAsync(FormDefinition previousDefinition, FormDefinition formDefinition, CancellationToken cancellationToken = default)
	{
		await EnsureSchemaAsync(formDefinition, cancellationToken);

		var schema = EscapeIdentifier(formDefinition.Schema ?? "dbo");
		var table = EscapeIdentifier(formDefinition.EntityName);

		var previousColumns = GetColumnNames(previousDefinition).ToHashSet(StringComparer.OrdinalIgnoreCase);
		var newColumns = BuildUserColumns(formDefinition);

		foreach (var column in newColumns)
		{
			if (previousColumns.Contains(column.Name))
				continue;

			if (await ColumnExistsAsync(schema, table, column.Name, cancellationToken))
				continue;

			var alterSql = $"ALTER TABLE [{schema}].[{table}] ADD [{column.Name}] {column.SqlType};";
			await dbContext.Database.ExecuteSqlRawAsync(alterSql, cancellationToken);
		}
	}

	private async Task EnsureSchemaExistsAsync(string schema, CancellationToken cancellationToken)
	{
		if (schema.Equals("dbo", StringComparison.OrdinalIgnoreCase))
			return;

		var sql = $@"
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'{schema}')
	EXEC(N'CREATE SCHEMA [{schema}]');";

		await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
	}

	private async Task<bool> TableExistsAsync(string schema, string table, CancellationToken cancellationToken)
	{
		var connection = dbContext.Database.GetDbConnection();
		if (connection.State != System.Data.ConnectionState.Open)
			await connection.OpenAsync(cancellationToken);

		await using var command = connection.CreateCommand();
		command.CommandText = @"
SELECT 1
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table";
		command.Parameters.Add(new SqlParameter("@schema", schema));
		command.Parameters.Add(new SqlParameter("@table", table));

		return await command.ExecuteScalarAsync(cancellationToken) != null;
	}

	private async Task<bool> ColumnExistsAsync(string schema, string table, string column, CancellationToken cancellationToken)
	{
		var connection = dbContext.Database.GetDbConnection();
		if (connection.State != System.Data.ConnectionState.Open)
			await connection.OpenAsync(cancellationToken);

		await using var command = connection.CreateCommand();
		command.CommandText = @"
SELECT 1
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table AND COLUMN_NAME = @column";
		command.Parameters.Add(new SqlParameter("@schema", schema));
		command.Parameters.Add(new SqlParameter("@table", table));
		command.Parameters.Add(new SqlParameter("@column", column));

		return await command.ExecuteScalarAsync(cancellationToken) != null;
	}

	private static IEnumerable<string> GetColumnNames(FormDefinition formDefinition)
	{
		return CollectProperties(formDefinition).Select(p => GetColumnName(p));
	}

	private static List<(string Name, string SqlType)> BuildUserColumns(FormDefinition formDefinition)
	{
		return CollectProperties(formDefinition)
			.Select(p => (Name: GetColumnName(p), SqlType: MapToSqlType(p)))
			.Where(c => !string.IsNullOrWhiteSpace(c.SqlType))
			.DistinctBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static IEnumerable<FormProperty> CollectProperties(FormDefinition formDefinition)
	{
		if (formDefinition.Sections == null)
			return [];

		return formDefinition.Sections
			.OrderBy(s => s.OrderIndex)
			.SelectMany(s => s.Properties ?? [])
			.Where(p => !p.SystemProperty && !SkippedColumnTypes.Contains(p.SystemType))
			.OrderBy(p => p.OrderIndex);
	}

	private static string GetColumnName(FormProperty prop)
	{
		if (prop.SystemType == SystemType.Entity)
			return $"{prop.PropertyName}Id";

		return prop.PropertyName;
	}

	private static string MapToSqlType(FormProperty prop)
	{
		var nullable = prop.Required ? "" : " NULL";

		return prop.SystemType switch
		{
			SystemType.String => $"nvarchar({(prop.MaxLength is > 0 and <= 4000 ? prop.MaxLength.ToString() : "max")}){nullable}",
			SystemType.Boolean => $"bit{(prop.Required ? " NOT NULL" : " NULL")}",
			SystemType.Int => $"int{nullable}",
			SystemType.Long or SystemType.AutoNumber => $"bigint{nullable}",
			SystemType.Decimal => $"decimal(18, 2){nullable}",
			SystemType.DateTime or SystemType.Date => $"datetime2{nullable}",
			SystemType.DateShamsi or SystemType.DateTimeShamsi => $"nvarchar(30){nullable}",
			SystemType.Select => $"int{nullable}",
			SystemType.Entity => $"bigint{nullable}",
			_ => $"nvarchar(max){nullable}"
		};
	}

	private static string EscapeIdentifier(string value)
	{
		return value.Replace("]", "]]", StringComparison.Ordinal);
	}
}
