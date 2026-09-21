/*
================================================================================
SyncAfterSalesWidenCustomerHtsIdFromTotalSystem.sql
================================================================================
گسترش نگاشت SLS.Customer.HtsId بدون پاک کردن مقادیر موجود (HtsId <> 0).

ترتیب کلیدهای یک‌به‌یک:
  1) Party.HamkaranId = Crm_Customer.ManCompany_FK          (میراث فاز ۱)
  2) Party.HamkaranId = Gnr_ManCompany.Hamkaran_ManCompany_FK
     سپس ManCompany_ID = Crm_Customer.ManCompany_FK         (کلید شرکت راهکاران)
  3) Customer.Code = Crm_Customer.Customer_Code             (هر دو unique)
  4) Party.NationalID = Gnr_ManCompany.NationalCode         (۱۰–۱۱ رقم، ۱:۱ در هر دو طرف)

تداخل / HtsId تکراری لاگ می‌شود (مثل مشتری HTS 41 در فاز ۱).
سپس MERGE آدرس و سریال تا CustomerAddressId از Customer_Address_FK پر شود.

DECLARE @Strict = 1 باعث شکست در صورت باقی‌ماندن skip می‌شود.
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Strict BIT = 0;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'sync-after-sales-customer-widen';
DECLARE @Rc INT;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

BEGIN TRY
    EXEC sp_testlinkedserver [TMS];
END TRY
BEGIN CATCH
    DECLARE @Ls NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'sp_testlinkedserver [TMS] ناموفق: %s', 16, 1, @Ls);
    RETURN;
END CATCH;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('tempdb..#Unmapped') IS NOT NULL DROP TABLE #Unmapped;
    CREATE TABLE #Unmapped (Kind NVARCHAR(80) NOT NULL, OldId BIGINT NULL, Detail NVARCHAR(400) NULL);

    PRINT N'=== Customer.HtsId step1: Party.HamkaranId = Crm_Customer.ManCompany_FK ===';
    ;WITH pairs AS (
        SELECT
            c.Id AS CustomerId,
            CAST(cc.Customer_ID AS BIGINT) AS HtsId,
            ROW_NUMBER() OVER (PARTITION BY CAST(cc.Customer_ID AS BIGINT) ORDER BY c.Id) AS RnPerHts,
            ROW_NUMBER() OVER (PARTITION BY c.Id ORDER BY CAST(cc.Customer_ID AS BIGINT)) AS RnPerCustomer
        FROM SLS.Customer c
        INNER JOIN Gnr.Party p ON p.Id = c.PartyId
        INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] cc
            ON cc.ManCompany_FK = p.HamkaranId
        WHERE ISNULL(c.HtsId, 0) = 0
          AND p.HamkaranId IS NOT NULL AND p.HamkaranId <> 0
    )
    UPDATE c SET c.HtsId = pairs.HtsId
    FROM SLS.Customer c
    INNER JOIN pairs ON pairs.CustomerId = c.Id
    WHERE pairs.RnPerHts = 1
      AND pairs.RnPerCustomer = 1
      AND NOT EXISTS (
          SELECT 1 FROM SLS.Customer x
          WHERE x.HtsId = pairs.HtsId AND x.HtsId <> 0 AND x.Id <> c.Id
      );
    SET @Rc = @@ROWCOUNT;
    PRINT N'step1 mapped: ' + CAST(@Rc AS NVARCHAR(20));

    INSERT INTO #Unmapped (Kind, OldId, Detail)
    SELECT N'CustomerHtsIdSkipped_ManCompanyPk', CAST(cc.Customer_ID AS BIGINT),
           N'Duplicate/taken ManCompany_FK; skipped Customer.Id=' + CAST(c.Id AS NVARCHAR(20))
    FROM SLS.Customer c
    INNER JOIN Gnr.Party p ON p.Id = c.PartyId
    INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] cc
        ON cc.ManCompany_FK = p.HamkaranId
    WHERE ISNULL(c.HtsId, 0) = 0
      AND p.HamkaranId IS NOT NULL AND p.HamkaranId <> 0;

    PRINT N'=== Customer.HtsId step2: Party.HamkaranId = Gnr_ManCompany.Hamkaran_ManCompany_FK ===';
    ;WITH pairs AS (
        SELECT
            c.Id AS CustomerId,
            CAST(cc.Customer_ID AS BIGINT) AS HtsId,
            ROW_NUMBER() OVER (PARTITION BY CAST(cc.Customer_ID AS BIGINT) ORDER BY c.Id) AS RnPerHts,
            ROW_NUMBER() OVER (PARTITION BY c.Id ORDER BY CAST(cc.Customer_ID AS BIGINT)) AS RnPerCustomer
        FROM SLS.Customer c
        INNER JOIN Gnr.Party p ON p.Id = c.PartyId
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] mc
            ON mc.Hamkaran_ManCompany_FK = p.HamkaranId
        INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] cc
            ON cc.ManCompany_FK = mc.ManCompany_ID
        WHERE ISNULL(c.HtsId, 0) = 0
          AND p.HamkaranId IS NOT NULL AND p.HamkaranId <> 0
    )
    UPDATE c SET c.HtsId = pairs.HtsId
    FROM SLS.Customer c
    INNER JOIN pairs ON pairs.CustomerId = c.Id
    WHERE pairs.RnPerHts = 1
      AND pairs.RnPerCustomer = 1
      AND NOT EXISTS (
          SELECT 1 FROM SLS.Customer x
          WHERE x.HtsId = pairs.HtsId AND x.HtsId <> 0 AND x.Id <> c.Id
      );
    SET @Rc = @@ROWCOUNT;
    PRINT N'step2 mapped: ' + CAST(@Rc AS NVARCHAR(20));

    INSERT INTO #Unmapped (Kind, OldId, Detail)
    SELECT N'CustomerHtsIdSkipped_HamkaranCompany', CAST(cc.Customer_ID AS BIGINT),
           N'Duplicate/taken Hamkaran company; skipped Customer.Id=' + CAST(c.Id AS NVARCHAR(20))
    FROM SLS.Customer c
    INNER JOIN Gnr.Party p ON p.Id = c.PartyId
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] mc
        ON mc.Hamkaran_ManCompany_FK = p.HamkaranId
    INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] cc
        ON cc.ManCompany_FK = mc.ManCompany_ID
    WHERE ISNULL(c.HtsId, 0) = 0
      AND p.HamkaranId IS NOT NULL AND p.HamkaranId <> 0;

    PRINT N'=== Customer.HtsId step3: Customer.Code = Crm_Customer.Customer_Code ===';
    ;WITH pairs AS (
        SELECT
            c.Id AS CustomerId,
            CAST(cc.Customer_ID AS BIGINT) AS HtsId,
            ROW_NUMBER() OVER (PARTITION BY CAST(cc.Customer_ID AS BIGINT) ORDER BY c.Id) AS RnPerHts,
            ROW_NUMBER() OVER (PARTITION BY c.Id ORDER BY CAST(cc.Customer_ID AS BIGINT)) AS RnPerCustomer
        FROM SLS.Customer c
        INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] cc
            ON cc.Customer_Code = c.Code
        WHERE ISNULL(c.HtsId, 0) = 0
          AND c.Code IS NOT NULL
    )
    UPDATE c SET c.HtsId = pairs.HtsId
    FROM SLS.Customer c
    INNER JOIN pairs ON pairs.CustomerId = c.Id
    WHERE pairs.RnPerHts = 1
      AND pairs.RnPerCustomer = 1
      AND NOT EXISTS (
          SELECT 1 FROM SLS.Customer x
          WHERE x.HtsId = pairs.HtsId AND x.HtsId <> 0 AND x.Id <> c.Id
      );
    SET @Rc = @@ROWCOUNT;
    PRINT N'step3 mapped: ' + CAST(@Rc AS NVARCHAR(20));

    INSERT INTO #Unmapped (Kind, OldId, Detail)
    SELECT N'CustomerHtsIdSkipped_Code', CAST(cc.Customer_ID AS BIGINT),
           N'Customer.Code matched but HtsId taken/collision; skipped Customer.Id=' + CAST(c.Id AS NVARCHAR(20))
    FROM SLS.Customer c
    INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] cc
        ON cc.Customer_Code = c.Code
    WHERE ISNULL(c.HtsId, 0) = 0
      AND c.Code IS NOT NULL;

    PRINT N'=== Customer.HtsId step4: unique NationalID (10-11 digits) ===';
    ;WITH natParty AS (
        SELECT
            c.Id AS CustomerId,
            c.HtsId,
            LTRIM(RTRIM(p.NationalID)) AS Nat,
            COUNT(*) OVER (PARTITION BY LTRIM(RTRIM(p.NationalID))) AS CntParty
        FROM SLS.Customer c
        INNER JOIN Gnr.Party p ON p.Id = c.PartyId
        WHERE LEN(LTRIM(RTRIM(p.NationalID))) BETWEEN 10 AND 11
          AND LTRIM(RTRIM(p.NationalID)) NOT LIKE N'%[^0-9]%'
          AND LTRIM(RTRIM(p.NationalID)) NOT IN (N'0000000000', N'00000000000', N'1111111111', N'1234567890')
    ),
    natMc AS (
        SELECT
            mc.ManCompany_ID,
            LTRIM(RTRIM(mc.NationalCode)) AS Nat,
            COUNT(*) OVER (PARTITION BY LTRIM(RTRIM(mc.NationalCode))) AS CntMc
        FROM [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] mc
        WHERE LEN(LTRIM(RTRIM(mc.NationalCode))) BETWEEN 10 AND 11
          AND LTRIM(RTRIM(mc.NationalCode)) NOT LIKE N'%[^0-9]%'
          AND LTRIM(RTRIM(mc.NationalCode)) NOT IN (N'0000000000', N'00000000000', N'1111111111', N'1234567890')
    ),
    pairs AS (
        SELECT
            np.CustomerId,
            CAST(cc.Customer_ID AS BIGINT) AS HtsId,
            ROW_NUMBER() OVER (PARTITION BY CAST(cc.Customer_ID AS BIGINT) ORDER BY np.CustomerId) AS RnPerHts,
            ROW_NUMBER() OVER (PARTITION BY np.CustomerId ORDER BY CAST(cc.Customer_ID AS BIGINT)) AS RnPerCustomer
        FROM natParty np
        INNER JOIN natMc nm ON nm.Nat = np.Nat AND np.CntParty = 1 AND nm.CntMc = 1 AND ISNULL(np.HtsId, 0) = 0
        INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] cc
            ON cc.ManCompany_FK = nm.ManCompany_ID
    )
    UPDATE c SET c.HtsId = pairs.HtsId
    FROM SLS.Customer c
    INNER JOIN pairs ON pairs.CustomerId = c.Id
    WHERE pairs.RnPerHts = 1
      AND pairs.RnPerCustomer = 1
      AND NOT EXISTS (
          SELECT 1 FROM SLS.Customer x
          WHERE x.HtsId = pairs.HtsId AND x.HtsId <> 0 AND x.Id <> c.Id
      );
    SET @Rc = @@ROWCOUNT;
    PRINT N'step4 mapped: ' + CAST(@Rc AS NVARCHAR(20));

    INSERT INTO #Unmapped (Kind, OldId, Detail)
    SELECT N'CustomerStillUnmapped', c.Id, N'No remaining unique HTS key'
    FROM SLS.Customer c
    WHERE ISNULL(c.HtsId, 0) = 0;

    PRINT N'=== SLS.CustomerAddress ===';
    MERGE SLS.CustomerAddress AS t
    USING (
        SELECT
            CAST(a.Customer_Address_ID AS BIGINT) AS HtsId,
            c.Id AS CustomerId,
            z.Id AS ZoneId,
            mapProv.Id AS ProvinceId,
            ap.Id AS AgencyPartyId,
            dl.Id AS AgencyDlId,
            NULLIF(LTRIM(RTRIM(a.Address_Title)), N'') AS Title,
            NULLIF(LTRIM(RTRIM(a.Address)), N'') AS Address,
            CAST(a.Grade_FK AS INT) AS Grade,
            ISNULL(a.Hamkaran_Add_FK, 0) AS HamkaranAddId,
            a.RahkaranId,
            a.RahkaranVersion
        FROM [TMS].[TotalSystem].[dbo].[Crm_Customer_Address] a
        INNER JOIN SLS.Customer c ON c.HtsId = a.Customer_FK
        LEFT JOIN Crm.Zone z ON z.HtsId = a.Zone_FK
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_Province] gp ON gp.Province_ID = a.Province_FK
        OUTER APPLY (
            SELECT TOP 1 r.Id
            FROM Gnr.Region r
            WHERE r.[Type] = 2 AND (
                (gp.Province_Title LIKE N'%تهران%' AND r.Name = N'استان تهران')
                OR r.Name = N'استان ' + gp.Province_Title
                OR r.Name = N'استان  ' + gp.Province_Title
                OR r.Name = gp.Province_Title
                OR (gp.Province_Title = N'خراسان' AND r.Name = N'استان خراسان رضوی')
                OR (gp.Province_Title LIKE N'اصفهان%' AND r.Name = N'استان اصفهان')
                OR (gp.Province_Title LIKE N'%کهکیلویه%' AND r.Name LIKE N'%کهکیلویه%')
            )
            ORDER BY CASE
                WHEN gp.Province_Title LIKE N'%تهران%' AND r.Name = N'استان تهران' THEN 0
                WHEN r.Name = N'استان ' + gp.Province_Title THEN 1
                WHEN r.Name = gp.Province_Title THEN 2
                ELSE 3
            END
        ) mapProv
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Acc_DL] ad ON ad.Acc_DL_ID = a.AgencyDlId
        LEFT JOIN FIN.DL dl ON dl.HamkaranId = ad.Hamkaran_Acc_DL_FK AND ISNULL(ad.Hamkaran_Acc_DL_FK, 0) <> 0
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] amc ON amc.ManCompany_ID = a.AgencyMancompanyId
        LEFT JOIN Gnr.Party ap ON ap.HamkaranId = amc.Hamkaran_ManCompany_FK AND ISNULL(amc.Hamkaran_ManCompany_FK, 0) <> 0
        WHERE c.HtsId <> 0
    ) AS s
    ON t.HtsId = s.HtsId
    WHEN MATCHED THEN UPDATE SET
        t.CustomerId = s.CustomerId, t.ZoneId = s.ZoneId, t.ProvinceId = s.ProvinceId,
        t.AgencyPartyId = s.AgencyPartyId, t.AgencyDlId = s.AgencyDlId,
        t.Title = s.Title, t.Address = s.Address, t.Grade = s.Grade,
        t.HamkaranAddId = s.HamkaranAddId, t.RahkaranId = s.RahkaranId, t.RahkaranVersion = s.RahkaranVersion,
        t.ModifiedById = 1, t.ModifiedByName = @SeedUser,
        t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED AND s.CustomerId IS NOT NULL THEN INSERT
        (HtsId, CustomerId, Title, Address, ZoneId, Grade, ProvinceId, AgencyPartyId, AgencyDlId,
         HamkaranAddId, RahkaranId, RahkaranVersion,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (s.HtsId, s.CustomerId, s.Title, s.Address, s.ZoneId, s.Grade, s.ProvinceId, s.AgencyPartyId, s.AgencyDlId,
         s.HamkaranAddId, s.RahkaranId, s.RahkaranVersion,
         1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
    SET @Rc = @@ROWCOUNT;
    PRINT N'CustomerAddress MERGE rows touched: ' + CAST(@Rc AS NVARCHAR(20));

    INSERT INTO #Unmapped (Kind, OldId, Detail)
    SELECT N'CustomerAddress', CAST(a.Customer_Address_ID AS BIGINT), N'Customer not mapped'
    FROM [TMS].[TotalSystem].[dbo].[Crm_Customer_Address] a
    WHERE NOT EXISTS (SELECT 1 FROM SLS.Customer c WHERE c.HtsId = a.Customer_FK AND c.HtsId <> 0);

    PRINT N'=== Sale.OrderDetailSerial CustomerAddressId / CustomerId remap ===';
    MERGE Sale.OrderDetailSerial AS t
    USING (
        SELECT
            CAST(s.OrderDetail_Serial_ID AS BIGINT) AS HtsId,
            d.Id AS OrderDetailId,
            c.Id AS CustomerId,
            addr.Id AS CustomerAddressId,
            NULLIF(LTRIM(RTRIM(s.Serial)), N'') AS Serial,
            NULLIF(LTRIM(RTRIM(s.Model)), N'') AS Model,
            CAST(s.Hours_Daily AS NVARCHAR(20)) AS HoursDaily,
            s.Runtime, s.LifeTime, s.Guarantee_Time AS GuaranteeTime,
            CAST(s.Guarantee_SendDay AS INT) AS GuaranteeSendDay,
            CAST(s.Guarantee_LaunchDay AS INT) AS GuaranteeLaunchDay,
            s.HasInsurance,
            s.SendDate AS SendMiladiDate, s.SendDate_Shamsi AS SendShamsiDate, s.SendTime,
            s.LaunchDate AS LaunchMiladiDate, s.LaunchDate_Shamsi AS LaunchShamsiDate,
            s.ExitFactory_Date AS ExitFactoryMiladiDate, s.ExitFactory_Date_Shamsi AS ExitFactoryShamsiDate, s.ExitFactory_Time AS ExitFactoryTime,
            CAST(s.ProductionOrder_Number AS NVARCHAR(50)) AS ProductionOrderNumber,
            s.IndustrialComment, s.InvComment, s.IndustrialConfirm,
            s.FinalExitDate AS FinalExitMiladiDate, s.FinalExitDate_Shamsi AS FinalExitShamsiDate, s.ExitTimeFinal
        FROM [TMS].[TotalSystem].[dbo].[Sale_OrderDetail_Serial] s
        INNER JOIN Sale.OrderDetail d ON d.HtsId = s.OrderDetail_FK AND d.HtsId <> 0
        LEFT JOIN SLS.Customer c ON c.HtsId = s.NewCustomer_FK AND c.HtsId <> 0
        LEFT JOIN SLS.CustomerAddress addr ON addr.HtsId = s.Customer_Address_FK AND addr.HtsId <> 0
    ) AS s
    ON t.HtsId = s.HtsId
    WHEN MATCHED THEN UPDATE SET
        t.OrderDetailId = s.OrderDetailId, t.CustomerId = s.CustomerId, t.CustomerAddressId = s.CustomerAddressId,
        t.Serial = s.Serial, t.Model = s.Model, t.HoursDaily = s.HoursDaily, t.Runtime = s.Runtime, t.LifeTime = s.LifeTime,
        t.GuaranteeTime = s.GuaranteeTime, t.GuaranteeSendDay = s.GuaranteeSendDay, t.GuaranteeLaunchDay = s.GuaranteeLaunchDay,
        t.HasInsurance = s.HasInsurance, t.SendMiladiDate = s.SendMiladiDate, t.SendShamsiDate = s.SendShamsiDate, t.SendTime = s.SendTime,
        t.LaunchMiladiDate = s.LaunchMiladiDate, t.LaunchShamsiDate = s.LaunchShamsiDate,
        t.ExitFactoryMiladiDate = s.ExitFactoryMiladiDate, t.ExitFactoryShamsiDate = s.ExitFactoryShamsiDate, t.ExitFactoryTime = s.ExitFactoryTime,
        t.ProductionOrderNumber = s.ProductionOrderNumber, t.IndustrialComment = s.IndustrialComment, t.InvComment = s.InvComment,
        t.IndustrialConfirm = s.IndustrialConfirm, t.FinalExitMiladiDate = s.FinalExitMiladiDate,
        t.FinalExitShamsiDate = s.FinalExitShamsiDate, t.ExitTimeFinal = s.ExitTimeFinal,
        t.ModifiedById = 1, t.ModifiedByName = @SeedUser,
        t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT
        (HtsId, OrderDetailId, CustomerId, CustomerAddressId, Serial, Model, HoursDaily, Runtime, LifeTime,
         GuaranteeTime, GuaranteeSendDay, GuaranteeLaunchDay, HasInsurance, SendMiladiDate, SendShamsiDate, SendTime,
         LaunchMiladiDate, LaunchShamsiDate, ExitFactoryMiladiDate, ExitFactoryShamsiDate, ExitFactoryTime,
         ProductionOrderNumber, IndustrialComment, InvComment, IndustrialConfirm,
         FinalExitMiladiDate, FinalExitShamsiDate, ExitTimeFinal,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (s.HtsId, s.OrderDetailId, s.CustomerId, s.CustomerAddressId, s.Serial, s.Model, s.HoursDaily, s.Runtime, s.LifeTime,
         s.GuaranteeTime, s.GuaranteeSendDay, s.GuaranteeLaunchDay, s.HasInsurance, s.SendMiladiDate, s.SendShamsiDate, s.SendTime,
         s.LaunchMiladiDate, s.LaunchShamsiDate, s.ExitFactoryMiladiDate, s.ExitFactoryShamsiDate, s.ExitFactoryTime,
         s.ProductionOrderNumber, s.IndustrialComment, s.InvComment, s.IndustrialConfirm,
         s.FinalExitMiladiDate, s.FinalExitShamsiDate, s.ExitTimeFinal,
         1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
    SET @Rc = @@ROWCOUNT;
    PRINT N'OrderDetailSerial MERGE rows touched: ' + CAST(@Rc AS NVARCHAR(20));

    INSERT INTO #Unmapped (Kind, OldId, Detail)
    SELECT N'SerialAddressUnmapped', CAST(s.OrderDetail_Serial_ID AS BIGINT), N'Customer_Address_FK not in CustomerAddress.HtsId'
    FROM [TMS].[TotalSystem].[dbo].[Sale_OrderDetail_Serial] s
    WHERE s.Customer_Address_FK IS NOT NULL AND s.Customer_Address_FK <> 0
      AND NOT EXISTS (SELECT 1 FROM SLS.CustomerAddress a WHERE a.HtsId = s.Customer_Address_FK AND a.HtsId <> 0);

    UPDATE o SET o.CustomerId = c.Id
    FROM Sale.[Order] o
    INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_Order] s ON o.HtsId = CAST(s.Order_ID AS BIGINT) AND o.HtsId <> 0
    INNER JOIN SLS.Customer c ON c.HtsId = s.Customer_FK AND c.HtsId <> 0
    WHERE o.CustomerId IS NULL;
    PRINT N'Order.CustomerId filled: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    UPDATE sr SET
        sr.CustomerId = COALESCE(c.Id, sr.CustomerId),
        sr.CustomerAddressId = COALESCE(a.Id, sr.CustomerAddressId)
    FROM Sale.ServiceRequest sr
    INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_ServiceRequest] s
        ON sr.HtsId = CAST(s.ServiceRequest_ID AS BIGINT) AND sr.HtsId <> 0
    LEFT JOIN SLS.Customer c ON c.HtsId = s.Customer_FK AND c.HtsId <> 0
    LEFT JOIN SLS.CustomerAddress a ON a.HtsId = s.Customer_Address_FK AND a.HtsId <> 0
    WHERE (sr.CustomerId IS NULL AND c.Id IS NOT NULL)
       OR (sr.CustomerAddressId IS NULL AND a.Id IS NOT NULL);
    PRINT N'ServiceRequest customer/address filled: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    SELECT Kind, COUNT(*) AS Cnt FROM #Unmapped GROUP BY Kind ORDER BY Kind;
    SELECT TOP 20 * FROM #Unmapped ORDER BY Kind, OldId;

    SELECT
        (SELECT COUNT(*) FROM SLS.Customer) AS CustomerTotal,
        (SELECT COUNT(*) FROM SLS.Customer WHERE HtsId <> 0) AS CustomerWithHtsId,
        (SELECT COUNT(*) FROM SLS.Customer WHERE ISNULL(HtsId, 0) = 0) AS CustomerHtsIdNull,
        (SELECT COUNT(*) FROM SLS.CustomerAddress) AS AddressRows,
        (SELECT COUNT(*) FROM Sale.OrderDetailSerial WHERE HtsId <> 0 AND CustomerAddressId IS NOT NULL) AS SerialHtsWithAddr,
        (SELECT COUNT(*) FROM Sale.OrderDetailSerial WHERE HtsId <> 0 AND CustomerAddressId IS NULL) AS SerialHtsWithoutAddr;

    IF @Strict = 1 AND EXISTS (SELECT 1 FROM #Unmapped)
    BEGIN
        RAISERROR(N'@Strict=1 and unmapped/skipped rows remain.', 16, 1);
    END;

    COMMIT TRANSACTION;
    PRINT N'=== DONE SyncAfterSalesWidenCustomerHtsIdFromTotalSystem ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
