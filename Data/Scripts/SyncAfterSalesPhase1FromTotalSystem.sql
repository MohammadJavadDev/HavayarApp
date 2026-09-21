/*
================================================================================
SyncAfterSalesPhase1FromTotalSystem.sql
================================================================================
قسمت ۱: Zone، Customer.HtsId (ManCompany PK، Hamkaran_ManCompany_FK، Code، NationalID)، CustomerAddress، Order/Detail/Serial (+ پیوست)، Branch.HtsId

پیش‌نیاز: Linked Server [TMS]، migration جداول/ستون‌های HtsId، Seed منو.
Idempotent: MERGE روی HtsId (HtsId <> 0).
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'sync-after-sales-phase1';

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

    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    IF OBJECT_ID('tempdb..#Unmapped') IS NOT NULL DROP TABLE #Unmapped;
    CREATE TABLE #Unmapped (Kind NVARCHAR(80) NOT NULL, OldId BIGINT NULL, Detail NVARCHAR(400) NULL);

    SELECT
        CAST(ou.User_ID AS BIGINT) AS OldUserId,
        MIN(nu.Id) AS NewUserId,
        MAX(COALESCE(NULLIF(LTRIM(RTRIM(nu.NameFa)), N''), nu.Name, nu.Username)) AS NewUserName
    INTO #UserMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
    INNER JOIN [system].[User] nu
        ON nu.Username = ou.Username
        OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
            AND nu.Username = LTRIM(RTRIM(ou.ActiveDirectoryUsername)))
    GROUP BY CAST(ou.User_ID AS BIGINT);

    PRINT N'=== Branch.HtsId ===';
    UPDATE b SET b.HtsId = CAST(s.Branch_ID AS BIGINT)
    FROM Sale.Branch b
    INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_Branch] s
        ON LTRIM(RTRIM(b.Title)) = LTRIM(RTRIM(s.Branch_Title))
    WHERE ISNULL(b.HtsId, 0) = 0;

    PRINT N'=== Customer.HtsId step1 DISABLED ===';
    PRINT N'    Do not match Party.HamkaranId to Crm_Customer.ManCompany_FK (HTS company PK).';
    PRINT N'    Incident 2026-09-21: Code 358 رفسنجان got HtsId 614; HtsId 482 landed on پارت سازان.';

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

    PRINT N'=== Crm.Zone ===';
    MERGE Crm.Zone AS t
    USING (
        SELECT
            CAST(s.Zone_ID AS BIGINT) AS HtsId,
            NULLIF(LTRIM(RTRIM(s.Zone_Title)), N'') AS Title,
            um.NewUserId AS SupervisorUserId,
            CAST(s.ZoneType_FK AS INT) AS ZoneType
        FROM [TMS].[TotalSystem].[dbo].[Crm_Zone] s
        LEFT JOIN #UserMap um ON um.OldUserId = s.Zone_SupervisorUser_FK
    ) AS s
    ON t.HtsId = s.HtsId
    WHEN MATCHED THEN UPDATE SET
        t.Title = s.Title, t.SupervisorUserId = s.SupervisorUserId, t.ZoneType = s.ZoneType,
        t.ModifiedById = 1, t.ModifiedByName = @SeedUser,
        t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT
        (HtsId, Title, SupervisorUserId, ZoneType,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (s.HtsId, ISNULL(s.Title, N''), s.SupervisorUserId, s.ZoneType,
         1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

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

    INSERT INTO #Unmapped (Kind, OldId, Detail)
    SELECT N'CustomerAddress', CAST(a.Customer_Address_ID AS BIGINT), N'Customer not mapped'
    FROM [TMS].[TotalSystem].[dbo].[Crm_Customer_Address] a
    WHERE NOT EXISTS (SELECT 1 FROM SLS.Customer c WHERE c.HtsId = a.Customer_FK AND c.HtsId <> 0);

    PRINT N'=== Sale.Order stamp HtsId by HamkaranId=VchHdrId ===';
    ;WITH cand AS (
        SELECT
            o.Id,
            CAST(s.Order_ID AS BIGINT) AS HtsId,
            CAST(s.VchNo AS BIGINT) AS HtsVchNo,
            CAST(s.Stock_FK AS BIGINT) AS HtsStockId,
            CAST(s.BaseVchType_FK AS BIGINT) AS HtsBaseVoucherTypeId,
            s.ContractVchHeaderId AS HtsContractVchHeaderId,
            umc.NewUserId AS ConfirmerUserId,
            umo.NewUserId AS OperatorUserId,
            b.Id AS BranchId,
            ROW_NUMBER() OVER (PARTITION BY CAST(s.Order_ID AS BIGINT) ORDER BY o.Id) AS RnHts,
            ROW_NUMBER() OVER (PARTITION BY o.Id ORDER BY CAST(s.Order_ID AS BIGINT)) AS RnOrd
        FROM Sale.[Order] o
        INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_Order] s ON o.HamkaranId = s.VchHdrId
        LEFT JOIN Sale.Branch b ON b.HtsId = s.Branch_FK AND b.HtsId <> 0
        LEFT JOIN #UserMap umc ON umc.OldUserId = s.Confirmer_FK
        LEFT JOIN #UserMap umo ON umo.OldUserId = s.Operator_FK
        WHERE ISNULL(o.HtsId, 0) = 0
          AND NOT EXISTS (
              SELECT 1 FROM Sale.[Order] x
              WHERE x.HtsId = CAST(s.Order_ID AS BIGINT) AND x.HtsId <> 0)
    )
    UPDATE o SET
        o.HtsId = c.HtsId,
        o.HtsVchNo = c.HtsVchNo,
        o.HtsStockId = c.HtsStockId,
        o.HtsBaseVoucherTypeId = c.HtsBaseVoucherTypeId,
        o.HtsContractVchHeaderId = c.HtsContractVchHeaderId,
        o.ConfirmerUserId = COALESCE(c.ConfirmerUserId, o.ConfirmerUserId),
        o.OperatorUserId = COALESCE(c.OperatorUserId, o.OperatorUserId),
        o.BranchId = COALESCE(c.BranchId, o.BranchId),
        o.ModifiedById = 1, o.ModifiedByName = @SeedUser,
        o.ModifiedDateMiladiDateTime = @Now, o.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sale.[Order] o
    INNER JOIN cand c ON c.Id = o.Id AND c.RnHts = 1 AND c.RnOrd = 1;

    PRINT N'=== Sale.Order ===';
    MERGE Sale.[Order] AS t
    USING (
        SELECT
            CAST(s.Order_ID AS BIGINT) AS HtsId,
            CAST(s.Year AS NVARCHAR(20)) AS [Year],
            s.VchDate AS VchDateMiladiDate,
            NULLIF(LTRIM(RTRIM(s.VchDate_Shamsi)), N'') AS VchDateShamsiDate,
            c.Id AS CustomerId,
            CAST(s.VchType_FK AS INT) AS VoucherTypeStatus,
            CAST(s.Status_FK AS INT) AS StatusEnum,
            b.Id AS BranchId,
            NULLIF(LTRIM(RTRIM(s.Contract_Number)), N'') AS ContractNumber,
            s.VchHdrId AS HamkaranId,
            CAST(s.VchNo AS BIGINT) AS OrderNumber,
            CAST(s.VchNo AS BIGINT) AS HtsVchNo,
            CAST(s.Stock_FK AS BIGINT) AS HtsStockId,
            CAST(s.BaseVchType_FK AS BIGINT) AS HtsBaseVoucherTypeId,
            s.ContractVchHeaderId AS HtsContractVchHeaderId,
            umc.NewUserId AS ConfirmerUserId,
            umo.NewUserId AS OperatorUserId
        FROM [TMS].[TotalSystem].[dbo].[Sale_Order] s
        LEFT JOIN SLS.Customer c ON c.HtsId = s.Customer_FK AND c.HtsId <> 0
        LEFT JOIN Sale.Branch b ON b.HtsId = s.Branch_FK AND b.HtsId <> 0
        LEFT JOIN #UserMap umc ON umc.OldUserId = s.Confirmer_FK
        LEFT JOIN #UserMap umo ON umo.OldUserId = s.Operator_FK
    ) AS s
    ON t.HtsId = s.HtsId
    WHEN MATCHED THEN UPDATE SET
        t.[Year] = s.[Year], t.VchDateMiladiDate = s.VchDateMiladiDate, t.VchDateShamsiDate = s.VchDateShamsiDate,
        t.CustomerId = s.CustomerId, t.VoucherTypeStatus = s.VoucherTypeStatus, t.StatusEnum = s.StatusEnum,
        t.BranchId = s.BranchId, t.ContractNumber = s.ContractNumber, t.HamkaranId = s.HamkaranId,
        t.OrderNumber = s.OrderNumber, t.HtsVchNo = s.HtsVchNo, t.HtsStockId = s.HtsStockId,
        t.HtsBaseVoucherTypeId = s.HtsBaseVoucherTypeId, t.HtsContractVchHeaderId = s.HtsContractVchHeaderId,
        t.ConfirmerUserId = s.ConfirmerUserId, t.OperatorUserId = s.OperatorUserId,
        t.ModifiedById = 1, t.ModifiedByName = @SeedUser,
        t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT
        (HtsId, [Year], VchDateMiladiDate, VchDateShamsiDate, CustomerId, VoucherTypeStatus, StatusEnum,
         BranchId, ContractNumber, HamkaranId, OrderNumber, HtsVchNo, HtsStockId, HtsBaseVoucherTypeId,
         HtsContractVchHeaderId, ConfirmerUserId, OperatorUserId,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (s.HtsId, s.[Year], s.VchDateMiladiDate, s.VchDateShamsiDate, s.CustomerId, s.VoucherTypeStatus, s.StatusEnum,
         s.BranchId, s.ContractNumber, s.HamkaranId, s.OrderNumber, s.HtsVchNo, s.HtsStockId, s.HtsBaseVoucherTypeId,
         s.HtsContractVchHeaderId, s.ConfirmerUserId, s.OperatorUserId,
         1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

    PRINT N'=== Sale.OrderDetail stamp HtsId by HamkaranId=Hamkaran_Vchitm_FK ===';
    ;WITH cand AS (
        SELECT
            d.Id,
            CAST(s.OrderDetail_ID AS BIGINT) AS HtsId,
            s.Hamkaran_InvVchitm_FK AS HamkaranInvVchItmId,
            s.IsPackage,
            s.NotComplete,
            o.Id AS SaleOrderId,
            ROW_NUMBER() OVER (PARTITION BY CAST(s.OrderDetail_ID AS BIGINT) ORDER BY d.Id) AS RnHts,
            ROW_NUMBER() OVER (PARTITION BY d.Id ORDER BY CAST(s.OrderDetail_ID AS BIGINT)) AS RnDet
        FROM Sale.OrderDetail d
        INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_OrderDetail] s ON d.HamkaranId = s.Hamkaran_Vchitm_FK
        LEFT JOIN Sale.[Order] o ON o.HtsId = s.Order_FK AND o.HtsId <> 0
        WHERE ISNULL(d.HtsId, 0) = 0
          AND NOT EXISTS (
              SELECT 1 FROM Sale.OrderDetail x
              WHERE x.HtsId = CAST(s.OrderDetail_ID AS BIGINT) AND x.HtsId <> 0)
    )
    UPDATE d SET
        d.HtsId = c.HtsId,
        d.HamkaranInvVchItmId = c.HamkaranInvVchItmId,
        d.IsPackage = c.IsPackage,
        d.NotComplete = c.NotComplete,
        d.Sale_OrderId = COALESCE(c.SaleOrderId, d.Sale_OrderId),
        d.ModifiedById = 1, d.ModifiedByName = @SeedUser,
        d.ModifiedDateMiladiDateTime = @Now, d.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sale.OrderDetail d
    INNER JOIN cand c ON c.Id = d.Id AND c.RnHts = 1 AND c.RnDet = 1;

    PRINT N'=== Sale.OrderDetail ===';
    MERGE Sale.OrderDetail AS t
    USING (
        SELECT
            CAST(d.OrderDetail_ID AS BIGINT) AS HtsId,
            o.Id AS SaleOrderId,
            d.Hamkaran_Vchitm_FK AS HamkaranId,
            d.Hamkaran_VchHdr_FK AS HamkaranOrderHdrId,
            d.Hamkaran_InvVchitm_FK AS HamkaranInvVchItmId,
            CAST(d.Prefactor_VchNo AS BIGINT) AS Prefactor_VchNo,
            CAST(d.Seq AS INT) AS Seq,
            p.Id AS PartId,
            d.Qty,
            d.BaseVchItmRef,
            d.Description,
            d.UnitPrice, d.Price, d.IncPrice, d.DecPrice,
            CAST(d.PayMethod_FK AS INT) AS PayMethod,
            d.PriceTemp, d.QtyTemp,
            d.IsPackage, d.NotComplete,
            o.CustomerId
        FROM [TMS].[TotalSystem].[dbo].[Sale_OrderDetail] d
        INNER JOIN Sale.[Order] o ON o.HtsId = d.Order_FK AND o.HtsId <> 0
        LEFT JOIN Inv.Part p ON p.HtsId = d.Part_FK AND p.HtsId <> 0
    ) AS s
    ON t.HtsId = s.HtsId
    WHEN MATCHED THEN UPDATE SET
        t.Sale_OrderId = s.SaleOrderId, t.HamkaranId = s.HamkaranId, t.HamkaranOrderHdrId = s.HamkaranOrderHdrId,
        t.HamkaranInvVchItmId = s.HamkaranInvVchItmId, t.Prefactor_VchNo = s.Prefactor_VchNo, t.Seq = s.Seq,
        t.PartId = s.PartId, t.Qty = s.Qty, t.BaseVchItmRef = s.BaseVchItmRef, t.Description = s.Description,
        t.UnitPrice = s.UnitPrice, t.Price = s.Price, t.IncPrice = s.IncPrice, t.DecPrice = s.DecPrice,
        t.PayMethod = s.PayMethod, t.PriceTemp = s.PriceTemp, t.QtyTemp = ISNULL(s.QtyTemp, 0),
        t.IsPackage = s.IsPackage, t.NotComplete = s.NotComplete, t.CustomerId = s.CustomerId,
        t.ModifiedById = 1, t.ModifiedByName = @SeedUser,
        t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT
        (HtsId, Sale_OrderId, HamkaranId, HamkaranOrderHdrId, HamkaranInvVchItmId, Prefactor_VchNo, Seq, PartId,
         Qty, BaseVchItmRef, Description, UnitPrice, Price, IncPrice, DecPrice, PayMethod, PriceTemp, QtyTemp,
         IsPackage, NotComplete, CustomerId,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (s.HtsId, s.SaleOrderId, s.HamkaranId, s.HamkaranOrderHdrId, s.HamkaranInvVchItmId, s.Prefactor_VchNo, s.Seq, s.PartId,
         s.Qty, s.BaseVchItmRef, s.Description, s.UnitPrice, s.Price, s.IncPrice, s.DecPrice, s.PayMethod, s.PriceTemp, ISNULL(s.QtyTemp, 0),
         s.IsPackage, s.NotComplete, s.CustomerId,
         1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

    PRINT N'=== Pln.Project.HtsId column ===';
    IF COL_LENGTH(N'Pln.Project', N'HtsId') IS NULL
    BEGIN
        ALTER TABLE Pln.Project ADD HtsId BIGINT NOT NULL CONSTRAINT DF_Pln_Project_HtsId DEFAULT (0);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pln_Project_HtsId' AND object_id = OBJECT_ID(N'Pln.Project'))
    BEGIN
        CREATE UNIQUE INDEX IX_Pln_Project_HtsId ON Pln.Project (HtsId) WHERE [HtsId] <> CAST(0 AS bigint);
    END;

    PRINT N'=== Pln.Project from HTS Pln_Project (referenced by serials) ===';
    MERGE Pln.Project AS t
    USING (
        SELECT
            CAST(p.Project_ID AS BIGINT) AS HtsId,
            NULLIF(LTRIM(RTRIM(p.Project_Code)), N'') AS Code,
            NULLIF(LTRIM(RTRIM(p.Project_Name)), N'') AS Name,
            NULLIF(LTRIM(RTRIM(p.Project_Startdate)), N'') AS ProjectStartShamsiDate,
            NULLIF(LTRIM(RTRIM(p.Project_Enddate)), N'') AS ProjectEndShamsiDate,
            NULLIF(LTRIM(RTRIM(p.Project_Description)), N'') AS Description,
            CASE WHEN p.Project_Status_FK BETWEEN 0 AND 7 THEN p.Project_Status_FK ELSE 0 END AS ProjectStatus,
            CASE WHEN p.IsActive = 1 THEN 1 ELSE 0 END AS IsActive
        FROM [TMS].[TotalSystem].[dbo].[Pln_Project] p
        WHERE p.Project_ID IN (
            SELECT DISTINCT s.Pln_Project_FK
            FROM [TMS].[TotalSystem].[dbo].[Sale_OrderDetail_Serial] s
            WHERE s.Pln_Project_FK IS NOT NULL AND s.Pln_Project_FK <> 0
        )
    ) AS s
    ON t.HtsId = s.HtsId
    WHEN MATCHED THEN UPDATE SET
        t.Code = s.Code, t.Name = s.Name,
        t.ProjectStartShamsiDate = s.ProjectStartShamsiDate,
        t.ProjectEndShamsiDate = s.ProjectEndShamsiDate,
        t.Description = s.Description, t.ProjectStatus = s.ProjectStatus,
        t.IsActive = s.IsActive,
        t.ModifiedById = 1, t.ModifiedByName = @SeedUser,
        t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT
        (HtsId, Code, Name, ProjectStartShamsiDate, ProjectEndShamsiDate, Description, ProjectStatus, IsActive,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    VALUES
        (s.HtsId, s.Code, s.Name, s.ProjectStartShamsiDate, s.ProjectEndShamsiDate, s.Description, s.ProjectStatus, s.IsActive,
         1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi);

    PRINT N'=== Sale.OrderDetailSerial ===';
    MERGE Sale.OrderDetailSerial AS t
    USING (
        SELECT
            CAST(s.OrderDetail_Serial_ID AS BIGINT) AS HtsId,
            d.Id AS OrderDetailId,
            c.Id AS CustomerId,
            addr.Id AS CustomerAddressId,
            pr.Id AS PlaningProjectId,
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
        LEFT JOIN Pln.Project pr ON pr.HtsId = CAST(s.Pln_Project_FK AS BIGINT) AND pr.HtsId <> 0
    ) AS s
    ON t.HtsId = s.HtsId
    WHEN MATCHED THEN UPDATE SET
        t.OrderDetailId = s.OrderDetailId, t.CustomerId = s.CustomerId, t.CustomerAddressId = s.CustomerAddressId,
        t.PlaningProjectId = s.PlaningProjectId,
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
        (HtsId, OrderDetailId, CustomerId, CustomerAddressId, PlaningProjectId, Serial, Model, HoursDaily, Runtime, LifeTime,
         GuaranteeTime, GuaranteeSendDay, GuaranteeLaunchDay, HasInsurance, SendMiladiDate, SendShamsiDate, SendTime,
         LaunchMiladiDate, LaunchShamsiDate, ExitFactoryMiladiDate, ExitFactoryShamsiDate, ExitFactoryTime,
         ProductionOrderNumber, IndustrialComment, InvComment, IndustrialConfirm,
         FinalExitMiladiDate, FinalExitShamsiDate, ExitTimeFinal,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (s.HtsId, s.OrderDetailId, s.CustomerId, s.CustomerAddressId, s.PlaningProjectId, s.Serial, s.Model, s.HoursDaily, s.Runtime, s.LifeTime,
         s.GuaranteeTime, s.GuaranteeSendDay, s.GuaranteeLaunchDay, s.HasInsurance, s.SendMiladiDate, s.SendShamsiDate, s.SendTime,
         s.LaunchMiladiDate, s.LaunchShamsiDate, s.ExitFactoryMiladiDate, s.ExitFactoryShamsiDate, s.ExitFactoryTime,
         s.ProductionOrderNumber, s.IndustrialComment, s.InvComment, s.IndustrialConfirm,
         s.FinalExitMiladiDate, s.FinalExitShamsiDate, s.ExitTimeFinal,
         1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

    PRINT N'=== OrderDetailSerialAttachment (UNC only) ===';
    MERGE Sale.OrderDetailSerialAttachment AS t
    USING (
        SELECT
            CAST(a.Sale_OrderDetail_Serial_Attachment_ID AS BIGINT) AS HtsId,
            s.Id AS OrderDetailSerialId,
            NULLIF(LTRIM(RTRIM(a.Attachment_FileName)), N'') AS Title,
            NULLIF(LTRIM(RTRIM(a.Attachment_Comment)), N'') AS Comment,
            NULLIF(LTRIM(RTRIM(a.AttachmentFilePath)), N'') AS PhysicalPath,
            ISNULL(a.Attachment_FileSize, 0) AS Size
        FROM [TMS].[TotalSystem].[dbo].[Sale_OrderDetail_Serial_Attachment] a
        INNER JOIN Sale.OrderDetailSerial s ON s.HtsId = a.Sale_OrderDetail_Serial_FK AND s.HtsId <> 0
    ) AS s
    ON t.HtsId = s.HtsId
    WHEN MATCHED THEN UPDATE SET
        t.OrderDetailSerialId = s.OrderDetailSerialId, t.Title = s.Title, t.Comment = s.Comment,
        t.ModifiedById = 1, t.ModifiedByName = @SeedUser,
        t.ModifiedDateMiladiDateTime = @Now, t.ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT
        (HtsId, OrderDetailSerialId, Title, Comment,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (s.HtsId, s.OrderDetailSerialId, s.Title, s.Comment,
         1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

    SELECT Kind, COUNT(*) AS Cnt FROM #Unmapped GROUP BY Kind;
    SELECT TOP 20 * FROM #Unmapped;

    COMMIT TRANSACTION;
    PRINT N'=== DONE SyncAfterSalesPhase1FromTotalSystem ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH
