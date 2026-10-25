/*
  گزارش توقفات مدارک — معادل HTS Edms_DocumentHoldReport (صفحه 248) و Vw_Edms_Document_Hold.
  نام یکتا: EdmsDocumentHoldReport
  صفحه: /panel/edms/document/documentholdreport
  UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-edms-document-hold-report';

    DECLARE @Name NVARCHAR(200) = N'Edms_DocumentHoldReport';
    DECLARE @Title NVARCHAR(200) = N'گزارش توقفات مدارک';
    DECLARE @EntityFullName NVARCHAR(200) = N'EdmsDocumentHoldReport';
    DECLARE @PageEntity NVARCHAR(200) = N'Entities.App.Edms.Document';
    DECLARE @PagePath NVARCHAR(300) = N'/panel/edms/document/documentholdreport';
    DECLARE @AnchorPath NVARCHAR(300) = N'/panel/edms/document/personnelworkloadreport';
    DECLARE @DirectRoleId BIGINT = 260248;
    DECLARE @DirectRoleName NVARCHAR(200) = N'Edms.DocumentHoldReport.Direct';
    DECLARE @DirectRoleTitle NVARCHAR(200) = N'گزارش توقفات مدارک';

    DECLARE @CustomQuery NVARCHAR(MAX) = N'DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
DECLARE @IsAdmin BIT = 0;
DECLARE @PmOnly BIT = 0;
SELECT @IsAdmin = CASE WHEN EXISTS (
    SELECT 1
    FROM system.[User] AS u
    CROSS APPLY OPENJSON(CASE WHEN ISJSON(u.Roles) = 1 THEN u.Roles ELSE N''[]'' END) AS j
    WHERE u.Id = @CuId AND j.[value] = N''admin''
) THEN 1 ELSE 0 END;
SET @PmOnly = CASE
    WHEN @IsAdmin = 1 OR @CuId IS NULL THEN 0
    WHEN EXISTS (
        SELECT 1
        FROM system.[User] AS u
        WHERE u.Id = @CuId
          AND LOWER(LTRIM(RTRIM(u.Username))) IN (N''habibi.m'', N''farahmand.m'')
    ) THEN 1 ELSE 0 END;
;WITH CommentEvent AS (
    SELECT
        c.DocumentId,
        c.Status,
        ROW_NUMBER() OVER (
            PARTITION BY c.DocumentId, c.Status
            ORDER BY CASE WHEN ISNULL(c.HtsId, 0) > 0 THEN c.HtsId ELSE 2000000000 + c.Id END
        ) AS Rn,
        CAST(CASE
            WHEN c.HtsId > 0 AND hts.Document_Comment_ID IS NOT NULL THEN hts.Comment
            ELSE c.Comment
        END AS nvarchar(4000)) AS CommentText,
        COALESCE(
            CASE WHEN c.HtsId > 0 AND hts.Document_Comment_ID IS NOT NULL THEN NULLIF(LTRIM(RTRIM(hu.FullName)), N'''') END,
            c.CreatedByName
        ) AS CreatedByName,
        CASE
            WHEN c.HtsId > 0 AND hts.Document_Comment_ID IS NOT NULL THEN hts.CreatedDate
            ELSE CAST(c.CreatedOnMiladiDateTime AS date)
        END AS EventDate
    FROM [Edms].[DocumentComment] AS c
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Edms_Document_Comment] AS hts
        ON c.HtsId > 0 AND hts.Document_Comment_ID = c.HtsId
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] AS hu
        ON hu.User_ID = hts.CreatedUser_FK
    WHERE c.Status IN (16, 17, 18)
      AND ISNULL(c.IsActive, 1) <> 2
)
--!--mainsection
SELECT
    CONCAT(CAST(src.DocumentId AS nvarchar(20)), N''-'', CAST(src.HoldSource AS nvarchar(2)), N''-'', CAST(src.KeepRn AS nvarchar(10))) AS [t1_Id],
    src.DocumentId AS [t1_DocumentId],
    src.Project_Code AS [Project_Code],
    src.Project_Name AS [Project_Name],
    src.Document_Number AS [Document_Number],
    src.Document_Title AS [Document_Title],
    src.Hold_Producer AS [Hold_Producer],
    src.CreatedUser_Title AS [CreatedUser_Title],
    src.Revision AS [Revision],
    src.Duration AS [Duration],
    src.StartDate AS [StartDate],
    src.EndDate AS [EndDate],
    src.Comment AS [Comment]
FROM (
    SELECT
        raw.DocumentId,
        raw.HoldSource,
        raw.Project_Code,
        raw.Project_Name,
        raw.Document_Number,
        raw.Document_Title,
        raw.Hold_Producer,
        raw.CreatedUser_Title,
        raw.Revision,
        raw.Duration,
        raw.StartDate,
        raw.EndDate,
        raw.Comment,
        raw.ProjectManagerId,
        raw.Rn AS KeepRn,
        ROW_NUMBER() OVER (
            PARTITION BY raw.DocumentId, raw.HoldSource, raw.Project_Code, raw.Project_Name, raw.Document_Number, raw.Document_Title,
                raw.Hold_Producer, raw.CreatedUser_Title, raw.Revision, raw.Duration, raw.StartDate, raw.EndDate, raw.Comment
            ORDER BY raw.Rn
        ) AS DupRn
    FROM (
        SELECT
            d.Id AS DocumentId,
            1 AS HoldSource,
            h.Rn,
            COALESCE(NULLIF(LTRIM(RTRIM(hp.Project_Code)), N''''), p.Code) AS Project_Code,
            COALESCE(NULLIF(LTRIM(RTRIM(hp.Project_Name)), N''''), p.Name) AS Project_Name,
            COALESCE(NULLIF(LTRIM(RTRIM(REPLACE(REPLACE(ISNULL(hv.Document_Number, N''''), NCHAR(160), N''''), NCHAR(9), N''''))), N''''), v.Code) AS Document_Number,
            COALESCE(NULLIF(LTRIM(RTRIM(hv.Document_Title)), N''''), v.Title) AS Document_Title,
            h.CommentText AS Hold_Producer,
            h.CreatedByName AS CreatedUser_Title,
            d.Revision AS Revision,
            NULLIF(DATEDIFF(DAY, h.EventDate, s.EventDate), 0) AS Duration,
            CASE WHEN h.EventDate IS NULL THEN NULL ELSE dbo.ToShamsiDate(h.EventDate, N''/'') END AS StartDate,
            CASE WHEN s.EventDate IS NULL THEN NULL ELSE dbo.ToShamsiDate(s.EventDate, N''/'') END AS EndDate,
            h.CommentText AS Comment,
            COALESCE(hpHy.ProjectManagerId, p.ProjectManagerId) AS ProjectManagerId
        FROM [Edms].[Document] AS d
        INNER JOIN [Edms].[ProjectVpis] AS v ON v.Id = d.DocumentVpisId
        INNER JOIN [Edms].[Project] AS p ON p.Id = v.ProjectNameId
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Edms_Document] AS hd
            ON d.HtsId > 0 AND hd.Document_ID = d.HtsId
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Edms_Project_Vpis] AS hv
            ON hv.Project_Vpis_ID = hd.Project_Vpis_FK
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Edms_Project] AS hp
            ON hp.Edms_Project_ID = hv.Project_FK
        OUTER APPLY (
            SELECT TOP 1 px.ProjectManagerId
            FROM [Edms].[Project] AS px
            WHERE hp.Edms_Project_ID > 0
              AND px.HtsId = hp.Edms_Project_ID
              AND ISNULL(px.IsActive, 1) <> 2
            ORDER BY px.Id
        ) AS hpHy
        INNER JOIN CommentEvent AS h ON h.DocumentId = d.Id AND h.Status = 16
        LEFT JOIN CommentEvent AS s ON s.DocumentId = d.Id AND s.Status = 17 AND s.Rn = h.Rn
        UNION ALL
        SELECT
            d.Id,
            0,
            h.Rn,
            COALESCE(NULLIF(LTRIM(RTRIM(hp.Project_Code)), N''''), p.Code),
            COALESCE(NULLIF(LTRIM(RTRIM(hp.Project_Name)), N''''), p.Name),
            COALESCE(NULLIF(LTRIM(RTRIM(REPLACE(REPLACE(ISNULL(hv.Document_Number, N''''), NCHAR(160), N''''), NCHAR(9), N''''))), N''''), v.Code),
            COALESCE(NULLIF(LTRIM(RTRIM(hv.Document_Title)), N''''), v.Title),
            h.CommentText,
            N'''',
            d.Revision,
            CAST(NULL AS int),
            CASE WHEN h.EventDate IS NULL THEN NULL ELSE dbo.ToShamsiDate(h.EventDate, N''/'') END,
            CAST(NULL AS nvarchar(30)),
            h.CommentText,
            COALESCE(hpHy.ProjectManagerId, p.ProjectManagerId)
        FROM [Edms].[Document] AS d
        INNER JOIN [Edms].[ProjectVpis] AS v ON v.Id = d.DocumentVpisId
        INNER JOIN [Edms].[Project] AS p ON p.Id = v.ProjectNameId
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Edms_Document] AS hd
            ON d.HtsId > 0 AND hd.Document_ID = d.HtsId
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Edms_Project_Vpis] AS hv
            ON hv.Project_Vpis_ID = hd.Project_Vpis_FK
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Edms_Project] AS hp
            ON hp.Edms_Project_ID = hv.Project_FK
        OUTER APPLY (
            SELECT TOP 1 px.ProjectManagerId
            FROM [Edms].[Project] AS px
            WHERE hp.Edms_Project_ID > 0
              AND px.HtsId = hp.Edms_Project_ID
              AND ISNULL(px.IsActive, 1) <> 2
            ORDER BY px.Id
        ) AS hpHy
        INNER JOIN CommentEvent AS h ON h.DocumentId = d.Id AND h.Status = 18
    ) AS raw
) AS src
WHERE src.DupRn = 1
  AND (@PmOnly = 0 OR src.ProjectManagerId = @CuId)';

    DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);

    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[
{"TableName":"Edms.Document","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"src.RowKey","SystemTypeName":"String","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":false,"Sortable":false,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"t1_DocumentId","DisplayName":"شناسه مدرک","Alliance":"t1_DocumentId","Address":"src.DocumentId","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":0,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":false,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Project","ColumnName":"Project_Code","DisplayName":"کد پروژه","Alliance":"Project_Code","Address":"p.Code","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.Project","ColumnName":"Project_Name","DisplayName":"نام پروژه","Alliance":"Project_Name","Address":"p.Name","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.ProjectVpis","ColumnName":"Document_Number","DisplayName":"کد مدرک","Alliance":"Document_Number","Address":"v.Code","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":250,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.ProjectVpis","ColumnName":"Document_Title","DisplayName":"عنوان مدرک","Alliance":"Document_Title","Address":"v.Title","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":400,"Filterable":true,"Sortable":true,"ClassName":"ltr"},
{"TableName":"Edms.DocumentComment","ColumnName":"Hold_Producer","DisplayName":"مولد تاخیر","Alliance":"Hold_Producer","Address":"h.CommentText","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.DocumentComment","ColumnName":"CreatedUser_Title","DisplayName":"کاربر ایجادکننده","Alliance":"CreatedUser_Title","Address":"h.CreatedByName","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.Document","ColumnName":"Revision","DisplayName":"نسخه","Alliance":"Revision","Address":"d.Revision","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.DocumentComment","ColumnName":"Duration","DisplayName":"طول / مدت","Alliance":"Duration","Address":"NULLIF(DATEDIFF(DAY, h.EventDate, s.EventDate), 0)","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.DocumentComment","ColumnName":"StartDate","DisplayName":"تاریخ شروع","Alliance":"StartDate","Address":"dbo.ToShamsiDate(h.EventDate, N''/'')","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.DocumentComment","ColumnName":"EndDate","DisplayName":"تاریخ خاتمه","Alliance":"EndDate","Address":"dbo.ToShamsiDate(s.EventDate, N''/'')","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Edms.DocumentComment","ColumnName":"Comment","DisplayName":"کامنت","Alliance":"Comment","Address":"h.CommentText","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":400,"Filterable":true,"Sortable":true,"ClassName":""}
]';

    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @CustomButtons NVARCHAR(MAX) = N'[]';

    IF @QueryJson IS NULL OR ISJSON(@QueryJson) <> 1
        THROW 51640, N'QueryJson معتبر نیست.', 1;
    IF ISJSON(@ColumnsJson) <> 1 OR ISJSON(@ActionOptions) <> 1 OR ISJSON(@EventScripts) <> 1
        THROW 51641, N'ColumnsJson / ActionOptions / EventScriptsJson معتبر نیست.', 1;
    IF CHARINDEX(N'--!--mainsection', JSON_VALUE(@QueryJson, '$.CustomQuery')) = 0
        THROW 51642, N'نشانگر mainsection در کوئری نیست.', 1;

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

    IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @DirectRoleId AND Name <> @DirectRoleName)
        THROW 51643, N'شناسه نقش 260248 برای نقش دیگری استفاده شده است.', 1;

    IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @DirectRoleId)
        UPDATE system.Role
        SET Name = @DirectRoleName,
            Title = @DirectRoleTitle,
            IsActive = 1,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @DirectRoleId;
    ELSE
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role
            (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
             CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@DirectRoleId, @DirectRoleName, @DirectRoleTitle, 1, @SeedUser, 1, @SeedUser,
             @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
    END

    DECLARE @FekriId BIGINT;
    SELECT TOP 1 @FekriId = Id FROM system.[User] WHERE LOWER(Username) = N'fekri.f';
    IF @FekriId IS NOT NULL AND NOT EXISTS (
        SELECT 1
        FROM system.[User] u
        CROSS APPLY OPENJSON(CASE WHEN ISJSON(u.RoleIds) = 1 THEN u.RoleIds ELSE N'[]' END) j
        WHERE u.Id = @FekriId AND TRY_CAST(j.value AS bigint) = @DirectRoleId
    )
    BEGIN
        UPDATE system.[User]
        SET RoleIds = CASE
                WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) IN (N'', N'[]') OR ISJSON(RoleIds) = 0
                    THEN N'[' + CAST(@DirectRoleId AS nvarchar(20)) + N']'
                ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@DirectRoleId AS nvarchar(20)) + N']')
            END,
            Roles = CASE
                WHEN ISJSON(ISNULL(Roles, N'[]')) = 1 AND EXISTS (
                    SELECT 1 FROM OPENJSON(Roles) j WHERE j.value = @DirectRoleName
                ) THEN Roles
                WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) IN (N'', N'[]') OR ISJSON(Roles) = 0
                    THEN N'["' + @DirectRoleName + N'"]'
                ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @DirectRoleName + N'"]')
            END,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @FekriId;
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
    WHERE r.Id IN (260105, 260107, 260350, 260488, @DirectRoleId)
       OR r.Name IN (
            N'مدیران پروژه در EDMS',
            N'کارشناسان گزارشات جامع سیستم EDMS',
            N'مشاهده کلی HTS',
            N'کارشناسان سیستم مهندسی دارای دسترسی های عمومی سیستم مهندسی',
            @DirectRoleName
       );

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT @PagePath, 1, 1, @PageEntity, @Title, NULL, NULL, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE (
            r.Id IN (260105, 260107, 260350, 260488, @DirectRoleId)
         OR r.Name IN (
                N'مدیران پروژه در EDMS',
                N'کارشناسان گزارشات جامع سیستم EDMS',
                N'مشاهده کلی HTS',
                N'کارشناسان سیستم مهندسی دارای دسترسی های عمومی سیستم مهندسی',
                @DirectRoleName
            )
        )
      AND NOT EXISTS (
          SELECT 1 FROM system.RoleAccess x
          WHERE x.RoleId = r.Id AND x.Path = @PagePath AND x.ActionAccessType = 1
      );

    ---------------------------------------------------------------------------
    -- منو: برگ گزارش کنار «حجم کاری پرسنل» در گره گزارشات
    ---------------------------------------------------------------------------
    DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"گزارش توقفات مدارک","icon":"ki-timer ki-outline","iconColor":"#3699ff","path":"/panel/edms/document/documentholdreport","a_attr":{"href":"/panel/edms/document/documentholdreport"},"data":{"iconColor":"#3699ff","path":"/panel/edms/document/documentholdreport"},"children":[]}';
    IF ISJSON(@Leaf) <> 1
        THROW 51644, N'JSON برگ منو معتبر نیست.', 1;

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
                    THROW 51645, N'Menu JSON after insert is invalid.', 1;

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

SELECT Id, Name, Title, EntityFullName, ISJSON(QueryJson) AS QueryOk, ISJSON(ColumnsJson) AS ColOk
FROM [system].[SavedQuery]
WHERE Name = N'Edms_DocumentHoldReport';

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Edms_DocumentHoldReport';

SELECT r.Id, r.Name, ra.Path, ra.ActionAccessType, ra.EntityName, ra.RowId
FROM [system].[RoleAccess] ra
INNER JOIN [system].[Role] r ON r.Id = ra.RoleId
WHERE ra.Path = N'/panel/edms/document/documentholdreport'
   OR (ra.ActionAccessType = 3 AND ra.EntityName = N'EdmsDocumentHoldReport');

SELECT Name
FROM [system].[SystemMenu]
WHERE CHARINDEX(N'/panel/edms/document/documentholdreport', Content) > 0;
