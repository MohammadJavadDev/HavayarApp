/*
================================================================================
Seed_EnexEntryForm_RolesAndAccess.sql — ورود اضطراری (HTS System 32، صفحات 367 و 376)
================================================================================
نقش‌ها: Id = 260000 + شناسه گروه HTS
  260223 کارشناسان ورود اضطراری — لیست، ویرایش، تایید نهایی، دانلود، گزارش
  260350 مشاهده کلی HTS — لیست، ویرایش، چاپ (نمایه مشاهده)، گزارش
  260545 کارشناسان مشاهده گزارش ورود اضطراری — فقط گزارش
  260541 دریافت کنندگان ایمیل خروج اضطراری بازگشت نشده — فقط عضویت برای جاب یادآوری
جدید و حذف فقط ادمین است و به این نقش‌ها داده نمی‌شود.
منو: حراست / دژبان ← عملیات ← ورود اضطراری ، و ← گزارش‌ها ← گزارش ورود اضطراری
پیش‌نیاز: Seed_EnexEntryForm_DataProfiles.sql و migration AddEnexEntryForm.
UTF-8 BOM. sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_EnexEntryForm_RolesAndAccess.sql"
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-enex-entry-access';

IF OBJECT_ID(N'Hrm.EnexEntryForm') IS NULL
BEGIN
    RAISERROR(N'جدول Hrm.EnexEntryForm وجود ندارد. ابتدا migration AddEnexEntryForm را اعمال کنید.', 16, 1);
    RETURN;
END

IF (SELECT COUNT(*) FROM [system].[SavedQuery] WHERE Name IN (
        N'Hrm_EnexEntryForm_Admin', N'Hrm_EnexEntryForm_List', N'Hrm_EnexEntryForm_View', N'Hrm_EnexEntryForm_Report')) <> 4
BEGIN
    RAISERROR(N'نمایه‌های Hrm_EnexEntryForm* یافت نشد. ابتدا Seed_EnexEntryForm_DataProfiles.sql را اجرا کنید.', 16, 1);
    RETURN;
END

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Roles TABLE (WantId BIGINT, Name NVARCHAR(200));
    INSERT INTO @Roles (WantId, Name) VALUES
        (260223, N'کارشناسان ورود اضطراری'),
        (260545, N'کارشناسان مشاهده گزارش ورود اضطراری'),
        (260541, N'دریافت کنندگان ایمیل خروج اضطراری بازگشت نشده');

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

    DECLARE @SecRole BIGINT = (SELECT Id FROM [system].[Role] WHERE Name = N'کارشناسان ورود اضطراری');
    DECLARE @ViewRole BIGINT = (SELECT Id FROM [system].[Role] WHERE Name = N'مشاهده کلی HTS');
    DECLARE @RepRole BIGINT = (SELECT Id FROM [system].[Role] WHERE Name = N'کارشناسان مشاهده گزارش ورود اضطراری');
    IF @SecRole IS NULL OR @ViewRole IS NULL OR @RepRole IS NULL
    BEGIN
        RAISERROR(N'یکی از نقش‌های ورود اضطراری یا «مشاهده کلی HTS» یافت نشد.', 16, 1);
    END

    DECLARE @Grants TABLE (RoleId BIGINT, Path NVARCHAR(300), AType INT, IType INT, Entity NVARCHAR(200), ProfileName NVARCHAR(100));
    INSERT INTO @Grants VALUES
        (@SecRole, N'/panel/hrm/enexentryform/list', 1, 1, N'Entities.App.Hrm.EnexEntryForm', N'Hrm_EnexEntryForm_List'),
        (@SecRole, N'/panel/hrm/enexentryform/fetchdata', 2, 2, N'Entities.App.Hrm.EnexEntryForm', N'Hrm_EnexEntryForm_List'),
        (@SecRole, N'/panel/hrm/enexentryform/edit', 1, 5, N'Entities.App.Hrm.EnexEntryForm', N'Hrm_EnexEntryForm_List'),
        (@SecRole, N'/panel/hrm/enexentryform/update', 2, 5, N'Entities.App.Hrm.EnexEntryForm', N'Hrm_EnexEntryForm_List'),
        (@SecRole, N'/panel/hrm/enexentryform/finalconfirm', 2, 1000, N'Entities.App.Hrm.EnexEntryForm', N'Hrm_EnexEntryForm_List'),
        (@SecRole, N'/panel/hrm/enexentryform/downloadattachment', 2, 1000, N'Entities.App.Hrm.EnexEntryForm', N'Hrm_EnexEntryForm_List'),
        (@SecRole, N'/panel/hrm/enexentryform/report', 1, 12, N'Entities.App.Hrm.EnexEntryForm', N'Hrm_EnexEntryForm_Report'),
        (@ViewRole, N'/panel/hrm/enexentryform/list', 1, 1, N'Entities.App.Hrm.EnexEntryForm', N'Hrm_EnexEntryForm_View'),
        (@ViewRole, N'/panel/hrm/enexentryform/fetchdata', 2, 2, N'Entities.App.Hrm.EnexEntryForm', N'Hrm_EnexEntryForm_View'),
        (@ViewRole, N'/panel/hrm/enexentryform/edit', 1, 5, N'Entities.App.Hrm.EnexEntryForm', N'Hrm_EnexEntryForm_View'),
        (@ViewRole, N'/panel/hrm/enexentryform/update', 2, 5, N'Entities.App.Hrm.EnexEntryForm', N'Hrm_EnexEntryForm_View'),
        (@ViewRole, N'/panel/hrm/enexentryform/report', 1, 12, N'Entities.App.Hrm.EnexEntryForm', N'Hrm_EnexEntryForm_Report'),
        (@RepRole, N'/panel/hrm/enexentryform/report', 1, 12, N'Entities.App.Hrm.EnexEntryForm', N'Hrm_EnexEntryForm_Report');

    DELETE ra
    FROM [system].[RoleAccess] ra
    WHERE ra.RoleId IN (@SecRole, @ViewRole, @RepRole)
      AND (ra.Path LIKE N'/panel/hrm/enexentryform/%'
           OR (ra.ActionAccessType = 3 AND ra.Path IN (
                SELECT N'dataProfile_' + CAST(q.Id AS NVARCHAR(20)) FROM [system].[SavedQuery] q
                WHERE q.Name LIKE N'Hrm_EnexEntryForm%')));

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT g.Path, g.AType, g.IType, g.Entity, g.RoleId, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Grants g;

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT N'dataProfile_' + CAST(q.Id AS NVARCHAR(20)), 3, 7, g.Entity, q.Title, q.Id, g.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Grants g
    INNER JOIN [system].[SavedQuery] q ON q.Name = g.ProfileName;

    /* منو — آیتم را به گروه موجود اضافه می‌کند و آیتم‌های قبلی عملیات را پاک نمی‌کند */
    DECLARE @EntryItem NVARCHAR(MAX) = N'{"text":"ورود اضطراری","icon":"ki-parcel ki-outline","iconColor":"#1fc4e5","path":"/panel/hrm/enexentryform/list","a_attr":{"href":"/panel/hrm/enexentryform/list"},"data":{"iconColor":"#1fc4e5","path":"/panel/hrm/enexentryform/list"},"children":[]}';
    DECLARE @ReportItem NVARCHAR(MAX) = N'{"text":"گزارش ورود اضطراری","icon":"ki-chart-line ki-outline","iconColor":"#1fc4e5","path":"/panel/hrm/enexentryform/report","a_attr":{"href":"/panel/hrm/enexentryform/report"},"data":{"iconColor":"#1fc4e5","path":"/panel/hrm/enexentryform/report"},"children":[]}';

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

    DECLARE @Pair TABLE (GroupName NVARCHAR(100), ItemText NVARCHAR(100), ItemJson NVARCHAR(MAX));
    INSERT INTO @Pair VALUES
        (N'عملیات', N'ورود اضطراری', @EntryItem),
        (N'گزارش‌ها', N'گزارش ورود اضطراری', @ReportItem);

    DECLARE @GName NVARCHAR(100), @IText NVARCHAR(100), @IJson NVARCHAR(MAX);
    DECLARE @GIdx INT, @NewGroup NVARCHAR(MAX), @G NVARCHAR(MAX), @Kids NVARCHAR(MAX);
    DECLARE pc CURSOR LOCAL FAST_FORWARD FOR SELECT GroupName, ItemText, ItemJson FROM @Pair;
    OPEN pc; FETCH NEXT FROM pc INTO @GName, @IText, @IJson;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @GIdx = (SELECT TOP 1 CAST(c.[key] AS INT) FROM OPENJSON(@GuardChildren) c WHERE JSON_VALUE(c.value, '$.text') = @GName);
        IF @GIdx IS NULL
        BEGIN
            SET @NewGroup = N'{"text":"' + @GName + N'","icon":"ki-arrow-right-left ki-outline","iconColor":"#1fc4e5","path":"","a_attr":{"href":""},"data":{"iconColor":"#1fc4e5","path":""},"children":[]}';
            SET @NewGroup = JSON_MODIFY(@NewGroup, 'append $.children', JSON_QUERY(@IJson));
            SET @GuardChildren = JSON_MODIFY(@GuardChildren, 'append $', JSON_QUERY(@NewGroup));
        END
        ELSE
        BEGIN
            SET @G = (SELECT TOP 1 CAST(c.value AS NVARCHAR(MAX)) FROM OPENJSON(@GuardChildren) c WHERE CAST(c.[key] AS INT) = @GIdx);
            SET @Kids = JSON_QUERY(@G, '$.children');
            SELECT @Kids = N'[' + ISNULL(STRING_AGG(CAST(c.value AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY CAST(c.[key] AS INT)), N'') + N']'
            FROM OPENJSON(ISNULL(@Kids, N'[]')) c
            WHERE JSON_VALUE(c.value, '$.text') <> @IText;
            SET @Kids = JSON_MODIFY(@Kids, 'append $', JSON_QUERY(@IJson));
            SET @G = JSON_MODIFY(@G, '$.children', JSON_QUERY(@Kids));
            SET @GuardChildren = JSON_MODIFY(@GuardChildren, '$[' + CAST(@GIdx AS NVARCHAR(10)) + ']', JSON_QUERY(@G));
        END
        FETCH NEXT FROM pc INTO @GName, @IText, @IJson;
    END
    CLOSE pc; DEALLOCATE pc;

    SET @GuardGroup = JSON_MODIFY(@GuardGroup, '$.children', JSON_QUERY(@GuardChildren));
    SET @MenuContent = JSON_MODIFY(@MenuContent, '$[' + CAST(@GuardIdx AS NVARCHAR(10)) + ']', JSON_QUERY(@GuardGroup));

    DECLARE @MenuRoleIds NVARCHAR(MAX) = (SELECT AccessRoleIds FROM [system].[SystemMenu] WHERE Name = N'HrmSystem');
    DECLARE @MenuRoleNames NVARCHAR(MAX) = (SELECT AccessRoles FROM [system].[SystemMenu] WHERE Name = N'HrmSystem');
    SELECT @MenuRoleIds = N'[' + STRING_AGG(CAST(x.Id AS NVARCHAR(20)), N',') + N']'
    FROM (
        SELECT TRY_CAST(j.value AS BIGINT) AS Id FROM OPENJSON(ISNULL(@MenuRoleIds, N'[]')) j
        UNION
        SELECT r.Id FROM [system].[Role] r WHERE r.Id IN (@SecRole, @ViewRole, @RepRole)
    ) x;
    SELECT @MenuRoleNames = N'[' + STRING_AGG(N'"' + STRING_ESCAPE(x.Name, 'json') + N'"', N',') + N']'
    FROM (
        SELECT CAST(j.value AS NVARCHAR(300)) AS Name FROM OPENJSON(ISNULL(@MenuRoleNames, N'[]')) j
        UNION
        SELECT r.Name FROM [system].[Role] r WHERE r.Id IN (@SecRole, @ViewRole, @RepRole)
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
            SELECT @HrChildren = N'[' + ISNULL(STRING_AGG(CAST(c.value AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY CAST(c.[key] AS INT)), N'') + N']'
            FROM OPENJSON(ISNULL(@HrChildren, N'[]')) c
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
            SELECT r.Id FROM [system].[Role] r WHERE r.Id IN (@SecRole, @RepRole)
        ) x;
        SELECT @AllRoleNames = N'[' + STRING_AGG(N'"' + STRING_ESCAPE(x.Name, 'json') + N'"', N',') + N']'
        FROM (
            SELECT CAST(j.value AS NVARCHAR(300)) AS Name FROM OPENJSON(ISNULL(@AllRoleNames, N'[]')) j
            UNION
            SELECT r.Name FROM [system].[Role] r WHERE r.Id IN (@SecRole, @RepRole)
        ) x;

        UPDATE [system].[SystemMenu]
        SET Content = @AllContent, AccessRoleIds = @AllIds, AccessRoles = @AllRoleNames,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = N'AllMenus';
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_EnexEntryForm_RolesAndAccess — WebApp را ری‌استارت کنید ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH

SELECT r.Id, r.Name,
       (SELECT COUNT(*) FROM [system].[RoleAccess] ra WHERE ra.RoleId = r.Id AND ra.ActionAccessType <> 3 AND ra.Path LIKE N'/panel/hrm/enexentryform/%') AS PageRows,
       (SELECT COUNT(*) FROM [system].[RoleAccess] ra WHERE ra.RoleId = r.Id AND ra.ActionAccessType = 3 AND ra.EntityName = N'Entities.App.Hrm.EnexEntryForm') AS ProfileRows
FROM [system].[Role] r
WHERE r.Name IN (N'کارشناسان ورود اضطراری', N'مشاهده کلی HTS', N'کارشناسان مشاهده گزارش ورود اضطراری', N'دریافت کنندگان ایمیل خروج اضطراری بازگشت نشده')
ORDER BY r.Id;
