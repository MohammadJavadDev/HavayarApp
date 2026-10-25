/*
================================================================================
Seed_GuardBasicInfo_RolesAndAccess.sql  —  اطلاعات پایه حراست/دژبان (HTS System 9 «منابع انسانی»، صفحات 421–424)
================================================================================
نقش‌ها: Id = 260000 + شناسه گروه HTS، نام = عنوان گروه (قرارداد پروژه، مثل Seed_PartCardex_Access_Menu.sql).
  260312 «کارشناسان ورود/خروج کالا (حراست/دژبان)»  (HTS گروه 312) — جدید + ویرایش روی هر ۴ صفحه
  260313 «کارشناسان ورود/خروج پرسنل (حراست/دژبان)» (HTS گروه 313) — فقط «جدید» روی پیمانکاران
  260350 «مشاهده کلی HTS» (از قبل هست)                           — مشاهده روی هر ۴ صفحه
«حذف» (PermissionType 6) در HTS به هیچ گروهی داده نشده → به هیچ نقشی داده نمی‌شود (فقط ادمین).
HTS خروجی اکسل برای این صفحات نداشت → exporttoexcel وجود ندارد.

ActionAccessType: View=1، Api=2، DataProfile=3. ActionAccessItemType: List=1، FetchData=2، Create=4، Update=5، Delete=6.

منو: SystemMenu «HrmSystem» (سیستم منابع انسانی) ← حراست / دژبان ← اطلاعات پایه ← کالا، رانندگان، پیمانکاران، اماکن / مناطق
+ همان گروه در «AllMenus».

پیش‌نیاز: Seed_GuardBasicInfo_DataProfiles.sql و migration AddGuardBasicInfo.
UTF-8 BOM.  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_GuardBasicInfo_RolesAndAccess.sql"
Idempotent. بعد از اجرا WebApp را ری‌استارت کنید (کاتالوگ دسترسی کنترلرهای جدید + کش نمایه).
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-guard-basicinfo-access';

IF OBJECT_ID(N'Hrm.GuardPart') IS NULL
BEGIN
    RAISERROR(N'جدول‌های Hrm.Guard* وجود ندارد. ابتدا migration AddGuardBasicInfo را اعمال کنید.', 16, 1);
    RETURN;
END

IF (SELECT COUNT(*) FROM [system].[SavedQuery] WHERE Name IN (N'Hrm_GuardPart_List', N'Hrm_GuardDriver_List', N'Hrm_GuardContractor_List', N'Hrm_GuardLocation_List')) <> 4
BEGIN
    RAISERROR(N'نمایه‌های Hrm_Guard*_List یافت نشد. ابتدا Seed_GuardBasicInfo_DataProfiles.sql را اجرا کنید.', 16, 1);
    RETURN;
END

BEGIN TRY
    BEGIN TRANSACTION;

    -- ---------------------------------------------------------------- roles
    DECLARE @Roles TABLE (WantId BIGINT, Name NVARCHAR(200));
    INSERT INTO @Roles (WantId, Name) VALUES
        (260312, N'کارشناسان ورود/خروج کالا (حراست/دژبان)'),
        (260313, N'کارشناسان ورود/خروج پرسنل (حراست/دژبان)');

    DECLARE @Rid BIGINT, @RName NVARCHAR(200);
    DECLARE rc CURSOR LOCAL FAST_FORWARD FOR SELECT WantId, Name FROM @Roles;
    OPEN rc; FETCH NEXT FROM rc INTO @Rid, @RName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [system].[Role] WHERE Name = @RName)
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM [system].[Role] WHERE Id = @Rid)
            BEGIN
                SET IDENTITY_INSERT [system].[Role] ON;
                INSERT INTO [system].[Role]
                    (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                     CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
                VALUES (@Rid, @RName, @RName, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
                SET IDENTITY_INSERT [system].[Role] OFF;
            END
            ELSE
                INSERT INTO [system].[Role]
                    (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                     CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
                VALUES (@RName, @RName, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
        END
        FETCH NEXT FROM rc INTO @Rid, @RName;
    END
    CLOSE rc; DEALLOCATE rc;

    DECLARE @GoodsRole BIGINT  = (SELECT Id FROM [system].[Role] WHERE Name = N'کارشناسان ورود/خروج کالا (حراست/دژبان)');
    DECLARE @PersonRole BIGINT = (SELECT Id FROM [system].[Role] WHERE Name = N'کارشناسان ورود/خروج پرسنل (حراست/دژبان)');
    DECLARE @ViewRole BIGINT   = (SELECT Id FROM [system].[Role] WHERE Name = N'مشاهده کلی HTS');
    IF @ViewRole IS NULL
    BEGIN
        RAISERROR(N'نقش «مشاهده کلی HTS» یافت نشد.', 16, 1);
    END

    -- ---------------------------------------------------------------- controller access
    DECLARE @Ctl TABLE (Controller NVARCHAR(60), Entity NVARCHAR(200), ProfileName NVARCHAR(100));
    INSERT INTO @Ctl VALUES
        (N'guardpart',       N'Entities.App.Hrm.GuardPart',       N'Hrm_GuardPart_List'),
        (N'guarddriver',     N'Entities.App.Hrm.GuardDriver',     N'Hrm_GuardDriver_List'),
        (N'guardcontractor', N'Entities.App.Hrm.GuardContractor', N'Hrm_GuardContractor_List'),
        (N'guardlocation',   N'Entities.App.Hrm.GuardLocation',   N'Hrm_GuardLocation_List');

    DECLARE @Act TABLE (Suffix NVARCHAR(30), AType INT, IType INT, ForView BIT, ForGoods BIT, ForPersonNew BIT);
    INSERT INTO @Act VALUES
        (N'list',      1, 1, 1, 1, 1),
        (N'fetchdata', 2, 2, 1, 1, 1),
        (N'new',       1, 4, 0, 1, 1),
        (N'add',       2, 4, 0, 1, 1),
        (N'edit',      1, 5, 0, 1, 0),
        (N'update',    2, 5, 0, 1, 0);

    DECLARE @Grants TABLE (RoleId BIGINT, Path NVARCHAR(300), AType INT, IType INT, Entity NVARCHAR(200));
    INSERT INTO @Grants
    SELECT @ViewRole, N'/panel/hrm/' + c.Controller + N'/' + a.Suffix, a.AType, a.IType, c.Entity
    FROM @Ctl c CROSS JOIN @Act a WHERE a.ForView = 1 AND @ViewRole IS NOT NULL;
    INSERT INTO @Grants
    SELECT @GoodsRole, N'/panel/hrm/' + c.Controller + N'/' + a.Suffix, a.AType, a.IType, c.Entity
    FROM @Ctl c CROSS JOIN @Act a WHERE a.ForGoods = 1;
    INSERT INTO @Grants
    SELECT @PersonRole, N'/panel/hrm/' + c.Controller + N'/' + a.Suffix, a.AType, a.IType, c.Entity
    FROM @Ctl c CROSS JOIN @Act a WHERE a.ForPersonNew = 1 AND c.Controller = N'guardcontractor';

    DELETE ra
    FROM [system].[RoleAccess] ra
    WHERE ra.RoleId IN (@GoodsRole, @PersonRole, @ViewRole)
      AND (ra.Path LIKE N'/panel/hrm/guard%' OR (ra.ActionAccessType = 3 AND ra.EntityName LIKE N'Entities.App.Hrm.Guard%'));

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT g.Path, g.AType, g.IType, g.Entity, g.RoleId, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Grants g;

    -- ---------------------------------------------------------------- data-profile access (ActionAccessType = 3)
    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT N'dataProfile_' + CAST(q.Id AS NVARCHAR(20)), 3, 7, c.Entity, q.Title, q.Id, g.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Grants g
    INNER JOIN @Ctl c ON g.Path LIKE N'/panel/hrm/' + c.Controller + N'/%'
    INNER JOIN [system].[SavedQuery] q ON q.Name = c.ProfileName;

    -- ---------------------------------------------------------------- menu «سیستم منابع انسانی»
    DECLARE @MenuName NVARCHAR(150) = N'HrmSystem';
    DECLARE @MenuTitle NVARCHAR(150) = N'سیستم منابع انسانی';
    DECLARE @MenuContent NVARCHAR(MAX) = N'[
  {"text":"حراست / دژبان","icon":"ki-shield-tick ki-outline","iconColor":"#3699ff","path":"","a_attr":{"href":""},"data":{"iconColor":"#3699ff","path":""},"children":[
    {"text":"اطلاعات پایه","icon":"ki-abstract-26 ki-outline","iconColor":"#1fc4e5","path":"","a_attr":{"href":""},"data":{"iconColor":"#1fc4e5","path":""},"children":[
      {"text":"کالا","icon":"ki-parcel ki-outline","iconColor":"#1fc4e5","path":"/panel/hrm/guardpart/list","a_attr":{"href":"/panel/hrm/guardpart/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/hrm/guardpart/list"},"children":[]},
      {"text":"رانندگان","icon":"ki-delivery ki-outline","iconColor":"#1fc4e5","path":"/panel/hrm/guarddriver/list","a_attr":{"href":"/panel/hrm/guarddriver/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/hrm/guarddriver/list"},"children":[]},
      {"text":"پیمانکاران","icon":"ki-people ki-outline","iconColor":"#1fc4e5","path":"/panel/hrm/guardcontractor/list","a_attr":{"href":"/panel/hrm/guardcontractor/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/hrm/guardcontractor/list"},"children":[]},
      {"text":"اماکن / مناطق","icon":"ki-geolocation ki-outline","iconColor":"#1fc4e5","path":"/panel/hrm/guardlocation/list","a_attr":{"href":"/panel/hrm/guardlocation/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/hrm/guardlocation/list"},"children":[]}
    ]}
  ]}
]';

    DECLARE @MenuRoleIds NVARCHAR(MAX) = N'[' + CAST(@GoodsRole AS NVARCHAR(20)) + N',' + CAST(@PersonRole AS NVARCHAR(20))
        + ISNULL(N',' + CAST(@ViewRole AS NVARCHAR(20)), N'') + N']';
    DECLARE @MenuRoleNames NVARCHAR(MAX) = N'["' + STRING_ESCAPE(N'کارشناسان ورود/خروج کالا (حراست/دژبان)', 'json') + N'","'
        + STRING_ESCAPE(N'کارشناسان ورود/خروج پرسنل (حراست/دژبان)', 'json') + N'"'
        + CASE WHEN @ViewRole IS NOT NULL THEN N',"' + STRING_ESCAPE(N'مشاهده کلی HTS', 'json') + N'"' ELSE N'' END + N']';

    IF EXISTS (SELECT 1 FROM [system].[SystemMenu] WHERE Name = @MenuName)
        UPDATE [system].[SystemMenu]
        SET Title = @MenuTitle, Content = @MenuContent, AccessRoles = @MenuRoleNames, AccessRoleIds = @MenuRoleIds,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
        WHERE Name = @MenuName;
    ELSE
        INSERT INTO [system].[SystemMenu]
            (Title, Name, Content, AccessRoles, AccessRoleIds,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@MenuTitle, @MenuName, @MenuContent, @MenuRoleNames, @MenuRoleIds,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);

    -- ---------------------------------------------------------------- «همه منوها» (AllMenus)
    -- گروه «حراست / دژبان» زیر گروه موجود «منابع انسانی» قرار می‌گیرد (در نبود آن، گروه مستقل «سیستم منابع انسانی»).
    DECLARE @GuardGroup NVARCHAR(MAX) = JSON_QUERY(@MenuContent, '$[0]');

    IF EXISTS (SELECT 1 FROM [system].[SystemMenu] WHERE Name = N'AllMenus' AND ISJSON(Content) = 1)
    BEGIN
        DECLARE @AllContent NVARCHAR(MAX) = (SELECT Content FROM [system].[SystemMenu] WHERE Name = N'AllMenus');

        -- حذف گروه مستقل قبلی «سیستم منابع انسانی» (اگر در اجرای قبلی ساخته شده)
        SELECT @AllContent = N'[' + ISNULL(STRING_AGG(CAST(j.value AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY CAST(j.[key] AS INT)), N'') + N']'
        FROM OPENJSON(@AllContent) j
        WHERE JSON_VALUE(j.value, '$.text') <> N'سیستم منابع انسانی';

        DECLARE @HrIdx INT = (SELECT TOP 1 CAST(j.[key] AS INT) FROM OPENJSON(@AllContent) j WHERE JSON_VALUE(j.value, '$.text') = N'منابع انسانی');
        IF @HrIdx IS NOT NULL
        BEGIN
            DECLARE @HrGroup NVARCHAR(MAX) = (SELECT TOP 1 CAST(j.value AS NVARCHAR(MAX)) FROM OPENJSON(@AllContent) j WHERE CAST(j.[key] AS INT) = @HrIdx);
            DECLARE @HrChildren NVARCHAR(MAX) = JSON_QUERY(@HrGroup, '$.children');
            SELECT @HrChildren = N'[' + ISNULL(STRING_AGG(CAST(c.value AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY CAST(c.[key] AS INT)), N'') + N']'
            FROM OPENJSON(@HrChildren) c
            WHERE JSON_VALUE(c.value, '$.text') <> N'حراست / دژبان';
            SET @HrChildren = JSON_MODIFY(@HrChildren, 'append $', JSON_QUERY(@GuardGroup));
            SET @HrGroup = JSON_MODIFY(@HrGroup, '$.children', JSON_QUERY(@HrChildren));
            SET @AllContent = JSON_MODIFY(@AllContent, '$[' + CAST(@HrIdx AS NVARCHAR(10)) + ']', JSON_QUERY(@HrGroup));
        END
        ELSE
        BEGIN
            DECLARE @AllMenuGroup NVARCHAR(MAX) = N'{"text":"سیستم منابع انسانی","icon":"ki-people ki-outline","iconColor":"#3699ff","path":"","a_attr":{"href":""},"data":{"iconColor":"#3699ff","path":""},"children":' + @MenuContent + N'}';
            SET @AllContent = JSON_MODIFY(@AllContent, 'append $', JSON_QUERY(@AllMenuGroup));
        END
        DECLARE @AllIds NVARCHAR(MAX) = (SELECT AccessRoleIds FROM [system].[SystemMenu] WHERE Name = N'AllMenus');
        DECLARE @AllRoleNames NVARCHAR(MAX) = (SELECT AccessRoles FROM [system].[SystemMenu] WHERE Name = N'AllMenus');
        SELECT @AllIds = N'[' + STRING_AGG(CAST(x.Id AS NVARCHAR(20)), N',') + N']'
        FROM (
            SELECT TRY_CAST(j.value AS BIGINT) AS Id FROM OPENJSON(ISNULL(@AllIds, N'[]')) j
            UNION
            SELECT r.Id FROM [system].[Role] r WHERE r.Id IN (@GoodsRole, @PersonRole)
        ) x;
        SELECT @AllRoleNames = N'[' + STRING_AGG(N'"' + STRING_ESCAPE(x.Name, 'json') + N'"', N',') + N']'
        FROM (
            SELECT CAST(j.value AS NVARCHAR(300)) AS Name FROM OPENJSON(ISNULL(@AllRoleNames, N'[]')) j
            UNION
            SELECT r.Name FROM [system].[Role] r WHERE r.Id IN (@GoodsRole, @PersonRole)
        ) x;

        UPDATE [system].[SystemMenu]
        SET Content = @AllContent, AccessRoleIds = @AllIds, AccessRoles = @AllRoleNames,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = N'AllMenus';
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_GuardBasicInfo_RolesAndAccess — WebApp را ری‌استارت کنید ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH

-- ---------------------------------------------------------------- verify
SELECT r.Id, r.Name,
       (SELECT COUNT(*) FROM [system].[RoleAccess] ra WHERE ra.RoleId = r.Id AND ra.ActionAccessType <> 3 AND ra.Path LIKE N'/panel/hrm/guard%') AS PageRows,
       (SELECT COUNT(*) FROM [system].[RoleAccess] ra WHERE ra.RoleId = r.Id AND ra.ActionAccessType = 3 AND ra.EntityName LIKE N'Entities.App.Hrm.Guard%') AS ProfileRows
FROM [system].[Role] r
WHERE r.Name IN (N'کارشناسان ورود/خروج کالا (حراست/دژبان)', N'کارشناسان ورود/خروج پرسنل (حراست/دژبان)', N'مشاهده کلی HTS')
ORDER BY r.Id;

SELECT Id, Name, Title, AccessRoleIds, LEFT(Content, 100) AS ContentHead FROM [system].[SystemMenu] WHERE Name = N'HrmSystem';
