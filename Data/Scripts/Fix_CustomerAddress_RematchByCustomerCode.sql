/*
  Fix_CustomerAddress_RematchByCustomerCode.sql
  علت: Customer.HtsId در فاز ۱ با join غلط
    Party.HamkaranId = Crm_Customer.ManCompany_FK
  پر شد (HamkaranId در واقع Hamkaran_ManCompany_FK است).
  بعد MERGE سایت‌ها با c.HtsId = Address.Customer_FK سایت مشتری دیگری
  را به کد مشتری غلط چسباند (نمونه: کد ۳۵۸ ← پاک سلولز به‌جای مجتمع صنعتی رفسنجان).

  اصلاح مالکیت سایت با Customer.Code = HTS Customer_Code (کد در HTS یکتاست).
  اگر کد تکراری در هوایار باشد، ردیف اصلی (غیر sync-*) انتخاب می‌شود.
  عنوان/آدرس/گرید/منطقه/استان/نمایندگی از HTS همسان می‌شود.
  CustomerId درخواست پشتیبانی / تعمیر / سریال هم از HTS با همان کلید کد اصلاح
  می‌شود تا با سایت ناسازگار نماند.
  Encoding: UTF-8 with BOM. Linked server [TMS] لازم است.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'sync-customeraddress-rematch-code';
DECLARE @RcAddr INT = 0, @RcSr INT = 0, @RcRr INT = 0, @RcSerial INT = 0, @RcOrphan INT = 0;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

IF OBJECT_ID('tempdb..#CanonCustomer') IS NOT NULL DROP TABLE #CanonCustomer;
CREATE TABLE #CanonCustomer (CustomerId BIGINT NOT NULL PRIMARY KEY, Code INT NOT NULL UNIQUE);

INSERT INTO #CanonCustomer (CustomerId, Code)
SELECT x.Id, x.Code
FROM (
    SELECT
        c.Id,
        c.Code,
        ROW_NUMBER() OVER (
            PARTITION BY c.Code
            ORDER BY CASE WHEN ISNULL(c.CreatedByName, N'') LIKE N'sync-%' THEN 1 ELSE 0 END, c.Id
        ) AS Rn
    FROM SLS.Customer c
    WHERE c.Code IS NOT NULL
) x
WHERE x.Rn = 1;

PRINT N'=== BEFORE sample code 358 ===';
SELECT ca.HtsId, ca.Title, ca.Address, c.Code, p.FullName
FROM SLS.CustomerAddress ca
INNER JOIN SLS.Customer c ON c.Id = ca.CustomerId
LEFT JOIN Gnr.Party p ON p.Id = c.PartyId
WHERE c.Code = 358
ORDER BY ca.HtsId;

BEGIN TRANSACTION;
BEGIN TRY
    PRINT N'=== [1] Rematch CustomerAddress by HTS Customer_Code ===';
    UPDATE ca SET
        ca.CustomerId = s.CustomerId,
        ca.ZoneId = s.ZoneId,
        ca.ProvinceId = COALESCE(s.ProvinceId, ca.ProvinceId),
        ca.AgencyPartyId = COALESCE(s.AgencyPartyId, ca.AgencyPartyId),
        ca.AgencyDlId = COALESCE(s.AgencyDlId, ca.AgencyDlId),
        ca.Title = s.Title,
        ca.Address = s.Address,
        ca.Grade = s.Grade,
        ca.HamkaranAddId = s.HamkaranAddId,
        ca.RahkaranId = s.RahkaranId,
        ca.RahkaranVersion = s.RahkaranVersion,
        ca.ModifiedById = 1,
        ca.ModifiedByName = @SeedUser,
        ca.ModifiedDateMiladiDateTime = @Now,
        ca.ModifiedDateShamsiDateTime = @NowShamsi
    FROM SLS.CustomerAddress ca
    INNER JOIN (
        SELECT
            CAST(a.Customer_Address_ID AS BIGINT) AS HtsId,
            cn.CustomerId,
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
        INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] cc ON cc.Customer_ID = a.Customer_FK
        INNER JOIN #CanonCustomer cn ON cn.Code = CAST(cc.Customer_Code AS INT)
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
    ) s ON s.HtsId = ca.HtsId
    WHERE ISNULL(ca.CustomerId, 0) <> s.CustomerId
       OR ISNULL(ca.Title, N'') <> ISNULL(s.Title, N'')
       OR ISNULL(ca.Address, N'') <> ISNULL(s.Address, N'')
       OR ISNULL(ca.Grade, -1) <> ISNULL(s.Grade, -1)
       OR ISNULL(ca.ZoneId, 0) <> ISNULL(s.ZoneId, 0)
       OR ISNULL(ca.HamkaranAddId, 0) <> ISNULL(s.HamkaranAddId, 0)
       OR ISNULL(ca.RahkaranId, 0) <> ISNULL(s.RahkaranId, 0)
       OR ISNULL(ca.RahkaranVersion, 0) <> ISNULL(s.RahkaranVersion, 0)
       OR (s.ProvinceId IS NOT NULL AND ISNULL(ca.ProvinceId, 0) <> s.ProvinceId)
       OR (s.AgencyDlId IS NOT NULL AND ISNULL(ca.AgencyDlId, 0) <> s.AgencyDlId)
       OR (s.AgencyPartyId IS NOT NULL AND ISNULL(ca.AgencyPartyId, 0) <> s.AgencyPartyId);
    SET @RcAddr = @@ROWCOUNT;
    PRINT N'  CustomerAddress updated: ' + CAST(@RcAddr AS nvarchar(20));

    PRINT N'=== [2] Rematch ServiceRequest.CustomerId / CustomerAddressId ===';
    UPDATE sr SET
        sr.CustomerId = cn.CustomerId,
        sr.CustomerAddressId = COALESCE(addr.Id, sr.CustomerAddressId),
        sr.ModifiedById = 1,
        sr.ModifiedByName = @SeedUser,
        sr.ModifiedDateMiladiDateTime = @Now,
        sr.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sale.ServiceRequest sr
    INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_ServiceRequest] h
        ON CAST(h.ServiceRequest_ID AS BIGINT) = sr.HtsId AND sr.HtsId <> 0
    INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] cc ON cc.Customer_ID = h.Customer_FK
    INNER JOIN #CanonCustomer cn ON cn.Code = CAST(cc.Customer_Code AS INT)
    LEFT JOIN SLS.CustomerAddress addr
        ON addr.HtsId = CAST(h.Customer_Address_FK AS BIGINT) AND h.Customer_Address_FK IS NOT NULL
    WHERE ISNULL(sr.CustomerId, 0) <> cn.CustomerId
       OR (addr.Id IS NOT NULL AND ISNULL(sr.CustomerAddressId, 0) <> addr.Id);
    SET @RcSr = @@ROWCOUNT;
    PRINT N'  ServiceRequest updated: ' + CAST(@RcSr AS nvarchar(20));

    PRINT N'=== [3] Rematch RepairRequest.CustomerId / CustomerAddressId ===';
    UPDATE r SET
        r.CustomerId = cn.CustomerId,
        r.CustomerAddressId = COALESCE(addr.Id, r.CustomerAddressId),
        r.ModifiedById = 1,
        r.ModifiedByName = @SeedUser,
        r.ModifiedDateMiladiDateTime = @Now,
        r.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Rpr.RepairRequest r
    INNER JOIN [TMS].[TotalSystem].[dbo].[Rpr_RepairRequest] h
        ON CAST(h.RepairRequest_ID AS BIGINT) = r.HtsId AND r.HtsId <> 0
    INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] cc ON cc.Customer_ID = h.Customer_FK
    INNER JOIN #CanonCustomer cn ON cn.Code = CAST(cc.Customer_Code AS INT)
    LEFT JOIN SLS.CustomerAddress addr
        ON addr.HtsId = CAST(h.CustomerAddressId AS BIGINT) AND h.CustomerAddressId IS NOT NULL
    WHERE ISNULL(r.CustomerId, 0) <> cn.CustomerId
       OR (addr.Id IS NOT NULL AND ISNULL(r.CustomerAddressId, 0) <> addr.Id);
    SET @RcRr = @@ROWCOUNT;
    PRINT N'  RepairRequest updated: ' + CAST(@RcRr AS nvarchar(20));

    PRINT N'=== [4] Rematch OrderDetailSerial.CustomerId / CustomerAddressId ===';
    UPDATE s SET
        s.CustomerId = COALESCE(cn.CustomerId, addr.CustomerId, s.CustomerId),
        s.CustomerAddressId = COALESCE(addr.Id, s.CustomerAddressId),
        s.ModifiedById = 1,
        s.ModifiedByName = @SeedUser,
        s.ModifiedDateMiladiDateTime = @Now,
        s.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sale.OrderDetailSerial s
    INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_OrderDetail_Serial] h
        ON CAST(h.OrderDetail_Serial_ID AS BIGINT) = s.HtsId AND s.HtsId <> 0
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] cc ON cc.Customer_ID = h.NewCustomer_FK
    LEFT JOIN #CanonCustomer cn ON cn.Code = CAST(cc.Customer_Code AS INT)
    LEFT JOIN SLS.CustomerAddress addr
        ON addr.HtsId = CAST(h.Customer_Address_FK AS BIGINT) AND h.Customer_Address_FK IS NOT NULL
    WHERE (cn.CustomerId IS NOT NULL AND ISNULL(s.CustomerId, 0) <> cn.CustomerId)
       OR (cn.CustomerId IS NULL AND addr.CustomerId IS NOT NULL AND ISNULL(s.CustomerId, 0) <> addr.CustomerId)
       OR (addr.Id IS NOT NULL AND ISNULL(s.CustomerAddressId, 0) <> addr.Id);
    SET @RcSerial = @@ROWCOUNT;
    PRINT N'  OrderDetailSerial updated: ' + CAST(@RcSerial AS nvarchar(20));

    PRINT N'=== [5] Havayar-only docs whose site moved to another customer ===';
    ;WITH pick AS (
        SELECT
            x.CustomerId,
            x.Id AS AddressId,
            ROW_NUMBER() OVER (
                PARTITION BY x.CustomerId
                ORDER BY CASE WHEN x.Title = p.FullName THEN 0 ELSE 1 END, x.HtsId
            ) AS Rn
        FROM SLS.CustomerAddress x
        INNER JOIN SLS.Customer c ON c.Id = x.CustomerId
        LEFT JOIN Gnr.Party p ON p.Id = c.PartyId
    )
    UPDATE sr SET
        sr.CustomerAddressId = pick.AddressId,
        sr.ModifiedById = 1,
        sr.ModifiedByName = @SeedUser,
        sr.ModifiedDateMiladiDateTime = @Now,
        sr.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sale.ServiceRequest sr
    INNER JOIN SLS.CustomerAddress cur ON cur.Id = sr.CustomerAddressId
    INNER JOIN pick ON pick.CustomerId = sr.CustomerId AND pick.Rn = 1
    WHERE ISNULL(sr.HtsId, 0) = 0
      AND cur.CustomerId <> sr.CustomerId;
    SET @RcOrphan = @@ROWCOUNT;

    UPDATE r SET
        r.CustomerAddressId = pick.AddressId,
        r.ModifiedById = 1,
        r.ModifiedByName = @SeedUser,
        r.ModifiedDateMiladiDateTime = @Now,
        r.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Rpr.RepairRequest r
    INNER JOIN SLS.CustomerAddress cur ON cur.Id = r.CustomerAddressId
    INNER JOIN (
        SELECT
            x.CustomerId,
            x.Id AS AddressId,
            ROW_NUMBER() OVER (
                PARTITION BY x.CustomerId
                ORDER BY CASE WHEN x.Title = p.FullName THEN 0 ELSE 1 END, x.HtsId
            ) AS Rn
        FROM SLS.CustomerAddress x
        INNER JOIN SLS.Customer c ON c.Id = x.CustomerId
        LEFT JOIN Gnr.Party p ON p.Id = c.PartyId
    ) pick ON pick.CustomerId = r.CustomerId AND pick.Rn = 1
    WHERE ISNULL(r.HtsId, 0) = 0
      AND cur.CustomerId <> r.CustomerId;
    SET @RcOrphan = @RcOrphan + @@ROWCOUNT;

    UPDATE s SET
        s.CustomerAddressId = pick.AddressId,
        s.ModifiedById = 1,
        s.ModifiedByName = @SeedUser,
        s.ModifiedDateMiladiDateTime = @Now,
        s.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sale.OrderDetailSerial s
    INNER JOIN SLS.CustomerAddress cur ON cur.Id = s.CustomerAddressId
    INNER JOIN (
        SELECT
            x.CustomerId,
            x.Id AS AddressId,
            ROW_NUMBER() OVER (
                PARTITION BY x.CustomerId
                ORDER BY CASE WHEN x.Title = p.FullName THEN 0 ELSE 1 END, x.HtsId
            ) AS Rn
        FROM SLS.CustomerAddress x
        INNER JOIN SLS.Customer c ON c.Id = x.CustomerId
        LEFT JOIN Gnr.Party p ON p.Id = c.PartyId
    ) pick ON pick.CustomerId = s.CustomerId AND pick.Rn = 1
    WHERE ISNULL(s.HtsId, 0) = 0
      AND cur.CustomerId <> s.CustomerId;
    SET @RcOrphan = @RcOrphan + @@ROWCOUNT;
    PRINT N'  orphan site reassigned: ' + CAST(@RcOrphan AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    PRINT N'=== ERROR rolled back ===';
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
    RETURN;
END CATCH;

SELECT
    @RcAddr AS AddressUpdated,
    @RcSr AS ServiceRequestUpdated,
    @RcRr AS RepairRequestUpdated,
    @RcSerial AS SerialUpdated,
    @RcOrphan AS OrphanSiteReassigned;

PRINT N'=== AFTER sample code 358 ===';
SELECT ca.HtsId, ca.Title, ca.Address, c.Code, p.FullName
FROM SLS.CustomerAddress ca
INNER JOIN SLS.Customer c ON c.Id = ca.CustomerId
LEFT JOIN Gnr.Party p ON p.Id = c.PartyId
WHERE c.Code = 358
ORDER BY ca.HtsId;
