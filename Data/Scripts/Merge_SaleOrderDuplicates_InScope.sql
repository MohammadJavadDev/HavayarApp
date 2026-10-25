/*
  Merge duplicate Sale.Order / Sale.OrderDetail rows (in-scope fiscal years 102+).

  Background (verified 2026-10-18):
    The AfterSales Phase1 import (2026-09-15 16:09:52, CreatedByName = 'sync-after-sales-phase1') inserted a second
    row for every HTS order/detail because the existing Rahkaran-synced rows had HtsId = 0 and the stamp step did not
    exist yet. Result: pairs of rows sharing one HamkaranId.
      A = lower Id, created by the Rahkaran sync (FiscalYearRef set, HtsId = 0)  -> KEPT
      B = higher Id, HTS import (HtsId <> 0, FiscalYearRef NULL)                  -> removed

  What this script does for each selected pair (all inside ONE transaction):
    1. Details: every B detail that has an A twin (same HamkaranId) -> all child rows are re-pointed to the A twin,
       B detail deleted, HTS fields (HtsId, HamkaranInvVchItmId, IsPackage, NotComplete) copied to the A twin.
       B details without an A twin are MOVED to the A order (nothing is lost).
    2. Order: order-level children (Cng.WorkReportConsumedPart, Sale.WebShopOrder, Crm.CustomerSatisfactionSurvey)
       re-pointed to A, B deleted, HTS fields copied to A (A values win; B only fills NULLs).
    3. Every step is logged in Sale._OrderDupMergeLog (KeptId / RemovedId) so the merge can be audited/reversed.
    4. Counts of every dependent table are compared before/after. Any mismatch THROWs and rolls everything back.

  Only "clean" pairs are touched: same OrderNumber/Year/CustomerId/StatusEnum on both rows, and every detail
  HamkaranId unique per side, no NULL HamkaranId, A details without HtsId, B details with HtsId.

  Usage:
    @Commit = 0  -> full dry run, everything is rolled back (default)
    @Commit = 1  -> commit
    @WithDep / @NoDep -> number of pairs with / without dependents on B to process
  Take a COPY_ONLY backup first. Run when Sale order sync job (JobSchedule Id 44) is not running.
*/
USE HavayarApp;
SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @Commit bit = 0;
DECLARE @WithDep int = 25, @NoDep int = 25;
DECLARE @Tag nvarchar(40) = N'merge-dup-order';

DROP TABLE IF EXISTS #pairs, #cand, #det0, #bad, #clean, #depHk, #pilot, #pd, #map, #ord;

DECLARE @msg nvarchar(400), @status nvarchar(20) = N'ERROR';
DECLARE @pairs int = 0, @detBefore int = 0, @twin int = 0, @bOnly int = 0, @detAfter int = 0;
DECLARE @repointed nvarchar(600) = N'';

BEGIN TRY
BEGIN TRAN;

IF OBJECT_ID('Sale._OrderDupMergeLog') IS NULL
    EXEC('CREATE TABLE Sale._OrderDupMergeLog (LogId bigint IDENTITY(1,1) PRIMARY KEY, RunAt datetime2 NOT NULL DEFAULT SYSDATETIME(), Tag nvarchar(40) NOT NULL, Kind nvarchar(80) NOT NULL, KeptId bigint NULL, RemovedId bigint NULL, Info nvarchar(400) NULL)');

/* ---------- 1. candidate pairs ---------- */
SELECT HamkaranId, MIN(Id) lo, MAX(Id) hi INTO #pairs FROM Sale.[Order] GROUP BY HamkaranId HAVING COUNT(*) = 2;

SELECT p.HamkaranId OrderHk, p.lo A, p.hi B INTO #cand
FROM #pairs p
JOIN Sale.[Order] a ON a.Id = p.lo
JOIN Sale.[Order] b ON b.Id = p.hi
WHERE a.FiscalYearRef IS NOT NULL AND ISNULL(a.HtsId, 0) = 0
  AND ISNULL(b.HtsId, 0) <> 0 AND b.FiscalYearRef IS NULL
  AND a.OrderNumber = b.OrderNumber
  AND ISNULL(a.[Year], '') = ISNULL(b.[Year], '')
  AND ISNULL(a.CustomerId, -1) = ISNULL(b.CustomerId, -1)
  AND ISNULL(a.StatusEnum, -1) = ISNULL(b.StatusEnum, -1);

SELECT d.Id, d.HamkaranId, d.HtsId, c.OrderHk, c.A, c.B,
       CASE WHEN d.Sale_OrderId = c.A THEN 'A' ELSE 'B' END side
INTO #det0
FROM Sale.OrderDetail d JOIN #cand c ON d.Sale_OrderId IN (c.A, c.B);
CREATE CLUSTERED INDEX ix0 ON #det0 (OrderHk, HamkaranId, side);

