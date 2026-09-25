/*
Sync_ProductionOrderItemBom_FromTotalSystem.sql
================================================================================
STAGE 1 cutover: Sale.ProductionOrderItemBom from TotalSystem (all versions).
Match key: App.RahkaranHistoryId = HTS.Pln_ProductionOrderItem.RahkaranId
Part key:  Inv.Part.HtsId = HTS.Bom.PartId (fallback Part.Code)
Business upsert: (ProductionOrderItemId, Revision, PartId)
Does NOT touch delays, comments, packages, or other child tables.
Does NOT delete existing App BOM rows (including routine-auto rows).

Encoding: UTF-8 with BOM
Apply:
  sqlcmd -S PortalSRV\PORTAL -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Sync_ProductionOrderItemBom_FromTotalSystem.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = dbo.ToShamsiDate(CAST(@Now AS DATE), N'/') + N' ' + CONVERT(VARCHAR(8), @Now, 108);
DECLARE @Seed NVARCHAR(80) = N'sync-po-item-bom-hts';
DECLARE @BeforeBom INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItemBom);
DECLARE @BeforeDelay INT = (SELECT COUNT(*) FROM Pln.ProductionOrderDelay);
DECLARE @Inserted INT = 0;
DECLARE @Updated INT = 0;
DECLARE @SkippedNoItem INT = 0;
DECLARE @SkippedNoPart INT = 0;
DECLARE @SkippedNoRevision INT = 0;

PRINT N'=== BEFORE ===';
PRINT N'BOM=' + CAST(@BeforeBom AS NVARCHAR(20)) + N' Delay=' + CAST(@BeforeDelay AS NVARCHAR(20));

IF OBJECT_ID(N'tempdb..#BomSrc') IS NOT NULL DROP TABLE #BomSrc;
SELECT
    CAST(hi.RahkaranId AS BIGINT) AS RahkaranHistoryId,
    CAST(b.Revision AS INT) AS Revision,
    CAST(b.PartId AS BIGINT) AS HtsPartId,
    CAST(ISNULL(b.Amount, 0) AS DECIMAL(18,4)) AS Amount,
    CAST(ISNULL(b.IsLatest, 0) AS BIT) AS IsLatest,
    b.Comment AS Description,
    b.SalesUnitComment AS SaleUnitDetails,
    CAST(ISNULL(b.HasNeedToAvl, 0) AS BIT) AS NeedsAVL,
    hp.Part_Code AS HtsPartCode
INTO #BomSrc
FROM [TMS].[TotalSystem].[dbo].[Pln_ProductionOrderItemBom] b
INNER JOIN [TMS].[TotalSystem].[dbo].[Pln_ProductionOrderItem] hi
    ON hi.Id = b.ProductionOrderItemId
   AND hi.RahkaranId IS NOT NULL
LEFT JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] hp ON hp.Part_ID = b.PartId;

CREATE CLUSTERED INDEX IX_BomSrc ON #BomSrc (RahkaranHistoryId, Revision, HtsPartId);

SELECT @SkippedNoRevision = COUNT(*) FROM #BomSrc WHERE Revision IS NULL;

IF OBJECT_ID(N'tempdb..#BomMapped') IS NOT NULL DROP TABLE #BomMapped;
SELECT
    ai.Id AS ProductionOrderItemId,
    s.Revision,
    COALESCE(pHts.Id, pCode.Id) AS PartId,
    s.Amount,
    s.IsLatest,
    s.Description,
    s.SaleUnitDetails,
    s.NeedsAVL,
    s.RahkaranHistoryId,
    s.HtsPartId
INTO #BomMapped
FROM #BomSrc s
INNER JOIN Sale.ProductionOrderItem ai ON ai.RahkaranHistoryId = s.RahkaranHistoryId
LEFT JOIN Inv.Part pHts ON pHts.HtsId = s.HtsPartId AND pHts.HtsId <> 0
OUTER APPLY (
    SELECT TOP (1) p.Id
    FROM Inv.Part p
    WHERE pHts.Id IS NULL
      AND s.HtsPartCode IS NOT NULL
      AND p.Code = s.HtsPartCode
    ORDER BY CASE WHEN p.IsActive = 1 THEN 0 ELSE 1 END, p.Id
) pCode
WHERE s.Revision IS NOT NULL;

SELECT @SkippedNoItem = COUNT(*)
FROM #BomSrc s
WHERE s.Revision IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM Sale.ProductionOrderItem ai WHERE ai.RahkaranHistoryId = s.RahkaranHistoryId);

SELECT @SkippedNoPart = COUNT(*)
FROM #BomSrc s
INNER JOIN Sale.ProductionOrderItem ai ON ai.RahkaranHistoryId = s.RahkaranHistoryId
WHERE s.Revision IS NOT NULL
  AND NOT EXISTS (
        SELECT 1 FROM Inv.Part pHts WHERE pHts.HtsId = s.HtsPartId AND pHts.HtsId <> 0
      )
  AND NOT EXISTS (
        SELECT 1 FROM Inv.Part p WHERE s.HtsPartCode IS NOT NULL AND p.Code = s.HtsPartCode
      );

-- Collapse duplicate source keys (keep max Amount / any IsLatest true / longest comments)
IF OBJECT_ID(N'tempdb..#BomDedup') IS NOT NULL DROP TABLE #BomDedup;
SELECT
    ProductionOrderItemId,
    Revision,
    PartId,
    MAX(Amount) AS Amount,
    CAST(MAX(CAST(IsLatest AS INT)) AS BIT) AS IsLatest,
    MAX(Description) AS Description,
    MAX(SaleUnitDetails) AS SaleUnitDetails,
    CAST(MAX(CAST(NeedsAVL AS INT)) AS BIT) AS NeedsAVL
INTO #BomDedup
FROM #BomMapped
WHERE PartId IS NOT NULL
GROUP BY ProductionOrderItemId, Revision, PartId;

CREATE UNIQUE CLUSTERED INDEX IX_BomDedup ON #BomDedup (ProductionOrderItemId, Revision, PartId);

BEGIN TRAN;

UPDATE t
SET
    t.Amount = d.Amount,
    t.IsLatest = d.IsLatest,
    t.Description = d.Description,
    t.SaleUnitDetails = d.SaleUnitDetails,
    t.NeedsAVL = d.NeedsAVL,
    t.ModifiedById = 1,
    t.ModifiedByName = @Seed,
    t.ModifiedDateMiladiDateTime = @Now,
    t.ModifiedDateShamsiDateTime = @NowShamsi
FROM Sale.ProductionOrderItemBom t
INNER JOIN #BomDedup d
    ON d.ProductionOrderItemId = t.ProductionOrderItemId
   AND d.Revision = t.Revision
   AND d.PartId = t.PartId
WHERE t.Amount <> d.Amount
   OR t.IsLatest <> d.IsLatest
   OR ISNULL(t.Description, N'') <> ISNULL(d.Description, N'')
   OR ISNULL(t.SaleUnitDetails, N'') <> ISNULL(d.SaleUnitDetails, N'')
   OR t.NeedsAVL <> d.NeedsAVL;

SET @Updated = @@ROWCOUNT;

INSERT INTO Sale.ProductionOrderItemBom
(
    ProductionOrderItemId,
    Revision,
    PartId,
    Amount,
    IsLatest,
    Description,
    SaleUnitDetails,
    NeedsAVL,
    ProductionStep,
    Status,
    NumberSupplied,
    CreatedById,
    CreatedByName,
    CreatedOnMiladiDateTime,
    CreatedOnShamsiDateTime,
    ModifiedById,
    ModifiedByName,
    ModifiedDateMiladiDateTime,
    ModifiedDateShamsiDateTime,
    IsActive
)
SELECT
    d.ProductionOrderItemId,
    d.Revision,
    d.PartId,
    d.Amount,
    d.IsLatest,
    d.Description,
    d.SaleUnitDetails,
    d.NeedsAVL,
    0, -- ProductionStep default (matches existing job inserts)
    0, -- Status default
    NULL,
    1,
    @Seed,
    @Now,
    @NowShamsi,
    1,
    @Seed,
    @Now,
    @NowShamsi,
    1
FROM #BomDedup d
WHERE NOT EXISTS (
    SELECT 1
    FROM Sale.ProductionOrderItemBom t
    WHERE t.ProductionOrderItemId = d.ProductionOrderItemId
      AND t.Revision = d.Revision
      AND t.PartId = d.PartId
);

SET @Inserted = @@ROWCOUNT;

COMMIT TRAN;

DECLARE @AfterBom INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItemBom);
DECLARE @AfterDelay INT = (SELECT COUNT(*) FROM Pln.ProductionOrderDelay);
DECLARE @LatestBom INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItemBom WHERE IsLatest = 1);
DECLARE @HistBom INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItemBom WHERE IsLatest = 0);

-- Reconciliation vs HTS Rahkaran-linked BOM keys
DECLARE @HtsKeys INT, @AppMatchedKeys INT, @MissingKeys INT, @DupKeys INT;

SELECT @HtsKeys = COUNT(*) FROM #BomDedup;

SELECT @AppMatchedKeys = COUNT(*)
FROM #BomDedup d
INNER JOIN Sale.ProductionOrderItemBom t
    ON t.ProductionOrderItemId = d.ProductionOrderItemId
   AND t.Revision = d.Revision
   AND t.PartId = d.PartId;

SET @MissingKeys = @HtsKeys - @AppMatchedKeys;

SELECT @DupKeys = COUNT(*)
FROM (
    SELECT ProductionOrderItemId, Revision, PartId
    FROM Sale.ProductionOrderItemBom
    GROUP BY ProductionOrderItemId, Revision, PartId
    HAVING COUNT(*) > 1
) x;

PRINT N'=== AFTER ===';
PRINT N'BOM=' + CAST(@AfterBom AS NVARCHAR(20))
    + N' Latest=' + CAST(@LatestBom AS NVARCHAR(20))
    + N' Hist=' + CAST(@HistBom AS NVARCHAR(20))
    + N' Delay=' + CAST(@AfterDelay AS NVARCHAR(20));
PRINT N'Inserted=' + CAST(@Inserted AS NVARCHAR(20))
    + N' Updated=' + CAST(@Updated AS NVARCHAR(20));
PRINT N'SkippedNoItem=' + CAST(@SkippedNoItem AS NVARCHAR(20))
    + N' SkippedNoPart=' + CAST(@SkippedNoPart AS NVARCHAR(20))
    + N' SkippedNoRevision=' + CAST(@SkippedNoRevision AS NVARCHAR(20));
PRINT N'HtsDedupKeys=' + CAST(@HtsKeys AS NVARCHAR(20))
    + N' AppMatched=' + CAST(@AppMatchedKeys AS NVARCHAR(20))
    + N' Missing=' + CAST(@MissingKeys AS NVARCHAR(20))
    + N' DupKeys=' + CAST(@DupKeys AS NVARCHAR(20));

IF @AfterDelay <> @BeforeDelay
    RAISERROR(N'Delay count changed — unexpected.', 16, 1);

IF @DupKeys <> 0
    RAISERROR(N'Duplicate BOM business keys detected.', 16, 1);

IF @MissingKeys <> 0
    PRINT N'WARN: some HTS keys still missing after merge.';

SELECT
    @BeforeBom AS BeforeBom,
    @AfterBom AS AfterBom,
    @LatestBom AS LatestBom,
    @HistBom AS HistBom,
    @BeforeDelay AS BeforeDelay,
    @AfterDelay AS AfterDelay,
    @Inserted AS Inserted,
    @Updated AS Updated,
    @SkippedNoItem AS SkippedNoItem,
    @SkippedNoPart AS SkippedNoPart,
    @SkippedNoRevision AS SkippedNoRevision,
    @HtsKeys AS HtsDedupKeys,
    @AppMatchedKeys AS AppMatchedKeys,
    @MissingKeys AS MissingKeys,
    @DupKeys AS DupKeys;

PRINT N'=== DONE Sync_ProductionOrderItemBom_FromTotalSystem ===';
