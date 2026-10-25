/*
================================================================================
Seed_Law_RoleAccess_Menu.sql  —  سیستم حقوقی (HTS System 33 «LawSystem»)
================================================================================
نقش‌ها، RoleAccess (اکشن‌های صفحه + نمایه‌ها) و منوی «سیستم حقوقی».
پیش‌نیاز: Seed_Law_JudicialFile_DataProfiles.sql (نمایه‌ها باید وجود داشته باشند) و migration AddLawJudicialFile.

نگاشت دسترسی HTS (Gnr_PageAction):
  صفحه 392 پرونده حقوقی          · دسترسی کامل(2)      → Law.JudicialFile.Full   (همه پرونده‌ها + همه اکشن‌ها)
                                  · مشاهده(3)           → Law.JudicialFile.View   (فقط پرونده‌های خود)
                                  · جدید(4)/ویرایش(5)/اکسل(10) → Law.JudicialFile.Editor
  صفحه 439 گزارش کلی پرونده‌ها    · دسترسی کامل(2)      → Law.JudicialFileReport.Full
                                  · مشاهده(3)           → Law.JudicialFileReport.View
  «نمایش همه»(7) در کد HTS هرگز چک نمی‌شد؛ نقشی ندارد.

ActionAccessType: View=1، Api=2، DataProfile=3. ActionAccessItemType: List=1، FetchData=2، Save=3، Create=4،
Update=5، Delete=6، (نمایه)=7، سایر=1000.
مسیر گزارش در منو: /panel/law/judicialfile/list?profileId=<Id نمایه گزارش> (MenuBuilder مسیر را دقیقاً با RoleAccess.Path تطبیق می‌دهد).

اجرا:  sqlcmd -S <server> -d HavayarApp -C -b -I -x -f 65001 -i "Data\Scripts\Seed_Law_RoleAccess_Menu.sql"
Idempotent. بعد از اجرا WebApp را ری‌استارت کنید (کاتالوگ دسترسی کنترلرهای جدید + کش نمایه).
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-law-access-menu';
DECLARE @Entity NVARCHAR(200) = N'Entities.App.Law.JudicialFile';

DECLARE @ListProfileId BIGINT = (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'Law_JudicialFile_List');
DECLARE @ReportProfileId BIGINT = (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'Law_JudicialFile_Report');
IF @ListProfileId IS NULL OR @ReportProfileId IS NULL
BEGIN
    RAISERROR(N'نمایه‌های Law_JudicialFile_List / Law_JudicialFile_Report یافت نشد. ابتدا Seed_Law_JudicialFile_DataProfiles.sql را اجرا کنید.', 16, 1);
    RETURN;
END

DECLARE @ReportMenuPath NVARCHAR(300) = N'/panel/law/judicialfile/list?profileId=' + CAST(@ReportProfileId AS NVARCHAR(20));

BEGIN TRY
    BEGIN TRANSACTION;

    -- ---------------------------------------------------------------- roles
    DECLARE @Roles TABLE (Name NVARCHAR(200), Title NVARCHAR(200));
    INSERT INTO @Roles (Name, Title) VALUES
        (N'Law.JudicialFile.Full', N'کارشناسان سیستم حقوقی (دسترسی کامل پرونده حقوقی)'),
        (N'Law.JudicialFile.View', N'مشاهده پرونده حقوقی'),
        (N'Law.JudicialFile.Editor', N'ثبت و ویرایش پرونده حقوقی'),
        (N'Law.JudicialFileReport.Full', N'دسترسی کامل گزارش کلی پرونده ها'),
        (N'Law.JudicialFileReport.View', N'مشاهده گزارش کلی پرونده ها');

    INSERT INTO [system].[Role]
        (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
         CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT r.Name, r.Title, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1
    FROM @Roles r
    WHERE NOT EXISTS (SELECT 1 FROM [system].[Role] x WHERE x.Name = r.Name);

    -- ---------------------------------------------------------------- page access (Path, AType, IType per role)
    DECLARE @Grants TABLE (RoleName NVARCHAR(200), Path NVARCHAR(300), AType INT, IType INT);

    -- اکشن‌های صفحه اصلی که هر نقش دارد
    DECLARE @Page TABLE (Path NVARCHAR(300), AType INT, IType INT, ForFull BIT, ForEditor BIT, ForView BIT, ForReport BIT);
    INSERT INTO @Page (Path, AType, IType, ForFull, ForEditor, ForView, ForReport) VALUES
        (N'/panel/law/judicialfile/list',          1, 1,    1, 1, 1, 1),
        (N'/panel/law/judicialfile/edit',          1, 5,    1, 1, 1, 0),
        (N'/panel/law/judicialfile/new',           1, 4,    1, 1, 0, 0),
        (N'/panel/law/judicialfile/save',          2, 3,    1, 1, 0, 0),
        (N'/panel/law/judicialfile/add',           2, 4,    1, 1, 0, 0),
        (N'/panel/law/judicialfile/update',        2, 5,    1, 1, 0, 0),
        (N'/panel/law/judicialfile/delete',        2, 6,    1, 0, 0, 0),
        (N'/panel/law/judicialfile/fetchdata',     2, 2,    1, 1, 1, 1),
        (N'/panel/law/judicialfile/exporttoexcel', 2, 1000, 1, 1, 0, 0);

    INSERT INTO @Grants SELECT N'Law.JudicialFile.Full',   Path, AType, IType FROM @Page WHERE ForFull = 1;
    INSERT INTO @Grants SELECT N'Law.JudicialFile.Editor', Path, AType, IType FROM @Page WHERE ForEditor = 1;
    INSERT INTO @Grants SELECT N'Law.JudicialFile.View',   Path, AType, IType FROM @Page WHERE ForView = 1;
    -- گزارش: صفحه List + FetchData؛ اکسل فقط برای Full (HTS: Full شامل اکسل بود)
    INSERT INTO @Grants SELECT N'Law.JudicialFileReport.View', Path, AType, IType FROM @Page WHERE ForReport = 1;
    INSERT INTO @Grants SELECT N'Law.JudicialFileReport.Full', Path, AType, IType FROM @Page WHERE ForReport = 1;
    INSERT INTO @Grants VALUES (N'Law.JudicialFileReport.Full', N'/panel/law/judicialfile/exporttoexcel', 2, 1000);
    -- مسیر منوی گزارش (برای نمایش برگ منو)
    INSERT INTO @Grants VALUES (N'Law.JudicialFileReport.View', @ReportMenuPath, 1, 1);
    INSERT INTO @Grants VALUES (N'Law.JudicialFileReport.Full', @ReportMenuPath, 1, 1);

    -- ردیف‌های وابسته (HTS: افزودن/حذف داخل فرم پرونده؛ ضمیمه مدارک): Save/Delete/New هر کنترلر
    DECLARE @Child TABLE (Controller NVARCHAR(100));
    INSERT INTO @Child VALUES (N'judicialfiledemand'), (N'judicialfileparty'), (N'judicialfileresult'), (N'judicialfilecost'),
        (N'judicialfilerelatedwork'), (N'judicialreviewauthority'), (N'judicialimportantdate'), (N'judicialfileattachment');
    INSERT INTO @Grants
    SELECT rl.RoleName, N'/panel/law/' + c.Controller + g.Suffix, g.AType, g.IType
    FROM @Child c
    CROSS JOIN (VALUES (N'/save', 2, 3), (N'/delete', 2, 6), (N'/new', 1, 4)) g(Suffix, AType, IType)
    CROSS JOIN (VALUES (N'Law.JudicialFile.Full'), (N'Law.JudicialFile.Editor')) rl(RoleName);

    DELETE ra
    FROM [system].[RoleAccess] ra
    INNER JOIN [system].[Role] r ON r.Id = ra.RoleId
    WHERE r.Name LIKE N'Law.JudicialFile%'
      AND (ra.ActionAccessType = 3 OR ra.Path LIKE N'/panel/law/%');

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT g.Path, g.AType, g.IType, @Entity, r.Id, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @Grants g
    INNER JOIN [system].[Role] r ON r.Name = g.RoleName;

    -- ---------------------------------------------------------------- data-profile access (ActionAccessType = 3)
    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(p.ProfileId AS NVARCHAR(20)), 3, 7, @Entity, p.Title, p.ProfileId, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM (VALUES
        (@ListProfileId,   N'لیست پرونده های حقوقی', N'Law.JudicialFile.Full'),
        (@ListProfileId,   N'لیست پرونده های حقوقی', N'Law.JudicialFile.Editor'),
        (@ListProfileId,   N'لیست پرونده های حقوقی', N'Law.JudicialFile.View'),
        (@ReportProfileId, N'گزارش کلی پرونده ها',   N'Law.JudicialFileReport.Full'),
        (@ReportProfileId, N'گزارش کلی پرونده ها',   N'Law.JudicialFileReport.View')
    ) p(ProfileId, Title, RoleName)
    INNER JOIN [system].[Role] r ON r.Name = p.RoleName;

    -- ---------------------------------------------------------------- menu «سیستم حقوقی»
    DECLARE @MenuName NVARCHAR(150) = N'LawSystem';
    DECLARE @MenuTitle NVARCHAR(150) = N'سیستم حقوقی';
    DECLARE @MenuContent NVARCHAR(MAX) = N'[
  {"text":"عملیات","icon":"ki-setting-2 ki-outline","iconColor":"#3699ff","path":"","a_attr":{"href":""},"data":{"iconColor":"#3699ff","path":""},"children":[
    {"text":"پرونده حقوقی","icon":"ki-document ki-outline","iconColor":"#d78819","path":"/panel/law/judicialfile/list","a_attr":{"href":"/panel/law/judicialfile/list"},"data":{"iconColor":"#d78819","path":"/panel/law/judicialfile/list"},"children":[]}
  ]},
  {"text":"گزارشات","icon":"ki-chart-simple-3 ki-outline","iconColor":"#e5ca1f","path":"","a_attr":{"href":""},"data":{"iconColor":"#e5ca1f","path":""},"children":[
    {"text":"گزارش کلی پرونده ها","icon":"ki-chart-simple-3 ki-outline","iconColor":"#e5ca1f","path":"' + @ReportMenuPath + N'","a_attr":{"href":"' + @ReportMenuPath + N'"},"data":{"iconColor":"#e5ca1f","path":"' + @ReportMenuPath + N'"},"children":[]}
  ]}
]';

    DECLARE @AccessRoles NVARCHAR(MAX) =
        N'["Law.JudicialFile.Full","Law.JudicialFile.View","Law.JudicialFile.Editor","Law.JudicialFileReport.Full","Law.JudicialFileReport.View"]';
    DECLARE @AccessRoleIds NVARCHAR(MAX) =
        N'[' + (SELECT STRING_AGG(CAST(Id AS NVARCHAR(20)), N',') FROM [system].[Role] WHERE Name LIKE N'Law.JudicialFile%') + N']';

    IF EXISTS (SELECT 1 FROM [system].[SystemMenu] WHERE Name = @MenuName)
        UPDATE [system].[SystemMenu]
        SET Title = @MenuTitle, Content = @MenuContent, AccessRoles = @AccessRoles, AccessRoleIds = @AccessRoleIds,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
        WHERE Name = @MenuName;
    ELSE
        INSERT INTO [system].[SystemMenu]
            (Title, Name, Content, AccessRoles, AccessRoleIds,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@MenuTitle, @MenuName, @MenuContent, @AccessRoles, @AccessRoleIds,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);

    -- ---------------------------------------------------------------- «همه منوها» (AllMenus): گروه «سیستم حقوقی»
    -- برگ‌ها با RoleAccess.Path فیلتر می‌شوند؛ نقش‌های حقوقی به AccessRoleIds اضافه می‌شوند تا کاربر حقوقی این منو را هم ببیند.
    DECLARE @AllMenuGroup NVARCHAR(MAX) = N'{"text":"سیستم حقوقی","icon":"ki-document ki-outline","iconColor":"#e5ca1f","path":"","a_attr":{"href":""},"data":{"iconColor":"#e5ca1f","path":""},"children":' + @MenuContent + N'}';

    IF EXISTS (SELECT 1 FROM [system].[SystemMenu] WHERE Name = N'AllMenus' AND ISJSON(Content) = 1)
    BEGIN
        -- حذف گروه قبلی (در اجرای مجدد) و افزودن مجدد، تا محتوا همیشه با آخرین نسخه یکی باشد
        DECLARE @AllContent NVARCHAR(MAX) = (SELECT Content FROM [system].[SystemMenu] WHERE Name = N'AllMenus');
        DECLARE @OldIdx INT = (SELECT TOP 1 CAST(j.[key] AS INT) FROM OPENJSON(@AllContent) j WHERE JSON_VALUE(j.value, '$.text') = N'سیستم حقوقی');
        IF @OldIdx IS NOT NULL
        BEGIN
            SELECT @AllContent = N'[' + ISNULL(STRING_AGG(CAST(j.value AS NVARCHAR(MAX)), N',') WITHIN GROUP (ORDER BY CAST(j.[key] AS INT)), N'') + N']'
            FROM OPENJSON(@AllContent) j
            WHERE CAST(j.[key] AS INT) <> @OldIdx;
        END
        SET @AllContent = JSON_MODIFY(@AllContent, 'append $', JSON_QUERY(@AllMenuGroup));

        DECLARE @AllIds NVARCHAR(MAX) = (SELECT AccessRoleIds FROM [system].[SystemMenu] WHERE Name = N'AllMenus');
        DECLARE @AllRoleNames NVARCHAR(MAX) = (SELECT AccessRoles FROM [system].[SystemMenu] WHERE Name = N'AllMenus');
        SELECT @AllIds = N'[' + STRING_AGG(CAST(x.Id AS NVARCHAR(20)), N',') + N']'
        FROM (
            SELECT TRY_CAST(j.value AS BIGINT) AS Id FROM OPENJSON(ISNULL(@AllIds, N'[]')) j
            UNION
            SELECT r.Id FROM [system].[Role] r WHERE r.Name LIKE N'Law.JudicialFile%'
        ) x;
        SELECT @AllRoleNames = N'[' + STRING_AGG(N'"' + STRING_ESCAPE(x.Name, 'json') + N'"', N',') + N']'
        FROM (
            SELECT CAST(j.value AS NVARCHAR(300)) AS Name FROM OPENJSON(ISNULL(@AllRoleNames, N'[]')) j
            UNION
            SELECT r.Name FROM [system].[Role] r WHERE r.Name LIKE N'Law.JudicialFile%'
        ) x;

        UPDATE [system].[SystemMenu]
        SET Content = @AllContent, AccessRoleIds = @AllIds, AccessRoles = @AllRoleNames,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = N'AllMenus';
    END

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_Law_RoleAccess_Menu — WebApp را ری‌استارت کنید ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH

-- ---------------------------------------------------------------- verify
SELECT r.Id, r.Name, r.Title,
       (SELECT COUNT(*) FROM [system].[RoleAccess] ra WHERE ra.RoleId = r.Id AND ra.ActionAccessType <> 3) AS PageRows,
       (SELECT COUNT(*) FROM [system].[RoleAccess] ra WHERE ra.RoleId = r.Id AND ra.ActionAccessType = 3) AS ProfileRows
FROM [system].[Role] r
WHERE r.Name LIKE N'Law.JudicialFile%'
ORDER BY r.Name;

SELECT Id, Name, Title, AccessRoleIds, LEFT(Content, 120) AS ContentHead FROM [system].[SystemMenu] WHERE Name = N'LawSystem';
