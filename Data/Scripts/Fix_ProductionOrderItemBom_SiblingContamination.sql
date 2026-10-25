/*
Fix_ProductionOrderItemBom_SiblingContamination.sql
================================================================================
Incident 1405/07/28 (2026-10-20): Sale.ProductionOrderItemBom of some items got rows copied from
SIBLING HTS items (same ProductionOrder + same Part, different HTS item id) by the bulk load that ran
at 1405/07/03 20:48:42..45 (CreatedByName IS NULL). Example: item 17124 (HTS 85886) received BOM rows
of item 17149 (HTS 85885).

HTS is per item (Vw_Pln_ProductionOrderItemBoms joins bom.ProductionOrderItemId = poi.Id), so the
correct BOM of an App item is the BOM of its OWN HTS item
(HTS id = -RahkaranHistoryId when negative, otherwise Pln_ProductionOrderItem.RahkaranId).

What this script does (only rows of that bulk-load window, CreatedByName IS NULL):

  A) FOREIGN rows  - part is NOT in the item's own HTS BOM (any revision) and IS in a sibling's BOM,
                     row never edited, no consideration, no history event outside the window.
                     -> same semantics as ProductionOrderItemBomService.DeleteLineAsync:
                        history rows of (item, part) get IsDeleted = 1, main row is deleted.

  B) CONTAMINATED rows - part IS in the item's own HTS BOM, but the window added a fake
                     "Update" history event (sibling revision/amount). The last real history event
                     matches the HTS latest row (amount + comment).
                     -> main row is restored from the last real event (Revision, Amount, texts,
                        flags, audit) and the fake events get IsDeleted = 1.

Nothing is inserted. Rows that do not satisfy every condition are left untouched.
Backup of every touched main/history row:
  Sale.ProductionOrderItemBom_Bak_20261020, Sale.ProductionOrderItemBomHistory_Bak_20261020

Usage: run with @DryRun = 1 first (prints counts, changes nothing), then @DryRun = 0.
Requires linked server [TMS]. Encoding: UTF-8 with BOM.
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = 1;
DECLARE @ExpectedForeign INT = NULL;   -- set after dry run to assert
DECLARE @ExpectedFix INT = NULL;       -- set after dry run to assert

DECLARE @Tag NVARCHAR(80) = N'fix-bom-sibling-20261020';
DECLARE @W NVARCHAR(30) = N'1405/07/03 20:48:4%';
DECLARE @WStart NVARCHAR(30) = N'1405/07/03 20:48';
DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), N'/') + N' ' + CONVERT(VARCHAR(8), @Now, 108);

IF OBJECT_ID(N'tempdb..##hb') IS NOT NULL DROP TABLE ##hb;
IF OBJECT_ID(N'tempdb..##hbLatest') IS NOT NULL DROP TABLE ##hbLatest;
IF OBJECT_ID(N'tempdb..##winKeys') IS NOT NULL DROP TABLE ##winKeys;
IF OBJECT_ID(N'tempdb..##sibOwn') IS NOT NULL DROP TABLE ##sibOwn;
IF OBJECT_ID(N'tempdb..##sib') IS NOT NULL DROP TABLE ##sib;
IF OBJECT_ID(N'tempdb..#c') IS NOT NULL DROP TABLE #c;
IF OBJECT_ID(N'tempdb..#foreign') IS NOT NULL DROP TABLE #foreign;
IF OBJECT_ID(N'tempdb..#fix') IS NOT NULL DROP TABLE #fix;
IF OBJECT_ID(N'tempdb..#bogusEvents') IS NOT NULL DROP TABLE #bogusEvents;

-- Keys of the App items touched by the bulk-load window
SELECT DISTINCT i.RahkaranHistoryId AS AppKey
INTO ##winKeys
FROM Sale.ProductionOrderItemBom b
INNER JOIN Sale.ProductionOrderItem i ON i.Id = b.ProductionOrderItemId
WHERE b.CreatedOnShamsiDateTime LIKE @W AND b.CreatedByName IS NULL;

DECLARE @idList NVARCHAR(MAX) = ISNULL((SELECT STRING_AGG(CAST(-AppKey AS NVARCHAR(20)), N',') FROM ##winKeys WHERE AppKey < 0), N'0');
DECLARE @rkList NVARCHAR(MAX) = ISNULL((SELECT STRING_AGG(CAST(AppKey AS NVARCHAR(20)), N',') FROM ##winKeys WHERE AppKey > 0), N'0');
DECLARE @sql NVARCHAR(MAX);

-- HTS items of the window (App key: RahkaranId, or -Id when RahkaranId is null)
SET @sql = N'SELECT s.Id AS HtsItemId, s.ProductionOrderId, s.PartId,
       CASE WHEN s.RahkaranId IS NOT NULL THEN CAST(s.RahkaranId AS BIGINT) ELSE -CAST(s.Id AS BIGINT) END AS AppKey
INTO ##sibOwn
FROM [TMS].[TotalSystem].[dbo].[Pln_ProductionOrderItem] s
WHERE s.Id IN (' + @idList + N') OR s.RahkaranId IN (' + @rkList + N')';
EXEC (@sql);

DECLARE @poList NVARCHAR(MAX) = ISNULL((SELECT STRING_AGG(CAST(ProductionOrderId AS NVARCHAR(20)), N',') FROM (SELECT DISTINCT ProductionOrderId FROM ##sibOwn) x), N'0');

-- all HTS items of those production orders (siblings = same order + same part)
SET @sql = N'SELECT s.Id AS HtsItemId, s.ProductionOrderId, s.PartId,
       CASE WHEN s.RahkaranId IS NOT NULL THEN CAST(s.RahkaranId AS BIGINT) ELSE -CAST(s.Id AS BIGINT) END AS AppKey
INTO ##sib
FROM [TMS].[TotalSystem].[dbo].[Pln_ProductionOrderItem] s
WHERE s.ProductionOrderId IN (' + @poList + N')';
EXEC (@sql);
CREATE CLUSTERED INDEX ix_sib ON ##sib (AppKey);

DECLARE @bomIds NVARCHAR(MAX) = ISNULL((SELECT STRING_AGG(CAST(HtsItemId AS NVARCHAR(20)), N',')
    FROM (SELECT DISTINCT s.HtsItemId FROM ##sib s
          WHERE EXISTS (SELECT 1 FROM ##sibOwn o WHERE o.ProductionOrderId = s.ProductionOrderId AND o.PartId = s.PartId)) x), N'0');

-- HTS BOM (all revisions) of window items and their siblings, with part codes
SET @sql = N'SELECT hb.ProductionOrderItemId AS HtsItemId, hb.PartId AS HtsPartId, hp.Part_Code AS PartCode,
       hb.Revision, hb.IsLatest, hb.Amount, ISNULL(hb.Comment, N'''') AS Comment
INTO ##hb
FROM [TMS].[TotalSystem].[dbo].[Pln_ProductionOrderItemBom] hb
INNER JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] hp ON hp.Part_ID = hb.PartId
WHERE hb.ProductionOrderItemId IN (' + @bomIds + N')';
EXEC (@sql);
CREATE CLUSTERED INDEX ix_hb ON ##hb (HtsItemId, PartCode);

-- HTS latest row per (item, part): only usable when exactly one latest row exists
SELECT HtsItemId, PartCode, MAX(Amount) AS Amount, MAX(Comment) AS Comment, COUNT(*) AS Cnt
INTO ##hbLatest
FROM ##hb WHERE IsLatest = 1
GROUP BY HtsItemId, PartCode;
CREATE CLUSTERED INDEX ix_hbl ON ##hbLatest (HtsItemId, PartCode);
-- Candidate rows: main rows created by the bulk-load window
SELECT b.Id AS BomId, b.ProductionOrderItemId AS ItemId, b.PartId, p.Code, p.HtsId AS PartHtsId,
       s.HtsItemId, s.ProductionOrderId AS HtsPO, s.PartId AS HtsItemPart,
       b.ModifiedByName, b.CreatedOnShamsiDateTime AS cdt, b.ModifiedDateShamsiDateTime AS mdt,
       b.NeedsAVL, b.NeedsInspection, b.SaleUnitDetails, b.NumberSupplied, b.Status, b.ProductionStep,
       CAST(0 AS INT) AS InOwn, CAST(0 AS INT) AS InSib, CAST(0 AS INT) AS OtherEvents, CAST(0 AS INT) AS ConsiderEvents
INTO #c
FROM Sale.ProductionOrderItemBom b
INNER JOIN Sale.ProductionOrderItem i ON i.Id = b.ProductionOrderItemId
INNER JOIN Inv.Part p ON p.Id = b.PartId
INNER JOIN ##sib s ON s.AppKey = i.RahkaranHistoryId
WHERE b.CreatedOnShamsiDateTime LIKE @W
  AND b.CreatedByName IS NULL;

UPDATE c SET InOwn = 1
FROM #c c
WHERE EXISTS (SELECT 1 FROM ##hb x WHERE x.HtsItemId = c.HtsItemId
              AND (x.PartCode = c.Code OR (c.PartHtsId <> 0 AND x.HtsPartId = c.PartHtsId)));

UPDATE c SET InSib = 1
FROM #c c
WHERE EXISTS (SELECT 1 FROM ##sib s2 INNER JOIN ##hb x ON x.HtsItemId = s2.HtsItemId
              WHERE s2.ProductionOrderId = c.HtsPO AND s2.PartId = c.HtsItemPart AND s2.HtsItemId <> c.HtsItemId
                AND (x.PartCode = c.Code OR (c.PartHtsId <> 0 AND x.HtsPartId = c.PartHtsId)));

UPDATE c SET
    OtherEvents = (SELECT COUNT(*) FROM Sale.ProductionOrderItemBomHistory h
                   WHERE h.ProductionOrderItemId = c.ItemId AND h.PartId = c.PartId AND h.IsDeleted = 0
                     AND NOT (h.CreatedOnShamsiDateTime LIKE @W AND h.CreatedByName IS NULL)),
    ConsiderEvents = (SELECT COUNT(*) FROM Sale.ProductionOrderItemBomHistory h
                      WHERE h.ProductionOrderItemId = c.ItemId AND h.PartId = c.PartId AND h.EventType = 2)
FROM #c c;

-- A) foreign rows
SELECT BomId, ItemId, PartId
INTO #foreign
FROM #c
WHERE InOwn = 0 AND InSib = 1 AND OtherEvents = 0 AND ConsiderEvents = 0
  AND ModifiedByName IS NULL AND mdt = cdt
  AND NeedsAVL = 0 AND NeedsInspection = 0 AND SaleUnitDetails IS NULL AND NumberSupplied IS NULL
  AND Status = 0 AND ProductionStep = 0;

-- B) contaminated rows (own part, fake window events, last real event == HTS latest)
SELECT c.BomId, c.ItemId, c.PartId,
       v.Id AS ValidEventId, v.Revision AS vRevision, v.Amount AS vAmount, v.Description AS vDescription,
       v.SaleUnitDetails AS vSaleUnitDetails, v.NeedsAVL AS vNeedsAVL, v.NeedsInspection AS vNeedsInspection,
       v.ProductionStep AS vProductionStep, v.NumberSupplied AS vNumberSupplied, v.Status AS vStatus,
       v.CreatedById AS vById, v.CreatedByName AS vByName,
       v.CreatedOnMiladiDateTime AS vOnMiladi, v.CreatedOnShamsiDateTime AS vOnShamsi,
       f.CreatedById AS fById, f.CreatedByName AS fByName,
       f.CreatedOnMiladiDateTime AS fOnMiladi, f.CreatedOnShamsiDateTime AS fOnShamsi
INTO #fix
FROM #c c
INNER JOIN ##hbLatest hl ON hl.HtsItemId = c.HtsItemId AND hl.PartCode = c.Code AND hl.Cnt = 1
OUTER APPLY (SELECT TOP (1) z.* FROM Sale.ProductionOrderItemBomHistory z
             WHERE z.ProductionOrderItemId = c.ItemId AND z.PartId = c.PartId AND z.IsDeleted = 0
               AND NOT (z.CreatedOnShamsiDateTime LIKE @W AND z.CreatedByName IS NULL)
             ORDER BY z.Id DESC) v
OUTER APPLY (SELECT TOP (1) z.* FROM Sale.ProductionOrderItemBomHistory z
             WHERE z.ProductionOrderItemId = c.ItemId AND z.PartId = c.PartId AND z.IsDeleted = 0 AND z.EventType = 0
               AND NOT (z.CreatedOnShamsiDateTime LIKE @W AND z.CreatedByName IS NULL)
             ORDER BY z.Id ASC) f
WHERE c.InOwn = 1
  AND v.Id IS NOT NULL AND f.Id IS NOT NULL
  AND v.Amount = hl.Amount AND ISNULL(v.Description, N'') = hl.Comment
  AND EXISTS (SELECT 1 FROM Sale.ProductionOrderItemBomHistory h
              WHERE h.ProductionOrderItemId = c.ItemId AND h.PartId = c.PartId AND h.IsDeleted = 0
                AND h.EventType <> 0 AND h.CreatedOnShamsiDateTime LIKE @W AND h.CreatedByName IS NULL)
  -- no real (user) activity at or after the window
  AND NOT EXISTS (SELECT 1 FROM Sale.ProductionOrderItemBomHistory h
                  WHERE h.ProductionOrderItemId = c.ItemId AND h.PartId = c.PartId AND h.IsDeleted = 0
                    AND NOT (h.CreatedOnShamsiDateTime LIKE @W AND h.CreatedByName IS NULL)
                    AND h.CreatedOnShamsiDateTime >= @WStart);

SELECT h.Id AS EventId, h.ProductionOrderItemId AS ItemId, h.PartId
INTO #bogusEvents
FROM Sale.ProductionOrderItemBomHistory h
INNER JOIN #fix f ON f.ItemId = h.ProductionOrderItemId AND f.PartId = h.PartId
WHERE h.IsDeleted = 0 AND h.EventType <> 0 AND h.CreatedOnShamsiDateTime LIKE @W AND h.CreatedByName IS NULL;

DECLARE @nForeign INT = (SELECT COUNT(*) FROM #foreign);
DECLARE @nFix INT = (SELECT COUNT(*) FROM #fix);
DECLARE @nBogus INT = (SELECT COUNT(*) FROM #bogusEvents);
DECLARE @nForeignItems INT = (SELECT COUNT(DISTINCT ItemId) FROM #foreign);
DECLARE @nFixItems INT = (SELECT COUNT(DISTINCT ItemId) FROM #fix);

PRINT N'candidates(window main rows)=' + CAST((SELECT COUNT(*) FROM #c) AS NVARCHAR(20));
PRINT N'A foreign rows=' + CAST(@nForeign AS NVARCHAR(20)) + N' items=' + CAST(@nForeignItems AS NVARCHAR(20));
PRINT N'B contaminated rows=' + CAST(@nFix AS NVARCHAR(20)) + N' items=' + CAST(@nFixItems AS NVARCHAR(20)) + N' fakeEvents=' + CAST(@nBogus AS NVARCHAR(20));

IF @DryRun = 1
BEGIN
    PRINT N'DRY RUN - nothing changed';
    DROP TABLE ##hb; DROP TABLE ##hbLatest; DROP TABLE ##winKeys; DROP TABLE ##sibOwn; DROP TABLE ##sib;
    SELECT @nForeign AS ForeignRows, @nForeignItems AS ForeignItems, @nFix AS ContaminatedRows, @nFixItems AS ContaminatedItems, @nBogus AS FakeEvents;
    RETURN;
END;

IF @ExpectedForeign IS NOT NULL AND @nForeign <> @ExpectedForeign
    THROW 51901, N'Foreign row count differs from expected; aborting.', 1;
IF @ExpectedFix IS NOT NULL AND @nFix <> @ExpectedFix
    THROW 51902, N'Contaminated row count differs from expected; aborting.', 1;

BEGIN TRANSACTION;

IF OBJECT_ID(N'Sale.ProductionOrderItemBom_Bak_20261020') IS NOT NULL
    OR OBJECT_ID(N'Sale.ProductionOrderItemBomHistory_Bak_20261020') IS NOT NULL
    THROW 51903, N'Backup tables already exist; refusing to run twice.', 1;

SELECT b.* INTO Sale.ProductionOrderItemBom_Bak_20261020
FROM Sale.ProductionOrderItemBom b
WHERE b.Id IN (SELECT BomId FROM #foreign UNION SELECT BomId FROM #fix);

SELECT h.* INTO Sale.ProductionOrderItemBomHistory_Bak_20261020
FROM Sale.ProductionOrderItemBomHistory h
WHERE EXISTS (SELECT 1 FROM #foreign f WHERE f.ItemId = h.ProductionOrderItemId AND f.PartId = h.PartId)
   OR EXISTS (SELECT 1 FROM #fix f WHERE f.ItemId = h.ProductionOrderItemId AND f.PartId = h.PartId);

-- A) foreign rows: history IsDeleted + delete main row
UPDATE h SET IsDeleted = 1,
    ModifiedById = 1, ModifiedByName = @Tag,
    ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
FROM Sale.ProductionOrderItemBomHistory h
INNER JOIN #foreign f ON f.ItemId = h.ProductionOrderItemId AND f.PartId = h.PartId
WHERE h.IsDeleted = 0;

DELETE b
FROM Sale.ProductionOrderItemBom b
INNER JOIN #foreign f ON f.BomId = b.Id;
IF @@ROWCOUNT <> @nForeign THROW 51904, N'Deleted main row count mismatch.', 1;

-- B) contaminated rows: restore main row from last real event, hide fake events
UPDATE b SET
    b.Revision = f.vRevision,
    b.Amount = f.vAmount,
    b.Description = f.vDescription,
    b.SaleUnitDetails = f.vSaleUnitDetails,
    b.NeedsAVL = f.vNeedsAVL,
    b.NeedsInspection = f.vNeedsInspection,
    b.ProductionStep = f.vProductionStep,
    b.NumberSupplied = f.vNumberSupplied,
    b.Status = f.vStatus,
    b.CreatedById = f.fById, b.CreatedByName = f.fByName,
    b.CreatedOnMiladiDateTime = f.fOnMiladi, b.CreatedOnShamsiDateTime = f.fOnShamsi,
    b.ModifiedById = f.vById, b.ModifiedByName = f.vByName,
    b.ModifiedDateMiladiDateTime = f.vOnMiladi, b.ModifiedDateShamsiDateTime = f.vOnShamsi
FROM Sale.ProductionOrderItemBom b
INNER JOIN #fix f ON f.BomId = b.Id;
IF @@ROWCOUNT <> @nFix THROW 51905, N'Restored main row count mismatch.', 1;

UPDATE h SET IsDeleted = 1,
    ModifiedById = 1, ModifiedByName = @Tag,
    ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
FROM Sale.ProductionOrderItemBomHistory h
INNER JOIN #bogusEvents e ON e.EventId = h.Id;
IF @@ROWCOUNT <> @nBogus THROW 51906, N'Hidden fake event count mismatch.', 1;

COMMIT TRANSACTION;

PRINT N'APPLIED: deleted foreign=' + CAST(@nForeign AS NVARCHAR(20))
    + N' restored=' + CAST(@nFix AS NVARCHAR(20)) + N' fakeEventsHidden=' + CAST(@nBogus AS NVARCHAR(20));
SELECT @nForeign AS DeletedForeignRows, @nFix AS RestoredRows, @nBogus AS HiddenFakeEvents;

DROP TABLE ##hb; DROP TABLE ##hbLatest; DROP TABLE ##winKeys; DROP TABLE ##sibOwn; DROP TABLE ##sib;
