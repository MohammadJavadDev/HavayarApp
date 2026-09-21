/*
================================================================================
Seed_AllMenus_Role_And_Users.sql
================================================================================
نقش «همه منوها» (system.Role Name=AllMenus, Id=400011):
  - به AccessRoles / AccessRoleIds همه system.SystemMenu اضافه می‌شود
  - به Roles / RoleIds همه کاربران فعال و غیرفعال (غیرحذف‌شده) اضافه می‌شود

این نقش فقط سوییچر منو را باز می‌کند. RoleAccess صفحات کپی نمی‌شود
تا همه کاربران دسترسی نوشتن/حذف همه صفحات را نگیرند.
برگ‌های سایدبار همچنان با RoleAccess فعلی هر کاربر فیلتر می‌شوند.

Idempotent. Encoding: UTF-8 with BOM.
بعد از اجرا: ری‌استارت WebApp (کش منو) و ورود مجدد کاربران (RoleIds).
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-allmenus-role';
DECLARE @RoleId BIGINT = 400011;
DECLARE @RoleName NVARCHAR(150) = N'AllMenus';
DECLARE @RoleTitle NVARCHAR(150) = N'همه منوها';

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT N'=== [1] Role AllMenus ===';
    IF EXISTS (SELECT 1 FROM system.Role WHERE Name = @RoleName)
    BEGIN
        SELECT @RoleId = Id FROM system.Role WHERE Name = @RoleName;
        UPDATE system.Role
        SET Title = @RoleTitle,
            IsActive = 1,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = @RoleName;
        PRINT N'  EXISTS Id=' + CAST(@RoleId AS nvarchar(20));
    END
    ELSE IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @RoleId)
    BEGIN
        INSERT INTO system.Role
            (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
             CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime,
             CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@RoleName, @RoleTitle, 1, @SeedUser, 1, @SeedUser,
             @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SELECT @RoleId = Id FROM system.Role WHERE Name = @RoleName;
        PRINT N'  CREATED (new id) Id=' + CAST(@RoleId AS nvarchar(20));
    END
    ELSE
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role
            (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
             CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime,
             CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@RoleId, @RoleName, @RoleTitle, 1, @SeedUser, 1, @SeedUser,
             @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
        PRINT N'  CREATED Id=400011';
    END

    PRINT N'=== [2] Add role to every SystemMenu ===';
    DECLARE @MenuUpdated INT = 0;
    DECLARE @MenuId BIGINT, @AccessRoles NVARCHAR(MAX), @AccessRoleIds NVARCHAR(MAX);

    DECLARE menu_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Id, ISNULL(AccessRoles, N'[]'), ISNULL(AccessRoleIds, N'[]')
        FROM system.SystemMenu;
    OPEN menu_cur;
    FETCH NEXT FROM menu_cur INTO @MenuId, @AccessRoles, @AccessRoleIds;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @AccessRoles IS NULL OR LTRIM(RTRIM(@AccessRoles)) = N'' OR ISJSON(@AccessRoles) = 0
            SET @AccessRoles = N'[]';
        IF @AccessRoleIds IS NULL OR LTRIM(RTRIM(@AccessRoleIds)) = N'' OR ISJSON(@AccessRoleIds) = 0
            SET @AccessRoleIds = N'[]';

        IF NOT EXISTS (
            SELECT 1 FROM OPENJSON(@AccessRoles) j WHERE j.value = @RoleName
        )
            SET @AccessRoles = CASE
                WHEN @AccessRoles = N'[]' THEN N'["' + @RoleName + N'"]'
                ELSE STUFF(@AccessRoles, LEN(@AccessRoles), 1, N',"' + @RoleName + N'"]')
            END;

        IF NOT EXISTS (
            SELECT 1 FROM OPENJSON(@AccessRoleIds) j WHERE TRY_CAST(j.value AS bigint) = @RoleId
        )
            SET @AccessRoleIds = CASE
                WHEN @AccessRoleIds = N'[]' THEN N'[' + CAST(@RoleId AS nvarchar(20)) + N']'
                ELSE STUFF(@AccessRoleIds, LEN(@AccessRoleIds), 1, N',' + CAST(@RoleId AS nvarchar(20)) + N']')
            END;

        UPDATE system.SystemMenu
        SET AccessRoles = @AccessRoles,
            AccessRoleIds = @AccessRoleIds,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId
          AND (ISNULL(AccessRoles, N'') <> @AccessRoles OR ISNULL(AccessRoleIds, N'') <> @AccessRoleIds);

        SET @MenuUpdated += @@ROWCOUNT;
        FETCH NEXT FROM menu_cur INTO @MenuId, @AccessRoles, @AccessRoleIds;
    END
    CLOSE menu_cur;
    DEALLOCATE menu_cur;
    PRINT N'  Menus updated: ' + CAST(@MenuUpdated AS nvarchar(20));

    PRINT N'=== [3] Add role to every non-deleted user ===';
    UPDATE u
    SET
        RoleIds = CASE
            WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]' OR ISJSON(RoleIds) = 0
                THEN N'[' + CAST(@RoleId AS nvarchar(20)) + N']'
            ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@RoleId AS nvarchar(20)) + N']')
        END,
        Roles = CASE
            WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]' OR ISJSON(Roles) = 0
                THEN N'["' + @RoleName + N'"]'
            ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @RoleName + N'"]')
        END,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    FROM system.[User] u
    WHERE ISNULL(u.IsActive, 1) <> 2
      AND NOT EXISTS (
          SELECT 1
          FROM OPENJSON(CASE WHEN ISJSON(ISNULL(u.RoleIds, N'[]')) = 1 THEN u.RoleIds ELSE N'[]' END) j
          WHERE TRY_CAST(j.value AS bigint) = @RoleId
      );
    PRINT N'  Users assigned: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;

    PRINT N'=== DONE Seed_AllMenus_Role_And_Users ===';
    SELECT r.Id, r.Name, r.Title, r.IsActive
    FROM system.Role r
    WHERE r.Name = @RoleName;

    SELECT m.Id, m.Name, m.Title, m.AccessRoleIds
    FROM system.SystemMenu m
    ORDER BY m.Id;

    SELECT
        (SELECT COUNT(*) FROM system.[User] u
         CROSS APPLY OPENJSON(CASE WHEN ISJSON(ISNULL(u.RoleIds, N'[]')) = 1 THEN u.RoleIds ELSE N'[]' END) j
         WHERE TRY_CAST(j.value AS bigint) = @RoleId) AS UsersWithRole,
        (SELECT COUNT(*) FROM system.[User] WHERE ISNULL(IsActive, 1) <> 2) AS NonDeletedUsers;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    IF @@TRANCOUNT = 0 AND SESSIONPROPERTY('IDENTITY_INSERT') = 1
        SET IDENTITY_INSERT system.Role OFF;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH
