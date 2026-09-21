/*
SyncAfterSalesPhase4FromTotalSystem.sql — CSS masters
ResponsibleZone: province unique-name + leftover aliases (البرز/تهران غرب/کل تهران غیر از غرب/اصفهان (کاشان));
CreatedBy/CreatedOn from HTS Sale_ResponsibleZone (Username / AD / email). Do not invent users.
Encoding: UTF-8 with BOM. Apply: sqlcmd -f 65001
*/
SET NOCOUNT ON; SET XACT_ABORT ON;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'sync-after-sales-phase4';
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1); RETURN; END;

BEGIN TRY
BEGIN TRANSACTION;

IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
;WITH UserCandidates AS (
    SELECT
        CAST(ou.User_ID AS BIGINT) AS OldUserId,
        nu.Id AS NewUserId,
        COALESCE(NULLIF(LTRIM(RTRIM(nu.NameFa)), N''), nu.Name, nu.Username) AS NewUserName,
        CASE
            WHEN LOWER(nu.Username) = LOWER(ou.Username) THEN 1
            WHEN NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
                 AND LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))) THEN 2
            WHEN NULLIF(LTRIM(RTRIM(ou.Username)), N'') IS NOT NULL
                 AND LOWER(REPLACE(ISNULL(nu.Email, N''), N'@havayar.com', N'')) = LOWER(ou.Username) THEN 3
            WHEN NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
                 AND LOWER(REPLACE(ISNULL(nu.Email, N''), N'@havayar.com', N'')) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))) THEN 4
        END AS MatchRank
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
    INNER JOIN [system].[User] nu
        ON LOWER(nu.Username) = LOWER(ou.Username)
        OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
            AND LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))))
        OR (NULLIF(LTRIM(RTRIM(ou.Username)), N'') IS NOT NULL
            AND LOWER(REPLACE(ISNULL(nu.Email, N''), N'@havayar.com', N'')) = LOWER(ou.Username))
        OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
            AND LOWER(REPLACE(ISNULL(nu.Email, N''), N'@havayar.com', N'')) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))))
)
SELECT OldUserId, NewUserId, NewUserName
INTO #UserMap
FROM (
    SELECT *, ROW_NUMBER() OVER (PARTITION BY OldUserId ORDER BY MatchRank, NewUserId) AS rn
    FROM UserCandidates
    WHERE MatchRank IS NOT NULL
) x
WHERE rn = 1;

MERGE Sale.OrderPoint AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, s.PartCardexId AS HtsPartCardexId, p.Id AS AlternativePartId, s.IsIgnore, s.IsExternal, s.Comment
    FROM [TMS].[TotalSystem].[dbo].[Sale_OrderPoint] s
    LEFT JOIN Inv.Part p ON p.HtsId = s.AlternativePartId
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.HtsPartCardexId=s.HtsPartCardexId, t.AlternativePartId=s.AlternativePartId, t.IsIgnore=s.IsIgnore, t.IsExternal=s.IsExternal, t.Comment=s.Comment,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, HtsPartCardexId, AlternativePartId, IsIgnore, IsExternal, Comment,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.HtsPartCardexId, s.AlternativePartId, s.IsIgnore, s.IsExternal, s.Comment, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

;WITH UniqueCustomerCode AS (
    SELECT Code
    FROM SLS.Customer
    WHERE Code IS NOT NULL
    GROUP BY Code
    HAVING COUNT(*) = 1
),
CustomerMap AS (
    SELECT
        CAST(s.Customer_AllowedGuarantee_ID AS BIGINT) AS HtsId,
        COALESCE(cHts.Id, cCode.Id) AS CustomerId,
        s.ProductionOrder_Number AS ProductionOrderNumber,
        s.AllowedGuaranteePart_Mount AS AllowedGuaranteePartAmount,
        s.AllowedMissionGuarantee_Mount AS AllowedMissionGuaranteeAmount
    FROM [TMS].[TotalSystem].[dbo].[Sale_Customer_AllowedGuarantee] s
    LEFT JOIN SLS.Customer cHts ON cHts.HtsId = s.Customer_FK AND cHts.HtsId <> 0
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] hc ON hc.Customer_ID = s.Customer_FK
    LEFT JOIN UniqueCustomerCode uc ON uc.Code = hc.Customer_Code
    LEFT JOIN SLS.Customer cCode ON cCode.Code = uc.Code
)
MERGE Sale.CustomerAllowedGuarantee AS t
USING CustomerMap AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.CustomerId=s.CustomerId, t.ProductionOrderNumber=s.ProductionOrderNumber,
    t.AllowedGuaranteePartAmount=s.AllowedGuaranteePartAmount, t.AllowedMissionGuaranteeAmount=s.AllowedMissionGuaranteeAmount,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, CustomerId, ProductionOrderNumber, AllowedGuaranteePartAmount, AllowedMissionGuaranteeAmount,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.CustomerId, s.ProductionOrderNumber, s.AllowedGuaranteePartAmount, s.AllowedMissionGuaranteeAmount, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

