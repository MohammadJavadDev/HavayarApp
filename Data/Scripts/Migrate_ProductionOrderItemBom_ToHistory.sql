/*
================================================================================
Migrate_ProductionOrderItemBom_ToHistory.sql
================================================================================
انتقال BOM اقلام سفارش ساخت به طراحی «جدول اصلی = فقط آخرین نسخه + جدول تاریخچه = همهٔ رخدادها».

پیش‌نیاز: EF Migration «AddProductionOrderItemBomHistory» اعمال شده باشد (جدول تاریخچه + ستون NeedsInspection).
WebApp و BackgroundJob باید متوقف باشند (جاب روتین هر ۶۰ ثانیه روی این جدول می‌نویسد).

مراحل:
  A) فقط‌خواندنی: ساخت جداول موقت، رتبه‌بندی هر گروه (قلم، کالا)، محاسبهٔ نتیجه و گزارش پیش‌بینی.
     DryRun=1 همین‌جا تمام می‌شود و به جداول واقعی دست نمی‌زند.
  B) فقط با DryRun=0، در یک تراکنش:
       1) درج همهٔ ردیف‌های فعلی در تاریخچه (نوع ایجاد برای نسخهٔ ۰، ویرایش برای بقیه)
       2) درج رخدادهای «ملاحظات» HTS (Pln_ProductionOrderItemBomChanges) در تاریخچه
       3) به‌روزرسانی ردیف نگه‌داشته‌شدهٔ هر گروه (Revision، IsLatest، فیلدهای اصلاح‌شده، ممیزی)
       4) حذف ردیف‌های اضافی
       5) گیت‌های تأیید؛ در صورت شکست همه چیز Rollback می‌شود.

قواعد:
  - ردیف نگه‌داشته‌شده: آخرین ردیف هر گروه با ORDER BY ISNULL(Revision,-1), Id
    (در ۳٬۰۸۵ گروه بیشترین Revision همان آخرین نسخهٔ واقعی HTS است، نه بیشترین Id).
  - Revision همهٔ ردیف‌های هر گروه از ۰ دوباره شمرده می‌شود.
  - ردیف‌های واردشده از HTS (ایجادکنندهٔ sync-po-item-bom-hts / sync-po-legacy-no-rahkaran / خالی):
    ایجادکننده و تاریخ‌ها از HTS برگردانده می‌شود (کلید: RahkaranHistoryId یا -Id قلم قدیمی + Revision + کالا).
  - فقط برای ردیف‌هایی که بعد از ورود از HTS در پورتال ویرایش نشده‌اند:
    NeedsAVL / NeedsInspection / SaleUnitDetails از آخرین رخداد ملاحظات HTS بازسازی می‌شود
    (در HTS مقدار HasNeedToAvl با HasNeedToBuyOrBuild یکی می‌شد). در نبود رخداد، پرچم‌های خود ردیف HTS.

اجرا (UTF-8 با BOM الزامی است):
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -v DryRun=1 -i "Data\Scripts\Migrate_ProductionOrderItemBom_ToHistory.sql"
  بعد از تأیید گزارش:  -v DryRun=0

پشتیبان جدول (فقط DryRun=0): Sale.ProductionOrderItemBom_Bak_20261014
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = $(DryRun);

/* ---------------------------------------------------------------------------
   0) پیش‌بررسی
   --------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
    THROW 51001, N'Linked server [TMS] not found.', 1;

IF OBJECT_ID(N'Sale.ProductionOrderItemBomHistory') IS NULL
    THROW 51002, N'History table Sale.ProductionOrderItemBomHistory does not exist. Apply EF migration first.', 1;

IF COL_LENGTH(N'Sale.ProductionOrderItemBom', N'NeedsInspection') IS NULL
    THROW 51003, N'Column Sale.ProductionOrderItemBom.NeedsInspection does not exist. Apply EF migration first.', 1;

IF EXISTS (SELECT 1 FROM Sale.ProductionOrderItemBomHistory)
    THROW 51004, N'History table is not empty. This script must run once; restore from backup table to re-run.', 1;

DECLARE @RowsBefore INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItemBom);
DECLARE @NullRevisionBefore INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItemBom WHERE Revision IS NULL);
PRINT N'Main rows before: ' + CAST(@RowsBefore AS NVARCHAR(20)) + N' (NULL revision: ' + CAST(@NullRevisionBefore AS NVARCHAR(20)) + N')';

/* ---------------------------------------------------------------------------
   A) فقط‌خواندنی: جداول موقت
   --------------------------------------------------------------------------- */
