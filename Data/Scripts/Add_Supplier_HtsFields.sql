-- ============================================================================
-- WP9 / D16 (Q5-a) — افزودن ستون‌های HTS-only تامین‌کننده به Gnr.Supplier
-- معادل دستی migration: 20260917203247_AddSupplierHtsFields (Data/Migrations/ApplicationDb)
--
-- ستون‌ها (طول عین HTS به‌جز آدرس که بلندتر است):
--   Grade                nvarchar(50)   NULL   گرید
--   ServicesAndProducts  nvarchar(256)  NULL   خدمات/محصولات
--   RelatedPersonName    nvarchar(128)  NULL   شخص مرتبط
--   Address              nvarchar(2000) NULL   آدرس کامل (HTS nvarchar(1024))
-- آدرس روی Supplier است نه Party — تا Gnr.Party.Address (مشتری/عمومی) آلوده نشود.
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
DECLARE @MigrationId NVARCHAR(150) = N'20260917203247_AddSupplierHtsFields';
DECLARE @ProductVersion NVARCHAR(32) = N'10.0.1';

IF COL_LENGTH('Gnr.Supplier', 'Grade') IS NULL
BEGIN
    ALTER TABLE [Gnr].[Supplier] ADD [Grade] NVARCHAR(50) NULL;
    PRINT N'Column Grade added to Gnr.Supplier';
END
ELSE
    PRINT N'Column Grade already exists on Gnr.Supplier';

IF COL_LENGTH('Gnr.Supplier', 'ServicesAndProducts') IS NULL
BEGIN
    ALTER TABLE [Gnr].[Supplier] ADD [ServicesAndProducts] NVARCHAR(256) NULL;
    PRINT N'Column ServicesAndProducts added to Gnr.Supplier';
END
ELSE
    PRINT N'Column ServicesAndProducts already exists on Gnr.Supplier';

IF COL_LENGTH('Gnr.Supplier', 'RelatedPersonName') IS NULL
BEGIN
    ALTER TABLE [Gnr].[Supplier] ADD [RelatedPersonName] NVARCHAR(128) NULL;
    PRINT N'Column RelatedPersonName added to Gnr.Supplier';
END
ELSE
    PRINT N'Column RelatedPersonName already exists on Gnr.Supplier';

IF COL_LENGTH('Gnr.Supplier', 'Address') IS NULL
BEGIN
    ALTER TABLE [Gnr].[Supplier] ADD [Address] NVARCHAR(2000) NULL;
    PRINT N'Column Address added to Gnr.Supplier';
END
ELSE
    PRINT N'Column Address already exists on Gnr.Supplier';

IF @RegisterEfMigration = 1
   AND @MigrationId NOT LIKE N'PENDING%'
   AND OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE MigrationId = @MigrationId)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] (MigrationId, ProductVersion)
    VALUES (@MigrationId, @ProductVersion);

    PRINT N'Registered migration ' + @MigrationId + N' in __EFMigrationsHistory';
END

-- بازبینی
SELECT c.name AS ColumnName, t.name AS TypeName, c.max_length / 2 AS MaxChars, c.is_nullable
FROM sys.columns c
INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'Gnr.Supplier')
  AND c.name IN (N'Grade', N'ServicesAndProducts', N'RelatedPersonName', N'Address', N'DlCode', N'PartyId');
