/*
================================================================================
Fix_Customer_HtsId_WrongStep1.sql
================================================================================
Incident 2026-09-21: Phase1 step1 مقدار Party.HamkaranId را با
Crm_Customer.ManCompany_FK (PK شرکت HTS) یکی گرفت. نتیجه: ۱۳۸۶ مشتری اصلی
HtsId مشتری دیگری را دزدیدند و ردیف‌های seed (sync-customeraddress-hts-fix)
HtsId درست را نگه داشتند. نمونه: کد ۳۵۸ رفسنجان / سریال‌های ۳۷۰۴۶۶ و ۱۶R۰۱۹۶۶۰.

قانون اصلاح:
  - برای هر مشتری wrong_step1 فقط ردیفی که ExpectedByHamkaran = ExpectedByCode
    است (یک HtsId هدف یکتا).
  - هدف آزاد، یا در دست seed همان Code، یا در دست wrong_step1 دیگر (جابه‌جایی)
    → اختصاص داده می‌شود. از مشتری واقعی شرکت دیگر دزدیده نمی‌شود.
  - ردیف seed بعد از واگذاری HtsId=0 می‌شود (ایندکس یکتا IX_SLS_Customer_HtsId).
  - مشتری ۱۰۰ (پارت سازان، HtsId=0) HtsId=۳۷۴ را از seed ۱۱۳۰۸ می‌گیرد.
  - سپس CustomerId فرزندها از FKهای HTS دوباره نگاشت می‌شود.

پیش‌نیاز زنده: SLS._CustHtsAudit و SLS._HtsCustomerMap
  (همان staging ممیزی ۲۱ سپتامبر ۲۰۲۶). Linked Server [TMS].

ایدمپوتنت: اگر HtsId / CustomerId از قبل درست باشد صفر ردیف عوض می‌شود.

Encoding: UTF-8 with BOM
Apply:
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Fix_Customer_HtsId_WrongStep1.sql"
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'SLS._CustHtsAudit') IS NULL
BEGIN
    RAISERROR(N'SLS._CustHtsAudit یافت نشد. اول ممیزی HtsId را بسازید.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @User NVARCHAR(80) = N'fix-customer-htsid-wrongstep1';

IF OBJECT_ID(N'SLS._CustHtsFix') IS NOT NULL DROP TABLE SLS._CustHtsFix;
CREATE TABLE SLS._CustHtsFix
(
    CustomerId       BIGINT         NOT NULL PRIMARY KEY,
    Code             INT            NULL,
    FullName         NVARCHAR(200)  NULL,
    OldHtsId         BIGINT         NOT NULL,
    NewHtsId         BIGINT         NOT NULL,
    Action           NVARCHAR(40)   NOT NULL,
    DonorCustomerId  BIGINT         NULL,
    HolderKind       NVARCHAR(40)   NULL
);

;WITH tgt AS (
    SELECT a.CustomerId, a.Code, a.FullName, a.ProposedHtsId, c.HtsId AS LiveHtsId
    FROM SLS._CustHtsAudit a
    INNER JOIN SLS.Customer c ON c.Id = a.CustomerId
    WHERE a.Kind = N'wrong_step1'
      AND a.ExpectedByHamkaran = a.ExpectedByCode
      AND c.HtsId <> a.ProposedHtsId
),
holder AS (
    SELECT t.CustomerId, t.Code, t.FullName, t.LiveHtsId, t.ProposedHtsId,
           h.Id AS HolderId,
           CASE
               WHEN h.Id IS NULL THEN N'free'
               WHEN EXISTS (SELECT 1 FROM tgt x WHERE x.CustomerId = h.Id) THEN N'wrong_step1'
               WHEN h.CreatedByName = N'sync-customeraddress-hts-fix' AND h.Code = t.Code THEN N'seed_same_code'
               ELSE N'other'
           END AS HolderKind
    FROM tgt t
    LEFT JOIN SLS.Customer h
        ON h.HtsId = t.ProposedHtsId AND h.Id <> t.CustomerId AND h.HtsId <> 0
)
INSERT INTO SLS._CustHtsFix (CustomerId, Code, FullName, OldHtsId, NewHtsId, Action, DonorCustomerId, HolderKind)
SELECT CustomerId, Code, FullName, LiveHtsId, ProposedHtsId,
       CASE HolderKind
           WHEN N'free' THEN N'assign_free'
           WHEN N'wrong_step1' THEN N'swap'
           WHEN N'seed_same_code' THEN N'take_seed'
           ELSE N'blocked'
       END,
       CASE WHEN HolderKind = N'seed_same_code' THEN HolderId END,
       HolderKind
FROM holder;

INSERT INTO SLS._CustHtsFix (CustomerId, Code, FullName, OldHtsId, NewHtsId, Action, DonorCustomerId, HolderKind)
SELECT 100, 108, N'پارت سازان', c.HtsId, 374, N'take_seed', 11308, N'seed_same_code'
FROM SLS.Customer c
WHERE c.Id = 100
  AND c.HtsId <> 374
  AND NOT EXISTS (SELECT 1 FROM SLS._CustHtsFix f WHERE f.CustomerId = 100)
  AND EXISTS (
        SELECT 1 FROM SLS.Customer s
        WHERE s.Id = 11308 AND s.Code = 108
          AND s.CreatedByName = N'sync-customeraddress-hts-fix'
          AND (s.HtsId = 374 OR c.HtsId = 374)
      );

IF EXISTS (SELECT 1 FROM SLS._CustHtsFix WHERE Action = N'blocked')
BEGIN
    RAISERROR(N'برنامه شامل هدف مسدود است؛ بدون دزدیدن HtsId مشتری واقعی متوقف شد.', 16, 1);
    RETURN;
END;

IF EXISTS (SELECT NewHtsId FROM SLS._CustHtsFix GROUP BY NewHtsId HAVING COUNT(*) > 1)
BEGIN
    RAISERROR(N'برنامه HtsId هدف تکراری دارد.', 16, 1);
    RETURN;
END;

BEGIN TRAN;

UPDATE c
SET c.HtsId = 0,
    c.ModifiedByName = @User,
    c.ModifiedDateMiladiDateTime = @Now
FROM SLS.Customer c
WHERE c.HtsId <> 0
  AND (
        EXISTS (SELECT 1 FROM SLS._CustHtsFix f WHERE f.CustomerId = c.Id AND f.OldHtsId = c.HtsId)
     OR EXISTS (SELECT 1 FROM SLS._CustHtsFix f WHERE f.DonorCustomerId = c.Id)
      );

UPDATE c
SET c.HtsId = f.NewHtsId,
    c.ModifiedByName = @User,
    c.ModifiedDateMiladiDateTime = @Now
FROM SLS.Customer c
INNER JOIN SLS._CustHtsFix f ON f.CustomerId = c.Id
WHERE c.HtsId <> f.NewHtsId;

IF OBJECT_ID(N'SLS._HtsOrderCust') IS NULL
BEGIN
    SELECT * INTO SLS._HtsOrderCust
    FROM OPENQUERY([TMS], 'SELECT CAST(Order_ID AS bigint) AS OrderHtsId, CAST(Customer_FK AS bigint) AS CustomerHtsId FROM TotalSystem.dbo.Sale_Order WHERE Customer_FK IS NOT NULL');
    CREATE UNIQUE CLUSTERED INDEX IX_HtsOrderCust ON SLS._HtsOrderCust(OrderHtsId);
END;
IF OBJECT_ID(N'SLS._HtsSerialCust') IS NULL
BEGIN
    SELECT * INTO SLS._HtsSerialCust
    FROM OPENQUERY([TMS], 'SELECT CAST(OrderDetail_Serial_ID AS bigint) AS SerialHtsId, CAST(NewCustomer_FK AS bigint) AS CustomerHtsId FROM TotalSystem.dbo.Sale_OrderDetail_Serial WHERE NewCustomer_FK IS NOT NULL');
    CREATE UNIQUE CLUSTERED INDEX IX_HtsSerialCust ON SLS._HtsSerialCust(SerialHtsId);
END;
IF OBJECT_ID(N'SLS._HtsAddrCust') IS NULL
BEGIN
    SELECT * INTO SLS._HtsAddrCust
    FROM OPENQUERY([TMS], 'SELECT CAST(Customer_Address_ID AS bigint) AS AddrHtsId, CAST(Customer_FK AS bigint) AS CustomerHtsId FROM TotalSystem.dbo.Crm_Customer_Address WHERE Customer_FK IS NOT NULL');
    CREATE UNIQUE CLUSTERED INDEX IX_HtsAddrCust ON SLS._HtsAddrCust(AddrHtsId);
END;
IF OBJECT_ID(N'SLS._HtsSrCust') IS NULL
BEGIN
    SELECT * INTO SLS._HtsSrCust
    FROM OPENQUERY([TMS], 'SELECT CAST(ServiceRequest_ID AS bigint) AS SrHtsId, CAST(Customer_FK AS bigint) AS CustomerHtsId FROM TotalSystem.dbo.Sale_ServiceRequest WHERE Customer_FK IS NOT NULL');
    CREATE UNIQUE CLUSTERED INDEX IX_HtsSrCust ON SLS._HtsSrCust(SrHtsId);
END;
IF OBJECT_ID(N'SLS._HtsCcCust') IS NULL
BEGIN
    SELECT * INTO SLS._HtsCcCust
    FROM OPENQUERY([TMS], 'SELECT CAST(Id AS bigint) AS CcHtsId, CAST(Customer_FK AS bigint) AS CustomerHtsId FROM TotalSystem.dbo.Sale_CollectionClaim WHERE Customer_FK IS NOT NULL');
    CREATE UNIQUE CLUSTERED INDEX IX_HtsCcCust ON SLS._HtsCcCust(CcHtsId);
END;
IF OBJECT_ID(N'SLS._HtsGagCust') IS NULL
BEGIN
    SELECT * INTO SLS._HtsGagCust
    FROM OPENQUERY([TMS], 'SELECT CAST(Customer_AllowedGuarantee_ID AS bigint) AS GagHtsId, CAST(Customer_FK AS bigint) AS CustomerHtsId FROM TotalSystem.dbo.Sale_Customer_AllowedGuarantee WHERE Customer_FK IS NOT NULL');
    CREATE UNIQUE CLUSTERED INDEX IX_HtsGagCust ON SLS._HtsGagCust(GagHtsId);
END;

UPDATE o SET o.CustomerId = c.Id, o.ModifiedByName = @User, o.ModifiedDateMiladiDateTime = @Now
FROM Sale.[Order] o
INNER JOIN SLS._HtsOrderCust h ON h.OrderHtsId = o.HtsId
INNER JOIN SLS.Customer c ON c.HtsId = h.CustomerHtsId AND c.HtsId <> 0
WHERE o.HtsId <> 0 AND (o.CustomerId IS NULL OR o.CustomerId <> c.Id);

UPDATE s SET s.CustomerId = c.Id, s.ModifiedByName = @User, s.ModifiedDateMiladiDateTime = @Now
FROM Sale.OrderDetailSerial s
INNER JOIN SLS._HtsSerialCust h ON h.SerialHtsId = s.HtsId
INNER JOIN SLS.Customer c ON c.HtsId = h.CustomerHtsId AND c.HtsId <> 0
WHERE s.HtsId <> 0 AND (s.CustomerId IS NULL OR s.CustomerId <> c.Id);

UPDATE a SET a.CustomerId = c.Id, a.ModifiedByName = @User, a.ModifiedDateMiladiDateTime = @Now
FROM SLS.CustomerAddress a
INNER JOIN SLS._HtsAddrCust h ON h.AddrHtsId = a.HtsId
INNER JOIN SLS.Customer c ON c.HtsId = h.CustomerHtsId AND c.HtsId <> 0
WHERE a.HtsId <> 0 AND (a.CustomerId IS NULL OR a.CustomerId <> c.Id);

UPDATE sr SET sr.CustomerId = c.Id, sr.ModifiedByName = @User, sr.ModifiedDateMiladiDateTime = @Now
FROM Sale.ServiceRequest sr
INNER JOIN SLS._HtsSrCust h ON h.SrHtsId = sr.HtsId
INNER JOIN SLS.Customer c ON c.HtsId = h.CustomerHtsId AND c.HtsId <> 0
WHERE sr.HtsId <> 0 AND (sr.CustomerId IS NULL OR sr.CustomerId <> c.Id);

UPDATE cc SET cc.CustomerId = c.Id, cc.ModifiedByName = @User, cc.ModifiedDateMiladiDateTime = @Now
FROM Sale.CollectionClaim cc
INNER JOIN SLS._HtsCcCust h ON h.CcHtsId = cc.HtsId
INNER JOIN SLS.Customer c ON c.HtsId = h.CustomerHtsId AND c.HtsId <> 0
WHERE cc.HtsId <> 0 AND (cc.CustomerId IS NULL OR cc.CustomerId <> c.Id);

UPDATE g SET g.CustomerId = c.Id, g.ModifiedByName = @User, g.ModifiedDateMiladiDateTime = @Now
FROM Sale.CustomerAllowedGuarantee g
INNER JOIN SLS._HtsGagCust h ON h.GagHtsId = g.HtsId
INNER JOIN SLS.Customer c ON c.HtsId = h.CustomerHtsId AND c.HtsId <> 0
WHERE g.HtsId <> 0 AND (g.CustomerId IS NULL OR g.CustomerId <> c.Id);

UPDATE d SET d.CustomerId = o.CustomerId, d.ModifiedByName = @User, d.ModifiedDateMiladiDateTime = @Now
FROM Sale.OrderDetail d
INNER JOIN Sale.[Order] o ON o.Id = d.Sale_OrderId
WHERE o.HtsId <> 0 AND o.CustomerId IS NOT NULL
  AND (d.CustomerId IS NULL OR d.CustomerId <> o.CustomerId);

UPDATE s SET s.CustomerId = o.CustomerId, s.ModifiedByName = @User, s.ModifiedDateMiladiDateTime = @Now
FROM Sale.OrderDetailSerial s
INNER JOIN Sale.OrderDetail d ON d.Id = s.OrderDetailId
INNER JOIN Sale.[Order] o ON o.Id = d.Sale_OrderId
WHERE o.HtsId <> 0 AND o.CustomerId IS NOT NULL
  AND (s.CustomerId IS NULL OR s.CustomerId <> o.CustomerId)
  AND NOT EXISTS (SELECT 1 FROM SLS._HtsSerialCust h WHERE h.SerialHtsId = s.HtsId AND s.HtsId <> 0);

COMMIT;

SELECT N'TargetsOk' AS Kind, COUNT(*) AS Cnt
FROM SLS._CustHtsFix f
INNER JOIN SLS.Customer c ON c.Id = f.CustomerId
WHERE c.HtsId = f.NewHtsId
UNION ALL
SELECT N'StillStep1', COUNT(*)
FROM SLS.Customer c
INNER JOIN Gnr.Party p ON p.Id = c.PartyId
INNER JOIN SLS._HtsCustomerMap m ON m.HtsId = c.HtsId
WHERE c.HtsId <> 0
  AND m.ManCompanyFk = p.HamkaranId
  AND ISNULL(m.HamkaranCompanyId, -1) <> p.HamkaranId;
