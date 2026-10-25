/*
  Seed_Edms_CumulativePerformanceReport_DataProfile.sql
  گزارش تجمعی عملکرد/فعالیت — معادل HTS صفحه 291 / Edms_CumulativePerformanceReport.
  نام یکتا: Edms_CumulativePerformanceReport
  صفحه: /panel/edms/document/cumulativeperformancereport

  NOTE: the query below reads HTS through [TMS]. After applying this seed, apply
    Data\Scripts\Patch_Edms_CumulativePerformanceReport_NoTms.sql
  to switch the report to Portal tables (Edms.Document / Edms.ProjectActivity); otherwise the [TMS] query stays.

  Apply:
    sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i Data\Scripts\Seed_Edms_CumulativePerformanceReport_DataProfile.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-edms-cumulative-performance';

    DECLARE @Name NVARCHAR(200) = N'Edms_CumulativePerformanceReport';
    DECLARE @Title NVARCHAR(200) = N'گزارش تجمعی عملکرد/فعالیت';
    DECLARE @EntityFullName NVARCHAR(300) = N'entities.app.edms.cumulativeperformancereport';
    DECLARE @EntityPascal NVARCHAR(300) = N'Entities.App.Edms.CumulativePerformanceReport';
    DECLARE @PagePath NVARCHAR(300) = N'/panel/edms/document/cumulativeperformancereport';
    DECLARE @AnchorPath NVARCHAR(300) = N'/panel/edms/document/mdrreport';

    DECLARE @CustomQuery NVARCHAR(MAX) = N'DECLARE @CuId bigint = TRY_CAST(@CurrentUserId AS bigint);
DECLARE @CuRoles nvarchar(max) = N''[]'';
DECLARE @CuRoleIds nvarchar(max) = N''[]'';
DECLARE @IsAdmin bit = 0;
DECLARE @HasShowAll bit = 0;
DECLARE @HtsUserId int = NULL;
DECLARE @FromRaw nvarchar(30) = NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(30), @FromDate))), N'''');
DECLARE @ToRaw nvarchar(30) = NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(30), @ToDate))), N'''');
SET @FromRaw = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@FromRaw, N''-'', N''/''), N''۰'', N''0''), N''۱'', N''1''), N''۲'', N''2''), N''۳'', N''3''), N''۴'', N''4''), N''۵'', N''5''), N''۶'', N''6''), N''۷'', N''7''), N''۸'', N''8''), N''۹'', N''9''), N''٠'', N''0''), N''١'', N''1''), N''٢'', N''2''), N''٣'', N''3''), N''٤'', N''4''), N''٥'', N''5''), N''٦'', N''6''), N''٧'', N''7''), N''٨'', N''8''), N''٩'', N''9'');
SET @ToRaw = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@ToRaw, N''-'', N''/''), N''۰'', N''0''), N''۱'', N''1''), N''۲'', N''2''), N''۳'', N''3''), N''۴'', N''4''), N''۵'', N''5''), N''۶'', N''6''), N''۷'', N''7''), N''۸'', N''8''), N''۹'', N''9''), N''٠'', N''0''), N''١'', N''1''), N''٢'', N''2''), N''٣'', N''3''), N''٤'', N''4''), N''٥'', N''5''), N''٦'', N''6''), N''٧'', N''7''), N''٨'', N''8''), N''٩'', N''9'');
IF @FromRaw IS NOT NULL AND LEN(@FromRaw) > 10 SET @FromRaw = LEFT(@FromRaw, 10);
IF @ToRaw IS NOT NULL AND LEN(@ToRaw) > 10 SET @ToRaw = LEFT(@ToRaw, 10);
DECLARE @From date = CASE WHEN @FromRaw IS NULL THEN NULL ELSE CAST(dbo.fn_ShamsiToMiladiDate(@FromRaw, N''/'') AS date) END;
DECLARE @To date = CASE WHEN @ToRaw IS NULL THEN NULL ELSE CAST(dbo.fn_ShamsiToMiladiDate(@ToRaw, N''/'') AS date) END;
DECLARE @HasRange bit = CASE WHEN @From IS NULL AND @To IS NULL THEN 0 ELSE 1 END;

SELECT
    @CuRoles = CASE WHEN ISJSON(u.Roles) = 1 THEN u.Roles ELSE N''[]'' END,
    @CuRoleIds = CASE WHEN ISJSON(u.RoleIds) = 1 THEN u.RoleIds ELSE N''[]'' END
FROM system.[User] AS u
WHERE u.Id = @CuId;

SET @IsAdmin = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoles) WHERE [value] = N''admin'') THEN 1 ELSE 0 END;
SET @HasShowAll = CASE
    WHEN @IsAdmin = 1 THEN 1
    WHEN EXISTS (
        SELECT 1
        FROM OPENJSON(@CuRoleIds) AS j
        INNER JOIN system.Role AS r ON r.Id = TRY_CAST(j.[value] AS bigint)
        WHERE r.Name = N''کارشناسان مشاهده کلی گزارش تجمعی عملکرد/فعالیت EDMS''
    ) THEN 1
    ELSE 0
END;

SELECT TOP (1) @HtsUserId = hu.User_ID
FROM system.[User] AS u
INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] AS hu
    ON LOWER(LTRIM(RTRIM(hu.Username))) = LOWER(LTRIM(RTRIM(u.Username)))
    OR (
        NULLIF(LTRIM(RTRIM(hu.ActiveDirectoryUsername)), N'''') IS NOT NULL
        AND LOWER(LTRIM(RTRIM(hu.ActiveDirectoryUsername))) = LOWER(LTRIM(RTRIM(u.Username)))
    )
WHERE u.Id = @CuId
ORDER BY CASE
    WHEN LOWER(LTRIM(RTRIM(hu.Username))) = LOWER(LTRIM(RTRIM(u.Username))) THEN 0
    ELSE 1
END;

DECLARE @CanSee bit = CASE
    WHEN @HasRange = 0 THEN 0
    WHEN @HasShowAll = 1 THEN 1
    WHEN @HtsUserId IS NULL THEN 0
    ELSE 1
END;

;WITH FilteredDocs AS (
    SELECT
        v.Project_FK AS ProjectId,
        d.PreparedUser_FK AS UserId,
        pu.FullName_En AS PreparedUser,
        d.ConsumedManHours
    FROM [TMS].[TotalSystem].[dbo].[Edms_Document] AS d
    INNER JOIN [TMS].[TotalSystem].[dbo].[Edms_Project_Vpis] AS v
        ON v.Project_Vpis_ID = d.Project_Vpis_FK
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] AS pu
        ON pu.User_ID = d.PreparedUser_FK
    WHERE @CanSee = 1
      AND (d.LastStatusId IS NULL OR d.LastStatusId <> 11)
      AND (@HasShowAll = 1 OR d.PreparedUser_FK = @HtsUserId)
      AND (@From IS NULL OR d.PreparedDate >= @From)
      AND (@To IS NULL OR d.PreparedDate <= @To)
),
DocAgg AS (
    SELECT
        ProjectId,
        UserId,
        MIN(PreparedUser) AS PreparedUser,
        SUM(CASE WHEN ConsumedManHours IS NOT NULL THEN CAST(ConsumedManHours AS bigint) ELSE CAST(0 AS bigint) END) AS TotalPerformance
    FROM FilteredDocs
    GROUP BY ProjectId, UserId
),
FilteredActs AS (
    SELECT
        a.Project_FK AS ProjectId,
        a.CreatedUser_FK AS UserId,
        gu.FullName AS FullName,
        CAST(a.Duration AS bigint) AS Duration
    FROM [TMS].[TotalSystem].[dbo].[Edms_Project_Activity] AS a
    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] AS gu
        ON gu.User_ID = a.CreatedUser_FK
    WHERE @CanSee = 1
      AND a.ModuleType_FK = 386
      AND (@HasShowAll = 1 OR a.CreatedUser_FK = @HtsUserId)
      AND (@From IS NULL OR a.ActivityDate_Miladi >= @From)
      AND (@To IS NULL OR a.ActivityDate_Miladi <= @To)
),
ActByProject AS (
    SELECT ProjectId, UserId, SUM(Duration) AS TotalActivity
    FROM FilteredActs
    GROUP BY ProjectId, UserId
),
DocRows AS (
    SELECT
        CAST(1 AS int) AS RowKind,
        d.UserId,
        CAST(d.PreparedUser AS nvarchar(1024)) AS PreparedUser,
        CAST(p.Project_Code AS nvarchar(256)) AS ProjectCode,
        CAST(p.Project_Name AS nvarchar(512)) AS ProjectName,
        CAST(ou.OrgUnit_Title AS nvarchar(256)) AS BeneficiaryUnit,
        d.TotalPerformance,
        ISNULL(a.TotalActivity, CAST(0 AS bigint)) AS TotalActivity
    FROM DocAgg AS d
    INNER JOIN [TMS].[TotalSystem].[dbo].[Edms_Project] AS p
        ON p.Edms_Project_ID = d.ProjectId
    INNER JOIN [TMS].[TotalSystem].[dbo].[HRM_OrgUnit] AS ou
        ON ou.OrgUnit_ID = p.BeneficiaryUnit_FK
    LEFT JOIN ActByProject AS a
        ON a.ProjectId = d.ProjectId AND a.UserId = d.UserId
),
ActOnly AS (
    SELECT
        CAST(2 AS int) AS RowKind,
        f.UserId,
        CAST(MIN(f.FullName) AS nvarchar(1024)) AS PreparedUser,
        CAST(NULL AS nvarchar(256)) AS ProjectCode,
        CAST(NULL AS nvarchar(512)) AS ProjectName,
        CAST(NULL AS nvarchar(256)) AS BeneficiaryUnit,
        CAST(0 AS bigint) AS TotalPerformance,
        SUM(f.Duration) AS TotalActivity
    FROM FilteredActs AS f
    WHERE NOT EXISTS (SELECT 1 FROM DocAgg AS d WHERE d.UserId = f.UserId)
    GROUP BY f.UserId
),
Combined AS (
    SELECT * FROM DocRows
    UNION ALL
    SELECT * FROM ActOnly
)
--!--mainsection
SELECT
    ROW_NUMBER() OVER (ORDER BY c.PreparedUser, c.ProjectCode, c.UserId, c.RowKind) AS [t1_Id],
    c.PreparedUser AS [t1_PreparedUser],
    c.ProjectCode AS [t1_ProjectCode],
    c.ProjectName AS [t1_ProjectName],
    c.BeneficiaryUnit AS [t1_BeneficiaryUnit],
    c.TotalPerformance AS [t1_TotalPerformance],
    CASE WHEN c.RowKind = 2 THEN NULL ELSE perf.HourText END AS [t1_TotalPerformanceInText],
    c.TotalActivity AS [t1_TotalActivity],
    act.HourText AS [t1_TotalActivityInText],
    SUM(c.TotalPerformance) OVER () AS [t1_PerformanceSum],
    SUM(c.TotalActivity) OVER () AS [t1_ActivitySum]
FROM Combined AS c
CROSS APPLY (
    SELECT CONCAT(
        CASE WHEN c.TotalPerformance < 0 THEN N''-'' ELSE N'''' END,
        CASE WHEN ABS(c.TotalPerformance) / 60 < 10 THEN N''0'' ELSE N'''' END,
        CONVERT(nvarchar(20), ABS(c.TotalPerformance) / 60),
        N'':'',
        RIGHT(N''0'' + CONVERT(nvarchar(20), ABS(c.TotalPerformance) % 60), 2)
    ) AS HourText
) AS perf
CROSS APPLY (
    SELECT CONCAT(
        CASE WHEN c.TotalActivity < 0 THEN N''-'' ELSE N'''' END,
        CASE WHEN ABS(c.TotalActivity) / 60 < 10 THEN N''0'' ELSE N'''' END,
        CONVERT(nvarchar(20), ABS(c.TotalActivity) / 60),
        N'':'',
        RIGHT(N''0'' + CONVERT(nvarchar(20), ABS(c.TotalActivity) % 60), 2)
    ) AS HourText
) AS act';

    DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.Parameters', JSON_QUERY(N'[{"Id":"fromDate","Name":"FromDate","DisplayName":"از تاریخ","DefaultValue":"","DataType":"string"},{"Id":"toDate","Name":"ToDate","DisplayName":"تا تاریخ","DefaultValue":"","DataType":"string"}]'));

    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[{"TableName":"Edms.CumulativePerformanceReport","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"ROW_NUMBER()","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":false,"Sortable":false,"ClassName":""},{"TableName":"Edms.CumulativePerformanceReport","ColumnName":"t1_PreparedUser","DisplayName":"کاربر آماده کننده","Alliance":"t1_PreparedUser","Address":"c.PreparedUser","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":0,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Edms.CumulativePerformanceReport","ColumnName":"t1_ProjectCode","DisplayName":"کد پروژه","Alliance":"t1_ProjectCode","Address":"c.ProjectCode","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":"ltr"},{"TableName":"Edms.CumulativePerformanceReport","ColumnName":"t1_ProjectName","DisplayName":"نام پروژه","Alliance":"t1_ProjectName","Address":"c.ProjectName","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Edms.CumulativePerformanceReport","ColumnName":"t1_BeneficiaryUnit","DisplayName":"واحد ذینفع","Alliance":"t1_BeneficiaryUnit","Address":"c.BeneficiaryUnit","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Edms.CumulativePerformanceReport","ColumnName":"t1_TotalPerformance","DisplayName":"مجموع عملکرد به دقیقه","Alliance":"t1_TotalPerformance","Address":"c.TotalPerformance","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Edms.CumulativePerformanceReport","ColumnName":"t1_TotalPerformanceInText","DisplayName":"مجموع عملکرد","Alliance":"t1_TotalPerformanceInText","Address":"perf.HourText","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Edms.CumulativePerformanceReport","ColumnName":"t1_TotalActivity","DisplayName":"مجموع فعالیت به دقیقه","Alliance":"t1_TotalActivity","Address":"c.TotalActivity","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Edms.CumulativePerformanceReport","ColumnName":"t1_TotalActivityInText","DisplayName":"مجموع فعالیت","Alliance":"t1_TotalActivityInText","Address":"act.HourText","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Edms.CumulativePerformanceReport","ColumnName":"t1_PerformanceSum","DisplayName":"جمع عملکرد","Alliance":"t1_PerformanceSum","Address":"SUM(c.TotalPerformance)","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":false,"Sortable":false,"ClassName":""},{"TableName":"Edms.CumulativePerformanceReport","ColumnName":"t1_ActivitySum","DisplayName":"جمع فعالیت","Alliance":"t1_ActivitySum","Address":"SUM(c.TotalActivity)","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":false,"Sortable":false,"ClassName":""}]';
    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @CustomButtons NVARCHAR(MAX) = N'[]';

    IF ISJSON(@ColumnsJson) <> 1 OR ISJSON(@QueryJson) <> 1 OR ISJSON(@ActionOptions) <> 1 OR ISJSON(@EventScripts) <> 1
        THROW 51640, N'ColumnsJson / QueryJson / ActionOptions / EventScriptsJson معتبر نیست.', 1;

    IF CHARINDEX(N'--!--mainsection', @CustomQuery) = 0
        THROW 51641, N'نشانگر --!--mainsection در کوئری نیست.', 1;

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
        N'کارشناسان مشاهده کلی گزارش تجمعی عملکرد/فعالیت EDMS',
        N'کارشناسان گزارش تجمعی عملکرد/فعالیت EDMS',
        N'مشاهده کلی HTS'
    );

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT @PagePath, 1, 1, @EntityPascal, @Title, NULL, NULL, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM [system].[Role] r
    WHERE r.Name IN (
        N'کارشناسان مشاهده کلی گزارش تجمعی عملکرد/فعالیت EDMS',
        N'کارشناسان گزارش تجمعی عملکرد/فعالیت EDMS',
        N'مشاهده کلی HTS'
    )
      AND NOT EXISTS (
          SELECT 1 FROM [system].[RoleAccess] ra
          WHERE ra.RoleId = r.Id AND ra.Path = @PagePath AND ra.ActionAccessType = 1
      );

    IF (SELECT COUNT(*) FROM [system].[Role] r WHERE r.Name IN (
            N'کارشناسان مشاهده کلی گزارش تجمعی عملکرد/فعالیت EDMS',
            N'کارشناسان گزارش تجمعی عملکرد/فعالیت EDMS',
            N'مشاهده کلی HTS'
        )) < 3
        THROW 51642, N'یکی از سه نقش دسترسی گزارش تجمعی عملکرد پیدا نشد.', 1;

    ---------------------------------------------------------------------------
    -- منو: برگ گزارش در همان شاخه گزارشات که گزارش MDR هست
    ---------------------------------------------------------------------------
    DECLARE @MenuId BIGINT;
    DECLARE @Content NVARCHAR(MAX);
    SELECT TOP 1 @MenuId = Id, @Content = Content
    FROM [system].[SystemMenu]
    WHERE Name = N'AllMenus'
    ORDER BY Id;

    IF @MenuId IS NULL
        THROW 51643, N'منوی AllMenus یافت نشد.', 1;

    DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"گزارش تجمعی عملکرد/فعالیت","icon":"ki-chart-line ki-outline","iconColor":"#93C5FD","path":"/panel/edms/document/cumulativeperformancereport","a_attr":{"href":"/panel/edms/document/cumulativeperformancereport"},"data":{"iconColor":"#93C5FD","path":"/panel/edms/document/cumulativeperformancereport"},"children":[]}';
    IF ISJSON(@Leaf) <> 1
        THROW 51644, N'JSON برگ منو معتبر نیست.', 1;

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
            THROW 51645, N'شاخه گزارشات (کنار گزارش MDR) در منو پیدا نشد.', 1;

        SET @Content = JSON_MODIFY(@Content, N'append ' + @BestPath, JSON_QUERY(@Leaf));
        IF @Content IS NULL OR ISJSON(@Content) <> 1
            THROW 51646, N'Content منو بعد از درج معتبر نیست.', 1;

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
WHERE Name = N'Edms_CumulativePerformanceReport';

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName,
       JSON_VALUE(j.value, '$.Visible') AS Visible
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Edms_CumulativePerformanceReport';

SELECT r.Name AS RoleName, ra.Path, ra.ActionAccessType, ra.RowId, ra.EntityName
FROM [system].[RoleAccess] ra
INNER JOIN [system].[Role] r ON r.Id = ra.RoleId
WHERE ra.Path = N'/panel/edms/document/cumulativeperformancereport'
   OR (ra.ActionAccessType = 3
       AND ra.EntityName = N'Entities.App.Edms.CumulativePerformanceReport'
       AND ra.RowId = (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'Edms_CumulativePerformanceReport'));
