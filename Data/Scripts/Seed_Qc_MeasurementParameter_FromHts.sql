/*
Seed_Qc_MeasurementParameter_FromHts.sql
کپی شناسه و عنوان HTS جدول dbo.Qc_MeasurementParameter به Qc.MeasurementParameter.
هر ۱۵ ردیف، با همان شناسه (شکاف‌های ۳، ۴، ۱۰، ۱۶ حفظ می‌شود).
UTF-8 BOM. Idempotent.
sqlcmd -S 172.20.40.42 -d HavayarApp -U sa -P <pwd> -C -b -I -f 65001 -i "Data\Scripts\Seed_Qc_MeasurementParameter_FromHts.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-qc-measurementparameter';

    IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
        THROW 50001, N'Linked server TMS is missing.', 1;

    IF EXISTS (
        SELECT 1
        FROM [TMS].[TotalSystem].[dbo].[Qc_MeasurementParameter]
        WHERE Title IS NULL OR LTRIM(RTRIM(Title)) = N''
    )
        THROW 50002, N'Qc_MeasurementParameter has an empty title.', 1;

    IF (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Qc_MeasurementParameter]) <> 15
        THROW 50003, N'Qc_MeasurementParameter row count is not 15.', 1;

    PRINT N'=== [1] Replace Qc.MeasurementParameter with HTS ids ===';
    DELETE FROM Qc.MeasurementParameter;

    SET IDENTITY_INSERT Qc.MeasurementParameter ON;
    INSERT INTO Qc.MeasurementParameter (
        Id, Title, IsActive,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    SELECT
        h.Id, h.Title, 1,
        1, @SeedUser, @Now, @NowShamsi,
        1, @SeedUser, @Now, @NowShamsi
    FROM [TMS].[TotalSystem].[dbo].[Qc_MeasurementParameter] h;
    PRINT N'  Inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));
    SET IDENTITY_INSERT Qc.MeasurementParameter OFF;

    IF EXISTS (
        SELECT 1
        FROM Qc.MeasurementParameter f
        FULL OUTER JOIN [TMS].[TotalSystem].[dbo].[Qc_MeasurementParameter] h
            ON h.Id = f.Id AND h.Title = f.Title
        WHERE f.Id IS NULL OR h.Id IS NULL
    )
        THROW 50004, N'Qc.MeasurementParameter does not match HTS Qc_MeasurementParameter.', 1;

    DECLARE @MaxId BIGINT = (SELECT MAX(Id) FROM Qc.MeasurementParameter);
    DBCC CHECKIDENT ('Qc.MeasurementParameter', RESEED, @MaxId);

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_Qc_MeasurementParameter_FromHts ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT COUNT(*) AS Matched
FROM Qc.MeasurementParameter f
INNER JOIN [TMS].[TotalSystem].[dbo].[Qc_MeasurementParameter] h
    ON h.Id = f.Id AND h.Title = f.Title;

SELECT MIN(Id) AS MinId, MAX(Id) AS MaxId, COUNT(*) AS Cnt FROM Qc.MeasurementParameter;
