-- ============================================================================
-- WP6 / D17 — افزودن ستون NonRoutineLeadTimeInDay به Pln.LeadTime
-- معادل دستی migration: 20260917194320_AddLeadTimeNonRoutineLeadTime (Data/Migrations/ApplicationDb)
--
-- روش ترجیحی: .\update-database.ps1 (اعمال migration)
-- این اسکریپت fallback است وقتی نمی‌خواهید/نمی‌توانید migration را اعمال کنید.
--   * Idempotent (COL_LENGTH)
--   * @RegisterEfMigration = 1 → ردیف migration در __EFMigrationsHistory ثبت می‌شود تا
--     update-database بعدی دوباره سعی نکند ستون را اضافه کند (فقط وقتی این اسکریپت را به‌جای
--     migration اجرا کرده‌اید مقدار 1 بدهید).
-- ============================================================================

SET NOCOUNT ON;

DECLARE @RegisterEfMigration BIT = 0;   -- 1 = ثبت در __EFMigrationsHistory
DECLARE @MigrationId NVARCHAR(150) = N'20260917194320_AddLeadTimeNonRoutineLeadTime';
DECLARE @ProductVersion NVARCHAR(32) = N'10.0.1';

IF COL_LENGTH('Pln.LeadTime', 'NonRoutineLeadTimeInDay') IS NULL
BEGIN
    ALTER TABLE [Pln].[LeadTime]
        ADD [NonRoutineLeadTimeInDay] INT NULL;

    PRINT N'Column NonRoutineLeadTimeInDay added to Pln.LeadTime';
END
ELSE
BEGIN
    PRINT N'Column NonRoutineLeadTimeInDay already exists on Pln.LeadTime';
END

IF @RegisterEfMigration = 1
   AND OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE MigrationId = @MigrationId)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] (MigrationId, ProductVersion)
    VALUES (@MigrationId, @ProductVersion);

    PRINT N'Registered migration ' + @MigrationId + N' in __EFMigrationsHistory';
END

-- بازبینی
SELECT c.name AS ColumnName, t.name AS TypeName, c.is_nullable
FROM sys.columns c
INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'Pln.LeadTime')
  AND c.name IN (N'LeadTimeDay', N'NonRoutineLeadTimeInDay', N'Supplier', N'PartId');
