/*
Seed_Qc_MeasurementParameterItem_FromHts.sql
کپی شناسه، گروه، عنوان و نماد HTS جدول dbo.Qc_MeasurementParameterItem
و شناسه و عنوان dbo.Qc_MeasurementParameterSubItem.
۱۳۳ قلم (شکاف شناسه حفظ می‌شود) و ۴ ریزقلم، از جمله عنوان خالی شناسه ۳۶.
UTF-8 BOM. Idempotent. گروه‌بندی باید از قبل در Qc.MeasurementParameter باشد.
sqlcmd -S 172.20.40.42 -d HavayarApp -U sa -P <pwd> -C -b -I -f 65001 -i "Data\Scripts\Seed_Qc_MeasurementParameterItem_FromHts.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-qc-measurementparameteritem';

    IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
        THROW 50031, N'Linked server TMS is missing.', 1;

    IF (SELECT COUNT(*) FROM Qc.MeasurementParameter) <> 15
        THROW 50032, N'Qc.MeasurementParameter row count is not 15. Run the grouping seed first.', 1;

    IF (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Qc_MeasurementParameterItem]) <> 133
        THROW 50033, N'Qc_MeasurementParameterItem row count is not 133.', 1;

    IF (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Qc_MeasurementParameterSubItem]) <> 4
        THROW 50034, N'Qc_MeasurementParameterSubItem row count is not 4.', 1;

    IF EXISTS (
        SELECT 1
        FROM [TMS].[TotalSystem].[dbo].[Qc_MeasurementParameterItem] h
        WHERE h.Title IS NULL OR LTRIM(RTRIM(h.Title)) = N''
    )
        THROW 50035, N'Qc_MeasurementParameterItem has an empty title.', 1;

    IF EXISTS (
        SELECT 1
        FROM [TMS].[TotalSystem].[dbo].[Qc_MeasurementParameterItem] h
        WHERE NOT EXISTS (SELECT 1 FROM Qc.MeasurementParameter g WHERE g.Id = h.MeasurementParameterId)
    )
        THROW 50036, N'An item group id is missing from Qc.MeasurementParameter.', 1;

    PRINT N'=== [1] Replace Qc.MeasurementParameterItem and SubItem with HTS ids ===';
    DELETE FROM Qc.MeasurementParameterSubItem;
    DELETE FROM Qc.MeasurementParameterItem;

    SET IDENTITY_INSERT Qc.MeasurementParameterItem ON;
    INSERT INTO Qc.MeasurementParameterItem (
        Id, MeasurementParameterId, Title, CommonSymbol, IsActive,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    SELECT
        h.Id, h.MeasurementParameterId, h.Title, h.CommonSymbol, 1,
        1, @SeedUser, @Now, @NowShamsi,
        1, @SeedUser, @Now, @NowShamsi
    FROM [TMS].[TotalSystem].[dbo].[Qc_MeasurementParameterItem] h;
    PRINT N'  Items inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));
    SET IDENTITY_INSERT Qc.MeasurementParameterItem OFF;

    SET IDENTITY_INSERT Qc.MeasurementParameterSubItem ON;
    INSERT INTO Qc.MeasurementParameterSubItem (
        Id, MeasurementParameterItemId, Title, IsActive,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    SELECT
        h.Id, h.MeasurementParameterItemId, h.Title, 1,
        1, @SeedUser, @Now, @NowShamsi,
        1, @SeedUser, @Now, @NowShamsi
    FROM [TMS].[TotalSystem].[dbo].[Qc_MeasurementParameterSubItem] h;
    PRINT N'  SubItems inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));
    SET IDENTITY_INSERT Qc.MeasurementParameterSubItem OFF;

    IF EXISTS (
        SELECT 1
        FROM Qc.MeasurementParameterItem f
        FULL OUTER JOIN [TMS].[TotalSystem].[dbo].[Qc_MeasurementParameterItem] h
            ON h.Id = f.Id
            AND h.MeasurementParameterId = f.MeasurementParameterId
            AND h.Title = f.Title
            AND ISNULL(h.CommonSymbol, N'') = ISNULL(f.CommonSymbol, N'')
        WHERE f.Id IS NULL OR h.Id IS NULL
    )
        THROW 50037, N'Qc.MeasurementParameterItem does not match HTS.', 1;

    IF EXISTS (
        SELECT 1
        FROM Qc.MeasurementParameterSubItem f
        FULL OUTER JOIN [TMS].[TotalSystem].[dbo].[Qc_MeasurementParameterSubItem] h
            ON h.Id = f.Id
            AND h.MeasurementParameterItemId = f.MeasurementParameterItemId
            AND ISNULL(h.Title, N'') = ISNULL(f.Title, N'')
        WHERE f.Id IS NULL OR h.Id IS NULL
    )
        THROW 50038, N'Qc.MeasurementParameterSubItem does not match HTS.', 1;

    DECLARE @MaxItemId BIGINT = (SELECT MAX(Id) FROM Qc.MeasurementParameterItem);
    DBCC CHECKIDENT ('Qc.MeasurementParameterItem', RESEED, @MaxItemId);

    DECLARE @MaxSubId BIGINT = (SELECT MAX(Id) FROM Qc.MeasurementParameterSubItem);
    DBCC CHECKIDENT ('Qc.MeasurementParameterSubItem', RESEED, @MaxSubId);

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_Qc_MeasurementParameterItem_FromHts ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT COUNT(*) AS ItemMatched
FROM Qc.MeasurementParameterItem f
INNER JOIN [TMS].[TotalSystem].[dbo].[Qc_MeasurementParameterItem] h
    ON h.Id = f.Id
    AND h.MeasurementParameterId = f.MeasurementParameterId
    AND h.Title = f.Title
    AND ISNULL(h.CommonSymbol, N'') = ISNULL(f.CommonSymbol, N'');

SELECT COUNT(*) AS SubItemMatched
FROM Qc.MeasurementParameterSubItem f
INNER JOIN [TMS].[TotalSystem].[dbo].[Qc_MeasurementParameterSubItem] h
    ON h.Id = f.Id
    AND h.MeasurementParameterItemId = f.MeasurementParameterItemId
    AND ISNULL(h.Title, N'') = ISNULL(f.Title, N'');
