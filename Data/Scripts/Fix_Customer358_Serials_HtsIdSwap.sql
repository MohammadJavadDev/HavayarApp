/*
================================================================================
Fix_Customer358_Serials_HtsIdSwap.sql
================================================================================
Incident 2026-09-21: مشتری کد 358 (مجتمع صنعتی رفسنجان) در HTS دو سریال دارد
(370466 / 16R019660) ولی در پورتال زیر این مشتری دیده نمی‌شد.

علت: Phase1 step1 مقدار Party.HamkaranId را با Crm_Customer.ManCompany_FK
(شناسه داخلی HTS) یکی گرفت. HtsId=482 مال رفسنجان به مشتری 100 پارت سازان رفت
و سریال‌ها با NewCustomer_FK=482 به همان مشتری وصل شدند.

این اسکریپت همان اصلاح زنده است (idempotent).
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @User NVARCHAR(80) = N'fix-cust358-serial';

BEGIN TRAN;

UPDATE SLS.Customer
SET HtsId = 0, ModifiedByName = @User, ModifiedDateMiladiDateTime = @Now
WHERE Id = 100 AND HtsId = 482;

UPDATE SLS.Customer
SET HtsId = 482, ModifiedByName = @User, ModifiedDateMiladiDateTime = @Now
WHERE Id = 331 AND Code = 358 AND HtsId <> 482;

UPDATE o
SET o.CustomerId = 331, o.ModifiedByName = @User, o.ModifiedDateMiladiDateTime = @Now
FROM Sale.[Order] o
INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_Order] h ON h.Order_ID = o.HtsId AND o.HtsId <> 0
WHERE o.CustomerId <> 331
  AND h.Customer_FK = 482;

UPDATE d
SET d.CustomerId = 331, d.ModifiedByName = @User, d.ModifiedDateMiladiDateTime = @Now
FROM Sale.OrderDetail d
INNER JOIN Sale.[Order] o ON o.Id = d.Sale_OrderId
WHERE d.CustomerId <> 331
  AND o.CustomerId = 331
  AND o.HtsId <> 0;

UPDATE s
SET s.CustomerId = 331, s.ModifiedByName = @User, s.ModifiedDateMiladiDateTime = @Now
FROM Sale.OrderDetailSerial s
WHERE s.HtsId IN (5615, 5616)
  AND s.CustomerId <> 331;

COMMIT;

SELECT c.Id, c.Code, c.HtsId, p.FullName
FROM SLS.Customer c
LEFT JOIN Gnr.Party p ON p.Id = c.PartyId
WHERE c.Id IN (100, 331);

SELECT s.Id, s.HtsId, s.Serial, s.CustomerId, c.Code
FROM Sale.OrderDetailSerial s
JOIN SLS.Customer c ON c.Id = s.CustomerId
WHERE s.HtsId IN (5615, 5616);
