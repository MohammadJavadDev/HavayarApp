-- ============================================================================
-- WP1 — جدول Sup.OpenOrderRequestEmailToSupplier
-- معادل دستی migration: AddOpenOrderRequestEmailToSupplier
--
-- روش ترجیحی: .\add-migration.ps1 -Name "AddOpenOrderRequestEmailToSupplier" سپس .\update-database.ps1
-- این اسکریپت fallback است. @RegisterEfMigration = 1 فقط وقتی این اسکریپت را به‌جای migration اجرا کرده‌اید.
-- ============================================================================

SET NOCOUNT ON;

DECLARE @RegisterEfMigration BIT = 0;
DECLARE @MigrationId NVARCHAR(150) = N'20260917204222_AddOpenOrderRequestEmailToSupplier';
DECLARE @ProductVersion NVARCHAR(32) = N'10.0.1';

IF OBJECT_ID(N'Sup.OpenOrderRequestEmailToSupplier', N'U') IS NULL
BEGIN
    CREATE TABLE [Sup].[OpenOrderRequestEmailToSupplier]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [OpenOrderRequestId] BIGINT NOT NULL,
        [SupplierId] BIGINT NOT NULL,
        [SendMiladiDateTime] DATETIME2 NULL,
        [SendShamsiDateTime] NVARCHAR(30) NULL,
        [SenderUserId] BIGINT NULL,
        [ToEmail] NVARCHAR(200) NULL,
        [Subject] NVARCHAR(500) NULL,
        [Body] NVARCHAR(MAX) NULL,
        [Comment] NVARCHAR(2048) NULL,
        [SendingCount] DECIMAL(18, 2) NULL,
        [HtsId] BIGINT NULL,
        [CreatedById] BIGINT NULL,
        [ModifiedById] BIGINT NULL,
        [CreatedByName] NVARCHAR(150) NULL,
        [ModifiedByName] NVARCHAR(150) NULL,
        [ModifiedDateMiladiDateTime] DATETIME2 NULL,
        [ModifiedDateShamsiDateTime] NVARCHAR(30) NULL,
        [CreatedOnMiladiDateTime] DATETIME2 NULL,
        [CreatedOnShamsiDateTime] NVARCHAR(30) NULL,
        [IsActive] INT NULL,
        CONSTRAINT [PK_OpenOrderRequestEmailToSupplier] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OpenOrderRequestEmailToSupplier_OpenOrderRequest]
            FOREIGN KEY ([OpenOrderRequestId]) REFERENCES [Sup].[OpenOrderRequest] ([Id]),
        CONSTRAINT [FK_OpenOrderRequestEmailToSupplier_Supplier]
            FOREIGN KEY ([SupplierId]) REFERENCES [Gnr].[Supplier] ([Id]),
        CONSTRAINT [FK_OpenOrderRequestEmailToSupplier_SenderUser]
            FOREIGN KEY ([SenderUserId]) REFERENCES [system].[User] ([Id])
    );

    CREATE INDEX [IX_OpenOrderRequestEmailToSupplier_OpenOrderRequestId]
        ON [Sup].[OpenOrderRequestEmailToSupplier] ([OpenOrderRequestId]);
    CREATE INDEX [IX_OpenOrderRequestEmailToSupplier_SupplierId]
        ON [Sup].[OpenOrderRequestEmailToSupplier] ([SupplierId]);
    CREATE UNIQUE INDEX [IX_OpenOrderRequestEmailToSupplier_HtsId]
        ON [Sup].[OpenOrderRequestEmailToSupplier] ([HtsId])
        WHERE [HtsId] IS NOT NULL;

    PRINT N'جدول Sup.OpenOrderRequestEmailToSupplier ایجاد شد';
END
ELSE
BEGIN
    PRINT N'جدول Sup.OpenOrderRequestEmailToSupplier از قبل موجود است';
END

IF @RegisterEfMigration = 1
   AND @MigrationId NOT LIKE N'PENDING%'
   AND OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE MigrationId = @MigrationId)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] (MigrationId, ProductVersion)
    VALUES (@MigrationId, @ProductVersion);
    PRINT N'Registered migration ' + @MigrationId + N' in __EFMigrationsHistory';
END

SELECT t.name AS TableName, c.name AS ColumnName, ty.name AS TypeName
FROM sys.tables t
INNER JOIN sys.columns c ON c.object_id = t.object_id
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE t.object_id = OBJECT_ID(N'Sup.OpenOrderRequestEmailToSupplier')
ORDER BY c.column_id;
