/*
Seed_HcmJob_FromTotalSystem.sql
Upsert همه ردیف‌های HRM_Job به Hcm.Job (شامل غیرفعال)، سپس پر کردن Hcm.Personel.JobId
از HRM_Personel.Job_FK با تطبیق HamkaranId / Code.
Idempotent. Linked server [TMS] مثل Sync_SrvTicketHotelRequest_FromTotalSystem.sql.
UTF-8 with BOM.

sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_HcmJob_FromTotalSystem.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'Hcm.Job', N'U') IS NULL
BEGIN
    RAISERROR(N'جدول Hcm.Job وجود ندارد — ابتدا migration AddSrvCarRequest را اعمال کنید.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'Hcm.Personel', N'JobId') IS NULL
BEGIN
    RAISERROR(N'ستون Hcm.Personel.JobId وجود ندارد — ابتدا migration AddSrvCarRequest را اعمال کنید.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @Seed NVARCHAR(80) = N'seed-hcm-job';

BEGIN TRY
    BEGIN TRANSACTION;

    /* ========== Jobs from HRM_Job (all rows) ========== */
    IF OBJECT_ID('tempdb..#Jobs') IS NOT NULL DROP TABLE #Jobs;
    SELECT
        CAST(j.Job_ID AS BIGINT) AS HtsId,
        LTRIM(RTRIM(j.Job_Title)) AS Title,
        CAST(j.Hamkaran_Job_FK AS BIGINT) AS HamkaranId,
        CAST(j.CompanyId AS SMALLINT) AS CompanyId,
        CAST(CASE WHEN j.IsActive = 1 THEN 1 ELSE 0 END AS INT) AS IsActiveVal,
        CAST(j.ShowPublic AS BIT) AS ShowPublic,
        j.RahkaranId AS RahkaranId
    INTO #Jobs
    FROM [TMS].[TotalSystem].[dbo].[HRM_Job] j;

    DECLARE @JobSource INT = (SELECT COUNT(*) FROM #Jobs);
    PRINT N'HRM_Job source rows: ' + CAST(@JobSource AS nvarchar(20));

    UPDATE t SET
        t.Title = s.Title,
        t.HamkaranId = s.HamkaranId,
        t.CompanyId = s.CompanyId,
        t.IsActive = s.IsActiveVal,
        t.ShowPublic = s.ShowPublic,
        t.RahkaranId = s.RahkaranId,
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Hcm.Job t
    INNER JOIN #Jobs s ON s.HtsId = t.HtsId;
    PRINT N'  Hcm.Job updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO Hcm.Job
        (HtsId, Title, HamkaranId, CompanyId, ShowPublic, RahkaranId,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.Title, s.HamkaranId, s.CompanyId, s.ShowPublic, s.RahkaranId,
        1, @Seed, @Now, @NowShamsi,
        1, @Seed, @Now, @NowShamsi, s.IsActiveVal
    FROM #Jobs s
    WHERE NOT EXISTS (SELECT 1 FROM Hcm.Job t WHERE t.HtsId = s.HtsId);
    PRINT N'  Hcm.Job inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    /* ========== Personel.JobId: HamkaranId first, else PrsCode=Code. Never clear JobId. ========== */
    IF OBJECT_ID('tempdb..#PersonelJobFinal') IS NOT NULL DROP TABLE #PersonelJobFinal;

    ;WITH ByHamkaran AS (
        SELECT
            p.Id AS PersonelId,
            job.Id AS JobId,
            ROW_NUMBER() OVER (PARTITION BY p.Id ORDER BY job.Id) AS rn
        FROM [TMS].[TotalSystem].[dbo].[HRM_Personel] hp
        INNER JOIN Hcm.Job job ON job.HtsId = CAST(hp.Job_FK AS BIGINT)
        INNER JOIN Hcm.Personel p
            ON hp.Hamkaran_Personel_FK IS NOT NULL
           AND p.HamkaranId = CAST(hp.Hamkaran_Personel_FK AS BIGINT)
        WHERE hp.Job_FK IS NOT NULL
    ),
    MatchedHamkaran AS (
        SELECT PersonelId, JobId FROM ByHamkaran WHERE rn = 1
    ),
    ByCode AS (
        SELECT
            p.Id AS PersonelId,
            job.Id AS JobId,
            ROW_NUMBER() OVER (PARTITION BY p.Id ORDER BY job.Id) AS rn
        FROM [TMS].[TotalSystem].[dbo].[HRM_Personel] hp
        INNER JOIN Hcm.Job job ON job.HtsId = CAST(hp.Job_FK AS BIGINT)
        INNER JOIN Hcm.Personel p
            ON hp.PrsCode IS NOT NULL
           AND p.Code = LTRIM(RTRIM(hp.PrsCode))
        WHERE hp.Job_FK IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM MatchedHamkaran m WHERE m.PersonelId = p.Id)
    )
    SELECT PersonelId, JobId
    INTO #PersonelJobFinal
    FROM (
        SELECT PersonelId, JobId FROM MatchedHamkaran
        UNION ALL
        SELECT PersonelId, JobId FROM ByCode WHERE rn = 1
    ) u;

    UPDATE p SET
        p.JobId = m.JobId,
        p.ModifiedById = 1,
        p.ModifiedByName = @Seed,
        p.ModifiedDateMiladiDateTime = @Now,
        p.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Hcm.Personel p
    INNER JOIN #PersonelJobFinal m ON m.PersonelId = p.Id
    WHERE p.JobId IS NULL OR p.JobId <> m.JobId;
    PRINT N'  Hcm.Personel.JobId updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    DECLARE @JobTable INT = (SELECT COUNT(*) FROM Hcm.Job);
    DECLARE @PersonelWithJob INT = (SELECT COUNT(*) FROM Hcm.Personel WHERE JobId IS NOT NULL);
    PRINT N'Hcm.Job total: ' + CAST(@JobTable AS nvarchar(20))
        + N' | Personel with JobId: ' + CAST(@PersonelWithJob AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'Seed_HcmJob_FromTotalSystem completed.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;
