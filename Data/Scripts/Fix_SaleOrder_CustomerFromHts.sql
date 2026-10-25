/*
================================================================================
Fix_SaleOrder_CustomerFromHts.sql
================================================================================
یک‌بار حواله و سریال HavayarApp را با مشتری TotalSystem هم‌تراز می‌کند.
جاب همگام‌سازی HTS تغییر نمی‌کند و شناسهٔ مشتری‌ها جابه‌جا نمی‌شود.

مشتری پذیرفته می‌شود فقط اگر:
  - SLS.Customer.Code = Crm_Customer.Customer_Code
  - LTRIM/RTRIM نام Gnr.Party.FullName با Customer_Title یکی باشد
  - و شناسه سازگار باشد: تنها یک ردیف با آن کد و نام، یا اگر چند ردیف است
    فقط ردیفی که HtsId برابر Customer_FK است

حوالهٔ HtsId=0 (نسخهٔ راهکاران) دست نمی‌خورد.
سریال فقط از NewCustomer_FK پر می‌شود.
قلم‌های بدون حواله در سیستم قبلی درج نمی‌شوند.

Encoding: UTF-8 with BOM
Apply:
  sqlcmd -S 172.20.40.42 -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Fix_SaleOrder_CustomerFromHts.sql"
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    THROW 50001, N'Linked Server [TMS] یافت نشد.', 1;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), GETDATE(), 120);
DECLARE @User NVARCHAR(80) = N'fix-saleorder-customer-from-hts';

IF OBJECT_ID(N'tempdb..#HtsCustomer') IS NOT NULL DROP TABLE #HtsCustomer;
IF OBJECT_ID(N'tempdb..#Named') IS NOT NULL DROP TABLE #Named;
IF OBJECT_ID(N'tempdb..#Resolve') IS NOT NULL DROP TABLE #Resolve;
IF OBJECT_ID(N'tempdb..#HtsOrder') IS NOT NULL DROP TABLE #HtsOrder;
IF OBJECT_ID(N'tempdb..#HtsDetail') IS NOT NULL DROP TABLE #HtsDetail;
IF OBJECT_ID(N'tempdb..#HtsSerialCust') IS NOT NULL DROP TABLE #HtsSerialCust;
IF OBJECT_ID(N'tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;

SELECT
    CAST(c.Customer_ID AS BIGINT) AS CustomerHtsId,
    CAST(c.Customer_Code AS INT) AS Code,
    NULLIF(LTRIM(RTRIM(c.Customer_Title)) COLLATE DATABASE_DEFAULT, N'') AS Title
INTO #HtsCustomer
FROM OPENQUERY(TMS, N'SELECT Customer_ID, Customer_Code, Customer_Title FROM TotalSystem.dbo.Crm_Customer') c;

CREATE UNIQUE CLUSTERED INDEX IX_HtsCustomer ON #HtsCustomer (CustomerHtsId);

SELECT
    h.CustomerHtsId,
    c.Id AS CustomerId,
    c.HtsId AS AppHtsId
INTO #Named
FROM #HtsCustomer h
INNER JOIN SLS.Customer c ON c.Code = h.Code
INNER JOIN Gnr.Party p ON p.Id = c.PartyId
WHERE h.Title IS NOT NULL
  AND NULLIF(LTRIM(RTRIM(p.FullName)), N'') IS NOT NULL
  AND LTRIM(RTRIM(p.FullName)) COLLATE DATABASE_DEFAULT = h.Title;

CREATE CLUSTERED INDEX IX_Named ON #Named (CustomerHtsId);

SELECT CustomerHtsId, CustomerId
INTO #Resolve
FROM (
    SELECT
        n.CustomerHtsId,
        n.CustomerId,
        n.AppHtsId,
        COUNT(*) OVER (PARTITION BY n.CustomerHtsId) AS Cnt,
        SUM(CASE WHEN n.AppHtsId = n.CustomerHtsId AND n.AppHtsId <> 0 THEN 1 ELSE 0 END)
            OVER (PARTITION BY n.CustomerHtsId) AS HtsHits
    FROM #Named n
) x
WHERE x.Cnt = 1
   OR (x.Cnt > 1 AND x.HtsHits = 1 AND x.AppHtsId = x.CustomerHtsId AND x.AppHtsId <> 0);

CREATE UNIQUE CLUSTERED INDEX IX_Resolve ON #Resolve (CustomerHtsId);

SELECT
    CAST(s.Order_ID AS BIGINT) AS HtsId,
    s.VchHdrId AS HamkaranId,
    CAST(s.Year AS NVARCHAR(20)) AS [Year],
    s.VchDate AS VchDateMiladiDate,
    NULLIF(LTRIM(RTRIM(s.VchDate_Shamsi)), N'') AS VchDateShamsiDate,
    CAST(s.Customer_FK AS BIGINT) AS CustomerHtsId,
    CAST(s.VchType_FK AS INT) AS VoucherTypeStatus,
    CAST(s.Status_FK AS INT) AS StatusEnum,
    CAST(s.Branch_FK AS BIGINT) AS BranchHtsId,
    NULLIF(LTRIM(RTRIM(s.Contract_Number)), N'') AS ContractNumber,
    CAST(s.VchNo AS BIGINT) AS OrderNumber,
    CAST(s.VchNo AS BIGINT) AS HtsVchNo,
    CAST(s.Stock_FK AS BIGINT) AS HtsStockId,
    CAST(s.BaseVchType_FK AS BIGINT) AS HtsBaseVoucherTypeId,
    s.ContractVchHeaderId AS HtsContractVchHeaderId,
    CAST(s.Confirmer_FK AS BIGINT) AS ConfirmerHtsId,
    CAST(s.Operator_FK AS BIGINT) AS OperatorHtsId
INTO #HtsOrder
FROM OPENQUERY(TMS, N'
    SELECT Order_ID, VchHdrId, Year, VchDate, VchDate_Shamsi, Customer_FK,
           VchType_FK, Status_FK, Branch_FK, Contract_Number, VchNo,
           Stock_FK, BaseVchType_FK, ContractVchHeaderId, Confirmer_FK, Operator_FK
    FROM TotalSystem.dbo.Sale_Order') s;

CREATE UNIQUE CLUSTERED INDEX IX_HtsOrder_HtsId ON #HtsOrder (HtsId);

SELECT
    CAST(d.OrderDetail_ID AS BIGINT) AS HtsId,
    CAST(d.Order_FK AS BIGINT) AS OrderHtsId,
    d.Hamkaran_Vchitm_FK AS HamkaranId,
    d.Hamkaran_VchHdr_FK AS HamkaranOrderHdrId,
    d.Hamkaran_InvVchitm_FK AS HamkaranInvVchItmId,
    CAST(d.Prefactor_VchNo AS BIGINT) AS Prefactor_VchNo,
    CAST(d.Seq AS INT) AS Seq,
    CAST(d.Part_FK AS BIGINT) AS PartHtsId,
    d.Qty,
    d.BaseVchItmRef,
    d.Description,
    d.UnitPrice, d.Price, d.IncPrice, d.DecPrice,
    CAST(d.PayMethod_FK AS INT) AS PayMethod,
    d.PriceTemp,
    ISNULL(d.QtyTemp, 0) AS QtyTemp,
    d.IsPackage, d.NotComplete
INTO #HtsDetail
FROM OPENQUERY(TMS, N'
    SELECT OrderDetail_ID, Order_FK, Hamkaran_Vchitm_FK, Hamkaran_VchHdr_FK,
           Hamkaran_InvVchitm_FK, Prefactor_VchNo, Seq, Part_FK, Qty,
           BaseVchItmRef, Description, UnitPrice, Price, IncPrice, DecPrice,
           PayMethod_FK, PriceTemp, QtyTemp, IsPackage, NotComplete
    FROM TotalSystem.dbo.Sale_OrderDetail') d;

CREATE UNIQUE CLUSTERED INDEX IX_HtsDetail_HtsId ON #HtsDetail (HtsId);
CREATE NONCLUSTERED INDEX IX_HtsDetail_Order ON #HtsDetail (OrderHtsId);

SELECT
    CAST(s.OrderDetail_Serial_ID AS BIGINT) AS HtsId,
    CAST(s.NewCustomer_FK AS BIGINT) AS CustomerHtsId
INTO #HtsSerialCust
FROM OPENQUERY(TMS, N'SELECT OrderDetail_Serial_ID, NewCustomer_FK FROM TotalSystem.dbo.Sale_OrderDetail_Serial') s;

CREATE UNIQUE CLUSTERED INDEX IX_HtsSerialCust ON #HtsSerialCust (HtsId);

SELECT
    CAST(ou.User_ID AS BIGINT) AS OldUserId,
    MIN(nu.Id) AS NewUserId
INTO #UserMap
FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
INNER JOIN [system].[User] nu
    ON nu.Username COLLATE DATABASE_DEFAULT = ou.Username COLLATE DATABASE_DEFAULT
    OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
        AND nu.Username COLLATE DATABASE_DEFAULT = LTRIM(RTRIM(ou.ActiveDirectoryUsername)) COLLATE DATABASE_DEFAULT)
GROUP BY CAST(ou.User_ID AS BIGINT);

CREATE UNIQUE CLUSTERED INDEX IX_UserMap ON #UserMap (OldUserId);

IF NOT EXISTS (SELECT 1 FROM #Resolve)
BEGIN
    THROW 50002, N'هیچ مشتری با کد و نام دقیق پیدا نشد. بارگذاری یا تطبیق نام غلط است.', 1;
END;

BEGIN TRANSACTION;

IF OBJECT_ID(N'SLS._SaleOrderCustFix') IS NOT NULL DROP TABLE SLS._SaleOrderCustFix;
IF OBJECT_ID(N'SLS._SaleOrderCustSkip') IS NOT NULL DROP TABLE SLS._SaleOrderCustSkip;
IF OBJECT_ID(N'SLS._SaleOrderSerialCustFix') IS NOT NULL DROP TABLE SLS._SaleOrderSerialCustFix;

SELECT
    o.Id AS OrderId,
    o.HtsId AS OrderHtsId,
    o.CustomerId AS OldCustomerId,
    r.CustomerId AS NewCustomerId,
    oldc.Code AS OldCode,
    newc.Code AS NewCode,
    h.Title AS HtsTitle
INTO SLS._SaleOrderCustFix
FROM Sale.[Order] o
INNER JOIN #HtsOrder ho ON ho.HtsId = o.HtsId
INNER JOIN #Resolve r ON r.CustomerHtsId = ho.CustomerHtsId
INNER JOIN #HtsCustomer h ON h.CustomerHtsId = ho.CustomerHtsId
LEFT JOIN SLS.Customer oldc ON oldc.Id = o.CustomerId
INNER JOIN SLS.Customer newc ON newc.Id = r.CustomerId
WHERE o.HtsId <> 0
  AND ISNULL(o.CustomerId, 0) <> r.CustomerId;

UPDATE o
SET o.CustomerId = f.NewCustomerId,
    o.ModifiedById = 1,
    o.ModifiedByName = @User,
    o.ModifiedDateMiladiDateTime = @Now,
    o.ModifiedDateShamsiDateTime = @NowShamsi
FROM Sale.[Order] o
INNER JOIN SLS._SaleOrderCustFix f ON f.OrderId = o.Id;

DECLARE @OrdersUpdated INT = @@ROWCOUNT;

INSERT INTO Sale.[Order]
    (HtsId, [Year], VchDateMiladiDate, VchDateShamsiDate, CustomerId, VoucherTypeStatus, StatusEnum,
     BranchId, ContractNumber, HamkaranId, OrderNumber, HtsVchNo, HtsStockId, HtsBaseVoucherTypeId,
     HtsContractVchHeaderId, ConfirmerUserId, OperatorUserId,
     CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT
    h.HtsId, h.[Year], h.VchDateMiladiDate, h.VchDateShamsiDate, r.CustomerId, h.VoucherTypeStatus, h.StatusEnum,
    b.Id, h.ContractNumber, h.HamkaranId, h.OrderNumber, h.HtsVchNo, h.HtsStockId, h.HtsBaseVoucherTypeId,
    h.HtsContractVchHeaderId, umc.NewUserId, umo.NewUserId,
    1, @User, @Now, @NowShamsi, 1, @User, @Now, @NowShamsi, 1
FROM #HtsOrder h
INNER JOIN #Resolve r ON r.CustomerHtsId = h.CustomerHtsId
LEFT JOIN Sale.Branch b ON b.HtsId = h.BranchHtsId AND b.HtsId <> 0
LEFT JOIN #UserMap umc ON umc.OldUserId = h.ConfirmerHtsId
LEFT JOIN #UserMap umo ON umo.OldUserId = h.OperatorHtsId
WHERE NOT EXISTS (
    SELECT 1 FROM Sale.[Order] o
    WHERE o.HtsId = h.HtsId AND o.HtsId <> 0);

DECLARE @OrdersInserted INT = @@ROWCOUNT;

UPDATE d
SET d.Sale_OrderId = o.Id,
    d.ModifiedById = 1,
    d.ModifiedByName = @User,
    d.ModifiedDateMiladiDateTime = @Now,
    d.ModifiedDateShamsiDateTime = @NowShamsi
FROM Sale.OrderDetail d
INNER JOIN #HtsDetail h ON h.HtsId = d.HtsId
INNER JOIN Sale.[Order] o ON o.HtsId = h.OrderHtsId AND o.HtsId <> 0
WHERE d.HtsId <> 0
  AND h.OrderHtsId IS NOT NULL
  AND d.Sale_OrderId <> o.Id;

DECLARE @DetailsReparented INT = @@ROWCOUNT;

INSERT INTO Sale.OrderDetail
    (HtsId, Sale_OrderId, HamkaranId, HamkaranOrderHdrId, HamkaranInvVchItmId, Prefactor_VchNo, Seq, PartId,
     Qty, BaseVchItmRef, Description, UnitPrice, Price, IncPrice, DecPrice, PayMethod, PriceTemp, QtyTemp,
     IsPackage, NotComplete, CustomerId,
     CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT
    h.HtsId, o.Id, h.HamkaranId, h.HamkaranOrderHdrId, h.HamkaranInvVchItmId, h.Prefactor_VchNo, ISNULL(h.Seq, 0), p.Id,
    ISNULL(h.Qty, 0), h.BaseVchItmRef, h.Description, h.UnitPrice, h.Price, h.IncPrice, h.DecPrice,
    ISNULL(h.PayMethod, 0), h.PriceTemp, h.QtyTemp, h.IsPackage, h.NotComplete, o.CustomerId,
    1, @User, @Now, @NowShamsi, 1, @User, @Now, @NowShamsi, 1
FROM #HtsDetail h
INNER JOIN Sale.[Order] o ON o.HtsId = h.OrderHtsId AND o.HtsId <> 0
LEFT JOIN Inv.Part p ON p.HtsId = h.PartHtsId AND p.HtsId <> 0
WHERE h.OrderHtsId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM Sale.OrderDetail d
      WHERE d.HtsId = h.HtsId AND d.HtsId <> 0);

DECLARE @DetailsInserted INT = @@ROWCOUNT;

UPDATE d
SET d.CustomerId = o.CustomerId,
    d.ModifiedById = 1,
    d.ModifiedByName = @User,
    d.ModifiedDateMiladiDateTime = @Now,
    d.ModifiedDateShamsiDateTime = @NowShamsi
FROM Sale.OrderDetail d
INNER JOIN Sale.[Order] o ON o.Id = d.Sale_OrderId
WHERE d.HtsId <> 0
  AND o.HtsId <> 0
  AND o.CustomerId IS NOT NULL
  AND ISNULL(d.CustomerId, 0) <> o.CustomerId;

DECLARE @DetailsAligned INT = @@ROWCOUNT;

SELECT
    s.Id AS SerialId,
    s.HtsId AS SerialHtsId,
    s.CustomerId AS OldCustomerId,
    r.CustomerId AS NewCustomerId
INTO SLS._SaleOrderSerialCustFix
FROM Sale.OrderDetailSerial s
INNER JOIN #HtsSerialCust hs ON hs.HtsId = s.HtsId
INNER JOIN #Resolve r ON r.CustomerHtsId = hs.CustomerHtsId
WHERE s.HtsId <> 0
  AND hs.CustomerHtsId IS NOT NULL
  AND ISNULL(s.CustomerId, 0) <> r.CustomerId;

UPDATE s
SET s.CustomerId = f.NewCustomerId,
    s.ModifiedById = 1,
    s.ModifiedByName = @User,
    s.ModifiedDateMiladiDateTime = @Now,
    s.ModifiedDateShamsiDateTime = @NowShamsi
FROM Sale.OrderDetailSerial s
INNER JOIN SLS._SaleOrderSerialCustFix f ON f.SerialId = s.Id;

DECLARE @SerialsUpdated INT = @@ROWCOUNT;

SELECT
    o.HtsId AS OrderHtsId,
    ho.OrderNumber,
    ho.[Year],
    hc.Code AS HtsCode,
    hc.Title AS HtsTitle,
    names.AppNames,
    CASE
        WHEN NOT EXISTS (SELECT 1 FROM SLS.Customer c WHERE c.Code = hc.Code) THEN N'NoCustomer'
        WHEN NOT EXISTS (SELECT 1 FROM #Named n WHERE n.CustomerHtsId = ho.CustomerHtsId) THEN N'NameMismatch'
        ELSE N'Ambiguous'
    END AS Reason
INTO SLS._SaleOrderCustSkip
FROM Sale.[Order] o
INNER JOIN #HtsOrder ho ON ho.HtsId = o.HtsId
INNER JOIN #HtsCustomer hc ON hc.CustomerHtsId = ho.CustomerHtsId
LEFT JOIN (
    SELECT
        c.Code,
        STRING_AGG(CAST(c.Id AS NVARCHAR(20)) + N':' + LTRIM(RTRIM(p.FullName)), N' | ')
            WITHIN GROUP (ORDER BY c.Id) AS AppNames
    FROM SLS.Customer c
    INNER JOIN Gnr.Party p ON p.Id = c.PartyId
    GROUP BY c.Code
) names ON names.Code = hc.Code
WHERE o.HtsId <> 0
  AND NOT EXISTS (SELECT 1 FROM #Resolve r WHERE r.CustomerHtsId = ho.CustomerHtsId)
  AND NOT EXISTS (
      SELECT 1
      FROM SLS.Customer c
      INNER JOIN Gnr.Party p ON p.Id = c.PartyId
      WHERE c.Id = o.CustomerId
        AND c.Code = hc.Code
        AND hc.Title IS NOT NULL
        AND LTRIM(RTRIM(p.FullName)) COLLATE DATABASE_DEFAULT = hc.Title
  );

DECLARE @OrdersSkipped INT = @@ROWCOUNT;

COMMIT TRANSACTION;

SELECT
    @OrdersUpdated AS OrdersUpdated,
    @OrdersInserted AS OrdersInserted,
    @DetailsReparented AS DetailsReparented,
    @DetailsInserted AS DetailsInserted,
    @DetailsAligned AS DetailsAligned,
    @SerialsUpdated AS SerialsUpdated,
    @OrdersSkipped AS OrdersSkipped;

SELECT Reason, COUNT(*) AS Cnt
FROM SLS._SaleOrderCustSkip
GROUP BY Reason
ORDER BY Reason;

SELECT OrderHtsId, OrderNumber, [Year], HtsCode, HtsTitle, AppNames, Reason
FROM SLS._SaleOrderCustSkip
ORDER BY Reason, HtsCode, OrderHtsId;

SELECT
    s.Id AS SerialId,
    s.HtsId,
    s.Serial,
    o.OrderNumber,
    o.[Year],
    d.Seq,
    c.Code AS CustomerCode,
    LTRIM(RTRIM(p.FullName)) AS CustomerName
FROM Sale.OrderDetailSerial s
INNER JOIN Sale.OrderDetail d ON d.Id = s.OrderDetailId
INNER JOIN Sale.[Order] o ON o.Id = d.Sale_OrderId
LEFT JOIN SLS.Customer c ON c.Id = o.CustomerId
LEFT JOIN Gnr.Party p ON p.Id = c.PartyId
WHERE s.HtsId = 9918;
