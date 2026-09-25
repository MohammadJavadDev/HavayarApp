/*
Seed_CngBasicInfo_RolesAndAccess.sql
دسترسی اطلاعات پایه CNG مطابق HTS صفحات 433/434:
  341 کارشناسان پارامترهای فنی → Cng.FailureInfo.Manage (New/Edit/Delete + Read)
  350 مشاهده کلی HTS → Cng.FailureInfo.View / Cng.StationInfo.View (+ نقش موجود مشاهده کلی HTS)
  345 کارشناسان اطلاعات مشتری/جایگاه → Cng.StationInfo.Manage + Cng.StationInfo.ShowAll
  340 کارشناسان نمایندگی‌ها → Cng.StationInfo.Agency (New + لیست خود)
UTF-8 BOM. Idempotent. Requires [TMS] for user assign.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-cng-basicinfo-roles';

    PRINT N'=== [1] Roles ===';
    IF OBJECT_ID('tempdb..#CngRoles') IS NOT NULL DROP TABLE #CngRoles;
    CREATE TABLE #CngRoles (WantId BIGINT NOT NULL, Name NVARCHAR(200) NOT NULL, Title NVARCHAR(200) NOT NULL);
    INSERT INTO #CngRoles (WantId, Name, Title) VALUES
        (600001, N'Cng.FailureInfo.Manage', N'پارامترهای فنی CNG - مدیریت (HTS 341)'),
        (600002, N'Cng.FailureInfo.View', N'پارامترهای فنی CNG - مشاهده (HTS 350)'),
        (600003, N'Cng.StationInfo.Manage', N'اطلاعات مشتری/جایگاه CNG - مدیریت (HTS 345)'),
        (600004, N'Cng.StationInfo.ShowAll', N'اطلاعات مشتری/جایگاه CNG - نمایش همه (HTS 345 ShowAll)'),
        (600005, N'Cng.StationInfo.Agency', N'اطلاعات مشتری/جایگاه CNG - نمایندگی (HTS 340)'),
        (600006, N'Cng.StationInfo.View', N'اطلاعات مشتری/جایگاه CNG - مشاهده (HTS 350)');

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

    -- FailureInfo View: list/fetchdata/edit(view)
    INSERT INTO #Map SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/cng/failureinfo/list', 1, 1),
        (N'/panel/cng/failureinfo/fetchdata', 2, 2),
        (N'/panel/cng/failureinfo/edit', 1, 5),
        (N'/panel/cng/failureinfo/exporttoexcel', 2, 0)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES (N'ShowAllMenus'), (N'مشاهده کلی HTS'), (N'Cng.FailureInfo.View'), (N'Cng.FailureInfo.Manage')) r(RoleName);

    -- FailureInfo Manage: CRUD
    INSERT INTO #Map SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/cng/failureinfo/new', 1, 4),
        (N'/panel/cng/failureinfo/save', 2, 3),
        (N'/panel/cng/failureinfo/add', 2, 4),
        (N'/panel/cng/failureinfo/update', 2, 5),
        (N'/panel/cng/failureinfo/delete', 2, 6)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES (N'ShowAllMenus'), (N'Cng.FailureInfo.Manage')) r(RoleName);

    -- StationInfo View
    INSERT INTO #Map SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/cng/stationinfo/list', 1, 1),
        (N'/panel/cng/stationinfo/fetchdata', 2, 2),
        (N'/panel/cng/stationinfo/edit', 1, 5),
        (N'/panel/cng/stationinfo/exporttoexcel', 2, 0),
        (N'/panel/cng/stationinfo/getserials', 2, 1000)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'ShowAllMenus'), (N'مشاهده کلی HTS'),
        (N'Cng.StationInfo.View'), (N'Cng.StationInfo.Manage'),
        (N'Cng.StationInfo.ShowAll'), (N'Cng.StationInfo.Agency')
    ) r(RoleName);

    -- StationInfo Manage full CRUD
    INSERT INTO #Map SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/cng/stationinfo/new', 1, 4),
        (N'/panel/cng/stationinfo/save', 2, 3),
        (N'/panel/cng/stationinfo/add', 2, 4),
        (N'/panel/cng/stationinfo/update', 2, 5),
        (N'/panel/cng/stationinfo/delete', 2, 6)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES (N'ShowAllMenus'), (N'Cng.StationInfo.Manage'), (N'Cng.StationInfo.ShowAll')) r(RoleName);

    -- Agency: New/Add/Save only (no delete) — HTS 340 New
    INSERT INTO #Map SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/cng/stationinfo/new', 1, 4),
        (N'/panel/cng/stationinfo/save', 2, 3),
        (N'/panel/cng/stationinfo/add', 2, 4),
        (N'/panel/cng/stationinfo/update', 2, 5)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES (N'Cng.StationInfo.Agency')) r(RoleName);

    DELETE ra FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    WHERE (ra.Path LIKE N'/panel/cng/failureinfo/%' OR ra.Path LIKE N'/panel/cng/stationinfo/%')
      AND r.Name IN (
            N'Cng.FailureInfo.Manage', N'Cng.FailureInfo.View',
            N'Cng.StationInfo.Manage', N'Cng.StationInfo.ShowAll',
            N'Cng.StationInfo.Agency', N'Cng.StationInfo.View',
            N'ShowAllMenus', N'مشاهده کلی HTS'
      );

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT m.Path, m.AType, m.IType,
        CASE WHEN m.Path LIKE N'/panel/cng/failureinfo/%' THEN N'Entities.App.Cng.FailureInfo' ELSE N'Entities.App.Cng.StationInfo' END,
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
            (N'Cng.FailureInfo.Manage'), (N'Cng.FailureInfo.View'),
            (N'Cng.StationInfo.Manage'), (N'Cng.StationInfo.ShowAll'),
            (N'Cng.StationInfo.Agency'), (N'Cng.StationInfo.View'),
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

    PRINT N'=== [4] Assign HTS page 433/434 users (if TMS linked) ===';
    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
    BEGIN
        IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
        CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL, RoleName NVARCHAR(200) NOT NULL, PRIMARY KEY (Username, RoleName));

        -- 341 Manage Failure
        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')), N'Cng.FailureInfo.Manage'
        FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_UserGroup] pug
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pug.PageAction_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = pug.UserGroup_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        WHERE pug.UserGroup_FK = 341 AND pa.Page_FK = 433
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

        -- 350 View both
        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')), v.RoleName
        FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_UserGroup] pug
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pug.PageAction_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = pug.UserGroup_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        CROSS JOIN (VALUES (N'Cng.FailureInfo.View'), (N'Cng.StationInfo.View'), (N'مشاهده کلی HTS')) v(RoleName)
        WHERE pug.UserGroup_FK = 350 AND pa.Page_FK IN (433, 434)
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM #UserRoleMap x WHERE x.Username = COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) AND x.RoleName = v.RoleName);

        -- 345 Manage + ShowAll Station
        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')), v.RoleName
        FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_UserGroup] pug
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pug.PageAction_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = pug.UserGroup_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        CROSS JOIN (VALUES (N'Cng.StationInfo.Manage'), (N'Cng.StationInfo.ShowAll')) v(RoleName)
        WHERE pug.UserGroup_FK = 345 AND pa.Page_FK = 434
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM #UserRoleMap x WHERE x.Username = COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) AND x.RoleName = v.RoleName);

        -- 340 Agency
        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')), N'Cng.StationInfo.Agency'
        FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_UserGroup] pug
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pug.PageAction_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = pug.UserGroup_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        WHERE pug.UserGroup_FK = 340 AND pa.Page_FK = 434
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM #UserRoleMap x WHERE x.Username = COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) AND x.RoleName = N'Cng.StationInfo.Agency');

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
    PRINT N'=== DONE Seed_CngBasicInfo_RolesAndAccess ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.Role WHERE Name LIKE N'Cng.%' ORDER BY Id;
SELECT r.Name, COUNT(*) AS Acc
FROM system.RoleAccess ra INNER JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.Path LIKE N'/panel/cng/%'
GROUP BY r.Name ORDER BY r.Name;
