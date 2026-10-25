/*
Seed_ProjectUtilizedMaterial_RolesAndAccess.sql
دسترسی اقلام مصرفی در پروژه مطابق HTS صفحه 408 Sale_ProjectUtilizedMaterial.
  گروه 281 کارشناسان اقلام مصرفی در پروژه → Manage (لیست، اکسل، ویرایش کالای جایگزین)
  گروه 350 مشاهده کلی HTS → View (لیست و اکسل)
  کاربر فعال HTS. اعطای مستقیم صفحه هم دیده می‌شود؛ عضویت 281 بر 350 مقدم است.
Sale.AfterSales و ShowAllMenus دست نمی‌خورند.
Encoding: UTF-8 with BOM
  sqlcmd -S 172.20.40.42 -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_ProjectUtilizedMaterial_RolesAndAccess.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-project-utilized-material-roles';
    DECLARE @EntityPascal NVARCHAR(200) = N'Entities.App.Sale.ProjectUtilizedMaterial';
    DECLARE @ChangePascal NVARCHAR(200) = N'Entities.App.Sale.ProjectUtilizedMaterialChange';

    PRINT N'=== [1] Roles 500038 / 500039 ===';
    IF OBJECT_ID('tempdb..#PumRoles') IS NOT NULL DROP TABLE #PumRoles;
    CREATE TABLE #PumRoles (WantId BIGINT NOT NULL, Name NVARCHAR(200) NOT NULL, Title NVARCHAR(200) NOT NULL);
    INSERT INTO #PumRoles (WantId, Name, Title) VALUES
        (500038, N'Sale.ProjectUtilizedMaterial.Manage', N'کارشناسان اقلام مصرفی در پروژه (HTS 281)'),
        (500039, N'Sale.ProjectUtilizedMaterial.View', N'اقلام مصرفی در پروژه - مشاهده (HTS 350)');

    DECLARE @Rid BIGINT, @RName NVARCHAR(200), @RTitle NVARCHAR(200);
    DECLARE rc CURSOR LOCAL FAST_FORWARD FOR SELECT WantId, Name, Title FROM #PumRoles;
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

    PRINT N'=== [2] RoleAccess ===';
    IF OBJECT_ID('tempdb..#PumMap') IS NOT NULL DROP TABLE #PumMap;
    CREATE TABLE #PumMap
    (
        RoleName NVARCHAR(200) NOT NULL,
        Path NVARCHAR(300) NOT NULL,
        AType INT NOT NULL,
        IType INT NOT NULL,
        EntityName NVARCHAR(200) NOT NULL
    );

    INSERT INTO #PumMap (RoleName, Path, AType, IType, EntityName)
    SELECT r.RoleName, a.Path, a.AType, a.IType, @EntityPascal
    FROM (VALUES
        (N'/panel/sale/projectutilizedmaterial/list', 1, 1),
        (N'/panel/sale/projectutilizedmaterial/fetchdata', 2, 2),
        (N'/panel/sale/projectutilizedmaterial/exporttoexcel', 2, 0)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'Sale.ProjectUtilizedMaterial.Manage'),
        (N'Sale.ProjectUtilizedMaterial.View')
    ) r(RoleName);

    INSERT INTO #PumMap (RoleName, Path, AType, IType, EntityName)
    SELECT r.RoleName, a.Path, a.AType, a.IType, @EntityPascal
    FROM (VALUES
        (N'/panel/sale/projectutilizedmaterial/edit', 1, 5),
        (N'/panel/sale/projectutilizedmaterial/save', 2, 3),
        (N'/panel/sale/projectutilizedmaterial/update', 2, 5)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES (N'Sale.ProjectUtilizedMaterial.Manage')) r(RoleName);

    INSERT INTO #PumMap (RoleName, Path, AType, IType, EntityName)
    SELECT r.RoleName, a.Path, a.AType, a.IType, @ChangePascal
    FROM (VALUES
        (N'/panel/sale/projectutilizedmaterialchange/listbyparentid', 1, 1),
        (N'/panel/sale/projectutilizedmaterialchange/getlistbyparentid', 2, 2)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'Sale.ProjectUtilizedMaterial.Manage'),
        (N'Sale.ProjectUtilizedMaterial.View')
    ) r(RoleName);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT m.Path, m.AType, m.IType, m.EntityName, NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #PumMap m
    INNER JOIN system.Role r ON r.Name = m.RoleName
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = r.Id AND ra.Path = m.Path AND ra.ActionAccessType = m.AType);
    PRINT N'  Controller RoleAccess inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    DECLARE @ProfileId BIGINT = (
        SELECT TOP 1 Id FROM system.SavedQuery WHERE Name = N'AfterSales_ProjectUtilizedMaterial_List');
    IF @ProfileId IS NOT NULL
    BEGIN
        INSERT INTO system.RoleAccess
            (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        SELECT
            N'dataProfile_' + CAST(@ProfileId AS nvarchar(20)),
            3, 7, @EntityPascal, N'اقلام مصرفی در پروژه', NULL, @ProfileId, r.Id,
            1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
        FROM system.Role r
        WHERE r.Name IN (N'Sale.ProjectUtilizedMaterial.Manage', N'Sale.ProjectUtilizedMaterial.View')
          AND NOT EXISTS (
              SELECT 1 FROM system.RoleAccess ra
              WHERE ra.RoleId = r.Id AND ra.ActionAccessType = 3 AND ra.RowId = @ProfileId);
        PRINT N'  Profile RoleAccess inserted: ' + CAST(@@ROWCOUNT AS nvarchar(20));
    END
    ELSE PRINT N'  WARN profile AfterSales_ProjectUtilizedMaterial_List missing';

    PRINT N'=== [3] Menu AccessRoleIds ===';
    DECLARE @MenuName NVARCHAR(200);
    DECLARE mc CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name FROM (VALUES (N'AfterSalesServiceSystem'), (N'AllMenus')) v(Name);
    OPEN mc;
    FETCH NEXT FROM mc INTO @MenuName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @MenuId BIGINT = (SELECT TOP 1 Id FROM system.SystemMenu WHERE Name = @MenuName);
        IF @MenuId IS NULL
        BEGIN
            PRINT N'  WARN menu missing: ' + @MenuName;
            FETCH NEXT FROM mc INTO @MenuName;
            CONTINUE;
        END

        IF OBJECT_ID('tempdb..#MenuRoles') IS NOT NULL DROP TABLE #MenuRoles;
        CREATE TABLE #MenuRoles (RoleName NVARCHAR(200) NOT NULL PRIMARY KEY);
        INSERT INTO #MenuRoles (RoleName)
        SELECT DISTINCT LTRIM(RTRIM(j.value))
        FROM system.SystemMenu m
        CROSS APPLY OPENJSON(ISNULL(m.AccessRoles, N'[]')) j
        WHERE m.Id = @MenuId AND LTRIM(RTRIM(j.value)) <> N'';

        INSERT INTO #MenuRoles (RoleName)
        SELECT v.RoleName
        FROM (VALUES
            (N'Sale.ProjectUtilizedMaterial.Manage'),
            (N'Sale.ProjectUtilizedMaterial.View')
        ) v(RoleName)
        WHERE NOT EXISTS (SELECT 1 FROM #MenuRoles x WHERE x.RoleName = v.RoleName);

        DECLARE @AccessRoles NVARCHAR(MAX);
        DECLARE @AccessRoleIds NVARCHAR(MAX);
        SELECT @AccessRoles = N'[' + STUFF((
            SELECT N',"' + STRING_ESCAPE(RoleName, 'json') + N'"' FROM #MenuRoles ORDER BY RoleName
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
        PRINT N'  Menu merged: ' + @MenuName;
        FETCH NEXT FROM mc INTO @MenuName;
    END
    CLOSE mc;
    DEALLOCATE mc;

    PRINT N'=== [4] Assign active HTS page 408 users ===';
    IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
    CREATE TABLE #UserRoleMap
    (
        Username NVARCHAR(200) NOT NULL,
        RoleName NVARCHAR(200) NOT NULL,
        Pri INT NOT NULL
    );

    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
    BEGIN
        INSERT INTO #UserRoleMap (Username, RoleName, Pri)
        SELECT DISTINCT
            COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')),
            N'Sale.ProjectUtilizedMaterial.Manage',
            1
        FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        WHERE m.UserGroup_FK = 281
          AND u.IsActive = 1
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

        INSERT INTO #UserRoleMap (Username, RoleName, Pri)
        SELECT DISTINCT
            COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')),
            N'Sale.ProjectUtilizedMaterial.View',
            2
        FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        WHERE m.UserGroup_FK = 350
          AND u.IsActive = 1
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

        INSERT INTO #UserRoleMap (Username, RoleName, Pri)
        SELECT DISTINCT
            COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')),
            CASE WHEN pa.Permission_FK IN (2, 5) THEN N'Sale.ProjectUtilizedMaterial.Manage'
                 ELSE N'Sale.ProjectUtilizedMaterial.View' END,
            CASE WHEN pa.Permission_FK IN (2, 5) THEN 1 ELSE 2 END
        FROM [TMS].[TotalSystem].[dbo].[Gnr_PageAction_User] pau
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_PageAction] pa ON pa.PageAction_ID = pau.PageAction_FK
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = pau.User_FK
        WHERE pa.Page_FK = 408
          AND u.IsActive = 1
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

        IF OBJECT_ID('tempdb..#Pick') IS NOT NULL DROP TABLE #Pick;
        CREATE TABLE #Pick (Username NVARCHAR(200) NOT NULL PRIMARY KEY, RoleName NVARCHAR(200) NOT NULL);
        INSERT INTO #Pick (Username, RoleName)
        SELECT c.Username,
            CASE MIN(c.Pri) WHEN 1 THEN N'Sale.ProjectUtilizedMaterial.Manage' ELSE N'Sale.ProjectUtilizedMaterial.View' END
        FROM #UserRoleMap c
        GROUP BY c.Username;

        DECLARE @Assigned INT = 0, @Missing INT = 0, @SkippedAlready INT = 0;
        DECLARE @UName NVARCHAR(200), @RoleN NVARCHAR(200), @LookupName NVARCHAR(200);
        DECLARE @UserId BIGINT, @RoleId BIGINT;

        DECLARE uc CURSOR LOCAL FAST_FORWARD FOR SELECT Username, RoleName FROM #Pick;
        OPEN uc; FETCH NEXT FROM uc INTO @UName, @RoleN;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @LookupName = CASE WHEN LOWER(@UName) = N'allahverdi.s' THEN N'allahvirdi.s' ELSE @UName END;
            SET @UserId = NULL;
            SET @RoleId = NULL;
            SELECT TOP 1 @UserId = u.Id
            FROM system.[User] u
            WHERE LOWER(u.Username) = LOWER(@LookupName)
               OR LOWER(REPLACE(ISNULL(u.Email, N''), N'@havayar.com', N'')) = LOWER(@LookupName)
            ORDER BY CASE WHEN LOWER(u.Username) = LOWER(@LookupName) THEN 0 ELSE 1 END;
            SELECT TOP 1 @RoleId = r.Id FROM system.Role r WHERE r.Name = @RoleN;

            IF @UserId IS NULL OR @RoleId IS NULL
            BEGIN
                SET @Missing += 1;
                PRINT N'  MISSING USER: ' + @UName + N' -> ' + @RoleN;
            END
            ELSE IF EXISTS (
                SELECT 1 FROM system.[User] u
                CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
                WHERE u.Id = @UserId AND TRY_CAST(j.value AS bigint) = @RoleId)
                SET @SkippedAlready += 1;
            ELSE
            BEGIN
                UPDATE system.[User]
                SET
                    RoleIds = CASE
                        WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) IN (N'', N'[]')
                            THEN N'[' + CAST(@RoleId AS nvarchar(20)) + N']'
                        ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@RoleId AS nvarchar(20)) + N']')
                    END,
                    Roles = CASE
                        WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @RoleN) THEN Roles
                        WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) IN (N'', N'[]')
                            THEN N'["' + @RoleN + N'"]'
                        ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @RoleN + N'"]')
                    END,
                    ModifiedById = 1,
                    ModifiedByName = @SeedUser,
                    ModifiedDateMiladiDateTime = @Now,
                    ModifiedDateShamsiDateTime = @NowShamsi
                WHERE Id = @UserId;
                SET @Assigned += 1;
            END
            FETCH NEXT FROM uc INTO @UName, @RoleN;
        END
        CLOSE uc; DEALLOCATE uc;
        PRINT N'  Assigned: ' + CAST(@Assigned AS nvarchar(20))
            + N' already: ' + CAST(@SkippedAlready AS nvarchar(20))
            + N' missing: ' + CAST(@Missing AS nvarchar(20));
    END
    ELSE PRINT N'  WARN TMS missing — users not assigned';

    DECLARE @ViewRoleId BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Sale.ProjectUtilizedMaterial.View');
    DECLARE @ManageRoleId BIGINT = (SELECT Id FROM system.Role WHERE Name = N'Sale.ProjectUtilizedMaterial.Manage');
    DECLARE @ViewStripped INT = 0;
    IF @ViewRoleId IS NOT NULL AND @ManageRoleId IS NOT NULL
    BEGIN
        DECLARE @StripUserId BIGINT, @StripRoleIds NVARCHAR(MAX), @StripRoles NVARCHAR(MAX);
        DECLARE strip_cur CURSOR LOCAL FAST_FORWARD FOR
            SELECT u.Id, u.RoleIds, u.Roles
            FROM system.[User] u
            WHERE EXISTS (SELECT 1 FROM OPENJSON(ISNULL(u.RoleIds, N'[]')) j WHERE TRY_CAST(j.value AS bigint) = @ViewRoleId)
              AND EXISTS (SELECT 1 FROM OPENJSON(ISNULL(u.RoleIds, N'[]')) j WHERE TRY_CAST(j.value AS bigint) = @ManageRoleId);
        OPEN strip_cur;
        FETCH NEXT FROM strip_cur INTO @StripUserId, @StripRoleIds, @StripRoles;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SELECT @StripRoleIds = N'[' + ISNULL(STUFF((
                SELECT N',' + j.value
                FROM OPENJSON(ISNULL(@StripRoleIds, N'[]')) j
                WHERE TRY_CAST(j.value AS bigint) <> @ViewRoleId
                FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N''), N'') + N']';
            SELECT @StripRoles = N'[' + ISNULL(STUFF((
                SELECT N',"' + REPLACE(j.value, N'"', N'\"') + N'"'
                FROM OPENJSON(ISNULL(@StripRoles, N'[]')) j
                WHERE j.value <> N'Sale.ProjectUtilizedMaterial.View'
                FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N''), N'') + N']';
            UPDATE system.[User]
            SET RoleIds = @StripRoleIds, Roles = @StripRoles,
                ModifiedById = 1, ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @StripUserId;
            SET @ViewStripped += 1;
            FETCH NEXT FROM strip_cur INTO @StripUserId, @StripRoleIds, @StripRoles;
        END
        CLOSE strip_cur;
        DEALLOCATE strip_cur;
    END
    PRINT N'  Stripped View from Manage users: ' + CAST(@ViewStripped AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_ProjectUtilizedMaterial_RolesAndAccess ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT r.Id, r.Title,
       (SELECT COUNT(*) FROM system.RoleAccess ra WHERE ra.RoleId = r.Id AND ra.Path LIKE N'/panel/sale/projectutilizedmaterial%') AS Paths,
       (SELECT COUNT(*) FROM system.[User] u CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j WHERE TRY_CAST(j.value AS bigint) = r.Id) AS Users
FROM system.Role r
WHERE r.Name IN (N'Sale.ProjectUtilizedMaterial.Manage', N'Sale.ProjectUtilizedMaterial.View');

SELECT u.Username, r.Name AS RoleName
FROM system.[User] u
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
INNER JOIN system.Role r ON r.Id = TRY_CAST(j.value AS bigint)
WHERE r.Name IN (N'Sale.ProjectUtilizedMaterial.Manage', N'Sale.ProjectUtilizedMaterial.View')
ORDER BY r.Name, u.Username;
