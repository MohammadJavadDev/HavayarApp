/*
Seed_SrvPmRequest_RolesAndAccess.sql
نقش و دسترسی درخواست PM (HTS صفحه 399). UTF-8 BOM. Idempotent.
Role ids (after Ambassador 600043-600047):
  600048 Srv.PmRequest.Support
  600049 Srv.PmRequest.RequesterConfirm
  600050 Srv.PmRequest.HavayarControl
  600051 Srv.PmRequest.Requester
  600052 Srv.PmRequest.View
عضویت انحصاری از TotalSystem (TMS) صفحه 399:
  1) گروه 258 یا 269 → Support
  2) else گروه 413 و (257 یا 408) → RequesterConfirm
  3) else گروه 413 → HavayarControl
  4) else گروه 257 یا 408 → Requester
  5) else گروه 350 → View
Username: COALESCE(ActiveDirectoryUsername, Username)؛ alias allahverdi.s → allahvirdi.s
sqlcmd -S PortalSRV\PORTAL -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_SrvPmRequest_RolesAndAccess.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-srv-pmrequest-roles';

    PRINT N'=== [1] Roles ===';
    IF OBJECT_ID('tempdb..#PmRoles') IS NOT NULL DROP TABLE #PmRoles;
    CREATE TABLE #PmRoles (WantId BIGINT NOT NULL, Name NVARCHAR(200) NOT NULL, Title NVARCHAR(200) NOT NULL);
    INSERT INTO #PmRoles (WantId, Name, Title) VALUES
        (600048, N'Srv.PmRequest.Support', N'درخواست PM — پشتیبانی'),
        (600049, N'Srv.PmRequest.RequesterConfirm', N'درخواست PM — درخواست‌کننده و تایید'),
        (600050, N'Srv.PmRequest.HavayarControl', N'درخواست PM — هوایار کنترل'),
        (600051, N'Srv.PmRequest.Requester', N'درخواست PM — درخواست‌کننده'),
        (600052, N'Srv.PmRequest.View', N'درخواست PM — مشاهده');

    DECLARE @Rid BIGINT, @RName NVARCHAR(200), @RTitle NVARCHAR(200);
    DECLARE rc CURSOR LOCAL FAST_FORWARD FOR SELECT WantId, Name, Title FROM #PmRoles;
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

    INSERT INTO #Map SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/srv/pmrequest/list', 1, 1),
        (N'/panel/srv/pmrequest/fetchdata', 2, 2),
        (N'/panel/srv/pmrequest/exporttoexcel', 2, 0)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'ShowAllMenus'),
        (N'Srv.PmRequest.Support'), (N'Srv.PmRequest.RequesterConfirm'),
        (N'Srv.PmRequest.HavayarControl'), (N'Srv.PmRequest.Requester'), (N'Srv.PmRequest.View')
    ) r(RoleName);

    INSERT INTO #Map SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/srv/pmrequest/new', 1, 4),
        (N'/panel/srv/pmrequest/edit', 1, 5),
        (N'/panel/srv/pmrequest/save', 2, 3),
        (N'/panel/srv/pmrequest/add', 2, 4),
        (N'/panel/srv/pmrequest/update', 2, 5),
        (N'/panel/srv/pmrequest/delete', 2, 6),
        (N'/panel/srv/pmrequest/attachments', 1, 19),
        (N'/panel/srv/pmrequest/saveattachment', 2, 4),
        (N'/panel/srv/pmrequest/deleteattachment', 2, 6),
        (N'/panel/srv/pmrequest/viewattachment', 1, 3)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'ShowAllMenus'),
        (N'Srv.PmRequest.Support'), (N'Srv.PmRequest.RequesterConfirm'),
        (N'Srv.PmRequest.Requester')
    ) r(RoleName);

    INSERT INTO #Map SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/srv/pmrequest/confirm', 2, 1000)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'ShowAllMenus'),
        (N'Srv.PmRequest.Support'), (N'Srv.PmRequest.RequesterConfirm'),
        (N'Srv.PmRequest.HavayarControl')
    ) r(RoleName);

    DELETE ra FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    WHERE ra.Path LIKE N'/panel/srv/pmrequest/%'
      AND r.Name IN (
            N'Srv.PmRequest.Support', N'Srv.PmRequest.RequesterConfirm',
            N'Srv.PmRequest.HavayarControl', N'Srv.PmRequest.Requester', N'Srv.PmRequest.View',
            N'ShowAllMenus'
      );

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT m.Path, m.AType, m.IType,
        N'Entities.App.Srv.PmRequest',
        NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #Map m
    INNER JOIN system.Role r ON r.Name = m.RoleName;
    PRINT N'  Controller RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== [3] Menu AccessRoleIds merge (AllMenus) ===';
    DECLARE @MenuId BIGINT = (SELECT TOP 1 Id FROM system.SystemMenu WHERE Name = N'AllMenus');
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
            (N'Srv.PmRequest.Support'), (N'Srv.PmRequest.RequesterConfirm'),
            (N'Srv.PmRequest.HavayarControl'), (N'Srv.PmRequest.Requester'), (N'Srv.PmRequest.View'),
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
    ELSE PRINT N'  WARN menu AllMenus not found';

    PRINT N'=== [4] Assign HTS page 399 users (if TMS linked) ===';
    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
    BEGIN
        IF OBJECT_ID('tempdb..#Cand') IS NOT NULL DROP TABLE #Cand;
        CREATE TABLE #Cand (Username NVARCHAR(200) NOT NULL, Pri INT NOT NULL, RoleName NVARCHAR(200) NOT NULL);

        INSERT INTO #Cand (Username, Pri, RoleName)
        SELECT DISTINCT
            COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')),
            1, N'Srv.PmRequest.Support'
        FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        WHERE m.UserGroup_FK IN (258, 269)
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

        INSERT INTO #Cand (Username, Pri, RoleName)
        SELECT DISTINCT
            COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')),
            2, N'Srv.PmRequest.RequesterConfirm'
        FROM [TMS].[TotalSystem].[dbo].[Gnr_User] u
        WHERE EXISTS (
                SELECT 1 FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
                WHERE m.User_FK = u.User_ID AND m.UserGroup_FK = 413)
          AND EXISTS (
                SELECT 1 FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
                WHERE m.User_FK = u.User_ID AND m.UserGroup_FK IN (257, 408))
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

        INSERT INTO #Cand (Username, Pri, RoleName)
        SELECT DISTINCT
            COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')),
            3, N'Srv.PmRequest.HavayarControl'
        FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        WHERE m.UserGroup_FK = 413
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

        INSERT INTO #Cand (Username, Pri, RoleName)
        SELECT DISTINCT
            COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')),
            4, N'Srv.PmRequest.Requester'
        FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        WHERE m.UserGroup_FK IN (257, 408)
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

        INSERT INTO #Cand (Username, Pri, RoleName)
        SELECT DISTINCT
            COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')),
            5, N'Srv.PmRequest.View'
        FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        WHERE m.UserGroup_FK = 350
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

        IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
        CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL PRIMARY KEY, RoleName NVARCHAR(200) NOT NULL);
        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT c.Username,
            CASE MIN(c.Pri)
                WHEN 1 THEN N'Srv.PmRequest.Support'
                WHEN 2 THEN N'Srv.PmRequest.RequesterConfirm'
                WHEN 3 THEN N'Srv.PmRequest.HavayarControl'
                WHEN 4 THEN N'Srv.PmRequest.Requester'
                ELSE N'Srv.PmRequest.View'
            END
        FROM #Cand c
        GROUP BY c.Username;

        DECLARE @Assigned INT = 0, @Missing INT = 0;
        DECLARE @MissingList NVARCHAR(MAX) = N'';
        DECLARE @UName NVARCHAR(200), @RoleN NVARCHAR(200), @LookupName NVARCHAR(200);
        DECLARE @UserId BIGINT, @RoleId BIGINT;
        DECLARE @RolesJson NVARCHAR(MAX), @RoleIdsJson NVARCHAR(MAX);
        DECLARE @PmRoleNames TABLE (Name NVARCHAR(200) NOT NULL);
        INSERT INTO @PmRoleNames (Name)
        SELECT Name FROM #PmRoles;

        DECLARE uc CURSOR LOCAL FAST_FORWARD FOR SELECT Username, RoleName FROM #UserRoleMap;
        OPEN uc; FETCH NEXT FROM uc INTO @UName, @RoleN;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @LookupName = CASE WHEN LOWER(@UName) = N'allahverdi.s' THEN N'allahvirdi.s' ELSE @UName END;
            SELECT TOP 1 @UserId = u.Id FROM system.[User] u
            WHERE LOWER(u.Username) = LOWER(@LookupName)
               OR LOWER(REPLACE(ISNULL(u.Email, N''), N'@havayar.com', N'')) = LOWER(@LookupName);
            SELECT TOP 1 @RoleId = r.Id FROM system.Role r WHERE r.Name = @RoleN;
            IF @UserId IS NULL OR @RoleId IS NULL
            BEGIN
                SET @Missing = @Missing + 1;
                SET @MissingList = @MissingList + N',' + @UName;
            END
            ELSE
            BEGIN
                SELECT @RolesJson = ISNULL(Roles, N'[]'), @RoleIdsJson = ISNULL(RoleIds, N'[]') FROM system.[User] WHERE Id = @UserId;
                ;WITH keepR AS (
                    SELECT [value] AS RoleName
                    FROM OPENJSON(@RolesJson)
                    WHERE LOWER([value]) NOT IN (SELECT LOWER(Name) FROM @PmRoleNames)
                       OR LOWER([value]) = LOWER(@RoleN)
                )
                SELECT @RolesJson = N'[' + ISNULL(STUFF((
                    SELECT N',"' + STRING_ESCAPE(RoleName, 'json') + N'"' FROM keepR
                    FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N''), N'') + N']';
                ;WITH keepI AS (
                    SELECT TRY_CAST([value] AS BIGINT) AS RoleId
                    FROM OPENJSON(@RoleIdsJson)
                    WHERE TRY_CAST([value] AS BIGINT) NOT IN (SELECT Id FROM system.Role WHERE Name IN (SELECT Name FROM @PmRoleNames))
                       OR TRY_CAST([value] AS BIGINT) = @RoleId
                )
                SELECT @RoleIdsJson = N'[' + ISNULL(STUFF((
                    SELECT N',' + CAST(RoleId AS nvarchar(20)) FROM keepI WHERE RoleId IS NOT NULL
                    FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N''), N'') + N']';
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
        IF @Missing > 0 AND LEN(@MissingList) > 1
            PRINT N'  Missing usernames:' + @MissingList;
    END
    ELSE PRINT N'  WARN: Linked Server TMS missing — user assign skipped';

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_SrvPmRequest_RolesAndAccess ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.Role WHERE Name LIKE N'Srv.PmRequest%' ORDER BY Id;
SELECT r.Name, COUNT(*) AS Acc
FROM system.RoleAccess ra INNER JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.Path LIKE N'/panel/srv/pmrequest%'
GROUP BY r.Name ORDER BY r.Name;
