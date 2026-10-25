/*
  Fix_RepairRequest_SiteAndAgencyFromHts.sql

  Scope: Rpr.RepairRequest rows that came from HTS (HtsId <> 0) where these fields are EMPTY
  in Havayar but have a value in HTS [Rpr_RepairRequest]:
    - CustomerAddressId  (Site of customer)            <- HTS CustomerAddressId   = Crm_Customer_Address.Customer_Address_ID
    - CustomerAgencyId   (Agency of customer)          <- HTS CustomerAgencyAddressId = Crm_Customer.Customer_ID (NOT an address id)
    - CustomerId         (only when NULL)              <- HTS Customer_FK

  Mapping rules (verified against HTS data):
    * Customers are matched by Customer_Code (unique in HTS). SLS.Customer.HtsId is NOT reliable
      (see Fix_Customer_HtsId_WrongStep1.sql / Fix_CustomerAddress_RematchByCustomerCode.sql).
      When Code is duplicated in Havayar, the non-"sync-" row with the lowest Id wins (same rule as
      Fix_CustomerAddress_RematchByCustomerCode.sql).
    * Site is matched by SLS.CustomerAddress.HtsId = HTS Customer_Address_ID and is only applied when
      the site belongs to the same customer as the repair request.

  Safety:
    * Only NULL -> value. Existing non-empty values are never overwritten
      (rows users edited in Havayar after the HTS cut-over stay untouched).
    * HTS customers that do not exist in Havayar are skipped and reported.
    * Whole script runs in one transaction.

  Encoding: UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001 -i Fix_RepairRequest_SiteAndAgencyFromHts.sql
  Linked server [TMS] required.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'fix-repairrequest-site-agency-hts';
DECLARE @RcCust INT = 0, @RcAddr INT = 0, @RcAg INT = 0;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] not found.', 16, 1);
    RETURN;
END;

IF OBJECT_ID('tempdb..#Canon') IS NOT NULL DROP TABLE #Canon;
CREATE TABLE #Canon (CustomerId BIGINT NOT NULL PRIMARY KEY, Code INT NOT NULL UNIQUE);
INSERT INTO #Canon (CustomerId, Code)
SELECT x.Id, x.Code
FROM (
    SELECT c.Id, c.Code,
           ROW_NUMBER() OVER (
               PARTITION BY c.Code
               ORDER BY CASE WHEN ISNULL(c.CreatedByName, N'') LIKE N'sync-%' THEN 1 ELSE 0 END, c.Id) AS Rn
    FROM SLS.Customer c
    WHERE c.Code IS NOT NULL
) x
WHERE x.Rn = 1;

IF OBJECT_ID('tempdb..#Src') IS NOT NULL DROP TABLE #Src;
SELECT
    CAST(h.RepairRequest_ID AS BIGINT) AS HtsId,
    h.CustomerAddressId               AS HtsAddressId,
    cn.CustomerId                     AS ExpCustomerId,
    ag.CustomerId                     AS ExpAgencyId,
    h.CustomerAgencyAddressId         AS HtsAgencyKey
INTO #Src
FROM [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest] h
LEFT JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] cc ON cc.Customer_ID = h.Customer_FK
LEFT JOIN #Canon cn ON cn.Code = CAST(cc.Customer_Code AS INT)
LEFT JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] ac ON ac.Customer_ID = h.CustomerAgencyAddressId
LEFT JOIN #Canon ag ON ag.Code = CAST(ac.Customer_Code AS INT);

PRINT N'=== BEFORE ===';
SELECT
    SUM(CASE WHEN CustomerId IS NULL THEN 1 ELSE 0 END)        AS CustomerNull,
    SUM(CASE WHEN CustomerAddressId IS NULL THEN 1 ELSE 0 END) AS AddressNull,
    SUM(CASE WHEN CustomerAgencyId IS NULL THEN 1 ELSE 0 END)  AS AgencyNull
FROM Rpr.RepairRequest;

BEGIN TRY
    BEGIN TRANSACTION;

    -- [1] CustomerId (only when NULL)
    UPDATE rr SET
        rr.CustomerId = s.ExpCustomerId,
        rr.ModifiedById = 1, rr.ModifiedByName = @SeedUser,
        rr.ModifiedDateMiladiDateTime = @Now, rr.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Rpr.RepairRequest rr
    INNER JOIN #Src s ON s.HtsId = rr.HtsId
    WHERE rr.HtsId <> 0 AND rr.CustomerId IS NULL AND s.ExpCustomerId IS NOT NULL;
    SET @RcCust = @@ROWCOUNT;
    PRINT N'  CustomerId filled: ' + CAST(@RcCust AS NVARCHAR(20));

    -- [2] CustomerAddressId (only when NULL; site must belong to the same customer)
    UPDATE rr SET
        rr.CustomerAddressId = a.Id,
        rr.ModifiedById = 1, rr.ModifiedByName = @SeedUser,
        rr.ModifiedDateMiladiDateTime = @Now, rr.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Rpr.RepairRequest rr
    INNER JOIN #Src s ON s.HtsId = rr.HtsId
    INNER JOIN SLS.CustomerAddress a ON a.HtsId = CAST(s.HtsAddressId AS BIGINT) AND a.HtsId <> 0
    WHERE rr.HtsId <> 0
      AND rr.CustomerAddressId IS NULL
      AND s.HtsAddressId IS NOT NULL
      AND a.CustomerId = rr.CustomerId;
    SET @RcAddr = @@ROWCOUNT;
    PRINT N'  CustomerAddressId filled: ' + CAST(@RcAddr AS NVARCHAR(20));

    -- [3] CustomerAgencyId (only when NULL)
    UPDATE rr SET
        rr.CustomerAgencyId = s.ExpAgencyId,
        rr.ModifiedById = 1, rr.ModifiedByName = @SeedUser,
        rr.ModifiedDateMiladiDateTime = @Now, rr.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Rpr.RepairRequest rr
    INNER JOIN #Src s ON s.HtsId = rr.HtsId
    WHERE rr.HtsId <> 0
      AND rr.CustomerAgencyId IS NULL
      AND s.HtsAgencyKey IS NOT NULL
      AND s.ExpAgencyId IS NOT NULL;
    SET @RcAg = @@ROWCOUNT;
    PRINT N'  CustomerAgencyId filled: ' + CAST(@RcAg AS NVARCHAR(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

PRINT N'=== AFTER ===';
SELECT
    @RcCust AS CustomerFilled, @RcAddr AS AddressFilled, @RcAg AS AgencyFilled,
    SUM(CASE WHEN CustomerId IS NULL THEN 1 ELSE 0 END)        AS CustomerNull,
    SUM(CASE WHEN CustomerAddressId IS NULL THEN 1 ELSE 0 END) AS AddressNull,
    SUM(CASE WHEN CustomerAgencyId IS NULL THEN 1 ELSE 0 END)  AS AgencyNull
FROM Rpr.RepairRequest;

PRINT N'=== Still empty because HTS customer is missing in Havayar ===';
SELECT rr.Id, rr.HtsId, rr.IdNumber, h.Customer_FK, cc.Customer_Code, cc.Customer_Title,
       h.CustomerAgencyAddressId AS HtsAgencyCustomerId, ac.Customer_Code AS AgencyCode, ac.Customer_Title AS AgencyTitle
FROM Rpr.RepairRequest rr
INNER JOIN #Src s ON s.HtsId = rr.HtsId
INNER JOIN [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest] h ON h.RepairRequest_ID = rr.HtsId
LEFT JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] cc ON cc.Customer_ID = h.Customer_FK
LEFT JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] ac ON ac.Customer_ID = h.CustomerAgencyAddressId
WHERE rr.HtsId <> 0
  AND ((rr.CustomerId IS NULL AND h.Customer_FK IS NOT NULL)
    OR (rr.CustomerAgencyId IS NULL AND h.CustomerAgencyAddressId IS NOT NULL))
ORDER BY rr.HtsId;

PRINT N'=== Conflicts: Havayar value differs from HTS (NOT changed) ===';
SELECT rr.Id, rr.HtsId, rr.IdNumber, rr.ModifiedByName, rr.ModifiedDateShamsiDateTime,
       rr.CustomerAddressId, a.Id AS HtsSiteIdInHavayar,
       rr.CustomerAgencyId, s.ExpAgencyId
FROM Rpr.RepairRequest rr
INNER JOIN #Src s ON s.HtsId = rr.HtsId
LEFT JOIN SLS.CustomerAddress a ON a.HtsId = CAST(s.HtsAddressId AS BIGINT) AND a.HtsId <> 0
WHERE rr.HtsId <> 0
  AND ((rr.CustomerAddressId IS NOT NULL AND a.Id IS NOT NULL AND rr.CustomerAddressId <> a.Id)
    OR (rr.CustomerAgencyId IS NOT NULL AND s.ExpAgencyId IS NOT NULL AND rr.CustomerAgencyId <> s.ExpAgencyId))
ORDER BY rr.HtsId;
