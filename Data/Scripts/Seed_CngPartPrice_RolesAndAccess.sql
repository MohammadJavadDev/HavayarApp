/*
Seed_CngPartPrice_RolesAndAccess.sql
دسترسی مدیریت قیمت قطعات CNG مطابق HTS صفحه 276:
  108 کارشناسان مدیریت قیمت قطعات CNG → Cng.PartPrice.Manage
  327 کارشناسان مشاهده مدیریت قیمت قطعات CNG → Cng.PartPrice.View
  350 مشاهده کلی HTS → Cng.PartPrice.View (+ نقش موجود مشاهده کلی HTS)
  231/232 تعمیرات CNG → Cng.Repairs.PartPriceAttachment (فقط افزودن/حذف پیوست)
UTF-8 BOM. Idempotent. Requires [TMS] for user assign.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-cng-partprice-roles';

    PRINT N'=== [1] Roles ===';
    IF OBJECT_ID('tempdb..#CngRoles') IS NOT NULL DROP TABLE #CngRoles;
    CREATE TABLE #CngRoles (WantId BIGINT NOT NULL, Name NVARCHAR(200) NOT NULL, Title NVARCHAR(200) NOT NULL);
    INSERT INTO #CngRoles (WantId, Name, Title) VALUES
        (600007, N'Cng.PartPrice.Manage', N'کارشناسان مدیریت قیمت قطعات CNG (HTS 108)'),
        (600008, N'Cng.PartPrice.View', N'مشاهده مدیریت قیمت قطعات CNG (HTS 327/350)'),
        (600009, N'Cng.Repairs.PartPriceAttachment', N'تعمیرات CNG - ثبت پیوست قیمت قطعات (HTS 231/232)');

    DECLARE @Rid BIGINT, @RName NVARCHAR(200), @RTitle NVARCHAR(200);
    DECLARE rc CURSOR LOCAL FAST_FORWARD FOR SELECT WantId, Name, Title FROM #CngRoles;
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

    PRINT N'=== [2] Controller RoleAccess ===';
    IF OBJECT_ID('tempdb..#Map') IS NOT NULL DROP TABLE #Map;
    CREATE TABLE #Map (RoleName NVARCHAR(200) NOT NULL, Path NVARCHAR(300) NOT NULL, AType INT NOT NULL, IType INT NOT NULL);

    -- PartPrice View: list/fetchdata/edit(view) — NO new/save/delete/export
    INSERT INTO #Map SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/cng/partprice/list', 1, 1),
        (N'/panel/cng/partprice/fetchdata', 2, 2),
        (N'/panel/cng/partprice/edit', 1, 5)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES (N'ShowAllMenus'), (N'مشاهده کلی HTS'), (N'Cng.PartPrice.View'), (N'Cng.PartPrice.Manage')) r(RoleName);

    -- PartPrice Manage: CRUD + export
    INSERT INTO #Map SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/cng/partprice/new', 1, 4),
        (N'/panel/cng/partprice/save', 2, 3),
        (N'/panel/cng/partprice/add', 2, 4),
        (N'/panel/cng/partprice/update', 2, 5),
        (N'/panel/cng/partprice/delete', 2, 6),
        (N'/panel/cng/partprice/exporttoexcel', 2, 0)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES (N'ShowAllMenus'), (N'Cng.PartPrice.Manage')) r(RoleName);

    -- Attachment list/view for View + Manage + ShowAllMenus
    INSERT INTO #Map SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/cng/partpriceattachment/listbyparentid', 1, 1),
        (N'/panel/cng/partpriceattachment/getlistbyparentid', 2, 2)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES (N'ShowAllMenus'), (N'مشاهده کلی HTS'), (N'Cng.PartPrice.View'), (N'Cng.PartPrice.Manage')) r(RoleName);

    -- Attachment Add/Delete only for Repairs + ShowAllMenus
    INSERT INTO #Map SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/cng/partpriceattachment/new', 1, 4),
        (N'/panel/cng/partpriceattachment/save', 2, 3),
        (N'/panel/cng/partpriceattachment/add', 2, 4),
        (N'/panel/cng/partpriceattachment/delete', 2, 6)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES (N'ShowAllMenus'), (N'Cng.Repairs.PartPriceAttachment')) r(RoleName);

    DELETE ra FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    WHERE (ra.Path LIKE N'/panel/cng/partprice/%' OR ra.Path LIKE N'/panel/cng/partpriceattachment/%')
      AND r.Name IN (
            N'Cng.PartPrice.Manage', N'Cng.PartPrice.View',
            N'Cng.Repairs.PartPriceAttachment',
            N'ShowAllMenus', N'مشاهده کلی HTS'
      );

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT m.Path, m.AType, m.IType,
        CASE WHEN m.Path LIKE N'/panel/cng/partpriceattachment/%'
             THEN N'Entities.App.Cng.PartPriceAttachment'
             ELSE N'Entities.App.Cng.PartPrice' END,
        NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #Map m
    INNER JOIN system.Role r ON r.Name = m.RoleName;
    PRINT N'  Controller RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== [3] Menu AccessRoleIds merge ===';
    DECLARE @MenuId BIGINT = (SELECT TOP 1 Id FROM system.SystemMenu WHERE Name = N'CngSystem');
    IF @MenuId IS NOT NULL
    BEGIN
        IF OBJECT_ID('tempdb..#MenuRoles') IS NOT NULL DROP TABLE #MenuRoles;
        CREATE TABLE #MenuRoles (RoleName NVARCHAR(200) NOT NULL PRIMARY KEY);
        INSERT INTO #MenuRoles (RoleName)
        SELECT DISTINCT LTRIM(RTRIM(j.value))
        FROM system.SystemMenu m CROSS APPLY OPENJSON(ISNULL(m.AccessRoles, N'[]')) j
        WHERE m.Id = @MenuId AND LTRIM(RTRIM(j.value)) <> N'';

        INSERT INTO #MenuRoles (RoleName)
        SELECT v.RoleName FROM (VALUES
            (N'Cng.PartPrice.Manage'), (N'Cng.PartPrice.View'),
            (N'Cng.Repairs.PartPriceAttachment'),
            (N'مشاهده کلی HTS'), (N'ShowAllMenus')
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
    ELSE PRINT N'  WARN menu CngSystem not found';

    PRINT N'=== [4] Assign HTS page 276 users (if TMS linked) ===';
    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
    BEGIN
        IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
        CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL, RoleName NVARCHAR(200) NOT NULL, PRIMARY KEY (Username, RoleName));

        -- 108 Manage
        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')), N'Cng.PartPrice.Manage'
        FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_UserGroup] pug
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pug.PageAction_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = pug.UserGroup_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        WHERE pug.UserGroup_FK = 108 AND pa.Page_FK = 276
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

        -- 327 View
        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')), N'Cng.PartPrice.View'
        FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_UserGroup] pug
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pug.PageAction_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = pug.UserGroup_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        WHERE pug.UserGroup_FK = 327 AND pa.Page_FK = 276
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM #UserRoleMap x WHERE x.Username = COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) AND x.RoleName = N'Cng.PartPrice.View');

        -- 350 View + مشاهده کلی HTS
        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')), v.RoleName
        FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_UserGroup] pug
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pug.PageAction_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = pug.UserGroup_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        CROSS JOIN (VALUES (N'Cng.PartPrice.View'), (N'مشاهده کلی HTS')) v(RoleName)
        WHERE pug.UserGroup_FK = 350 AND pa.Page_FK = 276
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM #UserRoleMap x WHERE x.Username = COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) AND x.RoleName = v.RoleName);

        -- 231 / 232 attachment role (from Cng_Repairs page groups — membership by UserGroup)
        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')), N'Cng.Repairs.PartPriceAttachment'
        FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        WHERE m.UserGroup_FK IN (231, 232)
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM #UserRoleMap x WHERE x.Username = COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) AND x.RoleName = N'Cng.Repairs.PartPriceAttachment');

        DECLARE @Assigned INT = 0, @Missing INT = 0;
        DECLARE @UName NVARCHAR(200), @RoleN NVARCHAR(200), @UserId BIGINT, @RoleId BIGINT;
        DECLARE @RolesJson NVARCHAR(MAX), @RoleIdsJson NVARCHAR(MAX);

        DECLARE uc CURSOR LOCAL FAST_FORWARD FOR SELECT Username, RoleName FROM #UserRoleMap;
        OPEN uc; FETCH NEXT FROM uc INTO @UName, @RoleN;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SELECT TOP 1 @UserId = u.Id FROM system.[User] u
            WHERE LOWER(u.Username) = LOWER(@UName)
               OR LOWER(REPLACE(ISNULL(u.Email, N''), N'@havayar.com', N'')) = LOWER(@UName);
            SELECT TOP 1 @RoleId = r.Id FROM system.Role r WHERE r.Name = @RoleN;
            IF @UserId IS NULL OR @RoleId IS NULL
                SET @Missing = @Missing + 1;
            ELSE
            BEGIN
                SELECT @RolesJson = ISNULL(Roles, N'[]'), @RoleIdsJson = ISNULL(RoleIds, N'[]') FROM system.[User] WHERE Id = @UserId;
                IF NOT EXISTS (SELECT 1 FROM OPENJSON(@RolesJson) WHERE LOWER([value]) = LOWER(@RoleN))
                    SET @RolesJson = JSON_MODIFY(@RolesJson, N'append $', @RoleN);
                IF NOT EXISTS (SELECT 1 FROM OPENJSON(@RoleIdsJson) WHERE TRY_CAST([value] AS BIGINT) = @RoleId)
                    SET @RoleIdsJson = JSON_MODIFY(@RoleIdsJson, N'append $', @RoleId);
                UPDATE system.[User] SET Roles = @RolesJson, RoleIds = @RoleIdsJson,
                    ModifiedById = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
                WHERE Id = @UserId;
                SET @Assigned = @Assigned + 1;
            END
            SET @UserId = NULL; SET @RoleId = NULL;
            FETCH NEXT FROM uc INTO @UName, @RoleN;
        END
        CLOSE uc; DEALLOCATE uc;
        PRINT N'  User-role assigns touched: ' + CAST(@Assigned AS nvarchar(20)) + N', missing user/role: ' + CAST(@Missing AS nvarchar(20));
    END
    ELSE PRINT N'  WARN: Linked Server TMS missing — user assign skipped';

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_CngPartPrice_RolesAndAccess ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.Role WHERE Name LIKE N'Cng.PartPrice%' OR Name = N'Cng.Repairs.PartPriceAttachment' ORDER BY Id;
SELECT r.Name, COUNT(*) AS Acc
FROM system.RoleAccess ra INNER JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.Path LIKE N'/panel/cng/partprice%'
GROUP BY r.Name ORDER BY r.Name;