IF OBJECT_ID(N'tempdb..#HtsUser') IS NOT NULL DROP TABLE #HtsUser;
SELECT hu.User_ID AS HtsUserId, hu.FullName AS HtsFullName, nu.Id AS NewUserId, nu.Name AS NewName
INTO #HtsUser
FROM [TMS].[TotalSystem].[dbo].[Gnr_User] hu
OUTER APPLY (
    SELECT TOP (1) u.Id, u.Name
    FROM system.[User] u
    WHERE LOWER(u.Username) = LOWER(hu.ActiveDirectoryUsername)
    ORDER BY CASE WHEN u.IsActive = 1 THEN 0 ELSE 1 END, u.Id
) nu;
CREATE UNIQUE CLUSTERED INDEX IX_HtsUser ON #HtsUser (HtsUserId);

IF OBJECT_ID(N'tempdb..#PartMap') IS NOT NULL DROP TABLE #PartMap;
SELECT x.HtsPartId, COALESCE(pHts.Id, pCode.Id) AS NewPartId
INTO #PartMap
FROM (
    SELECT DISTINCT CAST(b.PartId AS BIGINT) AS HtsPartId, hp.Part_Code AS HtsPartCode
    FROM [TMS].[TotalSystem].[dbo].[Pln_ProductionOrderItemBom] b
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] hp ON hp.Part_ID = b.PartId
) x
LEFT JOIN Inv.Part pHts ON pHts.HtsId = x.HtsPartId AND pHts.HtsId <> 0
OUTER APPLY (
    SELECT TOP (1) p.Id
    FROM Inv.Part p
    WHERE pHts.Id IS NULL
      AND x.HtsPartCode IS NOT NULL
      AND p.Code = x.HtsPartCode
    ORDER BY CASE WHEN p.IsActive = 1 THEN 0 ELSE 1 END, p.Id
) pCode;
CREATE UNIQUE CLUSTERED INDEX IX_PartMap ON #PartMap (HtsPartId);

IF OBJECT_ID(N'tempdb..#HtsBom') IS NOT NULL DROP TABLE #HtsBom;
SELECT
    b.Id AS HtsBomId,
    CASE WHEN hi.RahkaranId IS NOT NULL THEN CAST(hi.RahkaranId AS BIGINT) ELSE -CAST(hi.Id AS BIGINT) END AS AppRahkaranHistoryId,
    CAST(b.Revision AS INT) AS Revision,
    CAST(b.PartId AS BIGINT) AS HtsPartId,
    b.CreatedUserId, b.CreatedDate, b.CreatedDateInText,
    b.UpdatedUserId, b.UpdatedDate, b.UpdatedDateInText,
    b.HasNeedToAvl, b.HasNeedToBuyOrBuild, b.SalesUnitComment
INTO #HtsBom
FROM [TMS].[TotalSystem].[dbo].[Pln_ProductionOrderItemBom] b
INNER JOIN [TMS].[TotalSystem].[dbo].[Pln_ProductionOrderItem] hi ON hi.Id = b.ProductionOrderItemId;
CREATE UNIQUE CLUSTERED INDEX IX_HtsBom ON #HtsBom (HtsBomId);

