-- ============================================================================
-- WP6 / D5 (Q3-a) — کپی یک‌باره «زمان در راه» از HTS (TotalSystem.dbo.Pln_LeadTime، ~3,202 ردیف)
--                    به HavayarApp (Pln.LeadTime) — بعد از آن نگهداری در سیستم جدید (CRUD + ورود اکسل)
--
-- منبع: Linked Server [TMS] → [TotalSystem].[dbo].[Pln_LeadTime]
--   (Id, PartId→Inv_Part, LeadTimeInDay, LeadTimeInMinute (محاسبه‌ای؛ کپی نمی‌شود), NonRoutineLeadTimeInDay,
--    SupplierUnitId→HRM_OrgUnit {6 بازرگانی خارجی, 51 تامین و خرید}, CreatedUserId→Gnr_User,
--    CreatedDate, CreatedDateInText, Comment, RahkaranPartId)
--
-- نگاشت‌ها:
--   کالا:        Inv.Part.HtsId = Pln_LeadTime.PartId
--                → fallback Inv.Part.HamkaranId = COALESCE(Inv_Part.Hamkaran_Part_FK, Pln_LeadTime.RahkaranPartId)
--                → fallback Inv.Part.Code = Inv_Part.Part_Code            (ردیف‌های بی‌تطبیق گزارش می‌شوند)
--   تامین‌کننده: SupplierUnitId 6/51 → Pln.LeadTime.Supplier (LeadTimeSupplierEnum همان مقادیر 6/51)
--                مقادیر دیگر → رد و گزارش
--   کاربر:       Gnr_User.Username → system.User.Username،
--                fallback LOWER(Gnr_User.ActiveDirectoryUsername) = LOWER(system.User.Username)
--                بی‌تطبیق → CreatedById NULL و CreatedByName = Gnr_User.FullName
--   زمان:        LeadTimeInDay → LeadTimeDay | NonRoutineLeadTimeInDay → NonRoutineLeadTimeInDay | Comment → Comment
--   تاریخ:       CreatedDate → CreatedOnMiladiDateTime | CreatedDateInText → CreatedOnShamsiDateTime
--
-- کلید upsert: (PartId, Supplier). اگر HTS برای یک کالا/تامین‌کننده چند ردیف داشته باشد
--   (HTS فقط تکراری «کالا+تامین‌کننده+زمان» را رد می‌کرد) جدیدترین ردیف (CreatedDate, Id) برنده است
--   و تعداد ردیف‌های ادغام‌شده گزارش می‌شود.
--
-- اجرا:
--   1) @DryRun = 1 (پیش‌فرض): فقط شمارش‌ها + پیش‌نمایش درج/به‌روزرسانی + فهرست بی‌تطبیق‌ها — بدون تغییر داده
--   2) بعد از بررسی: @DryRun = 0 → اجرای واقعی در یک تراکنش
-- پیش‌نیاز: Linked Server TMS (CreateLinkedServer_TMS.sql)، ستون NonRoutineLeadTimeInDay (migration / Add_LeadTime_NonRoutineLeadTimeInDay.sql)
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = 1;                                    -- 0 = اجرای واقعی
DECLARE @SeedUser NVARCHAR(150) = N'migrate-leadtime-hts';
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] وجود ندارد. ابتدا Data/Scripts/CreateLinkedServer_TMS.sql را اجرا کنید.', 16, 1);
    RETURN;
END

IF COL_LENGTH('Pln.LeadTime', 'NonRoutineLeadTimeInDay') IS NULL
BEGIN
    RAISERROR(N'ستون Pln.LeadTime.NonRoutineLeadTimeInDay وجود ندارد. ابتدا migration AddLeadTimeNonRoutineLeadTime یا Add_LeadTime_NonRoutineLeadTimeInDay.sql را اجرا کنید.', 16, 1);
    RETURN;
END

