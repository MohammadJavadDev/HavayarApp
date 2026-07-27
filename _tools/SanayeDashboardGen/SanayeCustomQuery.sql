SELECT
    SUM(CASE WHEN poi.ProductionStep = 214 THEN 1 ELSE 0 END) AS Z_MontazereTolid,
    SUM(CASE WHEN poi.ProductionStep = 215 THEN 1 ELSE 0 END) AS Z_DarHaleTolid,
    SUM(CASE WHEN poi.ProductionStep = 216 THEN 1 ELSE 0 END) AS Z_Test,
    SUM(CASE WHEN poi.ProductionStep = 220 THEN 1 ELSE 0 END) AS Z_Bastebandi,
    SUM(CASE WHEN poi.ProductionStep = 221 THEN 1 ELSE 0 END) AS Z_AmadeyeErsal,
    SUM(CASE WHEN poi.ProductionStep = 223 THEN 1 ELSE 0 END) AS Z_TahvilForosh,
    SUM(CASE WHEN poi.ProductionStep = 246 THEN 1 ELSE 0 END) AS Z_TadarokatDarRah,
    SUM(CASE WHEN poi.ProductionStep IN (220, 216, 223, 221) THEN 1 ELSE 0 END) AS Z_SumOfPassedProductionSteps,
    SUM(CASE WHEN poi.ProductionStep IN (214, 215, 216, 220, 221, 223, 246) THEN 1 ELSE 0 END) AS Z_SumOfProductionSteps,
    SUM(CASE WHEN poi.ProductionStep IN (214, 215, 216, 220, 221, 223, 246) THEN 1 ELSE 0 END)
      - SUM(CASE WHEN poi.ProductionStep IN (220, 216, 223, 221) THEN 1 ELSE 0 END) AS Z_RemainedCount,
    CAST(
      CASE WHEN SUM(CASE WHEN poi.ProductionStep IN (214, 215, 216, 220, 221, 223, 246) THEN 1 ELSE 0 END) = 0 THEN 0
      ELSE 100.0 * (
        SUM(CASE WHEN poi.ProductionStep IN (214, 215, 216, 220, 221, 223, 246) THEN 1 ELSE 0 END)
        - SUM(CASE WHEN poi.ProductionStep IN (220, 216, 223, 221) THEN 1 ELSE 0 END)
      ) / SUM(CASE WHEN poi.ProductionStep IN (214, 215, 216, 220, 221, 223, 246) THEN 1 ELSE 0 END)
      END AS decimal(18,2)
    ) AS Z_RemainedPercent
FROM Sale.ProductionOrderItem poi
WHERE 
    poi.IsActive = 1
AND ISNULL(poi.IsDeleted, 0) = 0
AND ISNULL(poi.Serial, N'') <> N''
AND poi.ProductionStartMiladiDate IS NOT NULL
AND LEFT(ISNULL(poi.CreatedOnShamsiDateTime, N''), 4) = N'1404';

SELECT
    poi.Id,
    poi.Serial,
    ISNULL(poi.PartCode, p.Code) AS PartCode,
    p.Name AS PartName,
    b.Title AS BranchTitle,
    COALESCE(NULLIF(po.EndUser, N''), party.FullName, N'') AS CustomerName,
    CASE poi.EquipmentType
        WHEN 1947 THEN N'تجهیز اصلی'
        WHEN 1948 THEN N'متعلقات تجهیز'
        ELSE ISNULL(poi.PartModel, N'نامشخص')
    END AS EquipmentCategory,
    poi.ProductionStep,
    CASE poi.ProductionStep
        WHEN 214 THEN N'منتظر تولید'
        WHEN 215 THEN N'در حال تولید'
        WHEN 216 THEN N'تست'
        WHEN 220 THEN N'بسته‌بندی'
        WHEN 221 THEN N'آماده ارسال'
        WHEN 223 THEN N'تحویل فروش'
        WHEN 246 THEN N'تدارکات در راه'
        WHEN 213 THEN N'در انتظار مهندسی'
        ELSE N'سایر'
    END AS ProductionStepName,
    LEFT(ISNULL(poi.CreatedOnShamsiDateTime, N''), 4) AS ShamsiYear,
    1 AS ItemCount,
    poi.ProductionStartShamsiDate,
    poi.CreatedOnShamsiDateTime
FROM Sale.ProductionOrderItem poi
INNER JOIN Sale.ProductionOrder po ON po.Id = poi.ProductionOrderId
LEFT JOIN Inv.Part p ON p.Id = poi.PartId
LEFT JOIN Sale.Branch b ON b.Id = po.BranchId
LEFT JOIN SLS.Customer cust ON cust.Id = po.SalesAgencyId
LEFT JOIN Gnr.Party party ON party.Id = cust.PartyId
WHERE 
    poi.IsActive = 1
AND ISNULL(poi.IsDeleted, 0) = 0
AND ISNULL(poi.Serial, N'') <> N''
AND poi.ProductionStartMiladiDate IS NOT NULL
AND LEFT(ISNULL(poi.CreatedOnShamsiDateTime, N''), 4) = N'1404'
  AND poi.ProductionStep IN (214, 215, 216, 220, 221, 223, 246, 213);

SELECT
    poi.Id,
    poi.Serial,
    ISNULL(poi.PartCode, p.Code) AS PartCode,
    p.Name AS PartName,
    b.Title AS BranchTitle,
    COALESCE(NULLIF(po.EndUser, N''), party.FullName, N'') AS CustomerName,
    CASE poi.EquipmentType WHEN 1947 THEN N'تجهیز اصلی' WHEN 1948 THEN N'متعلقات تجهیز' ELSE ISNULL(poi.PartModel, N'نامشخص') END AS EquipmentCategory,
    poi.ProductionStep,
    N'در انتظار مهندسی' AS ProductionStepName,
    LEFT(ISNULL(poi.CreatedOnShamsiDateTime, N''), 4) AS ShamsiYear,
    1 AS ItemCount,
    poi.ProductionStartShamsiDate,
    poi.CreatedOnShamsiDateTime
FROM Sale.ProductionOrderItem poi
INNER JOIN Sale.ProductionOrder po ON po.Id = poi.ProductionOrderId
LEFT JOIN Inv.Part p ON p.Id = poi.PartId
LEFT JOIN Sale.Branch b ON b.Id = po.BranchId
LEFT JOIN SLS.Customer cust ON cust.Id = po.SalesAgencyId
LEFT JOIN Gnr.Party party ON party.Id = cust.PartyId
WHERE poi.IsActive = 1
  AND ISNULL(poi.IsDeleted, 0) = 0
  AND ISNULL(poi.Serial, N'') <> N''
  AND poi.ProductionStartMiladiDate IS NOT NULL
  AND poi.ProductionStep = 213;

SELECT
    poi.Id,
    poi.Serial,
    ISNULL(poi.PartCode, p.Code) AS PartCode,
    p.Name AS PartName,
    b.Title AS BranchTitle,
    COALESCE(NULLIF(po.EndUser, N''), party.FullName, N'') AS CustomerName,
    poi.ProductionEndShamsiDate,
    poi.PreparationShamsiDate,
    poi.TestingEndShamsiDate,
    1 AS ItemCount
FROM Sale.ProductionOrderItem poi
INNER JOIN Sale.ProductionOrder po ON po.Id = poi.ProductionOrderId
LEFT JOIN Inv.Part p ON p.Id = poi.PartId
LEFT JOIN Sale.Branch b ON b.Id = po.BranchId
LEFT JOIN SLS.Customer cust ON cust.Id = po.SalesAgencyId
LEFT JOIN Gnr.Party party ON party.Id = cust.PartyId
WHERE 
    poi.IsActive = 1
AND ISNULL(poi.IsDeleted, 0) = 0
AND ISNULL(poi.Serial, N'') <> N''
AND poi.ProductionStartMiladiDate IS NOT NULL
AND LEFT(ISNULL(poi.CreatedOnShamsiDateTime, N''), 4) = N'1404'
  AND poi.ProductionStep = 221;

SELECT
    o.Id,
    o.PurchaseRequestNumber,
    p.Code AS PartCode,
    p.Name AS PartName,
    o.RequiredQty,
    o.OrderQty,
    o.StopShamsiDate,
    o.Status,
    o.RequestedPersonel,
    o.Comment
FROM Sup.OpenOrderRequest o
LEFT JOIN Inv.Part p ON p.Id = o.PartId
WHERE o.IsStop = 1
  AND o.IsDeleted = 0;

SELECT
    o.Id,
    o.PurchaseRequestNumber,
    p.Code AS PartCode,
    p.Name AS PartName,
    o.RequiredQty,
    o.OrderQty,
    o.PurchaseRequestShamsiDate,
    o.RequestedPersonel,
    o.IsRoutineRequest,
    o.Status
FROM Sup.OpenOrderRequest o
LEFT JOIN Inv.Part p ON p.Id = o.PartId
WHERE ISNULL(o.EngineeringAccept, 0) = 0
  AND o.IsDeleted = 0
  AND o.IsActive = 1;

SELECT
    CONCAT(N'هفته ', CONVERT(varchar(10), WeekStart, 23)) AS WeekLabel,
    CONVERT(varchar(10), WeekStart, 111) AS WeekStartShamsi,
    COUNT(1) AS ItemCount,
    SUM(CASE WHEN ProductionStep IN (220, 216, 223, 221) THEN 1 ELSE 0 END) AS PassedCount,
    SUM(CASE WHEN ProductionStep IN (214, 215, 216, 220, 221, 223, 246) THEN 1 ELSE 0 END)
      - SUM(CASE WHEN ProductionStep IN (220, 216, 223, 221) THEN 1 ELSE 0 END) AS RemainCount
FROM (
    SELECT
        poi.ProductionStep,
        DATEADD(DAY, 1 - DATEPART(WEEKDAY, poi.ProductionStartMiladiDate), CAST(poi.ProductionStartMiladiDate AS date)) AS WeekStart
    FROM Sale.ProductionOrderItem poi
    WHERE 
    poi.IsActive = 1
AND ISNULL(poi.IsDeleted, 0) = 0
AND ISNULL(poi.Serial, N'') <> N''
AND poi.ProductionStartMiladiDate IS NOT NULL
AND LEFT(ISNULL(poi.CreatedOnShamsiDateTime, N''), 4) = N'1404'
      AND poi.ProductionStep IN (214, 215, 216, 220, 221, 223, 246)
) Src
GROUP BY WeekStart
ORDER BY WeekStart;

SELECT
    CASE WHEN o.IsRoutineRequest = 1 THEN N'روتین' ELSE N'غیر روتین' END AS RequestType,
    COUNT(1) AS Cnt
FROM Sup.OpenOrderRequest o
WHERE o.IsDeleted = 0
  AND o.IsActive = 1
GROUP BY CASE WHEN o.IsRoutineRequest = 1 THEN N'روتین' ELSE N'غیر روتین' END;