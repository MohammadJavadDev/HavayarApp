/*
================================================================================
Seed_GuardPartIo_RoleMembers_FromHts.sql  —  اعضای نقش‌های ورود/خروج کالا حراست/دژبان از گروه‌های HTS
================================================================================
منبع: [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] (فقط کاربران فعال HTS). دسترسی مستقیم کاربر روی این صفحات در HTS وجود ندارد.
  گروه 312 «کارشناسان ورود/خروج کالا (حراست/دژبان)»           → نقش 260312 (همنام)
  گروه 330 «کارشناسان مشاهده ورود و خروج کالا (حراست/دژبان)» → نقش 260330 (همنام)
  گروه 316 «کارشناسان گزارش ورود/خروج کالا (حراست/دژبان)»    → نقش 260316 (همنام)
  گروه 350 «مشاهده کلی HTS»                                   → نقش «مشاهده کلی HTS» (260350)
  (دسترسی مستقیم کاربر 661 «حسابرس» با ShowAll روی صفحه 431 در گزارش استفاده نمی‌شود و منتقل نمی‌شود.)
تطبیق کاربر: COALESCE(ActiveDirectoryUsername, Username) ↔ system.[User].Username (بدون حساسیت به حروف).

اجرا:  @DryRun = 1 (پیش‌فرض) فقط گزارش؛  @DryRun = 0 عضویت‌ها را اضافه می‌کند (چیزی حذف نمی‌شود). Idempotent.
پیش‌نیاز: Linked Server [TMS]؛ Seed_GuardPartIo_RolesAndAccess.sql اجرا شده باشد.
UTF-8 BOM.  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_GuardBasicInfo_RoleMembers_FromHts.sql"
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = 1;   -- 1 = فقط گزارش  |  0 = اعمال عضویت‌ها

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-guard-partio-rolemembers';

IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL, RoleName NVARCHAR(200) NOT NULL, Source NVARCHAR(120) NOT NULL);

DECLARE @GroupRole TABLE (UserGroup_ID INT, RoleName NVARCHAR(200));
INSERT INTO @GroupRole VALUES
    (312, N'کارشناسان ورود/خروج کالا (حراست/دژبان)'),
    (330, N'کارشناسان مشاهده ورود و خروج کالا (حراست/دژبان)'),
    (316, N'کارشناسان گزارش ورود/خروج کالا (حراست/دژبان)'),
    (350, N'مشاهده کلی HTS');

INSERT INTO #UserRoleMap (Username, RoleName, Source)
SELECT DISTINCT COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), u.Username), gr.RoleName,
       N'group:' + CAST(gr.UserGroup_ID AS NVARCHAR(10))
FROM [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m
INNER JOIN @GroupRole gr ON gr.UserGroup_ID = m.UserGroup_FK
INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
WHERE u.IsActive = 1;

-- نام کاربری جایگزین HTS (اگر با AD فرق دارد)
INSERT INTO #UserRoleMap (Username, RoleName, Source)
SELECT DISTINCT hu.Username, m.RoleName, m.Source + N'+htsUser'
FROM #UserRoleMap m
INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON LOWER(hu.ActiveDirectoryUsername) = LOWER(m.Username)
WHERE hu.Username IS NOT NULL AND LOWER(hu.Username) <> LOWER(m.Username)
  AND NOT EXISTS (SELECT 1 FROM #UserRoleMap x WHERE LOWER(x.Username) = LOWER(hu.Username) AND x.RoleName = m.RoleName);

IF OBJECT_ID('tempdb..#Resolved') IS NOT NULL DROP TABLE #Resolved;
CREATE TABLE #Resolved
(
    Username NVARCHAR(200) NOT NULL, RoleName NVARCHAR(200) NOT NULL, Source NVARCHAR(120) NOT NULL,
    UserId BIGINT NULL, RoleId BIGINT NULL,
    Status NVARCHAR(20) NOT NULL   -- add | already | missing-user | inactive-user | missing-role
);

INSERT INTO #Resolved (Username, RoleName, Source, UserId, RoleId, Status)
SELECT m.Username, m.RoleName, m.Source, u.Id, r.Id,
    CASE
        WHEN r.Id IS NULL THEN N'missing-role'
        WHEN u.Id IS NULL THEN N'missing-user'
        WHEN ISNULL(u.IsActive, 0) <> 1 THEN N'inactive-user'
        WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(u.RoleIds, N'[]')) j WHERE TRY_CAST(j.value AS BIGINT) = r.Id) THEN N'already'
        ELSE N'add'
    END
FROM #UserRoleMap m
LEFT JOIN system.Role r ON r.Name = m.RoleName
OUTER APPLY (
    SELECT TOP 1 x.Id, x.IsActive, x.RoleIds
    FROM system.[User] x
    WHERE LOWER(x.Username) = LOWER(m.Username)
    ORDER BY CASE WHEN x.IsActive = 1 THEN 0 ELSE 1 END, x.Id
) u;

;WITH d AS (
    SELECT UserId, RoleName,
           ROW_NUMBER() OVER (PARTITION BY UserId, RoleName
               ORDER BY CASE Status WHEN N'add' THEN 0 WHEN N'already' THEN 1 ELSE 2 END, Username) AS rn
    FROM #Resolved WHERE UserId IS NOT NULL
)
DELETE FROM d WHERE rn > 1;

PRINT N'=== گزارش به ازای هر نقش ===';
SELECT RoleName,
       SUM(CASE WHEN Status = N'add' THEN 1 ELSE 0 END) AS ToAdd,
       SUM(CASE WHEN Status = N'already' THEN 1 ELSE 0 END) AS Already,
       SUM(CASE WHEN Status = N'inactive-user' THEN 1 ELSE 0 END) AS InactiveUser,
       SUM(CASE WHEN Status = N'missing-user' THEN 1 ELSE 0 END) AS MissingUser,
       SUM(CASE WHEN Status = N'missing-role' THEN 1 ELSE 0 END) AS MissingRole
FROM #Resolved GROUP BY RoleName ORDER BY RoleName;

SELECT Status, RoleName, Username, Source, UserId, RoleId
FROM #Resolved
WHERE Status <> N'already'
ORDER BY CASE Status WHEN N'add' THEN 0 WHEN N'inactive-user' THEN 2 WHEN N'missing-user' THEN 3 ELSE 4 END,
         RoleName, Username;

IF @DryRun = 1
BEGIN
    PRINT N'=== DRY RUN — هیچ تغییری اعمال نشد. برای اعمال: SET @DryRun = 0 ===';
    RETURN;
END

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @UserId BIGINT, @RoleId BIGINT, @RoleName NVARCHAR(200);
    DECLARE @Assigned INT = 0;

    DECLARE apply_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT UserId, RoleId, RoleName FROM #Resolved WHERE Status = N'add';
    OPEN apply_cur;
    FETCH NEXT FROM apply_cur INTO @UserId, @RoleId, @RoleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM system.[User] u
            CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
            WHERE u.Id = @UserId AND TRY_CAST(j.value AS BIGINT) = @RoleId)
        BEGIN
            UPDATE system.[User]
            SET RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                        THEN N'[' + CAST(@RoleId AS NVARCHAR(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@RoleId AS NVARCHAR(20)) + N']')
                END,
                Roles = CASE
                    WHEN EXISTS (SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.value = @RoleName) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]'
                        THEN N'["' + @RoleName + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @RoleName + N'"]')
                END,
                ModifiedById = 1, ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @UserId;
            SET @Assigned = @Assigned + 1;
        END
        FETCH NEXT FROM apply_cur INTO @UserId, @RoleId, @RoleName;
    END
    CLOSE apply_cur;
    DEALLOCATE apply_cur;

    COMMIT TRANSACTION;
    PRINT N'  User role assignments applied: ' + CAST(@Assigned AS NVARCHAR(20));
    PRINT N'=== DONE Seed_GuardBasicInfo_RoleMembers_FromHts — کاربران باید دوباره لاگین کنند (کش نقش) ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH

SELECT r.Id, r.Name,
       (SELECT COUNT(*) FROM system.[User] u CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j WHERE TRY_CAST(j.value AS BIGINT) = r.Id) AS Members
FROM system.Role r
WHERE r.Name IN (N'کارشناسان ورود/خروج کالا (حراست/دژبان)', N'کارشناسان ورود/خروج پرسنل (حراست/دژبان)', N'مشاهده کلی HTS')
ORDER BY r.Id;