BEGIN TRY
    /* ─────────────────────────────────────────────
       0) منبع HTS (یک رفت‌وبرگشت به linked server)
       ───────────────────────────────────────────── */
    IF OBJECT_ID('tempdb..#Src') IS NOT NULL DROP TABLE #Src;
    IF OBJECT_ID('tempdb..#PartByHamkaran') IS NOT NULL DROP TABLE #PartByHamkaran;
    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    IF OBJECT_ID('tempdb..#Final') IS NOT NULL DROP TABLE #Final;

    SELECT
        CAST(lt.Id AS BIGINT)                 AS HtsId,
        CAST(lt.PartId AS BIGINT)             AS HtsPartId,
        CAST(lt.LeadTimeInDay AS INT)         AS LeadTimeInDay,
        CAST(lt.NonRoutineLeadTimeInDay AS INT) AS NonRoutineLeadTimeInDay,
        CAST(lt.SupplierUnitId AS INT)        AS SupplierUnitId,
        CAST(lt.CreatedUserId AS INT)         AS HtsUserId,
        CAST(lt.CreatedDate AS DATETIME2)     AS CreatedDate,
        CAST(lt.CreatedDateInText AS NVARCHAR(30)) AS CreatedDateInText,
        CAST(lt.Comment AS NVARCHAR(MAX))     AS Comment,
        CAST(lt.RahkaranPartId AS BIGINT)     AS RahkaranPartId,
        CAST(op.Part_Code AS NVARCHAR(40))    AS Part_Code,
        CAST(op.Part_Name AS NVARCHAR(510))   AS Part_Name,
        CAST(op.Hamkaran_Part_FK AS BIGINT)   AS Hamkaran_Part_FK,
        CAST(ou.Username AS NVARCHAR(160))    AS HtsUsername,
        CAST(ou.ActiveDirectoryUsername AS NVARCHAR(800)) AS HtsAdUsername,
        CAST(ou.FullName AS NVARCHAR(150))    AS HtsUserFullName,
        CAST(NULL AS BIGINT)                  AS NewPartId,
        CAST(NULL AS NVARCHAR(20))            AS PartMatchBy,
        CAST(NULL AS BIGINT)                  AS NewUserId,
        CAST(NULL AS NVARCHAR(150))           AS NewUserName
    INTO #Src
    FROM [TMS].[TotalSystem].[dbo].[Pln_LeadTime] lt
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] op ON op.Part_ID = lt.PartId
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] ou ON ou.User_ID = lt.CreatedUserId;

    CREATE UNIQUE CLUSTERED INDEX IX_Src ON #Src(HtsId);

    DECLARE @SrcRows INT = (SELECT COUNT(*) FROM #Src);
    PRINT N'HTS Pln_LeadTime rows: ' + CAST(@SrcRows AS NVARCHAR(20));

    /* ─────────────────────────────────────────────
       1) نگاشت کالا: HtsId → HamkaranId → Code
       ───────────────────────────────────────────── */
    UPDATE s
    SET NewPartId = p.Id, PartMatchBy = N'HtsId'
    FROM #Src s
    INNER JOIN Inv.Part p ON p.HtsId = s.HtsPartId AND p.HtsId <> 0
    WHERE s.NewPartId IS NULL;

    -- یک ردیف به ازای هر HamkaranId (اولویت: فعال، سپس کوچک‌ترین Id)
    ;WITH Ranked AS
    (
        SELECT p.Id, p.HamkaranId,
               ROW_NUMBER() OVER (PARTITION BY p.HamkaranId
                                  ORDER BY CASE WHEN p.IsActive = 1 THEN 0 ELSE 1 END, p.Id) AS Rn
        FROM Inv.Part p
        WHERE p.HamkaranId IS NOT NULL AND p.HamkaranId <> 0
    )
    SELECT Id, HamkaranId INTO #PartByHamkaran FROM Ranked WHERE Rn = 1;
    CREATE UNIQUE CLUSTERED INDEX IX_PartByHamkaran ON #PartByHamkaran(HamkaranId);

    UPDATE s
    SET NewPartId = p.Id, PartMatchBy = N'HamkaranId'
    FROM #Src s
    INNER JOIN #PartByHamkaran p ON p.HamkaranId = COALESCE(s.Hamkaran_Part_FK, s.RahkaranPartId)
    WHERE s.NewPartId IS NULL;

    UPDATE s
    SET NewPartId = p.Id, PartMatchBy = N'Code'
    FROM #Src s
    INNER JOIN Inv.Part p ON p.Code = LTRIM(RTRIM(s.Part_Code))
    WHERE s.NewPartId IS NULL
      AND NULLIF(LTRIM(RTRIM(s.Part_Code)), N'') IS NOT NULL;

    /* ─────────────────────────────────────────────
       2) نگاشت کاربر: Username → ActiveDirectoryUsername
       ───────────────────────────────────────────── */
    SELECT hu.HtsUserId, x.NewUserId, x.NewUserName
    INTO #UserMap
    FROM (SELECT DISTINCT HtsUserId, HtsUsername, HtsAdUsername FROM #Src WHERE HtsUserId IS NOT NULL) hu
    CROSS APPLY
    (
        SELECT TOP (1)
               u.Id AS NewUserId,
               COALESCE(NULLIF(LTRIM(RTRIM(u.NameFa)), N''), u.Name) AS NewUserName
        FROM [system].[User] u
        WHERE (hu.HtsUsername IS NOT NULL AND u.Username = hu.HtsUsername)
           OR (hu.HtsAdUsername IS NOT NULL AND LOWER(u.Username) = LOWER(hu.HtsAdUsername))
        ORDER BY CASE WHEN u.Username = hu.HtsUsername THEN 0 ELSE 1 END,
                 CASE WHEN u.IsActive = 1 THEN 0 ELSE 1 END,
                 u.Id
    ) x;

    UPDATE s
    SET NewUserId = m.NewUserId, NewUserName = m.NewUserName
    FROM #Src s
    INNER JOIN #UserMap m ON m.HtsUserId = s.HtsUserId;

    /* ─────────────────────────────────────────────
       3) ردیف‌های قابل انتقال + ادغام تکراری‌ها روی (PartId, Supplier)
       ───────────────────────────────────────────── */
    ;WITH Valid AS
    (
        SELECT s.*,
               ROW_NUMBER() OVER (PARTITION BY s.NewPartId, s.SupplierUnitId
                                  ORDER BY s.CreatedDate DESC, s.HtsId DESC) AS Rn
        FROM #Src s
        WHERE s.NewPartId IS NOT NULL
          AND s.SupplierUnitId IN (6, 51)
    )
    SELECT v.HtsId, v.NewPartId, v.SupplierUnitId, v.LeadTimeInDay, v.NonRoutineLeadTimeInDay,
           NULLIF(LTRIM(RTRIM(v.Comment)), N'') AS Comment,
           v.CreatedDate, v.CreatedDateInText,
           v.NewUserId,
           COALESCE(v.NewUserName, NULLIF(LTRIM(RTRIM(v.HtsUserFullName)), N''), @SeedUser) AS CreatedByName,
           e.Id AS ExistingId
    INTO #Final
    FROM Valid v
    LEFT JOIN Pln.LeadTime e ON e.PartId = v.NewPartId AND e.Supplier = v.SupplierUnitId
    WHERE v.Rn = 1;

    /* ─────────────────────────────────────────────
       4) شمارش‌ها
       ───────────────────────────────────────────── */
    DECLARE @MatchedParts INT   = (SELECT COUNT(*) FROM #Src WHERE NewPartId IS NOT NULL);
    DECLARE @UnmatchedParts INT = (SELECT COUNT(*) FROM #Src WHERE NewPartId IS NULL);
    DECLARE @BadSupplier INT    = (SELECT COUNT(*) FROM #Src WHERE NewPartId IS NOT NULL AND SupplierUnitId NOT IN (6, 51));
    DECLARE @Collapsed INT      = (SELECT COUNT(*) FROM #Src WHERE NewPartId IS NOT NULL AND SupplierUnitId IN (6, 51)) - (SELECT COUNT(*) FROM #Final);
    DECLARE @ToInsert INT       = (SELECT COUNT(*) FROM #Final WHERE ExistingId IS NULL);
    DECLARE @ToUpdate INT       = (SELECT COUNT(*) FROM #Final WHERE ExistingId IS NOT NULL);
    DECLARE @UsersUnmatched INT = (SELECT COUNT(DISTINCT HtsUserId) FROM #Src WHERE HtsUserId IS NOT NULL AND NewUserId IS NULL);
    DECLARE @NonPositive INT    = (SELECT COUNT(*) FROM #Final WHERE LeadTimeInDay <= 0);
    DECLARE @ByHtsId INT        = (SELECT COUNT(*) FROM #Src WHERE PartMatchBy = N'HtsId');
    DECLARE @ByHamkaranId INT   = (SELECT COUNT(*) FROM #Src WHERE PartMatchBy = N'HamkaranId');
    DECLARE @ByCode INT         = (SELECT COUNT(*) FROM #Src WHERE PartMatchBy = N'Code');

    PRINT N'--- Summary (' + CASE WHEN @DryRun = 1 THEN N'DRY RUN' ELSE N'APPLY' END + N') ---';
    PRINT N'Source rows              : ' + CAST(@SrcRows AS NVARCHAR(20));
    PRINT N'Matched parts            : ' + CAST(@MatchedParts AS NVARCHAR(20))
        + N'  (HtsId=' + CAST(@ByHtsId AS NVARCHAR(20))
        + N', HamkaranId=' + CAST(@ByHamkaranId AS NVARCHAR(20))
        + N', Code=' + CAST(@ByCode AS NVARCHAR(20)) + N')';
    PRINT N'Unmatched parts (skipped): ' + CAST(@UnmatchedParts AS NVARCHAR(20));
    PRINT N'Bad supplier (skipped)   : ' + CAST(@BadSupplier AS NVARCHAR(20));
    PRINT N'Collapsed duplicates     : ' + CAST(@Collapsed AS NVARCHAR(20));
    PRINT N'To insert                : ' + CAST(@ToInsert AS NVARCHAR(20));
    PRINT N'To update                : ' + CAST(@ToUpdate AS NVARCHAR(20));
    PRINT N'HTS users without match  : ' + CAST(@UsersUnmatched AS NVARCHAR(20)) + N'  (CreatedById=NULL, نام HTS نگه داشته می‌شود)';
    PRINT N'LeadTimeInDay <= 0       : ' + CAST(@NonPositive AS NVARCHAR(20)) + N'  (عیناً کپی می‌شود؛ ورود اکسل جدید > 0 می‌خواهد)';

    -- فهرست بی‌تطبیق‌ها
    SELECT
        CASE WHEN s.NewPartId IS NULL THEN N'کالا یافت نشد' ELSE N'تامین‌کننده نامعتبر' END AS Reason,
        s.HtsId, s.HtsPartId, s.Part_Code, s.Part_Name, s.Hamkaran_Part_FK, s.RahkaranPartId,
        s.SupplierUnitId, s.LeadTimeInDay, s.NonRoutineLeadTimeInDay, s.CreatedDateInText, s.HtsUserFullName
    FROM #Src s
    WHERE s.NewPartId IS NULL OR s.SupplierUnitId NOT IN (6, 51)
    ORDER BY Reason, s.Part_Code, s.HtsId;

    -- کاربران HTS بدون معادل
    SELECT DISTINCT s.HtsUserId, s.HtsUsername, s.HtsAdUsername, s.HtsUserFullName, COUNT(*) OVER (PARTITION BY s.HtsUserId) AS RowCnt
    FROM #Src s
    WHERE s.HtsUserId IS NOT NULL AND s.NewUserId IS NULL
    ORDER BY s.HtsUserId;

    IF @DryRun = 1
    BEGIN
        -- پیش‌نمایش برنامه درج/به‌روزرسانی
        SELECT
            CASE WHEN f.ExistingId IS NULL THEN N'INSERT' ELSE N'UPDATE' END AS Action,
            f.HtsId, f.NewPartId, p.Code AS PartCode, p.Name AS PartName,
            f.SupplierUnitId AS Supplier, f.LeadTimeInDay, f.NonRoutineLeadTimeInDay, f.Comment,
            f.CreatedDate, f.CreatedDateInText, f.NewUserId, f.CreatedByName, f.ExistingId
        FROM #Final f
        INNER JOIN Inv.Part p ON p.Id = f.NewPartId
        ORDER BY Action, p.Code, f.SupplierUnitId;

        PRINT N'DRY RUN — هیچ تغییری اعمال نشد. برای اجرای واقعی @DryRun = 0 بگذارید.';
        RETURN;
    END

    /* ─────────────────────────────────────────────
       5) اعمال (upsert)
       ───────────────────────────────────────────── */
    DECLARE @Updated INT = 0, @Inserted INT = 0;

    BEGIN TRANSACTION;

    UPDATE e
    SET e.LeadTimeDay = f.LeadTimeInDay,
        e.NonRoutineLeadTimeInDay = COALESCE(f.NonRoutineLeadTimeInDay, e.NonRoutineLeadTimeInDay),
        e.Comment = COALESCE(f.Comment, e.Comment),
        e.ModifiedById = 1,
        e.ModifiedByName = @SeedUser,
        e.ModifiedDateMiladiDateTime = @Now,
        e.ModifiedDateShamsiDateTime = @NowShamsi,
        e.IsActive = 1
    FROM Pln.LeadTime e
    INNER JOIN #Final f ON f.ExistingId = e.Id;
    SET @Updated = @@ROWCOUNT;

    INSERT INTO Pln.LeadTime
        (PartId, LeadTimeDay, NonRoutineLeadTimeInDay, Supplier, Comment,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
         IsActive)
    SELECT
        f.NewPartId, f.LeadTimeInDay, f.NonRoutineLeadTimeInDay, f.SupplierUnitId, f.Comment,
        f.NewUserId, f.NewUserId, f.CreatedByName, f.CreatedByName,
        f.CreatedDate, f.CreatedDateInText, f.CreatedDate, f.CreatedDateInText,
        1
    FROM #Final f
    WHERE f.ExistingId IS NULL;
    SET @Inserted = @@ROWCOUNT;

    COMMIT TRANSACTION;

    DECLARE @TotalRows INT = (SELECT COUNT(*) FROM Pln.LeadTime);

    PRINT N'--- Applied ---';
    PRINT N'Inserted: ' + CAST(@Inserted AS NVARCHAR(20));
    PRINT N'Updated : ' + CAST(@Updated AS NVARCHAR(20));
    PRINT N'Pln.LeadTime total rows: ' + CAST(@TotalRows AS NVARCHAR(20));
    PRINT N'=== DONE Migrate_LeadTime_FromHts ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
