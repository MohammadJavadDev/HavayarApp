/*
  Seed_SaleTenderManagement_RolesAndUsers.sql
  دسترسی مناقصه خدمات پس از فروش مطابق HTS:
    - اعطا روی کاربر (Vw_Permission.UserGroup_ID IS NULL)
    - اعطا روی گروه (Vw_Permission + Gnr_UserGroupMember)
    - گروه 479 فقط اعلان مناقصه تکراری (کد HTS)
  Idempotent — safe to re-run. Requires linked server [TMS] → TotalSystem.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-sale-tender-roles';

    -----------------------------------------------------------------------------
    PRINT N'=== [1] Ensure Sale.Tenders.View ===';
    -----------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.Tenders.View')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Id = 500016)
        BEGIN
            SET IDENTITY_INSERT system.Role ON;
            INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (500016, N'Sale.Tenders.View', N'مناقصه خدمات پس از فروش - مشاهده',
                1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
            SET IDENTITY_INSERT system.Role OFF;
            PRINT N'  CREATED Role Id=500016 Name=Sale.Tenders.View';
        END
        ELSE
        BEGIN
            INSERT INTO system.Role (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (N'Sale.Tenders.View', N'مناقصه خدمات پس از فروش - مشاهده',
                1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
            PRINT N'  CREATED Role Name=Sale.Tenders.View (generated Id)';
        END
    END
    ELSE
        PRINT N'  EXISTS Role Name=Sale.Tenders.View';

    -----------------------------------------------------------------------------
    PRINT N'=== [2] Controller RoleAccess gaps ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#TenderActionAccess') IS NOT NULL DROP TABLE #TenderActionAccess;
    CREATE TABLE #TenderActionAccess
    (
        RoleName             NVARCHAR(200) NOT NULL,
        Path                 NVARCHAR(300) NOT NULL,
        ActionAccessType     INT           NOT NULL,
        ActionAccessItemType INT           NOT NULL,
        EntityName           NVARCHAR(200) NULL
    );

    -- مشاهده صفحه مناقصه / کارتابل (Read HTS)
    INSERT INTO #TenderActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType, EntityName)
    SELECT r.RoleName, a.Path, a.ActionAccessType, a.ActionAccessItemType, a.EntityName
    FROM (VALUES
        (N'/panel/sale/tendermanagement/list',                1, 1,    N'Entities.App.Sale.TenderManagement'),
        (N'/panel/sale/tendermanagement/edit',                1, 5,    N'Entities.App.Sale.TenderManagement'),
        (N'/panel/sale/tendermanagement/fetchdata',           2, 2,    N'Entities.App.Sale.TenderManagement'),
        (N'/panel/sale/tendermanagement/exporttoexcel',       2, 0,    N'Entities.App.Sale.TenderManagement'),
        (N'/panel/sale/tendermanagement/getcompanyinfo',      2, 1000, N'Entities.App.Sale.TenderManagement'),
        (N'/panel/sale/tendermanagement/getindustryiteminfo', 2, 1000, N'Entities.App.Sale.TenderManagement'),
        (N'/panel/sale/tendermanagement/approvequeue',        1, 1,    N'Entities.App.Sale.TenderManagement'),
        (N'/panel/sale/tendermanagement/fetchapprovequeue',   2, 2,    N'Entities.App.Sale.TenderManagement')
    ) a(Path, ActionAccessType, ActionAccessItemType, EntityName)
    CROSS JOIN (VALUES
        (N'Sale.Tenders.View'),
        (N'Sale.Tenders.Approver')
    ) r(RoleName);

    -- GetIndustryItemInfo / GetCompanyInfo برای نقش‌های عملیاتی
    INSERT INTO #TenderActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType, EntityName)
    SELECT r.RoleName, a.Path, 2, 1000, N'Entities.App.Sale.TenderManagement'
    FROM (VALUES
        (N'/panel/sale/tendermanagement/getindustryiteminfo'),
        (N'/panel/sale/tendermanagement/getcompanyinfo')
    ) a(Path)
    CROSS JOIN (VALUES
        (N'Sale.Tenders.ShowAll'),
        (N'Sale.Tenders.CompressedAir'),
        (N'Sale.Tenders.OilGas'),
        (N'Sale.Tenders.Manager'),
        (N'Sale.AfterSales'),
        (N'ShowAllMenus')
    ) r(RoleName);

    -- فرزندان مناقصه
    IF OBJECT_ID('tempdb..#TenderChildAction') IS NOT NULL DROP TABLE #TenderChildAction;
    CREATE TABLE #TenderChildAction
    (
        Path                 NVARCHAR(300) NOT NULL,
        ActionAccessType     INT           NOT NULL,
        ActionAccessItemType INT           NOT NULL,
        EntityName           NVARCHAR(200) NOT NULL,
        IsWrite              BIT           NOT NULL
    );

    INSERT INTO #TenderChildAction (Path, ActionAccessType, ActionAccessItemType, EntityName, IsWrite)
    SELECT N'/panel/sale/' + ctrl.Ctrl + N'/' + act.ActionName, act.AccessType, act.ItemType, ctrl.EntityName, act.IsWrite
    FROM (VALUES
        (N'tenderpart',         N'Entities.App.Sale.TenderPart'),
        (N'tendertask',         N'Entities.App.Sale.TenderTask'),
        (N'tendercomment',      N'Entities.App.Sale.TenderComment'),
        (N'tenderattachment',   N'Entities.App.Sale.TenderAttachment'),
        (N'tenderproductgroup', N'Entities.App.Sale.TenderProductGroup')
    ) ctrl(Ctrl, EntityName)
    CROSS JOIN (VALUES
        (N'save',              2, 3,    1),
        (N'add',               2, 4,    1),
        (N'update',            2, 5,    1),
        (N'delete',            2, 6,    1),
        (N'new',               1, 4,    1),
        (N'edit',              1, 5,    0),
        (N'list',              1, 1,    0),
        (N'listbyparentid',    1, 1,    0),
        (N'getlistbyparentid', 2, 2,    0),
        (N'fetchdata',         2, 2,    0),
        (N'exporttoexcel',     2, 0,    0)
    ) act(ActionName, AccessType, ItemType, IsWrite);

    INSERT INTO #TenderActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType, EntityName)
    SELECT r.RoleName, c.Path, c.ActionAccessType, c.ActionAccessItemType, c.EntityName
    FROM #TenderChildAction c
    CROSS JOIN (VALUES
        (N'Sale.Tenders.ShowAll'),
        (N'Sale.Tenders.CompressedAir'),
        (N'Sale.Tenders.OilGas'),
        (N'Sale.Tenders.Manager')
    ) r(RoleName);

    INSERT INTO #TenderActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType, EntityName)
    SELECT r.RoleName, c.Path, c.ActionAccessType, c.ActionAccessItemType, c.EntityName
    FROM #TenderChildAction c
    CROSS JOIN (VALUES
        (N'Sale.Tenders.View'),
        (N'Sale.Tenders.Approver')
    ) r(RoleName)
    WHERE c.IsWrite = 0;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT a.Path, a.ActionAccessType, a.ActionAccessItemType, a.EntityName, NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #TenderActionAccess a
    INNER JOIN system.Role r ON r.Name = a.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = r.Id AND x.Path = a.Path AND x.ActionAccessType = a.ActionAccessType
    );
    PRINT N'  Inserted controller RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [3] DataProfile RoleAccess ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#TenderProfileRole') IS NOT NULL DROP TABLE #TenderProfileRole;
    CREATE TABLE #TenderProfileRole (ProfileName NVARCHAR(100) NOT NULL, RoleName NVARCHAR(200) NOT NULL);

    INSERT INTO #TenderProfileRole (ProfileName, RoleName)
    SELECT p.Name, r.Name
    FROM (VALUES
        (N'AfterSales_TenderManagement_List'),
        (N'AfterSales_TenderManagement_ApproveQueue')
    ) p(Name)
    CROSS JOIN (VALUES
        (N'Sale.Tenders.View'),
        (N'Sale.Tenders.Approver')
    ) r(Name);

    INSERT INTO #TenderProfileRole (ProfileName, RoleName)
    SELECT p.Name, r.Name
    FROM (VALUES
        (N'AfterSales_TenderPart_List'),
        (N'AfterSales_TenderTask_List'),
        (N'AfterSales_TenderComment_List'),
        (N'AfterSales_TenderAttachment_List'),
        (N'AfterSales_TenderProductGroup_List')
    ) p(Name)
    CROSS JOIN (VALUES
        (N'Sale.Tenders.View'),
        (N'Sale.Tenders.Approver'),
        (N'Sale.Tenders.ShowAll'),
        (N'Sale.Tenders.CompressedAir'),
        (N'Sale.Tenders.OilGas'),
        (N'Sale.Tenders.Manager')
    ) r(Name);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT
        N'dataProfile_' + CAST(q.Id AS nvarchar(20)),
        3, 7, q.EntityFullName, q.Title, NULL, q.Id, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #TenderProfileRole map
    INNER JOIN system.SavedQuery q ON q.Name = map.ProfileName
    INNER JOIN system.Role r ON r.Name = map.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.ActionAccessType = 3 AND ra.RowId = q.Id AND ra.RoleId = r.Id
    );
    PRINT N'  Inserted DataProfile RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [4] Menu AccessRoleIds merge View ===';
    -----------------------------------------------------------------------------
    DECLARE @AccessRoles NVARCHAR(MAX);
    DECLARE @AccessRoleIds NVARCHAR(MAX);
    SELECT @AccessRoles = N'[' + STUFF((
        SELECT N',"' + Name + N'"'
        FROM (VALUES
            (N'Sale.AfterSales'), (N'Rpr.Repairs'), (N'Crm.AfterSalesSurvey'), (N'ShowAllMenus'),
            (N'Sale.Tenders.Approver'), (N'Sale.Tenders.Manager'), (N'Sale.Tenders.ShowAll'),
            (N'Sale.Tenders.CompressedAir'), (N'Sale.Tenders.OilGas'), (N'Sale.Tenders.View')
        ) x(Name)
        FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
    SELECT @AccessRoleIds = N'[' + STUFF((
        SELECT N',' + CAST(Id AS nvarchar(20))
        FROM system.Role
        WHERE Name IN (
            N'Sale.AfterSales', N'Rpr.Repairs', N'Crm.AfterSalesSurvey', N'ShowAllMenus',
            N'Sale.Tenders.Approver', N'Sale.Tenders.Manager', N'Sale.Tenders.ShowAll',
            N'Sale.Tenders.CompressedAir', N'Sale.Tenders.OilGas', N'Sale.Tenders.View')
        FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';

    UPDATE system.SystemMenu
    SET AccessRoles = @AccessRoles,
        AccessRoleIds = @AccessRoleIds,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = N'AfterSalesServiceSystem';
    PRINT N'  Menu rows updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [5] Assign HTS users (direct + group) ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
    CREATE TABLE #UserRoleMap
    (
        Username NVARCHAR(200) NOT NULL,
        RoleName NVARCHAR(200) NOT NULL,
        Source   NVARCHAR(80)  NOT NULL
    );

    ;WITH HtsGrant AS (
        SELECT
            p.Page_ID,
            p.Permission_ID,
            p.User_FK,
            NULLIF(LTRIM(RTRIM(p.ActiveDirectoryUsername)), N'') AS AdName,
            NULLIF(LTRIM(RTRIM(hu.Username)), N'') AS HtsUsername,
            CASE WHEN p.UserGroup_ID IS NULL THEN N'direct' ELSE N'group:' + CAST(p.UserGroup_ID AS nvarchar(20)) END AS Source
        FROM [TMS].[TotalSystem].[dbo].[Vw_Permission] p
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON hu.User_ID = p.User_FK
        WHERE p.System_ID = 14
          AND p.Page_ID IN (493, 503)
          AND p.IsActive = 1
          AND p.User_FK IS NOT NULL
    ),
    HtsUser AS (
        SELECT DISTINCT User_FK, AdName, HtsUsername FROM HtsGrant
        UNION
        SELECT u.User_ID, NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')
        FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        WHERE m.UserGroup_FK = 479 AND u.IsActive = 1
    )
    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT x.Username, x.RoleName, x.Source
    FROM (
        -- هر دسترسی صفحه 493 → مشاهده
        SELECT COALESCE(g.AdName, g.HtsUsername) AS Username, N'Sale.Tenders.View' AS RoleName, g.Source
        FROM HtsGrant g
        WHERE g.Page_ID = 493 AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        -- FullAccess / ShowAll / ExtraOperations روی 493 → نمایش همه
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.Tenders.ShowAll', g.Source
        FROM HtsGrant g
        WHERE g.Page_ID = 493 AND g.Permission_ID IN (2, 7, 151) AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        -- هوای فشرده
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.Tenders.CompressedAir', g.Source
        FROM HtsGrant g
        WHERE g.Page_ID = 493 AND g.Permission_ID = 152 AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        -- نفت و گاز
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.Tenders.OilGas', g.Source
        FROM HtsGrant g
        WHERE g.Page_ID = 493 AND g.Permission_ID = 153 AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        -- FullAccess / TendersManager روی 493 → مدیر
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.Tenders.Manager', g.Source
        FROM HtsGrant g
        WHERE g.Page_ID = 493 AND g.Permission_ID IN (2, 154) AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        -- کارتابل 503: FullAccess / ShowAll / تایید اولیه → تاییدکننده
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.Tenders.Approver', g.Source
        FROM HtsGrant g
        WHERE g.Page_ID = 503 AND g.Permission_ID IN (2, 7, 122) AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        -- کارتابل 503: FullAccess / تایید نهایی → مدیر
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.Tenders.Manager', g.Source
        FROM HtsGrant g
        WHERE g.Page_ID = 503 AND g.Permission_ID IN (2, 123) AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        -- گروه 479 HTS: اعلان مناقصه تکراری
        SELECT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')),
               N'Sale.Tenders.DuplicateNotify',
               N'group:479'
        FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        WHERE m.UserGroup_FK = 479 AND u.IsActive = 1
    ) x
    WHERE x.Username IS NOT NULL;

    -- نام کاربری جایگزین Gnr_User.Username اگر با AD فرق دارد
    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT hu.Username, m.RoleName, m.Source + N'+htsUser'
    FROM #UserRoleMap m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu
        ON LOWER(hu.ActiveDirectoryUsername) = LOWER(m.Username)
    WHERE hu.Username IS NOT NULL
      AND LOWER(hu.Username) <> LOWER(m.Username)
      AND NOT EXISTS (
          SELECT 1 FROM #UserRoleMap x
          WHERE LOWER(x.Username) = LOWER(hu.Username) AND x.RoleName = m.RoleName
      );

    ;WITH d AS (
        SELECT Username, RoleName, ROW_NUMBER() OVER (PARTITION BY LOWER(Username), RoleName ORDER BY Username) AS rn
        FROM #UserRoleMap
    )
    DELETE FROM d WHERE rn > 1;

    DECLARE @MapUsername nvarchar(200), @MapRoleName nvarchar(200), @MapRoleId bigint, @MapUserId bigint;
    DECLARE @Assigned int = 0, @Missing int = 0, @SkippedAlready int = 0;

    DECLARE map_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Username, RoleName FROM #UserRoleMap;
    OPEN map_cur;
    FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @MapRoleId = Id FROM system.Role WHERE Name = @MapRoleName;
        SELECT TOP 1 @MapUserId = Id
        FROM system.[User]
        WHERE LOWER(Username) = LOWER(@MapUsername);

        IF @MapUserId IS NULL
        BEGIN
            PRINT N'  MISSING USER (skip): ' + @MapUsername + N' for role ' + @MapRoleName;
            SET @Missing = @Missing + 1;
        END
        ELSE IF @MapRoleId IS NULL
            PRINT N'  MISSING ROLE (skip): ' + @MapRoleName;
        ELSE IF EXISTS (
            SELECT 1 FROM system.[User] u
            CROSS APPLY OPENJSON(u.RoleIds) j
            WHERE u.Id = @MapUserId AND TRY_CAST(j.value AS bigint) = @MapRoleId
        )
            SET @SkippedAlready = @SkippedAlready + 1;
        ELSE
        BEGIN
            UPDATE system.[User]
            SET
                RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                        THEN N'[' + CAST(@MapRoleId AS nvarchar(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@MapRoleId AS nvarchar(20)) + N']')
                END,
                Roles = CASE
                    WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @MapRoleName) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]'
                        THEN N'["' + @MapRoleName + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @MapRoleName + N'"]')
                END,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @MapUserId;
            SET @Assigned = @Assigned + 1;
        END

        SET @MapUserId = NULL;
        SET @MapRoleId = NULL;
        FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleName;
    END
    CLOSE map_cur;
    DEALLOCATE map_cur;

    PRINT N'  User role assignments applied: ' + CAST(@Assigned AS nvarchar(20));
    PRINT N'  Already had role (skipped): ' + CAST(@SkippedAlready AS nvarchar(20));
    PRINT N'  Missing users (logged): ' + CAST(@Missing AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE: Seed_SaleTenderManagement_RolesAndUsers committed ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

PRINT N'--- verify ---';
SELECT r.Name, r.Id,
       (SELECT COUNT(*) FROM system.RoleAccess ra WHERE ra.RoleId = r.Id AND ra.Path LIKE N'/panel/sale/tender%') AS TenderAccess,
       (SELECT COUNT(*) FROM system.[User] u CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j WHERE TRY_CAST(j.value AS bigint) = r.Id) AS Users
FROM system.Role r
WHERE r.Name LIKE N'Sale.Tenders%'
ORDER BY r.Name;

SELECT u.Username, r.Name AS RoleName
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
INNER JOIN system.Role r ON r.Id = TRY_CAST(j.value AS bigint)
WHERE r.Name LIKE N'Sale.Tenders%'
ORDER BY r.Name, u.Username;
