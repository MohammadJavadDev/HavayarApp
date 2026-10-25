/*
  Merge the REMAINING in-scope (fiscal year 102+) duplicate Sale.Order / Sale.OrderDetail pairs.
  Follow-up of Merge_SaleOrderDuplicates_InScope.sql (which handled the "clean" pairs).

  Differences from the first script (decided with the data owner on 2026-10-18):
    1. CustomerId / StatusEnum may differ between A and B. A (Rahkaran-synced) WINS. Verified against Rahkaran (Erps.SLS3.[Order]):
       in all 39 customer and all 85 status differences A equals Rahkaran and B never does. B only fills NULLs.
    2. A B detail is merged into an A twin ONLY when its HamkaranId is unique on both sides. Every other B detail
       (e.g. HTS split one Rahkaran item into several lines) is MOVED to the A order. Nothing is deleted.
    3. The "duplicate details inside an A order" check is dropped because moved lines legitimately share a HamkaranId.

  Still excluded (left for manual review): pairs with a NULL detail HamkaranId, an A detail that already has an HtsId,
  or a B detail without HtsId.

  Same safety as the first script: one transaction, counts of every dependent table before/after, any mismatch
  THROWs and rolls back, every action logged in Sale._OrderDupMergeLog.
  @Commit = 0 (default) is a full dry run that rolls back.
*/
USE HavayarApp;
SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @Commit bit = 0;
DECLARE @Max int = 1000;
DECLARE @Tag nvarchar(40) = N'merge-dup-order-2';

DROP TABLE IF EXISTS #pairs, #cand, #det0, #bad, #pilot, #pd, #map, #ord;

DECLARE @msg nvarchar(400), @status nvarchar(30) = N'ERROR';
DECLARE @pairs int = 0, @detBefore int = 0, @twin int = 0, @bOnly int = 0, @detAfter int = 0, @repointed nvarchar(600) = N'';
DECLARE @serB int, @sdB int, @spB int, @pr1B int, @pr2B int, @svdB int, @cngB int, @webB int, @svoB int, @n int;

BEGIN TRY
BEGIN TRAN;

SELECT HamkaranId, MIN(Id) lo, MAX(Id) hi INTO #pairs FROM Sale.[Order] GROUP BY HamkaranId HAVING COUNT(*) = 2;

SELECT p.HamkaranId OrderHk, p.lo A, p.hi B INTO #cand
FROM #pairs p
JOIN Sale.[Order] a ON a.Id = p.lo
JOIN Sale.[Order] b ON b.Id = p.hi
WHERE a.FiscalYearRef IS NOT NULL AND ISNULL(a.HtsId, 0) = 0
  AND ISNULL(b.HtsId, 0) <> 0 AND b.FiscalYearRef IS NULL
  AND a.OrderNumber = b.OrderNumber
  AND ISNULL(a.[Year], '') = ISNULL(b.[Year], '');

SELECT d.Id, d.HamkaranId, d.HtsId, c.OrderHk, c.A, c.B, CASE WHEN d.Sale_OrderId = c.A THEN 'A' ELSE 'B' END side
INTO #det0 FROM Sale.OrderDetail d JOIN #cand c ON d.Sale_OrderId IN (c.A, c.B);

SELECT DISTINCT x.OrderHk INTO #bad FROM #det0 x
WHERE x.HamkaranId IS NULL
   OR (x.side = 'A' AND ISNULL(x.HtsId, 0) <> 0)
   OR (x.side = 'B' AND ISNULL(x.HtsId, 0) = 0);

