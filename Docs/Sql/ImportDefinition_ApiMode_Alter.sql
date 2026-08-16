-- ============================================================
-- Import API Mode: alter System.ImportDefinition / ImportLog / ImportLogDetail
-- Run once against the application database before deploying the feature.
-- Safe to re-run (checks column existence).
-- ============================================================

SET NOCOUNT ON;

-- ---------- System.ImportDefinition ----------
IF COL_LENGTH('System.ImportDefinition', 'ImportType') IS NULL
BEGIN
    ALTER TABLE [System].[ImportDefinition]
        ADD [ImportType] INT NOT NULL
            CONSTRAINT [DF_ImportDefinition_ImportType] DEFAULT (0);
END
GO

IF COL_LENGTH('System.ImportDefinition', 'ApiUrl') IS NULL
BEGIN
    ALTER TABLE [System].[ImportDefinition]
        ADD [ApiUrl] NVARCHAR(1000) NULL;
END
GO

IF COL_LENGTH('System.ImportDefinition', 'ApiHttpMethod') IS NULL
BEGIN
    ALTER TABLE [System].[ImportDefinition]
        ADD [ApiHttpMethod] NVARCHAR(20) NULL
            CONSTRAINT [DF_ImportDefinition_ApiHttpMethod] DEFAULT (N'POST');
END
GO

IF COL_LENGTH('System.ImportDefinition', 'ApiCallMode') IS NULL
BEGIN
    ALTER TABLE [System].[ImportDefinition]
        ADD [ApiCallMode] INT NULL;
END
GO

IF COL_LENGTH('System.ImportDefinition', 'ApiIsInternal') IS NULL
BEGIN
    ALTER TABLE [System].[ImportDefinition]
        ADD [ApiIsInternal] BIT NOT NULL
            CONSTRAINT [DF_ImportDefinition_ApiIsInternal] DEFAULT (0);
END
GO

-- SqlQuery is no longer always required at DB level if a NOT NULL constraint exists.
-- Only drop NOT NULL when the column currently disallows nulls.
IF EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON c.object_id = t.object_id
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'System'
      AND t.name = N'ImportDefinition'
      AND c.name = N'SqlQuery'
      AND c.is_nullable = 0
)
BEGIN
    ALTER TABLE [System].[ImportDefinition]
        ALTER COLUMN [SqlQuery] NVARCHAR(MAX) NULL;
END
GO

-- ---------- System.ImportLog ----------
IF COL_LENGTH('System.ImportLog', 'BatchResponseJson') IS NULL
BEGIN
    ALTER TABLE [System].[ImportLog]
        ADD [BatchResponseJson] NVARCHAR(MAX) NULL;
END
GO

-- ---------- System.ImportLogDetail ----------
IF COL_LENGTH('System.ImportLogDetail', 'ResponseJson') IS NULL
BEGIN
    ALTER TABLE [System].[ImportLogDetail]
        ADD [ResponseJson] NVARCHAR(MAX) NULL;
END
GO

PRINT N'Import API Mode schema changes applied.';
GO
