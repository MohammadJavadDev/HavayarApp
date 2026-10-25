/*
  Seed_Edms_DocumentDelayReport_DataProfile.sql
  گزارش تاخیرات مدارک — نمایه Edms_DocumentDelayReport
  معادل Vw_Edms_DocumentDelay، یک ردیف برای هر مدرک.
  ارسال = اولین تاریخ ارسال؛ پاسخ = اولین پاسخ کارفرما بعد از ارسال؛
  پاسخ ریویژن قبل = آخرین پاسخ. UTF-8 with BOM.
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_Edms_DocumentDelayReport_DataProfile.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-edms-document-delay';
    DECLARE @Name NVARCHAR(150) = N'Edms_DocumentDelayReport';
    DECLARE @Title NVARCHAR(200) = N'گزارش تاخیرات مدارک';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.edms.documentdelayreport';
    DECLARE @PagePath NVARCHAR(300) = N'/panel/edms/document/documentdelayreport';
    DECLARE @DirectRoleName NVARCHAR(200) = N'گزارش تاخیرات مدارک EDMS';
    DECLARE @DirectRoleTitle NVARCHAR(200) = N'گزارش تاخیرات مدارک EDMS';
    DECLARE @OwnRoleName NVARCHAR(200) = N'Edms.DocumentDelay.OwnProjects';
    DECLARE @OwnRoleTitle NVARCHAR(200) = N'محدوده پروژه‌های خود در گزارش تاخیرات مدارک';
    DECLARE @Sql NVARCHAR(MAX) = N'DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
DECLARE @CuRoles NVARCHAR(MAX) = N''[]'';
DECLARE @CuRoleIds NVARCHAR(MAX) = N''[]'';
DECLARE @IsAdmin BIT = 0;
DECLARE @OwnProjects BIT = 0;
SELECT
    @CuRoles = CASE WHEN ISJSON(u.Roles) = 1 THEN u.Roles ELSE N''[]'' END,
    @CuRoleIds = CASE WHEN ISJSON(u.RoleIds) = 1 THEN u.RoleIds ELSE N''[]'' END
FROM system.[User] AS u
WHERE u.Id = @CuId;
SET @IsAdmin = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoles) WHERE [value] = N''admin'') THEN 1 ELSE 0 END;
SET @OwnProjects = CASE
    WHEN @IsAdmin = 1 THEN 0
    WHEN EXISTS (
        SELECT 1
        FROM OPENJSON(@CuRoleIds) AS j
        INNER JOIN system.Role AS r ON r.Id = TRY_CAST(j.[value] AS BIGINT)
        WHERE r.Name = N''Edms.DocumentDelay.OwnProjects''
    ) THEN 1
    ELSE 0
END;
--!--mainsection
SELECT
    a.[t1_Id],
    a.[Project_Code],
    a.[Project_Name],
    a.[Project_Vpis_ID],
    a.[Document_Number],
    a.[Document_Title],
    a.[Revision],
    a.[AllowedHavayar_DelayDay],
    a.[AllowedEmployer_DelayDay],
    a.[First_Issue_Baseline],
    a.[SendToEmployer_Date],
    CASE WHEN a.[HavayarDelay] IS NULL OR a.[HavayarDelay] < 0 THEN 0 ELSE a.[HavayarDelay] END AS [HavayarDelay],
    CASE WHEN a.[Employer_Delay] IS NULL OR a.[Employer_Delay] < 0 THEN 0 ELSE a.[Employer_Delay] END AS [Employer_Delay],
    a.[Employer_ResponseDate]
FROM (
    SELECT
        CAST(d.Id AS nvarchar(30)) AS [t1_Id],
        p.Code AS [Project_Code],
        p.Name AS [Project_Name],
        v.HtsId AS [Project_Vpis_ID],
        v.Code AS [Document_Number],
        v.Title AS [Document_Title],
        d.Revision AS [Revision],
        p.LegalDelayHavayar AS [AllowedHavayar_DelayDay],
        p.LegalClientDayDelay AS [AllowedEmployer_DelayDay],
        COALESCE(
            NULLIF(LTRIM(RTRIM(v.FirstDegreeDocumentShamsiDate)), N''''),
            dbo.fn_MiladiToShamsi(v.FirstDegreeDocumentMiladiDate)
        ) AS [First_Issue_Baseline],
        CASE WHEN hs.SendMiladi IS NULL THEN NULL ELSE hs.SendShamsi END AS [SendToEmployer_Date],
        CASE
            WHEN d.Revision = 0 THEN DATEDIFF(DAY,
                CASE
                    WHEN v.ReplaceMiladiDate IS NULL THEN CAST(v.FirstDegreeDocumentMiladiDate AS date)
                    ELSE CAST(v.ReplaceMiladiDate AS date)
                END,
                ISNULL(hs.SendMiladi, CAST(GETDATE() AS date)))
            ELSE DATEDIFF(DAY, ps.ResponseMiladi, hs.SendMiladi) - ISNULL(p.LegalDelayHavayar, 14)
        END AS [HavayarDelay],
        DATEDIFF(DAY, hs.SendMiladi, ISNULL(es.ResponseMiladi, CAST(GETDATE() AS date)))
            - ISNULL(p.LegalClientDayDelay, 14) AS [Employer_Delay],
        CASE WHEN es.ResponseMiladi IS NULL THEN NULL ELSE es.ResponseShamsi END AS [Employer_ResponseDate]
    FROM [Edms].[Project] AS p
    INNER JOIN [Edms].[ProjectVpis] AS v ON v.ProjectNameId = p.Id
    INNER JOIN [Edms].[Document] AS d ON d.DocumentVpisId = v.Id
    OUTER APPLY (
        SELECT TOP 1
            x.SendShamsi,
            x.SendMiladi
        FROM (
            SELECT
                t.Id,
                LEFT(REPLACE(LTRIM(RTRIM(t.DeliveryDateToEmployer)), N''-'', N''/''), 10) AS SendShamsi,
                dbo.fn_ShamsiToMiladiDate(LEFT(REPLACE(LTRIM(RTRIM(t.DeliveryDateToEmployer)), N''-'', N''/''), 10), N''/'') AS SendMiladi
            FROM [Edms].[Transmital] AS t
            WHERE t.DocumentId = d.Id
        ) AS x
        WHERE x.SendMiladi IS NOT NULL
        ORDER BY x.SendMiladi, x.Id
    ) AS hs
    OUTER APPLY (
        SELECT TOP 1
            z.ResponseMiladi,
            z.ResponseShamsi
        FROM (
            SELECT
                c.Id,
                COALESCE(CAST(hc.CreatedDate AS date), CAST(c.CreatedOnMiladiDateTime AS date)) AS ResponseMiladi,
                COALESCE(
                    NULLIF(LTRIM(RTRIM(hc.CreatedDate_Shamsi)), N''''),
                    CASE
                        WHEN c.CreatedOnShamsiDateTime IS NULL OR LTRIM(RTRIM(c.CreatedOnShamsiDateTime)) = N'''' THEN dbo.fn_MiladiToShamsi(c.CreatedOnMiladiDateTime)
                        WHEN CHARINDEX(N'' '', LTRIM(RTRIM(c.CreatedOnShamsiDateTime))) > 0 THEN LEFT(LTRIM(RTRIM(c.CreatedOnShamsiDateTime)), CHARINDEX(N'' '', LTRIM(RTRIM(c.CreatedOnShamsiDateTime))) - 1)
                        ELSE LEFT(LTRIM(RTRIM(c.CreatedOnShamsiDateTime)), 10)
                    END
                ) AS ResponseShamsi
            FROM [Edms].[DocumentComment] AS c
            LEFT JOIN [TMS].[TotalSystem].[dbo].[Edms_Document_Comment] AS hc
                ON hc.Document_Comment_ID = c.HtsId AND c.HtsId > 0
            WHERE c.DocumentId = d.Id
              AND c.Status IN (12, 13, 14, 15)
        ) AS z
        WHERE hs.SendMiladi IS NULL
           OR z.ResponseMiladi >= hs.SendMiladi
           OR NOT EXISTS (
                SELECT 1
                FROM [Edms].[DocumentComment] AS c2
                LEFT JOIN [TMS].[TotalSystem].[dbo].[Edms_Document_Comment] AS hc2
                    ON hc2.Document_Comment_ID = c2.HtsId AND c2.HtsId > 0
                WHERE c2.DocumentId = d.Id
                  AND c2.Status IN (12, 13, 14, 15)
                  AND COALESCE(CAST(hc2.CreatedDate AS date), CAST(c2.CreatedOnMiladiDateTime AS date)) >= hs.SendMiladi
           )
        ORDER BY
            CASE WHEN hs.SendMiladi IS NULL OR z.ResponseMiladi >= hs.SendMiladi THEN 0 ELSE 1 END,
            z.ResponseMiladi,
            z.Id
    ) AS es
    OUTER APPLY (
        SELECT TOP 1
            COALESCE(CAST(hc.CreatedDate AS date), CAST(c.CreatedOnMiladiDateTime AS date)) AS ResponseMiladi
        FROM [Edms].[Document] AS d2
        INNER JOIN [Edms].[DocumentComment] AS c
            ON c.DocumentId = d2.Id
           AND c.Status IN (12, 13, 14, 15)
        LEFT JOIN [TMS].[TotalSystem].[dbo].[Edms_Document_Comment] AS hc
            ON hc.Document_Comment_ID = c.HtsId AND c.HtsId > 0
        WHERE d2.DocumentVpisId = d.DocumentVpisId
          AND d2.Revision = d.Revision - 1
        ORDER BY COALESCE(CAST(hc.CreatedDate AS date), CAST(c.CreatedOnMiladiDateTime AS date)) DESC, c.Id DESC
    ) AS ps
    WHERE (@OwnProjects = 0 OR p.ProjectManagerId = @CuId)
      AND NOT (
            ISNULL(d.HtsId, 0) = 0
            AND EXISTS (
                SELECT 1
                FROM [Edms].[Document] AS dDup
                WHERE dDup.DocumentVpisId = d.DocumentVpisId
                  AND dDup.Revision = d.Revision
                  AND dDup.Id <> d.Id
                  AND dDup.HtsId > 0
            )
      )
) AS a';
    DECLARE @Qj NVARCHAR(MAX) = N'{"Tables": [], "Relations": [], "Filters": [], "CustomConditions": [], "CustomQuery": "'
        + STRING_ESCAPE(@Sql, 'json')
        + N'", "Parameters": [], "Selects": null}';
    DECLARE @Cols NVARCHAR(MAX) = N'[{"TableName": "Edms.Document", "ColumnName": "t1_Id", "DisplayName": "شناسه", "Alliance": "t1_Id", "Address": "[d].[Id]", "SystemTypeName": "String", "SystemType": 0, "Visible": false, "PrimaryKey": true, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 90, "Filterable": false, "Sortable": false, "ClassName": ""}, {"TableName": "Edms.Project", "ColumnName": "Project_Code", "DisplayName": "کد پروژه", "Alliance": "Project_Code", "Address": "[p].[Code]", "SystemTypeName": "String", "SystemType": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": "ltr"}, {"TableName": "Edms.Project", "ColumnName": "Project_Name", "DisplayName": "نام پروژه", "Alliance": "Project_Name", "Address": "[p].[Name]", "SystemTypeName": "String", "SystemType": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 200, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.ProjectVpis", "ColumnName": "Project_Vpis_ID", "DisplayName": "شناسه Vpis", "Alliance": "Project_Vpis_ID", "Address": "[v].[HtsId]", "SystemTypeName": "Long", "SystemType": 6, "Visible": true, "PrimaryKey": false, "SortDirection": 0, "SortOrder": 0, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 100, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.ProjectVpis", "ColumnName": "Document_Number", "DisplayName": "کد مدرک", "Alliance": "Document_Number", "Address": "[v].[Code]", "SystemTypeName": "String", "SystemType": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 250, "Filterable": true, "Sortable": true, "ClassName": "ltr"}, {"TableName": "Edms.ProjectVpis", "ColumnName": "Document_Title", "DisplayName": "عنوان مدرک", "Alliance": "Document_Title", "Address": "[v].[Title]", "SystemTypeName": "String", "SystemType": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 250, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.Document", "ColumnName": "Revision", "DisplayName": "نسخه", "Alliance": "Revision", "Address": "[d].[Revision]", "SystemTypeName": "Int", "SystemType": 7, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.Project", "ColumnName": "AllowedHavayar_DelayDay", "DisplayName": "تاخیر مجاز هوایار", "Alliance": "AllowedHavayar_DelayDay", "Address": "[p].[LegalDelayHavayar]", "SystemTypeName": "Int", "SystemType": 7, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 150, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.Project", "ColumnName": "AllowedEmployer_DelayDay", "DisplayName": "تاخیر مجاز کارفرما", "Alliance": "AllowedEmployer_DelayDay", "Address": "[p].[LegalClientDayDelay]", "SystemTypeName": "Int", "SystemType": 7, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 150, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.ProjectVpis", "ColumnName": "First_Issue_Baseline", "DisplayName": "تاریخ مبنای اولین مدرک", "Alliance": "First_Issue_Baseline", "Address": "[v].[FirstDegreeDocumentShamsiDate]", "SystemTypeName": "DateShamsi", "SystemType": 5, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.Transmital", "ColumnName": "SendToEmployer_Date", "DisplayName": "تاریخ ارسال به کارفرما", "Alliance": "SendToEmployer_Date", "Address": "[t].[DeliveryDateToEmployer]", "SystemTypeName": "DateShamsi", "SystemType": 5, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.Document", "ColumnName": "HavayarDelay", "DisplayName": "تاخیر هوایار", "Alliance": "HavayarDelay", "Address": "[d].[Revision]", "SystemTypeName": "Int", "SystemType": 7, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 150, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.DocumentComment", "ColumnName": "Employer_Delay", "DisplayName": "تاخیر کافرما", "Alliance": "Employer_Delay", "Address": "[c].[CreatedOnMiladiDateTime]", "SystemTypeName": "Int", "SystemType": 7, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 150, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.DocumentComment", "ColumnName": "Employer_ResponseDate", "DisplayName": "تاریخ پاسخ کارفرما", "Alliance": "Employer_ResponseDate", "Address": "[c].[CreatedOnMiladiDateTime]", "SystemTypeName": "DateShamsi", "SystemType": 5, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": ""}]';
    DECLARE @Act NVARCHAR(MAX) = N'[{"dataActionName": "new", "enable": false, "title": "جدید"}, {"dataActionName": "edit", "enable": false, "title": "ویرایش"}, {"dataActionName": "delete", "enable": false, "title": "حذف"}, {"dataActionName": "exportExcell", "enable": true, "title": "خروجی اکسل"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @Pid BIGINT;
    DECLARE @DirectRoleId BIGINT;
    DECLARE @OwnRoleId BIGINT;

    IF ISJSON(@Qj) <> 1 OR ISJSON(@Cols) <> 1 OR ISJSON(@Act) <> 1
        THROW 51690, N'JSON نمایه گزارش تاخیرات مدارک معتبر نیست.', 1;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
    BEGIN
        UPDATE system.SavedQuery
        SET Title = @Title,
            QueryJson = @Qj,
            ColumnsJson = @Cols,
            EntityFullName = @Ent,
            Mode = 1,
            Type = 1,
            ActionOptions = @Act,
            CustomActionButtonsJson = N'[]',
            EventScriptsJson = @EventScripts,
            DiagramJson = N'{}',
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Name = @Name;
        SELECT @Pid = Id FROM system.SavedQuery WHERE Name = @Name;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@Name, @Title, @Qj, @Cols, N'{}', N'[]', @EventScripts,
             1, 1, @Ent, @Act,
             1, 1, @SeedUser, @SeedUser,
             @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @Pid = SCOPE_IDENTITY();
    END

    IF EXISTS (SELECT 1 FROM system.Role WHERE Name = @DirectRoleName)
        SELECT @DirectRoleId = Id FROM system.Role WHERE Name = @DirectRoleName;
    ELSE
    BEGIN
        INSERT INTO system.Role
            (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
             CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime,
             CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@DirectRoleName, @DirectRoleTitle, 1, @SeedUser, 1, @SeedUser,
             @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET @DirectRoleId = SCOPE_IDENTITY();
    END

    IF EXISTS (SELECT 1 FROM system.Role WHERE Name = @OwnRoleName)
        SELECT @OwnRoleId = Id FROM system.Role WHERE Name = @OwnRoleName;
    ELSE
    BEGIN
        INSERT INTO system.Role
            (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
             CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime,
             CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@OwnRoleName, @OwnRoleTitle, 1, @SeedUser, 1, @SeedUser,
             @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET @OwnRoleId = SCOPE_IDENTITY();
    END

    DECLARE @Roles TABLE (RoleName NVARCHAR(200) NOT NULL PRIMARY KEY);
    INSERT INTO @Roles (RoleName) VALUES
        (N'admin'),
        (N'ShowAllMenus'),
        (N'مدیران پروژه در EDMS'),
        (N'کارشناسان گزارشات جامع سیستم EDMS'),
        (N'مشاهده کلی HTS'),
        (N'کارشناسان سیستم مهندسی دارای دسترسی های عمومی سیستم مهندسی'),
        (@DirectRoleName);

    DELETE ra
    FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    INNER JOIN @Roles n ON n.RoleName = r.Name
    WHERE ra.Path = @PagePath;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT @PagePath, 1, 1, N'Entities.App.Edms.Document', @Title, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    INNER JOIN @Roles n ON n.RoleName = r.Name;

    DELETE ra
    FROM system.RoleAccess ra
    WHERE ra.ActionAccessType = 3 AND ra.RowId = @Pid;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, @Ent, @Title, NULL, @Pid, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    INNER JOIN @Roles n ON n.RoleName = r.Name;

    DECLARE @Assign TABLE (
        Username NVARCHAR(200) NOT NULL,
        RoleId BIGINT NOT NULL,
        RoleName NVARCHAR(200) NOT NULL
    );
    INSERT INTO @Assign (Username, RoleId, RoleName)
    SELECT v.Username, @DirectRoleId, @DirectRoleName
    FROM (VALUES (N'Bayat.f'), (N'Fekri.f'), (N'Hazrati.z'), (N'sohani.a')) AS v(Username);
    INSERT INTO @Assign (Username, RoleId, RoleName)
    SELECT v.Username, @OwnRoleId, @OwnRoleName
    FROM (VALUES (N'Farahmand.m'), (N'habibi.m'), (N'Joula.m')) AS v(Username);

    DECLARE @MapUsername NVARCHAR(200);
    DECLARE @MapRoleId BIGINT;
    DECLARE @MapRoleName NVARCHAR(200);
    DECLARE @MapUserId BIGINT;
    DECLARE @Assigned INT = 0;

    DECLARE map_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Username, RoleId, RoleName FROM @Assign;
    OPEN map_cur;
    FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleId, @MapRoleName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @MapUserId = NULL;
        SELECT TOP 1 @MapUserId = Id
        FROM system.[User]
        WHERE LOWER(Username) = LOWER(@MapUsername)
        ORDER BY CASE WHEN ISNULL(IsActive, 0) = 1 THEN 0 ELSE 1 END, Id;

        IF @MapUserId IS NULL
            PRINT N'  MISSING USER (skip): ' + @MapUsername;
        ELSE IF EXISTS (
            SELECT 1 FROM system.[User] u
            CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
            WHERE u.Id = @MapUserId AND TRY_CAST(j.[value] AS bigint) = @MapRoleId
        )
            PRINT N'  already assigned: ' + @MapUsername + N' / ' + @MapRoleName;
        ELSE
        BEGIN
            UPDATE system.[User]
            SET RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                        THEN N'[' + CAST(@MapRoleId AS nvarchar(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@MapRoleId AS nvarchar(20)) + N']')
                END,
                Roles = CASE
                    WHEN ISJSON(Roles) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(Roles) j WHERE j.[value] = @MapRoleName) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]'
                        THEN N'["' + @MapRoleName + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @MapRoleName + N'"]')
                END,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @MapUserId;
            SET @Assigned = @Assigned + 1;
        END
        FETCH NEXT FROM map_cur INTO @MapUsername, @MapRoleId, @MapRoleName;
    END
    CLOSE map_cur;
    DEALLOCATE map_cur;
    PRINT N'  Users newly assigned: ' + CAST(@Assigned AS nvarchar(20));

    DECLARE @LeafEng NVARCHAR(MAX) = N'{"text":"گزارش تاخیرات مدارک","icon":"ki-calendar ki-outline","iconColor":"#d78819","path":"/panel/edms/document/documentdelayreport","a_attr":{"href":"/panel/edms/document/documentdelayreport"},"data":{"iconColor":"#d78819","path":"/panel/edms/document/documentdelayreport"},"children":[]}';
    DECLARE @LeafAll NVARCHAR(MAX) = N'{"text":"گزارش تاخیرات مدارک","icon":"ki-calendar ki-outline","iconColor":"#93C5FD","path":"/panel/edms/document/documentdelayreport","a_attr":{"href":"/panel/edms/document/documentdelayreport"},"data":{"iconColor":"#93C5FD","path":"/panel/edms/document/documentdelayreport"},"children":[]}';
    DECLARE @MenuName NVARCHAR(100);
    DECLARE @L1 NVARCHAR(200);
    DECLARE @L2 NVARCHAR(200);
    DECLARE @L3 NVARCHAR(200);
    DECLARE @Leaf NVARCHAR(MAX);
    DECLARE @MenuId BIGINT;
    DECLARE @Content NVARCHAR(MAX);
    DECLARE @Idx1 INT;
    DECLARE @Idx2 INT;
    DECLARE @Idx3 INT;
    DECLARE @Children NVARCHAR(MAX);
    DECLARE @Grand NVARCHAR(MAX);

    DECLARE menu_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT MenuName, L1, L2, L3, Leaf
        FROM (VALUES
            (N'eng', N'گزارشات', NULL, NULL, @LeafEng),
            (N'AllMenus', N'مهندسی', N'مهندسی پروژه', N'گزارشات', @LeafAll)
        ) AS m(MenuName, L1, L2, L3, Leaf);
    OPEN menu_cur;
    FETCH NEXT FROM menu_cur INTO @MenuName, @L1, @L2, @L3, @Leaf;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @MenuId = NULL;
        SET @Content = NULL;
        SELECT TOP 1 @MenuId = Id, @Content = Content
        FROM system.SystemMenu
        WHERE Name = @MenuName
        ORDER BY Id;

        IF @MenuId IS NULL
            PRINT N'  WARN menu missing: ' + @MenuName;
        ELSE IF CHARINDEX(N'"path":"' + @PagePath + N'"', @Content) > 0
            PRINT N'  menu leaf exists: ' + @MenuName;
        ELSE IF ISJSON(@Content) <> 1 OR ISJSON(@Leaf) <> 1
            THROW 51691, N'Content منو یا برگه JSON معتبر نیست', 1;
        ELSE
        BEGIN
            SET @Idx1 = NULL;
            SELECT TOP 1 @Idx1 = CAST([key] AS INT)
            FROM OPENJSON(@Content)
            WHERE JSON_VALUE([value], '$.text') = @L1
            ORDER BY CAST([key] AS INT);

            IF @Idx1 IS NULL
                PRINT N'  WARN root missing: ' + @MenuName + N' / ' + @L1;
            ELSE IF @L2 IS NULL
            BEGIN
                SET @Content = JSON_MODIFY(@Content,
                    N'append $[' + CAST(@Idx1 AS nvarchar(10)) + N'].children',
                    JSON_QUERY(@Leaf));
                UPDATE system.SystemMenu
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
                SET @Children = JSON_QUERY(@Content, '$[' + CAST(@Idx1 AS nvarchar(10)) + '].children');
                SET @Idx2 = NULL;
                SELECT TOP 1 @Idx2 = CAST([key] AS INT)
                FROM OPENJSON(@Children)
                WHERE JSON_VALUE([value], '$.text') = @L2;

                IF @Idx2 IS NULL
                    PRINT N'  WARN mid missing: ' + @MenuName + N' / ' + @L2;
                ELSE IF @L3 IS NULL
                BEGIN
                    SET @Content = JSON_MODIFY(@Content,
                        N'append $[' + CAST(@Idx1 AS nvarchar(10)) + N'].children[' + CAST(@Idx2 AS nvarchar(10)) + N'].children',
                        JSON_QUERY(@Leaf));
                    UPDATE system.SystemMenu
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
                    SET @Grand = JSON_QUERY(@Content, '$[' + CAST(@Idx1 AS nvarchar(10)) + N'].children[' + CAST(@Idx2 AS nvarchar(10)) + N'].children');
                    SET @Idx3 = NULL;
                    SELECT TOP 1 @Idx3 = CAST([key] AS INT)
                    FROM OPENJSON(@Grand)
                    WHERE JSON_VALUE([value], '$.text') = @L3;

                    IF @Idx3 IS NULL
                        PRINT N'  WARN child missing: ' + @MenuName + N' / ' + @L3;
                    ELSE
                    BEGIN
                        SET @Content = JSON_MODIFY(@Content,
                            N'append $[' + CAST(@Idx1 AS nvarchar(10)) + N'].children[' + CAST(@Idx2 AS nvarchar(10)) + N'].children[' + CAST(@Idx3 AS nvarchar(10)) + N'].children',
                            JSON_QUERY(@Leaf));
                        UPDATE system.SystemMenu
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
        END
        FETCH NEXT FROM menu_cur INTO @MenuName, @L1, @L2, @L3, @Leaf;
    END
    CLOSE menu_cur;
    DEALLOCATE menu_cur;

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
FROM system.SavedQuery q
WHERE q.Name = N'Edms_DocumentDelayReport';

SELECT JSON_VALUE(j.[value], '$.Alliance') AS Alliance,
       JSON_VALUE(j.[value], '$.DisplayName') AS DisplayName
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Edms_DocumentDelayReport';
