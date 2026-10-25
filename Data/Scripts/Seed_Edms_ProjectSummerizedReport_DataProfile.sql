/*
  Seed_Edms_ProjectSummerizedReport_DataProfile.sql
  گزارش تجمعی پروژه — معادل HTS صفحه 282 / Edms_ProjectSummerized_Report.
  نام یکتا: Edms_ProjectSummerizedReport
  صفحه: /panel/edms/document/projectsummerizedreport

  اعداد از ویوی زنده TotalSystem خوانده می‌شود تا با گرید قدیم یکی باشد.
  دسترسی: گروه‌های HTS 105، 107، 350، 488 (نقش‌های موجود هاویار).

  Apply:
    sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i Data\Scripts\Seed_Edms_ProjectSummerizedReport_DataProfile.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-edms-project-summerized';

    DECLARE @Name NVARCHAR(200) = N'Edms_ProjectSummerizedReport';
    DECLARE @Title NVARCHAR(200) = N'گزارش تجمعی پروژه';
    DECLARE @EntityFullName NVARCHAR(300) = N'entities.app.edms.projectsummerizedreport';
    DECLARE @EntityPascal NVARCHAR(300) = N'Entities.App.Edms.ProjectSummerizedReport';
    DECLARE @PagePath NVARCHAR(300) = N'/panel/edms/document/projectsummerizedreport';
    DECLARE @AnchorPath NVARCHAR(300) = N'/panel/edms/document/cumulativeperformancereport';

    DECLARE @CustomQuery NVARCHAR(MAX) = N'SELECT
    v.Edms_Project_ID AS [t1_Id],
    v.Project_Code AS [t1_ProjectCode],
    v.Project_Name AS [t1_ProjectName],
    v.Project_Manager AS [t1_ProjectManager],
    v.Project_Startdate AS [t1_ProjectStartDate],
    v.Project_Enddate AS [t1_ProjectEndDate],
    v.DocumentCount AS [t1_DocumentCount],
    v.Issued AS [t1_Issued],
    v.NotIssued AS [t1_NotIssued],
    v.Approved AS [t1_Approved],
    v.ApprovedAsnote AS [t1_ApprovedAsnote],
    v.Commented AS [t1_Commented],
    v.Reject AS [t1_Reject],
    v.Hold AS [t1_Hold],
    v.NotReviewed AS [t1_NotReviewed]
FROM [TMS].[TotalSystem].[dbo].[Vw_Edms_ProjectSummerized] AS v';

    DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);

    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[
{"TableName":"dbo.Vw_Edms_ProjectSummerized","ColumnName":"t1_Id","DisplayName":"شناسه پروژه","Alliance":"t1_Id","Address":"[v].[Edms_Project_ID]","SystemTypeName":"Int","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":7,"SortDirection":1,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":false,"Sortable":true,"ClassName":""},
{"TableName":"dbo.Vw_Edms_ProjectSummerized","ColumnName":"t1_ProjectCode","DisplayName":"کد پروژه","Alliance":"t1_ProjectCode","Address":"[v].[Project_Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"dbo.Vw_Edms_ProjectSummerized","ColumnName":"t1_ProjectName","DisplayName":"نام پروژه","Alliance":"t1_ProjectName","Address":"[v].[Project_Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":250,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"dbo.Vw_Edms_ProjectSummerized","ColumnName":"t1_ProjectManager","DisplayName":"مدیر پروژه","Alliance":"t1_ProjectManager","Address":"[v].[Project_Manager]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"dbo.Vw_Edms_ProjectSummerized","ColumnName":"t1_ProjectStartDate","DisplayName":"تاریخ شروع پروژه","Alliance":"t1_ProjectStartDate","Address":"[v].[Project_Startdate]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"dbo.Vw_Edms_ProjectSummerized","ColumnName":"t1_ProjectEndDate","DisplayName":"تاریخ پایان پروژه","Alliance":"t1_ProjectEndDate","Address":"[v].[Project_Enddate]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"dbo.Vw_Edms_ProjectSummerized","ColumnName":"t1_DocumentCount","DisplayName":"تعداد اسناد","Alliance":"t1_DocumentCount","Address":"[v].[DocumentCount]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"dbo.Vw_Edms_ProjectSummerized","ColumnName":"t1_Issued","DisplayName":"صادر شده","Alliance":"t1_Issued","Address":"[v].[Issued]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"dbo.Vw_Edms_ProjectSummerized","ColumnName":"t1_NotIssued","DisplayName":"صادر نشده","Alliance":"t1_NotIssued","Address":"[v].[NotIssued]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"dbo.Vw_Edms_ProjectSummerized","ColumnName":"t1_Approved","DisplayName":"تایید شد","Alliance":"t1_Approved","Address":"[v].[Approved]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"dbo.Vw_Edms_ProjectSummerized","ColumnName":"t1_ApprovedAsnote","DisplayName":"تایید مشروط","Alliance":"t1_ApprovedAsnote","Address":"[v].[ApprovedAsnote]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"dbo.Vw_Edms_ProjectSummerized","ColumnName":"t1_Commented","DisplayName":"کامنت شده","Alliance":"t1_Commented","Address":"[v].[Commented]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"dbo.Vw_Edms_ProjectSummerized","ColumnName":"t1_Reject","DisplayName":"رد شده","Alliance":"t1_Reject","Address":"[v].[Reject]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"dbo.Vw_Edms_ProjectSummerized","ColumnName":"t1_Hold","DisplayName":"معلق","Alliance":"t1_Hold","Address":"[v].[Hold]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"dbo.Vw_Edms_ProjectSummerized","ColumnName":"t1_NotReviewed","DisplayName":"در انتظار پاسخ(کارفرما)","Alliance":"t1_NotReviewed","Address":"[v].[NotReviewed]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""}
]';

    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @CustomButtons NVARCHAR(MAX) = N'[]';

    IF @QueryJson IS NULL OR ISJSON(@QueryJson) <> 1
        THROW 51650, N'QueryJson معتبر نیست.', 1;
    IF ISJSON(@ColumnsJson) <> 1 OR ISJSON(@ActionOptions) <> 1 OR ISJSON(@EventScripts) <> 1
        THROW 51651, N'ColumnsJson / ActionOptions / EventScriptsJson معتبر نیست.', 1;

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
        PRINT N'SavedQuery updated: Id=' + CAST(@ProfileId AS NVARCHAR(20));
    END
    ELSE
    BEGIN
        INSERT INTO [system].[SavedQuery]
        (
            Name, Title, QueryJson, ColumnsJson, DiagramJson,
            CustomActionButtonsJson, EventScriptsJson,
            Mode, [Type], EntityFullName, ActionOptions,
            CreatedById, ModifiedById, CreatedByName, ModifiedByName,
            CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
            IsActive
        )
        VALUES
        (
            @Name, @Title, @QueryJson, @ColumnsJson, N'{}',
            @CustomButtons, @EventScripts,
            1, 1, @EntityFullName, @ActionOptions,
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi,
            1
        );
        SET @ProfileId = SCOPE_IDENTITY();
        PRINT N'SavedQuery inserted: Id=' + CAST(@ProfileId AS NVARCHAR(20));
    END

    DELETE ra
    FROM [system].[RoleAccess] ra
    WHERE ra.RowId = @ProfileId
      AND ra.ActionAccessType = 3
      AND ra.EntityName = @EntityPascal;

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@ProfileId AS NVARCHAR(20)), 3, 7, @EntityPascal, @Title, NULL, @ProfileId, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM [system].[Role] r
    WHERE r.Name IN (
        N'مدیران پروژه در EDMS',
        N'کارشناسان گزارشات جامع سیستم EDMS',
        N'مشاهده کلی HTS',
        N'کارشناسان سیستم مهندسی دارای دسترسی های عمومی سیستم مهندسی'
    );

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT @PagePath, 1, 1, @EntityPascal, @Title, NULL, NULL, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM [system].[Role] r
    WHERE r.Name IN (
        N'مدیران پروژه در EDMS',
        N'کارشناسان گزارشات جامع سیستم EDMS',
        N'مشاهده کلی HTS',
        N'کارشناسان سیستم مهندسی دارای دسترسی های عمومی سیستم مهندسی'
    )
      AND NOT EXISTS (
          SELECT 1 FROM [system].[RoleAccess] ra
          WHERE ra.RoleId = r.Id AND ra.Path = @PagePath AND ra.ActionAccessType = 1
      );

    IF (SELECT COUNT(*) FROM [system].[Role] r WHERE r.Name IN (
            N'مدیران پروژه در EDMS',
            N'کارشناسان گزارشات جامع سیستم EDMS',
            N'مشاهده کلی HTS',
            N'کارشناسان سیستم مهندسی دارای دسترسی های عمومی سیستم مهندسی'
        )) < 4
        THROW 51652, N'یکی از چهار نقش دسترسی گزارش تجمعی پروژه پیدا نشد.', 1;

    ---------------------------------------------------------------------------
    -- منو: برگ گزارش کنار گزارش تجمعی عملکرد
    ---------------------------------------------------------------------------
    DECLARE @MenuId BIGINT;
    DECLARE @Content NVARCHAR(MAX);
    SELECT TOP 1 @MenuId = Id, @Content = Content
    FROM [system].[SystemMenu]
    WHERE Name = N'AllMenus'
    ORDER BY Id;

    IF @MenuId IS NULL
        THROW 51653, N'منوی AllMenus یافت نشد.', 1;

    DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"گزارش تجمعی پروژه","icon":"ki-chart-line ki-outline","iconColor":"#F59E0B","path":"/panel/edms/document/projectsummerizedreport","a_attr":{"href":"/panel/edms/document/projectsummerizedreport"},"data":{"iconColor":"#F59E0B","path":"/panel/edms/document/projectsummerizedreport"},"children":[]}';
    IF ISJSON(@Leaf) <> 1
        THROW 51654, N'JSON برگ منو معتبر نیست.', 1;

    IF CHARINDEX(@PagePath, @Content) > 0
        PRINT N'  menu leaf already present';
    ELSE
    BEGIN
        DECLARE @BestPath NVARCHAR(400) = NULL;
        DECLARE @i INT = 0, @n INT, @j INT, @n1 INT, @k INT, @n2 INT, @m INT, @n3 INT;
        DECLARE @p1 NVARCHAR(200), @p2 NVARCHAR(300), @p3 NVARCHAR(400), @p4 NVARCHAR(500);
        DECLARE @c1 NVARCHAR(MAX), @c2 NVARCHAR(MAX), @c3 NVARCHAR(MAX), @c4 NVARCHAR(MAX);

        SET @n = (SELECT COUNT(*) FROM OPENJSON(@Content));
        WHILE @i < @n
        BEGIN
            SET @p1 = N'$[' + CAST(@i AS NVARCHAR(10)) + N'].children';
            SET @c1 = JSON_QUERY(@Content, @p1);
            IF EXISTS (SELECT 1 FROM OPENJSON(@c1) WHERE JSON_VALUE([value], '$.path') = @AnchorPath)
                SET @BestPath = @p1;

            SET @n1 = ISNULL((SELECT COUNT(*) FROM OPENJSON(@c1)), 0);
            SET @j = 0;
            WHILE @j < @n1
            BEGIN
                SET @p2 = @p1 + N'[' + CAST(@j AS NVARCHAR(10)) + N'].children';
                SET @c2 = JSON_QUERY(@Content, @p2);
                IF EXISTS (SELECT 1 FROM OPENJSON(@c2) WHERE JSON_VALUE([value], '$.path') = @AnchorPath)
                    SET @BestPath = @p2;

                SET @n2 = ISNULL((SELECT COUNT(*) FROM OPENJSON(@c2)), 0);
                SET @k = 0;
                WHILE @k < @n2
                BEGIN
                    SET @p3 = @p2 + N'[' + CAST(@k AS NVARCHAR(10)) + N'].children';
                    SET @c3 = JSON_QUERY(@Content, @p3);
                    IF EXISTS (SELECT 1 FROM OPENJSON(@c3) WHERE JSON_VALUE([value], '$.path') = @AnchorPath)
                        SET @BestPath = @p3;

                    SET @n3 = ISNULL((SELECT COUNT(*) FROM OPENJSON(@c3)), 0);
                    SET @m = 0;
                    WHILE @m < @n3
                    BEGIN
                        SET @p4 = @p3 + N'[' + CAST(@m AS NVARCHAR(10)) + N'].children';
                        SET @c4 = JSON_QUERY(@Content, @p4);
                        IF EXISTS (SELECT 1 FROM OPENJSON(@c4) WHERE JSON_VALUE([value], '$.path') = @AnchorPath)
                            SET @BestPath = @p4;
                        SET @m += 1;
                    END
                    SET @k += 1;
                END
                SET @j += 1;
            END
            SET @i += 1;
        END

        IF @BestPath IS NULL
            THROW 51655, N'شاخه گزارشات (کنار گزارش تجمعی عملکرد) در منو پیدا نشد.', 1;

        SET @Content = JSON_MODIFY(@Content, N'append ' + @BestPath, JSON_QUERY(@Leaf));
        IF @Content IS NULL OR ISJSON(@Content) <> 1
            THROW 51656, N'Content منو بعد از درج معتبر نیست.', 1;

        UPDATE [system].[SystemMenu]
        SET Content = @Content,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;
        PRINT N'  menu added at ' + @BestPath;
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;

SELECT Id, Name, Title, EntityFullName, Mode, [Type]
FROM [system].[SavedQuery]
WHERE Name = N'Edms_ProjectSummerizedReport';

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName,
       JSON_VALUE(j.value, '$.Visible') AS Visible
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Edms_ProjectSummerizedReport';

SELECT r.Name AS RoleName, ra.Path, ra.ActionAccessType, ra.RowId, ra.EntityName
FROM [system].[RoleAccess] ra
INNER JOIN [system].[Role] r ON r.Id = ra.RoleId
WHERE ra.Path = N'/panel/edms/document/projectsummerizedreport'
   OR (ra.ActionAccessType = 3
       AND ra.EntityName = N'Entities.App.Edms.ProjectSummerizedReport'
       AND ra.RowId = (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'Edms_ProjectSummerizedReport'));
