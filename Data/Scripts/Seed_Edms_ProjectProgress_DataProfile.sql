/*
  Seed_Edms_ProjectProgress_DataProfile.sql
  گزارش پیشرفت پروژه — معادل HTS edms-projectProgressReport (صفحه 246)
  و ویو dbo.Vw_Edms_ProjectProgress.

  داده از جداول TotalSystem (لینک‌سرور TMS) خوانده می‌شود، با همان فرمول
  dbo.Vw_Edms_ProjectProgress و dbo.Vw_Edms_DocumentDelay.
  ویو قدیم برای هر ترانسمیتال و هر کامنت کارفرما یک ردیف می‌سازد و SUM/COUNT
  تاخیر را چند برابر می‌کند (مثلاً SP/TM/1400/16). اینجا برای هر مدرک فقط
  یک تاریخ ارسال (اولین) و یک پاسخ کارفرما (اولین) حساب می‌شود.
  پروژه‌ای که مدرکش یک ارسال و یک پاسخ دارد، مثل SP/96/01، با ویو قدیم یکی می‌ماند.

  نام یکتا: Edms_ProjectProgress_Report
  صفحه: /panel/edms/project/projectprogressreport

  Apply:
    sqlcmd -S 172.20.40.42 -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_Edms_ProjectProgress_DataProfile.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-edms-project-progress';

    DECLARE @Name NVARCHAR(200) = N'Edms_ProjectProgress_Report';
    DECLARE @Title NVARCHAR(200) = N'گزارش پیشرفت پروژه';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.edms.projectprogressreport';
    DECLARE @PageEntity NVARCHAR(200) = N'Entities.App.Edms.Project';
    DECLARE @PagePath NVARCHAR(300) = N'/panel/edms/project/projectprogressreport';

    DECLARE @RoleName NVARCHAR(200) = N'گزارش پیشرفت پروژه';
    DECLARE @Role350Name NVARCHAR(200) = N'مشاهده کلی HTS';
    DECLARE @Role488Name NVARCHAR(200) = N'کارشناسان سیستم مهندسی دارای دسترسی های عمومی سیستم مهندسی';
    DECLARE @RoleId BIGINT;

    IF NOT EXISTS (SELECT 1 FROM [system].[Role] WHERE Name = @Role350Name)
        THROW 51650, N'نقش مشاهده کلی HTS پیدا نشد.', 1;
    IF NOT EXISTS (SELECT 1 FROM [system].[Role] WHERE Name = @Role488Name)
        THROW 51651, N'نقش کارشناسان سیستم مهندسی پیدا نشد.', 1;

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

    DECLARE @CustomQuery NVARCHAR(MAX) = N'WITH SendOnce AS (
    SELECT Document_FK, MIN(SendToEmployer_Date) AS SendDate
    FROM [TMS].[TotalSystem].[dbo].[Edms_Transmital_Document]
    GROUP BY Document_FK
),
EmployerOnce AS (
    SELECT Document_FK, MIN(CreatedDate) AS ResponseDate
    FROM [TMS].[TotalSystem].[dbo].[Edms_Document_Comment]
    WHERE Document_Status_FK IN (7, 8, 9, 10)
    GROUP BY Document_FK
),
PrevEmployer AS (
    SELECT d.Project_Vpis_FK, d.Revision, MAX(c.CreatedDate) AS ResponseDate
    FROM [TMS].[TotalSystem].[dbo].[Edms_Document] AS d
    INNER JOIN [TMS].[TotalSystem].[dbo].[Edms_Document_Comment] AS c ON c.Document_FK = d.Document_ID
    WHERE c.Document_Status_FK IN (7, 8, 9, 10)
    GROUP BY d.Project_Vpis_FK, d.Revision
),
DocDelay AS (
    SELECT
        d.Document_ID,
        CASE
            WHEN x.RawHavayar IS NULL OR x.RawHavayar < 0 THEN 0
            ELSE x.RawHavayar
        END AS HavayarDelay,
        CASE
            WHEN x.RawEmployer IS NULL OR x.RawEmployer < 0 THEN 0
            ELSE x.RawEmployer
        END AS EmployerDelay
    FROM [TMS].[TotalSystem].[dbo].[Edms_Document] AS d
    INNER JOIN [TMS].[TotalSystem].[dbo].[Edms_Project_Vpis] AS v ON v.Project_Vpis_ID = d.Project_Vpis_FK
    INNER JOIN [TMS].[TotalSystem].[dbo].[Edms_Project] AS p ON p.Edms_Project_ID = v.Project_FK
    LEFT JOIN SendOnce AS s ON s.Document_FK = d.Document_ID
    LEFT JOIN EmployerOnce AS e ON e.Document_FK = d.Document_ID
    LEFT JOIN PrevEmployer AS prev ON prev.Project_Vpis_FK = d.Project_Vpis_FK AND prev.Revision = d.Revision - 1
    CROSS APPLY (
        SELECT
            CASE
                WHEN d.Revision = 0 THEN DATEDIFF(DAY, IIF(v.First_Issue_Plan IS NULL, v.First_Issue_Baseline, v.First_Issue_Plan), ISNULL(s.SendDate, GETDATE()))
                ELSE DATEDIFF(DAY, prev.ResponseDate, s.SendDate) - ISNULL(p.AllowedHavayar_DelayDay, 14)
            END AS RawHavayar,
            DATEDIFF(DAY, s.SendDate, ISNULL(e.ResponseDate, GETDATE())) - ISNULL(p.AllowedEmployer_DelayDay, 14) AS RawEmployer
    ) AS x
)
--!--mainsection
SELECT
    CAST(p.Edms_Project_ID AS nvarchar(20)) + N''|'' + ISNULL(disc.DocDisipline_Title, N'''') AS [t1_Id],
    p.Edms_Project_ID AS [Project_FK],
    p.Project_Code AS [Project_Code],
    p.Project_Name AS [Project_Name],
    disc.DocDisipline_Title AS [DocDisipline_Title],
    COUNT(DISTINCT v.Project_Vpis_ID) AS [DocumentCount],
    COUNT(DISTINCT CASE WHEN d.Document_ID IS NOT NULL THEN v.Project_Vpis_ID END) AS [Issued],
    COUNT(DISTINCT v.Project_Vpis_ID) - COUNT(DISTINCT CASE WHEN d.Document_ID IS NOT NULL THEN v.Project_Vpis_ID END) AS [NotIssued],
    COUNT(CASE WHEN d.LastStatusId = 8 THEN d.Document_ID END) AS [ApprovedStatus_Count],
    SUM(dd.HavayarDelay) AS [SumHavayarDelay],
    SUM(dd.EmployerDelay) AS [SumEmployer_Delay],
    COUNT(CASE WHEN d.LastStatusId = 13 THEN d.Document_ID END) AS [NotReview_Count],
    COUNT(CASE WHEN dd.HavayarDelay > 0 THEN d.Document_ID END) AS [DocumentDelayCount]
FROM [TMS].[TotalSystem].[dbo].[Edms_Project] AS p
INNER JOIN [TMS].[TotalSystem].[dbo].[Edms_Project_Vpis] AS v ON v.Project_FK = p.Edms_Project_ID
LEFT JOIN [TMS].[TotalSystem].[dbo].[Edms_DocDisipline] AS disc ON disc.DocDisipline_ID = v.DocDisipline_FK
LEFT JOIN [TMS].[TotalSystem].[dbo].[Edms_Document] AS d ON d.Project_Vpis_FK = v.Project_Vpis_ID
LEFT JOIN DocDelay AS dd ON dd.Document_ID = d.Document_ID
GROUP BY p.Edms_Project_ID, p.Project_Code, p.Project_Name, disc.DocDisipline_ID, disc.DocDisipline_Title';

    DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);

    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[
{"TableName":"Edms.Project","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"CAST([v].[Project_FK] AS nvarchar(20)) + N''|'' + ISNULL([v].[DocDisipline_Title], N'''')","SystemTypeName":"String","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":false,"Sortable":false,"ClassName":""},
{"TableName":"Edms.Project","ColumnName":"Project_FK","DisplayName":"شناسه پروژه","Alliance":"Project_FK","Address":"[v].[Project_FK]","SystemTypeName":"Int","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":7,"SortDirection":0,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":false,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Project","ColumnName":"Project_Code","DisplayName":"کد پروژه","Alliance":"Project_Code","Address":"[v].[Project_Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.Project","ColumnName":"Project_Name","DisplayName":"نام پروژه","Alliance":"Project_Name","Address":"[v].[Project_Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":250,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.ProjectVpis","ColumnName":"DocDisipline_Title","DisplayName":"دیسیپلین مدرک","Alliance":"DocDisipline_Title","Address":"[v].[DocDisipline_Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.ProjectVpis","ColumnName":"DocumentCount","DisplayName":"تعداد اسناد","Alliance":"DocumentCount","Address":"[v].[DocumentCount]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"Issued","DisplayName":"صادر شده","Alliance":"Issued","Address":"[v].[Issued]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"NotIssued","DisplayName":"صادر نشده","Alliance":"NotIssued","Address":"[v].[NotIssued]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"ApprovedStatus_Count","DisplayName":"تایید شده کارفرما","Alliance":"ApprovedStatus_Count","Address":"[v].[ApprovedStatus_Count]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"SumHavayarDelay","DisplayName":"مجموع تاخیر هوایار","Alliance":"SumHavayarDelay","Address":"[v].[SumHavayarDelay]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"SumEmployer_Delay","DisplayName":"مجموع تاخیر کارفرما","Alliance":"SumEmployer_Delay","Address":"[v].[SumEmployer_Delay]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"NotReview_Count","DisplayName":"تعداد توقف","Alliance":"NotReview_Count","Address":"[v].[NotReview_Count]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"DocumentDelayCount","DisplayName":"تعداد تاخیر","Alliance":"DocumentDelayCount","Address":"[v].[DocumentDelayCount]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""}
]';
    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @CustomButtons NVARCHAR(MAX) = N'[]';

    IF ISJSON(@ColumnsJson) <> 1 OR ISJSON(@QueryJson) <> 1 OR ISJSON(@ActionOptions) <> 1 OR ISJSON(@EventScripts) <> 1
        THROW 51652, N'JSON نمایه گزارش پیشرفت پروژه معتبر نیست.', 1;

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
        (@Role488Name),
        (@RoleName);

    DELETE ra
    FROM [system].[RoleAccess] ra
    WHERE ra.ActionAccessType = 3 AND ra.RowId = @ProfileId;

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@ProfileId AS nvarchar(20)), 3, 7, @EntityFullName, @Title, NULL, @ProfileId, r.Id,
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
    SELECT @PagePath, 1, 1, @PageEntity, @Title, NULL, NULL, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM [system].[Role] r
    INNER JOIN @Roles n ON n.RoleName = r.Name;

    DECLARE @Role350Id BIGINT = (SELECT Id FROM [system].[Role] WHERE Name = @Role350Name);
    DECLARE @Role488Id BIGINT = (SELECT Id FROM [system].[Role] WHERE Name = @Role488Name);
    DECLARE @Uid BIGINT;
    DECLARE @Username NVARCHAR(200);
    DECLARE @IsActive BIT;
    DECLARE @RoleIds NVARCHAR(MAX);
    DECLARE @UserRoles NVARCHAR(MAX);
    DECLARE @Assigned INT = 0;
    DECLARE @Inactive INT = 0;
    DECLARE @Missing INT = 0;
    DECLARE @Covered INT = 0;

    DECLARE userCur CURSOR LOCAL FAST_FORWARD FOR
        SELECT n.Username
        FROM (VALUES
            (N'azizi.sha'),
            (N'dehrouyeh.m'),
            (N'eezi.a'),
            (N'Hazrati.z'),
            (N'hesabres1'),
            (N'kadkhodaei.v'),
            (N'keshvari.o'),
            (N'khodakarami.f'),
            (N'khodamoradi.a'),
            (N'Kiani.a'),
            (N'kouchehbaghi.i'),
            (N'mehrabi.h'),
            (N'mehrsadeghi'),
            (N'Mosayebi.m'),
            (N'Sabzevari.r'),
            (N'sohani.a'),
            (N'zandi.n')
        ) AS n(Username);

    OPEN userCur;
    FETCH NEXT FROM userCur INTO @Username;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Uid = NULL;
        SET @IsActive = NULL;
        SET @RoleIds = NULL;
        SET @UserRoles = NULL;

        SELECT TOP 1
            @Uid = u.Id,
            @IsActive = u.IsActive,
            @RoleIds = u.RoleIds,
            @UserRoles = u.Roles
        FROM [system].[User] u
        WHERE LOWER(u.Username) = LOWER(@Username);

        IF @Uid IS NULL
        BEGIN
            PRINT N'  MISSING USER (skip): ' + @Username;
            SET @Missing = @Missing + 1;
        END
        ELSE IF ISNULL(@IsActive, 0) = 0
        BEGIN
            PRINT N'  INACTIVE USER (skip): ' + @Username;
            SET @Inactive = @Inactive + 1;
        END
        ELSE IF @RoleIds IS NOT NULL AND LTRIM(RTRIM(@RoleIds)) NOT IN (N'', N'[]') AND ISJSON(@RoleIds) <> 1
            PRINT N'  SKIP invalid RoleIds: ' + @Username;
        ELSE IF @UserRoles IS NOT NULL AND LTRIM(RTRIM(@UserRoles)) NOT IN (N'', N'[]') AND ISJSON(@UserRoles) <> 1
            PRINT N'  SKIP invalid Roles: ' + @Username;
        ELSE IF EXISTS (
            SELECT 1 FROM OPENJSON(ISNULL(@RoleIds, N'[]')) j
            WHERE TRY_CAST(j.[value] AS BIGINT) IN (@Role350Id, @Role488Id, @RoleId)
        )
        BEGIN
            PRINT N'  covered by existing role: ' + @Username;
            SET @Covered = @Covered + 1;
        END
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

        FETCH NEXT FROM userCur INTO @Username;
    END
    CLOSE userCur;
    DEALLOCATE userCur;

    PRINT N'  newly assigned: ' + CAST(@Assigned AS nvarchar(20));
    PRINT N'  covered by group role: ' + CAST(@Covered AS nvarchar(20));
    PRINT N'  inactive skipped: ' + CAST(@Inactive AS nvarchar(20));
    PRINT N'  missing skipped: ' + CAST(@Missing AS nvarchar(20));

    DECLARE @MenuName NVARCHAR(100);
    DECLARE @RootText NVARCHAR(200);
    DECLARE @MidText NVARCHAR(200);
    DECLARE @Leaf NVARCHAR(MAX);
    DECLARE @MenuId BIGINT;
    DECLARE @Content NVARCHAR(MAX);
    DECLARE @RootIdx INT;
    DECLARE @MidIdx INT;
    DECLARE @Children NVARCHAR(MAX);

    DECLARE menu_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT *
        FROM (VALUES
            (N'AllMenus', N'مهندسی', N'مهندسی پروژه',
             N'{"text":"گزارش پیشرفت پروژه","icon":"ki-chart-line-up ki-outline","iconColor":"#93C5FD","path":"/panel/edms/project/projectprogressreport","a_attr":{"href":"/panel/edms/project/projectprogressreport"},"data":{"iconColor":"#93C5FD","path":"/panel/edms/project/projectprogressreport"},"children":[]}'),
            (N'eng', N'پروژه', NULL,
             N'{"text":"گزارش پیشرفت پروژه","icon":"bi-bar-chart-line bi","iconColor":"#1fe57f","path":"/panel/edms/project/projectprogressreport","a_attr":{"href":"/panel/edms/project/projectprogressreport"},"data":{"iconColor":"#1fe57f","path":"/panel/edms/project/projectprogressreport"},"children":[]}')
        ) AS m(MenuName, RootText, MidText, Leaf);
    OPEN menu_cur;
    FETCH NEXT FROM menu_cur INTO @MenuName, @RootText, @MidText, @Leaf;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @MenuId = NULL;
        SET @Content = NULL;
        SELECT TOP 1 @MenuId = Id, @Content = Content
        FROM [system].[SystemMenu]
        WHERE Name = @MenuName
        ORDER BY Id;

        IF @MenuId IS NULL
            PRINT N'  WARN menu missing: ' + @MenuName;
        ELSE IF CHARINDEX(N'"path":"' + @PagePath + N'"', @Content) > 0
            PRINT N'  menu leaf exists: ' + @MenuName;
        ELSE IF ISJSON(@Content) <> 1
            THROW 51653, N'Content منو JSON معتبر نیست', 1;
        ELSE
        BEGIN
            SET @RootIdx = NULL;
            SELECT TOP 1 @RootIdx = CAST([key] AS INT)
            FROM OPENJSON(@Content)
            WHERE JSON_VALUE([value], '$.text') = @RootText
            ORDER BY CAST([key] AS INT);

            IF @RootIdx IS NULL
                PRINT N'  WARN root missing: ' + @MenuName + N' / ' + @RootText;
            ELSE IF @MidText IS NULL
            BEGIN
                SET @Content = JSON_MODIFY(@Content,
                    N'append $[' + CAST(@RootIdx AS nvarchar(10)) + N'].children',
                    JSON_QUERY(@Leaf));
                UPDATE [system].[SystemMenu]
                SET Content = @Content,
                    ModifiedById = 1,
                    ModifiedByName = @SeedUser,
                    ModifiedDateMiladiDateTime = @Now,
                    ModifiedDateShamsiDateTime = @NowShamsi
                WHERE Id = @MenuId;
                PRINT N'  menu leaf added: ' + @MenuName;
            END
            ELSE
            BEGIN
                SET @Children = JSON_QUERY(@Content, '$[' + CAST(@RootIdx AS nvarchar(10)) + '].children');
                SET @MidIdx = NULL;
                SELECT TOP 1 @MidIdx = CAST([key] AS INT)
                FROM OPENJSON(@Children)
                WHERE JSON_VALUE([value], '$.text') = @MidText;

                IF @MidIdx IS NULL
                    PRINT N'  WARN mid missing: ' + @MenuName + N' / ' + @MidText;
                ELSE
                BEGIN
                    SET @Content = JSON_MODIFY(@Content,
                        N'append $[' + CAST(@RootIdx AS nvarchar(10)) + N'].children[' + CAST(@MidIdx AS nvarchar(10)) + N'].children',
                        JSON_QUERY(@Leaf));
                    UPDATE [system].[SystemMenu]
                    SET Content = @Content,
                        ModifiedById = 1,
                        ModifiedByName = @SeedUser,
                        ModifiedDateMiladiDateTime = @Now,
                        ModifiedDateShamsiDateTime = @NowShamsi
                    WHERE Id = @MenuId;
                    PRINT N'  menu leaf added: ' + @MenuName;
                END
            END
        END
        FETCH NEXT FROM menu_cur INTO @MenuName, @RootText, @MidText, @Leaf;
    END
    CLOSE menu_cur;
    DEALLOCATE menu_cur;

    COMMIT TRANSACTION;
    PRINT N'=== DONE: Seed_Edms_ProjectProgress_DataProfile committed ===';
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
WHERE q.Name = N'Edms_ProjectProgress_Report';

SELECT JSON_VALUE(j.[value], '$.Alliance') AS Alliance,
       JSON_VALUE(j.[value], '$.DisplayName') AS DisplayName
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Edms_ProjectProgress_Report'
ORDER BY CAST(j.[key] AS int);
