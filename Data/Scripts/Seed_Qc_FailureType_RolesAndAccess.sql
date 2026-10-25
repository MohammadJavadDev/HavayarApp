/*
Seed_Qc_FailureType_RolesAndAccess.sql
دسترسی انواع خرابی کنترل کیفیت مطابق HTS صفحه 280:
  33 کارشناسان کنترل کیفیت → Qc.FailureType.Experts (کامل)
  140 کارشناسان سیستم کنترل کیفیت → Qc.FailureType.SystemExperts (کامل)
  350 مشاهده کلی HTS → نقش موجود — list/fetch/excel فقط
  ShowAllMenus → همه مسیرها
UTF-8 BOM. Idempotent. Requires [TMS] for user assign.
sqlcmd -S 172.20.40.42 -d HavayarApp -U sa -P <pwd> -C -b -I -f 65001 -i "Data\Scripts\Seed_Qc_FailureType_RolesAndAccess.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-qc-failuretype-roles';

    PRINT N'=== [1] Roles ===';
    IF OBJECT_ID('tempdb..#QcRoles') IS NOT NULL DROP TABLE #QcRoles;
    CREATE TABLE #QcRoles (WantId BIGINT NOT NULL, Name NVARCHAR(200) NOT NULL, Title NVARCHAR(200) NOT NULL);
    INSERT INTO #QcRoles (WantId, Name, Title) VALUES
        (600065, N'Qc.FailureType.Experts', N'کارشناسان کنترل کیفیت (HTS 33)'),
        (600066, N'Qc.FailureType.SystemExperts', N'کارشناسان سیستم کنترل کیفیت (HTS 140)');

    DECLARE @Rid BIGINT, @RName NVARCHAR(200), @RTitle NVARCHAR(200);
    DECLARE rc CURSOR LOCAL FAST_FORWARD FOR SELECT WantId, Name, Title FROM #QcRoles;
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
        (N'/panel/qc/failuretype/list', 1, 1),
        (N'/panel/qc/failuretype/fetchdata', 2, 2),
        (N'/panel/qc/failuretype/exporttoexcel', 2, 0)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'ShowAllMenus'), (N'مشاهده کلی HTS'),
        (N'Qc.FailureType.Experts'), (N'Qc.FailureType.SystemExperts')
    ) r(RoleName);

    INSERT INTO #Map SELECT r.RoleName, a.Path, a.AType, a.IType
    FROM (VALUES
        (N'/panel/qc/failuretype/new', 1, 4),
        (N'/panel/qc/failuretype/edit', 1, 5),
        (N'/panel/qc/failuretype/save', 2, 3),
        (N'/panel/qc/failuretype/add', 2, 4),
        (N'/panel/qc/failuretype/update', 2, 5),
        (N'/panel/qc/failuretype/delete', 2, 6)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES
        (N'ShowAllMenus'),
        (N'Qc.FailureType.Experts'), (N'Qc.FailureType.SystemExperts')
    ) r(RoleName);

    DELETE ra FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    WHERE ra.Path LIKE N'/panel/qc/failuretype/%'
      AND r.Name IN (
            N'Qc.FailureType.Experts', N'Qc.FailureType.SystemExperts',
            N'ShowAllMenus', N'مشاهده کلی HTS'
      );

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT m.Path, m.AType, m.IType,
        N'Entities.App.Qc.FailureType',
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
            (N'Qc.FailureType.Experts'), (N'Qc.FailureType.SystemExperts'),
            (N'مشاهده کلی HTS'), (N'ShowAllMenus')
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
        PRINT N'  Menu AccessRoleIds merged';
    END
    ELSE PRINT N'  WARN menu AllMenus not found';

    PRINT N'=== [4] Assign HTS groups 33/140 (if TMS linked) ===';
    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
    BEGIN
        IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
        CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL, RoleName NVARCHAR(200) NOT NULL, PRIMARY KEY (Username, RoleName));

        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')), N'Qc.FailureType.Experts'
        FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        WHERE m.UserGroup_FK = 33
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')), N'Qc.FailureType.SystemExperts'
        FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        WHERE m.UserGroup_FK = 140
          AND COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')) IS NOT NULL;

        DECLARE @Assigned INT = 0, @Missing INT = 0;
        DECLARE @MissingList NVARCHAR(MAX) = N'';
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
            BEGIN
                SET @Missing = @Missing + 1;
                SET @MissingList = @MissingList + N',' + @UName;
            END
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
        IF @Missing > 0 AND LEN(@MissingList) > 1
            PRINT N'  Missing usernames:' + @MissingList;
    END
    ELSE PRINT N'  WARN: Linked Server TMS missing — user assign skipped';

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_Qc_FailureType_RolesAndAccess ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.Role WHERE Name LIKE N'Qc.FailureType%' ORDER BY Id;
SELECT r.Name, ra.Path
FROM system.RoleAccess ra INNER JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.Path LIKE N'/panel/qc/failuretype%'
ORDER BY r.Name, ra.Path;
