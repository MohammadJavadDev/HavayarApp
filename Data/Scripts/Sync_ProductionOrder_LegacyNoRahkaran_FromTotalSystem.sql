/*
Sync_ProductionOrder_LegacyNoRahkaran_FromTotalSystem.sql
================================================================================
STAGE 1b: import HTS ProductionOrder / Item / BOM rows that have no Rahkaran id
(and orphan positive-history items whose parent Number is free).

KEY STRATEGY (no new columns; no HamkaranId overload):
  HEADER: ProductionOrderNumber = HTS.Pln_ProductionOrder.Number, HamkaranId = NULL
          Skip if App already has that ProductionOrderNumber (live Rahkaran collision).
  ITEM (null RahkaranId): RahkaranHistoryId = -HTS.Pln_ProductionOrderItem.Id
          (negative sentinel — never collides with positive Rahkaran History.Id 55..28k)
  ITEM (positive RahkaranId missing in App): RahkaranHistoryId = HTS.RahkaranId as-is
  BOM: (ProductionOrderItemId, Revision, PartId) — same as prior script

Does NOT touch delays / comments / packages / attachments.
Does NOT delete. Does NOT overwrite CheckStatus/ProductionStep/Status/NumberSupplied
on existing App rows (insert-only for header/item; BOM updates only Amount/IsLatest/text).

Encoding: UTF-8 with BOM
Apply:
  sqlcmd -S 172.20.40.42 -d HavayarApp -U sa -P "..." -C -b -I -f 65001 -i "Data\Scripts\Sync_ProductionOrder_LegacyNoRahkaran_FromTotalSystem.sql"
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
DECLARE @Seed NVARCHAR(80) = N'sync-po-legacy-no-rahkaran';

DECLARE @BeforePo INT = (SELECT COUNT(*) FROM Sale.ProductionOrder);
DECLARE @BeforeItem INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItem);
DECLARE @BeforeBom INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItemBom);
DECLARE @BeforeDelay INT = (SELECT COUNT(*) FROM Pln.ProductionOrderDelay);

DECLARE @InsPo INT = 0, @SkipPoCollision INT = 0;
DECLARE @InsItem INT = 0, @SkipItemNoParent INT = 0, @SkipItemNoPart INT = 0, @SkipItemCollision INT = 0;
DECLARE @InsBom INT = 0, @UpdBom INT = 0, @SkipBomNoItem INT = 0, @SkipBomNoPart INT = 0;

PRINT N'=== BEFORE ===';
PRINT N'PO=' + CAST(@BeforePo AS NVARCHAR(20))
    + N' Item=' + CAST(@BeforeItem AS NVARCHAR(20))
    + N' BOM=' + CAST(@BeforeBom AS NVARCHAR(20))
    + N' Delay=' + CAST(@BeforeDelay AS NVARCHAR(20));

BEGIN TRAN;

/* --------------------------------------------------------------------------
   1) HEADERS — null RahkaranId OR missing HamkaranId, Number not already in App
   -------------------------------------------------------------------------- */
IF OBJECT_ID(N'tempdb..#HdrSrc') IS NOT NULL DROP TABLE #HdrSrc;
SELECT
    h.Id AS HtsPoId,
    h.Number,
    CAST(h.Revision AS INT) AS Revision,
    h.IsLatestVersion,
    h.RahkaranId,
    h.LastStatusId,
    h.EndUser,
    h.ProjectStartDate,
    h.MetreDate,
    h.MetreNumber,
    h.AgreedDeliveryDate,
    CAST(ISNULL(h.IsNeedInspectionBeforePacking, 0) AS BIT) AS IsNeedInspectionBeforePacking,
    CAST(ISNULL(h.HasContractor, 0) AS BIT) AS HasContractor,
    CAST(ISNULL(h.CompressorHouseIsReady, 0) AS BIT) AS CompressorHouseIsReady,
    CAST(ISNULL(h.InstallationIsByHy, 0) AS BIT) AS InstallationIsByHy,
    CAST(ISNULL(h.HasFinancialGuarantee, 0) AS BIT) AS HasFinancialGuarantee,
    h.SalesComment,
    h.Comment,
    CAST(ISNULL(h.CentrifugeHasCompressor, 0) AS BIT) AS CentrifugeHasCompressor,
    h.CentrifugeCompressorCount,
    h.CentrifugeElectromotorVoltage,
    CAST(ISNULL(h.HasGa, 0) AS BIT) AS HasGA,
    h.PackingTypeId,
    h.DeliveryTypeId,
    h.ProductTypeId,
    h.CentrifugeSetupTypeId,
    CAST(ISNULL(h.IsNeedProjectManager, 0) AS BIT) AS IsNeedProjectManager,
    CAST(ISNULL(h.IsDisableForTimelyDeliveryReport, 0) AS BIT) AS IsDisableForTimelyDeliveryReport,
    h.DisableForTimelyDeliveryReportComment,
    h.DeliveryLocation,
    h.EdmsProjectId,
    h.CreatedDate,
    h.CreatedDateInText
INTO #HdrSrc
FROM [TMS].[TotalSystem].[dbo].[Pln_ProductionOrder] h
WHERE (
        h.RahkaranId IS NULL
        OR NOT EXISTS (
            SELECT 1 FROM Sale.ProductionOrder a WHERE a.HamkaranId = h.RahkaranId
        )
      )
  AND NOT EXISTS (
        SELECT 1 FROM Sale.ProductionOrder a WHERE a.ProductionOrderNumber = h.Number
      );

SELECT @SkipPoCollision = COUNT(*)
FROM [TMS].[TotalSystem].[dbo].[Pln_ProductionOrder] h
WHERE (
        h.RahkaranId IS NULL
        OR NOT EXISTS (
            SELECT 1 FROM Sale.ProductionOrder a WHERE a.HamkaranId = h.RahkaranId
        )
      )
  AND EXISTS (
        SELECT 1 FROM Sale.ProductionOrder a WHERE a.ProductionOrderNumber = h.Number
      );

INSERT INTO Sale.ProductionOrder
(
    State,
    Number,
    ProductionOrderNumber,
    Revision,
    HamkaranId,
    EndUser,
    ProjectStartDate,
    MetreDate,
    MetreNumber,
    AgreedDeliveryDate,
    IsNeedInspectionBeforePacking,
    HasContractor,
    CompressorHouseIsReady,
    InstallationIsByHy,
    HasFinancialGuarantee,
    SalesComment,
    Comment,
    CentrifugeHasCompressor,
    CentrifugeCompressorCount,
    CentrifugeElectromotorVoltage,
    HasGA,
    PackingType,
    DeliveryType,
    ProductType,
    CentrifugeSetupType,
    IsNeedProjectManager,
    IsDisableForTimelyDeliveryReport,
    DisableForTimelyDeliveryReportComment,
    DeliveryLocation,
    EdmsProject,
    DelayNotCalculated,
    ObsoletedOnMiladiDate,
    ObsoletedOnShamsiDate,
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
    CASE h.LastStatusId
        WHEN 2055 THEN 1
        WHEN 2056 THEN 1
        WHEN 2436 THEN 1
        WHEN 2084 THEN 2
        WHEN 2057 THEN 3
        WHEN 2071 THEN 3
        WHEN 2207 THEN 4
        ELSE 1
    END AS State,
    CAST(h.Number AS NVARCHAR(255)),
    CAST(h.Number AS BIGINT),
    h.Revision,
    NULL, -- never overload HamkaranId for legacy / stale ids
    h.EndUser,
    h.ProjectStartDate,
    h.MetreDate,
    h.MetreNumber,
    h.AgreedDeliveryDate,
    h.IsNeedInspectionBeforePacking,
    h.HasContractor,
    h.CompressorHouseIsReady,
    h.InstallationIsByHy,
    h.HasFinancialGuarantee,
    h.SalesComment,
    h.Comment,
    h.CentrifugeHasCompressor,
    h.CentrifugeCompressorCount,
    h.CentrifugeElectromotorVoltage,
    h.HasGA,
    h.PackingTypeId,
    h.DeliveryTypeId,
    h.ProductTypeId,
    h.CentrifugeSetupTypeId,
    h.IsNeedProjectManager,
    h.IsDisableForTimelyDeliveryReport,
    h.DisableForTimelyDeliveryReportComment,
    h.DeliveryLocation,
    h.EdmsProjectId,
    0,
    CASE WHEN h.LastStatusId = 2207 THEN ISNULL(h.CreatedDate, @Now) ELSE NULL END,
    CASE WHEN h.LastStatusId = 2207 THEN ISNULL(NULLIF(h.CreatedDateInText, N''), @NowShamsi) ELSE NULL END,
    1,
    @Seed,
    ISNULL(h.CreatedDate, @Now),
    ISNULL(NULLIF(h.CreatedDateInText, N''), @NowShamsi),
    1,
    @Seed,
    @Now,
    @NowShamsi,
    1
FROM #HdrSrc h;

SET @InsPo = @@ROWCOUNT;

/* Parent map: HtsPoId -> AppPoId (linked by HamkaranId OR legacy by Number+null Hamkaran) */
IF OBJECT_ID(N'tempdb..#PoMap') IS NOT NULL DROP TABLE #PoMap;
SELECT
    h.Id AS HtsPoId,
    a.Id AS AppPoId,
    h.Number AS HtsNumber,
    h.RahkaranId
INTO #PoMap
FROM [TMS].[TotalSystem].[dbo].[Pln_ProductionOrder] h
INNER JOIN Sale.ProductionOrder a
    ON (
        (h.RahkaranId IS NOT NULL AND a.HamkaranId = h.RahkaranId)
        OR (
            a.ProductionOrderNumber = h.Number
            AND a.HamkaranId IS NULL
        )
      );

CREATE UNIQUE CLUSTERED INDEX IX_PoMap ON #PoMap (HtsPoId);

/* Collision Numbers: live App order — do not attach null-R history to them */
IF OBJECT_ID(N'tempdb..#CollisionNumbers') IS NOT NULL DROP TABLE #CollisionNumbers;
SELECT DISTINCT h.Number
INTO #CollisionNumbers
FROM [TMS].[TotalSystem].[dbo].[Pln_ProductionOrder] h
INNER JOIN Sale.ProductionOrder a ON a.ProductionOrderNumber = h.Number AND a.HamkaranId IS NOT NULL
WHERE h.RahkaranId IS NULL
   OR (
        h.RahkaranId IS NOT NULL
        AND NOT EXISTS (SELECT 1 FROM Sale.ProductionOrder x WHERE x.HamkaranId = h.RahkaranId)
      );

CREATE UNIQUE CLUSTERED INDEX IX_CollisionNumbers ON #CollisionNumbers (Number);

/* --------------------------------------------------------------------------
   2) ITEMS — null RahkaranId (negative key) + missing positive RahkaranHistoryId
   -------------------------------------------------------------------------- */
IF OBJECT_ID(N'tempdb..#ItemSrc') IS NOT NULL DROP TABLE #ItemSrc;
SELECT
    hi.Id AS HtsItemId,
    hi.ProductionOrderId AS HtsPoId,
    CASE
        WHEN hi.RahkaranId IS NOT NULL THEN CAST(hi.RahkaranId AS BIGINT)
        ELSE -CAST(hi.Id AS BIGINT)
    END AS AppRahkaranHistoryId,
    CAST(hi.Revision AS DECIMAL(18,2)) AS Revision,
    CAST(ISNULL(hi.IsLatestVersion, 0) AS BIT) AS IsLatestVersion,
    hi.PartId AS HtsPartId,
    CAST(hi.Mount AS DECIMAL(18,4)) AS Amount,
    hi.PartModel,
    hi.AirendOrCategory,
    hi.Capacity,
    hi.InputPressureBar,
    hi.OutputPressureBar,
    hi.GasType,
    hi.MovingType,
    hi.PurityPercentage,
    hi.BarometricPressure,
    hi.MaximumTemperature,
    hi.RelativeHumidity,
    hi.Scale,
    CAST(ISNULL(hi.HasInspection, 0) AS BIT) AS HasInspection,
    hi.AgreedDeliverDate,
    hi.AgreedDeliverDateInText,
    hi.SalesConsideration,
    hi.BuyStatusId,
    hi.Serial,
    hi.StatusId,
    hi.SerialTypeId,
    hi.PlanningNumber,
    hi.ProductionStepId,
    hi.ProductionStatusId,
    hi.ProductionStartDateInEurope,
    hi.ProductionStartDate,
    hi.ProductionEndDateInEurope,
    hi.ProductionEndDate,
    hi.TestingEndDateInEurope,
    hi.TestingEndDate,
    hi.PreparationDate,
    hi.PreparationDateInText,
    hi.DeliveryDate,
    hi.DeliveryDateInText,
    hi.DocumentPreparationDate,
    hi.DocumentPreparationDateInText,
    hi.StandardDeliveryDate,
    hi.StandardDeliveryDateInText,
    hi.ReceivedDate,
    hi.ReceivedDateInText,
    hi.EngineeringConsideration,
    hi.PlanningConsideration,
    CAST(ISNULL(hi.IsDeleted, 0) AS BIT) AS IsDeleted,
    CAST(ISNULL(hi.IsRoutine, 0) AS BIT) AS IsRoutine,
    CAST(ISNULL(hi.IsBuildInside, 0) AS BIT) AS IsBuildInside,
    hi.DeviceTypeId,
    hi.EquipmentTypeId,
    hi.CreatedDate,
    hi.CreatedDateInText,
    po.Number AS HtsPoNumber
INTO #ItemSrc
FROM [TMS].[TotalSystem].[dbo].[Pln_ProductionOrderItem] hi
INNER JOIN [TMS].[TotalSystem].[dbo].[Pln_ProductionOrder] po ON po.Id = hi.ProductionOrderId
WHERE (
        hi.RahkaranId IS NULL
        OR NOT EXISTS (
            SELECT 1 FROM Sale.ProductionOrderItem ai WHERE ai.RahkaranHistoryId = hi.RahkaranId
        )
      );

-- Skip attaching onto collision live Numbers when item has no resolvable PoMap
SELECT @SkipItemCollision = COUNT(*)
FROM #ItemSrc s
INNER JOIN #CollisionNumbers c ON c.Number = s.HtsPoNumber
WHERE NOT EXISTS (SELECT 1 FROM #PoMap m WHERE m.HtsPoId = s.HtsPoId);

IF OBJECT_ID(N'tempdb..#ItemMapped') IS NOT NULL DROP TABLE #ItemMapped;
SELECT
    s.*,
    m.AppPoId,
    ap.Id AS AppPartId,
    ap.Code AS AppPartCode
INTO #ItemMapped
FROM #ItemSrc s
INNER JOIN #PoMap m ON m.HtsPoId = s.HtsPoId
LEFT JOIN Inv.Part ap ON ap.HtsId = s.HtsPartId AND ap.HtsId <> 0
WHERE NOT EXISTS (
    SELECT 1 FROM Sale.ProductionOrderItem ai WHERE ai.RahkaranHistoryId = s.AppRahkaranHistoryId
);

SELECT @SkipItemNoParent = COUNT(*)
FROM #ItemSrc s
WHERE NOT EXISTS (SELECT 1 FROM #PoMap m WHERE m.HtsPoId = s.HtsPoId)
  AND NOT EXISTS (SELECT 1 FROM #CollisionNumbers c WHERE c.Number = s.HtsPoNumber);

SELECT @SkipItemNoPart = COUNT(*)
FROM #ItemMapped WHERE AppPartId IS NULL;

INSERT INTO Sale.ProductionOrderItem
(
    ProductionOrderId,
    HamkaranId,
    RahkaranHistoryId,
    Revision,
    IsLatestVersion,
    PartId,
    PartCode,
    Amount,
    PartModel,
    AirendOrCategory,
    Capacity,
    InputPressureBar,
    OutputPressureBar,
    GasType,
    MovingType,
    PurityPercentage,
    BarometricPressure,
    MaximumTemperature,
    RelativeHumidity,
    Scale,
    HasInspection,
    AgreedDeliverDate,
    SalesConsideration,
    CheckStatus,
    Serial,
    Status,
    SerialType,
    PlanningNumber,
    ProductionStep,
    ProductionStatus,
    ProductionStartMiladiDate,
    ProductionStartShamsiDate,
    ProductionEndMiladiDate,
    ProductionEndShamsiDate,
    TestingEndMiladiDate,
    TestingEndShamsiDate,
    PreparationMiladiDate,
    PreparationShamsiDate,
    DeliveryMiladiDate,
    DeliveryShamsiDate,
    DocumentPreparationMiladiDate,
    DocumentPreparationShamsiDate,
    StandardDeliveryMiladiDate,
    StandardDeliveryShamsiDate,
    SendToIndustrialMiladiDate,
    SendToIndustrialShamsiDate,
    EngineeringConsideration,
    PlanningConsideration,
    IsDeleted,
    IsRoutine,
    IsBuildInside,
    DeviceType,
    EquipmentType,
    Version,
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
    m.AppPoId,
    NULL,
    m.AppRahkaranHistoryId,
    m.Revision,
    m.IsLatestVersion,
    m.AppPartId,
    m.AppPartCode,
    m.Amount,
    m.PartModel,
    m.AirendOrCategory,
    m.Capacity,
    m.InputPressureBar,
    m.OutputPressureBar,
    m.GasType,
    m.MovingType,
    m.PurityPercentage,
    m.BarometricPressure,
    m.MaximumTemperature,
    m.RelativeHumidity,
    m.Scale,
    m.HasInspection,
    m.AgreedDeliverDate,
    m.SalesConsideration,
    CASE WHEN m.BuyStatusId IS NULL THEN NULL ELSE CAST(m.BuyStatusId AS INT) END,
    m.Serial,
    ISNULL(CAST(m.StatusId AS INT), 579),
    ISNULL(CAST(m.SerialTypeId AS INT), 0),
    CAST(ISNULL(m.PlanningNumber, 0) AS BIGINT),
    ISNULL(CAST(m.ProductionStepId AS INT), 0),
    ISNULL(CAST(m.ProductionStatusId AS INT), 0),
    m.ProductionStartDateInEurope,
    m.ProductionStartDate,
    m.ProductionEndDateInEurope,
    m.ProductionEndDate,
    m.TestingEndDateInEurope,
    m.TestingEndDate,
    m.PreparationDate,
    m.PreparationDateInText,
    m.DeliveryDate,
    m.DeliveryDateInText,
    m.DocumentPreparationDate,
    m.DocumentPreparationDateInText,
    m.StandardDeliveryDate,
    m.StandardDeliveryDateInText,
    m.ReceivedDate,
    m.ReceivedDateInText,
    m.EngineeringConsideration,
    m.PlanningConsideration,
    m.IsDeleted,
    m.IsRoutine,
    m.IsBuildInside,
    CASE WHEN m.DeviceTypeId IS NULL THEN NULL ELSE CAST(m.DeviceTypeId AS INT) END,
    CASE WHEN m.EquipmentTypeId IS NULL THEN NULL ELSE CAST(m.EquipmentTypeId AS INT) END,
    CASE WHEN m.AppRahkaranHistoryId < 0 THEN N'HTS:' + CAST(m.HtsItemId AS NVARCHAR(20)) ELSE NULL END,
    1,
    @Seed,
    ISNULL(m.CreatedDate, @Now),
    ISNULL(NULLIF(m.CreatedDateInText, N''), @NowShamsi),
    1,
    @Seed,
    @Now,
    @NowShamsi,
    1
FROM #ItemMapped m
WHERE m.AppPartId IS NOT NULL;

SET @InsItem = @@ROWCOUNT;

/* --------------------------------------------------------------------------
   3) BOM — for items we can resolve (including newly inserted legacy items)
   -------------------------------------------------------------------------- */
IF OBJECT_ID(N'tempdb..#BomSrc') IS NOT NULL DROP TABLE #BomSrc;
SELECT
    CASE
        WHEN hi.RahkaranId IS NOT NULL THEN CAST(hi.RahkaranId AS BIGINT)
        ELSE -CAST(hi.Id AS BIGINT)
    END AS AppRahkaranHistoryId,
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
INNER JOIN [TMS].[TotalSystem].[dbo].[Pln_ProductionOrderItem] hi ON hi.Id = b.ProductionOrderItemId
LEFT JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] hp ON hp.Part_ID = b.PartId
WHERE b.Revision IS NOT NULL
  AND (
        -- legacy items without Rahkaran id
        hi.RahkaranId IS NULL
        -- or items inserted in this cutover (positive history that was missing)
        OR EXISTS (
            SELECT 1
            FROM Sale.ProductionOrderItem ai
            WHERE ai.RahkaranHistoryId = hi.RahkaranId
              AND ai.CreatedByName = @Seed
        )
      );

IF OBJECT_ID(N'tempdb..#BomMapped') IS NOT NULL DROP TABLE #BomMapped;
SELECT
    ai.Id AS ProductionOrderItemId,
    s.Revision,
    COALESCE(pHts.Id, pCode.Id) AS PartId,
    s.Amount,
    s.IsLatest,
    s.Description,
    s.SaleUnitDetails,
    s.NeedsAVL
INTO #BomMapped
FROM #BomSrc s
INNER JOIN Sale.ProductionOrderItem ai ON ai.RahkaranHistoryId = s.AppRahkaranHistoryId
LEFT JOIN Inv.Part pHts ON pHts.HtsId = s.HtsPartId AND pHts.HtsId <> 0
OUTER APPLY (
    SELECT TOP (1) p.Id
    FROM Inv.Part p
    WHERE pHts.Id IS NULL
      AND s.HtsPartCode IS NOT NULL
      AND p.Code = s.HtsPartCode
    ORDER BY CASE WHEN p.IsActive = 1 THEN 0 ELSE 1 END, p.Id
) pCode;

SELECT @SkipBomNoItem = COUNT(*)
FROM #BomSrc s
WHERE NOT EXISTS (
    SELECT 1 FROM Sale.ProductionOrderItem ai WHERE ai.RahkaranHistoryId = s.AppRahkaranHistoryId
);

SELECT @SkipBomNoPart = COUNT(*)
FROM #BomMapped WHERE PartId IS NULL;

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

SET @UpdBom = @@ROWCOUNT;

INSERT INTO Sale.ProductionOrderItemBom
(
    ProductionOrderItemId, Revision, PartId, Amount, IsLatest,
    Description, SaleUnitDetails, NeedsAVL,
    ProductionStep, Status, NumberSupplied,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
    ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
    IsActive
)
SELECT
    d.ProductionOrderItemId, d.Revision, d.PartId, d.Amount, d.IsLatest,
    d.Description, d.SaleUnitDetails, d.NeedsAVL,
    0, 0, NULL,
    1, @Seed, @Now, @NowShamsi,
    1, @Seed, @Now, @NowShamsi,
    1
FROM #BomDedup d
WHERE NOT EXISTS (
    SELECT 1 FROM Sale.ProductionOrderItemBom t
    WHERE t.ProductionOrderItemId = d.ProductionOrderItemId
      AND t.Revision = d.Revision
      AND t.PartId = d.PartId
);

SET @InsBom = @@ROWCOUNT;

COMMIT TRAN;

DECLARE @AfterPo INT = (SELECT COUNT(*) FROM Sale.ProductionOrder);
DECLARE @AfterItem INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItem);
DECLARE @AfterBom INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItemBom);
DECLARE @AfterDelay INT = (SELECT COUNT(*) FROM Pln.ProductionOrderDelay);
DECLARE @ItemLatest INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItem WHERE IsLatestVersion = 1);
DECLARE @ItemHist INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItem WHERE IsLatestVersion = 0);
DECLARE @BomLatest INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItemBom WHERE IsLatest = 1);
DECLARE @BomHist INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItemBom WHERE IsLatest = 0);
DECLARE @NegHist INT = (SELECT COUNT(*) FROM Sale.ProductionOrderItem WHERE RahkaranHistoryId < 0);
DECLARE @DupBom INT = (
    SELECT COUNT(*) FROM (
        SELECT ProductionOrderItemId, Revision, PartId
        FROM Sale.ProductionOrderItemBom
        GROUP BY ProductionOrderItemId, Revision, PartId
        HAVING COUNT(*) > 1
    ) x
);
DECLARE @DupNegHist INT = (
    SELECT COUNT(*) FROM (
        SELECT RahkaranHistoryId FROM Sale.ProductionOrderItem
        WHERE RahkaranHistoryId < 0
        GROUP BY RahkaranHistoryId HAVING COUNT(*) > 1
    ) x
);
DECLARE @DupPoNumNullHam INT = (
    SELECT COUNT(*) FROM (
        SELECT ProductionOrderNumber FROM Sale.ProductionOrder
        WHERE HamkaranId IS NULL AND ProductionOrderNumber IS NOT NULL
        GROUP BY ProductionOrderNumber HAVING COUNT(*) > 1
    ) x
);

SELECT
    @BeforePo AS BeforePo, @AfterPo AS AfterPo, @InsPo AS InsertedPo, @SkipPoCollision AS SkippedPoCollision,
    @BeforeItem AS BeforeItem, @AfterItem AS AfterItem, @ItemLatest AS ItemLatest, @ItemHist AS ItemHist,
    @InsItem AS InsertedItem, @SkipItemNoParent AS SkippedItemNoParent,
    @SkipItemNoPart AS SkippedItemNoPart, @SkipItemCollision AS SkippedItemCollision,
    @BeforeBom AS BeforeBom, @AfterBom AS AfterBom, @BomLatest AS BomLatest, @BomHist AS BomHist,
    @InsBom AS InsertedBom, @UpdBom AS UpdatedBom,
    @SkipBomNoItem AS SkippedBomNoItem, @SkipBomNoPart AS SkippedBomNoPart,
    @BeforeDelay AS BeforeDelay, @AfterDelay AS AfterDelay,
    @NegHist AS NegHistoryItemCount,
    @DupBom AS DupBomKeys, @DupNegHist AS DupNegHistKeys, @DupPoNumNullHam AS DupLegacyPoNumbers;

IF @AfterDelay <> @BeforeDelay
    RAISERROR(N'Delay count changed — unexpected.', 16, 1);
IF @DupBom <> 0 OR @DupNegHist <> 0 OR @DupPoNumNullHam <> 0
    RAISERROR(N'Duplicate business keys detected.', 16, 1);

PRINT N'=== DONE Sync_ProductionOrder_LegacyNoRahkaran_FromTotalSystem ===';