;WITH NormRegion AS (
    SELECT Id,
        LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(Name, N'استان', N''), N'  ', N' '), N'  ', N' '))) AS NormName
    FROM Gnr.Region
    WHERE Type = 2
),
UniqueNorm AS (
    SELECT NormName FROM NormRegion GROUP BY NormName HAVING COUNT(*) = 1
),
NameMap AS (
    SELECT CAST(p.Province_ID AS INT) AS HtsProvinceId, n.Id AS RegionId
    FROM [TMS].[TotalSystem].[dbo].[Gnr_Province] p
    INNER JOIN UniqueNorm u ON u.NormName = LTRIM(RTRIM(REPLACE(REPLACE(p.Province_Title, N'  ', N' '), N'  ', N' ')))
    INNER JOIN NormRegion n ON n.NormName = u.NormName
),
AliasMap AS (
    SELECT CAST(p.Province_ID AS INT) AS HtsProvinceId, r.Id AS RegionId
    FROM [TMS].[TotalSystem].[dbo].[Gnr_Province] p
    INNER JOIN Gnr.Region r ON r.Type = 2 AND (
        (p.Province_Title = N'البرز' AND r.Name = N'استان البرز')
        OR (p.Province_Title IN (N'تهران غرب', N'کل تهران غیر از غرب') AND r.Name = N'استان تهران')
        OR (p.Province_Title = N'اصفهان (کاشان)' AND r.Name = N'استان اصفهان')
    )
),
ProvinceMap AS (
    SELECT HtsProvinceId, RegionId FROM NameMap
    UNION
    SELECT HtsProvinceId, RegionId FROM AliasMap
)
MERGE Sale.ResponsibleZone AS t
USING (
    SELECT
        CAST(s.Id AS BIGINT) AS HtsId,
        um.NewUserId AS ResponsibleId,
        z.Id AS ZoneId,
        pm.RegionId AS ProvinceId,
        s.Comment,
        s.CustomersTitle,
        cm.NewUserId AS CreatedById,
        cm.NewUserName AS CreatedByName,
        s.CreatedDate AS CreatedOnMiladi,
        NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N'') AS CreatedOnShamsi
    FROM [TMS].[TotalSystem].[dbo].[Sale_ResponsibleZone] s
    LEFT JOIN #UserMap um ON um.OldUserId = s.ResponsibleId
    LEFT JOIN #UserMap cm ON cm.OldUserId = CAST(s.CreatedUserId AS BIGINT)
    LEFT JOIN Crm.Zone z ON z.HtsId = s.ZoneId
    LEFT JOIN ProvinceMap pm ON pm.HtsProvinceId = s.ProvinceId
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.ResponsibleId=s.ResponsibleId, t.ZoneId=s.ZoneId, t.ProvinceId=s.ProvinceId, t.Comment=s.Comment, t.CustomersTitle=s.CustomersTitle,
    t.CreatedById=COALESCE(s.CreatedById, t.CreatedById),
    t.CreatedByName=COALESCE(s.CreatedByName, t.CreatedByName),
    t.CreatedOnMiladiDateTime=COALESCE(s.CreatedOnMiladi, t.CreatedOnMiladiDateTime),
    t.CreatedOnShamsiDateTime=COALESCE(s.CreatedOnShamsi, t.CreatedOnShamsiDateTime),
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, ResponsibleId, ZoneId, ProvinceId, Comment, CustomersTitle,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.ResponsibleId, s.ZoneId, s.ProvinceId, s.Comment, s.CustomersTitle,
    s.CreatedById, s.CreatedByName, COALESCE(s.CreatedOnMiladi, @Now), COALESCE(s.CreatedOnShamsi, @NowShamsi),
    1, @SeedUser, @Now, @NowShamsi, 1);

;WITH UniqueCustomerCode AS (
    SELECT Code FROM SLS.Customer WHERE Code IS NOT NULL GROUP BY Code HAVING COUNT(*) = 1
)
MERGE Sale.ResponsibleZoneCustomer AS t
USING (
    SELECT
        CAST(s.Id AS BIGINT) AS HtsId,
        rz.Id AS ResponsibleZoneId,
        COALESCE(cHts.Id, cCode.Id) AS CustomerId
    FROM [TMS].[TotalSystem].[dbo].[Sale_ResponsibleZoneCustomer] s
    INNER JOIN Sale.ResponsibleZone rz ON rz.HtsId = s.ResponsibleZoneId AND rz.HtsId <> 0
    LEFT JOIN SLS.Customer cHts ON cHts.HtsId = s.CustomerId AND cHts.HtsId <> 0
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] hc ON hc.Customer_ID = s.CustomerId
    LEFT JOIN UniqueCustomerCode uc ON uc.Code = TRY_CONVERT(INT, hc.Customer_Code)
    LEFT JOIN SLS.Customer cCode ON cCode.Code = uc.Code
    WHERE COALESCE(cHts.Id, cCode.Id) IS NOT NULL
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.ResponsibleZoneId=s.ResponsibleZoneId, t.CustomerId=s.CustomerId,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, ResponsibleZoneId, CustomerId,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.ResponsibleZoneId, s.CustomerId, 1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);

COMMIT;
PRINT N'Phase 4 sync committed';
END TRY
BEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK; THROW; END CATCH;