SELECT TOP (@Max) c.* INTO #pilot FROM #cand c WHERE NOT EXISTS (SELECT 1 FROM #bad b WHERE b.OrderHk = c.OrderHk) ORDER BY c.A DESC;
SET @pairs = (SELECT COUNT(*) FROM #pilot);

SELECT d.Id, d.HamkaranId, d.HtsId, d.HamkaranInvVchItmId, d.IsPackage, d.NotComplete, p.OrderHk, p.A, p.B,
       CASE WHEN d.Sale_OrderId = p.A THEN 'A' ELSE 'B' END side
INTO #pd FROM Sale.OrderDetail d JOIN #pilot p ON d.Sale_OrderId IN (p.A, p.B);
CREATE CLUSTERED INDEX ixpd ON #pd (OrderHk, HamkaranId, side);

/* twin only when unique on both sides; everything else on B is moved */
SELECT b.Id bId, a.Id aId, b.HtsId, b.HamkaranInvVchItmId, b.IsPackage, b.NotComplete, b.A, b.B
INTO #map
FROM #pd b
LEFT JOIN #pd a ON a.OrderHk = b.OrderHk AND a.HamkaranId = b.HamkaranId AND a.side = 'A'
   AND (SELECT COUNT(*) FROM #pd x WHERE x.OrderHk = b.OrderHk AND x.HamkaranId = b.HamkaranId AND x.side = 'A') = 1
   AND (SELECT COUNT(*) FROM #pd x WHERE x.OrderHk = b.OrderHk AND x.HamkaranId = b.HamkaranId AND x.side = 'B') = 1
WHERE b.side = 'B';

SET @detBefore = (SELECT COUNT(*) FROM #pd);
SET @twin = (SELECT COUNT(*) FROM #map WHERE aId IS NOT NULL);
SET @bOnly = (SELECT COUNT(*) FROM #map WHERE aId IS NULL);

SET @serB = (SELECT COUNT(*) FROM Sale.OrderDetailSerial WHERE OrderDetailId IN (SELECT Id FROM #pd));
SET @sdB = (SELECT COUNT(*) FROM Sale.ServiceRequestDetail WHERE OrderDetailId IN (SELECT Id FROM #pd));
SET @spB = (SELECT COUNT(*) FROM Sale.ServiceRequestPart WHERE OrderDetailId IN (SELECT Id FROM #pd));
SET @pr1B = (SELECT COUNT(*) FROM Sale.OrderDetailProduct WHERE OrderDetailId IN (SELECT Id FROM #pd));
SET @pr2B = (SELECT COUNT(*) FROM Sale.OrderDetailProduct WHERE ProductOrderDetailId IN (SELECT Id FROM #pd));
SET @svdB = (SELECT COUNT(*) FROM Crm.CustomerSatisfactionSurvey WHERE SaleOrderDetailId IN (SELECT Id FROM #pd));
SET @cngB = (SELECT COUNT(*) FROM Cng.WorkReportConsumedPart WHERE SaleOrderId IN (SELECT A FROM #pilot UNION SELECT B FROM #pilot));
SET @webB = (SELECT COUNT(*) FROM Sale.WebShopOrder WHERE SaleOrderId IN (SELECT A FROM #pilot UNION SELECT B FROM #pilot));
SET @svoB = (SELECT COUNT(*) FROM Crm.CustomerSatisfactionSurvey WHERE SaleOrderId IN (SELECT A FROM #pilot UNION SELECT B FROM #pilot));

UPDATE s SET s.OrderDetailId = m.aId FROM Sale.OrderDetailSerial s JOIN #map m ON s.OrderDetailId = m.bId WHERE m.aId IS NOT NULL;
SET @n = @@ROWCOUNT; SET @repointed += N'serial=' + CAST(@n AS nvarchar(20)) + N' ';
UPDATE s SET s.OrderDetailId = m.aId FROM Sale.ServiceRequestDetail s JOIN #map m ON s.OrderDetailId = m.bId WHERE m.aId IS NOT NULL;
SET @n = @@ROWCOUNT; SET @repointed += N'srvDetail=' + CAST(@n AS nvarchar(20)) + N' ';
UPDATE s SET s.OrderDetailId = m.aId FROM Sale.ServiceRequestPart s JOIN #map m ON s.OrderDetailId = m.bId WHERE m.aId IS NOT NULL;
SET @n = @@ROWCOUNT; SET @repointed += N'srvPart=' + CAST(@n AS nvarchar(20)) + N' ';
UPDATE s SET s.OrderDetailId = m.aId FROM Sale.OrderDetailProduct s JOIN #map m ON s.OrderDetailId = m.bId WHERE m.aId IS NOT NULL;
SET @n = @@ROWCOUNT; SET @repointed += N'product1=' + CAST(@n AS nvarchar(20)) + N' ';
UPDATE s SET s.ProductOrderDetailId = m.aId FROM Sale.OrderDetailProduct s JOIN #map m ON s.ProductOrderDetailId = m.bId WHERE m.aId IS NOT NULL;
SET @n = @@ROWCOUNT; SET @repointed += N'product2=' + CAST(@n AS nvarchar(20)) + N' ';
UPDATE s SET s.SaleOrderDetailId = m.aId FROM Crm.CustomerSatisfactionSurvey s JOIN #map m ON s.SaleOrderDetailId = m.bId WHERE m.aId IS NOT NULL;
SET @n = @@ROWCOUNT; SET @repointed += N'surveyDetail=' + CAST(@n AS nvarchar(20)) + N' ';

INSERT INTO Sale._OrderDupMergeLog (Tag, Kind, KeptId, RemovedId, Info)
SELECT @Tag, N'OrderDetail merged into twin', m.aId, m.bId, N'HtsId=' + CAST(m.HtsId AS nvarchar(30)) FROM #map m WHERE m.aId IS NOT NULL;
INSERT INTO Sale._OrderDupMergeLog (Tag, Kind, KeptId, RemovedId, Info)
SELECT @Tag, N'OrderDetail moved to A order', m.bId, NULL, N'order ' + CAST(m.B AS nvarchar(30)) + N' -> ' + CAST(m.A AS nvarchar(30)) FROM #map m WHERE m.aId IS NULL;

UPDATE d SET d.Sale_OrderId = m.A, d.ModifiedByName = @Tag, d.ModifiedDateMiladiDateTime = SYSDATETIME()
FROM Sale.OrderDetail d JOIN #map m ON d.Id = m.bId WHERE m.aId IS NULL;

DELETE d FROM Sale.OrderDetail d JOIN #map m ON m.bId = d.Id WHERE m.aId IS NOT NULL;

UPDATE a SET a.HtsId = m.HtsId,
             a.HamkaranInvVchItmId = COALESCE(m.HamkaranInvVchItmId, a.HamkaranInvVchItmId),
             a.IsPackage = COALESCE(m.IsPackage, a.IsPackage),
             a.NotComplete = COALESCE(m.NotComplete, a.NotComplete),
             a.ModifiedByName = @Tag, a.ModifiedDateMiladiDateTime = SYSDATETIME()
FROM Sale.OrderDetail a JOIN #map m ON a.Id = m.aId WHERE m.aId IS NOT NULL;

SELECT b.Id bId, p.A, b.HtsId, b.HtsVchNo, b.HtsStockId, b.HtsBaseVoucherTypeId, b.HtsContractVchHeaderId,
       b.ContractNumber, b.ConfirmerUserId, b.OperatorUserId
INTO #ord FROM #pilot p JOIN Sale.[Order] b ON b.Id = p.B;

UPDATE s SET s.SaleOrderId = o.A FROM Cng.WorkReportConsumedPart s JOIN #ord o ON s.SaleOrderId = o.bId;
SET @n = @@ROWCOUNT; SET @repointed += N'cng=' + CAST(@n AS nvarchar(20)) + N' ';
UPDATE s SET s.SaleOrderId = o.A FROM Sale.WebShopOrder s JOIN #ord o ON s.SaleOrderId = o.bId;
SET @n = @@ROWCOUNT; SET @repointed += N'webshop=' + CAST(@n AS nvarchar(20)) + N' ';
UPDATE s SET s.SaleOrderId = o.A FROM Crm.CustomerSatisfactionSurvey s JOIN #ord o ON s.SaleOrderId = o.bId;
SET @n = @@ROWCOUNT; SET @repointed += N'surveyOrder=' + CAST(@n AS nvarchar(20)) + N' ';

IF EXISTS (SELECT 1 FROM Sale.OrderDetail d JOIN #ord o ON d.Sale_OrderId = o.bId)
    THROW 51001, 'Details still attached to a B order; aborting.', 1;

INSERT INTO Sale._OrderDupMergeLog (Tag, Kind, KeptId, RemovedId, Info)
SELECT @Tag, N'Order merged', o.A, o.bId, N'HtsId=' + CAST(o.HtsId AS nvarchar(30)) FROM #ord o;

DELETE b FROM Sale.[Order] b JOIN #ord o ON o.bId = b.Id;

/* A wins: only HTS-link fields are copied; B only fills NULLs */
UPDATE a SET a.HtsId = o.HtsId, a.HtsVchNo = o.HtsVchNo, a.HtsStockId = o.HtsStockId,
             a.HtsBaseVoucherTypeId = o.HtsBaseVoucherTypeId, a.HtsContractVchHeaderId = o.HtsContractVchHeaderId,
             a.ContractNumber = COALESCE(a.ContractNumber, o.ContractNumber),
             a.ConfirmerUserId = COALESCE(a.ConfirmerUserId, o.ConfirmerUserId),
             a.OperatorUserId = COALESCE(a.OperatorUserId, o.OperatorUserId),
             a.ModifiedByName = @Tag, a.ModifiedDateMiladiDateTime = SYSDATETIME()
FROM Sale.[Order] a JOIN #ord o ON a.Id = o.A;

IF EXISTS (SELECT 1 FROM Sale.[Order] o JOIN #pilot p ON o.Id = p.B) THROW 51002, 'A B order still exists.', 1;
IF EXISTS (SELECT 1 FROM Sale.[Order] o JOIN #pilot p ON o.Id = p.A WHERE ISNULL(o.HtsId, 0) = 0) THROW 51003, 'An A order has no HtsId.', 1;
IF EXISTS (SELECT 1 FROM Sale.[Order] o WHERE o.HamkaranId IN (SELECT OrderHk FROM #pilot) GROUP BY o.HamkaranId HAVING COUNT(*) <> 1) THROW 51004, 'HamkaranId still duplicated.', 1;
IF EXISTS (SELECT 1 FROM #map m JOIN Sale.OrderDetail a ON a.Id = m.aId WHERE m.aId IS NOT NULL AND a.HtsId <> m.HtsId) THROW 51005, 'Twin HtsId not copied.', 1;

SET @detAfter = (SELECT COUNT(*) FROM Sale.OrderDetail WHERE Sale_OrderId IN (SELECT A FROM #pilot));
IF @detAfter <> @detBefore - @twin THROW 51007, 'Detail count not conserved.', 1;

DECLARE @ids TABLE (Id bigint PRIMARY KEY);
INSERT INTO @ids SELECT Id FROM Sale.OrderDetail WHERE Sale_OrderId IN (SELECT A FROM #pilot);
IF (SELECT COUNT(*) FROM Sale.OrderDetailSerial WHERE OrderDetailId IN (SELECT Id FROM @ids)) <> @serB THROW 51010, 'Serial count not conserved.', 1;
IF (SELECT COUNT(*) FROM Sale.ServiceRequestDetail WHERE OrderDetailId IN (SELECT Id FROM @ids)) <> @sdB THROW 51011, 'ServiceRequestDetail count not conserved.', 1;
IF (SELECT COUNT(*) FROM Sale.ServiceRequestPart WHERE OrderDetailId IN (SELECT Id FROM @ids)) <> @spB THROW 51012, 'ServiceRequestPart count not conserved.', 1;
IF (SELECT COUNT(*) FROM Sale.OrderDetailProduct WHERE OrderDetailId IN (SELECT Id FROM @ids)) <> @pr1B THROW 51013, 'OrderDetailProduct(1) count not conserved.', 1;
IF (SELECT COUNT(*) FROM Sale.OrderDetailProduct WHERE ProductOrderDetailId IN (SELECT Id FROM @ids)) <> @pr2B THROW 51014, 'OrderDetailProduct(2) count not conserved.', 1;
IF (SELECT COUNT(*) FROM Crm.CustomerSatisfactionSurvey WHERE SaleOrderDetailId IN (SELECT Id FROM @ids)) <> @svdB THROW 51015, 'Survey(detail) count not conserved.', 1;
IF (SELECT COUNT(*) FROM Cng.WorkReportConsumedPart WHERE SaleOrderId IN (SELECT A FROM #pilot)) <> @cngB THROW 51016, 'Cng consumed part count not conserved.', 1;
IF (SELECT COUNT(*) FROM Sale.WebShopOrder WHERE SaleOrderId IN (SELECT A FROM #pilot)) <> @webB THROW 51017, 'WebShopOrder count not conserved.', 1;
IF (SELECT COUNT(*) FROM Crm.CustomerSatisfactionSurvey WHERE SaleOrderId IN (SELECT A FROM #pilot)) <> @svoB THROW 51018, 'Survey(order) count not conserved.', 1;

SET @status = CASE WHEN @Commit = 1 THEN N'COMMITTED' ELSE N'DRYRUN_ROLLED_BACK' END;
SET @msg = N'all checks passed';
IF @Commit = 1 COMMIT TRAN; ELSE ROLLBACK TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    SET @status = N'ROLLED_BACK_ON_ERROR';
    SET @msg = ERROR_MESSAGE();
END CATCH;

DROP TABLE IF EXISTS #pairs, #cand, #det0, #bad, #pilot, #pd, #map, #ord;
SELECT @status status, @msg message, @pairs pairs, @detBefore details_before, @twin details_merged_into_twin,
       @bOnly details_moved_to_A, @detAfter details_after, @repointed repointed_children;