IF OBJECT_ID(N'tempdb..#HtsMapAll') IS NOT NULL DROP TABLE #HtsMapAll;
SELECT h.HtsBomId, ai.Id AS NewItemId, pm.NewPartId, h.Revision,
       h.CreatedUserId, h.CreatedDate, h.CreatedDateInText,
       h.UpdatedUserId, h.UpdatedDate, h.UpdatedDateInText,
       h.HasNeedToAvl, h.HasNeedToBuyOrBuild, h.SalesUnitComment
INTO #HtsMapAll
FROM #HtsBom h
INNER JOIN Sale.ProductionOrderItem ai ON ai.RahkaranHistoryId = h.AppRahkaranHistoryId
INNER JOIN #PartMap pm ON pm.HtsPartId = h.HtsPartId AND pm.NewPartId IS NOT NULL;
CREATE CLUSTERED INDEX IX_HtsMapAll ON #HtsMapAll (HtsBomId);

IF OBJECT_ID(N'tempdb..#HtsMap') IS NOT NULL DROP TABLE #HtsMap;
SELECT q.*
INTO #HtsMap
FROM (
    SELECT a.*, ROW_NUMBER() OVER (PARTITION BY a.NewItemId, a.NewPartId, a.Revision ORDER BY a.HtsBomId DESC) AS rn
    FROM #HtsMapAll a
) q
WHERE q.rn = 1;
CREATE UNIQUE CLUSTERED INDEX IX_HtsMap ON #HtsMap (NewItemId, NewPartId, Revision);

IF OBJECT_ID(N'tempdb..#Ranked') IS NOT NULL DROP TABLE #Ranked;
SELECT
    m.Id,
    m.ProductionOrderItemId,
    m.PartId,
    m.Revision AS OldRevision,
    CAST(ROW_NUMBER() OVER (PARTITION BY m.ProductionOrderItemId, m.PartId ORDER BY ISNULL(m.Revision, -1), m.Id) - 1 AS INT) AS NewRevision,
    COUNT(*) OVER (PARTITION BY m.ProductionOrderItemId, m.PartId) AS GroupCnt,
    m.Amount, m.Description, m.SaleUnitDetails, m.NeedsAVL, m.ProductionStep, m.NumberSupplied, m.Status,
    m.CreatedById, m.CreatedByName, m.CreatedOnMiladiDateTime, m.CreatedOnShamsiDateTime,
    m.ModifiedById, m.ModifiedByName, m.ModifiedDateMiladiDateTime, m.ModifiedDateShamsiDateTime,
    CAST(CASE WHEN m.CreatedById IS NULL
                OR m.CreatedByName IN (N'sync-po-item-bom-hts', N'sync-po-legacy-no-rahkaran') THEN 1 ELSE 0 END AS BIT) AS IsImported,
    CAST(0 AS BIT) AS IsUntouched,
    CAST(0 AS BIT) AS IsKeep
INTO #Ranked
FROM Sale.ProductionOrderItemBom m;

UPDATE #Ranked
SET IsKeep = CASE WHEN NewRevision = GroupCnt - 1 THEN 1 ELSE 0 END,
    IsUntouched = CASE WHEN IsImported = 1 AND (ModifiedByName IS NULL OR ModifiedByName LIKE N'sync-%') THEN 1 ELSE 0 END;

CREATE UNIQUE CLUSTERED INDEX IX_Ranked ON #Ranked (Id);
CREATE INDEX IX_Ranked_Key ON #Ranked (ProductionOrderItemId, PartId, OldRevision);

IF OBJECT_ID(N'tempdb..#HtsEv') IS NOT NULL DROP TABLE #HtsEv;
SELECT c.Id AS ChangeId, c.ProductionOrderItemBomId AS HtsBomId,
       c.HasNeedToBuyOrBuild, c.HasNeedToAvl, c.SalesUnitComment,
       c.CreatedUserId, c.CreatedDate, c.CreatedDateInText
INTO #HtsEv
FROM [TMS].[TotalSystem].[dbo].[Pln_ProductionOrderItemBomChanges] c;

