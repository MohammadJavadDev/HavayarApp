/*
Seed_Qc_FailureType_FromHts.sql
بازگرداندن شناسه HTS جدول Qc.FailureType از dbo.Qc_FailureType.
قبل از جایگزینی، یادداشت سفارش ساختی که با شناسهٔ شماره‌گذاری‌شدهٔ جدید ذخیره شده
به شناسهٔ HTS همان عنوان برمی‌گردد. ردیف‌هایی که از قبل شناسهٔ قدیم دارند دست نمی‌خورند.
UTF-8 BOM. Idempotent.
sqlcmd -S 172.20.40.42 -d HavayarApp -U sa -P <pwd> -C -b -I -f 65001 -i "Data\Scripts\Seed_Qc_FailureType_FromHts.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-qc-failuretype';

    IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
        THROW 50001, N'Linked server TMS is missing.', 1;

    IF EXISTS (
        SELECT 1
        FROM [TMS].[TotalSystem].[dbo].[Qc_FailureType]
        WHERE FailureType IS NULL OR LTRIM(RTRIM(FailureType)) = N''
    )
        THROW 50002, N'Qc_FailureType has an empty title.', 1;

    IF (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Qc_FailureType]) <> 99
        THROW 50003, N'Qc_FailureType row count is not 99.', 1;

    PRINT N'=== [1] Remap new-id comments to HTS id ===';
    UPDATE c
    SET c.FailureTypeIds = CAST(hts.Id AS nvarchar(20))
    FROM Sale.ProductionOrderItemComment c
    INNER JOIN Qc.FailureType cur
        ON cur.Id = TRY_CAST(LTRIM(RTRIM(c.FailureTypeIds)) AS bigint)
    INNER JOIN [TMS].[TotalSystem].[dbo].[Qc_FailureType] hts
        ON LTRIM(RTRIM(hts.FailureType)) = LTRIM(RTRIM(cur.Title))
    WHERE c.FailureTypeIds IS NOT NULL
      AND LTRIM(RTRIM(c.FailureTypeIds)) <> N''
      AND c.FailureTypeIds NOT LIKE N'%,%'
      AND LTRIM(RTRIM(c.FailureTypeNames)) = LTRIM(RTRIM(cur.Title))
      AND cur.Id <> hts.Id;
    PRINT N'  Comments remapped: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== [2] Replace Qc.FailureType with HTS ids ===';
    DELETE FROM Qc.FailureType;

    SET IDENTITY_INSERT Qc.FailureType ON;
    INSERT INTO Qc.FailureType (
        Id, Title, IsActive,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    SELECT
        h.Id, h.FailureType, 1,
        1, @SeedUser, @Now, @NowShamsi,
        1, @SeedUser, @Now, @NowShamsi
    FROM [TMS].[TotalSystem].[dbo].[Qc_FailureType] h;
    PRINT N'  Inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));
    SET IDENTITY_INSERT Qc.FailureType OFF;

    IF EXISTS (
        SELECT 1
        FROM Qc.FailureType f
        FULL OUTER JOIN [TMS].[TotalSystem].[dbo].[Qc_FailureType] h
            ON h.Id = f.Id AND h.FailureType = f.Title
        WHERE f.Id IS NULL OR h.Id IS NULL
    )
        THROW 50004, N'Qc.FailureType does not match HTS Qc_FailureType.', 1;

    DECLARE @MaxId BIGINT = (SELECT MAX(Id) FROM Qc.FailureType);
    DBCC CHECKIDENT ('Qc.FailureType', RESEED, @MaxId);

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_Qc_FailureType_FromHts ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT COUNT(*) AS Matched
FROM Qc.FailureType f
INNER JOIN [TMS].[TotalSystem].[dbo].[Qc_FailureType] h
    ON h.Id = f.Id AND h.FailureType = f.Title;

SELECT MIN(Id) AS MinId, MAX(Id) AS MaxId, COUNT(*) AS Cnt FROM Qc.FailureType;
