/*
  Seed_SaleServiceRequest_AgencyProfile.sql
  نمایه «نمایش اطلاعات نمایندگی» = کپی AfterSales_ServiceRequest_List بدون ستون هزینه ماموریت.
  RoleAccess فقط برای نقش‌های «نمایندگی ها» و «نمایندگان فروش» (HTS 53 و 102).
  هر نمایندگی فقط اندیکاتورهای خودش و مسئول منطقه فروش (منطقه N) در استان‌های شرکت نمایندگی را می‌بیند.
  منطق فیلتر هم‌تراز با WebApp/Actions/Sale/ServiceRequestAgencyAccess.cs است.
  CreatedBy ماموریت‌های همگام‌شده از Sale_Mission.CreatedUser_FK پر می‌شود
  تا نمایندگی فقط قیمت ماموریت خودش را ببیند.
  Encoding: UTF-8 with BOM. Apply: sqlcmd -f 65001.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-sale-servicerequest-agency-profile';
    DECLARE @Name NVARCHAR(100) = N'AfterSales_ServiceRequest_Agency';
    DECLARE @Title NVARCHAR(200) = N'نمایش اطلاعات نمایندگی';

    DECLARE @QueryJson NVARCHAR(MAX);
    DECLARE @ColumnsJson NVARCHAR(MAX);
    DECLARE @ActionOptions NVARCHAR(MAX);
    DECLARE @Buttons NVARCHAR(MAX);
    DECLARE @Entity NVARCHAR(200);
    DECLARE @Q NVARCHAR(MAX);

    SELECT
        @QueryJson = QueryJson,
        @ColumnsJson = ColumnsJson,
        @ActionOptions = ActionOptions,
        @Buttons = CustomActionButtonsJson,
        @Entity = EntityFullName,
        @Q = JSON_VALUE(QueryJson, N'$.CustomQuery')
    FROM system.SavedQuery
    WHERE Name = N'AfterSales_ServiceRequest_List';

    IF @Q IS NULL
        RAISERROR(N'AfterSales_ServiceRequest_List not found.', 16, 1);

    DECLARE @i INT = CHARINDEX(N'ISNULL([tCost].[TotalCost]', @Q);
    DECLARE @j INT = CHARINDEX(N'AS [tMission_Cost],', @Q, @i);
    IF @i = 0 OR @j = 0
        RAISERROR(N'tMission_Cost select line was not found.', 16, 1);
    SET @j = @j + LEN(N'AS [tMission_Cost],');
    WHILE @j <= LEN(@Q) AND UNICODE(SUBSTRING(@Q, @j, 1)) IN (9, 10, 13, 32)
        SET @j = @j + 1;
    SET @Q = STUFF(@Q, @i, @j - @i, N'');

    SET @i = CHARINDEX(N'OUTER APPLY (', @Q);
    SET @j = CHARINDEX(N') AS [tCost]', @Q);
    IF @i = 0 OR @j = 0 OR @j < @i
        RAISERROR(N'tCost apply block was not found.', 16, 1);
    SET @j = @j + LEN(N') AS [tCost]');
    WHILE @j <= LEN(@Q) AND UNICODE(SUBSTRING(@Q, @j, 1)) IN (9, 10, 13, 32)
        SET @j = @j + 1;
    SET @Q = STUFF(@Q, @i, @j - @i, N'');

    IF CHARINDEX(N'tMission_Cost', @Q) > 0 OR CHARINDEX(N'[tCost]', @Q) > 0
        RAISERROR(N'Mission cost was not removed from the agency query.', 16, 1);

    IF CHARINDEX(N'LEFT JOIN [SLS].[CustomerAddress] AS [t4]', @Q) = 0
        RAISERROR(N'CustomerAddress alias [t4] was not found.', 16, 1);

    DECLARE @Scope NVARCHAR(MAX) = N'DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
DECLARE @Allowed TABLE (UserId BIGINT NOT NULL PRIMARY KEY);
DECLARE @Managers TABLE (UserId BIGINT NOT NULL PRIMARY KEY);
DECLARE @Provinces TABLE (ProvinceId BIGINT NOT NULL PRIMARY KEY);
DECLARE @ZoneId BIGINT;

INSERT INTO @Allowed (UserId)
SELECT DISTINCT x.UserId
FROM (
    SELECT u.Id AS UserId
    FROM [system].[User] AS me
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] AS huMe
        ON LOWER(LTRIM(RTRIM(huMe.Username))) = LOWER(LTRIM(RTRIM(me.Username)))
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] AS hu
        ON hu.Company_FK = huMe.Company_FK
    INNER JOIN [system].[User] AS u
        ON LOWER(LTRIM(RTRIM(u.Username))) = LOWER(LTRIM(RTRIM(hu.Username)))
    WHERE me.Id = @CuId AND huMe.Company_FK IS NOT NULL
    UNION
    SELECT @CuId WHERE @CuId IS NOT NULL
) AS x
WHERE x.UserId IS NOT NULL;

INSERT INTO @Provinces (ProvinceId)
SELECT DISTINCT r.Id
FROM [system].[User] AS me
INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] AS hu
    ON LOWER(LTRIM(RTRIM(hu.Username))) = LOWER(LTRIM(RTRIM(me.Username)))
INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] AS c
    ON c.ManCompany_FK = hu.Company_FK
INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer_Address] AS a
    ON a.Customer_FK = c.Customer_ID
INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_Province] AS pr
    ON pr.Province_ID = a.Province_FK
INNER JOIN [Gnr].[Region] AS r
    ON r.Type = 2
   AND (
        r.Name = pr.Province_Title
        OR r.Name = N''استان '' + pr.Province_Title
        OR REPLACE(r.Name, N''استان '', N'''') = pr.Province_Title
   )
WHERE me.Id = @CuId
  AND hu.Company_FK IS NOT NULL
  AND NULLIF(LTRIM(RTRIM(pr.Province_Title)), N'''') IS NOT NULL;

SELECT TOP (1) @ZoneId = s.ZoneId
FROM (
    SELECT z.Id AS ZoneId,
           COUNT(DISTINCT ap.ProvinceId) AS Hit,
           COUNT(DISTINCT rz2.ProvinceId) AS Cnt
    FROM [Crm].[Zone] AS z
    INNER JOIN [Sale].[ResponsibleZone] AS rz2 ON rz2.ZoneId = z.Id
    LEFT JOIN @Provinces AS ap ON ap.ProvinceId = rz2.ProvinceId
    WHERE z.Title LIKE N''منطقه [0-9]%''
      AND z.Title NOT LIKE N''%-%''
      AND z.Title NOT LIKE N''%نمایندگی%''
    GROUP BY z.Id
) AS s
WHERE s.Hit > 0
ORDER BY s.Hit DESC, s.Cnt ASC;

INSERT INTO @Managers (UserId)
SELECT DISTINCT rz.ResponsibleId
FROM [Sale].[ResponsibleZone] AS rz
WHERE rz.ZoneId = @ZoneId AND rz.ResponsibleId IS NOT NULL;
--!--mainsection
';

    SET @Q = @Scope + @Q + N'
WHERE (
    [t1].[CreatedById] IN (SELECT UserId FROM @Allowed)
    OR (
        [t1].[CreatedById] IN (SELECT UserId FROM @Managers)
        AND [t4].[ProvinceId] IN (SELECT ProvinceId FROM @Provinces)
    )
)';

    DECLARE @Describe NVARCHAR(MAX) = N'DECLARE @CurrentUserId NVARCHAR(30) = N''1'';' + CHAR(10) + @Q;
    IF EXISTS (
        SELECT 1
        FROM sys.dm_exec_describe_first_result_set(@Describe, NULL, 0)
        WHERE error_message IS NOT NULL
    )
        RAISERROR(N'Agency profile query does not compile.', 16, 1);

    SET @QueryJson = JSON_MODIFY(@QueryJson, N'$.CustomQuery', @Q);

    SELECT @ColumnsJson = N'[' + STRING_AGG(CAST(j.value AS NVARCHAR(MAX)), N',')
        WITHIN GROUP (ORDER BY TRY_CAST(j.[key] AS INT)) + N']'
    FROM OPENJSON(@ColumnsJson) j
    WHERE JSON_VALUE(j.value, N'$.Alliance') <> N'tMission_Cost';

    IF @ColumnsJson IS NULL OR CHARINDEX(N'tMission_Cost', @ColumnsJson) > 0
        RAISERROR(N'tMission_Cost column was not removed from ColumnsJson.', 16, 1);

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
    BEGIN
        UPDATE system.SavedQuery
        SET Title = @Title,
            QueryJson = @QueryJson,
            ColumnsJson = @ColumnsJson,
            CustomActionButtonsJson = @Buttons,
            ActionOptions = @ActionOptions,
            EntityFullName = @Entity,
            DiagramJson = N'{}',
            EventScriptsJson = N'{"onSelectedRow":"","onRowAdded":""}',
            Mode = 1,
            Type = 1,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Name = @Name;
        PRINT N'  UPDATED ' + @Name;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@Name, @Title, @QueryJson, @ColumnsJson, N'{}', @Buttons, N'{"onSelectedRow":"","onRowAdded":""}',
             1, 1, @Entity, @ActionOptions,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        PRINT N'  CREATED ' + @Name;
    END

    DECLARE @ProfileId BIGINT = (SELECT Id FROM system.SavedQuery WHERE Name = @Name);

    DELETE FROM system.RoleAccess
    WHERE ActionAccessType = 3 AND RowId = @ProfileId;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        N'dataProfile_' + CAST(@ProfileId AS NVARCHAR(20)),
        3, 7, N'Entities.App.Sale.ServiceRequest', @Title, NULL, @ProfileId, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'نمایندگی ها', N'نمایندگان فروش');

    IF @@ROWCOUNT <> 2
        RAISERROR(N'Agency roles were not both found for RoleAccess.', 16, 1);

    COMMIT TRANSACTION;
    PRINT N'=== DONE agency profile ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

BEGIN TRY
BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
        RAISERROR(N'Linked Server [TMS] was not found.', 16, 1);

    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    SELECT CAST(ou.User_ID AS BIGINT) AS OldUserId, w.Id AS NewUserId,
           LEFT(COALESCE(NULLIF(LTRIM(RTRIM(ou.FullName)), N''), NULLIF(LTRIM(RTRIM(w.NameFa)), N''), NULLIF(LTRIM(RTRIM(w.Name)), N''), w.Username), 150) AS NewUserName
    INTO #UserMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
    CROSS APPLY (
        SELECT TOP 1 nu.Id, nu.NameFa, nu.Name, nu.Username
        FROM [system].[User] nu
        WHERE LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.Username)))
           OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
               AND LOWER(nu.Username) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))))
           OR (NULLIF(LTRIM(RTRIM(ou.ActiveDirectoryUsername)), N'') IS NOT NULL
               AND LOWER(REPLACE(ISNULL(nu.Email, N''), N'@havayar.com', N'')) = LOWER(LTRIM(RTRIM(ou.ActiveDirectoryUsername))))
           OR LOWER(REPLACE(ISNULL(nu.Email, N''), N'@havayar.com', N'')) = LOWER(LTRIM(RTRIM(ou.Username)))
        ORDER BY CASE WHEN nu.IsActive = 1 THEN 0 ELSE 1 END, nu.Id
    ) w;

    UPDATE m
    SET m.CreatedById = um.NewUserId,
        m.CreatedByName = LEFT(COALESCE(NULLIF(LTRIM(RTRIM(htsUser.FullName)), N''), um.NewUserName, m.CreatedByName), 150)
    FROM Sale.Mission m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Sale_Mission] s
        ON m.HtsId = CAST(s.Mission_Id AS BIGINT) AND m.HtsId <> 0
    INNER JOIN #UserMap um ON um.OldUserId = CAST(s.CreatedUser_FK AS BIGINT)
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] htsUser ON htsUser.User_ID = s.CreatedUser_FK
    WHERE m.CreatedById IS NULL OR m.CreatedById <> um.NewUserId;

    PRINT N'Mission creator rows updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));
    COMMIT TRANSACTION;
    PRINT N'=== DONE mission creators ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT Id, Name, Title
FROM system.SavedQuery
WHERE Name = N'AfterSales_ServiceRequest_Agency';

SELECT r.Name AS RoleName, ra.Path, ra.EntityName
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
INNER JOIN system.SavedQuery q ON q.Id = ra.RowId
WHERE q.Name = N'AfterSales_ServiceRequest_Agency' AND ra.ActionAccessType = 3;

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'AfterSales_ServiceRequest_Agency'
ORDER BY TRY_CAST(j.[key] AS INT);
