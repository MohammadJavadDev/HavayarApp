/*
================================================================================
Seed_Law_RoleMembers_FromHts.sql   —   اعضای نقش‌های سیستم حقوقی از دسترسی‌های HTS
================================================================================
منبع: [TMS].[TotalSystem].[dbo].[Vw_Permission] (System_ID = 33 «سیستم حقوقی») صفحات 392 و 439
      (گرنت مستقیم + اعضای گروه‌ها؛ فقط کاربران فعال HTS).
تطبیق کاربر: COALESCE(ActiveDirectoryUsername, Username) ↔ system.[User].Username (بدون حساسیت به حروف).

  صفحه · مجوز                           → نقش
  --------------------------------------------------------------------
  392 · 2  دسترسی کامل                  → Law.JudicialFile.Full
  392 · 4/5/10 جدید/ویرایش/اکسل         → Law.JudicialFile.Editor
  392 · هر مجوز جز NoAccess(1)           → Law.JudicialFile.View
  439 · 2  دسترسی کامل                  → Law.JudicialFileReport.Full
  439 · هر مجوز جز NoAccess(1)           → Law.JudicialFileReport.View
  «نمایش همه»(7) در HTS اثری نداشت؛ نقش ندارد.

اجرا:  @DryRun = 1 (پیش‌فرض) فقط گزارش؛  @DryRun = 0 عضویت‌ها را اضافه می‌کند (چیزی حذف نمی‌شود). Idempotent.
پیش‌نیاز: Linked Server [TMS]؛ Seed_Law_RoleAccess_Menu.sql اجرا شده باشد.
اجرا با Unicode:  sqlcmd -S <server> -d HavayarApp -C -b -I -x -f 65001 -i "Data\Scripts\Seed_Law_RoleMembers_FromHts.sql"
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = 1;   -- 1 = فقط گزارش  |  0 = اعمال عضویت‌ها

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-law-rolemembers-hts';

IF OBJECT_ID('tempdb..#HtsGrant') IS NOT NULL DROP TABLE #HtsGrant;
CREATE TABLE #HtsGrant
(
    Page_ID INT NOT NULL, Permission_ID INT NOT NULL, UserGroup_ID INT NULL, User_FK INT NOT NULL,
    AdName NVARCHAR(200) NULL, HtsUsername NVARCHAR(200) NULL, Source NVARCHAR(80) NOT NULL
);

INSERT INTO #HtsGrant (Page_ID, Permission_ID, UserGroup_ID, User_FK, AdName, HtsUsername, Source)
SELECT p.Page_ID, p.Permission_ID, p.UserGroup_ID, p.User_FK,
       NULLIF(LTRIM(RTRIM(p.ActiveDirectoryUsername)), N''),
       NULLIF(LTRIM(RTRIM(hu.Username)), N''),
       CASE WHEN p.UserGroup_ID IS NULL THEN N'direct' ELSE N'group:' + CAST(p.UserGroup_ID AS NVARCHAR(20)) END
FROM [TMS].[TotalSystem].[dbo].[Vw_Permission] p
LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON hu.User_ID = p.User_FK
WHERE p.System_ID = 33 AND p.Page_ID IN (392, 439) AND p.IsActive = 1 AND p.User_FK IS NOT NULL;

-- کاربرانی که روی صفحه گرنت مستقیم NoAccess دارند → دسترسی ندارند
DELETE g FROM #HtsGrant g
WHERE EXISTS (SELECT 1 FROM #HtsGrant n WHERE n.Page_ID = g.Page_ID AND n.User_FK = g.User_FK
              AND n.Permission_ID = 1 AND n.UserGroup_ID IS NULL);

IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL, RoleName NVARCHAR(200) NOT NULL, Source NVARCHAR(120) NOT NULL);

DECLARE @PermMap TABLE (Page_ID INT, Permission_ID INT, RoleName NVARCHAR(200));
INSERT INTO @PermMap VALUES
    (392, 2,  N'Law.JudicialFile.Full'),
    (392, 4,  N'Law.JudicialFile.Editor'),
    (392, 5,  N'Law.JudicialFile.Editor'),
    (392, 10, N'Law.JudicialFile.Editor'),
    (439, 2,  N'Law.JudicialFileReport.Full');

INSERT INTO #UserRoleMap (Username, RoleName, Source)
SELECT DISTINCT COALESCE(g.AdName, g.HtsUsername), m.RoleName, g.Source + N' p' + CAST(g.Permission_ID AS NVARCHAR(10))
FROM #HtsGrant g
INNER JOIN @PermMap m ON m.Page_ID = g.Page_ID AND m.Permission_ID = g.Permission_ID
WHERE COALESCE(g.AdName, g.HtsUsername) IS NOT NULL;

INSERT INTO #UserRoleMap (Username, RoleName, Source)
SELECT DISTINCT COALESCE(g.AdName, g.HtsUsername),
       CASE g.Page_ID WHEN 392 THEN N'Law.JudicialFile.View' ELSE N'Law.JudicialFileReport.View' END,
       g.Source + N' p' + CAST(g.Page_ID AS NVARCHAR(10)) + N'-any'
FROM #HtsGrant g
WHERE g.Permission_ID <> 1 AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL;

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
ORDER BY CASE Status WHEN N'add' THEN 0 WHEN N'already' THEN 1 WHEN N'inactive-user' THEN 2 WHEN N'missing-user' THEN 3 ELSE 4 END,
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
    PRINT N'=== DONE Seed_Law_RoleMembers_FromHts — کاربران باید دوباره لاگین کنند (کش نقش) ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH

SELECT r.Id, r.Name,
       (SELECT COUNT(*) FROM system.[User] u CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j WHERE TRY_CAST(j.value AS BIGINT) = r.Id) AS Members
FROM system.Role r
WHERE r.Name LIKE N'Law.JudicialFile%'
ORDER BY r.Name;
