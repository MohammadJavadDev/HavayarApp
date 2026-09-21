/*
  Fix_ResponsibleZone_SyncFromHts.sql
  - Province: unique stripped name + leftover aliases
      البرز → استان البرز
      تهران غرب / کل تهران غیر از غرب → استان تهران
      اصفهان (کاشان) → استان اصفهان
    (resolve Gnr.Region by Name, Type=2; do not create provinces)
  - CreatedById / CreatedByName / CreatedOn* from HTS Sale_ResponsibleZone
    (Username / ActiveDirectoryUsername / email local-part). Do not invent users.
  - Customer MERGE unchanged: HtsId then unique Code; leave unmapped leftovers.
  Encoding: UTF-8 with BOM (sqlcmd -f 65001). Requires linked server [TMS].
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'sync-responsiblezone-20260918';

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sale_ResponsibleZoneCustomer_HtsId' AND object_id = OBJECT_ID(N'Sale.ResponsibleZoneCustomer'))
    BEGIN
        CREATE UNIQUE INDEX IX_Sale_ResponsibleZoneCustomer_HtsId
            ON Sale.ResponsibleZoneCustomer (HtsId)
            WHERE [HtsId] <> CAST(0 AS bigint);
        PRINT N'  Created IX_Sale_ResponsibleZoneCustomer_HtsId';
    END

    IF EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = N'FK_ResponsibleZoneCustomer_ResponsibleZone_ResponsibleZoneId'
          AND delete_referential_action_desc <> N'CASCADE'
    )
    BEGIN
        ALTER TABLE Sale.ResponsibleZoneCustomer
            DROP CONSTRAINT FK_ResponsibleZoneCustomer_ResponsibleZone_ResponsibleZoneId;
        ALTER TABLE Sale.ResponsibleZoneCustomer
            ADD CONSTRAINT FK_ResponsibleZoneCustomer_ResponsibleZone_ResponsibleZoneId
            FOREIGN KEY (ResponsibleZoneId) REFERENCES Sale.ResponsibleZone (Id) ON DELETE CASCADE;
        PRINT N'  FK cascade enabled';
    END

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
    UPDATE rz
    SET rz.ProvinceId = pm.RegionId,
        rz.ModifiedById = 1,
        rz.ModifiedByName = @SeedUser,
        rz.ModifiedDateMiladiDateTime = @Now,
        rz.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sale.ResponsibleZone rz
    INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_ResponsibleZone] s ON s.Id = rz.HtsId
    INNER JOIN ProvinceMap pm ON pm.HtsProvinceId = s.ProvinceId
    WHERE rz.HtsId <> 0
      AND (rz.ProvinceId IS NULL OR rz.ProvinceId <> pm.RegionId);
    PRINT N'  Province mapped: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    UPDATE rz
    SET
        rz.CreatedById = COALESCE(cm.NewUserId, rz.CreatedById),
        rz.CreatedByName = COALESCE(cm.NewUserName, rz.CreatedByName),
        rz.CreatedOnMiladiDateTime = COALESCE(s.CreatedDate, rz.CreatedOnMiladiDateTime),
        rz.CreatedOnShamsiDateTime = COALESCE(NULLIF(LTRIM(RTRIM(s.CreatedDateInText)), N''), rz.CreatedOnShamsiDateTime)
    FROM Sale.ResponsibleZone rz
    INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_ResponsibleZone] s ON s.Id = rz.HtsId
    LEFT JOIN #UserMap cm ON cm.OldUserId = CAST(s.CreatedUserId AS BIGINT)
    WHERE rz.HtsId <> 0;
    PRINT N'  CreatedBy/date refreshed: ' + CAST(@@ROWCOUNT AS nvarchar(20));

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
    WHEN MATCHED THEN UPDATE SET
        t.ResponsibleZoneId = s.ResponsibleZoneId,
        t.CustomerId = s.CustomerId,
        t.ModifiedById = 1,
        t.ModifiedByName = @SeedUser,
        t.ModifiedDateMiladiDateTime = @Now,
        t.ModifiedDateShamsiDateTime = @NowShamsi
    WHEN NOT MATCHED THEN INSERT (
        HtsId, ResponsibleZoneId, CustomerId,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive
    ) VALUES (
        s.HtsId, s.ResponsibleZoneId, s.CustomerId,
        1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1
    );
    PRINT N'  Customer MERGE done';

    COMMIT TRANSACTION;
    PRINT N'=== DONE Fix_ResponsibleZone_SyncFromHts ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    PRINT N'=== ERROR rolled back ===';
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT
    (SELECT COUNT(*) FROM Sale.ResponsibleZone) AS RzRows,
    (SELECT COUNT(*) FROM Sale.ResponsibleZone WHERE ProvinceId IS NULL) AS RzNullProvince,
    (SELECT COUNT(*) FROM Sale.ResponsibleZoneCustomer) AS RzCustRows,
    (SELECT COUNT(*) FROM Sale.ResponsibleZoneReceipt) AS RzReceiptRows;

SELECT r.Name AS ProvinceName, COUNT(*) AS Cnt
FROM Sale.ResponsibleZone rz
INNER JOIN Gnr.Region r ON r.Id = rz.ProvinceId
WHERE r.Name IN (N'استان البرز', N'استان تهران', N'استان اصفهان')
GROUP BY r.Name
ORDER BY r.Name;

SELECT
    rz.CreatedById,
    rz.CreatedByName,
    COUNT(*) AS Cnt
FROM Sale.ResponsibleZone rz
GROUP BY rz.CreatedById, rz.CreatedByName
ORDER BY Cnt DESC;

SELECT
    s.CreatedUserId,
    hu.Username AS HtsUsername,
    hu.ActiveDirectoryUsername AS HtsAd,
    hu.FullName AS HtsFullName,
    COUNT(*) AS UnmappedRows
FROM Sale.ResponsibleZone rz
INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_ResponsibleZone] s ON s.Id = rz.HtsId
INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON hu.User_ID = s.CreatedUserId
LEFT JOIN #UserMap cm ON cm.OldUserId = CAST(s.CreatedUserId AS BIGINT)
WHERE rz.HtsId <> 0
  AND cm.NewUserId IS NULL
GROUP BY s.CreatedUserId, hu.Username, hu.ActiveDirectoryUsername, hu.FullName;
