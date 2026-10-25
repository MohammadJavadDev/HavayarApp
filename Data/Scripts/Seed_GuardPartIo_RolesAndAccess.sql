/*
================================================================================
Seed_GuardPartIo_RolesAndAccess.sql  —  ورود و خروج کالا (حراست/دژبان) (HTS System 9، صفحات 426 و 431)
================================================================================
نقش‌ها: Id = 260000 + شناسه گروه HTS، نام = عنوان گروه.
  260312 «کارشناسان ورود/خروج کالا (حراست/دژبان)» — از قبل هست (اطلاعات پایه) — صفحه 426: جدید + ویرایش + حذف
  260330 «کارشناسان مشاهده ورود و خروج کالا (حراست/دژبان)» — جدید — صفحه 426: فقط مشاهده
  260350 «مشاهده کلی HTS» — از قبل هست — مشاهده صفحه 426 و گزارش 431
  260316 «کارشناسان گزارش ورود/خروج کالا (حراست/دژبان)» — جدید — گزارش 431: مشاهده + خروجی اکسل
«تایید و ارسال اعلان» (confirm) با دسترسی ویرایش (Update) گره خورده → فقط نقش 260312.
HTS صفحه 426 اکسل نداشت → exporttoexcel ندارد. صفحه 431 اکسل داشت → دارد.
HTS یک دسترسی مستقیم کاربر (کاربر 661 «حسابرس»، ShowAll روی صفحه 431) داشت که در گزارش استفاده نمی‌شود → منتقل نمی‌شود.

ActionAccessType: View=1، Api=2، DataProfile=3. ActionAccessItemType: List=1، FetchData=2، Create=4، Update=5، Delete=6، Custom=1000.

منو: SystemMenu «HrmSystem» ← حراست / دژبان ← «عملیات» ← ورود و خروج کالا، گزارش ورود/خروج کالا
+ همان گروه در «AllMenus» (زیر «منابع انسانی»).

پیش‌نیاز: Seed_GuardPartIo_DataProfiles.sql و migration AddGuardPartIo.
UTF-8 BOM.  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_GuardPartIo_RolesAndAccess.sql"
Idempotent. بعد از اجرا WebApp را ری‌استارت کنید (کاتالوگ دسترسی کنترلرهای جدید + کش نمایه).
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-guard-partio-access';

IF OBJECT_ID(N'Hrm.GuardPartIo') IS NULL
BEGIN
    RAISERROR(N'جدول‌های Hrm.GuardPartIo وجود ندارد. ابتدا migration AddGuardPartIo را اعمال کنید.', 16, 1);
    RETURN;
END

IF (SELECT COUNT(*) FROM [system].[SavedQuery] WHERE Name IN (N'Hrm_GuardPartIo_List', N'Hrm_GuardPartIoDetail_Report')) <> 2
BEGIN
    RAISERROR(N'نمایه‌های Hrm_GuardPartIo* یافت نشد. ابتدا Seed_GuardPartIo_DataProfiles.sql را اجرا کنید.', 16, 1);
    RETURN;
END

BEGIN TRY
    BEGIN TRANSACTION;

    -- ---------------------------------------------------------------- roles
    DECLARE @Roles TABLE (WantId BIGINT, Name NVARCHAR(200));
    INSERT INTO @Roles (WantId, Name) VALUES
        (260330, N'کارشناسان مشاهده ورود و خروج کالا (حراست/دژبان)'),
        (260316, N'کارشناسان گزارش ورود/خروج کالا (حراست/دژبان)');

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
    DECLARE @ViewIoRole BIGINT = (SELECT Id FROM [system].[Role] WHERE Name = N'کارشناسان مشاهده ورود و خروج کالا (حراست/دژبان)');
    DECLARE @ReportRole BIGINT = (SELECT Id FROM [system].[Role] WHERE Name = N'کارشناسان گزارش ورود/خروج کالا (حراست/دژبان)');
    DECLARE @ViewRole BIGINT   = (SELECT Id FROM [system].[Role] WHERE Name = N'مشاهده کلی HTS');
    IF @GoodsRole IS NULL OR @ViewRole IS NULL
    BEGIN
        RAISERROR(N'نقش «کارشناسان ورود/خروج کالا» یا «مشاهده کلی HTS» یافت نشد. ابتدا Seed_GuardBasicInfo_RolesAndAccess.sql را اجرا کنید.', 16, 1);
    END

    -- ---------------------------------------------------------------- controller access
    DECLARE @Act TABLE (Suffix NVARCHAR(30), AType INT, IType INT, ForView BIT, ForGoods BIT);
    INSERT INTO @Act VALUES
        (N'list',      1, 1, 1, 1),
        (N'fetchdata', 2, 2, 1, 1),
        (N'new',       1, 4, 0, 1),
        (N'add',       2, 4, 0, 1),
        (N'edit',      1, 5, 0, 1),
        (N'update',    2, 5, 0, 1),
        (N'delete',    2, 6, 0, 1),
        (N'confirm',   2, 1000, 0, 1);

    DECLARE @Grants TABLE (RoleId BIGINT, Path NVARCHAR(300), AType INT, IType INT, Entity NVARCHAR(200), ProfileName NVARCHAR(100));
    INSERT INTO @Grants
    SELECT @ViewIoRole, N'/panel/hrm/guardpartio/' + a.Suffix, a.AType, a.IType, N'Entities.App.Hrm.GuardPartIo', N'Hrm_GuardPartIo_List'
    FROM @Act a WHERE a.ForView = 1;
    INSERT INTO @Grants
    SELECT @GoodsRole, N'/panel/hrm/guardpartio/' + a.Suffix, a.AType, a.IType, N'Entities.App.Hrm.GuardPartIo', N'Hrm_GuardPartIo_List'
    FROM @Act a WHERE a.ForGoods = 1;
    INSERT INTO @Grants
    SELECT @ViewRole, N'/panel/hrm/guardpartio/' + a.Suffix, a.AType, a.IType, N'Entities.App.Hrm.GuardPartIo', N'Hrm_GuardPartIo_List'
    FROM @Act a WHERE a.ForView = 1 AND @ViewRole IS NOT NULL;

    INSERT INTO @Grants VALUES
        (@ReportRole, N'/panel/hrm/guardpartiodetail/list',          1, 1, N'Entities.App.Hrm.GuardPartIoDetail', N'Hrm_GuardPartIoDetail_Report'),
        (@ReportRole, N'/panel/hrm/guardpartiodetail/fetchdata',     2, 2, N'Entities.App.Hrm.GuardPartIoDetail', N'Hrm_GuardPartIoDetail_Report'),
        (@ReportRole, N'/panel/hrm/guardpartiodetail/exporttoexcel', 2, 1000, N'Entities.App.Hrm.GuardPartIoDetail', N'Hrm_GuardPartIoDetail_Report');
    INSERT INTO @Grants
    SELECT @ViewRole, Path, AType, IType, Entity, ProfileName
    FROM @Grants WHERE RoleId = @ReportRole;

    DELETE ra
    FROM [system].[RoleAccess] ra
    WHERE ra.RoleId IN (@GoodsRole, @ViewIoRole, @ReportRole, @ViewRole)
      AND (ra.Path LIKE N'/panel/hrm/guardpartio/%' OR ra.Path LIKE N'/panel/hrm/guardpartiodetail/%'
           OR (ra.ActionAccessType = 3 AND ra.Path IN (
                SELECT N'dataProfile_' + CAST(q.Id AS NVARCHAR(20)) FROM [system].[SavedQuery] q
                WHERE q.Name IN (N'Hrm_GuardPartIo_List', N'Hrm_GuardPartIoDetail_Report'))));

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
    SELECT DISTINCT N'dataProfile_' + CAST(q.Id AS NVARCHAR(20)), 3, 7, g.Entity, q.Title, q.Id, g.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Grants g
    INNER JOIN [system].[SavedQuery] q ON q.Name = g.ProfileName;

    -- ---------------------------------------------------------------- menu «سیستم منابع انسانی» — افزودن گروه «عملیات»
    DECLARE @OpsGroup NVARCHAR(MAX) = N'{
  "text":"عملیات","icon":"ki-arrow-right-left ki-outline","iconColor":"#1fc4e5","path":"","a_attr":{"href":""},"data":{"iconColor":"#1fc4e5","path":""},"children":[
    {"text":"ورود و خروج کالا","icon":"ki-parcel ki-outline","iconColor":"#1fc4e5","path":"/panel/hrm/guardpartio/list","a_attr":{"href":"/panel/hrm/guardpartio/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/hrm/guardpartio/list"},"children":[]},
    {"text":"گزارش ورود/خروج کالا","icon":"ki-chart-line ki-outline","iconColor":"#1fc4e5","path":"/panel/hrm/guardpartiodetail/list","a_attr":{"href":"/panel/hrm/guardpartiodetail/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/hrm/guardpartiodetail/list"},"children":[]}
  ]}';

    DECLARE @MenuContent NVARCHAR(MAX) = (SELECT Content FROM [system].[SystemMenu] WHERE Name = N'HrmSystem');
    IF @MenuContent IS NULL OR ISJSON(@MenuContent) <> 1
    BEGIN
        RAISERROR(N'منوی HrmSystem یافت نشد یا JSON نامعتبر است.', 16, 1);
    END

    DECLARE @GuardIdx INT = (SELECT TOP 1 CAST(j.[key] AS INT) FROM OPENJSON(@MenuContent) j WHERE JSON_VALUE(j.value, '$.text') = N'حراست / دژبان');
    DECLARE @GuardGroup NVARCHAR(MAX) = (SELECT TOP 1 CAST(j.value AS NVARCHAR(MAX)) FROM OPENJSON(@MenuContent) j WHERE CAST(j.[key] AS INT) = @GuardIdx);
    DECLARE @GuardChildren NVARCHAR(MAX) = JSON_QUERY(@GuardGroup, '$.children');
    SELECT @GuardChildren = N'[' + ISNULL(STRING_AGG(CAST(c.value AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY CAST(c.[key] AS INT)), N'') + N']'
    FROM OPENJSON(@GuardChildren) c
    WHERE JSON_VALUE(c.value, '$.text') <> N'عملیات';
    SET @GuardChildren = JSON_MODIFY(@GuardChildren, 'append $', JSON_QUERY(@OpsGroup));
    SET @GuardGroup = JSON_MODIFY(@GuardGroup, '$.children', JSON_QUERY(@GuardChildren));
    SET @MenuContent = JSON_MODIFY(@MenuContent, '$[' + CAST(@GuardIdx AS NVARCHAR(10)) + ']', JSON_QUERY(@GuardGroup));

    DECLARE @MenuRoleIds NVARCHAR(MAX) = (SELECT AccessRoleIds FROM [system].[SystemMenu] WHERE Name = N'HrmSystem');
    DECLARE @MenuRoleNames NVARCHAR(MAX) = (SELECT AccessRoles FROM [system].[SystemMenu] WHERE Name = N'HrmSystem');
    SELECT @MenuRoleIds = N'[' + STRING_AGG(CAST(x.Id AS NVARCHAR(20)), N',') + N']'
    FROM (
        SELECT TRY_CAST(j.value AS BIGINT) AS Id FROM OPENJSON(ISNULL(@MenuRoleIds, N'[]')) j
        UNION
        SELECT r.Id FROM [system].[Role] r WHERE r.Id IN (@GoodsRole, @ViewIoRole, @ReportRole, @ViewRole)
    ) x;
    SELECT @MenuRoleNames = N'[' + STRING_AGG(N'"' + STRING_ESCAPE(x.Name, 'json') + N'"', N',') + N']'
    FROM (
        SELECT CAST(j.value AS NVARCHAR(300)) AS Name FROM OPENJSON(ISNULL(@MenuRoleNames, N'[]')) j
        UNION
        SELECT r.Name FROM [system].[Role] r WHERE r.Id IN (@GoodsRole, @ViewIoRole, @ReportRole, @ViewRole)
    ) x;

    UPDATE [system].[SystemMenu]
    SET Content = @MenuContent, AccessRoles = @MenuRoleNames, AccessRoleIds = @MenuRoleIds,
        ModifiedById = 1, ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
    WHERE Name = N'HrmSystem';

    -- ---------------------------------------------------------------- «همه منوها» (AllMenus) — گروه «حراست / دژبان» زیر «منابع انسانی»
    IF EXISTS (SELECT 1 FROM [system].[SystemMenu] WHERE Name = N'AllMenus' AND ISJSON(Content) = 1)
    BEGIN
        DECLARE @AllContent NVARCHAR(MAX) = (SELECT Content FROM [system].[SystemMenu] WHERE Name = N'AllMenus');
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

        DECLARE @AllIds NVARCHAR(MAX) = (SELECT AccessRoleIds FROM [system].[SystemMenu] WHERE Name = N'AllMenus');
        DECLARE @AllRoleNames NVARCHAR(MAX) = (SELECT AccessRoles FROM [system].[SystemMenu] WHERE Name = N'AllMenus');
        SELECT @AllIds = N'[' + STRING_AGG(CAST(x.Id AS NVARCHAR(20)), N',') + N']'
        FROM (
            SELECT TRY_CAST(j.value AS BIGINT) AS Id FROM OPENJSON(ISNULL(@AllIds, N'[]')) j
            UNION
            SELECT r.Id FROM [system].[Role] r WHERE r.Id IN (@ViewIoRole, @ReportRole)
        ) x;
        SELECT @AllRoleNames = N'[' + STRING_AGG(N'"' + STRING_ESCAPE(x.Name, 'json') + N'"', N',') + N']'
        FROM (
            SELECT CAST(j.value AS NVARCHAR(300)) AS Name FROM OPENJSON(ISNULL(@AllRoleNames, N'[]')) j
            UNION
            SELECT r.Name FROM [system].[Role] r WHERE r.Id IN (@ViewIoRole, @ReportRole)
        ) x;

        UPDATE [system].[SystemMenu]
        SET Content = @AllContent, AccessRoleIds = @AllIds, AccessRoles = @AllRoleNames,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = N'AllMenus';
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_GuardPartIo_RolesAndAccess — WebApp را ری‌استارت کنید ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH

-- ---------------------------------------------------------------- verify
SELECT r.Id, r.Name,
       (SELECT COUNT(*) FROM [system].[RoleAccess] ra WHERE ra.RoleId = r.Id AND ra.ActionAccessType <> 3
           AND (ra.Path LIKE N'/panel/hrm/guardpartio/%' OR ra.Path LIKE N'/panel/hrm/guardpartiodetail/%')) AS PageRows,
       (SELECT COUNT(*) FROM [system].[RoleAccess] ra WHERE ra.RoleId = r.Id AND ra.ActionAccessType = 3
           AND ra.EntityName LIKE N'Entities.App.Hrm.GuardPartIo%') AS ProfileRows
FROM [system].[Role] r
WHERE r.Name IN (N'کارشناسان ورود/خروج کالا (حراست/دژبان)', N'کارشناسان مشاهده ورود و خروج کالا (حراست/دژبان)',
                 N'کارشناسان گزارش ورود/خروج کالا (حراست/دژبان)', N'مشاهده کلی HTS')
ORDER BY r.Id;

SELECT JSON_QUERY(j.value, '$.children') AS GuardChildren
FROM [system].[SystemMenu] m CROSS APPLY OPENJSON(m.Content) j
WHERE m.Name = N'HrmSystem' AND JSON_VALUE(j.value, '$.text') = N'حراست / دژبان';
