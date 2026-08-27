-- Adds HtsId / SecondReplySheetId to Edms document tables for idempotent sync from TotalSystem
IF COL_LENGTH('Edms.Document', 'HtsId') IS NULL
BEGIN
    ALTER TABLE [Edms].[Document]
    ADD [HtsId] BIGINT NOT NULL CONSTRAINT DF_Edms_Document_HtsId DEFAULT (0);

    PRINT 'Column HtsId added to Edms.Document';
END
ELSE
BEGIN
    PRINT 'Column HtsId already exists on Edms.Document';
END
GO

IF COL_LENGTH('Edms.Document', 'SecondReplySheetId') IS NULL
BEGIN
    ALTER TABLE [Edms].[Document]
    ADD [SecondReplySheetId] BIGINT NULL;

    PRINT 'Column SecondReplySheetId added to Edms.Document';
END
ELSE
BEGIN
    PRINT 'Column SecondReplySheetId already exists on Edms.Document';
END
GO

IF COL_LENGTH('Edms.DocumentComment', 'HtsId') IS NULL
BEGIN
    ALTER TABLE [Edms].[DocumentComment]
    ADD [HtsId] BIGINT NOT NULL CONSTRAINT DF_Edms_DocumentComment_HtsId DEFAULT (0);

    PRINT 'Column HtsId added to Edms.DocumentComment';
END
ELSE
BEGIN
    PRINT 'Column HtsId already exists on Edms.DocumentComment';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Edms_Document_HtsId'
      AND object_id = OBJECT_ID('Edms.Document')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Edms_Document_HtsId
        ON [Edms].[Document]([HtsId])
        WHERE [HtsId] <> 0;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Edms_DocumentComment_HtsId'
      AND object_id = OBJECT_ID('Edms.DocumentComment')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Edms_DocumentComment_HtsId
        ON [Edms].[DocumentComment]([HtsId])
        WHERE [HtsId] <> 0;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Document_SecondReplySheet_FileEntity'
      AND parent_object_id = OBJECT_ID('Edms.Document')
)
BEGIN
    ALTER TABLE [Edms].[Document]
    ADD CONSTRAINT FK_Document_SecondReplySheet_FileEntity
        FOREIGN KEY ([SecondReplySheetId]) REFERENCES [dbo].[FileEntity]([Id]);
END
GO