SELECT DISTINCT x.OrderHk INTO #bad
FROM #det0 x
WHERE x.HamkaranId IS NULL
   OR EXISTS (SELECT 1 FROM #det0 y WHERE y.OrderHk = x.OrderHk AND y.HamkaranId = x.HamkaranId AND y.side = x.side AND y.Id <> x.Id)
   OR (x.side = 'A' AND ISNULL(x.HtsId, 0) <> 0)
   OR (x.side = 'B' AND ISNULL(x.HtsId, 0) = 0);

SELECT c.* INTO #clean FROM #cand c WHERE NOT EXISTS (SELECT 1 FROM #bad b WHERE b.OrderHk = c.OrderHk);

/* pairs that have dependents on the B side (to make sure the pilot exercises re-pointing) */
SELECT DISTINCT x.OrderHk INTO #depHk FROM (
    SELECT d.OrderHk FROM #det0 d JOIN Sale.OrderDetailSerial s ON s.OrderDetailId = d.Id WHERE d.side = 'B'
    UNION SELECT d.OrderHk FROM #det0 d JOIN Sale.ServiceRequestDetail s ON s.OrderDetailId = d.Id WHERE d.side = 'B'
    UNION SELECT d.OrderHk FROM #det0 d JOIN Sale.ServiceRequestPart s ON s.OrderDetailId = d.Id WHERE d.side = 'B'
    UNION SELECT d.OrderHk FROM #det0 d JOIN Sale.OrderDetailProduct s ON s.OrderDetailId = d.Id WHERE d.side = 'B'
    UNION SELECT d.OrderHk FROM #det0 d JOIN Sale.OrderDetailProduct s ON s.ProductOrderDetailId = d.Id WHERE d.side = 'B'
    UNION SELECT d.OrderHk FROM #det0 d JOIN Crm.CustomerSatisfactionSurvey s ON s.SaleOrderDetailId = d.Id WHERE d.side = 'B'
    UNION SELECT c.OrderHk FROM #clean c JOIN Cng.WorkReportConsumedPart s ON s.SaleOrderId = c.B
    UNION SELECT c.OrderHk FROM #clean c JOIN Sale.WebShopOrder s ON s.SaleOrderId = c.B
    UNION SELECT c.OrderHk FROM #clean c JOIN Crm.CustomerSatisfactionSurvey s ON s.SaleOrderId = c.B
) x;

