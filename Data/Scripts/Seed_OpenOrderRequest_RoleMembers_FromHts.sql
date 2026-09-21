/*
================================================================================
Seed_OpenOrderRequest_RoleMembers_FromHts.sql   (WP4 — D38 نگاشت اعضای گروه‌های HTS → نقش‌های جدید)
================================================================================
اعضای گروه‌ها و گرنت‌های مستقیم HTS روی صفحات 74 (درخواست های باز)، 76 (تنظیمات) و 77 (پیشینه) به
نقش‌های Sup.OpenOrderRequest.* اضافه می‌شوند (system.[User].RoleIds / Roles — آرایه JSON).
منبع: [TMS].[TotalSystem].[dbo].[Vw_Permission] (System_ID = 13 «سیستم تدارکات») + اعضای فعلی Gnr_UserGroupMember.
تطبیق کاربر: COALESCE(Gnr_User.ActiveDirectoryUsername, Gnr_User.Username) ↔ system.[User].Username (بدون حساسیت به حروف)
             + نام کاربری جایگزین Gnr_User.Username (همان روش Seed_SaleOrderDetail_RolesAndUsers.sql / SyncOpenOrderRequestFromTotalSystem.sql).

  نگاشت مجوز (Page · Permission_ID)            → نقش جدید
  --------------------------------------------------------------------------------------------
  74 · هر مجوز جز NoAccess(1)                   → Sup.OpenOrderRequest.View          (HTS: هر مجوز صفحه 74 = دیدن صفحه)
  74 · 2  FullAccess                            → SupplyAndPurchase                  (گروه 17 مدیر سیستم تدارکات)
  74 · 7  ShowAll                               → Sup.OpenOrderRequest.ShowAll       (گروه 44، مستقیم ×1)
  74 · 12 Accept_Engineering                    → Sup.OpenOrderRequest.EngineeringAccept (گروه‌های 70، 28، مستقیم ×24)
  74 · 22 Stop                                  → Sup.OpenOrderRequest.Stop          (گروه‌های 44، 27، 593، مستقیم ×5)
  74 · 23 Start                                 → Sup.OpenOrderRequest.Start         (گروه 28)
  74 · 24 Sending                               → Sup.OpenOrderRequest.Sending       (فقط اگر گرنت صریح وجود داشته باشد)
  74 · 25 Query                                 → Sup.OpenOrderRequest.Query         (فقط اگر گرنت صریح وجود داشته باشد)
  74 · 82 HasEngineeringPermission              → Sup.OpenOrderRequest.HasEngineering (گروه‌های 462، 70)
  74 · 101 Terminate                            → Sup.OpenOrderRequest.Terminate     (گروه 233)
  74 · 187 SalesOrProjectPermission             → Sup.OpenOrderRequest.SalesOrProjectAccept (گروه 536)
  76 · 2  FullAccess                            → Sup.OpenOrderRequest.ConfigManage  (گروه‌های 430، 28)
  77 · 2/3 FullAccess/Read                      → Sup.OpenOrderRequest.HistoryView   (گروه‌های 395، 44، 70، 28، 303، 33، 60، 350)
  گروه 27 «کارشناسان تدارکات» (واحد 51)         → Sup.OpenOrderRequest.Supply
  گروه 28 «کارشناسان صنایع» (واحد 12)           → Sup.OpenOrderRequest.Industrial
  74 · 13 Accept / 26 Refresh                   → (بی‌اثر — D52)

  بدون معادل و عمداً نادیده گرفته‌شده: Sup.OpenOrderRequest.ConfigManageStaticPersonel (نفرات ثابت؛ در HTS لیست هاردکد بود)،
  نقش‌های منو (SupplierMenu و …) — D46 تصمیم جدا.

اجرا:  @DryRun = 1 (پیش‌فرض) فقط گزارش می‌دهد (شمارش به ازای هر نقش + فهرست کامل کاربر↔نقش).
       @DryRun = 0 عضویت‌ها را اضافه می‌کند. هیچ عضویتی حذف نمی‌شود. Idempotent.
پیش‌نیاز: Linked Server [TMS] → TotalSystem؛ نقش‌های 100013/100014 (Seed_OpenOrderRequest_RoleAccess.sql) از قبل اجرا شده باشند.
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun BIT = 1;   -- 1 = فقط گزارش  |  0 = اعمال عضویت‌ها

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-oor-rolemembers-hts';

-----------------------------------------------------------------------------
PRINT N'=== [1] گرنت‌های HTS (Vw_Permission + اعضای فعلی گروه‌ها) — صفحات 74/76/77 ===';
-----------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#HtsGrant') IS NOT NULL DROP TABLE #HtsGrant;
CREATE TABLE #HtsGrant
(
    Page_ID        INT            NOT NULL,
    Permission_ID  INT            NOT NULL,
    UserGroup_ID   INT            NULL,
    User_FK        INT            NOT NULL,
    AdName         NVARCHAR(200)  NULL,
    HtsUsername    NVARCHAR(200)  NULL,
    Source         NVARCHAR(80)   NOT NULL
);

-- 1.1 ردیف‌های ویو (مستقیم + گروهی)
INSERT INTO #HtsGrant (Page_ID, Permission_ID, UserGroup_ID, User_FK, AdName, HtsUsername, Source)
SELECT
    p.Page_ID,
    p.Permission_ID,
    p.UserGroup_ID,
    p.User_FK,
    NULLIF(LTRIM(RTRIM(p.ActiveDirectoryUsername)), N''),
    NULLIF(LTRIM(RTRIM(hu.Username)), N''),
    CASE WHEN p.UserGroup_ID IS NULL THEN N'direct' ELSE N'group:' + CAST(p.UserGroup_ID AS NVARCHAR(20)) END
FROM [TMS].[TotalSystem].[dbo].[Vw_Permission] p
LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON hu.User_ID = p.User_FK
WHERE p.System_ID = 13
  AND p.Page_ID IN (74, 76, 77)
  AND p.IsActive = 1
  AND p.User_FK IS NOT NULL;

-- 1.2 اعضای فعلی گروه‌هایی که روی 74/76/77 گرنت دارند (اگر ویو عقب مانده باشد) — فقط کاربران فعال HTS
INSERT INTO #HtsGrant (Page_ID, Permission_ID, UserGroup_ID, User_FK, AdName, HtsUsername, Source)
SELECT DISTINCT
    gp.Page_ID,
    gp.Permission_ID,
    gp.UserGroup_ID,
    u.User_ID,
    NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''),
    NULLIF(LTRIM(RTRIM(u.Username)), N''),
    N'group-member:' + CAST(gp.UserGroup_ID AS NVARCHAR(20))
FROM (
    SELECT DISTINCT Page_ID, Permission_ID, UserGroup_ID
    FROM [TMS].[TotalSystem].[dbo].[Vw_Permission]
    WHERE System_ID = 13
      AND Page_ID IN (74, 76, 77)
      AND IsActive = 1
      AND UserGroup_ID IS NOT NULL
) gp
INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = gp.UserGroup_ID
INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
WHERE u.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 FROM #HtsGrant x
      WHERE x.Page_ID = gp.Page_ID
        AND x.Permission_ID = gp.Permission_ID
        AND x.User_FK = u.User_ID
  );

-- 1.3 کاربرانی که روی صفحه گرنت مستقیم NoAccess دارند (HTS: hasViewPagePermission → بدون دسترسی)
IF OBJECT_ID('tempdb..#NoAccess') IS NOT NULL DROP TABLE #NoAccess;
SELECT DISTINCT Page_ID, User_FK
INTO #NoAccess
FROM #HtsGrant
WHERE Permission_ID = 1 AND UserGroup_ID IS NULL;

DELETE g
FROM #HtsGrant g
WHERE EXISTS (SELECT 1 FROM #NoAccess n WHERE n.Page_ID = g.Page_ID AND n.User_FK = g.User_FK);

DECLARE @HtsGrantRows INT = (SELECT COUNT(*) FROM #HtsGrant);
PRINT N'  HTS grant rows: ' + CAST(@HtsGrantRows AS NVARCHAR(20));

-----------------------------------------------------------------------------
PRINT N'=== [2] نگاشت مجوز/گروه → نقش ===';
-----------------------------------------------------------------------------
DECLARE @PermMap TABLE (Page_ID INT, Permission_ID INT, RoleName NVARCHAR(200));
INSERT INTO @PermMap (Page_ID, Permission_ID, RoleName) VALUES
    (74,   2, N'SupplyAndPurchase'),
    (74,   7, N'Sup.OpenOrderRequest.ShowAll'),
    (74,  12, N'Sup.OpenOrderRequest.EngineeringAccept'),
    (74,  22, N'Sup.OpenOrderRequest.Stop'),
    (74,  23, N'Sup.OpenOrderRequest.Start'),
    (74,  24, N'Sup.OpenOrderRequest.Sending'),
    (74,  25, N'Sup.OpenOrderRequest.Query'),
    (74,  82, N'Sup.OpenOrderRequest.HasEngineering'),
    (74, 101, N'Sup.OpenOrderRequest.Terminate'),
    (74, 187, N'Sup.OpenOrderRequest.SalesOrProjectAccept'),
    (76,   2, N'Sup.OpenOrderRequest.ConfigManage'),
    (77,   2, N'Sup.OpenOrderRequest.HistoryView'),
    (77,   3, N'Sup.OpenOrderRequest.HistoryView');

DECLARE @GroupMap TABLE (UserGroup_ID INT, RoleName NVARCHAR(200));
INSERT INTO @GroupMap (UserGroup_ID, RoleName) VALUES
    (27, N'Sup.OpenOrderRequest.Supply'),       -- کارشناسان تدارکات (واحد 51)
    (28, N'Sup.OpenOrderRequest.Industrial');   -- کارشناسان صنایع (واحد 12)

IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
CREATE TABLE #UserRoleMap
(
    Username NVARCHAR(200) NOT NULL,
    RoleName NVARCHAR(200) NOT NULL,
    Source   NVARCHAR(120) NOT NULL
);

-- 2.1 مجوز → نقش
INSERT INTO #UserRoleMap (Username, RoleName, Source)
SELECT DISTINCT COALESCE(g.AdName, g.HtsUsername), m.RoleName, g.Source + N' p' + CAST(g.Permission_ID AS NVARCHAR(10))
FROM #HtsGrant g
INNER JOIN @PermMap m ON m.Page_ID = g.Page_ID AND m.Permission_ID = g.Permission_ID
WHERE COALESCE(g.AdName, g.HtsUsername) IS NOT NULL;

-- 2.2 هر مجوز صفحه 74 (جز NoAccess) → View
INSERT INTO #UserRoleMap (Username, RoleName, Source)
SELECT DISTINCT COALESCE(g.AdName, g.HtsUsername), N'Sup.OpenOrderRequest.View', g.Source + N' p74-any'
FROM #HtsGrant g
WHERE g.Page_ID = 74
  AND g.Permission_ID <> 1
  AND COALESCE(g.AdName, g.HtsUsername) IS NOT NULL;

-- 2.3 گروه → نقش (Supply / Industrial)
INSERT INTO #UserRoleMap (Username, RoleName, Source)
SELECT DISTINCT COALESCE(g.AdName, g.HtsUsername), m.RoleName, N'group:' + CAST(g.UserGroup_ID AS NVARCHAR(20))
FROM #HtsGrant g
INNER JOIN @GroupMap m ON m.UserGroup_ID = g.UserGroup_ID
WHERE COALESCE(g.AdName, g.HtsUsername) IS NOT NULL;

-- 2.4 نام کاربری جایگزین (Gnr_User.Username) اگر با AD فرق دارد
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

;WITH d AS (
    SELECT Username, RoleName, ROW_NUMBER() OVER (PARTITION BY LOWER(Username), RoleName ORDER BY Username) AS rn
    FROM #UserRoleMap
)
DELETE FROM d WHERE rn > 1;

-----------------------------------------------------------------------------
PRINT N'=== [3] تطبیق با کاربران/نقش‌های پنل ===';
-----------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#Resolved') IS NOT NULL DROP TABLE #Resolved;
CREATE TABLE #Resolved
(
    Username NVARCHAR(200) NOT NULL,
    RoleName NVARCHAR(200) NOT NULL,
    Source   NVARCHAR(120) NOT NULL,
    UserId   BIGINT NULL,
    RoleId   BIGINT NULL,
    Status   NVARCHAR(20) NOT NULL   -- add | already | missing-user | inactive-user | missing-role
);

INSERT INTO #Resolved (Username, RoleName, Source, UserId, RoleId, Status)
SELECT
    m.Username,
    m.RoleName,
    m.Source,
    u.Id,
    r.Id,
    CASE
        WHEN r.Id IS NULL THEN N'missing-role'
        WHEN u.Id IS NULL THEN N'missing-user'
        WHEN ISNULL(u.IsActive, 0) <> 1 THEN N'inactive-user'
        WHEN EXISTS (
            SELECT 1 FROM OPENJSON(ISNULL(u.RoleIds, N'[]')) j
            WHERE TRY_CAST(j.value AS BIGINT) = r.Id
        ) THEN N'already'
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

-- یک کاربر ممکن است با دو نام کاربری (AD / HTS) به یک نقش نگاشت شده باشد → یکی کافی است
;WITH d AS (
    SELECT UserId, RoleName, Status,
           ROW_NUMBER() OVER (PARTITION BY UserId, RoleName ORDER BY CASE Status WHEN N'add' THEN 0 WHEN N'already' THEN 1 ELSE 2 END, Username) AS rn
    FROM #Resolved
    WHERE UserId IS NOT NULL
)
DELETE FROM d WHERE rn > 1;

-----------------------------------------------------------------------------
PRINT N'=== [4] گزارش به ازای هر نقش (قبل از اعمال) ===';
-----------------------------------------------------------------------------
DECLARE @RptRole NVARCHAR(200), @RptAdd INT, @RptAlready INT, @RptMissing INT, @RptInactive INT, @RptCurrent INT;
DECLARE rpt_cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT RoleName,
           SUM(CASE WHEN Status = N'add' THEN 1 ELSE 0 END),
           SUM(CASE WHEN Status = N'already' THEN 1 ELSE 0 END),
           SUM(CASE WHEN Status = N'missing-user' THEN 1 ELSE 0 END),
           SUM(CASE WHEN Status = N'inactive-user' THEN 1 ELSE 0 END)
    FROM #Resolved
    GROUP BY RoleName
    ORDER BY RoleName;
OPEN rpt_cur;
FETCH NEXT FROM rpt_cur INTO @RptRole, @RptAdd, @RptAlready, @RptMissing, @RptInactive;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @RptCurrent = COUNT(*)
    FROM system.[User] u
    CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
    INNER JOIN system.Role r ON r.Id = TRY_CAST(j.value AS BIGINT)
    WHERE r.Name = @RptRole;

    PRINT N'  ' + @RptRole
        + N'  | current=' + CAST(ISNULL(@RptCurrent, 0) AS NVARCHAR(10))
        + N'  add=' + CAST(@RptAdd AS NVARCHAR(10))
        + N'  already=' + CAST(@RptAlready AS NVARCHAR(10))
        + N'  missing-user=' + CAST(@RptMissing AS NVARCHAR(10))
        + N'  inactive-user=' + CAST(@RptInactive AS NVARCHAR(10));

    FETCH NEXT FROM rpt_cur INTO @RptRole, @RptAdd, @RptAlready, @RptMissing, @RptInactive;
END
CLOSE rpt_cur;
DEALLOCATE rpt_cur;

IF EXISTS (SELECT 1 FROM #Resolved WHERE Status = N'missing-role')
    PRINT N'  !! نقش(هایی) در system.Role یافت نشد — ابتدا Seed_OpenOrderRequest_RoleAccess.sql را اجرا کنید.';

-- فهرست کامل برای بازبینی (ابتدا add ها)
SELECT Status, RoleName, Username, Source, UserId, RoleId
FROM #Resolved
ORDER BY CASE Status WHEN N'add' THEN 0 WHEN N'already' THEN 1 WHEN N'inactive-user' THEN 2 WHEN N'missing-user' THEN 3 ELSE 4 END, RoleName, Username;

-----------------------------------------------------------------------------
IF @DryRun = 1
BEGIN
    PRINT N'=== DRY RUN — هیچ تغییری اعمال نشد. برای اعمال: SET @DryRun = 0 ===';
    RETURN;
END

PRINT N'=== [5] اعمال عضویت‌ها (فقط افزودن؛ هیچ عضویتی حذف نمی‌شود) ===';
-----------------------------------------------------------------------------
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @UserId BIGINT, @RoleId BIGINT, @RoleName NVARCHAR(200);
    DECLARE @Assigned INT = 0;

    DECLARE apply_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT UserId, RoleId, RoleName
        FROM #Resolved
        WHERE Status = N'add';
    OPEN apply_cur;
    FETCH NEXT FROM apply_cur INTO @UserId, @RoleId, @RoleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- چک مجدد (idempotent)
        IF NOT EXISTS (
            SELECT 1 FROM system.[User] u
            CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
            WHERE u.Id = @UserId AND TRY_CAST(j.value AS BIGINT) = @RoleId
        )
        BEGIN
            UPDATE system.[User]
            SET
                RoleIds = CASE
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
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @UserId;
            SET @Assigned = @Assigned + 1;
        END

        FETCH NEXT FROM apply_cur INTO @UserId, @RoleId, @RoleName;
    END
    CLOSE apply_cur;
    DEALLOCATE apply_cur;

    COMMIT TRANSACTION;
    PRINT N'  User role assignments applied: ' + CAST(@Assigned AS NVARCHAR(20));
    PRINT N'=== DONE Seed_OpenOrderRequest_RoleMembers_FromHts — کاربران باید دوباره لاگین کنند (کش نقش) ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

-- ---------------------------------------------------------------- verify
SELECT r.Id, r.Name,
       (SELECT COUNT(*) FROM system.[User] u CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j WHERE TRY_CAST(j.value AS BIGINT) = r.Id) AS Members
FROM system.Role r
WHERE r.Name LIKE N'Sup.OpenOrderRequest.%' OR r.Name = N'SupplyAndPurchase'
ORDER BY r.Id;