IF OBJECT_ID(N'tempdb..#EvRows') IS NOT NULL DROP TABLE #EvRows;
SELECT e.ChangeId, r.Id AS RankedId, r.ProductionOrderItemId, r.PartId, r.NewRevision,
       e.HasNeedToBuyOrBuild, e.HasNeedToAvl, e.SalesUnitComment,
       e.CreatedUserId, e.CreatedDate, e.CreatedDateInText
INTO #EvRows
FROM #HtsEv e
INNER JOIN #HtsMapAll m ON m.HtsBomId = e.HtsBomId
INNER JOIN #Ranked r
    ON r.ProductionOrderItemId = m.NewItemId
   AND r.PartId = m.NewPartId
   AND r.OldRevision = m.Revision
   AND r.IsImported = 1;
CREATE CLUSTERED INDEX IX_EvRows ON #EvRows (RankedId, CreatedDate, ChangeId);

IF OBJECT_ID(N'tempdb..#Final') IS NOT NULL DROP TABLE #Final;
SELECT
    r.Id, r.ProductionOrderItemId, r.PartId, r.NewRevision, r.IsKeep,
    r.Amount, r.Description,
    CASE WHEN r.IsUntouched = 1 AND le.ChangeId IS NOT NULL THEN ISNULL(le.SalesUnitComment, r.SaleUnitDetails)
         ELSE r.SaleUnitDetails END AS SaleUnitDetails,
    CAST(CASE WHEN r.IsUntouched = 1 AND le.ChangeId IS NOT NULL THEN ISNULL(le.HasNeedToAvl, 0)
              WHEN r.IsUntouched = 1 AND h.NewItemId IS NOT NULL THEN ISNULL(h.HasNeedToAvl, 0)
              ELSE r.NeedsAVL END AS BIT) AS NeedsAVL,
    CAST(CASE WHEN r.IsUntouched = 1 AND le.ChangeId IS NOT NULL THEN ISNULL(le.HasNeedToBuyOrBuild, 0)
              WHEN r.IsUntouched = 1 AND h.NewItemId IS NOT NULL THEN ISNULL(h.HasNeedToBuyOrBuild, 0)
              ELSE 0 END AS BIT) AS NeedsInspection,
    r.ProductionStep, r.NumberSupplied, r.Status,
    CASE WHEN r.IsImported = 1 AND h.NewItemId IS NOT NULL THEN cu.NewUserId ELSE r.CreatedById END AS CreatedById,
    CASE WHEN r.IsImported = 1 AND h.NewItemId IS NOT NULL THEN COALESCE(cu.NewName, cu.HtsFullName) ELSE r.CreatedByName END AS CreatedByName,
    CASE WHEN r.IsImported = 1 AND h.NewItemId IS NOT NULL THEN h.CreatedDate ELSE r.CreatedOnMiladiDateTime END AS CreatedOnMiladiDateTime,
    CASE WHEN r.IsImported = 1 AND h.NewItemId IS NOT NULL THEN h.CreatedDateInText ELSE r.CreatedOnShamsiDateTime END AS CreatedOnShamsiDateTime,
    CASE WHEN r.IsUntouched = 1 AND h.NewItemId IS NOT NULL THEN uu.NewUserId ELSE r.ModifiedById END AS ModifiedById,
    CASE WHEN r.IsUntouched = 1 AND h.NewItemId IS NOT NULL THEN COALESCE(uu.NewName, uu.HtsFullName) ELSE r.ModifiedByName END AS ModifiedByName,
    CASE WHEN r.IsUntouched = 1 AND h.NewItemId IS NOT NULL THEN h.UpdatedDate ELSE r.ModifiedDateMiladiDateTime END AS ModifiedDateMiladiDateTime,
    CASE WHEN r.IsUntouched = 1 AND h.NewItemId IS NOT NULL THEN h.UpdatedDateInText ELSE r.ModifiedDateShamsiDateTime END AS ModifiedDateShamsiDateTime,
    CAST(CASE WHEN r.IsImported = 1 AND h.NewItemId IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS CreatorBackfilled,
    CAST(CASE WHEN le.ChangeId IS NOT NULL AND r.IsUntouched = 1 THEN 1 ELSE 0 END AS BIT) AS FlagsFromEvent,
    r.NeedsAVL AS OldNeedsAVL,
    r.SaleUnitDetails AS OldSaleUnitDetails
INTO #Final
FROM #Ranked r
LEFT JOIN #HtsMap h
    ON r.IsImported = 1
   AND h.NewItemId = r.ProductionOrderItemId
   AND h.NewPartId = r.PartId
   AND h.Revision = r.OldRevision
LEFT JOIN #HtsUser cu ON cu.HtsUserId = h.CreatedUserId
LEFT JOIN #HtsUser uu ON uu.HtsUserId = h.UpdatedUserId
OUTER APPLY (
    SELECT TOP (1) e.ChangeId, e.HasNeedToBuyOrBuild, e.HasNeedToAvl, e.SalesUnitComment
    FROM #EvRows e
    WHERE e.RankedId = r.Id
    ORDER BY e.CreatedDate DESC, e.ChangeId DESC
) le;
CREATE UNIQUE CLUSTERED INDEX IX_Final ON #Final (Id);

/* گزارش پیش‌بینی */
DECLARE @Groups INT = (SELECT COUNT(*) FROM #Final WHERE IsKeep = 1);
DECLARE @ToDelete INT = (SELECT COUNT(*) FROM #Final WHERE IsKeep = 0);
DECLARE @EventsMapped INT = (SELECT COUNT(*) FROM #EvRows);
DECLARE @EventsTotal INT = (SELECT COUNT(*) FROM #HtsEv);

SELECT
    @RowsBefore                                                        AS MainRowsBefore,
    @Groups                                                            AS GroupsKeptRows,
    @ToDelete                                                          AS RowsToDelete,
    @RowsBefore                                                        AS HistoryRowsToInsertFromRows,
    @EventsMapped                                                      AS HistoryRowsToInsertFromEvents,
    @EventsTotal - @EventsMapped                                       AS HtsEventsNotMapped,
    (SELECT COUNT(*) FROM #Final WHERE CreatorBackfilled = 1)          AS CreatorsBackfilled,
    (SELECT COUNT(*) FROM #Final WHERE FlagsFromEvent = 1)             AS RowsFlagsFromEvents,
    (SELECT COUNT(*) FROM #Final WHERE NeedsInspection = 1)            AS RowsNeedsInspection,
    (SELECT COUNT(*) FROM #Final WHERE NeedsAVL <> OldNeedsAVL)        AS RowsNeedsAvlChanged,
    @NullRevisionBefore                                                AS NullRevisionBefore;

PRINT N'Phase A done (read-only). DryRun = ' + CAST(@DryRun AS NVARCHAR(1));

IF @DryRun = 1
BEGIN
    PRINT N'DryRun=1: no changes were made to real tables.';
    RETURN;
END

/* ---------------------------------------------------------------------------
   B) اجرای واقعی
   --------------------------------------------------------------------------- */
IF OBJECT_ID(N'Sale.ProductionOrderItemBom_Bak_20261014') IS NULL
BEGIN
    SELECT * INTO Sale.ProductionOrderItemBom_Bak_20261014 FROM Sale.ProductionOrderItemBom;
    PRINT N'Backup table created: Sale.ProductionOrderItemBom_Bak_20261014 (' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' rows)';
END
ELSE
    PRINT N'Backup table already exists; reusing it.';

IF (SELECT COUNT(*) FROM Sale.ProductionOrderItemBom_Bak_20261014) <> @RowsBefore
    THROW 51010, N'Backup table row count differs from main table.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    /* 1) تاریخچه از ردیف‌های فعلی */
    INSERT INTO Sale.ProductionOrderItemBomHistory
    (
        ProductionOrderItemId, PartId, Revision, EventType,
        Amount, Description, SaleUnitDetails, NeedsAVL, NeedsInspection,
        ProductionStep, NumberSupplied, Status, IsDeleted,
        CreatedById, ModifiedById, CreatedByName, ModifiedByName,
        ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive
    )
    SELECT
        f.ProductionOrderItemId, f.PartId, f.NewRevision, CASE WHEN f.NewRevision = 0 THEN 0 ELSE 1 END,
        f.Amount, f.Description, f.SaleUnitDetails, f.NeedsAVL, f.NeedsInspection,
        f.ProductionStep, f.NumberSupplied, f.Status, 0,
        f.CreatedById, f.ModifiedById, f.CreatedByName, f.ModifiedByName,
        f.ModifiedDateMiladiDateTime, f.ModifiedDateShamsiDateTime,
        f.CreatedOnMiladiDateTime, f.CreatedOnShamsiDateTime, 1
    FROM #Final f
    ORDER BY f.ProductionOrderItemId, f.PartId, f.NewRevision;
    DECLARE @HistRows INT = @@ROWCOUNT;
    PRINT N'History rows inserted from BOM rows: ' + CAST(@HistRows AS NVARCHAR(20));

    /* 2) رخدادهای ملاحظات HTS */
    INSERT INTO Sale.ProductionOrderItemBomHistory
    (
        ProductionOrderItemId, PartId, Revision, EventType,
        Amount, Description, SaleUnitDetails, NeedsAVL, NeedsInspection,
        ProductionStep, NumberSupplied, Status, IsDeleted,
        CreatedById, ModifiedById, CreatedByName, ModifiedByName,
        ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive
    )
    SELECT
        e.ProductionOrderItemId, e.PartId, e.NewRevision, 2,
        f.Amount, f.Description, e.SalesUnitComment, ISNULL(e.HasNeedToAvl, 0), ISNULL(e.HasNeedToBuyOrBuild, 0),
        f.ProductionStep, f.NumberSupplied, f.Status, 0,
        eu.NewUserId, eu.NewUserId, COALESCE(eu.NewName, eu.HtsFullName), COALESCE(eu.NewName, eu.HtsFullName),
        e.CreatedDate, e.CreatedDateInText,
        e.CreatedDate, e.CreatedDateInText, 1
    FROM #EvRows e
    INNER JOIN #Final f ON f.Id = e.RankedId
    LEFT JOIN #HtsUser eu ON eu.HtsUserId = e.CreatedUserId
    ORDER BY e.ProductionOrderItemId, e.PartId, e.CreatedDate, e.ChangeId;
    DECLARE @EvRowsInserted INT = @@ROWCOUNT;
    PRINT N'History rows inserted from HTS considerations: ' + CAST(@EvRowsInserted AS NVARCHAR(20));

    /* 3) به‌روزرسانی ردیف نگه‌داشته‌شده */
    UPDATE m
    SET m.Revision = f.NewRevision,
        m.IsLatest = 1,
        m.SaleUnitDetails = f.SaleUnitDetails,
        m.NeedsAVL = f.NeedsAVL,
        m.NeedsInspection = f.NeedsInspection,
        m.CreatedById = f.CreatedById,
        m.CreatedByName = f.CreatedByName,
        m.CreatedOnMiladiDateTime = f.CreatedOnMiladiDateTime,
        m.CreatedOnShamsiDateTime = f.CreatedOnShamsiDateTime,
        m.ModifiedById = f.ModifiedById,
        m.ModifiedByName = f.ModifiedByName,
        m.ModifiedDateMiladiDateTime = f.ModifiedDateMiladiDateTime,
        m.ModifiedDateShamsiDateTime = f.ModifiedDateShamsiDateTime
    FROM Sale.ProductionOrderItemBom m
    INNER JOIN #Final f ON f.Id = m.Id
    WHERE f.IsKeep = 1;
    PRINT N'Main rows updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    /* 4) حذف ردیف‌های اضافی */
    IF OBJECT_ID(N'tempdb..#ToDelete') IS NOT NULL DROP TABLE #ToDelete;
    SELECT Id INTO #ToDelete FROM #Final WHERE IsKeep = 0;
    CREATE UNIQUE CLUSTERED INDEX IX_ToDelete ON #ToDelete (Id);

    DECLARE @Del INT = 1, @TotalDel INT = 0;
    WHILE @Del > 0
    BEGIN
        DELETE TOP (20000) m
        FROM Sale.ProductionOrderItemBom m
        INNER JOIN #ToDelete d ON d.Id = m.Id;
        SET @Del = @@ROWCOUNT;
        SET @TotalDel += @Del;
    END
    PRINT N'Main rows deleted: ' + CAST(@TotalDel AS NVARCHAR(20));

    /* 5) گیت‌های تأیید */
    DECLARE @MainAfter INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItemBom);
    DECLARE @DupGroups INT = (SELECT COUNT(*) FROM (SELECT ProductionOrderItemId, PartId FROM Sale.ProductionOrderItemBom GROUP BY ProductionOrderItemId, PartId HAVING COUNT(*) > 1) x);
    DECLARE @NoHistory INT = (
        SELECT COUNT(*) FROM Sale.ProductionOrderItemBom m
        WHERE NOT EXISTS (SELECT 1 FROM Sale.ProductionOrderItemBomHistory h WHERE h.ProductionOrderItemId = m.ProductionOrderItemId AND h.PartId = m.PartId AND h.IsDeleted = 0));
    DECLARE @NullRevAfter INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItemBom WHERE Revision IS NULL);
    DECLARE @NotLatest INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItemBom WHERE IsLatest = 0);
    DECLARE @HistCreateUpdate INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItemBomHistory WHERE EventType IN (0, 1));
    DECLARE @HistConsider INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItemBomHistory WHERE EventType = 2);
    DECLARE @RevMismatch INT = (
        SELECT COUNT(*) FROM Sale.ProductionOrderItemBom m
        WHERE m.Revision <> (SELECT MAX(h.Revision) FROM Sale.ProductionOrderItemBomHistory h WHERE h.ProductionOrderItemId = m.ProductionOrderItemId AND h.PartId = m.PartId AND h.EventType IN (0, 1)));

    SELECT @MainAfter AS MainAfter, @Groups AS ExpectedGroups, @DupGroups AS DupGroups, @NoHistory AS MainRowsWithoutHistory,
           @NullRevAfter AS NullRevisionAfter, @NotLatest AS NotLatestRows,
           @HistCreateUpdate AS HistoryCreateUpdate, @RowsBefore AS ExpectedHistoryCreateUpdate,
           @HistConsider AS HistoryConsiderations, @EventsMapped AS ExpectedConsiderations, @RevMismatch AS RevisionMismatch;

    IF @MainAfter <> @Groups THROW 51021, N'Gate failed: main row count differs from number of groups.', 1;
    IF @DupGroups <> 0 THROW 51022, N'Gate failed: duplicate (item, part) groups remain.', 1;
    IF @NoHistory <> 0 THROW 51023, N'Gate failed: main rows without history exist.', 1;
    IF @NullRevAfter <> 0 THROW 51024, N'Gate failed: NULL revision remains.', 1;
    IF @NotLatest <> 0 THROW 51025, N'Gate failed: IsLatest = 0 rows remain.', 1;
    IF @HistCreateUpdate <> @RowsBefore THROW 51026, N'Gate failed: history create/update rows differ from original BOM rows.', 1;
    IF @HistConsider <> @EventsMapped THROW 51027, N'Gate failed: consideration history rows differ from mapped events.', 1;
    IF @RevMismatch <> 0 THROW 51028, N'Gate failed: main revision differs from history max revision.', 1;

    COMMIT TRANSACTION;
    PRINT N'=== DONE Migrate_ProductionOrderItemBom_ToHistory (committed) ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrNum INT = ERROR_NUMBER();
    PRINT N'ROLLED BACK. Error ' + CAST(@ErrNum AS NVARCHAR(20)) + N': ' + @ErrMsg;
    THROW;
END CATCH;
