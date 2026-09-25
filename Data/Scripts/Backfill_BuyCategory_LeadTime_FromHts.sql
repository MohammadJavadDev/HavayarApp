/*
================================================================================
Backfill_BuyCategory_LeadTime_FromHts.sql  (WP5 / D37)
================================================================================
نسبت با تصمیم Q2 = (a) «راهکاران مرجع کامل، دسته‌ها در سامانه جدید فقط‌خواندنی»:
  این اسکریپت یک‌بارهٴ پرکردن شکاف است، نه ویرایش کاربری — فقط ردیف‌هایی را پر می‌کند که
  راهکاران برایشان مقدار ندارد (NULL) و HTS مقدار عملیاتی دارد. با فقط‌خواندنی بودن تناقضی ندارد:
  اگر روزی راهکاران مقدار بدهد، جاب آن را بازنویسی می‌کند (قاعدهٴ جاب: «NULL راهکاران مقدار موجود را پاک نمی‌کند»،
  BuyCategoryJob.cs — بلوک UPDATE دسته). پنل هیچ مسیر ویرایشی برای این ستون‌ها ندارد.
================================================================================
پر کردن RoutineLeadTimeInDay / NonRoutineLeadTimeInDay خالی (NULL) در Sup.BuyCategory
از HTS (TotalSystem.dbo.Sup_BuyCategory) — امروز ۱۱۴/۱۱۴ دسته در جدید NULL است و
CalculateDelaysBuyDay (OpenOrderRequestJob) عملاً DelaysBuyDay را محاسبه نمی‌کند.

کلید تطبیق (همان کلید جاب SyncBuyCategoryJobFromRahkaran):
  Sup.BuyCategory.HamkaranId = USR3.Sup_BuyCategory.Sup_BuyCategoryID (راهکاران)
  = TotalSystem.dbo.Sup_BuyCategory.BuyCategory_ID (smallint، غیر identity — آینهٴ همان شناسه راهکاران؛ 04-db-truth B1)
  برای اطمینان، تطبیق عنوان (نرمال‌شده ي/ی ك/ک، بدون فاصله‌های اضافه) هم گزارش می‌شود و
  به‌صورت پیش‌فرض فقط ردیف‌هایی UPDATE می‌شوند که عنوان‌شان هم یکی است (@RequireTitleMatch = 1).

پارامترها:
  @DryRun            1 (پیش‌فرض) = فقط پیش‌نمایش + شمارش، در انتها ROLLBACK · 0 = COMMIT
  @RequireTitleMatch 1 (پیش‌فرض) = فقط ردیف‌های با عنوان یکسان · 0 = فقط کلید کافی است
  @OverwriteExisting 0 (پیش‌فرض) = فقط NULL ها پر می‌شوند · 1 = مقادیر موجود هم از HTS بازنویسی می‌شوند

پیش‌نیاز: Linked Server [TMS] → TotalSystem (Data/Scripts/CreateLinkedServer_TMS.sql). اجرا روی HavayarApp.
بعد از backfill: جاب اصلاح‌شدهٴ BuyCategoryJob مقدار NULL راهکاران را دیگر روی این دو ستون بازنویسی نمی‌کند (D37).
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun            BIT = 0;
DECLARE @RequireTitleMatch BIT = 1;
DECLARE @OverwriteExisting BIT = 0;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'backfill-buycategory-leadtime-hts';

/* ─────────────────────────────────────────────
   0) منبع HTS (یک بار از linked server) — خارج از تراکنش تا خواندن از linked server
      به تراکنش توزیع‌شده (MSDTC) ارتقا پیدا نکند
   ───────────────────────────────────────────── */
IF OBJECT_ID('tempdb..#Hts') IS NOT NULL DROP TABLE #Hts;
CREATE TABLE #Hts
(
    BuyCategory_ID          INT           NOT NULL PRIMARY KEY,
    BuyCategory_Title       NVARCHAR(400) NULL,
    User_FK                 INT           NULL,
    TypeId                  INT           NULL,
    RoutineLeadTimeInDay    INT           NULL,
    NonRoutineLeadTimeInDay INT           NULL
);

