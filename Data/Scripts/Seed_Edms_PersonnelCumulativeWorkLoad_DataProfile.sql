/*
  Seed_Edms_PersonnelCumulativeWorkLoad_DataProfile.sql
  گزارش تجمعی حجم کاری پرسنل — معادل برگ HTS edms-personnelCumulativeWorkLoadReport.
  گرید زنده HTS از GetGridData خوانده می‌شود: یک ردیف برای هر پروژه در جریان و تهیه‌کننده.
  وضعیت آخرین کامنت، نفرساعت VPIS و تاریخ تولید از TotalSystem خوانده می‌شود
  تا کامنت همگام‌نشده یا نفرساعتِ متفاوت در Havayar ردیف تکراری یا جمع غلط نسازد.

  نام یکتا: Edms_PersonnelCumulativeWorkLoadReport
  صفحه: /panel/edms/document/personnelcumulativeworkloadreport

  Apply:
    sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i Data\Scripts\Seed_Edms_PersonnelCumulativeWorkLoad_DataProfile.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-edms-cumulative-workload';

    DECLARE @Name NVARCHAR(200) = N'Edms_PersonnelCumulativeWorkLoadReport';
    DECLARE @Title NVARCHAR(200) = N'گزارش تجمعی حجم کاری پرسنل';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.edms.personnelcumulativeworkload';
    DECLARE @PageEntity NVARCHAR(200) = N'Entities.App.Edms.Document';
    DECLARE @PagePath NVARCHAR(300) = N'/panel/edms/document/personnelcumulativeworkloadreport';
    DECLARE @AnchorPath NVARCHAR(300) = N'/panel/edms/document/personnelworkloadreport';

    DECLARE @CustomQuery NVARCHAR(MAX) = N'DECLARE @Now datetime = GETDATE();
DECLARE @OneMonth datetime = DATEADD(month, 1, @Now);
DECLARE @TwoMonth datetime = DATEADD(month, 1, @OneMonth);
DECLARE @YearEnd datetime = DATEFROMPARTS(YEAR(@OneMonth), 12, 31);
;WITH LastComment AS (
    SELECT c.Document_FK, c.Document_Status_FK, c.CreatedDate,
           ROW_NUMBER() OVER (PARTITION BY c.Document_FK ORDER BY c.Document_Comment_ID DESC) AS rn
    FROM [TMS].[TotalSystem].[dbo].[Edms_Document_Comment] AS c
),
Docs AS (
    SELECT
        d.Document_ID AS DocumentId,
        d.Project_Vpis_FK AS VpisId,
        ISNULL(d.Revision, 0) AS Revision,
        ISNULL(d.ConsumedManHours, 0) AS Consumed,
        p.Edms_Project_ID AS ProjectId,
        p.Project_Code AS ProjectCode,
        p.Project_Name AS ProjectName,
        hu.User_ID AS PreparedUserId,
        LTRIM(RTRIM(hu.FullName_En)) AS PreparedUserName,
        CASE
            WHEN lc.Document_Status_FK = 13 THEN DATEADD(day, ISNULL(p.AllowedHavayar_DelayDay, 0) + ISNULL(p.AllowedEmployer_DelayDay, 0), lc.CreatedDate)
            ELSE DATEADD(day, ISNULL(p.AllowedHavayar_DelayDay, 0), lc.CreatedDate)
        END AS ProductionDate,
        CASE WHEN ISNULL(d.Revision, 0) = 0 THEN ISNULL(v.Rev0_ManHour, 0) ELSE 0 END AS Rev0Part,
        CASE WHEN d.Revision = 1 THEN ISNULL(v.Rev1_ManHour, 0) ELSE 0 END AS Rev1Part,
        CASE WHEN d.Revision > 1 THEN ISNULL(v.RevN_ManHour, 0) ELSE 0 END AS RevNPart
    FROM [TMS].[TotalSystem].[dbo].[Edms_Document] AS d
    INNER JOIN LastComment AS lc ON lc.Document_FK = d.Document_ID AND lc.rn = 1
    INNER JOIN [TMS].[TotalSystem].[dbo].[Edms_Project_Vpis] AS v ON v.Project_Vpis_ID = d.Project_Vpis_FK
    INNER JOIN [TMS].[TotalSystem].[dbo].[Edms_Project] AS p ON p.Edms_Project_ID = v.Project_FK
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] AS hu ON hu.User_ID = d.PreparedUser_FK
    WHERE lc.Document_Status_FK IN (9, 10, 13)
      AND lc.CreatedDate IS NOT NULL
      AND p.Project_Status_FK = 4
      AND ISNULL(hu.IsActive, 1) <> 0
),
Grouped AS (
    SELECT
        ProjectId,
        MAX(ProjectCode) AS ProjectCode,
        MAX(ProjectName) AS ProjectName,
        PreparedUserId,
        MAX(PreparedUserName) AS PreparedUserName,
        SUM(Rev0Part) AS Rev0ManHour,
        SUM(Rev1Part) AS Rev1ManHour,
        SUM(RevNPart) AS RevNManHour
    FROM Docs
    GROUP BY ProjectId, PreparedUserId
),
Checking AS (
    SELECT
        ProjectId,
        PreparedUserId,
        CAST(ROUND(SUM(CAST(Consumed AS float)) * 0.2, 0) AS int) AS CheckingManHour
    FROM (
        SELECT
            ProjectId,
            PreparedUserId,
            Consumed,
            ROW_NUMBER() OVER (PARTITION BY ProjectId, PreparedUserId, VpisId ORDER BY Revision DESC, DocumentId DESC) AS rn
        FROM Docs
    ) AS lastRev
    WHERE rn = 1
    GROUP BY ProjectId, PreparedUserId
),
BucketDoc AS (
    SELECT
        ProjectId,
        PreparedUserId,
        Revision,
        DocumentId,
        CASE
            WHEN ProductionDate <= @OneMonth THEN 1
            WHEN ProductionDate > @OneMonth AND ProductionDate <= @TwoMonth THEN 2
            WHEN ProductionDate > @TwoMonth AND ProductionDate <= @YearEnd THEN 3
            ELSE 0
        END AS Bucket
    FROM Docs
),
BucketWinner AS (
    SELECT ProjectId, PreparedUserId, Bucket, Revision
    FROM (
        SELECT
            ProjectId,
            PreparedUserId,
            Bucket,
            Revision,
            ROW_NUMBER() OVER (PARTITION BY ProjectId, PreparedUserId, Bucket ORDER BY DocumentId DESC) AS rn
        FROM BucketDoc
        WHERE Bucket > 0
    ) AS ranked
    WHERE rn = 1
)
--!--mainsection
SELECT
    CONCAT(CONVERT(nvarchar(30), g.ProjectId), ''-'', CONVERT(nvarchar(20), g.PreparedUserId)) AS [t1_Id],
    g.ProjectCode AS [t1_ProjectCode],
    g.ProjectName AS [t1_ProjectName],
    g.PreparedUserName AS [t1_MainResponsible],
    CASE
        WHEN b1.Revision IS NULL THEN 0
        WHEN b1.Revision = 0 AND g.Rev0ManHour > 0 THEN g.Rev0ManHour + ISNULL(chk.CheckingManHour, 0)
        WHEN b1.Revision = 1 AND g.Rev1ManHour > 0 THEN g.Rev1ManHour + ISNULL(chk.CheckingManHour, 0)
        WHEN b1.Revision > 1 AND g.RevNManHour > 0 THEN g.RevNManHour + ISNULL(chk.CheckingManHour, 0)
        ELSE 0
    END AS [t1_ManHour1MonthsLater],
    CASE
        WHEN b2.Revision IS NULL THEN 0
        WHEN b2.Revision = 0 AND g.Rev0ManHour > 0 THEN g.Rev0ManHour + ISNULL(chk.CheckingManHour, 0)
        WHEN b2.Revision = 1 AND g.Rev1ManHour > 0 THEN g.Rev1ManHour + ISNULL(chk.CheckingManHour, 0)
        WHEN b2.Revision > 1 AND g.RevNManHour > 0 THEN g.RevNManHour + ISNULL(chk.CheckingManHour, 0)
        ELSE 0
    END AS [t1_ManHour2MonthsLater],
    CASE
        WHEN b3.Revision IS NULL THEN 0
        WHEN b3.Revision = 0 AND g.Rev0ManHour > 0 THEN g.Rev0ManHour + ISNULL(chk.CheckingManHour, 0)
        WHEN b3.Revision = 1 AND g.Rev1ManHour > 0 THEN g.Rev1ManHour + ISNULL(chk.CheckingManHour, 0)
        WHEN b3.Revision > 1 AND g.RevNManHour > 0 THEN g.RevNManHour + ISNULL(chk.CheckingManHour, 0)
        ELSE 0
    END AS [t1_ManHourNMonthsLater],
    g.Rev0ManHour AS [t1_Rev0ManHour],
    g.Rev1ManHour AS [t1_Rev1ManHour],
    g.RevNManHour AS [t1_RevNManHour]
FROM Grouped AS g
LEFT JOIN Checking AS chk ON chk.ProjectId = g.ProjectId AND chk.PreparedUserId = g.PreparedUserId
LEFT JOIN BucketWinner AS b1 ON b1.ProjectId = g.ProjectId AND b1.PreparedUserId = g.PreparedUserId AND b1.Bucket = 1
LEFT JOIN BucketWinner AS b2 ON b2.ProjectId = g.ProjectId AND b2.PreparedUserId = g.PreparedUserId AND b2.Bucket = 2
LEFT JOIN BucketWinner AS b3 ON b3.ProjectId = g.ProjectId AND b3.PreparedUserId = g.PreparedUserId AND b3.Bucket = 3';

    DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);

    DECLARE @HourRender NVARCHAR(MAX) = N'function(data, type, row) { if (data == null || data === '''') return ''''; var minutes = Number(data); if (!isFinite(minutes)) return data; var sign = minutes < 0 ? ''-'' : ''''; var abs = Math.abs(Math.round(minutes)); var hours = Math.floor(abs / 60); var mins = abs % 60; var hh = hours < 10 ? ''0'' + hours : String(hours); var mm = mins < 10 ? ''0'' + mins : String(mins); return sign + hh + '':'' + mm; }';

    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[
{"TableName":"Edms.Document","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"CONCAT(CONVERT(nvarchar(30), g.ProjectId), ''-'', CONVERT(nvarchar(20), g.PreparedUserId))","SystemTypeName":"String","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":0,"SortDirection":0,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":false,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Project","ColumnName":"t1_ProjectCode","DisplayName":"کد پروژه","Alliance":"t1_ProjectCode","Address":"g.ProjectCode","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.Project","ColumnName":"t1_ProjectName","DisplayName":"نام پروژه","Alliance":"t1_ProjectName","Address":"g.ProjectName","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"dbo.Gnr_User","ColumnName":"t1_MainResponsible","DisplayName":"مسئول اصلی","Alliance":"t1_MainResponsible","Address":"g.PreparedUserName","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.ProjectVpis","ColumnName":"t1_ManHour1MonthsLater","DisplayName":"نفرساعت ماه بعد","Alliance":"t1_ManHour1MonthsLater","Address":"t1_ManHour1MonthsLater","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"__HOUR_RENDER__","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":170,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.ProjectVpis","ColumnName":"t1_ManHour2MonthsLater","DisplayName":"نفرساعت ماه بعد+1","Alliance":"t1_ManHour2MonthsLater","Address":"t1_ManHour2MonthsLater","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"__HOUR_RENDER__","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":170,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.ProjectVpis","ColumnName":"t1_ManHourNMonthsLater","DisplayName":"نفرساعت ماه بعد+1 تا پابان سال","Alliance":"t1_ManHourNMonthsLater","Address":"t1_ManHourNMonthsLater","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"__HOUR_RENDER__","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":170,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.ProjectVpis","ColumnName":"t1_Rev0ManHour","DisplayName":"نفر ساعت رویژن 0","Alliance":"t1_Rev0ManHour","Address":"g.Rev0ManHour","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"__HOUR_RENDER__","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.ProjectVpis","ColumnName":"t1_Rev1ManHour","DisplayName":"نفر ساعت رویژن 1","Alliance":"t1_Rev1ManHour","Address":"g.Rev1ManHour","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"__HOUR_RENDER__","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.ProjectVpis","ColumnName":"t1_RevNManHour","DisplayName":"نفر ساعت رویژن بعدی","Alliance":"t1_RevNManHour","Address":"g.RevNManHour","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"__HOUR_RENDER__","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":"ltr"}
]';
    SET @ColumnsJson = REPLACE(@ColumnsJson, N'__HOUR_RENDER__', @HourRender);

    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @CustomButtons NVARCHAR(MAX) = N'[]';

    IF @QueryJson IS NULL OR ISJSON(@QueryJson) <> 1
        THROW 51630, N'QueryJson معتبر نیست.', 1;
    IF ISJSON(@ColumnsJson) <> 1 OR ISJSON(@ActionOptions) <> 1 OR ISJSON(@EventScripts) <> 1
        THROW 51631, N'ColumnsJson / ActionOptions / EventScriptsJson معتبر نیست.', 1;

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
      AND ra.EntityName = @EntityFullName;

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT N'dataProfile_' + CAST(@ProfileId AS NVARCHAR(20)), 3, 7, @EntityFullName, @Title, NULL, @ProfileId, src.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM [system].[RoleAccess] src
    WHERE src.Path = @AnchorPath
      AND src.ActionAccessType = 1
      AND ISNULL(src.IsActive, 1) = 1;

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT @PagePath, 1, 1, @PageEntity, @Title, NULL, NULL, src.RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM [system].[RoleAccess] src
    WHERE src.Path = @AnchorPath
      AND src.ActionAccessType = 1
      AND ISNULL(src.IsActive, 1) = 1
      AND NOT EXISTS (
          SELECT 1 FROM [system].[RoleAccess] ra
          WHERE ra.RoleId = src.RoleId AND ra.Path = @PagePath AND ra.ActionAccessType = 1
      );

    ---------------------------------------------------------------------------
    -- منو: برگ گزارش کنار «حجم کاری پرسنل»
    ---------------------------------------------------------------------------
    DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"گزارش تجمعی حجم کاری پرسنل","icon":"ki-chart-line-up ki-outline","iconColor":"#3699ff","path":"/panel/edms/document/personnelcumulativeworkloadreport","a_attr":{"href":"/panel/edms/document/personnelcumulativeworkloadreport"},"data":{"iconColor":"#3699ff","path":"/panel/edms/document/personnelcumulativeworkloadreport"},"children":[]}';
    IF ISJSON(@Leaf) <> 1
        THROW 51632, N'JSON برگ منو معتبر نیست.', 1;

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
                    THROW 51633, N'Menu JSON after insert is invalid.', 1;

                UPDATE [system].[SystemMenu]
                SET Content = @Content,
                    ModifiedById = 1,
                    ModifiedByName = @SeedUser,
                    ModifiedDateMiladiDateTime = @Now,
                    ModifiedDateShamsiDateTime = @NowShamsi
                WHERE Name = @MenuName;

                PRINT N'  menu added: ' + @MenuName + N' at ' + @BestPath;
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

SELECT Id, Name, Title, EntityFullName, Mode, [Type]
FROM [system].[SavedQuery]
WHERE Name = N'Edms_PersonnelCumulativeWorkLoadReport';

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Edms_PersonnelCumulativeWorkLoadReport';

SELECT r.Name AS RoleName, ra.Path, ra.ActionAccessType, ra.RowId, ra.EntityName
FROM [system].[RoleAccess] ra
INNER JOIN [system].[Role] r ON r.Id = ra.RoleId
WHERE ra.Path = N'/panel/edms/document/personnelcumulativeworkloadreport'
   OR (ra.ActionAccessType = 3
       AND ra.EntityName = N'entities.app.edms.personnelcumulativeworkload'
       AND ra.RowId = (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'Edms_PersonnelCumulativeWorkLoadReport'));

SELECT Name
FROM [system].[SystemMenu]
WHERE CHARINDEX(N'/panel/edms/document/personnelcumulativeworkloadreport', Content) > 0;
