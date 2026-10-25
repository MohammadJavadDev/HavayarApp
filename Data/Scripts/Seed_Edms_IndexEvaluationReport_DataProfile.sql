/*
  گزارش شاخص ارزیابی — معادل HTS Edms_IndexEvaluationReport (صفحه 380) و Vw_Edms_IndexEvaluation.
  نام یکتا: EdmsIndexEvaluationReport
  صفحه: /panel/edms/document/indexevaluationreport
  داده از ویوی زنده TotalSystem خوانده می‌شود تا ردیف‌ها با سیستم قدیم یکی بماند.
  UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-edms-index-evaluation-report';

    DECLARE @Name NVARCHAR(200) = N'Edms_IndexEvaluationReport';
    DECLARE @Title NVARCHAR(200) = N'گزارش شاخص ارزیابی';
    DECLARE @EntityFullName NVARCHAR(200) = N'EdmsIndexEvaluationReport';
    DECLARE @PageEntity NVARCHAR(200) = N'Entities.App.Edms.Document';
    DECLARE @PagePath NVARCHAR(300) = N'/panel/edms/document/indexevaluationreport';
    DECLARE @AnchorPath NVARCHAR(300) = N'/panel/edms/document/documentholdreport';

    IF (SELECT COUNT(*) FROM system.Role WHERE Id IN (260229, 260350, 260488)) < 3
        THROW 51650, N'یکی از نقش‌های گزارش شاخص ارزیابی پیدا نشد.', 1;

    DECLARE @CustomQuery NVARCHAR(MAX) = N'SELECT
    v.Document_ID AS [t1_Id],
    v.Project_Code AS [Project_Code],
    v.Project_Name AS [Project_Name],
    v.Document_Number AS [Document_Number],
    v.Document_Title AS [Document_Title],
    v.Revision AS [Revision],
    v.FinalStatus AS [FinalStatus],
    v.Disipline AS [Disipline],
    v.CreatedDate AS [CreatedDate],
    v.Responsible AS [Responsible],
    v.SumOfManHourInText AS [SumOfManHourInText]
FROM [TMS].[TotalSystem].[dbo].[Vw_Edms_IndexEvaluation] AS v';

    DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);

    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[
{"TableName":"Edms.Document","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[v].[Document_ID]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":false,"Sortable":false,"ClassName":""},
{"TableName":"Edms.Project","ColumnName":"Project_Code","DisplayName":"کد پروژه","Alliance":"Project_Code","Address":"[v].[Project_Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.Project","ColumnName":"Project_Name","DisplayName":"پروژه","Alliance":"Project_Name","Address":"[v].[Project_Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.ProjectVpis","ColumnName":"Document_Number","DisplayName":"کد سند","Alliance":"Document_Number","Address":"[v].[Document_Number]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.ProjectVpis","ColumnName":"Document_Title","DisplayName":"عنوان سند","Alliance":"Document_Title","Address":"[v].[Document_Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":320,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"Revision","DisplayName":"نسخه","Alliance":"Revision","Address":"[v].[Revision]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"FinalStatus","DisplayName":"وضعیت نهایی","Alliance":"FinalStatus","Address":"[v].[FinalStatus]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.ProjectVpis","ColumnName":"Disipline","DisplayName":"دیسیپلین","Alliance":"Disipline","Address":"[v].[Disipline]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"CreatedDate","DisplayName":"تاریخ ایجاد","Alliance":"CreatedDate","Address":"[v].[CreatedDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":1,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"system.User","ColumnName":"Responsible","DisplayName":"مسئول","Alliance":"Responsible","Address":"[v].[Responsible]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":170,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"SumOfManHourInText","DisplayName":"مجموع نفرساعت","Alliance":"SumOfManHourInText","Address":"[v].[SumOfManHourInText]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""}
]';

    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @CustomButtons NVARCHAR(MAX) = N'[]';

    IF @QueryJson IS NULL OR ISJSON(@QueryJson) <> 1
        THROW 51651, N'QueryJson معتبر نیست.', 1;
    IF ISJSON(@ColumnsJson) <> 1 OR ISJSON(@ActionOptions) <> 1 OR ISJSON(@EventScripts) <> 1
        THROW 51652, N'ColumnsJson / ActionOptions / EventScriptsJson معتبر نیست.', 1;

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
    END

    DELETE FROM system.RoleAccess
    WHERE ActionAccessType = 3 AND RowId = @ProfileId;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@ProfileId AS nvarchar(20)), 3, 7, @EntityFullName, @Title, NULL, @ProfileId, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Id IN (260229, 260350, 260488)
       OR r.Name IN (
            N'کارشناسان شاخص ارزیابی EDMS',
            N'مشاهده کلی HTS',
            N'کارشناسان سیستم مهندسی دارای دسترسی های عمومی سیستم مهندسی'
       );

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT @PagePath, 1, 1, @PageEntity, @Title, NULL, NULL, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE (
            r.Id IN (260229, 260350, 260488)
         OR r.Name IN (
                N'کارشناسان شاخص ارزیابی EDMS',
                N'مشاهده کلی HTS',
                N'کارشناسان سیستم مهندسی دارای دسترسی های عمومی سیستم مهندسی'
            )
        )
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess x
          WHERE x.RoleId = r.Id AND x.Path = @PagePath AND x.ActionAccessType = 1
      );

    ---------------------------------------------------------------------------
    -- منو: برگ گزارش کنار «گزارش توقفات مدارک»
    ---------------------------------------------------------------------------
    DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"گزارش شاخص ارزیابی","icon":"ki-chart-line-up ki-outline","iconColor":"#8950FC","path":"/panel/edms/document/indexevaluationreport","a_attr":{"href":"/panel/edms/document/indexevaluationreport"},"data":{"iconColor":"#8950FC","path":"/panel/edms/document/indexevaluationreport"},"children":[]}';
    IF ISJSON(@Leaf) <> 1
        THROW 51653, N'JSON برگ منو معتبر نیست.', 1;

    DECLARE @MenuName NVARCHAR(200);
    DECLARE @Content NVARCHAR(MAX);
    DECLARE @i INT;
    DECLARE @j INT;
    DECLARE @k INT;
    DECLARE @RootCount INT;
    DECLARE @ChildCount INT;
    DECLARE @GrandCount INT;
    DECLARE @ChildJson NVARCHAR(MAX);
    DECLARE @GrandJson NVARCHAR(MAX);
    DECLARE @GreatJson NVARCHAR(MAX);
    DECLARE @BestPath NVARCHAR(400);
    DECLARE @BestDepth INT;
    DECLARE @NodePath NVARCHAR(400);
    DECLARE @p1 NVARCHAR(200);
    DECLARE @p2 NVARCHAR(300);
    DECLARE @p3 NVARCHAR(400);
    DECLARE @Arr NVARCHAR(MAX);
    DECLARE @Rebuilt NVARCHAR(MAX);
    DECLARE @n INT;
    DECLARE @x INT;
    DECLARE @comma BIT;

    DECLARE menuCur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name
        FROM [system].[SystemMenu]
        WHERE CHARINDEX(@AnchorPath, ISNULL(Content, N'')) > 0;

    OPEN menuCur;
    FETCH NEXT FROM menuCur INTO @MenuName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @Content = Content FROM [system].[SystemMenu] WHERE Name = @MenuName;

        IF CHARINDEX(@PagePath, @Content) > 0
        BEGIN
            PRINT N'  menu already has report: ' + @MenuName;
        END
        ELSE IF ISJSON(@Content) <> 1
        BEGIN
            PRINT N'  WARN menu JSON invalid: ' + @MenuName;
        END
        ELSE
        BEGIN
            SET @BestPath = NULL;
            SET @BestDepth = -1;
            SET @RootCount = (SELECT COUNT(*) FROM OPENJSON(@Content));
            SET @i = 0;

            WHILE @i < @RootCount
            BEGIN
                SET @NodePath = JSON_VALUE(@Content, N'$[' + CAST(@i AS NVARCHAR(10)) + N'].path');
                IF @NodePath = @AnchorPath AND @BestDepth < 0
                BEGIN
                    SET @BestPath = N'$';
                    SET @BestDepth = 0;
                END

                SET @p1 = N'$[' + CAST(@i AS NVARCHAR(10)) + N'].children';
                SET @ChildJson = JSON_QUERY(@Content, @p1);
                SET @ChildCount = ISNULL((SELECT COUNT(*) FROM OPENJSON(@ChildJson)), 0);
                SET @j = 0;
                WHILE @j < @ChildCount
                BEGIN
                    IF JSON_VALUE(@ChildJson, N'$[' + CAST(@j AS NVARCHAR(10)) + N'].path') = @AnchorPath AND @BestDepth < 1
                    BEGIN
                        SET @BestPath = @p1;
                        SET @BestDepth = 1;
                    END

                    SET @p2 = @p1 + N'[' + CAST(@j AS NVARCHAR(10)) + N'].children';
                    SET @GrandJson = JSON_QUERY(@Content, @p2);
                    SET @GrandCount = ISNULL((SELECT COUNT(*) FROM OPENJSON(@GrandJson)), 0);
                    SET @k = 0;
                    WHILE @k < @GrandCount
                    BEGIN
                        IF JSON_VALUE(@GrandJson, N'$[' + CAST(@k AS NVARCHAR(10)) + N'].path') = @AnchorPath AND @BestDepth < 2
                        BEGIN
                            SET @BestPath = @p2;
                            SET @BestDepth = 2;
                        END

                        SET @p3 = @p2 + N'[' + CAST(@k AS NVARCHAR(10)) + N'].children';
                        SET @GreatJson = JSON_QUERY(@Content, @p3);
                        IF EXISTS (
                            SELECT 1 FROM OPENJSON(@GreatJson)
                            WHERE JSON_VALUE([value], '$.path') = @AnchorPath
                        ) AND @BestDepth < 3
                        BEGIN
                            SET @BestPath = @p3;
                            SET @BestDepth = 3;
                        END

                        SET @k = @k + 1;
                    END
                    SET @j = @j + 1;
                END
                SET @i = @i + 1;
            END

            IF @BestPath IS NULL
                PRINT N'  WARN menu not placed: ' + @MenuName;
            ELSE
            BEGIN
                SET @Arr = CASE WHEN @BestPath = N'$' THEN @Content ELSE JSON_QUERY(@Content, @BestPath) END;
                SET @Rebuilt = N'[';
                SET @n = ISNULL((SELECT COUNT(*) FROM OPENJSON(@Arr)), 0);
                SET @x = 0;
                SET @comma = 0;
                WHILE @x < @n
                BEGIN
                    IF @comma = 1 SET @Rebuilt += N',';
                    SET @comma = 1;
                    SET @Rebuilt += JSON_QUERY(@Arr, N'$[' + CAST(@x AS NVARCHAR(10)) + N']');
                    IF JSON_VALUE(@Arr, N'$[' + CAST(@x AS NVARCHAR(10)) + N'].path') = @AnchorPath
                        SET @Rebuilt += N',' + @Leaf;
                    SET @x += 1;
                END
                SET @Rebuilt += N']';

                IF @BestPath = N'$'
                    SET @Content = @Rebuilt;
                ELSE
                    SET @Content = JSON_MODIFY(@Content, @BestPath, JSON_QUERY(@Rebuilt));

                IF @Content IS NULL OR ISJSON(@Content) <> 1
                    THROW 51654, N'Menu JSON after insert is invalid.', 1;

                UPDATE [system].[SystemMenu]
                SET Content = @Content,
                    ModifiedById = 1,
                    ModifiedByName = @SeedUser,
                    ModifiedDateMiladiDateTime = @Now,
                    ModifiedDateShamsiDateTime = @NowShamsi
                WHERE Name = @MenuName;
            END
        END

        FETCH NEXT FROM menuCur INTO @MenuName;
    END
    CLOSE menuCur;
    DEALLOCATE menuCur;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF CURSOR_STATUS('local', 'menuCur') >= 0
    BEGIN
        CLOSE menuCur;
        DEALLOCATE menuCur;
    END
    ELSE IF CURSOR_STATUS('local', 'menuCur') = -1
        DEALLOCATE menuCur;
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;

SELECT Id, Name, Title, EntityFullName, ISJSON(QueryJson) AS QueryOk, ISJSON(ColumnsJson) AS ColOk,
       CASE WHEN Title LIKE N'%[یگچپژ]%' THEN N'ok' ELSE N'CHECK' END AS TitleEncoding
FROM [system].[SavedQuery]
WHERE Name = N'Edms_IndexEvaluationReport';

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Edms_IndexEvaluationReport';

SELECT r.Id, r.Name, ra.Path, ra.ActionAccessType, ra.EntityName, ra.RowId
FROM [system].[RoleAccess] ra
INNER JOIN [system].[Role] r ON r.Id = ra.RoleId
WHERE ra.Path = N'/panel/edms/document/indexevaluationreport'
   OR (ra.ActionAccessType = 3 AND ra.EntityName = N'EdmsIndexEvaluationReport');

SELECT Name
FROM [system].[SystemMenu]
WHERE CHARINDEX(N'/panel/edms/document/indexevaluationreport', Content) > 0;
