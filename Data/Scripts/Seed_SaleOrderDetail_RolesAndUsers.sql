/*
  Seed_SaleOrderDetail_RolesAndUsers.sql
  دسترسی حواله فروش مطابق HTS صفحه Sale_Order_Detail (132) و سریال (133):
    - اعطا روی کاربر (Vw_Permission.UserGroup_ID IS NULL)
    - اعطا روی گروه (Vw_Permission + اعضای فعلی Gnr_UserGroupMember)
  نگاشت PermissionType:
    2 FullAccess  → Manage + ViewWithPrice + ExportToExcel  (FullAccess در HTS شامل قیمت و اکسل است)
    8 ShowPrice   → ViewWithPrice
   10 ExportToExcel → ExportToExcel + ViewWithPrice
    3 Read        → View (فقط اگر FullAccess نداشته باشد)
    4/5 روی 133   → Manage (ثبت/ویرایش سریال)
  Idempotent — safe to re-run. Requires linked server [TMS] → TotalSystem.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-sale-orderdetail-roles';
    DECLARE @EntityFullName NVARCHAR(200) = N'Entities.App.Sale.OrderDetail';

    -----------------------------------------------------------------------------
    PRINT N'=== [1] Ensure Roles (system.Role) Ids 200057..200060 ===';
    -----------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.OrderDetail.Manage')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (200057, N'Sale.OrderDetail.Manage', N'فروش - اقلام حواله فروش - دسترسی کامل و مدیریت سریال',
            1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Role Id=200057 Name=Sale.OrderDetail.Manage';
    END
    ELSE
        PRINT N'  EXISTS Role Name=Sale.OrderDetail.Manage';

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.OrderDetail.ViewWithPrice')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (200058, N'Sale.OrderDetail.ViewWithPrice', N'فروش - اقلام حواله فروش - مشاهده با قیمت',
            1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Role Id=200058 Name=Sale.OrderDetail.ViewWithPrice';
    END
    ELSE
        PRINT N'  EXISTS Role Name=Sale.OrderDetail.ViewWithPrice';

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.OrderDetail.View')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (200059, N'Sale.OrderDetail.View', N'فروش - اقلام حواله فروش - مشاهده بدون قیمت',
            1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Role Id=200059 Name=Sale.OrderDetail.View';
    END
    ELSE
        PRINT N'  EXISTS Role Name=Sale.OrderDetail.View';

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.OrderDetail.ExportToExcel')
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (200060, N'Sale.OrderDetail.ExportToExcel', N'فروش - اقلام حواله فروش - خروجی اکسل',
            1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Role Id=200060 Name=Sale.OrderDetail.ExportToExcel';
    END
    ELSE
        PRINT N'  EXISTS Role Name=Sale.OrderDetail.ExportToExcel';

    -----------------------------------------------------------------------------
    PRINT N'=== [2] Controller RoleAccess gaps (without wiping AfterSales/ShowAllMenus) ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#OdActionAccess') IS NOT NULL DROP TABLE #OdActionAccess;
    CREATE TABLE #OdActionAccess
    (
        RoleName             NVARCHAR(200) NOT NULL,
        Path                 NVARCHAR(300) NOT NULL,
        ActionAccessType     INT           NOT NULL,
        ActionAccessItemType INT           NOT NULL,
        EntityName           NVARCHAR(200) NULL
    );

    -- لیست / دریافت / تب سریال‌ها
    INSERT INTO #OdActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType, EntityName)
    SELECT r.RoleName, a.Path, a.ActionAccessType, a.ActionAccessItemType, @EntityFullName
    FROM (VALUES
        (N'/panel/sale/orderdetail/list',           1, 1),
        (N'/panel/sale/orderdetail/fetchdata',      2, 2),
        (N'/panel/sale/orderdetail/listbyparentid', 1, 1),
        (N'/panel/sale/orderdetail/getserials',     2, 1000)
    ) a(Path, ActionAccessType, ActionAccessItemType)
    CROSS JOIN (VALUES
        (N'Sale.OrderDetail.Manage'),
        (N'Sale.OrderDetail.ViewWithPrice'),
        (N'Sale.OrderDetail.View'),
        (N'Sale.OrderDetail.ExportToExcel')
    ) r(RoleName);

    -- خروجی اکسل + مشاهده قیمت
    INSERT INTO #OdActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType, EntityName)
    SELECT r.RoleName, a.Path, a.ActionAccessType, a.ActionAccessItemType, a.EntityName
    FROM (VALUES
        (N'/panel/sale/orderdetail/exporttoexcel', 2, 0,    N'Entities.App.Sale.OrderDetail'),
        (N'/panel/sale/orderdetailserial/showprice', 1, 1000, N'Entities.App.Sale.OrderDetailSerial')
    ) a(Path, ActionAccessType, ActionAccessItemType, EntityName)
    CROSS JOIN (VALUES
        (N'Sale.OrderDetail.Manage'),
        (N'Sale.OrderDetail.ViewWithPrice'),
        (N'Sale.OrderDetail.ExportToExcel')
    ) r(RoleName);

    -- مشاهده سریال / پیوست (بدون نوشتن)
    INSERT INTO #OdActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType, EntityName)
    SELECT r.RoleName, a.Path, a.ActionAccessType, a.ActionAccessItemType, a.EntityName
    FROM (VALUES
        (N'/panel/sale/orderdetailserial/edit',                       1, 5, N'Entities.App.Sale.OrderDetailSerial'),
        (N'/panel/sale/orderdetailserial/list',                       1, 1, N'Entities.App.Sale.OrderDetailSerial'),
        (N'/panel/sale/orderdetailserial/fetchdata',                  2, 2, N'Entities.App.Sale.OrderDetailSerial'),
        (N'/panel/sale/orderdetailserial/exporttoexcel',              2, 0, N'Entities.App.Sale.OrderDetailSerial'),
        (N'/panel/sale/orderdetailserialattachment/edit',             1, 5, N'Entities.App.Sale.OrderDetailSerialAttachment'),
        (N'/panel/sale/orderdetailserialattachment/list',             1, 1, N'Entities.App.Sale.OrderDetailSerialAttachment'),
        (N'/panel/sale/orderdetailserialattachment/listbyparentid',   1, 1, N'Entities.App.Sale.OrderDetailSerialAttachment'),
        (N'/panel/sale/orderdetailserialattachment/getlistbyparentid',2, 2, N'Entities.App.Sale.OrderDetailSerialAttachment'),
        (N'/panel/sale/orderdetailserialattachment/fetchdata',        2, 2, N'Entities.App.Sale.OrderDetailSerialAttachment')
    ) a(Path, ActionAccessType, ActionAccessItemType, EntityName)
    CROSS JOIN (VALUES
        (N'Sale.OrderDetail.Manage'),
        (N'Sale.OrderDetail.ViewWithPrice'),
        (N'Sale.OrderDetail.View')
    ) r(RoleName);

    -- مدیریت سریال / پیوست
    INSERT INTO #OdActionAccess (RoleName, Path, ActionAccessType, ActionAccessItemType, EntityName)
    SELECT N'Sale.OrderDetail.Manage', a.Path, a.ActionAccessType, a.ActionAccessItemType, a.EntityName
    FROM (VALUES
        (N'/panel/sale/orderdetail/getserialeditmodal', 2, 1000, N'Entities.App.Sale.OrderDetail'),
        (N'/panel/sale/orderdetail/saveserial',         2, 3,    N'Entities.App.Sale.OrderDetail'),
        (N'/panel/sale/orderdetail/deleteserial',       2, 6,    N'Entities.App.Sale.OrderDetail'),
        (N'/panel/sale/orderdetailserial/new',          1, 4,    N'Entities.App.Sale.OrderDetailSerial'),
        (N'/panel/sale/orderdetailserial/save',         2, 3,    N'Entities.App.Sale.OrderDetailSerial'),
        (N'/panel/sale/orderdetailserial/add',          2, 4,    N'Entities.App.Sale.OrderDetailSerial'),
        (N'/panel/sale/orderdetailserial/update',       2, 5,    N'Entities.App.Sale.OrderDetailSerial'),
        (N'/panel/sale/orderdetailserial/delete',       2, 6,    N'Entities.App.Sale.OrderDetailSerial'),
        (N'/panel/sale/orderdetailserialattachment/new',    1, 4, N'Entities.App.Sale.OrderDetailSerialAttachment'),
        (N'/panel/sale/orderdetailserialattachment/save',   2, 3, N'Entities.App.Sale.OrderDetailSerialAttachment'),
        (N'/panel/sale/orderdetailserialattachment/add',    2, 4, N'Entities.App.Sale.OrderDetailSerialAttachment'),
        (N'/panel/sale/orderdetailserialattachment/update', 2, 5, N'Entities.App.Sale.OrderDetailSerialAttachment'),
        (N'/panel/sale/orderdetailserialattachment/delete', 2, 6, N'Entities.App.Sale.OrderDetailSerialAttachment')
    ) a(Path, ActionAccessType, ActionAccessItemType, EntityName);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT a.Path, a.ActionAccessType, a.ActionAccessItemType, a.EntityName, NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #OdActionAccess a
    INNER JOIN system.Role r ON r.Name = a.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = r.Id AND x.Path = a.Path AND x.ActionAccessType = a.ActionAccessType
    );
    PRINT N'  Inserted controller RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [3] DataProfile RoleAccess ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#OdProfileRole') IS NOT NULL DROP TABLE #OdProfileRole;
    CREATE TABLE #OdProfileRole (ProfileName NVARCHAR(100) NOT NULL, RoleName NVARCHAR(200) NOT NULL);

    INSERT INTO #OdProfileRole (ProfileName, RoleName)
    VALUES
        (N'OrderDetail_WithPrice', N'Sale.OrderDetail.Manage'),
        (N'OrderDetail_WithPrice', N'Sale.OrderDetail.ViewWithPrice'),
        (N'OrderDetail_WithPrice', N'Sale.AfterSales'),
        (N'OrderDetail_WithPrice', N'ShowAllMenus'),
        (N'OrderDetail_NoPrice',   N'Sale.OrderDetail.View'),
        (N'OrderDetail_NoPrice',   N'Sale.AfterSales'),
        (N'OrderDetail_NoPrice',   N'ShowAllMenus');

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT
        N'dataProfile_' + CAST(q.Id AS nvarchar(20)),
        3, 7, q.EntityFullName, q.Title, NULL, q.Id, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #OdProfileRole map
    INNER JOIN system.SavedQuery q ON q.Name = map.ProfileName
    INNER JOIN system.Role r ON r.Name = map.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.ActionAccessType = 3 AND ra.RowId = q.Id AND ra.RoleId = r.Id
    );
    PRINT N'  Inserted DataProfile RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [4] Assign HTS users (direct + group) ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
    CREATE TABLE #UserRoleMap
    (
        Username NVARCHAR(200) NOT NULL,
        RoleName NVARCHAR(200) NOT NULL,
        Source   NVARCHAR(80)  NOT NULL
    );

    IF OBJECT_ID('tempdb..#HtsGrant') IS NOT NULL DROP TABLE #HtsGrant;
    CREATE TABLE #HtsGrant
    (
        Page_ID        INT            NOT NULL,
        Permission_ID  INT            NOT NULL,
        User_FK        INT            NULL,
        AdName         NVARCHAR(200)  NULL,
        HtsUsername    NVARCHAR(200)  NULL,
        Source         NVARCHAR(80)   NOT NULL
    );

    -- ردیف‌های Vw_Permission: هم اعطای مستقیم هم اعضای گروه (ویو هر دو را باز می‌کند)
    INSERT INTO #HtsGrant (Page_ID, Permission_ID, User_FK, AdName, HtsUsername, Source)
    SELECT
        p.Page_ID,
        p.Permission_ID,
        p.User_FK,
        NULLIF(LTRIM(RTRIM(p.ActiveDirectoryUsername)), N''),
        NULLIF(LTRIM(RTRIM(hu.Username)), N''),
        CASE WHEN p.UserGroup_ID IS NULL THEN N'direct' ELSE N'group:' + CAST(p.UserGroup_ID AS nvarchar(20)) END
    FROM [TMS].[TotalSystem].[dbo].[Vw_Permission] p
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON hu.User_ID = p.User_FK
    WHERE p.System_ID = 14
      AND p.Page_ID IN (132, 133)
      AND p.IsActive = 1
      AND p.User_FK IS NOT NULL;

    -- اعضای فعلی گروه‌هایی که روی 132/133 دسترسی دارند (اگر ویو عقب مانده باشد)
    INSERT INTO #HtsGrant (Page_ID, Permission_ID, User_FK, AdName, HtsUsername, Source)
    SELECT DISTINCT
        gp.Page_ID,
        gp.Permission_ID,
        u.User_ID,
        NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''),
        NULLIF(LTRIM(RTRIM(u.Username)), N''),
        N'group-member:' + CAST(gp.UserGroup_ID AS nvarchar(20))
    FROM (
        SELECT DISTINCT Page_ID, Permission_ID, UserGroup_ID
        FROM [TMS].[TotalSystem].[dbo].[Vw_Permission]
        WHERE System_ID = 14
          AND Page_ID IN (132, 133)
          AND IsActive = 1
          AND UserGroup_ID IS NOT NULL
    ) gp
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = gp.UserGroup_ID
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
    WHERE u.IsActive = 1
      AND NOT EXISTS (
          SELECT 1 FROM #HtsGrant x
          WHERE x.Page_ID = gp.Page_ID
            AND x.Permission_ID = gp.Permission_ID
            AND x.User_FK = u.User_ID
      );

    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT x.Username, x.RoleName, x.Source
    FROM (
        -- FullAccess صفحه حواله → مدیریت + قیمت + اکسل
        SELECT COALESCE(g.AdName, g.HtsUsername) AS Username, N'Sale.OrderDetail.Manage' AS RoleName, g.Source
        FROM #HtsGrant g
        WHERE g.Page_ID = 132 AND g.Permission_ID = 2 AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.OrderDetail.ViewWithPrice', g.Source
        FROM #HtsGrant g
        WHERE g.Page_ID = 132 AND g.Permission_ID IN (2, 8, 10) AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.OrderDetail.ExportToExcel', g.Source
        FROM #HtsGrant g
        WHERE g.Page_ID = 132 AND g.Permission_ID IN (2, 10) AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        -- New/Edit/FullAccess سریال → مدیریت سریال
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.OrderDetail.Manage', g.Source
        FROM #HtsGrant g
        WHERE g.Page_ID = 133 AND g.Permission_ID IN (2, 4, 5) AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        -- Read حواله بدون FullAccess → مشاهده بدون قیمت
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.OrderDetail.View', g.Source
        FROM #HtsGrant g
        WHERE g.Page_ID = 132 AND g.Permission_ID = 3 AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL
          AND NOT EXISTS (
              SELECT 1 FROM #HtsGrant f
              WHERE f.Page_ID = 132 AND f.Permission_ID = 2
                AND LOWER(COALESCE(f.AdName, f.HtsUsername)) = LOWER(COALESCE(g.AdName, g.HtsUsername))
          )
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

    -- کسانی که FullAccess دارند نمایه بدون قیمت نبینند
    DECLARE @ViewRoleId bigint = (SELECT Id FROM system.Role WHERE Name = N'Sale.OrderDetail.View');
    DECLARE @ManageRoleId bigint = (SELECT Id FROM system.Role WHERE Name = N'Sale.OrderDetail.Manage');
    DECLARE @ViewStripped int = 0;

    IF @ViewRoleId IS NOT NULL AND @ManageRoleId IS NOT NULL
    BEGIN
        DECLARE @StripUserId bigint, @StripRoleIds nvarchar(max), @StripRoles nvarchar(max);
        DECLARE strip_cur CURSOR LOCAL FAST_FORWARD FOR
            SELECT u.Id, u.RoleIds, u.Roles
            FROM system.[User] u
            WHERE EXISTS (
                SELECT 1 FROM OPENJSON(ISNULL(u.RoleIds, N'[]')) j
                WHERE TRY_CAST(j.value AS bigint) = @ManageRoleId
            )
            AND EXISTS (
                SELECT 1 FROM OPENJSON(ISNULL(u.RoleIds, N'[]')) j
                WHERE TRY_CAST(j.value AS bigint) = @ViewRoleId
            );
        OPEN strip_cur;
        FETCH NEXT FROM strip_cur INTO @StripUserId, @StripRoleIds, @StripRoles;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SELECT @StripRoleIds = N'[' + STUFF((
                SELECT N',' + j.value
                FROM OPENJSON(ISNULL(@StripRoleIds, N'[]')) j
                WHERE TRY_CAST(j.value AS bigint) <> @ViewRoleId
                FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
            IF @StripRoleIds IS NULL OR @StripRoleIds = N'[]' OR @StripRoleIds = N'[]'
                SET @StripRoleIds = N'[]';

            SELECT @StripRoles = N'[' + STUFF((
                SELECT N',"' + REPLACE(j.value, N'"', N'\"') + N'"'
                FROM OPENJSON(ISNULL(@StripRoles, N'[]')) j
                WHERE j.value <> N'Sale.OrderDetail.View'
                FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
            IF @StripRoles IS NULL
                SET @StripRoles = N'[]';

            UPDATE system.[User]
            SET RoleIds = @StripRoleIds,
                Roles = @StripRoles,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @StripUserId;

            SET @ViewStripped = @ViewStripped + 1;
            FETCH NEXT FROM strip_cur INTO @StripUserId, @StripRoleIds, @StripRoles;
        END
        CLOSE strip_cur;
        DEALLOCATE strip_cur;
    END
    PRINT N'  Stripped View from Manage users: ' + CAST(@ViewStripped AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE: Seed_SaleOrderDetail_RolesAndUsers committed ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    PRINT N'=== ERROR: Seed_SaleOrderDetail_RolesAndUsers rolled back ===';
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

PRINT N'--- verify ---';
SELECT r.Name, r.Id,
       (SELECT COUNT(*) FROM system.RoleAccess ra WHERE ra.RoleId = r.Id AND ra.Path LIKE N'/panel/sale/orderdetail%') AS OdAccess,
       (SELECT COUNT(*) FROM system.[User] u CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j WHERE TRY_CAST(j.value AS bigint) = r.Id) AS Users
FROM system.Role r
WHERE r.Name LIKE N'Sale.OrderDetail%'
ORDER BY r.Name;

SELECT u.Username, r.Name AS RoleName
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
INNER JOIN system.Role r ON r.Id = TRY_CAST(j.value AS bigint)
WHERE r.Name LIKE N'Sale.OrderDetail%'
ORDER BY r.Name, u.Username;
