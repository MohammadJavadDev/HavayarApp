/*
  Old-year (fiscal year <= 101) duplicate Sale.Order cleanup: KEEP the HTS row (B), DELETE the legacy row (A).

  Background (verified 2026-10-18, see Merge_SaleOrderDuplicates_InScope.sql for the origin of the pairs):
    A = lower Id, original HTS-migration copy (Id = HTS Order_ID, HtsId = 0, FiscalYearRef NULL).
        Its CustomerId / PartId hold RAW HTS ids, so they point at unrelated customers/parts (verified against TMS:
        in 500 random pairs B's customer equals HTS in 97.6% and A's in 0%).
    B = higher Id, HTS import with correctly mapped foreign keys and every dependent (service requests, parts,
        consumed parts, web shop, serials).
    A has NO dependents except serials, and those serials are duplicates (same text) of B's serials.

  Decision (data owner, 2026-10-18): A is removed. The only column A has and B lacks is Order.PartyId
  ("company"), which is unrelated to the order's customer. It is NOT copied; the full A rows are archived first.

  Per batch (ONE transaction, all-or-nothing):
    1. Select "fully safe" pairs only: same OrderNumber/Year/Status/VoucherType/Branch; no dependents on the A order;
       EVERY A detail has an exact 1:1 twin (unique HamkaranId on both sides) with equal Qty/prices/Seq;
       every A serial is a duplicate (same text, counted) of a serial on the B twin and has no children.
    2. Archive A order / A details / A serials as JSON in Sale._OrderDupArchive (restorable with OPENJSON).
    3. Delete A serials, A details, A orders. B rows are NEVER modified.
    4. Verify: A gone, every B order/detail/serial still present and unchanged in count, serial count dropped by
       exactly the number deleted. Any mismatch THROWs and rolls the batch back.

  @Commit = 0 (default) is a full dry run that rolls back.
*/
USE HavayarApp;
SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @Commit bit = 0;
DECLARE @Max int = 50;
DECLARE @Tag nvarchar(40) = N'del-old-A';

DROP TABLE IF EXISTS #old, #d, #tw, #bad, #pilot, #pd, #map, #sdel;

DECLARE @msg nvarchar(400), @status nvarchar(30) = N'ERROR';
DECLARE @pairs int = 0, @detDel int = 0, @serDel int = 0, @bOrders int, @bDetails int, @bSerials int, @serTotalBefore int, @n int;

BEGIN TRY
BEGIN TRAN;

IF OBJECT_ID('Sale._OrderDupArchive') IS NULL
    EXEC('CREATE TABLE Sale._OrderDupArchive (ArchiveId bigint IDENTITY(1,1) PRIMARY KEY, ArchivedAt datetime2 NOT NULL DEFAULT SYSDATETIME(), Tag nvarchar(40) NOT NULL, TableName nvarchar(60) NOT NULL, RowId bigint NOT NULL, KeptId bigint NULL, RowJson nvarchar(max) NOT NULL)');

/* ---- 1. eligible pairs ---- */
SELECT g.HamkaranId OrderHk, g.lo A, g.hi B INTO #old
FROM (SELECT HamkaranId, MIN(Id) lo, MAX(Id) hi, COUNT(*) n FROM Sale.[Order] GROUP BY HamkaranId HAVING COUNT(*) = 2) g
JOIN Sale.[Order] a ON a.Id = g.lo
JOIN Sale.[Order] b ON b.Id = g.hi
WHERE a.FiscalYearRef IS NULL AND ISNULL(a.HtsId, 0) = 0 AND ISNULL(b.HtsId, 0) <> 0
  AND a.OrderNumber = b.OrderNumber AND ISNULL(a.[Year], '') = ISNULL(b.[Year], '')
  AND ISNULL(a.StatusEnum, -1) = ISNULL(b.StatusEnum, -1)
  AND ISNULL(a.VoucherTypeStatus, -1) = ISNULL(b.VoucherTypeStatus, -1)
  AND ISNULL(a.BranchId, -1) = ISNULL(b.BranchId, -1)
  AND NOT EXISTS (SELECT 1 FROM Cng.WorkReportConsumedPart c WHERE c.SaleOrderId = a.Id)
  AND NOT EXISTS (SELECT 1 FROM Sale.WebShopOrder c WHERE c.SaleOrderId = a.Id)
  AND NOT EXISTS (SELECT 1 FROM Crm.CustomerSatisfactionSurvey c WHERE c.SaleOrderId = a.Id)
  AND (a.ContractId IS NULL OR b.ContractId IS NOT NULL) AND (a.ContractNumber IS NULL OR b.ContractNumber IS NOT NULL)
  AND (a.FactorMiladiDate IS NULL OR b.FactorMiladiDate IS NOT NULL) AND (a.ConfirmerUserId IS NULL OR b.ConfirmerUserId IS NOT NULL)
  AND (a.OperatorUserId IS NULL OR b.OperatorUserId IS NOT NULL) AND (a.VchDateMiladiDate IS NULL OR b.VchDateMiladiDate IS NOT NULL);

SELECT d.Id, d.HamkaranId, d.HtsId, d.Qty, d.UnitPrice, d.Price, d.IncPrice, d.DecPrice, d.PayMethod, d.PriceTemp, d.QtyTemp, d.Seq,
       d.FactorMiladiDate, d.Description, d.BaseVchItmRef, d.Prefactor_VchNo, o.OrderHk, o.A, o.B,
       CASE WHEN d.Sale_OrderId = o.A THEN 'A' ELSE 'B' END side
INTO #d FROM Sale.OrderDetail d JOIN #old o ON d.Sale_OrderId IN (o.A, o.B);
CREATE CLUSTERED INDEX ix ON #d (OrderHk, HamkaranId, side);

SELECT da.Id aId, db.Id bId, da.OrderHk INTO #tw
FROM #d da JOIN #d db ON db.OrderHk = da.OrderHk AND db.HamkaranId = da.HamkaranId AND db.side = 'B'
WHERE da.side = 'A'
  AND (SELECT COUNT(*) FROM #d x WHERE x.OrderHk = da.OrderHk AND x.HamkaranId = da.HamkaranId AND x.side = 'A') = 1
  AND (SELECT COUNT(*) FROM #d x WHERE x.OrderHk = da.OrderHk AND x.HamkaranId = da.HamkaranId AND x.side = 'B') = 1;
CREATE CLUSTERED INDEX ix2 ON #tw (aId);

SELECT x.OrderHk INTO #bad FROM #d x
WHERE x.side = 'A' AND (x.HamkaranId IS NULL OR ISNULL(x.HtsId, 0) <> 0 OR x.Id NOT IN (SELECT aId FROM #tw))
UNION
SELECT t.OrderHk FROM #tw t JOIN #d a ON a.Id = t.aId JOIN #d b ON b.Id = t.bId
WHERE ISNULL(b.HtsId, 0) = 0
   OR ISNULL(a.Qty, -1) <> ISNULL(b.Qty, -1) OR ISNULL(a.UnitPrice, -1) <> ISNULL(b.UnitPrice, -1) OR ISNULL(a.Price, -1) <> ISNULL(b.Price, -1)
   OR ISNULL(a.IncPrice, -1) <> ISNULL(b.IncPrice, -1) OR ISNULL(a.DecPrice, -1) <> ISNULL(b.DecPrice, -1) OR ISNULL(a.PayMethod, -1) <> ISNULL(b.PayMethod, -1)
   OR ISNULL(a.PriceTemp, -1) <> ISNULL(b.PriceTemp, -1) OR ISNULL(a.QtyTemp, -1) <> ISNULL(b.QtyTemp, -1) OR ISNULL(a.Seq, -1) <> ISNULL(b.Seq, -1)
   OR (a.FactorMiladiDate IS NOT NULL AND (b.FactorMiladiDate IS NULL OR a.FactorMiladiDate <> b.FactorMiladiDate))
   OR (a.Description IS NOT NULL AND ISNULL(b.Description, '') <> a.Description)
   OR (a.BaseVchItmRef IS NOT NULL AND ISNULL(b.BaseVchItmRef, -1) <> a.BaseVchItmRef)
   OR (a.Prefactor_VchNo IS NOT NULL AND ISNULL(b.Prefactor_VchNo, -1) <> a.Prefactor_VchNo)
UNION
SELECT t.OrderHk FROM #tw t
WHERE EXISTS (SELECT 1 FROM Sale.ServiceRequestDetail r WHERE r.OrderDetailId = t.aId)
   OR EXISTS (SELECT 1 FROM Sale.ServiceRequestPart r WHERE r.OrderDetailId = t.aId)
   OR EXISTS (SELECT 1 FROM Sale.OrderDetailProduct r WHERE r.OrderDetailId = t.aId OR r.ProductOrderDetailId = t.aId)
   OR EXISTS (SELECT 1 FROM Crm.CustomerSatisfactionSurvey r WHERE r.SaleOrderDetailId = t.aId)
UNION
SELECT t.OrderHk FROM #tw t JOIN Sale.OrderDetailSerial s ON s.OrderDetailId = t.aId
WHERE ISNULL(s.HtsId, 0) <> 0
   OR NOT EXISTS (SELECT 1 FROM Sale.OrderDetailSerial s2 WHERE s2.OrderDetailId = t.bId AND ISNULL(s2.Serial, '') = ISNULL(s.Serial, ''))
   OR (SELECT COUNT(*) FROM Sale.OrderDetailSerial x WHERE x.OrderDetailId = t.aId AND ISNULL(x.Serial, '') = ISNULL(s.Serial, ''))
    > (SELECT COUNT(*) FROM Sale.OrderDetailSerial y WHERE y.OrderDetailId = t.bId AND ISNULL(y.Serial, '') = ISNULL(s.Serial, ''))
   OR EXISTS (SELECT 1 FROM Sale.EquipmentMonitoring e WHERE e.OrderDetailSerialId = s.Id)
   OR EXISTS (SELECT 1 FROM Sale.OrderDetailProduct p WHERE p.OrderDetailSerialId = s.Id)
   OR EXISTS (SELECT 1 FROM Sale.OrderDetailSerialAttachment a WHERE a.OrderDetailSerialId = s.Id)
   OR EXISTS (SELECT 1 FROM Sale.ServiceRequestDetail r WHERE r.OrderDetailSerialId = s.Id)
   OR EXISTS (SELECT 1 FROM Rpr.RepairRequest r WHERE r.MotherProductId = s.Id);

SELECT TOP (@Max) o.* INTO #pilot FROM #old o
WHERE NOT EXISTS (SELECT 1 FROM #bad b WHERE b.OrderHk = o.OrderHk)
ORDER BY NEWID();
SET @pairs = (SELECT COUNT(*) FROM #pilot);

SELECT t.aId, t.bId INTO #map FROM #tw t JOIN #pilot p ON p.OrderHk = t.OrderHk;
SELECT s.Id INTO #sdel FROM Sale.OrderDetailSerial s JOIN #map m ON m.aId = s.OrderDetailId;

SET @detDel = (SELECT COUNT(*) FROM #map);
SET @serDel = (SELECT COUNT(*) FROM #sdel);
IF (SELECT COUNT(*) FROM Sale.OrderDetail d JOIN #pilot p ON d.Sale_OrderId = p.A) <> @detDel THROW 51020, 'Not every A detail has a twin.', 1;

SET @bOrders = (SELECT COUNT(*) FROM Sale.[Order] o JOIN #pilot p ON o.Id = p.B);
SET @bDetails = (SELECT COUNT(*) FROM Sale.OrderDetail d JOIN #pilot p ON d.Sale_OrderId = p.B);
SET @bSerials = (SELECT COUNT(*) FROM Sale.OrderDetailSerial s JOIN Sale.OrderDetail d ON d.Id = s.OrderDetailId JOIN #pilot p ON d.Sale_OrderId = p.B);
SET @serTotalBefore = (SELECT COUNT(*) FROM Sale.OrderDetailSerial);

/* ---- 2. archive (full rows as JSON, restorable) ---- */
INSERT INTO Sale._OrderDupArchive (Tag, TableName, RowId, KeptId, RowJson)
SELECT @Tag, N'Sale.Order', o.Id, p.B, (SELECT o.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES)
FROM Sale.[Order] o JOIN #pilot p ON o.Id = p.A;
INSERT INTO Sale._OrderDupArchive (Tag, TableName, RowId, KeptId, RowJson)
SELECT @Tag, N'Sale.OrderDetail', d.Id, m.bId, (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES)
FROM Sale.OrderDetail d JOIN #map m ON m.aId = d.Id;
INSERT INTO Sale._OrderDupArchive (Tag, TableName, RowId, KeptId, RowJson)
SELECT @Tag, N'Sale.OrderDetailSerial', s.Id, NULL, (SELECT s.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES)
FROM Sale.OrderDetailSerial s JOIN #sdel x ON x.Id = s.Id;

IF (SELECT COUNT(*) FROM Sale._OrderDupArchive WHERE Tag = @Tag AND TableName = N'Sale.Order' AND RowId IN (SELECT A FROM #pilot)) <> @pairs THROW 51021, 'Order archive incomplete.', 1;
IF (SELECT COUNT(*) FROM Sale._OrderDupArchive WHERE Tag = @Tag AND TableName = N'Sale.OrderDetail' AND RowId IN (SELECT aId FROM #map)) <> @detDel THROW 51022, 'Detail archive incomplete.', 1;
IF (SELECT COUNT(*) FROM Sale._OrderDupArchive WHERE Tag = @Tag AND TableName = N'Sale.OrderDetailSerial' AND RowId IN (SELECT Id FROM #sdel)) <> @serDel THROW 51023, 'Serial archive incomplete.', 1;

INSERT INTO Sale._OrderDupMergeLog (Tag, Kind, KeptId, RemovedId, Info)
SELECT @Tag, N'Old A order deleted', p.B, p.A, N'HamkaranId=' + CAST(p.OrderHk AS nvarchar(30)) FROM #pilot p;

/* ---- 3. delete A (serials -> details -> orders) ---- */
DELETE s FROM Sale.OrderDetailSerial s JOIN #sdel x ON x.Id = s.Id;
DELETE d FROM Sale.OrderDetail d JOIN #map m ON m.aId = d.Id;
DELETE o FROM Sale.[Order] o JOIN #pilot p ON o.Id = p.A;

/* ---- 4. verification ---- */
IF EXISTS (SELECT 1 FROM Sale.[Order] o JOIN #pilot p ON o.Id = p.A) THROW 51030, 'An A order still exists.', 1;
IF (SELECT COUNT(*) FROM Sale.[Order] o JOIN #pilot p ON o.Id = p.B) <> @bOrders THROW 51031, 'B order count changed.', 1;
IF (SELECT COUNT(*) FROM Sale.OrderDetail d JOIN #pilot p ON d.Sale_OrderId = p.B) <> @bDetails THROW 51032, 'B detail count changed.', 1;
IF (SELECT COUNT(*) FROM Sale.OrderDetailSerial s JOIN Sale.OrderDetail d ON d.Id = s.OrderDetailId JOIN #pilot p ON d.Sale_OrderId = p.B) <> @bSerials THROW 51033, 'B serial count changed.', 1;
IF (SELECT COUNT(*) FROM Sale.OrderDetailSerial) <> @serTotalBefore - @serDel THROW 51034, 'Serial total not as expected.', 1;
IF EXISTS (SELECT 1 FROM Sale.[Order] o WHERE o.HamkaranId IN (SELECT OrderHk FROM #pilot) GROUP BY o.HamkaranId HAVING COUNT(*) <> 1) THROW 51035, 'HamkaranId not unique after delete.', 1;

SET @status = CASE WHEN @Commit = 1 THEN N'COMMITTED' ELSE N'DRYRUN_ROLLED_BACK' END;
SET @msg = N'all checks passed';
IF @Commit = 1 COMMIT TRAN; ELSE ROLLBACK TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    SET @status = N'ROLLED_BACK_ON_ERROR';
    SET @msg = ERROR_MESSAGE();
END CATCH;

DROP TABLE IF EXISTS #old, #d, #tw, #bad, #pilot, #pd, #map, #sdel;
SELECT @status status, @msg message, @pairs pairs, @detDel details_deleted, @serDel duplicate_serials_deleted,
       @bOrders B_orders_kept, @bDetails B_details_kept, @bSerials B_serials_kept;
