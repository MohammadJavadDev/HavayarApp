/*
  همه نمایه‌های Edms.Document یک مجموعه ستون، با ترتیب گرید مدارک:
  # (ستون ردیف گرید) سپس
  بررسی شده هست نیست، کد پروژه، نام پروژه، کد مدرک، عنوان مدرک،
  نسخه(رویزن)، وضعیت مدرک، نسخه آخر هست نیست، هدف از نشر، دیسیپلین،
  عنوان فایل، توضیحات، تاریخ شمسی ایجاد، تاریخ شمسی ویرایش،
  کاربر آماده کننده، کاربر بررسی کننده، کاربر تایید کننده، شناسه.
  شناسه پروژه مخفی می‌ماند تا دکمه‌هایی که projectId می‌خوانند خالی نشوند.
  فیلتر WHERE هر نمایه دست نمی‌خورد.
  UTF-8 with BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'patch-edms-document-unified-columns';
    DECLARE @Cols NVARCHAR(MAX) = N'[
{"TableName":"Edms.Document","ColumnName":"IsChecked","DisplayName":"بررسی شده هست نیست","Alliance":"IsChecked","Address":"CASE WHEN [t1].[RevieweMiladiDateTime] IS NOT NULL THEN 1 ELSE 0 END","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Project","ColumnName":"t2_Code","DisplayName":"کد پروژه","Alliance":"t2_Code","Address":"[t2].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.Project","ColumnName":"t2_Name","DisplayName":"نام پروژه","Alliance":"t2_Name","Address":"[t2].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.ProjectVpis","ColumnName":"t3_Code","DisplayName":"کد مدرک","Alliance":"t3_Code","Address":"[t3].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.ProjectVpis","ColumnName":"t3_Title","DisplayName":"عنوان مدرک","Alliance":"t3_Title","Address":"[t3].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":280,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"t1_Revision","DisplayName":"نسخه(رویزن)","Alliance":"t1_Revision","Address":"[t1].[Revision]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"t1_Status","DisplayName":"وضعیت مدرک","Alliance":"t1_Status","Address":"[t1].[Status]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Edms.Enums.DocumentStatusEnums"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":170,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"t1_StatusId","DisplayName":"کد وضعیت","Alliance":"t1_StatusId","Address":"[t1].[Status]","SystemTypeName":"Int","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":70,"Filterable":false,"Sortable":false,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"IsLatest","DisplayName":"نسخه آخر هست نیست","Alliance":"IsLatest","Address":"[t1].[IsLatest]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"t1_GoalOfProduction","DisplayName":"هدف از نشر","Alliance":"t1_GoalOfProduction","Address":"[t1].[GoalOfProduction]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Edms.Enums.DocumentGoalOfProductionEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.ProjectVpis","ColumnName":"t3_Displaying","DisplayName":"دیسیپلین","Alliance":"t3_Displaying","Address":"[t3].[Displaying]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Edms.Enums.ProjectVpisDisplayingEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"dbo.FileEntity","ColumnName":"t4_OriginalName","DisplayName":"عنوان فایل","Alliance":"t4_OriginalName","Address":"[t4].[OriginalName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":260,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.Document","ColumnName":"t1_Comment","DisplayName":"توضیحات","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"t1_CreatedOnShamsiDateTime","DisplayName":"تاریخ شمسی ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":1,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"t1_ModifiedDateShamsiDateTime","DisplayName":"تاریخ شمسی ویرایش","Alliance":"t1_ModifiedDateShamsiDateTime","Address":"[t1].[ModifiedDateShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"system.User","ColumnName":"t6_Name","DisplayName":"کاربر آماده کننده","Alliance":"t6_Name","Address":"[t6].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"system.User","ColumnName":"t5_Name","DisplayName":"کاربر بررسی کننده","Alliance":"t5_Name","Address":"[t5].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"system.User","ColumnName":"t7_Name","DisplayName":"کاربر تایید کننده","Alliance":"t7_Name","Address":"[t7].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"t1_ProjectId","DisplayName":"شناسه پروژه","Alliance":"t1_ProjectId","Address":"[t1].[ProjectId]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":false,"Sortable":false,"ClassName":""}
]';

    DECLARE @Select NVARCHAR(MAX) = N'SELECT
    CAST(CASE WHEN [t1].[RevieweMiladiDateTime] IS NOT NULL THEN 1 ELSE 0 END AS bit) AS [IsChecked],
    [t2].[Code] AS [t2_Code],
    [t2].[Name] AS [t2_Name],
    [t3].[Code] AS [t3_Code],
    [t3].[Title] AS [t3_Title],
    [t1].[Revision] AS [t1_Revision],
    [t1].[Status] AS [t1_Status],
    [t1].[Status] AS [t1_StatusId],
    [t1].[IsLatest] AS [IsLatest],
    [t1].[GoalOfProduction] AS [t1_GoalOfProduction],
    [t3].[Displaying] AS [t3_Displaying],
    [t4].[OriginalName] AS [t4_OriginalName],
    [t1].[Comment] AS [t1_Comment],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],
    [t6].[Name] AS [t6_Name],
    [t5].[Name] AS [t5_Name],
    [t7].[Name] AS [t7_Name],
    [t1].[Id] AS [t1_Id],
    [t1].[ProjectId] AS [t1_ProjectId]
';

    DECLARE @JoinsLeft NVARCHAR(MAX) = N'FROM [Edms].[Document] AS [t1]
LEFT JOIN [Edms].[Project] AS [t2] ON [t1].[ProjectId] = [t2].[Id]
LEFT JOIN [Edms].[ProjectVpis] AS [t3] ON [t1].[DocumentVpisId] = [t3].[Id]
LEFT JOIN [dbo].[FileEntity] AS [t4] ON [t4].[Id] = [t1].[MainFileId]
LEFT JOIN [system].[User] AS [t5] ON [t1].[ReviewerId] = [t5].[Id]
LEFT JOIN [system].[User] AS [t6] ON [t3].[ProducerId] = [t6].[Id]
LEFT JOIN [system].[User] AS [t7] ON [t1].[ApproverId] = [t7].[Id]
';

    DECLARE @JoinsInner NVARCHAR(MAX) = N'FROM [Edms].[Document] AS [t1]
INNER JOIN [Edms].[ProjectVpis] AS [t3] ON [t1].[DocumentVpisId] = [t3].[Id]
INNER JOIN [Edms].[Project] AS [t2] ON [t1].[ProjectId] = [t2].[Id]
LEFT JOIN [dbo].[FileEntity] AS [t4] ON [t4].[Id] = [t1].[MainFileId]
LEFT JOIN [system].[User] AS [t5] ON [t1].[ReviewerId] = [t5].[Id]
LEFT JOIN [system].[User] AS [t6] ON [t3].[ProducerId] = [t6].[Id]
LEFT JOIN [system].[User] AS [t7] ON [t1].[ApproverId] = [t7].[Id]
';

    DECLARE @Apply NVARCHAR(MAX) = N'OUTER APPLY (
    SELECT TOP (1) c.CreatedById
    FROM [Edms].[DocumentComment] AS c
    WHERE c.DocumentId = [t1].[Id]
    ORDER BY c.Id DESC
) AS lastComment
';

    IF ISJSON(@Cols) <> 1
        THROW 51660, N'JSON ستون‌های یکسان مدارک معتبر نیست.', 1;

    IF (SELECT COUNT(*) FROM system.SavedQuery WHERE EntityFullName = N'entities.app.edms.document') <> 8
        THROW 51661, N'تعداد نمایه‌های Edms.Document برابر ۸ نیست.', 1;

    DECLARE @Name NVARCHAR(100);
    DECLARE @Q NVARCHAR(MAX);
    DECLARE @WhereAt INT;
    DECLARE @Scan INT;
    DECLARE @TailWhere NVARCHAR(MAX);
    DECLARE @Joins NVARCHAR(MAX);
    DECLARE @Extra NVARCHAR(MAX);
    DECLARE @NewSql NVARCHAR(MAX);
    DECLARE @Qj NVARCHAR(MAX);
    DECLARE @Probe NVARCHAR(MAX);
    DECLARE @Updated INT = 0;

    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name
        FROM system.SavedQuery
        WHERE EntityFullName = N'entities.app.edms.document'
        ORDER BY Id;

    OPEN cur;
    FETCH NEXT FROM cur INTO @Name;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @Q = JSON_VALUE(QueryJson, N'$.CustomQuery')
        FROM system.SavedQuery
        WHERE Name = @Name;

        IF @Q IS NULL OR CHARINDEX(N'FROM [Edms].[Document]', @Q) = 0
            THROW 51662, N'کوئری نمایه مدرک پیدا نشد.', 1;

        SET @WhereAt = 0;
        SET @Scan = CHARINDEX(N'WHERE ', @Q);
        WHILE @Scan > 0
        BEGIN
            SET @WhereAt = @Scan;
            SET @Scan = CHARINDEX(N'WHERE ', @Q, @Scan + 6);
        END

        IF @WhereAt = 0
            THROW 51663, N'WHERE نمایه مدرک پیدا نشد.', 1;

        SET @TailWhere = SUBSTRING(@Q, @WhereAt, (DATALENGTH(@Q) / 2) - @WhereAt + 1);

        IF CHARINDEX(N'INNER JOIN [Edms].[ProjectVpis]', @Q) > 0
            SET @Joins = @JoinsInner;
        ELSE
            SET @Joins = @JoinsLeft;

        IF CHARINDEX(N'lastComment', @Q) > 0
            SET @Extra = @Apply;
        ELSE
            SET @Extra = N'';

        SET @NewSql = N'DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
--!--mainsection
' + @Select + @Joins + @Extra + @TailWhere;

        SET @Probe = N'DECLARE @CurrentUserId NVARCHAR(30) = N''1'';
DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
IF EXISTS (
    SELECT 1
    FROM ('
            + SUBSTRING(@NewSql, CHARINDEX(N'SELECT', @NewSql), 200000)
            + N') AS probe
    WHERE 1 = 0
)
    SELECT 1 AS ProbeOk;';
        EXEC sys.sp_executesql @Probe;

        SET @Qj = (
            SELECT
                JSON_QUERY(N'[]') AS Tables,
                JSON_QUERY(N'[]') AS Relations,
                JSON_QUERY(N'[]') AS Filters,
                JSON_QUERY(N'[]') AS CustomConditions,
                @NewSql AS CustomQuery,
                JSON_QUERY(N'[]') AS [Parameters],
                CAST(NULL AS NVARCHAR(10)) AS Selects
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES
        );

        DECLARE @StoredQuery NVARCHAR(MAX);
        SELECT @StoredQuery = CustomQuery
        FROM OPENJSON(@Qj) WITH (CustomQuery NVARCHAR(MAX) N'$.CustomQuery');

        IF ISJSON(@Qj) <> 1 OR @StoredQuery IS NULL OR CHARINDEX(@TailWhere, @StoredQuery) = 0
            THROW 51664, N'بازسازی کوئری نمایه مدرک فیلتر را حفظ نکرد.', 1;

        UPDATE system.SavedQuery
        SET ColumnsJson = @Cols,
            QueryJson = @Qj,
            Mode = 1,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = @Name;

        SET @Updated += 1;
        FETCH NEXT FROM cur INTO @Name;
    END

    CLOSE cur;
    DEALLOCATE cur;

    IF @Updated <> 8
        THROW 51665, N'هر ۸ نمایه مدرک به‌روز نشد.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF CURSOR_STATUS('local', 'cur') > 0
        CLOSE cur;
    IF CURSOR_STATUS('local', 'cur') >= -1
        DEALLOCATE cur;
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
    RETURN;
END CATCH;

SELECT q.Name,
       ROW_NUMBER() OVER (PARTITION BY q.Id ORDER BY CAST(j.[key] AS int)) AS Ord,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName,
       JSON_VALUE(j.value, '$.Visible') AS Visible
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.EntityFullName = N'entities.app.edms.document'
  AND JSON_VALUE(j.value, '$.Visible') = N'true'
ORDER BY q.Name, CAST(j.[key] AS int);
