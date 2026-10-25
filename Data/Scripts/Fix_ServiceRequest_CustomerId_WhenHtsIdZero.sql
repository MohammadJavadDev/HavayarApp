/*
  Fix_ServiceRequest_CustomerId_WhenHtsIdZero.sql

  گرید مدیریت درخواست پشتیبانی ستون مشتری را از
  Sale.ServiceRequest.CustomerId -> SLS.Customer -> Gnr.Party.FullName می‌خواند.
  فاز ۵ با LEFT JOIN SLS.Customer c ON c.HtsId = Sale_ServiceRequest.Customer_FK
  پر می‌کند. اگر مشتری با همان کد در هوایار باشد ولی HtsId=0 باشد،
  CustomerId درخواست خالی می‌ماند (کد مشتری و نام مشتری در گرید خالی است)
  در حالی که سایت از Customer_Address_FK جداگانه مچ شده و پر است.

  این اسکریپت فقط جایی HtsId می‌گذارد که:
    - کد مشتری با Crm_Customer.Customer_Code یک‌به‌یک است
    - HtsId هدف الان روی مشتری دیگری نیست
    - همان مشتری روی درخواست پشتیبانی با CustomerId خالی دیده می‌شود
  سپس CustomerId همان درخواست‌ها از Customer_FK پر می‌شود.

  نمونه: اندیکاتور ۳۳۴۳۸ / ۳۳۴۴۰، کد ۱۴۶۲، HTS Customer_ID=2078، HtsId هوایار=0.

  دو درخواست کد ۲۰ (مینوخرم دره) مشتری متناظر در هوایار ندارند و عمداً دست نمی‌خورند.

  Encoding: UTF-8 with BOM. Linked server [TMS].
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Fix_ServiceRequest_CustomerId_WhenHtsIdZero.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'fix-sr-customerid-htsid-zero';
DECLARE @RcHts INT = 0, @RcSr INT = 0;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID('tempdb..#SrCustMap') IS NOT NULL DROP TABLE #SrCustMap;
CREATE TABLE #SrCustMap (
    CustomerId BIGINT NOT NULL PRIMARY KEY,
    NewHtsId   BIGINT NOT NULL UNIQUE,
    Code       INT    NOT NULL
);

;WITH pairs AS (
    SELECT
        c.Id AS CustomerId,
        c.Code,
        CAST(cc.Customer_ID AS BIGINT) AS NewHtsId,
        COUNT(*) OVER (PARTITION BY c.Id) AS PerCustomer,
        COUNT(*) OVER (PARTITION BY CAST(cc.Customer_ID AS BIGINT)) AS PerHts
    FROM SLS.Customer c
    INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] cc
        ON cc.Customer_Code = c.Code
    WHERE ISNULL(c.HtsId, 0) = 0
      AND c.Code IS NOT NULL
      AND EXISTS (
          SELECT 1
          FROM Sale.ServiceRequest sr
          INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_ServiceRequest] s
              ON CAST(s.ServiceRequest_ID AS BIGINT) = sr.HtsId
             AND sr.HtsId <> 0
          WHERE sr.CustomerId IS NULL
            AND s.Customer_FK = cc.Customer_ID
      )
)
INSERT INTO #SrCustMap (CustomerId, NewHtsId, Code)
SELECT p.CustomerId, p.NewHtsId, p.Code
FROM pairs p
WHERE p.PerCustomer = 1
  AND p.PerHts = 1
  AND NOT EXISTS (
      SELECT 1
      FROM SLS.Customer holder
      WHERE holder.HtsId = p.NewHtsId
        AND holder.HtsId <> 0
        AND holder.Id <> p.CustomerId
  )
  AND NOT EXISTS (
      SELECT 1
      FROM SLS.Customer sibling
      WHERE sibling.Code = p.Code
        AND sibling.Id <> p.CustomerId
  );

BEGIN TRANSACTION;
BEGIN TRY
    UPDATE c SET
        c.HtsId = m.NewHtsId,
        c.ModifiedById = 1,
        c.ModifiedByName = @SeedUser,
        c.ModifiedDateMiladiDateTime = @Now,
        c.ModifiedDateShamsiDateTime = @NowShamsi
    FROM SLS.Customer c
    INNER JOIN #SrCustMap m ON m.CustomerId = c.Id
    WHERE ISNULL(c.HtsId, 0) = 0;
    SET @RcHts = @@ROWCOUNT;

    UPDATE sr SET
        sr.CustomerId = c.Id,
        sr.ModifiedById = 1,
        sr.ModifiedByName = @SeedUser,
        sr.ModifiedDateMiladiDateTime = @Now,
        sr.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sale.ServiceRequest sr
    INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_ServiceRequest] s
        ON CAST(s.ServiceRequest_ID AS BIGINT) = sr.HtsId
       AND sr.HtsId <> 0
    INNER JOIN SLS.Customer c
        ON c.HtsId = CAST(s.Customer_FK AS BIGINT)
       AND c.HtsId <> 0
    WHERE sr.CustomerId IS NULL;
    SET @RcSr = @@ROWCOUNT;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT @RcHts AS CustomerHtsIdSet, @RcSr AS ServiceRequestCustomerIdSet;

SELECT sr.Id, sr.CustomerId, c.Code, p.FullName
FROM Sale.ServiceRequest sr
LEFT JOIN SLS.Customer c ON c.Id = sr.CustomerId
LEFT JOIN Gnr.Party p ON p.Id = c.PartyId
WHERE sr.Id IN (33438, 33440);

SELECT sr.Id, s.Customer_FK, cc.Customer_Code, cc.Customer_Title
FROM Sale.ServiceRequest sr
INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_ServiceRequest] s
    ON CAST(s.ServiceRequest_ID AS BIGINT) = sr.HtsId
LEFT JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] cc ON cc.Customer_ID = s.Customer_FK
WHERE sr.CustomerId IS NULL;