INSERT INTO #Hts (BuyCategory_ID, BuyCategory_Title, User_FK, TypeId, RoutineLeadTimeInDay, NonRoutineLeadTimeInDay)
SELECT CAST(h.BuyCategory_ID AS INT),
       h.BuyCategory_Title,
       CAST(h.User_FK AS INT),
       CAST(h.TypeId AS INT),
       CAST(h.RoutineLeadTimeInDay AS INT),
       CAST(h.NonRoutineLeadTimeInDay AS INT)
FROM [TMS].[TotalSystem].[dbo].[Sup_BuyCategory] h;

PRINT N'HTS Sup_BuyCategory rows: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

BEGIN TRY
    BEGIN TRANSACTION;

    /* ─────────────────────────────────────────────
       1) نگاشت HTS ↔ جدید
       ───────────────────────────────────────────── */
    IF OBJECT_ID('tempdb..#Map') IS NOT NULL DROP TABLE #Map;
    SELECT
        n.Id                            AS NewId,
        n.HamkaranId,
        n.Title                         AS NewTitle,
        n.RoutineLeadTimeInDay          AS NewRoutine,
        n.NonRoutineLeadTimeInDay       AS NewNonRoutine,
        h.BuyCategory_ID                AS HtsId,
        h.BuyCategory_Title             AS HtsTitle,
        h.RoutineLeadTimeInDay          AS HtsRoutine,
        h.NonRoutineLeadTimeInDay       AS HtsNonRoutine,
        CASE WHEN
            REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(ISNULL(n.Title, N''))), N'ي', N'ی'), N'ك', N'ک'), N'  ', N' ')
          = REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(ISNULL(h.BuyCategory_Title, N''))), N'ي', N'ی'), N'ك', N'ک'), N'  ', N' ')
        THEN 1 ELSE 0 END               AS TitleMatches
    INTO #Map
    FROM Sup.BuyCategory n
    INNER JOIN #Hts h ON h.BuyCategory_ID = n.HamkaranId
    WHERE n.HamkaranId IS NOT NULL;

    /* ─────────────────────────────────────────────
       2) پیش‌نمایش
       ───────────────────────────────────────────── */
    PRINT N'=== [Preview] دسته‌های جدید بدون تطبیق در HTS (HamkaranId خالی یا در HTS نیست) ===';
    SELECT n.Id, n.HamkaranId, n.Title, n.RoutineLeadTimeInDay, n.NonRoutineLeadTimeInDay
    FROM Sup.BuyCategory n
    WHERE NOT EXISTS (SELECT 1 FROM #Map m WHERE m.NewId = n.Id)
    ORDER BY n.Id;

    PRINT N'=== [Preview] تطبیق کلید ولی عنوان متفاوت (کلید را دستی بررسی کنید) ===';
    SELECT NewId, HamkaranId, NewTitle, HtsTitle, HtsRoutine, HtsNonRoutine
    FROM #Map
    WHERE TitleMatches = 0
    ORDER BY HamkaranId;

    PRINT N'=== [Preview] ردیف‌هایی که UPDATE می‌شوند ===';
    SELECT m.NewId, m.HamkaranId, m.NewTitle, m.HtsTitle,
           m.NewRoutine    AS Routine_Before,    m.HtsRoutine    AS Routine_After,
           m.NewNonRoutine AS NonRoutine_Before, m.HtsNonRoutine AS NonRoutine_After
    FROM #Map m
    WHERE (m.HtsRoutine IS NOT NULL OR m.HtsNonRoutine IS NOT NULL)
      AND (@RequireTitleMatch = 0 OR m.TitleMatches = 1)
      AND (@OverwriteExisting = 1
           OR (m.NewRoutine IS NULL AND m.HtsRoutine IS NOT NULL)
           OR (m.NewNonRoutine IS NULL AND m.HtsNonRoutine IS NOT NULL))
    ORDER BY m.HamkaranId;

    /* ─────────────────────────────────────────────
       3) شمارش قبل
       ───────────────────────────────────────────── */
    DECLARE @Total INT, @NullRoutineBefore INT, @NullNonRoutineBefore INT;
    SELECT @Total = COUNT(*),
           @NullRoutineBefore    = SUM(CASE WHEN RoutineLeadTimeInDay    IS NULL THEN 1 ELSE 0 END),
           @NullNonRoutineBefore = SUM(CASE WHEN NonRoutineLeadTimeInDay IS NULL THEN 1 ELSE 0 END)
    FROM Sup.BuyCategory;

    DECLARE @MatchedCount INT = (SELECT COUNT(*) FROM #Map);
    DECLARE @TitleMismatchCount INT = (SELECT COUNT(*) FROM #Map WHERE TitleMatches = 0);

    PRINT N'BEFORE: total=' + CAST(@Total AS NVARCHAR(10))
        + N' | Routine NULL=' + CAST(@NullRoutineBefore AS NVARCHAR(10))
        + N' | NonRoutine NULL=' + CAST(@NullNonRoutineBefore AS NVARCHAR(10))
        + N' | matched(HTS)=' + CAST(@MatchedCount AS NVARCHAR(10))
        + N' | title-mismatch=' + CAST(@TitleMismatchCount AS NVARCHAR(10));

    /* ─────────────────────────────────────────────
       4) UPDATE
       ───────────────────────────────────────────── */
    UPDATE n
    SET n.RoutineLeadTimeInDay =
            CASE WHEN m.HtsRoutine IS NOT NULL AND (@OverwriteExisting = 1 OR n.RoutineLeadTimeInDay IS NULL)
                 THEN m.HtsRoutine ELSE n.RoutineLeadTimeInDay END,
        n.NonRoutineLeadTimeInDay =
            CASE WHEN m.HtsNonRoutine IS NOT NULL AND (@OverwriteExisting = 1 OR n.NonRoutineLeadTimeInDay IS NULL)
                 THEN m.HtsNonRoutine ELSE n.NonRoutineLeadTimeInDay END,
        n.ModifiedById = 1,
        n.ModifiedByName = @SeedUser,
        n.ModifiedDateMiladiDateTime = @Now,
        n.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sup.BuyCategory n
    INNER JOIN #Map m ON m.NewId = n.Id
    WHERE (m.HtsRoutine IS NOT NULL OR m.HtsNonRoutine IS NOT NULL)
      AND (@RequireTitleMatch = 0 OR m.TitleMatches = 1)
      AND (@OverwriteExisting = 1
           OR (n.RoutineLeadTimeInDay IS NULL AND m.HtsRoutine IS NOT NULL)
           OR (n.NonRoutineLeadTimeInDay IS NULL AND m.HtsNonRoutine IS NOT NULL));

    PRINT N'UPDATED rows: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

    /* ─────────────────────────────────────────────
       5) شمارش بعد
       ───────────────────────────────────────────── */
    DECLARE @NullRoutineAfter INT, @NullNonRoutineAfter INT;
    SELECT @NullRoutineAfter    = SUM(CASE WHEN RoutineLeadTimeInDay    IS NULL THEN 1 ELSE 0 END),
           @NullNonRoutineAfter = SUM(CASE WHEN NonRoutineLeadTimeInDay IS NULL THEN 1 ELSE 0 END)
    FROM Sup.BuyCategory;

    PRINT N'AFTER:  total=' + CAST(@Total AS NVARCHAR(10))
        + N' | Routine NULL=' + CAST(@NullRoutineAfter AS NVARCHAR(10))
        + N' | NonRoutine NULL=' + CAST(@NullNonRoutineAfter AS NVARCHAR(10));

    IF @DryRun = 1
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'=== DRY RUN — تغییرات ROLLBACK شد. برای اعمال: SET @DryRun = 0 ===';
    END
    ELSE
    BEGIN
        COMMIT TRANSACTION;
        PRINT N'=== COMMITTED Backfill_BuyCategory_LeadTime_FromHts ===';
    END
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

-- وضعیت فعلی (بعد از COMMIT واقعی معنی‌دار است)
SELECT Id, HamkaranId, Title, Type, PurchaseResponsibleId, RoutineLeadTimeInDay, NonRoutineLeadTimeInDay, ModifiedDateShamsiDateTime
FROM Sup.BuyCategory
ORDER BY CASE WHEN RoutineLeadTimeInDay IS NULL AND NonRoutineLeadTimeInDay IS NULL THEN 1 ELSE 0 END, Title;