SELECT TOP (@WithDep) c.* INTO #pilot FROM #clean c WHERE c.OrderHk IN (SELECT OrderHk FROM #depHk) ORDER BY c.A DESC;
INSERT INTO #pilot SELECT TOP (@NoDep) c.* FROM #clean c WHERE c.OrderHk NOT IN (SELECT OrderHk FROM #depHk) ORDER BY c.A DESC;
SET @pairs = (SELECT COUNT(*) FROM #pilot);

/* ---------- 2. detail map ---------- */
SELECT d.Id, d.HamkaranId, d.HtsId, d.HamkaranInvVchItmId, d.IsPackage, d.NotComplete, p.OrderHk, p.A, p.B,
       CASE WHEN d.Sale_OrderId = p.A THEN 'A' ELSE 'B' END side
INTO #pd
FROM Sale.OrderDetail d JOIN #pilot p ON d.Sale_OrderId IN (p.A, p.B);

SELECT b.Id bId, a.Id aId, b.HtsId, b.HamkaranInvVchItmId, b.IsPackage, b.NotComplete, b.A, b.B
INTO #map
FROM #pd b
LEFT JOIN #pd a ON a.OrderHk = b.OrderHk AND a.HamkaranId = b.HamkaranId AND a.side = 'A'
WHERE b.side = 'B';

SET @detBefore = (SELECT COUNT(*) FROM #pd);
SET @twin = (SELECT COUNT(*) FROM #map WHERE aId IS NOT NULL);
SET @bOnly = (SELECT COUNT(*) FROM #map WHERE aId IS NULL);

/* ---------- 3. BEFORE counts of every dependent ---------- */
DECLARE @serB int = (SELECT COUNT(*) FROM Sale.OrderDetailSerial WHERE OrderDetailId IN (SELECT Id FROM #pd));
DECLARE @sdB  int = (SELECT COUNT(*) FROM Sale.ServiceRequestDetail WHERE OrderDetailId IN (SELECT Id FROM #pd));
DECLARE @spB  int = (SELECT COUNT(*) FROM Sale.ServiceRequestPart WHERE OrderDetailId IN (SELECT Id FROM #pd));
DECLARE @pr1B int = (SELECT COUNT(*) FROM Sale.OrderDetailProduct WHERE OrderDetailId IN (SELECT Id FROM #pd));
DECLARE @pr2B int = (SELECT COUNT(*) FROM Sale.OrderDetailProduct WHERE ProductOrderDetailId IN (SELECT Id FROM #pd));
DECLARE @svdB int = (SELECT COUNT(*) FROM Crm.CustomerSatisfactionSurvey WHERE SaleOrderDetailId IN (SELECT Id FROM #pd));
DECLARE @cngB int = (SELECT COUNT(*) FROM Cng.WorkReportConsumedPart WHERE SaleOrderId IN (SELECT A FROM #pilot UNION SELECT B FROM #pilot));
DECLARE @webB int = (SELECT COUNT(*) FROM Sale.WebShopOrder WHERE SaleOrderId IN (SELECT A FROM #pilot UNION SELECT B FROM #pilot));
DECLARE @svoB int = (SELECT COUNT(*) FROM Crm.CustomerSatisfactionSurvey WHERE SaleOrderId IN (SELECT A FROM #pilot UNION SELECT B FROM #pilot));
DECLARE @n int;

/* ---------- 4. re-point detail-level children (B detail -> A twin) ---------- */
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

/* audit rows for details */
INSERT INTO Sale._OrderDupMergeLog (Tag, Kind, KeptId, RemovedId, Info)
SELECT @Tag, N'OrderDetail merged into twin', m.aId, m.bId, N'HtsId=' + CAST(m.HtsId AS nvarchar(30)) FROM #map m WHERE m.aId IS NOT NULL;
INSERT INTO Sale._OrderDupMergeLog (Tag, Kind, KeptId, RemovedId, Info)
SELECT @Tag, N'OrderDetail moved to A order', m.bId, NULL, N'order ' + CAST(m.B AS nvarchar(30)) + N' -> ' + CAST(m.A AS nvarchar(30)) FROM #map m WHERE m.aId IS NULL;

/* B-only details: move to the A order (nothing lost) */
UPDATE d SET d.Sale_OrderId = m.A, d.ModifiedByName = @Tag, d.ModifiedDateMiladiDateTime = SYSDATETIME()
FROM Sale.OrderDetail d JOIN #map m ON d.Id = m.bId WHERE m.aId IS NULL;

/* delete twin B details (FK errors here if any dependent was missed -> whole transaction aborts) */
DELETE d FROM Sale.OrderDetail d JOIN #map m ON m.bId = d.Id WHERE m.aId IS NOT NULL;

/* copy HTS link to A twins (B no longer holds the unique HtsId) */
UPDATE a SET a.HtsId = m.HtsId,
             a.HamkaranInvVchItmId = COALESCE(m.HamkaranInvVchItmId, a.HamkaranInvVchItmId),
             a.IsPackage = COALESCE(m.IsPackage, a.IsPackage),
             a.NotComplete = COALESCE(m.NotComplete, a.NotComplete),
             a.ModifiedByName = @Tag, a.ModifiedDateMiladiDateTime = SYSDATETIME()
FROM Sale.OrderDetail a JOIN #map m ON a.Id = m.aId WHERE m.aId IS NOT NULL;

/* ---------- 5. orders ---------- */
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

UPDATE a SET a.HtsId = o.HtsId, a.HtsVchNo = o.HtsVchNo, a.HtsStockId = o.HtsStockId,
             a.HtsBaseVoucherTypeId = o.HtsBaseVoucherTypeId, a.HtsContractVchHeaderId = o.HtsContractVchHeaderId,
             a.ContractNumber = COALESCE(a.ContractNumber, o.ContractNumber),
             a.ConfirmerUserId = COALESCE(a.ConfirmerUserId, o.ConfirmerUserId),
             a.OperatorUserId = COALESCE(a.OperatorUserId, o.OperatorUserId),
             a.ModifiedByName = @Tag, a.ModifiedDateMiladiDateTime = SYSDATETIME()
FROM Sale.[Order] a JOIN #ord o ON a.Id = o.A;

/* ---------- 6. verification (any mismatch -> THROW -> rollback) ---------- */
IF EXISTS (SELECT 1 FROM Sale.[Order] o JOIN #pilot p ON o.Id = p.B) THROW 51002, 'A B order still exists.', 1;
IF EXISTS (SELECT 1 FROM Sale.[Order] o JOIN #pilot p ON o.Id = p.A WHERE ISNULL(o.HtsId, 0) = 0) THROW 51003, 'An A order has no HtsId.', 1;
IF EXISTS (SELECT 1 FROM Sale.[Order] o WHERE o.HamkaranId IN (SELECT OrderHk FROM #pilot) GROUP BY o.HamkaranId HAVING COUNT(*) <> 1) THROW 51004, 'HamkaranId still duplicated.', 1;
IF EXISTS (SELECT 1 FROM #map m JOIN Sale.OrderDetail a ON a.Id = m.aId WHERE m.aId IS NOT NULL AND a.HtsId <> m.HtsId) THROW 51005, 'Twin HtsId not copied.', 1;
IF EXISTS (SELECT 1 FROM Sale.OrderDetail d WHERE d.HamkaranId IN (SELECT HamkaranId FROM #pd) AND d.Sale_OrderId IN (SELECT A FROM #pilot) GROUP BY d.HamkaranId, d.Sale_OrderId HAVING COUNT(*) > 1) THROW 51006, 'Duplicate details inside an A order.', 1;

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

DROP TABLE IF EXISTS #pairs, #cand, #det0, #bad, #clean, #depHk, #pilot, #pd, #map, #ord;

SELECT @status status, @msg message, @pairs pairs, @detBefore details_before, @twin details_merged_into_twin,
       @bOnly details_moved_to_A, @detAfter details_after, @repointed repointed_children;
