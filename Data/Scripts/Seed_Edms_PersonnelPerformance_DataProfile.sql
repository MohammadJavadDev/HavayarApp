/*
  Seed_Edms_PersonnelPerformance_DataProfile.sql
  گزارش عملکرد پرسنل EDMS — معادل HTS edms-personnelPerformanceReport (صفحه 245)
  و ویو dbo.Vw_Edms_PersonelPerformance.

  داده از TotalSystem (لینک‌سرور TMS) خوانده می‌شود تا ستون‌ها با سیستم قدیم یکی بماند.
  نفرساعت بررسی‌کننده = ۲۰٪ نفرساعت، همان خروجی MinutesToDuration داخل ویو.
  تاریخ بررسی = InternalCheckedDateShamsi (تاریخ کامنت داخلی، نه CheckedDate مدرک).
  اگر کاربر جاری در Edms_User_Subordinate مدیر باشد، فقط ردیف آماده کننده‌های زیرمجموعه را می‌بیند.

  نام یکتا: Edms_PersonnelPerformance_Report
  صفحه: /panel/edms/document/personnelperformancereport

  Apply:
    sqlcmd -S 172.20.40.42 -d HavayarApp -C -b -I -f 65001 -i Data\Scripts\Seed_Edms_PersonnelPerformance_DataProfile.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-edms-personnel-performance';

    DECLARE @Name NVARCHAR(200) = N'Edms_PersonnelPerformance_Report';
    DECLARE @Title NVARCHAR(200) = N'گزارش عملکرد پرسنل';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.edms.personnelperformancereport';
    DECLARE @EntityPascal NVARCHAR(200) = N'Entities.App.Edms.PersonnelPerformanceReport';
    DECLARE @PagePath NVARCHAR(300) = N'/panel/edms/document/personnelperformancereport';

    DECLARE @RoleName NVARCHAR(200) = N'کارشناسان گزارش عملکرد پرسنل EDMS';
    DECLARE @Role350Name NVARCHAR(200) = N'مشاهده کلی HTS';
    DECLARE @RoleId BIGINT;

    IF NOT EXISTS (SELECT 1 FROM [system].[Role] WHERE Name = @Role350Name)
        THROW 51640, N'نقش مشاهده کلی HTS پیدا نشد.', 1;

    SELECT @RoleId = Id FROM [system].[Role] WHERE Name = @RoleName;
    IF @RoleId IS NULL
    BEGIN
        INSERT INTO [system].[Role]
            (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
             CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime,
             CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@RoleName, @RoleName, 1, @SeedUser, 1, @SeedUser,
             @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET @RoleId = SCOPE_IDENTITY();
    END

    DECLARE @CustomQuery NVARCHAR(MAX) = N'DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
DECLARE @Login NVARCHAR(200) = (SELECT TOP 1 u.Username FROM system.[User] AS u WHERE u.Id = @CuId);
--!--mainsection
SELECT
    v.Document_ID AS [t1_Id],
    v.Project_Code AS [Project_Code],
    v.Project_Name AS [Project_Name],
    v.BeneficiaryUnitTitle AS [BeneficiaryUnitTitle],
    v.Project_ManHours AS [Project_ManHours],
    v.Document_Number AS [Document_Number],
    v.Document_Title AS [Document_Title],
    v.ConsumedManHours AS [ConsumedManHours],
    v.ConsumedManHoursInText AS [ConsumedManHoursInText],
    v.CheckedUserConsumedManHours AS [CheckedUserConsumedManHours],
    v.CheckedUserConsumedManHoursInText AS [CheckedUserConsumedManHoursInText],
    v.Revision AS [Revision],
    v.Document_SendDate_Shamsi AS [Document_SendDate_Shamsi],
    v.InternalCheckedDateShamsi AS [CheckedDate_Shamsi],
    v.PreparedUser AS [PreparedUser],
    v.PreparedUserPersonOrgUnit AS [PreparedUserPersonOrgUnit],
    v.CheckedUser AS [CheckedUser],
    v.CheckerUserPersonOrgUnit AS [CheckerUserPersonOrgUnit]
FROM [TMS].[TotalSystem].[dbo].[Vw_Edms_PersonelPerformance] AS v
LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] AS pu
    ON pu.User_ID = v.PreparedUserId
WHERE NOT EXISTS (
        SELECT 1
        FROM (VALUES
        (N''Amiri.a'', N''Hashemi.mo''),
        (N''Amiri.a'', N''houshmand.a''),
        (N''Amiri.a'', N''Kazemzadeh.h''),
        (N''Amiri.a'', N''Moradi.m''),
        (N''Amiri.a'', N''Sarkhani.s''),
        (N''Amiri.a'', N''yousefi.b''),
        (N''bagheri.m'', N''amirifar.e''),
        (N''bagheri.m'', N''cheraghi.h''),
        (N''bagheri.m'', N''elhami.n''),
        (N''bagheri.m'', N''jafari.f''),
        (N''bagheri.m'', N''javadian.p''),
        (N''bagheri.m'', N''maleki.z''),
        (N''bagheri.m'', N''SalarKia.s''),
        (N''bagheri.m'', N''Samii.m''),
        (N''bagheri.m'', N''Sobhani.sa''),
        (N''bagheri.m'', N''yousefi.b''),
        (N''bagheri.m'', N''Zarei.mohamad''),
        (N''bagheri.m'', N''Zarei.s''),
        (N''dehghan.r'', N''amirifar.e''),
        (N''dehghan.r'', N''Arab.f''),
        (N''dehghan.r'', N''cheraghi.h''),
        (N''dehghan.r'', N''Farokhi.m''),
        (N''dehghan.r'', N''forouzandeh.f''),
        (N''dehghan.r'', N''Hosseini.a''),
        (N''dehghan.r'', N''kiazadeh.s''),
        (N''dehghan.r'', N''Moradi.p''),
        (N''hasani.m'', N''mofakher.s''),
        (N''hasani.m'', N''Sedaghati.m''),
        (N''sabermanesh.s'', N''amirifar.e''),
        (N''sabermanesh.s'', N''cheraghi.h''),
        (N''sabermanesh.s'', N''forouzandeh.f''),
        (N''SalarKia.s'', N''bateni.m''),
        (N''SalarKia.s'', N''Ferasat.r''),
        (N''SalarKia.s'', N''Kamal.m''),
        (N''SalarKia.s'', N''KhanMohammadi.f''),
        (N''SalarKia.s'', N''Mahdavi.m''),
        (N''SalarKia.s'', N''Ranjbarian.a''),
        (N''SalarKia.s'', N''raoufi.a''),
        (N''SalarKia.s'', N''Tabatabaei.z''),
        (N''shevidi.m'', N''Gharibian.r''),
        (N''shevidi.m'', N''maleki.z''),
        (N''shevidi.m'', N''Najafzadeh.m''),
        (N''shevidi.m'', N''ramezani.h''),
        (N''shevidi.m'', N''yousefi.b''),
        (N''shevidi.m'', N''zemestani.a'')
        ) AS s(ManagerUser, SubUser)
        WHERE LOWER(s.ManagerUser) = LOWER(@Login)
    )
    OR EXISTS (
        SELECT 1
        FROM (VALUES
        (N''Amiri.a'', N''Hashemi.mo''),
        (N''Amiri.a'', N''houshmand.a''),
        (N''Amiri.a'', N''Kazemzadeh.h''),
        (N''Amiri.a'', N''Moradi.m''),
        (N''Amiri.a'', N''Sarkhani.s''),
        (N''Amiri.a'', N''yousefi.b''),
        (N''bagheri.m'', N''amirifar.e''),
        (N''bagheri.m'', N''cheraghi.h''),
        (N''bagheri.m'', N''elhami.n''),
        (N''bagheri.m'', N''jafari.f''),
        (N''bagheri.m'', N''javadian.p''),
        (N''bagheri.m'', N''maleki.z''),
        (N''bagheri.m'', N''SalarKia.s''),
        (N''bagheri.m'', N''Samii.m''),
        (N''bagheri.m'', N''Sobhani.sa''),
        (N''bagheri.m'', N''yousefi.b''),
        (N''bagheri.m'', N''Zarei.mohamad''),
        (N''bagheri.m'', N''Zarei.s''),
        (N''dehghan.r'', N''amirifar.e''),
        (N''dehghan.r'', N''Arab.f''),
        (N''dehghan.r'', N''cheraghi.h''),
        (N''dehghan.r'', N''Farokhi.m''),
        (N''dehghan.r'', N''forouzandeh.f''),
        (N''dehghan.r'', N''Hosseini.a''),
        (N''dehghan.r'', N''kiazadeh.s''),
        (N''dehghan.r'', N''Moradi.p''),
        (N''hasani.m'', N''mofakher.s''),
        (N''hasani.m'', N''Sedaghati.m''),
        (N''sabermanesh.s'', N''amirifar.e''),
        (N''sabermanesh.s'', N''cheraghi.h''),
        (N''sabermanesh.s'', N''forouzandeh.f''),
        (N''SalarKia.s'', N''bateni.m''),
        (N''SalarKia.s'', N''Ferasat.r''),
        (N''SalarKia.s'', N''Kamal.m''),
        (N''SalarKia.s'', N''KhanMohammadi.f''),
        (N''SalarKia.s'', N''Mahdavi.m''),
        (N''SalarKia.s'', N''Ranjbarian.a''),
        (N''SalarKia.s'', N''raoufi.a''),
        (N''SalarKia.s'', N''Tabatabaei.z''),
        (N''shevidi.m'', N''Gharibian.r''),
        (N''shevidi.m'', N''maleki.z''),
        (N''shevidi.m'', N''Najafzadeh.m''),
        (N''shevidi.m'', N''ramezani.h''),
        (N''shevidi.m'', N''yousefi.b''),
        (N''shevidi.m'', N''zemestani.a'')
        ) AS s(ManagerUser, SubUser)
        WHERE LOWER(s.ManagerUser) = LOWER(@Login)
          AND LOWER(s.SubUser) = LOWER(pu.Username)
    )';

    DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);

    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[
{"TableName":"Edms.Document","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[v].[Document_ID]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":false,"Sortable":false,"ClassName":""},
{"TableName":"Edms.Project","ColumnName":"Project_Code","DisplayName":"کد پروژه","Alliance":"Project_Code","Address":"[v].[Project_Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.Project","ColumnName":"Project_Name","DisplayName":"نام پروژه","Alliance":"Project_Name","Address":"[v].[Project_Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hrm.OrgUnit","ColumnName":"BeneficiaryUnitTitle","DisplayName":"واحد ذینفع","Alliance":"BeneficiaryUnitTitle","Address":"[v].[BeneficiaryUnitTitle]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":250,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Project","ColumnName":"Project_ManHours","DisplayName":"نفر ساعت پروژه","Alliance":"Project_ManHours","Address":"[v].[Project_ManHours]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.ProjectVpis","ColumnName":"Document_Number","DisplayName":"کد مدرک","Alliance":"Document_Number","Address":"[v].[Document_Number]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.ProjectVpis","ColumnName":"Document_Title","DisplayName":"عنوان مدرک","Alliance":"Document_Title","Address":"[v].[Document_Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":250,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"ConsumedManHours","DisplayName":"نفر ساعت","Alliance":"ConsumedManHours","Address":"[v].[ConsumedManHours]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":"Sum","Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"ConsumedManHoursInText","DisplayName":"نفر ساعت","Alliance":"ConsumedManHoursInText","Address":"[v].[ConsumedManHoursInText]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"CheckedUserConsumedManHours","DisplayName":"نفر ساعت بررسی کننده","Alliance":"CheckedUserConsumedManHours","Address":"[v].[CheckedUserConsumedManHours]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":"Sum","Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"CheckedUserConsumedManHoursInText","DisplayName":"نفر ساعت بررسی کننده","Alliance":"CheckedUserConsumedManHoursInText","Address":"[v].[CheckedUserConsumedManHoursInText]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"Revision","DisplayName":"نسخه","Alliance":"Revision","Address":"[v].[Revision]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"Document_SendDate_Shamsi","DisplayName":"تاریخ ارسال","Alliance":"Document_SendDate_Shamsi","Address":"[v].[Document_SendDate_Shamsi]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.DocumentComment","ColumnName":"CheckedDate_Shamsi","DisplayName":"تاریخ بررسی","Alliance":"CheckedDate_Shamsi","Address":"[v].[InternalCheckedDateShamsi]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"system.User","ColumnName":"PreparedUser","DisplayName":"کاربر آماده کننده","Alliance":"PreparedUser","Address":"[v].[PreparedUser]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hrm.OrgUnit","ColumnName":"PreparedUserPersonOrgUnit","DisplayName":"واحد سازمانی آماده کننده","Alliance":"PreparedUserPersonOrgUnit","Address":"[v].[PreparedUserPersonOrgUnit]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":250,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"system.User","ColumnName":"CheckedUser","DisplayName":"کاربر بررسی کننده","Alliance":"CheckedUser","Address":"[v].[CheckedUser]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Hrm.OrgUnit","ColumnName":"CheckerUserPersonOrgUnit","DisplayName":"واحد سازمانی بررسی کننده","Alliance":"CheckerUserPersonOrgUnit","Address":"[v].[CheckerUserPersonOrgUnit]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":250,"Filterable":true,"Sortable":true,"ClassName":""}
]';
    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @CustomButtons NVARCHAR(MAX) = N'[]';

    IF ISJSON(@ColumnsJson) <> 1 OR ISJSON(@QueryJson) <> 1 OR ISJSON(@ActionOptions) <> 1 OR ISJSON(@EventScripts) <> 1
        THROW 51641, N'JSON نمایه گزارش عملکرد پرسنل معتبر نیست.', 1;

    DECLARE @ProfileId BIGINT;
    SELECT @ProfileId = Id FROM [system].[SavedQuery] WHERE Name = @Name;

    IF @ProfileId IS NOT NULL
    BEGIN
        UPDATE [system].[SavedQuery]
        SET Title = @Title,
            QueryJson = @QueryJson,
            ColumnsJson = @ColumnsJson,
            EntityFullName = @EntityFullName,
            Mode = 1,
            [Type] = 1,
            ActionOptions = @ActionOptions,
            CustomActionButtonsJson = @CustomButtons,
            EventScriptsJson = @EventScripts,
            DiagramJson = N'{}',
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Id = @ProfileId;
    END
    ELSE
    BEGIN
        INSERT INTO [system].[SavedQuery]
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, [Type], EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@Name, @Title, @QueryJson, @ColumnsJson, N'{}', @CustomButtons, @EventScripts,
             1, 1, @EntityFullName, @ActionOptions,
             1, 1, @SeedUser, @SeedUser,
             @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @ProfileId = SCOPE_IDENTITY();
    END

    DECLARE @Roles TABLE (RoleName NVARCHAR(200) NOT NULL PRIMARY KEY);
    INSERT INTO @Roles (RoleName) VALUES
        (N'admin'),
        (N'ShowAllMenus'),
        (@Role350Name),
        (@RoleName);

    DELETE ra
    FROM [system].[RoleAccess] ra
    WHERE ra.ActionAccessType = 3 AND ra.RowId = @ProfileId;

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@ProfileId AS nvarchar(20)), 3, 7, @EntityPascal, @Title, NULL, @ProfileId, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM [system].[Role] r
    INNER JOIN @Roles n ON n.RoleName = r.Name;

    DELETE ra
    FROM [system].[RoleAccess] ra
    INNER JOIN [system].[Role] r ON r.Id = ra.RoleId
    INNER JOIN @Roles n ON n.RoleName = r.Name
    WHERE ra.Path = @PagePath;

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT @PagePath, 1, 1, @EntityPascal, @Title, NULL, NULL, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM [system].[Role] r
    INNER JOIN @Roles n ON n.RoleName = r.Name;

    DECLARE @Uid BIGINT;
    DECLARE @Username NVARCHAR(200);
    DECLARE @RoleIds NVARCHAR(MAX);
    DECLARE @UserRoles NVARCHAR(MAX);
    DECLARE @Assigned INT = 0;

    DECLARE userCur CURSOR LOCAL FAST_FORWARD FOR
        SELECT u.Id, u.Username, u.RoleIds, u.Roles
        FROM [system].[User] u
        INNER JOIN (VALUES
        (N'Ahadpour'),
        (N'akbari.a'),
        (N'Azimzadeh.kh'),
        (N'hashemi.n'),
        (N'hesabres1'),
        (N'khodabandeh.m'),
        (N'Khoshdel.f'),
        (N'Kiani.a'),
        (N'mehrabi.h'),
        (N'mehrsadeghi'),
        (N'Momenirad.s'),
        (N'Naderipour.f'),
        (N'Naghdi.f'),
        (N'Namazi.r'),
        (N'Saadat.a'),
        (N'kadkhodaei.v'),
        (N'bairami.a'),
        (N'khodakarami.f'),
        (N'salimi.s'),
        (N'DCC'),
        (N'sabermanesh.s'),
        (N'bagheri.m'),
        (N'javadian.p'),
        (N'Mosayebi.m'),
        (N'Rajabi.m'),
        (N'Neysari.n'),
        (N'Sepehrnejad.s'),
        (N'Sabzevari.r'),
        (N'azizi.sha'),
        (N'zandi.n'),
        (N'eezi.a')
        ) AS n(Username) ON LOWER(u.Username) = LOWER(n.Username);

    OPEN userCur;
    FETCH NEXT FROM userCur INTO @Uid, @Username, @RoleIds, @UserRoles;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @RoleIds IS NOT NULL AND LTRIM(RTRIM(@RoleIds)) NOT IN (N'', N'[]') AND ISJSON(@RoleIds) <> 1
        BEGIN
            PRINT N'  SKIP invalid RoleIds: ' + @Username;
        END
        ELSE IF @UserRoles IS NOT NULL AND LTRIM(RTRIM(@UserRoles)) NOT IN (N'', N'[]') AND ISJSON(@UserRoles) <> 1
        BEGIN
            PRINT N'  SKIP invalid Roles: ' + @Username;
        END
        ELSE IF EXISTS (
            SELECT 1 FROM OPENJSON(ISNULL(@RoleIds, N'[]')) j
            WHERE TRY_CAST(j.[value] AS BIGINT) = @RoleId
        )
            PRINT N'  already assigned: ' + @Username;
        ELSE
        BEGIN
            UPDATE [system].[User]
            SET RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) IN (N'', N'[]')
                        THEN N'[' + CAST(@RoleId AS nvarchar(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@RoleId AS nvarchar(20)) + N']')
                END,
                Roles = CASE
                    WHEN ISJSON(Roles) = 1 AND EXISTS (
                        SELECT 1 FROM OPENJSON(Roles) j WHERE j.[value] = @RoleName
                    ) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) IN (N'', N'[]')
                        THEN N'["' + @RoleName + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @RoleName + N'"]')
                END,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @Uid;
            SET @Assigned = @Assigned + 1;
            PRINT N'  role assigned: ' + @Username;
        END
        FETCH NEXT FROM userCur INTO @Uid, @Username, @RoleIds, @UserRoles;
    END
    CLOSE userCur;
    DEALLOCATE userCur;
    PRINT N'  newly assigned: ' + CAST(@Assigned AS nvarchar(20));

    DECLARE @MenuId BIGINT;
    DECLARE @Content NVARCHAR(MAX);
    DECLARE @RootIdx INT;
    DECLARE @MidIdx INT;
    DECLARE @RepIdx INT;
    DECLARE @Children NVARCHAR(MAX);
    DECLARE @RepChildren NVARCHAR(MAX);
    DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"گزارش عملکرد پرسنل","icon":"ki-chart-line-up ki-outline","iconColor":"#93C5FD","path":"/panel/edms/document/personnelperformancereport","a_attr":{"href":"/panel/edms/document/personnelperformancereport"},"data":{"iconColor":"#93C5FD","path":"/panel/edms/document/personnelperformancereport"},"children":[]}';

    SELECT TOP 1 @MenuId = Id, @Content = Content
    FROM [system].[SystemMenu]
    WHERE Name = N'AllMenus'
    ORDER BY Id;

    IF @MenuId IS NULL
        PRINT N'  WARN menu missing: AllMenus';
    ELSE IF CHARINDEX(N'"path":"' + @PagePath + N'"', @Content) > 0
        PRINT N'  menu leaf exists: AllMenus';
    ELSE IF ISJSON(@Content) <> 1
        THROW 51642, N'Content منوی AllMenus معتبر نیست.', 1;
    ELSE
    BEGIN
        SET @RootIdx = NULL;
        SELECT TOP 1 @RootIdx = CAST([key] AS INT)
        FROM OPENJSON(@Content)
        WHERE JSON_VALUE([value], '$.text') = N'مهندسی'
        ORDER BY CAST([key] AS INT);

        IF @RootIdx IS NULL
            PRINT N'  WARN root missing: مهندسی';
        ELSE
        BEGIN
            SET @Children = JSON_QUERY(@Content, '$[' + CAST(@RootIdx AS nvarchar(10)) + '].children');
            SET @MidIdx = NULL;
            SELECT TOP 1 @MidIdx = CAST([key] AS INT)
            FROM OPENJSON(@Children)
            WHERE JSON_VALUE([value], '$.text') = N'مهندسی پروژه';

            IF @MidIdx IS NULL
                PRINT N'  WARN mid missing: مهندسی پروژه';
            ELSE
            BEGIN
                SET @RepChildren = JSON_QUERY(@Content, '$[' + CAST(@RootIdx AS nvarchar(10)) + '].children[' + CAST(@MidIdx AS nvarchar(10)) + '].children');
                SET @RepIdx = NULL;
                SELECT TOP 1 @RepIdx = CAST([key] AS INT)
                FROM OPENJSON(@RepChildren)
                WHERE JSON_VALUE([value], '$.text') = N'گزارشات';

                IF @RepIdx IS NULL
                    PRINT N'  WARN folder missing: گزارشات';
                ELSE
                BEGIN
                    SET @Content = JSON_MODIFY(@Content,
                        N'append $[' + CAST(@RootIdx AS nvarchar(10)) + N'].children[' + CAST(@MidIdx AS nvarchar(10)) + N'].children[' + CAST(@RepIdx AS nvarchar(10)) + N'].children',
                        JSON_QUERY(@Leaf));
                    UPDATE [system].[SystemMenu]
                    SET Content = @Content,
                        ModifiedById = 1,
                        ModifiedByName = @SeedUser,
                        ModifiedDateMiladiDateTime = @Now,
                        ModifiedDateShamsiDateTime = @NowShamsi
                    WHERE Id = @MenuId;
                    PRINT N'  menu leaf added: AllMenus';
                END
            END
        END
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
    RETURN;
END CATCH

SELECT q.Id, q.Name, q.Title,
    CASE WHEN q.Title LIKE N'%[یگچپژ]%' THEN N'ok' ELSE N'CHECK' END AS TitleEncoding
FROM [system].[SavedQuery] q
WHERE q.Name = N'Edms_PersonnelPerformance_Report';

SELECT JSON_VALUE(j.[value], '$.Alliance') AS Alliance,
       JSON_VALUE(j.[value], '$.DisplayName') AS DisplayName
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Edms_PersonnelPerformance_Report'
ORDER BY CAST(j.[key] AS int);
