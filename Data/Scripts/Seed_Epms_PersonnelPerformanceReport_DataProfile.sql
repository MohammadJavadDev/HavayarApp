/*
  Seed_Epms_PersonnelPerformanceReport_DataProfile.sql
  گزارش عملکرد پرسنل EPMS — معادل HTS epms-epmsPersonnelPerformanceReport
  و ReadData.GetEpmsPersonelPerformance.

  داده از TotalSystem (لینک‌سرور TMS) است. تابع راه‌دور MinutesToDuration
  قابل فراخوانی نیست؛ همان قالب ساعت:دقیقه داخل SELECT تکرار شده است.

  نام یکتا: Epms_PersonnelPerformanceReport
  موجودیت: Entities.App.Epms.ProposalDocument
  صفحه: /panel/epms/proposal/personnelperformancereport

  دسترسی از صفحه HTS 351 (نه 245 که گزارش EDMS است):
    - کارشناسان گزارش عملکرد پرسنل EPMS
    - مشاهده کلی HTS
    - کاربران DCC و Zare.mo به نقش گروه ۲۹۵ اضافه می‌شوند

  Apply:
    sqlcmd -S 172.20.40.42 -d HavayarApp -C -b -I -f 65001 -i Data\Scripts\Seed_Epms_PersonnelPerformanceReport_DataProfile.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-epms-personnel-performance';

    DECLARE @Name NVARCHAR(200) = N'Epms_PersonnelPerformanceReport';
    DECLARE @Title NVARCHAR(200) = N'گزارش عملکرد پرسنل';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.epms.proposaldocument';
    DECLARE @EntityPascal NVARCHAR(200) = N'Entities.App.Epms.ProposalDocument';
    DECLARE @PagePath NVARCHAR(200) = N'/panel/epms/proposal/personnelperformancereport';
    DECLARE @AnchorPath NVARCHAR(200) = N'/panel/epms/proposal/list';

    DECLARE @Role295Name NVARCHAR(200) = N'کارشناسان گزارش عملکرد پرسنل EPMS';
    DECLARE @Role350Name NVARCHAR(200) = N'مشاهده کلی HTS';
    DECLARE @Role295Id BIGINT;
    SELECT @Role295Id = Id FROM [system].[Role] WHERE Name = @Role295Name;
    IF @Role295Id IS NULL
        THROW 51623, N'نقش کارشناسان گزارش عملکرد پرسنل EPMS پیدا نشد.', 1;
    IF NOT EXISTS (SELECT 1 FROM [system].[Role] WHERE Name = @Role350Name)
        THROW 51624, N'نقش مشاهده کلی HTS پیدا نشد.', 1;

    DECLARE @CustomQuery NVARCHAR(MAX) = N'SELECT
    [d].[Document_ID] AS [t1_Id],
    [p].[Proposal_Code] AS [t1_ProposalCode],
    [p].[Proposal_Title] AS [t1_ProposalTitle],
    [v].[DocumentNumber] AS [t1_DocumentNumber],
    [v].[DocumentTitle] AS [t1_DocumentTitle],
    [mh].[ManHours] / 60 AS [t1_ProposalManHours],
    [dur].[ConsumedManHoursInText] AS [t1_ConsumedManHoursInText],
    [dur].[CheckedUserConsumedManHoursInText] AS [t1_CheckedUserConsumedManHoursInText],
    [d].[Revision] AS [t1_Revision],
    [d].[Due_Date_Shamsi] AS [t1_IssueDateShamsi],
    [pu].[FullName_En] AS [t1_PreparedUser],
    [cu].[FullName_En] AS [t1_CheckedUser]
FROM [TMS].[TotalSystem].[dbo].[Epms_Document] AS [d]
INNER JOIN [TMS].[TotalSystem].[dbo].[Epms_Proposal_Vpis] AS [v]
    ON [d].[Proposal_Vpis_FK] = [v].[Proposal_Vpis_ID]
INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] AS [pu]
    ON [d].[PreparedUser_FK] = [pu].[User_ID]
INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] AS [cu]
    ON [d].[CheckedUser_FK] = [cu].[User_ID]
INNER JOIN [TMS].[TotalSystem].[dbo].[Epms_Proposal] AS [p]
    ON [v].[Proposal_FK] = [p].[Proposal_ID]
INNER JOIN (
    SELECT [p1].[Proposal_ID], SUM([d1].[ConsumedManHours]) AS [ManHours]
    FROM [TMS].[TotalSystem].[dbo].[Epms_Proposal] AS [p1]
    INNER JOIN [TMS].[TotalSystem].[dbo].[Epms_Proposal_Vpis] AS [v1]
        ON [p1].[Proposal_ID] = [v1].[Proposal_FK]
    INNER JOIN [TMS].[TotalSystem].[dbo].[Epms_Document] AS [d1]
        ON [v1].[Proposal_Vpis_ID] = [d1].[Proposal_Vpis_FK]
    GROUP BY [p1].[Proposal_ID]
) AS [mh]
    ON [p].[Proposal_ID] = [mh].[Proposal_ID]
CROSS APPLY (
    SELECT
        CAST([d].[ConsumedManHours] AS int) AS [ConsumedMinutes],
        CAST([d].[ConsumedManHours] * 0.2 AS int) AS [CheckedMinutes]
) AS [mins]
CROSS APPLY (
    SELECT
        CASE
            WHEN [mins].[ConsumedMinutes] >= 60
                THEN CAST([mins].[ConsumedMinutes] / 60 AS varchar(20)) + CHAR(58)
                     + RIGHT(CHAR(48) + CAST([mins].[ConsumedMinutes] % 60 AS varchar(2)), 2)
            ELSE CHAR(48) + CHAR(48) + CHAR(58) + CAST([mins].[ConsumedMinutes] % 60 AS varchar(2))
        END AS [ConsumedManHoursInText],
        CASE
            WHEN [mins].[CheckedMinutes] >= 60
                THEN CAST([mins].[CheckedMinutes] / 60 AS varchar(20)) + CHAR(58)
                     + RIGHT(CHAR(48) + CAST([mins].[CheckedMinutes] % 60 AS varchar(2)), 2)
            ELSE CHAR(48) + CHAR(48) + CHAR(58) + CAST([mins].[CheckedMinutes] % 60 AS varchar(2))
        END AS [CheckedUserConsumedManHoursInText]
) AS [dur]
WHERE [d].[ConsumedManHours] IS NOT NULL';

    DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);

    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[
{"TableName":"Epms_Document","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[d].[Document_ID]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Epms_Proposal","ColumnName":"t1_ProposalCode","DisplayName":"کد پروپوزال","Alliance":"t1_ProposalCode","Address":"[p].[Proposal_Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Epms_Proposal","ColumnName":"t1_ProposalTitle","DisplayName":"عنوان پروپوزال","Alliance":"t1_ProposalTitle","Address":"[p].[Proposal_Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Epms_Proposal_Vpis","ColumnName":"t1_DocumentNumber","DisplayName":"کد مدرک","Alliance":"t1_DocumentNumber","Address":"[v].[DocumentNumber]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Epms_Proposal_Vpis","ColumnName":"t1_DocumentTitle","DisplayName":"عنوان مدرک","Alliance":"t1_DocumentTitle","Address":"[v].[DocumentTitle]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Epms_Document","ColumnName":"t1_ProposalManHours","DisplayName":"نفر ساعت پروپوزال","Alliance":"t1_ProposalManHours","Address":"[mh].[ManHours] / 60","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Epms_Document","ColumnName":"t1_ConsumedManHoursInText","DisplayName":"نفر ساعت","Alliance":"t1_ConsumedManHoursInText","Address":"[dur].[ConsumedManHoursInText]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Epms_Document","ColumnName":"t1_CheckedUserConsumedManHoursInText","DisplayName":"نفر ساعت بررسی کننده","Alliance":"t1_CheckedUserConsumedManHoursInText","Address":"[dur].[CheckedUserConsumedManHoursInText]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Epms_Document","ColumnName":"t1_Revision","DisplayName":"نسخه","Alliance":"t1_Revision","Address":"[d].[Revision]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Epms_Document","ColumnName":"t1_IssueDateShamsi","DisplayName":"تاریخ انتشار/ارسال","Alliance":"t1_IssueDateShamsi","Address":"[d].[Due_Date_Shamsi]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr_User","ColumnName":"t1_PreparedUser","DisplayName":"کاربر آماده کننده","Alliance":"t1_PreparedUser","Address":"[pu].[FullName_En]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr_User","ColumnName":"t1_CheckedUser","DisplayName":"کاربر بررسی کننده","Alliance":"t1_CheckedUser","Address":"[cu].[FullName_En]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""}
]';

    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @CustomButtons NVARCHAR(MAX) = N'[]';

    IF ISJSON(@ColumnsJson) <> 1 OR ISJSON(@QueryJson) <> 1 OR ISJSON(@ActionOptions) <> 1 OR ISJSON(@EventScripts) <> 1
        THROW 51620, N'ColumnsJson / QueryJson / ActionOptions / EventScriptsJson معتبر نیست.', 1;

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
    WHERE r.Name IN (@Role295Name, @Role350Name);

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT @PagePath, 1, 1, @EntityPascal, @Title, NULL, NULL, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM [system].[Role] r
    WHERE r.Name IN (@Role295Name, @Role350Name)
      AND NOT EXISTS (
          SELECT 1 FROM [system].[RoleAccess] ra
          WHERE ra.RoleId = r.Id AND ra.Path = @PagePath AND ra.ActionAccessType = 1
      );

    ---------------------------------------------------------------------------
    -- کاربران با دسترسی مستقیم صفحه ۳۵۱ که عضو گروه ۲۹۵ یا ۳۵۰ نیستند
    ---------------------------------------------------------------------------
    IF (SELECT COUNT(*) FROM [system].[User] u WHERE LOWER(u.Username) IN (N'dcc', N'zare.mo')) <> 2
        THROW 51626, N'کاربر DCC یا Zare.mo پیدا نشد.', 1;

    DECLARE @Uid BIGINT;
    DECLARE @Username NVARCHAR(200);
    DECLARE @RoleIds NVARCHAR(MAX);
    DECLARE @Roles NVARCHAR(MAX);
    DECLARE userCur CURSOR LOCAL FAST_FORWARD FOR
        SELECT u.Id, u.Username, u.RoleIds, u.Roles
        FROM [system].[User] u
        WHERE LOWER(u.Username) IN (N'dcc', N'zare.mo');

    OPEN userCur;
    FETCH NEXT FROM userCur INTO @Uid, @Username, @RoleIds, @Roles;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF ISJSON(@RoleIds) <> 1 OR ISJSON(@Roles) <> 1
            THROW 51625, N'RoleIds یا Roles کاربر نامعتبر است.', 1;

        IF NOT EXISTS (
            SELECT 1
            FROM OPENJSON(ISNULL(@RoleIds, N'[]')) j
            WHERE TRY_CAST(j.[value] AS BIGINT) = @Role295Id
        )
        BEGIN
            UPDATE [system].[User]
            SET RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) IN (N'', N'[]')
                        THEN N'[' + CAST(@Role295Id AS NVARCHAR(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@Role295Id AS NVARCHAR(20)) + N']')
                END,
                Roles = CASE
                    WHEN EXISTS (
                        SELECT 1 FROM OPENJSON(ISNULL(Roles, N'[]')) j WHERE j.[value] = @Role295Name
                    ) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) IN (N'', N'[]')
                        THEN N'["' + @Role295Name + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @Role295Name + N'"]')
                END,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @Uid;
            PRINT N'  role 295 assigned: ' + @Username;
        END
        ELSE
            PRINT N'  role 295 already assigned: ' + @Username;

        FETCH NEXT FROM userCur INTO @Uid, @Username, @RoleIds, @Roles;
    END
    CLOSE userCur;
    DEALLOCATE userCur;

    ---------------------------------------------------------------------------
    -- منو: برگ گزارش کنار لیست پروپوزال
    ---------------------------------------------------------------------------
    DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"گزارش عملکرد پرسنل","icon":"ki-element-11 ki-outline","iconColor":"#3699ff","path":"/panel/epms/proposal/personnelperformancereport","a_attr":{"href":"/panel/epms/proposal/personnelperformancereport"},"data":{"iconColor":"#3699ff","path":"/panel/epms/proposal/personnelperformancereport"},"children":[]}';
    IF ISJSON(@Leaf) <> 1
        THROW 51621, N'JSON برگ منو معتبر نیست.', 1;

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
                    THROW 51622, N'Menu JSON after insert is invalid.', 1;

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
    IF CURSOR_STATUS('local', 'userCur') >= 0
    BEGIN
        CLOSE userCur;
        DEALLOCATE userCur;
    END
    ELSE IF CURSOR_STATUS('local', 'userCur') = -1
        DEALLOCATE userCur;

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
WHERE Name = N'Epms_PersonnelPerformanceReport';

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName,
       JSON_VALUE(j.value, '$.Visible') AS Visible
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Epms_PersonnelPerformanceReport';

SELECT r.Name AS RoleName, ra.Path, ra.ActionAccessType, ra.ActionAccessItemType, ra.RowId, ra.EntityName
FROM [system].[RoleAccess] ra
INNER JOIN [system].[Role] r ON r.Id = ra.RoleId
WHERE ra.Path = N'/panel/epms/proposal/personnelperformancereport'
   OR (ra.ActionAccessType = 3 AND ra.EntityName = N'Entities.App.Epms.ProposalDocument'
       AND ra.RowId = (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'Epms_PersonnelPerformanceReport'));

SELECT Name
FROM [system].[SystemMenu]
WHERE CHARINDEX(N'/panel/epms/proposal/personnelperformancereport', Content) > 0;

SELECT u.Username,
       CASE WHEN EXISTS (
           SELECT 1 FROM OPENJSON(u.RoleIds) j
           WHERE TRY_CAST(j.[value] AS BIGINT) = (SELECT Id FROM [system].[Role] WHERE Name = N'کارشناسان گزارش عملکرد پرسنل EPMS')
       ) THEN 1 ELSE 0 END AS HasRole295
FROM [system].[User] u
WHERE LOWER(u.Username) IN (N'dcc', N'zare.mo', N'khodakarami.f');
