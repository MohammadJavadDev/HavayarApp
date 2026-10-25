/*
Seed_HrmOrgPost_FromTotalSystem.sql
Upsert پست‌های HRM_OrgPost به Hrm.OrgPost، سپس پر کردن Hcm.Personel.OrgPostId
از HRM_Personel.OrgPost_FK با تطبیق HamkaranId / Code.
کلید راهکاران تکراری (عنوان یکسان) به یک ردیف جمع می‌شود تا ایندکس یکتای HamkaranPostId نشکند.
Idempotent. Linked server [TMS].
UTF-8 with BOM.

sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_HrmOrgPost_FromTotalSystem.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'Hrm.OrgPost', N'U') IS NULL
BEGIN
    RAISERROR(N'جدول Hrm.OrgPost وجود ندارد — ابتدا migration AddHrmOrgPost را اعمال کنید.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'Hcm.Personel', N'OrgPostId') IS NULL
BEGIN
    RAISERROR(N'ستون Hcm.Personel.OrgPostId وجود ندارد — ابتدا migration AddHrmOrgPost را اعمال کنید.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @Seed NVARCHAR(80) = N'seed-hrm-orgpost';

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('tempdb..#Src') IS NOT NULL DROP TABLE #Src;
    SELECT
        CAST(op.OrgPost_ID AS BIGINT) AS HtsId,
        NULLIF(LTRIM(RTRIM(op.OrgPost_Title)), N'') AS Title,
        CAST(op.Hamkaran_Post_FK AS BIGINT) AS HamkaranPostId,
        CAST(ISNULL(op.ExternalPost, 0) AS BIT) AS ExternalPost,
        CAST(CASE WHEN op.IsActive = 1 THEN 1 ELSE 0 END AS INT) AS IsActiveVal,
        (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[HRM_Personel] p WHERE p.OrgPost_FK = op.OrgPost_ID) AS PersonelCnt
    INTO #Src
    FROM [TMS].[TotalSystem].[dbo].[HRM_OrgPost] op;

    DECLARE @Source INT = (SELECT COUNT(*) FROM #Src);
    PRINT N'HRM_OrgPost source rows: ' + CAST(@Source AS nvarchar(20));

    IF OBJECT_ID('tempdb..#Posts') IS NOT NULL DROP TABLE #Posts;
    ;WITH Ranked AS (
        SELECT
            HtsId, Title, HamkaranPostId, ExternalPost, IsActiveVal,
            ROW_NUMBER() OVER (
                PARTITION BY HamkaranPostId
                ORDER BY PersonelCnt DESC, HtsId DESC
            ) AS rn
        FROM #Src
        WHERE HamkaranPostId IS NOT NULL
    )
    SELECT HtsId, Title, HamkaranPostId, ExternalPost, IsActiveVal
    INTO #Posts
    FROM Ranked
    WHERE rn = 1;

    INSERT INTO #Posts (HtsId, Title, HamkaranPostId, ExternalPost, IsActiveVal)
    SELECT HtsId, Title, NULL, ExternalPost, IsActiveVal
    FROM #Src
    WHERE HamkaranPostId IS NULL;

    DECLARE @Canonical INT = (SELECT COUNT(*) FROM #Posts);
    PRINT N'Canonical posts: ' + CAST(@Canonical AS nvarchar(20));

    UPDATE t SET
        t.HtsId = s.HtsId,
        t.Title = s.Title,
        t.ExternalPost = s.ExternalPost,
        t.IsActive = s.IsActiveVal,
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Hrm.OrgPost t
    INNER JOIN #Posts s ON s.HamkaranPostId IS NOT NULL AND t.HamkaranPostId = s.HamkaranPostId;
    PRINT N'  Hrm.OrgPost updated by HamkaranPostId: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    UPDATE t SET
        t.Title = s.Title,
        t.ExternalPost = s.ExternalPost,
        t.IsActive = s.IsActiveVal,
        t.ModifiedById = 1,
        t.ModifiedByName = @Seed,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Hrm.OrgPost t
    INNER JOIN #Posts s ON s.HamkaranPostId IS NULL AND t.HamkaranPostId IS NULL AND t.HtsId = s.HtsId;
    PRINT N'  Hrm.OrgPost updated by HtsId: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO Hrm.OrgPost
        (HtsId, Title, HamkaranPostId, ExternalPost,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.Title, s.HamkaranPostId, s.ExternalPost,
        1, @Seed, @Now, @NowShamsi,
        1, @Seed, @Now, @NowShamsi, s.IsActiveVal
    FROM #Posts s
    WHERE s.HamkaranPostId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM Hrm.OrgPost t WHERE t.HamkaranPostId = s.HamkaranPostId);
    PRINT N'  Hrm.OrgPost inserted by HamkaranPostId: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    INSERT INTO Hrm.OrgPost
        (HtsId, Title, HamkaranPostId, ExternalPost,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        s.HtsId, s.Title, NULL, s.ExternalPost,
        1, @Seed, @Now, @NowShamsi,
        1, @Seed, @Now, @NowShamsi, s.IsActiveVal
    FROM #Posts s
    WHERE s.HamkaranPostId IS NULL
      AND NOT EXISTS (
          SELECT 1 FROM Hrm.OrgPost t
          WHERE t.HamkaranPostId IS NULL AND t.HtsId = s.HtsId
      );
    PRINT N'  Hrm.OrgPost inserted manual: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    /* Personel.OrgPostId: HamkaranId first, else PrsCode=Code. Does not clear an existing value when HTS has no post. */
    IF OBJECT_ID('tempdb..#PersonelPostFinal') IS NOT NULL DROP TABLE #PersonelPostFinal;

    ;WITH Linked AS (
        SELECT
            hp.Personel_ID,
            hp.Hamkaran_Personel_FK,
            hp.PrsCode,
            op.Id AS OrgPostId
        FROM [TMS].[TotalSystem].[dbo].[HRM_Personel] hp
        INNER JOIN [TMS].[TotalSystem].[dbo].[HRM_OrgPost] src ON src.OrgPost_ID = hp.OrgPost_FK
        INNER JOIN Hrm.OrgPost op ON
            (src.Hamkaran_Post_FK IS NOT NULL AND op.HamkaranPostId = CAST(src.Hamkaran_Post_FK AS BIGINT))
            OR (src.Hamkaran_Post_FK IS NULL AND op.HamkaranPostId IS NULL AND op.HtsId = CAST(src.OrgPost_ID AS BIGINT))
        WHERE hp.OrgPost_FK IS NOT NULL
    ),
    ByHamkaran AS (
        SELECT
            p.Id AS PersonelId,
            l.OrgPostId,
            ROW_NUMBER() OVER (PARTITION BY p.Id ORDER BY l.OrgPostId) AS rn
        FROM Linked l
        INNER JOIN Hcm.Personel p
            ON l.Hamkaran_Personel_FK IS NOT NULL
           AND p.HamkaranId = CAST(l.Hamkaran_Personel_FK AS BIGINT)
    ),
    MatchedHamkaran AS (
        SELECT PersonelId, OrgPostId FROM ByHamkaran WHERE rn = 1
    ),
    ByCode AS (
        SELECT
            p.Id AS PersonelId,
            l.OrgPostId,
            ROW_NUMBER() OVER (PARTITION BY p.Id ORDER BY l.OrgPostId) AS rn
        FROM Linked l
        INNER JOIN Hcm.Personel p
            ON l.PrsCode IS NOT NULL
           AND p.Code = LTRIM(RTRIM(l.PrsCode))
        WHERE NOT EXISTS (SELECT 1 FROM MatchedHamkaran m WHERE m.PersonelId = p.Id)
    )
    SELECT PersonelId, OrgPostId
    INTO #PersonelPostFinal
    FROM (
        SELECT PersonelId, OrgPostId FROM MatchedHamkaran
        UNION ALL
        SELECT PersonelId, OrgPostId FROM ByCode WHERE rn = 1
    ) u;

    UPDATE p SET
        p.OrgPostId = m.OrgPostId,
        p.ModifiedById = 1,
        p.ModifiedByName = @Seed,
        p.ModifiedDateMiladiDateTime = @Now,
        p.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Hcm.Personel p
    INNER JOIN #PersonelPostFinal m ON m.PersonelId = p.Id
    WHERE p.OrgPostId IS NULL OR p.OrgPostId <> m.OrgPostId;
    PRINT N'  Hcm.Personel.OrgPostId updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    DECLARE @PostTable INT = (SELECT COUNT(*) FROM Hrm.OrgPost);
    DECLARE @PersonelWithPost INT = (SELECT COUNT(*) FROM Hcm.Personel WHERE OrgPostId IS NOT NULL);
    PRINT N'Hrm.OrgPost total: ' + CAST(@PostTable AS nvarchar(20))
        + N' | Personel with OrgPostId: ' + CAST(@PersonelWithPost AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'Seed_HrmOrgPost_FromTotalSystem completed.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;
