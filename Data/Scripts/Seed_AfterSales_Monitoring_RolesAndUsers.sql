/*
  Seed_AfterSales_Monitoring_RolesAndUsers.sql
  مانیتورینگ فروش مطابق HTS صفحه 151 Sale_Order_Detail_Serial_Public:
    - اعطا روی گروه (Gnr_PageAction_UserGroup + اعضای Gnr_UserGroupMember)
    - اعطا روی کاربر مستقیم (Gnr_PageAction_User) — فقط کپی عضویت گروه کافی نیست
    - گروه‌های ایمیل 542 / 570 حتی اگر فقط برای اعلان باشند
  نگاشت Permission:
    2 FullAccess     → Sale.AfterSales (500000)  گروه 69
    3 Read           → Monitoring.View
    5 Edit           → Monitoring.Edit           گروه‌های 51 و 48
   53 تایید صنایع    → Monitoring.IndustrialConfirm  گروه 304
  188 ثبت ساعت خروج  → Monitoring.Exit           گروه 543
  189 دسترسی انبار   → Monitoring.Inventory      گروه 544
   10 اکسل           → روی نقش ویرایش/خروج/انبار/صنایع (در HTS کسی perm 10 نداشت)
  ایمیل خروج: 542 → 500017 ، 570 → 500018
  Bucket 2923 در HTS فقط لاگ ارسال است نه لیست گیرنده.
  Idempotent. Requires linked server [TMS] → TotalSystem.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-aftersales-monitoring-roles';
    DECLARE @EntityPascal NVARCHAR(200) = N'Entities.App.Sale.OrderDetailSerial';

    -----------------------------------------------------------------------------
    PRINT N'=== [1] Roles 500017..500023 ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#MonRoles') IS NOT NULL DROP TABLE #MonRoles;
    CREATE TABLE #MonRoles (WantId BIGINT NOT NULL, Name NVARCHAR(200) NOT NULL, Title NVARCHAR(200) NOT NULL);
    INSERT INTO #MonRoles (WantId, Name, Title) VALUES
        (500017, N'Sale.AfterSales.FactoryExitNotify', N'مانیتورینگ فروش - اعلان خروج کارخانه (HTS 542)'),
        (500018, N'Sale.AfterSales.FactoryExitCustomerCall', N'مانیتورینگ فروش - تماس خروج کارخانه (HTS 570)'),
        (500019, N'Sale.AfterSales.Monitoring.View', N'مانیتورینگ فروش - مشاهده (HTS 61/350/590)'),
        (500020, N'Sale.AfterSales.Monitoring.Edit', N'مانیتورینگ فروش - ویرایش (HTS 51/48)'),
        (500021, N'Sale.AfterSales.Monitoring.IndustrialConfirm', N'مانیتورینگ فروش - تایید صنایع (HTS 304)'),
        (500022, N'Sale.AfterSales.Monitoring.Exit', N'مانیتورینگ فروش - ثبت ساعت خروج (HTS 543)'),
        (500023, N'Sale.AfterSales.Monitoring.Inventory', N'مانیتورینگ فروش - دسترسی انبار (HTS 544)');

    DECLARE @Rid BIGINT, @RName NVARCHAR(200), @RTitle NVARCHAR(200);
    DECLARE rc CURSOR LOCAL FAST_FORWARD FOR SELECT WantId, Name, Title FROM #MonRoles;
    OPEN rc; FETCH NEXT FROM rc INTO @Rid, @RName, @RTitle;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = @RName)
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Id = @Rid)
            BEGIN
                SET IDENTITY_INSERT system.Role ON;
                INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                    CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
                VALUES (@Rid, @RName, @RTitle, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
                SET IDENTITY_INSERT system.Role OFF;
                PRINT N'  CREATED ' + @RName;
            END
            ELSE
            BEGIN
                INSERT INTO system.Role (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                    CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
                VALUES (@RName, @RTitle, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
                PRINT N'  CREATED (new id) ' + @RName;
            END
        END
        ELSE PRINT N'  EXISTS ' + @RName;
        FETCH NEXT FROM rc INTO @Rid, @RName, @RTitle;
    END
    CLOSE rc; DEALLOCATE rc;

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sale.AfterSales')
        THROW 51021, N'نقش Sale.AfterSales یافت نشد.', 1;

    -----------------------------------------------------------------------------
    PRINT N'=== [2] RoleAccess (monitoring paths + profiles 233/234) ===';
    -----------------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#MonActionMap') IS NOT NULL DROP TABLE #MonActionMap;
    CREATE TABLE #MonActionMap
    (
        RoleName NVARCHAR(200) NOT NULL,
        Path NVARCHAR(300) NOT NULL,
        ActionAccessType INT NOT NULL,
        ActionAccessItemType INT NOT NULL
    );

    -- مشاهده صفحه / داده
    INSERT INTO #MonActionMap (RoleName, Path, ActionAccessType, ActionAccessItemType)
    SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/sale/orderdetailserial/monitoring', 1, 1),
        (N'/panel/sale/orderdetailserial/fetchdata', 2, 2)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'ShowAllMenus'), (N'Sale.AfterSales'),
        (N'Sale.AfterSales.Monitoring.View'),
        (N'Sale.AfterSales.Monitoring.Edit'),
        (N'Sale.AfterSales.Monitoring.IndustrialConfirm'),
        (N'Sale.AfterSales.Monitoring.Exit'),
        (N'Sale.AfterSales.Monitoring.Inventory')
    ) r(RoleName);

    -- ویرایش عملیاتی + اکسل + اقلام پروژه
    INSERT INTO #MonActionMap (RoleName, Path, ActionAccessType, ActionAccessItemType)
    SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/sale/orderdetailserial/monitoringedit', 1, 5),
        (N'/panel/sale/orderdetailserial/monitoringsave', 2, 5),
        (N'/panel/sale/orderdetailserial/getprojectparts', 2, 1000),
        (N'/panel/sale/orderdetailserial/exporttoexcel', 2, 0)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'ShowAllMenus'), (N'Sale.AfterSales'),
        (N'Sale.AfterSales.Monitoring.Edit'),
        (N'Sale.AfterSales.Monitoring.Exit'),
        (N'Sale.AfterSales.Monitoring.Inventory')
    ) r(RoleName);

    INSERT INTO #MonActionMap (RoleName, Path, ActionAccessType, ActionAccessItemType) VALUES
        (N'Sale.AfterSales.Monitoring.IndustrialConfirm', N'/panel/sale/orderdetailserial/getprojectparts', 2, 1000),
        (N'Sale.AfterSales.Monitoring.IndustrialConfirm', N'/panel/sale/orderdetailserial/exporttoexcel', 2, 0),
        (N'ShowAllMenus', N'/panel/sale/orderdetailserial/industrialconfirm', 2, 1000),
        (N'Sale.AfterSales.Monitoring.IndustrialConfirm', N'/panel/sale/orderdetailserial/industrialconfirm', 2, 1000),
        (N'ShowAllMenus', N'/panel/sale/orderdetailserial/monitoringexitaccess', 1, 1000),
        (N'Sale.AfterSales.Monitoring.Exit', N'/panel/sale/orderdetailserial/monitoringexitaccess', 1, 1000),
        (N'ShowAllMenus', N'/panel/sale/orderdetailserial/monitoringinventoryaccess', 1, 1000),
        (N'Sale.AfterSales.Monitoring.Inventory', N'/panel/sale/orderdetailserial/monitoringinventoryaccess', 1, 1000);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT a.Path, a.ActionAccessType, a.ActionAccessItemType, @EntityPascal, NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #MonActionMap a
    INNER JOIN system.Role r ON r.Name = a.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = r.Id AND x.Path = a.Path AND x.ActionAccessType = a.ActionAccessType
    );
    PRINT N'  Inserted controller RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -- تایید صنایع نباید روی همه Sale.AfterSales باشد (فقط 304 + ShowAllMenus)
    DELETE ra
    FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    WHERE r.Name = N'Sale.AfterSales'
      AND ra.Path = N'/panel/sale/orderdetailserial/industrialconfirm';
    PRINT N'  Revoked AfterSales industrialconfirm: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -- نمایه‌های صنایع / انبار
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        N'dataProfile_' + CAST(q.Id AS nvarchar(20)),
        3, 7, @EntityPascal, q.Title, NULL, q.Id, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.SavedQuery q
    INNER JOIN system.Role r ON r.Name IN (
        N'Sale.AfterSales', N'ShowAllMenus',
        N'Sale.AfterSales.Monitoring.View',
        N'Sale.AfterSales.Monitoring.Edit',
        N'Sale.AfterSales.Monitoring.IndustrialConfirm',
        N'Sale.AfterSales.Monitoring.Exit',
        N'Sale.AfterSales.Monitoring.Inventory'
    )
    WHERE q.Name IN (
        N'AfterSales_OrderDetailSerial_Monitoring_Industries',
        N'AfterSales_OrderDetailSerial_Monitoring_Warehouse'
    )
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess ra
          WHERE ra.ActionAccessType = 3 AND ra.RowId = q.Id AND ra.RoleId = r.Id
      );
    PRINT N'  Inserted DataProfile RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -----------------------------------------------------------------------------
    PRINT N'=== [3] Menu AccessRoleIds merge ===';
    -----------------------------------------------------------------------------
    DECLARE @MenuId BIGINT = (SELECT TOP 1 Id FROM system.SystemMenu WHERE Name = N'AfterSalesServiceSystem');
    IF @MenuId IS NOT NULL
    BEGIN
        IF OBJECT_ID('tempdb..#MenuRoles') IS NOT NULL DROP TABLE #MenuRoles;
        CREATE TABLE #MenuRoles (RoleName NVARCHAR(200) NOT NULL PRIMARY KEY);
        INSERT INTO #MenuRoles (RoleName)
        SELECT DISTINCT j.value
        FROM system.SystemMenu m
        CROSS APPLY OPENJSON(ISNULL(m.AccessRoles, N'[]')) j
        WHERE m.Id = @MenuId AND NULLIF(LTRIM(RTRIM(j.value)), N'') IS NOT NULL;

        INSERT INTO #MenuRoles (RoleName)
        SELECT v.RoleName FROM (VALUES
            (N'Sale.AfterSales'),
            (N'Sale.AfterSales.FactoryExitNotify'),
            (N'Sale.AfterSales.FactoryExitCustomerCall'),
            (N'Sale.AfterSales.Monitoring.View'),
            (N'Sale.AfterSales.Monitoring.Edit'),
            (N'Sale.AfterSales.Monitoring.IndustrialConfirm'),
            (N'Sale.AfterSales.Monitoring.Exit'),
            (N'Sale.AfterSales.Monitoring.Inventory'),
            (N'ShowAllMenus')
        ) v(RoleName)
        WHERE NOT EXISTS (SELECT 1 FROM #MenuRoles x WHERE x.RoleName = v.RoleName);

        DECLARE @AccessRoles NVARCHAR(MAX);
        DECLARE @AccessRoleIds NVARCHAR(MAX);
        SELECT @AccessRoles = N'[' + STUFF((
            SELECT N',"' + RoleName + N'"' FROM #MenuRoles ORDER BY RoleName
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
        SELECT @AccessRoleIds = N'[' + STUFF((
            SELECT N',' + CAST(r.Id AS nvarchar(20))
            FROM #MenuRoles m INNER JOIN system.Role r ON r.Name = m.RoleName
            ORDER BY r.Id
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';

        UPDATE system.SystemMenu
        SET AccessRoles = @AccessRoles, AccessRoleIds = @AccessRoleIds,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;
        PRINT N'  Menu AccessRoleIds merged';
    END
    ELSE PRINT N'  WARN menu AfterSalesServiceSystem not found';

    -----------------------------------------------------------------------------
    PRINT N'=== [4] Assign HTS users (direct + group + notify 542/570) ===';
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
        Permission_ID  INT            NOT NULL,
        User_FK        INT            NOT NULL,
        AdName         NVARCHAR(200)  NULL,
        HtsUsername    NVARCHAR(200)  NULL,
        Source         NVARCHAR(80)   NOT NULL
    );

    -- اعضای گروه‌هایی که روی صفحه 151 دسترسی دارند (فعال و غیرفعال)
    INSERT INTO #HtsGrant (Permission_ID, User_FK, AdName, HtsUsername, Source)
    SELECT DISTINCT
        pa.Permission_FK,
        u.User_ID,
        NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''),
        NULLIF(LTRIM(RTRIM(u.Username)), N''),
        N'group:' + CAST(pug.UserGroup_FK AS nvarchar(20))
    FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_UserGroup] pug
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pug.PageAction_FK
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = pug.UserGroup_FK
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
    WHERE pa.Page_FK = 151;

    -- اعطای مستقیم به کاربر (نه از طریق گروه)
    INSERT INTO #HtsGrant (Permission_ID, User_FK, AdName, HtsUsername, Source)
    SELECT
        pa.Permission_FK,
        u.User_ID,
        NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''),
        NULLIF(LTRIM(RTRIM(u.Username)), N''),
        N'direct'
    FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_User] pau
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pau.PageAction_FK
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = pau.User_FK
    WHERE pa.Page_FK = 151
      AND NOT EXISTS (
          SELECT 1 FROM #HtsGrant x
          WHERE x.Permission_ID = pa.Permission_FK AND x.User_FK = u.User_ID AND x.Source = N'direct'
      );

    -- اعضای 542 / 570 حتی اگر ردیف PageAction نداشته باشند
    INSERT INTO #HtsGrant (Permission_ID, User_FK, AdName, HtsUsername, Source)
    SELECT
        CASE WHEN m.UserGroup_FK = 542 THEN 542 ELSE 570 END,
        u.User_ID,
        NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''),
        NULLIF(LTRIM(RTRIM(u.Username)), N''),
        N'notify:' + CAST(m.UserGroup_FK AS nvarchar(20))
    FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
    WHERE m.UserGroup_FK IN (542, 570);

    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT x.Username, x.RoleName, x.Source
    FROM (
        SELECT COALESCE(g.AdName, g.HtsUsername) AS Username, N'Sale.AfterSales' AS RoleName, g.Source
        FROM #HtsGrant g
        WHERE g.Permission_ID = 2 AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.AfterSales.Monitoring.View', g.Source
        FROM #HtsGrant g
        WHERE g.Permission_ID = 3 AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.AfterSales.Monitoring.Edit', g.Source
        FROM #HtsGrant g
        WHERE g.Permission_ID = 5 AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.AfterSales.Monitoring.IndustrialConfirm', g.Source
        FROM #HtsGrant g
        WHERE g.Permission_ID = 53 AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.AfterSales.Monitoring.Exit', g.Source
        FROM #HtsGrant g
        WHERE g.Permission_ID = 188 AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.AfterSales.Monitoring.Inventory', g.Source
        FROM #HtsGrant g
        WHERE g.Permission_ID = 189 AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.AfterSales.FactoryExitNotify', g.Source
        FROM #HtsGrant g
        WHERE (g.Permission_ID = 542 OR g.Source LIKE N'notify:542' OR g.Source = N'group:542')
          AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL

        UNION ALL
        SELECT COALESCE(g.AdName, g.HtsUsername), N'Sale.AfterSales.FactoryExitCustomerCall', g.Source
        FROM #HtsGrant g
        WHERE (g.Permission_ID = 570 OR g.Source LIKE N'notify:570' OR g.Source = N'group:570')
          AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL
    ) x
    WHERE x.Username IS NOT NULL;

    -- نام جایگزین Gnr_User.Username اگر با AD فرق دارد
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

    INSERT INTO #UserRoleMap (Username, RoleName, Source)
    SELECT DISTINCT hu.ActiveDirectoryUsername, m.RoleName, m.Source + N'+htsAd'
    FROM #UserRoleMap m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu
        ON LOWER(hu.Username) = LOWER(m.Username)
    WHERE hu.ActiveDirectoryUsername IS NOT NULL
      AND LOWER(hu.ActiveDirectoryUsername) <> LOWER(m.Username)
      AND NOT EXISTS (
          SELECT 1 FROM #UserRoleMap x
          WHERE LOWER(x.Username) = LOWER(hu.ActiveDirectoryUsername) AND x.RoleName = m.RoleName
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
        FROM system.[User] u
        WHERE LOWER(u.Username) = LOWER(@MapUsername)
           OR LOWER(REPLACE(ISNULL(u.Email, N''), N'@havayar.com', N'')) = LOWER(@MapUsername)
        ORDER BY CASE WHEN LOWER(u.Username) = LOWER(@MapUsername) THEN 0 ELSE 1 END;

        IF @MapUserId IS NULL
        BEGIN
            PRINT N'  MISSING USER (skip): ' + @MapUsername + N' for role ' + @MapRoleName;
            SET @Missing = @Missing + 1;
        END
        ELSE IF @MapRoleId IS NULL
            PRINT N'  MISSING ROLE (skip): ' + @MapRoleName;
        ELSE IF EXISTS (
            SELECT 1 FROM system.[User] u
            CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
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

    -- مشاهده را از کسانی که ویرایش/مدیریت کامل دارند بردار
    DECLARE @ViewRoleId bigint = (SELECT Id FROM system.Role WHERE Name = N'Sale.AfterSales.Monitoring.View');
    DECLARE @EditRoleId bigint = (SELECT Id FROM system.Role WHERE Name = N'Sale.AfterSales.Monitoring.Edit');
    DECLARE @AfterSalesRoleId bigint = (SELECT Id FROM system.Role WHERE Name = N'Sale.AfterSales');
    DECLARE @ViewStripped int = 0;

    IF @ViewRoleId IS NOT NULL AND @EditRoleId IS NOT NULL AND @AfterSalesRoleId IS NOT NULL
    BEGIN
        DECLARE @StripUserId bigint, @StripRoleIds nvarchar(max), @StripRoles nvarchar(max);
        DECLARE strip_cur CURSOR LOCAL FAST_FORWARD FOR
            SELECT u.Id, u.RoleIds, u.Roles
            FROM system.[User] u
            WHERE EXISTS (
                SELECT 1 FROM OPENJSON(ISNULL(u.RoleIds, N'[]')) j
                WHERE TRY_CAST(j.value AS bigint) = @ViewRoleId
            )
            AND EXISTS (
                SELECT 1 FROM OPENJSON(ISNULL(u.RoleIds, N'[]')) j
                WHERE TRY_CAST(j.value AS bigint) IN (@EditRoleId, @AfterSalesRoleId)
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
            IF @StripRoleIds IS NULL OR @StripRoleIds = N'[]'
                SET @StripRoleIds = N'[]';

            SELECT @StripRoles = N'[' + STUFF((
                SELECT N',"' + REPLACE(j.value, N'"', N'\"') + N'"'
                FROM OPENJSON(ISNULL(@StripRoles, N'[]')) j
                WHERE j.value <> N'Sale.AfterSales.Monitoring.View'
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
    PRINT N'  Stripped View from Edit/AfterSales users: ' + CAST(@ViewStripped AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE: Seed_AfterSales_Monitoring_RolesAndUsers committed ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    PRINT N'=== ERROR: Seed_AfterSales_Monitoring_RolesAndUsers rolled back ===';
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

PRINT N'--- verify ---';
SELECT r.Id, r.Name, r.Title,
       (SELECT COUNT(*) FROM system.RoleAccess ra
        WHERE ra.RoleId = r.Id
          AND (ra.Path LIKE N'/panel/sale/orderdetailserial/%' OR ra.Path LIKE N'dataProfile_%')) AS MonAccess,
       (SELECT COUNT(*) FROM system.[User] u
        CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
        WHERE TRY_CAST(j.value AS bigint) = r.Id) AS Users
FROM system.Role r
WHERE r.Name IN (
    N'Sale.AfterSales',
    N'Sale.AfterSales.FactoryExitNotify',
    N'Sale.AfterSales.FactoryExitCustomerCall',
    N'Sale.AfterSales.Monitoring.View',
    N'Sale.AfterSales.Monitoring.Edit',
    N'Sale.AfterSales.Monitoring.IndustrialConfirm',
    N'Sale.AfterSales.Monitoring.Exit',
    N'Sale.AfterSales.Monitoring.Inventory',
    N'ShowAllMenus'
)
ORDER BY r.Id;

SELECT r.Name AS RoleName, ra.Path, ra.ActionAccessType
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
WHERE r.Name LIKE N'Sale.AfterSales.Monitoring%'
   OR (r.Name IN (N'Sale.AfterSales.FactoryExitNotify', N'Sale.AfterSales.FactoryExitCustomerCall')
       AND ra.Path LIKE N'/panel/sale/orderdetailserial/%')
   OR (r.Name = N'Sale.AfterSales' AND ra.Path IN (
        N'/panel/sale/orderdetailserial/industrialconfirm',
        N'/panel/sale/orderdetailserial/monitoringexitaccess',
        N'/panel/sale/orderdetailserial/monitoringinventoryaccess'))
ORDER BY r.Name, ra.Path;

SELECT u.Username, r.Name AS RoleName
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
INNER JOIN system.Role r ON r.Id = TRY_CAST(j.value AS bigint)
WHERE r.Name IN (
    N'Sale.AfterSales',
    N'Sale.AfterSales.FactoryExitNotify',
    N'Sale.AfterSales.FactoryExitCustomerCall',
    N'Sale.AfterSales.Monitoring.View',
    N'Sale.AfterSales.Monitoring.Edit',
    N'Sale.AfterSales.Monitoring.IndustrialConfirm',
    N'Sale.AfterSales.Monitoring.Exit',
    N'Sale.AfterSales.Monitoring.Inventory'
)
ORDER BY r.Name, u.Username;
