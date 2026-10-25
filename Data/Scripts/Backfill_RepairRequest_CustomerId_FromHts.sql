/*
  Backfill_RepairRequest_CustomerId_FromHts.sql
  بعضی درخواست‌های تعمیر مهاجرت‌شده CustomerId خالی دارند، در حالی که در HTS مشتری دارند؛
  در چاپ «شناسنامه درخواست تعمیر» / «برچسب تعمیر» فیلد «نام مشتری» خالی می‌ماند.

  فقط ردیف‌هایی که CustomerId = NULL هستند پر می‌شوند (مقدار موجود هرگز تغییر نمی‌کند)،
  و فقط وقتی SLS.Customer.HtsId = Customer_FK قدیمی دقیقاً یک مشتری بدهد.
  مشتری ساختگی ساخته نمی‌شود؛ مشتری‌هایی که در سیستم جدید نیستند NULL می‌مانند.
  (نسخهٔ عمومی‌تر و تهاجمی‌تر: Fix_RepairRequest_CustomerAndHeaderFks.sql)

  Encoding: UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

PRINT N'=== BEFORE ===';
SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN CustomerId IS NULL THEN 1 ELSE 0 END) AS CustomerNull
FROM Rpr.RepairRequest;

-- خواندن از Linked Server بیرون از تراکنش (تا به DTC نیاز نباشد)
SELECT
    rr.Id AS RepairRequestId,
    rr.HtsId,
    s.Customer_FK AS HtsCustomerId,
    c.Id AS NewCustomerId
INTO #Map
FROM Rpr.RepairRequest AS rr
INNER JOIN [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest] AS s
    ON s.RepairRequest_ID = rr.HtsId
CROSS APPLY (
    SELECT MIN(cu.Id) AS Id, COUNT(*) AS Cnt
    FROM SLS.Customer AS cu
    WHERE cu.HtsId = s.Customer_FK AND cu.HtsId <> 0
) AS c
WHERE rr.CustomerId IS NULL
  AND s.Customer_FK IS NOT NULL
  AND c.Cnt = 1;

PRINT N'=== rows to fill (RepairRequestId list for rollback) ===';
SELECT STRING_AGG(CAST(RepairRequestId AS NVARCHAR(20)), N',') AS FilledIds, COUNT(*) AS Cnt FROM #Map;

BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE rr
    SET rr.CustomerId = m.NewCustomerId
    FROM Rpr.RepairRequest AS rr
    INNER JOIN #Map AS m ON m.RepairRequestId = rr.Id
    WHERE rr.CustomerId IS NULL;

    PRINT N'  CustomerId filled: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

PRINT N'=== AFTER ===';
SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN CustomerId IS NULL THEN 1 ELSE 0 END) AS CustomerNull
FROM Rpr.RepairRequest;

SELECT TOP 5
    rr.Id, rr.HtsId, rr.IdNumber, rr.CustomerId, p.FullName AS CustomerName
FROM Rpr.RepairRequest AS rr
INNER JOIN #Map AS m ON m.RepairRequestId = rr.Id
LEFT JOIN SLS.Customer AS c ON c.Id = rr.CustomerId
LEFT JOIN Gnr.Party AS p ON p.Id = c.PartyId
ORDER BY rr.Id DESC;

DROP TABLE #Map;
