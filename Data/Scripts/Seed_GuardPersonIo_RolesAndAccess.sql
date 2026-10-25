/*
================================================================================
Seed_GuardPersonIo_RolesAndAccess.sql  —  ورود/خروج پرسنل/پیمانکار (HTS System 9، صفحه 427)
================================================================================
نقش‌ها از قبل هستند (Seed_GuardBasicInfo):
  260312 «کارشناسان ورود/خروج کالا (حراست/دژبان)» — HTS: جدید + ویرایش + حذف
  260313 «کارشناسان ورود/خروج پرسنل (حراست/دژبان)» — HTS: جدید + ویرایش + حذف
  «مشاهده کلی HTS» — HTS: فقط مشاهده
در سیستم جدید نقش‌های عملیاتی لیست/دریافت را هم دارند چون صفحه را باز می‌کنند.
مشاهده کلی: لیست + دریافت + اکسل + نمایه. حذف ندارد.
اکسل گرید قدیم برای هر کسی که صفحه را می‌بیند روشن بود.

منو: برگ به گروه موجود «عملیات» زیر «حراست / دژبان» (HrmSystem و AllMenus). گروه از نو ساخته نمی‌شود.

پیش‌نیاز: Seed_GuardPersonIo_DataProfiles.sql و migration AddGuardPersonIo و منوی عملیات (Seed_GuardPartIo).
UTF-8 BOM. Idempotent.
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_GuardPersonIo_RolesAndAccess.sql"
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-guard-personio-access';

IF OBJECT_ID(N'Hrm.GuardPersonIo') IS NULL
BEGIN
    RAISERROR(N'جدول Hrm.GuardPersonIo وجود ندارد. ابتدا migration AddGuardPersonIo را اعمال کنید.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM [system].[SavedQuery] WHERE Name = N'Hrm_GuardPersonIo_List')
BEGIN
    RAISERROR(N'نمایه Hrm_GuardPersonIo_List یافت نشد. ابتدا Seed_GuardPersonIo_DataProfiles.sql را اجرا کنید.', 16, 1);
    RETURN;
END

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @GoodsRole BIGINT  = (SELECT Id FROM [system].[Role] WHERE Name = N'کارشناسان ورود/خروج کالا (حراست/دژبان)');
    DECLARE @PersonRole BIGINT = (SELECT Id FROM [system].[Role] WHERE Name = N'کارشناسان ورود/خروج پرسنل (حراست/دژبان)');
    DECLARE @ViewRole BIGINT   = (SELECT Id FROM [system].[Role] WHERE Name = N'مشاهده کلی HTS');
    IF @GoodsRole IS NULL OR @PersonRole IS NULL OR @ViewRole IS NULL
    BEGIN
        RAISERROR(N'یکی از نقش‌های کالا، پرسنل یا مشاهده کلی HTS یافت نشد. ابتدا Seed_GuardBasicInfo_RolesAndAccess.sql را اجرا کنید.', 16, 1);
    END

    DECLARE @Act TABLE (Suffix NVARCHAR(30), AType INT, IType INT, ForView BIT, ForWrite BIT);
    INSERT INTO @Act VALUES
        (N'list',          1, 1,    1, 1),
        (N'fetchdata',     2, 2,    1, 1),
        (N'new',           1, 4,    0, 1),
        (N'add',           2, 4,    0, 1),
        (N'edit',          1, 5,    0, 1),
        (N'update',        2, 5,    0, 1),
        (N'delete',        2, 6,    0, 1),
        (N'exporttoexcel', 2, 1000, 1, 1);

    DECLARE @Grants TABLE (RoleId BIGINT, Path NVARCHAR(300), AType INT, IType INT);
    INSERT INTO @Grants
    SELECT @ViewRole, N'/panel/hrm/guardpersonio/' + a.Suffix, a.AType, a.IType
    FROM @Act a WHERE a.ForView = 1;
    INSERT INTO @Grants
    SELECT @GoodsRole, N'/panel/hrm/guardpersonio/' + a.Suffix, a.AType, a.IType
    FROM @Act a WHERE a.ForWrite = 1;
    INSERT INTO @Grants
    SELECT @PersonRole, N'/panel/hrm/guardpersonio/' + a.Suffix, a.AType, a.IType
    FROM @Act a WHERE a.ForWrite = 1;

    DELETE ra
    FROM [system].[RoleAccess] ra
    WHERE ra.RoleId IN (@GoodsRole, @PersonRole, @ViewRole)
      AND (ra.Path LIKE N'/panel/hrm/guardpersonio/%'
           OR (ra.ActionAccessType = 3 AND ra.Path IN (
                SELECT N'dataProfile_' + CAST(q.Id AS NVARCHAR(20)) FROM [system].[SavedQuery] q
                WHERE q.Name = N'Hrm_GuardPersonIo_List')));

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT g.Path, g.AType, g.IType, N'Entities.App.Hrm.GuardPersonIo', g.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Grants g;

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT N'dataProfile_' + CAST(q.Id AS NVARCHAR(20)), 3, 7, N'Entities.App.Hrm.GuardPersonIo', q.Title, q.Id, g.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM (SELECT DISTINCT RoleId FROM @Grants) g
    INNER JOIN [system].[SavedQuery] q ON q.Name = N'Hrm_GuardPersonIo_List';

    -- ---------------------------------------------------------------- menu leaf under existing «عملیات»
    DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"ورود/خروج پرسنل/پیمانکار","icon":"ki-user ki-outline","iconColor":"#1fc4e5","path":"/panel/hrm/guardpersonio/list","a_attr":{"href":"/panel/hrm/guardpersonio/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/hrm/guardpersonio/list"},"children":[]}';

    DECLARE @MenuContent NVARCHAR(MAX) = (SELECT Content FROM [system].[SystemMenu] WHERE Name = N'HrmSystem');
    IF @MenuContent IS NULL OR ISJSON(@MenuContent) <> 1
    BEGIN
        RAISERROR(N'منوی HrmSystem یافت نشد یا JSON نامعتبر است.', 16, 1);
    END

    DECLARE @GuardIdx INT = (SELECT TOP 1 CAST(j.[key] AS INT) FROM OPENJSON(@MenuContent) j WHERE JSON_VALUE(j.value, '$.text') = N'حراست / دژبان');
    IF @GuardIdx IS NULL
    BEGIN
        RAISERROR(N'گروه «حراست / دژبان» در منوی HrmSystem یافت نشد.', 16, 1);
    END
    DECLARE @GuardGroup NVARCHAR(MAX) = (SELECT TOP 1 CAST(j.value AS NVARCHAR(MAX)) FROM OPENJSON(@MenuContent) j WHERE CAST(j.[key] AS INT) = @GuardIdx);
    DECLARE @GuardChildren NVARCHAR(MAX) = JSON_QUERY(@GuardGroup, '$.children');
    DECLARE @OpsIdx INT = (SELECT TOP 1 CAST(c.[key] AS INT) FROM OPENJSON(@GuardChildren) c WHERE JSON_VALUE(c.value, '$.text') = N'عملیات');
    IF @OpsIdx IS NULL
    BEGIN
        RAISERROR(N'گروه «عملیات» زیر حراست / دژبان یافت نشد. ابتدا Seed_GuardPartIo_RolesAndAccess.sql را اجرا کنید.', 16, 1);
    END
    DECLARE @OpsGroup NVARCHAR(MAX) = (SELECT TOP 1 CAST(c.value AS NVARCHAR(MAX)) FROM OPENJSON(@GuardChildren) c WHERE CAST(c.[key] AS INT) = @OpsIdx);
    DECLARE @OpsChildren NVARCHAR(MAX) = JSON_QUERY(@OpsGroup, '$.children');
    SELECT @OpsChildren = N'[' + ISNULL(STRING_AGG(CAST(c.value AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY CAST(c.[key] AS INT)), N'') + N']'
    FROM OPENJSON(@OpsChildren) c
    WHERE JSON_VALUE(c.value, '$.text') <> N'ورود/خروج پرسنل/پیمانکار';
    SET @OpsChildren = JSON_MODIFY(@OpsChildren, 'append $', JSON_QUERY(@Leaf));
    SET @OpsGroup = JSON_MODIFY(@OpsGroup, '$.children', JSON_QUERY(@OpsChildren));
    SET @GuardChildren = JSON_MODIFY(@GuardChildren, '$[' + CAST(@OpsIdx AS NVARCHAR(10)) + ']', JSON_QUERY(@OpsGroup));
    SET @GuardGroup = JSON_MODIFY(@GuardGroup, '$.children', JSON_QUERY(@GuardChildren));
    SET @MenuContent = JSON_MODIFY(@MenuContent, '$[' + CAST(@GuardIdx AS NVARCHAR(10)) + ']', JSON_QUERY(@GuardGroup));

    DECLARE @MenuRoleIds NVARCHAR(MAX) = (SELECT AccessRoleIds FROM [system].[SystemMenu] WHERE Name = N'HrmSystem');
    DECLARE @MenuRoleNames NVARCHAR(MAX) = (SELECT AccessRoles FROM [system].[SystemMenu] WHERE Name = N'HrmSystem');
    SELECT @MenuRoleIds = N'[' + STRING_AGG(CAST(x.Id AS NVARCHAR(20)), N',') + N']'
    FROM (
        SELECT TRY_CAST(j.value AS BIGINT) AS Id FROM OPENJSON(ISNULL(@MenuRoleIds, N'[]')) j
        UNION
        SELECT r.Id FROM [system].[Role] r WHERE r.Id IN (@GoodsRole, @PersonRole, @ViewRole)
    ) x;
    SELECT @MenuRoleNames = N'[' + STRING_AGG(N'"' + STRING_ESCAPE(x.Name, 'json') + N'"', N',') + N']'
    FROM (
        SELECT CAST(j.value AS NVARCHAR(300)) AS Name FROM OPENJSON(ISNULL(@MenuRoleNames, N'[]')) j
        UNION
        SELECT r.Name FROM [system].[Role] r WHERE r.Id IN (@GoodsRole, @PersonRole, @ViewRole)
    ) x;

    UPDATE [system].[SystemMenu]
    SET Content = @MenuContent, AccessRoles = @MenuRoleNames, AccessRoleIds = @MenuRoleIds,
        ModifiedById = 1, ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
    WHERE Name = N'HrmSystem';

    IF EXISTS (SELECT 1 FROM [system].[SystemMenu] WHERE Name = N'AllMenus' AND ISJSON(Content) = 1)
    BEGIN
        DECLARE @AllContent NVARCHAR(MAX) = (SELECT Content FROM [system].[SystemMenu] WHERE Name = N'AllMenus');
        DECLARE @HrIdx INT = (SELECT TOP 1 CAST(j.[key] AS INT) FROM OPENJSON(@AllContent) j WHERE JSON_VALUE(j.value, '$.text') = N'منابع انسانی');
        IF @HrIdx IS NOT NULL
        BEGIN
            DECLARE @HrGroup NVARCHAR(MAX) = (SELECT TOP 1 CAST(j.value AS NVARCHAR(MAX)) FROM OPENJSON(@AllContent) j WHERE CAST(j.[key] AS INT) = @HrIdx);
            DECLARE @HrChildren NVARCHAR(MAX) = JSON_QUERY(@HrGroup, '$.children');
            DECLARE @AllGuardIdx INT = (SELECT TOP 1 CAST(c.[key] AS INT) FROM OPENJSON(@HrChildren) c WHERE JSON_VALUE(c.value, '$.text') = N'حراست / دژبان');
            IF @AllGuardIdx IS NOT NULL
            BEGIN
                DECLARE @AllGuard NVARCHAR(MAX) = (SELECT TOP 1 CAST(c.value AS NVARCHAR(MAX)) FROM OPENJSON(@HrChildren) c WHERE CAST(c.[key] AS INT) = @AllGuardIdx);
                DECLARE @AllGuardChildren NVARCHAR(MAX) = JSON_QUERY(@AllGuard, '$.children');
                DECLARE @AllOpsIdx INT = (SELECT TOP 1 CAST(c.[key] AS INT) FROM OPENJSON(@AllGuardChildren) c WHERE JSON_VALUE(c.value, '$.text') = N'عملیات');
                IF @AllOpsIdx IS NOT NULL
                BEGIN
                    DECLARE @AllOps NVARCHAR(MAX) = (SELECT TOP 1 CAST(c.value AS NVARCHAR(MAX)) FROM OPENJSON(@AllGuardChildren) c WHERE CAST(c.[key] AS INT) = @AllOpsIdx);
                    DECLARE @AllOpsChildren NVARCHAR(MAX) = JSON_QUERY(@AllOps, '$.children');
                    SELECT @AllOpsChildren = N'[' + ISNULL(STRING_AGG(CAST(c.value AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY CAST(c.[key] AS INT)), N'') + N']'
                    FROM OPENJSON(@AllOpsChildren) c
                    WHERE JSON_VALUE(c.value, '$.text') <> N'ورود/خروج پرسنل/پیمانکار';
                    SET @AllOpsChildren = JSON_MODIFY(@AllOpsChildren, 'append $', JSON_QUERY(@Leaf));
                    SET @AllOps = JSON_MODIFY(@AllOps, '$.children', JSON_QUERY(@AllOpsChildren));
                    SET @AllGuardChildren = JSON_MODIFY(@AllGuardChildren, '$[' + CAST(@AllOpsIdx AS NVARCHAR(10)) + ']', JSON_QUERY(@AllOps));
                    SET @AllGuard = JSON_MODIFY(@AllGuard, '$.children', JSON_QUERY(@AllGuardChildren));
                    SET @HrChildren = JSON_MODIFY(@HrChildren, '$[' + CAST(@AllGuardIdx AS NVARCHAR(10)) + ']', JSON_QUERY(@AllGuard));
                    SET @HrGroup = JSON_MODIFY(@HrGroup, '$.children', JSON_QUERY(@HrChildren));
                    SET @AllContent = JSON_MODIFY(@AllContent, '$[' + CAST(@HrIdx AS NVARCHAR(10)) + ']', JSON_QUERY(@HrGroup));
                END
            END
        END

        DECLARE @AllIds NVARCHAR(MAX) = (SELECT AccessRoleIds FROM [system].[SystemMenu] WHERE Name = N'AllMenus');
        DECLARE @AllRoleNames NVARCHAR(MAX) = (SELECT AccessRoles FROM [system].[SystemMenu] WHERE Name = N'AllMenus');
        SELECT @AllIds = N'[' + STRING_AGG(CAST(x.Id AS NVARCHAR(20)), N',') + N']'
        FROM (
            SELECT TRY_CAST(j.value AS BIGINT) AS Id FROM OPENJSON(ISNULL(@AllIds, N'[]')) j
            UNION
            SELECT r.Id FROM [system].[Role] r WHERE r.Id IN (@GoodsRole, @PersonRole, @ViewRole)
        ) x;
        SELECT @AllRoleNames = N'[' + STRING_AGG(N'"' + STRING_ESCAPE(x.Name, 'json') + N'"', N',') + N']'
        FROM (
            SELECT CAST(j.value AS NVARCHAR(300)) AS Name FROM OPENJSON(ISNULL(@AllRoleNames, N'[]')) j
            UNION
            SELECT r.Name FROM [system].[Role] r WHERE r.Id IN (@GoodsRole, @PersonRole, @ViewRole)
        ) x;

        UPDATE [system].[SystemMenu]
        SET Content = @AllContent, AccessRoleIds = @AllIds, AccessRoles = @AllRoleNames,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = N'AllMenus';
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_GuardPersonIo_RolesAndAccess — WebApp را ری‌استارت کنید ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH

SELECT r.Id, r.Name,
       (SELECT COUNT(*) FROM [system].[RoleAccess] ra WHERE ra.RoleId = r.Id AND ra.ActionAccessType <> 3
           AND ra.Path LIKE N'/panel/hrm/guardpersonio/%') AS PageRows,
       (SELECT COUNT(*) FROM [system].[RoleAccess] ra WHERE ra.RoleId = r.Id AND ra.ActionAccessType = 3
           AND ra.EntityName = N'Entities.App.Hrm.GuardPersonIo') AS ProfileRows
FROM [system].[Role] r
WHERE r.Name IN (N'کارشناسان ورود/خروج کالا (حراست/دژبان)', N'کارشناسان ورود/خروج پرسنل (حراست/دژبان)', N'مشاهده کلی HTS')
ORDER BY r.Id;

SELECT JSON_VALUE(c.value, '$.text') AS OpsChild
FROM [system].[SystemMenu] m
CROSS APPLY OPENJSON(m.Content) g
CROSS APPLY OPENJSON(JSON_QUERY(g.value, '$.children')) ops
CROSS APPLY OPENJSON(JSON_QUERY(ops.value, '$.children')) c
WHERE m.Name = N'HrmSystem'
  AND JSON_VALUE(g.value, '$.text') = N'حراست / دژبان'
  AND JSON_VALUE(ops.value, '$.text') = N'عملیات';
