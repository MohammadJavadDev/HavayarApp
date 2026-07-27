using Common;
using Microsoft.EntityFrameworkCore;

namespace Data.Services;

public interface IPageBuilderSchemaService
{
	Task EnsureTableAsync(CancellationToken cancellationToken = default);
}

public sealed class PageBuilderSchemaService(ApplicationDbContext dbContext)
	: IPageBuilderSchemaService, IScopedDependency
{
	public async Task EnsureTableAsync(CancellationToken cancellationToken = default)
	{
		const string sql = """
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'System')
	EXEC('CREATE SCHEMA [System]');

IF NOT EXISTS (
	SELECT 1 FROM sys.tables t
	INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
	WHERE s.name = N'System' AND t.name = N'PageDefinition'
)
BEGIN
	CREATE TABLE [System].[PageDefinition] (
		[Id] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
		[ViewPath] nvarchar(500) NOT NULL,
		[DisplayName] nvarchar(200) NOT NULL,
		[Content] nvarchar(max) NOT NULL CONSTRAINT DF_PageDefinition_Content DEFAULT N'',
		[PageKind] int NOT NULL CONSTRAINT DF_PageDefinition_PageKind DEFAULT 0,
		[SourceType] int NOT NULL CONSTRAINT DF_PageDefinition_SourceType DEFAULT 0,
		[FormDefinitionId] bigint NULL,
		[Module] nvarchar(100) NULL,
		[Version] int NOT NULL CONSTRAINT DF_PageDefinition_Version DEFAULT 1,
		[CreatedById] bigint NULL,
		[ModifiedById] bigint NULL,
		[CreatedByName] nvarchar(150) NULL,
		[ModifiedByName] nvarchar(150) NULL,
		[ModifiedDateMiladiDateTime] datetime2 NULL,
		[ModifiedDateShamsiDateTime] nvarchar(30) NULL,
		[CreatedOnMiladiDateTime] datetime2 NULL,
		[CreatedOnShamsiDateTime] nvarchar(30) NULL,
		[IsActive] int NULL
	);
END

IF NOT EXISTS (
	SELECT 1 FROM sys.indexes
	WHERE name = N'UX_PageDefinition_ViewPath' AND object_id = OBJECT_ID(N'[System].[PageDefinition]')
)
CREATE UNIQUE INDEX [UX_PageDefinition_ViewPath] ON [System].[PageDefinition]([ViewPath]);
""";

		await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
	}
}
