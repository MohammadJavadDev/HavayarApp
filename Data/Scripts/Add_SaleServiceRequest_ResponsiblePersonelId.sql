-- ============================================================================
-- مسئول منطقه = Hcm.Personel (معادل HTS Responsible_Zone_FK → HRM_Personel)
-- نگاشت: HTS PrsCode = Hcm.Personel.Code (نه Personel.Id و نه HamkaranId)
--
-- روش ترجیحی: .\add-migration.ps1 -Name "AddServiceRequestResponsiblePersonel"
--              سپس .\update-database.ps1
-- این اسکریپت fallback + backfill است.
-- ============================================================================

SET NOCOUNT ON;

DECLARE @RegisterEfMigration BIT = 0;
DECLARE @MigrationId NVARCHAR(150) = N'20260918140000_AddServiceRequestResponsiblePersonel';
DECLARE @ProductVersion NVARCHAR(32) = N'10.0.1';

IF COL_LENGTH('Sale.ServiceRequest', 'ResponsiblePersonelId') IS NULL
BEGIN
    ALTER TABLE [Sale].[ServiceRequest]
        ADD [ResponsiblePersonelId] BIGINT NULL;

    PRINT N'Column ResponsiblePersonelId added to Sale.ServiceRequest';
END
ELSE
BEGIN
    PRINT N'Column ResponsiblePersonelId already exists on Sale.ServiceRequest';
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Sale_ServiceRequest_ResponsiblePersonelId'
      AND object_id = OBJECT_ID(N'Sale.ServiceRequest')
)
BEGIN
    CREATE INDEX [IX_Sale_ServiceRequest_ResponsiblePersonelId]
        ON [Sale].[ServiceRequest] ([ResponsiblePersonelId]);
    PRINT N'Index IX_Sale_ServiceRequest_ResponsiblePersonelId created';
END

IF @RegisterEfMigration = 1
   AND OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE MigrationId = @MigrationId)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] (MigrationId, ProductVersion)
    VALUES (@MigrationId, @ProductVersion);
    PRINT N'Registered migration ' + @MigrationId + N' in __EFMigrationsHistory';
END

IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    UPDATE t
    SET t.ResponsiblePersonelId = p.Id
    FROM [Sale].[ServiceRequest] t
    INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_ServiceRequest] s
        ON CAST(s.ServiceRequest_ID AS BIGINT) = t.HtsId
    INNER JOIN [TMS].[TotalSystem].[dbo].[HRM_Personel] hp
        ON hp.Personel_ID = s.Responsible_Zone_FK
    INNER JOIN [Hcm].[Personel] p
        ON LTRIM(RTRIM(p.Code)) = LTRIM(RTRIM(CAST(hp.PrsCode AS nvarchar(50))))
    WHERE t.ResponsiblePersonelId IS NULL
      AND t.HtsId <> 0
      AND hp.PrsCode IS NOT NULL
      AND LTRIM(RTRIM(CAST(hp.PrsCode AS nvarchar(50)))) <> N'';

    PRINT N'Backfilled ResponsiblePersonelId: ' + CAST(@@ROWCOUNT AS nvarchar(20));
END
ELSE
BEGIN
    PRINT N'Linked Server [TMS] یافت نشد — backfill انجام نشد.';
END

SELECT
    COUNT(*) AS ServiceRequestCount,
    SUM(CASE WHEN ResponsiblePersonelId IS NULL THEN 1 ELSE 0 END) AS MissingResponsiblePersonel,
    SUM(CASE WHEN ResponsiblePersonelId IS NOT NULL THEN 1 ELSE 0 END) AS MappedResponsiblePersonel
FROM [Sale].[ServiceRequest];

-- باقیمانده‌های غیرقابل مپ (جعلی نشود):
--   HTS FK خالی؛ PrsCode خالی (نمایندگی‌ها)؛ 999999123 / 999999092 / -1111 / -1112
--   42017 مهدی دانشمندی: در Hcm.Personel نیست (کد ملی 0070254273 / Hamkaran 42017)
