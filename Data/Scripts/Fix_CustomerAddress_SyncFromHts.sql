/*
  Fix_CustomerAddress_SyncFromHts.sql
  Additive: widen/insert SLS.Customer for missing HTS address owners, then MERGE
  SLS.CustomerAddress with HTS joins:
    Owner: Customer.Code = Crm_Customer.Customer_Code (canonical row; not Customer.HtsId).
      HtsId step1 (Party.HamkaranId = ManCompany_FK) is off-by-company and must not
      drive site ownership — see Fix_CustomerAddress_RematchByCustomerCode.sql.
    Province: Gnr_Province.Province_Title → Gnr.Region.Name (استان X / تهران variants)
    AgencyDl: FIN.DL.HamkaranId = Acc_DL.Hamkaran_Acc_DL_FK
    AgencyParty: Party.HamkaranId = Gnr_ManCompany.Hamkaran_ManCompany_FK
  Does not insert addresses without a customer. Does not insert a second Customer
  when Code already exists. Encoding: UTF-8 with BOM.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'sync-customeraddress-hts-fix';

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

PRINT N'=== BEFORE ===';
SELECT
    (SELECT COUNT(*) FROM SLS.Customer) AS CustomerTotal,
    (SELECT COUNT(*) FROM SLS.Customer WHERE ISNULL(HtsId,0) <> 0) AS CustomerHts,
    (SELECT COUNT(*) FROM SLS.CustomerAddress) AS AddrTotal,
    (SELECT COUNT(*) FROM SLS.CustomerAddress WHERE ISNULL(ProvinceId,0) <> 0) AS AddrProvince,
    (SELECT COUNT(*) FROM SLS.CustomerAddress WHERE ISNULL(AgencyDlId,0) <> 0) AS AddrAgencyDl,
    (SELECT COUNT(*) FROM SLS.CustomerAddress WHERE ISNULL(AgencyPartyId,0) <> 0) AS AddrAgencyParty,
    (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Crm_Customer_Address]) AS HtsAddr,
    (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Crm_Customer_Address] a
      WHERE NOT EXISTS (SELECT 1 FROM SLS.Customer c WHERE c.HtsId = a.Customer_FK AND ISNULL(c.HtsId,0) <> 0)
    ) AS UnmappedAddr;

BEGIN TRANSACTION;
BEGIN TRY
    PRINT N'=== [1] Customer.HtsId remaining zero on unique Party/Hamkaran ===';
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
    PRINT N'  HtsId updates: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== [2] Insert Party for HTS address-owners with no Party ===';
    INSERT INTO Gnr.Party
        (FullName, CompanyName, NationalID, Type, HamkaranId,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT
        LEFT(ISNULL(NULLIF(LTRIM(RTRIM(mc.FullName)), N''), ISNULL(NULLIF(LTRIM(RTRIM(mc.CompanyName)), N''), N'مشتری HTS ' + CAST(cc.Customer_ID AS nvarchar(20)))), 101),
        LEFT(NULLIF(LTRIM(RTRIM(mc.CompanyName)), N''), 100),
        LEFT(NULLIF(LTRIM(RTRIM(mc.NationalCode)), N''), 20),
        1,
        NULLIF(mc.Hamkaran_ManCompany_FK, 0),
        1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[Crm_Customer_Address] a
    INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] cc ON cc.Customer_ID = a.Customer_FK
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] mc ON mc.ManCompany_ID = cc.ManCompany_FK
    WHERE NOT EXISTS (SELECT 1 FROM SLS.Customer c WHERE c.HtsId = CAST(cc.Customer_ID AS BIGINT) AND ISNULL(c.HtsId,0) <> 0)
      AND NOT EXISTS (
          SELECT 1 FROM Gnr.Party p
          WHERE ISNULL(mc.Hamkaran_ManCompany_FK, 0) <> 0 AND p.HamkaranId = mc.Hamkaran_ManCompany_FK
      );
    PRINT N'  Parties inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== [3] Insert SLS.Customer for unmapped HTS address-owners ===';
    INSERT INTO SLS.Customer
        (Code, PartyId, HamkaranId, HtsId,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT
        TRY_CAST(cc.Customer_Code AS INT),
        p.Id,
        -- HamkaranId = شناسه مشتری راهکاران، نه Party.HamkaranId (برخورد Key و خرابی جاب‌ها).
        -- اگر مشتری راهکاران یکتا نبود، HtsId+۱۰۰۰۰۰۰۰۰۰ تا با CustomerID واقعی برخورد نکند.
        ISNULL((
            SELECT MIN(rc.CustomerID)
            FROM [ERPS].[Erps].[SLS3].[Customer] rc
            WHERE rc.PartyRef = p.HamkaranId
            GROUP BY rc.PartyRef
            HAVING COUNT(*) = 1
        ), CAST(cc.Customer_ID AS BIGINT) + 1000000000),
        CAST(cc.Customer_ID AS BIGINT),
        1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1
    FROM [TMS].[TotalSystem].[dbo].[Crm_Customer_Address] a
    INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] cc ON cc.Customer_ID = a.Customer_FK
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] mc ON mc.ManCompany_ID = cc.ManCompany_FK
    INNER JOIN Gnr.Party p ON (
        (ISNULL(mc.Hamkaran_ManCompany_FK, 0) <> 0 AND p.HamkaranId = mc.Hamkaran_ManCompany_FK)
    )
    WHERE NOT EXISTS (
        SELECT 1 FROM SLS.Customer c
        WHERE c.HtsId = CAST(cc.Customer_ID AS BIGINT) AND ISNULL(c.HtsId, 0) <> 0
    )
      AND NOT EXISTS (
        SELECT 1 FROM SLS.Customer c
        WHERE c.Code = TRY_CAST(cc.Customer_Code AS INT) AND c.Code IS NOT NULL
    );
    PRINT N'  Customers inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== [4] MERGE CustomerAddress with corrected joins ===';
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
        INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] cc ON cc.Customer_ID = a.Customer_FK
        INNER JOIN (
            SELECT x.Id, x.Code
            FROM (
                SELECT
                    cu.Id,
                    cu.Code,
                    ROW_NUMBER() OVER (
                        PARTITION BY cu.Code
                        ORDER BY CASE WHEN ISNULL(cu.CreatedByName, N'') LIKE N'sync-%' THEN 1 ELSE 0 END, cu.Id
                    ) AS Rn
                FROM SLS.Customer cu
                WHERE cu.Code IS NOT NULL
            ) x
            WHERE x.Rn = 1
        ) c ON c.Code = CAST(cc.Customer_Code AS INT)
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
    ) AS s
    ON t.HtsId = s.HtsId
    WHEN MATCHED THEN UPDATE SET
        t.CustomerId = s.CustomerId,
        t.ZoneId = s.ZoneId,
        t.ProvinceId = COALESCE(s.ProvinceId, t.ProvinceId),
        t.AgencyPartyId = COALESCE(s.AgencyPartyId, t.AgencyPartyId),
        t.AgencyDlId = COALESCE(s.AgencyDlId, t.AgencyDlId),
        t.Title = s.Title,
        t.Address = s.Address,
        t.Grade = s.Grade,
        t.HamkaranAddId = s.HamkaranAddId,
        t.RahkaranId = s.RahkaranId,
        t.RahkaranVersion = s.RahkaranVersion,
        t.ModifiedById = 1,
        t.ModifiedByName = @SeedUser,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED AND s.CustomerId IS NOT NULL THEN INSERT
        (HtsId, CustomerId, Title, Address, ZoneId, Grade, ProvinceId, AgencyPartyId, AgencyDlId,
         HamkaranAddId, RahkaranId, RahkaranVersion,
         CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
         ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (s.HtsId, s.CustomerId, s.Title, s.Address, s.ZoneId, s.Grade, s.ProvinceId, s.AgencyPartyId, s.AgencyDlId,
         s.HamkaranAddId, s.RahkaranId, s.RahkaranVersion,
         1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

    PRINT N'  MERGE done';

    COMMIT TRANSACTION;
    PRINT N'=== AFTER ===';
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
    (SELECT COUNT(*) FROM SLS.Customer) AS CustomerTotal,
    (SELECT COUNT(*) FROM SLS.Customer WHERE ISNULL(HtsId,0) <> 0) AS CustomerHts,
    (SELECT COUNT(*) FROM SLS.CustomerAddress) AS AddrTotal,
    (SELECT COUNT(*) FROM SLS.CustomerAddress WHERE ISNULL(ProvinceId,0) <> 0) AS AddrProvince,
    (SELECT COUNT(*) FROM SLS.CustomerAddress WHERE ISNULL(AgencyDlId,0) <> 0) AS AddrAgencyDl,
    (SELECT COUNT(*) FROM SLS.CustomerAddress WHERE ISNULL(AgencyPartyId,0) <> 0) AS AddrAgencyParty,
    (SELECT COUNT(*) FROM [TMS].[TotalSystem].[dbo].[Crm_Customer_Address] a
      WHERE NOT EXISTS (SELECT 1 FROM SLS.Customer c WHERE c.HtsId = a.Customer_FK AND ISNULL(c.HtsId,0) <> 0)
    ) AS UnmappedAddr;
