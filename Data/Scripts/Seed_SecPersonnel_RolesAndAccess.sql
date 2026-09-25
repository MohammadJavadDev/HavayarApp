/*
Seed_SecPersonnel_RolesAndAccess.sql
دسترسی مدیریت پرسنل دبیرخانه (HTS صفحه 281).
UTF-8 BOM. Idempotent.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-sec-personnel-roles';

    PRINT N'=== [1] Roles ===';
    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sec.Personnel.Manage')
        INSERT INTO system.Role (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (N'Sec.Personnel.Manage', N'پرسنل دبیرخانه - مدیریت', 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);

    IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = N'Sec.Personnel.View')
        INSERT INTO system.Role (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
            CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (N'Sec.Personnel.View', N'پرسنل دبیرخانه - مشاهده', 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);

    PRINT N'=== [2] Controller RoleAccess ===';
    IF OBJECT_ID('tempdb..#Map') IS NOT NULL DROP TABLE #Map;
    CREATE TABLE #Map (RoleName NVARCHAR(200) NOT NULL, Path NVARCHAR(300) NOT NULL, AType INT NOT NULL, IType INT NOT NULL);

    INSERT INTO #Map SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/sec/personnel/list', 1, 1),
        (N'/panel/sec/personnel/fetchdata', 2, 2),
        (N'/panel/sec/personnel/edit', 1, 5),
        (N'/panel/sec/personnel/exporttoexcel', 2, 0)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES (N'ShowAllMenus'), (N'مشاهده کلی HTS'), (N'Sec.Personnel.View'), (N'Sec.Personnel.Manage')) r(RoleName);

    INSERT INTO #Map SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/sec/personnel/new', 1, 4),
        (N'/panel/sec/personnel/save', 2, 3),
        (N'/panel/sec/personnel/add', 2, 4),
        (N'/panel/sec/personnel/update', 2, 5),
        (N'/panel/sec/personnel/delete', 2, 6)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES (N'ShowAllMenus'), (N'Sec.Personnel.Manage')) r(RoleName);

    DECLARE @RoleName NVARCHAR(200), @Path NVARCHAR(300), @AType INT, @IType INT, @RoleId BIGINT;
    DECLARE mc CURSOR LOCAL FAST_FORWARD FOR SELECT RoleName, Path, AType, IType FROM #Map;
    OPEN mc; FETCH NEXT FROM mc INTO @RoleName, @Path, @AType, @IType;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @RoleId = Id FROM system.Role WHERE Name = @RoleName;
        IF @RoleId IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM system.RoleAccess WHERE RoleId = @RoleId AND Path = @Path AND ActionAccessType = @AType)
        BEGIN
            INSERT INTO system.RoleAccess
                (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
                 CreatedById, ModifiedById, CreatedByName, ModifiedByName,
                 CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (@Path, @AType, @IType, N'Entities.App.Sec.Personnel', N'پرسنل دبیرخانه', NULL, NULL, @RoleId,
                1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        END
        FETCH NEXT FROM mc INTO @RoleName, @Path, @AType, @IType;
    END
    CLOSE mc; DEALLOCATE mc;

    PRINT N'=== [3] Menu AccessRoleIds merge (AllMenus) ===';
    DECLARE @MenuId BIGINT = (SELECT TOP 1 Id FROM system.SystemMenu WHERE Name = N'AllMenus' ORDER BY Id);
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
            (N'Sec.Personnel.Manage'), (N'Sec.Personnel.View')
        ) v(RoleName)
        WHERE NOT EXISTS (SELECT 1 FROM #MenuRoles x WHERE x.RoleName = v.RoleName)
          AND EXISTS (SELECT 1 FROM system.Role r WHERE r.Name = v.RoleName);

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
    ELSE PRINT N'  WARN menu AllMenus not found';

    PRINT N'=== [4] Assign HTS page 281 users ===';
    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
    BEGIN
        IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
        CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL, RoleName NVARCHAR(200) NOT NULL, PRIMARY KEY (Username, RoleName));

        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT DISTINCT
            COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')),
            CASE WHEN pa.Permission_FK = 2 THEN N'Sec.Personnel.Manage' ELSE N'Sec.Personnel.View' END
        FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_User] pau
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pau.PageAction_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = pau.User_FK
        WHERE pa.Page_FK = 281 AND pa.Permission_FK IN (2, 3)
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

        DECLARE @Assigned INT = 0, @Missing INT = 0;
        DECLARE @UName NVARCHAR(200), @RoleN NVARCHAR(200), @UserId BIGINT, @AssignRoleId BIGINT;
        DECLARE @RolesJson NVARCHAR(MAX), @RoleIdsJson NVARCHAR(MAX);

        DECLARE uc CURSOR LOCAL FAST_FORWARD FOR SELECT Username, RoleName FROM #UserRoleMap;
        OPEN uc; FETCH NEXT FROM uc INTO @UName, @RoleN;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SELECT TOP 1 @UserId = u.Id FROM system.[User] u
            WHERE LOWER(u.Username) = LOWER(@UName)
               OR LOWER(REPLACE(ISNULL(u.Email, N''), N'@havayar.com', N'')) = LOWER(@UName);
            SELECT TOP 1 @AssignRoleId = r.Id FROM system.Role r WHERE r.Name = @RoleN;
            IF @UserId IS NULL OR @AssignRoleId IS NULL
                SET @Missing = @Missing + 1;
            ELSE
            BEGIN
                SELECT @RolesJson = ISNULL(Roles, N'[]'), @RoleIdsJson = ISNULL(RoleIds, N'[]') FROM system.[User] WHERE Id = @UserId;
                IF NOT EXISTS (SELECT 1 FROM OPENJSON(@RolesJson) WHERE LOWER([value]) = LOWER(@RoleN))
                    SET @RolesJson = JSON_MODIFY(@RolesJson, N'append $', @RoleN);
                IF NOT EXISTS (SELECT 1 FROM OPENJSON(@RoleIdsJson) WHERE TRY_CAST([value] AS BIGINT) = @AssignRoleId)
                    SET @RoleIdsJson = JSON_MODIFY(@RoleIdsJson, N'append $', @AssignRoleId);
                UPDATE system.[User] SET Roles = @RolesJson, RoleIds = @RoleIdsJson,
                    ModifiedById = 1, ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
                WHERE Id = @UserId;
                SET @Assigned = @Assigned + 1;
            END
            SET @UserId = NULL; SET @AssignRoleId = NULL;
            FETCH NEXT FROM uc INTO @UName, @RoleN;
        END
        CLOSE uc; DEALLOCATE uc;
        PRINT N'  User-role assigns touched: ' + CAST(@Assigned AS nvarchar(20)) + N', missing user/role: ' + CAST(@Missing AS nvarchar(20));
    END
    ELSE PRINT N'  WARN: Linked Server TMS missing — user assign skipped';

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_SecPersonnel_RolesAndAccess ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH
