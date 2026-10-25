/*
  Seed_Edms_ProjectAttachmentArchive_DataProfile.sql
  آرشیو اسناد ورودی پروژه — نمایه Edms_Project_Attachment_Archive
  فقط جداول هوایار (Edms.ProjectAttachment، Edms.Project، Edms.ProjectUser، FileEntity). UTF-8 with BOM.
  sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_Edms_ProjectAttachmentArchive_DataProfile.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-edms-project-attachment-archive';
    DECLARE @Name NVARCHAR(150) = N'Edms_Project_Attachment_Archive';
    DECLARE @Title NVARCHAR(200) = N'آرشیو اسناد ورودی پروژه';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.edms.projectattachment';
    DECLARE @Pascal NVARCHAR(200) = N'Entities.App.Edms.ProjectAttachment';
    DECLARE @PagePath NVARCHAR(300) = N'/panel/edms/project/projectattachmentarchive';
    DECLARE @SpecRoleName NVARCHAR(200) = N'دسترسی فقط به نوع مدرک SPEC در آرشیو اسناد ورودی';
    DECLARE @SpecRoleTitle NVARCHAR(200) = N'دسترسی فقط به نوع مدرک SPEC در آرشیو اسناد ورودی';
    DECLARE @SpecRoleId BIGINT = 279075;
    DECLARE @Qj NVARCHAR(MAX) = N'{"Tables": [], "Relations": [], "Filters": [], "CustomConditions": [], "CustomQuery": "DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);\nDECLARE @CuRoles NVARCHAR(MAX) = N''[]'';\nDECLARE @CuRoleIds NVARCHAR(MAX) = N''[]'';\nDECLARE @IsAdmin BIT = 0;\nDECLARE @OnlySpec BIT = 0;\nDECLARE @HasProjectLimit BIT = 0;\nSELECT\n    @CuRoles = CASE WHEN ISJSON(u.Roles) = 1 THEN u.Roles ELSE N''[]'' END,\n    @CuRoleIds = CASE WHEN ISJSON(u.RoleIds) = 1 THEN u.RoleIds ELSE N''[]'' END\nFROM system.[User] AS u\nWHERE u.Id = @CuId;\nSET @IsAdmin = CASE WHEN EXISTS (SELECT 1 FROM OPENJSON(@CuRoles) WHERE [value] = N''admin'') THEN 1 ELSE 0 END;\nSET @OnlySpec = CASE WHEN @IsAdmin = 1 THEN 0 WHEN EXISTS (\n    SELECT 1\n    FROM OPENJSON(@CuRoleIds) AS j\n    INNER JOIN system.Role AS r ON r.Id = TRY_CAST(j.[value] AS BIGINT)\n    WHERE r.Name = N''دسترسی فقط به نوع مدرک SPEC در آرشیو اسناد ورودی''\n) THEN 1 ELSE 0 END;\nSET @HasProjectLimit = CASE\n    WHEN @IsAdmin = 1 OR @CuId IS NULL THEN 0\n    WHEN EXISTS (\n        SELECT 1\n        FROM [Edms].[ProjectUser] AS pu\n        WHERE pu.UserId = @CuId\n          AND pu.UserIsSupervisor = 0\n          AND ISNULL(pu.IsActive, 1) = 1\n    ) THEN 1\n    ELSE 0\nEND;\n--!--mainsection\nSELECT\n    N''A'' + CAST(pa.Id AS nvarchar(30)) AS [t1_Id],\n    CAST(fe.Id AS bigint) AS [FileId],\n    p.Code AS [Project_Code],\n    p.Name AS [Project_Name],\n    CASE pa.[Type]\n        WHEN 257 THEN N''پروپزال''\n        WHEN 258 THEN N''متره''\n        WHEN 259 THEN N''Spec''\n        WHEN 260 THEN N''MOM''\n        WHEN 261 THEN N''سفارش ساخت''\n        WHEN 262 THEN N''قرارداد''\n        WHEN 471 THEN N''فرمت مدارک''\n        ELSE N''''\n    END AS [DocType],\n    COALESCE(NULLIF(LTRIM(RTRIM(pa.DocumentCode)), N''''),\n        CASE\n            WHEN fe.OriginalName IS NULL OR CHARINDEX(N''.'', fe.OriginalName) = 0 THEN fe.OriginalName\n            ELSE LEFT(fe.OriginalName, LEN(fe.OriginalName) - CHARINDEX(N''.'', REVERSE(fe.OriginalName)))\n        END) AS [DocumentCode],\n    COALESCE(NULLIF(LTRIM(RTRIM(pa.DocumentName)), N''''),\n        CASE\n            WHEN fe.OriginalName IS NULL OR CHARINDEX(N''.'', fe.OriginalName) = 0 THEN fe.OriginalName\n            ELSE LEFT(fe.OriginalName, LEN(fe.OriginalName) - CHARINDEX(N''.'', REVERSE(fe.OriginalName)))\n        END) AS [DocumentName],\n    CAST(fe.Size AS bigint) AS [FileSize],\n    COALESCE(NULLIF(LTRIM(RTRIM(pa.CreatedByName)), N''''), NULLIF(LTRIM(RTRIM(cu.NameFa)), N''''), NULLIF(LTRIM(RTRIM(cu.Name)), N''''), NULLIF(LTRIM(RTRIM(fu.NameFa)), N''''), NULLIF(LTRIM(RTRIM(fu.Name)), N''''), NULLIF(LTRIM(RTRIM(fe.CreatedByName)), N'''')) AS [FullName],\n    CASE\n        WHEN COALESCE(NULLIF(LTRIM(RTRIM(pa.CreatedOnShamsiDateTime)), N''''), NULLIF(LTRIM(RTRIM(fe.CreatedOnShamsiDateTime)), N'''')) IS NULL OR LTRIM(RTRIM(COALESCE(NULLIF(LTRIM(RTRIM(pa.CreatedOnShamsiDateTime)), N''''), NULLIF(LTRIM(RTRIM(fe.CreatedOnShamsiDateTime)), N'''')))) = N'''' THEN NULL\n        WHEN CHARINDEX(N'' '', COALESCE(NULLIF(LTRIM(RTRIM(pa.CreatedOnShamsiDateTime)), N''''), NULLIF(LTRIM(RTRIM(fe.CreatedOnShamsiDateTime)), N''''))) > 0 THEN LEFT(COALESCE(NULLIF(LTRIM(RTRIM(pa.CreatedOnShamsiDateTime)), N''''), NULLIF(LTRIM(RTRIM(fe.CreatedOnShamsiDateTime)), N'''')), CHARINDEX(N'' '', COALESCE(NULLIF(LTRIM(RTRIM(pa.CreatedOnShamsiDateTime)), N''''), NULLIF(LTRIM(RTRIM(fe.CreatedOnShamsiDateTime)), N''''))) - 1)\n        ELSE COALESCE(NULLIF(LTRIM(RTRIM(pa.CreatedOnShamsiDateTime)), N''''), NULLIF(LTRIM(RTRIM(fe.CreatedOnShamsiDateTime)), N''''))\n    END AS [CreatedDate],\n    CASE\n        WHEN COALESCE(NULLIF(LTRIM(RTRIM(pa.CreatedOnShamsiDateTime)), N''''), NULLIF(LTRIM(RTRIM(fe.CreatedOnShamsiDateTime)), N'''')) IS NULL OR CHARINDEX(N'' '', COALESCE(NULLIF(LTRIM(RTRIM(pa.CreatedOnShamsiDateTime)), N''''), NULLIF(LTRIM(RTRIM(fe.CreatedOnShamsiDateTime)), N''''))) = 0 THEN NULL\n        ELSE LEFT(LTRIM(SUBSTRING(COALESCE(NULLIF(LTRIM(RTRIM(pa.CreatedOnShamsiDateTime)), N''''), NULLIF(LTRIM(RTRIM(fe.CreatedOnShamsiDateTime)), N'''')), CHARINDEX(N'' '', COALESCE(NULLIF(LTRIM(RTRIM(pa.CreatedOnShamsiDateTime)), N''''), NULLIF(LTRIM(RTRIM(fe.CreatedOnShamsiDateTime)), N''''))) + 1, 8)), 5)\n    END AS [CreatedTime],\n    pa.Comment AS [Comment]\nFROM [Edms].[ProjectAttachment] AS pa\nINNER JOIN [Edms].[Project] AS p ON p.Id = pa.ProjectId\nLEFT JOIN [dbo].[FileEntity] AS fe ON fe.Id = pa.AttachmentId\nLEFT JOIN system.[User] AS cu ON cu.Id = pa.CreatedById\nLEFT JOIN system.[User] AS fu ON NULLIF(LTRIM(RTRIM(fe.CreatedByName)), N'''') IS NOT NULL AND LOWER(LTRIM(RTRIM(fu.Username))) = LOWER(LTRIM(RTRIM(fe.CreatedByName)))\nWHERE ISNULL(pa.IsActive, 1) <> 2\n  AND (\n        @IsAdmin = 1\n        OR (\n            (\n                @OnlySpec = 0\n                OR pa.[Type] = 259\n                OR p.ProjectManagerId = @CuId\n            )\n            AND (\n                @HasProjectLimit = 0\n                OR p.ProjectManagerId = @CuId\n                OR EXISTS (\n                    SELECT 1\n                    FROM [Edms].[ProjectUser] AS pu\n                    WHERE pu.UserId = @CuId\n                      AND pu.UserIsSupervisor = 0\n                      AND ISNULL(pu.IsActive, 1) = 1\n                      AND pu.ProjectId = p.Id\n                )\n            )\n        )\n    )\n", "Parameters": [], "Selects": null}';
    DECLARE @Cols NVARCHAR(MAX) = N'[{"TableName": "Edms.ProjectAttachment", "ColumnName": "t1_Id", "DisplayName": "شناسه", "Alliance": "t1_Id", "Address": "N''A'' + CAST([pa].[Id] AS nvarchar(30))", "SystemTypeName": "String", "SystemType": 0, "Visible": false, "PrimaryKey": true, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 90, "Filterable": false, "Sortable": false, "ClassName": ""}, {"TableName": "dbo.FileEntity", "ColumnName": "FileId", "DisplayName": "شناسه فایل", "Alliance": "FileId", "Address": "CAST([fe].[Id] AS bigint)", "SystemTypeName": "Long", "SystemType": 6, "Visible": false, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 80, "Filterable": false, "Sortable": false, "ClassName": ""}, {"TableName": "Edms.Project", "ColumnName": "Project_Code", "DisplayName": "کد پروژه", "Alliance": "Project_Code", "Address": "[p].[Code]", "SystemTypeName": "String", "SystemType": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": "ltr"}, {"TableName": "Edms.Project", "ColumnName": "Project_Name", "DisplayName": "نام پروژه", "Alliance": "Project_Name", "Address": "[p].[Name]", "SystemTypeName": "String", "SystemType": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 250, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.ProjectAttachment", "ColumnName": "DocType", "DisplayName": "نوع مدرک", "Alliance": "DocType", "Address": "[pa].[Type]", "SystemTypeName": "String", "SystemType": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 150, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.ProjectAttachment", "ColumnName": "DocumentCode", "DisplayName": "کد سند", "Alliance": "DocumentCode", "Address": "[pa].[DocumentCode]", "SystemTypeName": "String", "SystemType": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 300, "Filterable": true, "Sortable": true, "ClassName": "ltr"}, {"TableName": "Edms.ProjectAttachment", "ColumnName": "DocumentName", "DisplayName": "نام سند", "Alliance": "DocumentName", "Address": "[pa].[DocumentName]", "SystemTypeName": "String", "SystemType": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 350, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "dbo.FileEntity", "ColumnName": "FileSize", "DisplayName": "حجم فایل", "Alliance": "FileSize", "Address": "CAST([fe].[Size] AS bigint)", "SystemTypeName": "Long", "SystemType": 6, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": "function(data, type, row) { if (data === 0 || data == null || data === '''') return ''''; var bytes = Number(data); if (!isFinite(bytes) || bytes <= 0) return ''''; var k = 1000; var sizes = [''Bytes'', ''KB'', ''MB'', ''GB'', ''TB'', ''PB'', ''EB'', ''ZB'', ''YB'']; var i = Math.floor(Math.log(bytes) / Math.log(k)); if (i < 0) i = 0; if (i >= sizes.length) i = sizes.length - 1; return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + '' '' + sizes[i]; }", "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 90, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.ProjectAttachment", "ColumnName": "FullName", "DisplayName": "کاربر", "Alliance": "FullName", "Address": "[pa].[CreatedByName]", "SystemTypeName": "String", "SystemType": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 150, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.ProjectAttachment", "ColumnName": "CreatedDate", "DisplayName": "تاریخ ایجاد", "Alliance": "CreatedDate", "Address": "[pa].[CreatedOnShamsiDateTime]", "SystemTypeName": "DateShamsi", "SystemType": 5, "Visible": true, "PrimaryKey": false, "SortDirection": 1, "SortOrder": 0, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 100, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.ProjectAttachment", "ColumnName": "CreatedTime", "DisplayName": "زمان ایجاد", "Alliance": "CreatedTime", "Address": "[pa].[CreatedOnShamsiDateTime]", "SystemTypeName": "String", "SystemType": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 100, "Filterable": true, "Sortable": true, "ClassName": "ltr"}, {"TableName": "Edms.ProjectAttachment", "ColumnName": "Comment", "DisplayName": "توضیحات پیوست", "Alliance": "Comment", "Address": "[pa].[Comment]", "SystemTypeName": "String", "SystemType": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "SelectIndex": 0, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 100, "Filterable": true, "Sortable": true, "ClassName": ""}]';
    DECLARE @Btn NVARCHAR(MAX) = N'[{"id": "edms_view_project_attachment", "title": "مشاهده", "dataActionName": "viewAttachment", "colorClass": "btn-color-primary", "iconClass": "ki-eye ki-outline", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var row = (ctx && ctx.selectedRow) || {};\n    var fileId = row.fileId != null && row.fileId !== '''' ? row.fileId : row.FileId;\n    if (fileId == null || fileId === '''') {\n        toastr.error(''فایل پیوست برای این ردیف در سیستم جدید نیست'');\n        return;\n    }\n    window.open(''/File/download/'' + fileId, ''_blank'');\n}"}]';
    DECLARE @Act NVARCHAR(MAX) = N'[{"dataActionName": "new", "enable": false, "title": "جدید"}, {"dataActionName": "edit", "enable": false, "title": "ویرایش"}, {"dataActionName": "delete", "enable": false, "title": "حذف"}, {"dataActionName": "exportExcell", "enable": true, "title": "خروجی اکسل"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @Pid BIGINT;

    IF ISJSON(@Qj) <> 1 OR ISJSON(@Cols) <> 1 OR ISJSON(@Btn) <> 1 OR ISJSON(@Act) <> 1
        THROW 51679, N'JSON نمایه آرشیو اسناد ورودی معتبر نیست.', 1;

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
            CustomActionButtonsJson = @Btn,
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
            (@Name, @Title, @Qj, @Cols, N'{}', @Btn, @EventScripts,
             1, 1, @Ent, @Act,
             1, 1, @SeedUser, @SeedUser,
             @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @Pid = SCOPE_IDENTITY();
    END

    UPDATE system.SavedQuery
    SET Title = N'پیوست پروژه',
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = N'projectattachment_listinfo'
      AND Title = N'آرشیو اسناد ورودی پروژه';

    UPDATE system.RoleAccess
    SET DisplayName = N'پیوست پروژه',
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE ActionAccessType = 3
      AND RowId = (SELECT Id FROM system.SavedQuery WHERE Name = N'projectattachment_listinfo')
      AND DisplayName = N'آرشیو اسناد ورودی پروژه';

    IF EXISTS (SELECT 1 FROM system.Role WHERE Name = @SpecRoleName)
        SELECT @SpecRoleId = Id FROM system.Role WHERE Name = @SpecRoleName;
    ELSE IF EXISTS (SELECT 1 FROM system.Role WHERE Id = @SpecRoleId)
    BEGIN
        INSERT INTO system.Role
            (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
             CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime,
             CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@SpecRoleName, @SpecRoleTitle, 1, @SeedUser, 1, @SeedUser,
             @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET @SpecRoleId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        SET IDENTITY_INSERT system.Role ON;
        INSERT INTO system.Role
            (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
             CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime,
             CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@SpecRoleId, @SpecRoleName, @SpecRoleTitle, 1, @SeedUser, 1, @SeedUser,
             @Now, @Now, @NowShamsi, @NowShamsi, 1);
        SET IDENTITY_INSERT system.Role OFF;
    END

    DECLARE @Roles TABLE (RoleName NVARCHAR(200) NOT NULL PRIMARY KEY);
    INSERT INTO @Roles (RoleName) VALUES
        (N'admin'),
        (N'ShowAllMenus'),
        (N'کارشناسان بارگذاری اسناد EDMS'),
        (N'کارشناسان آرشیو اسناد ورودی پروژه EDMS'),
        (N'مشاهده کلی HTS'),
        (N'کارشناسان سیستم مهندسی دارای دسترسی های عمومی سیستم مهندسی'),
        (@SpecRoleName);

    DELETE ra
    FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    INNER JOIN @Roles n ON n.RoleName = r.Name
    WHERE ra.Path = @PagePath;

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT @PagePath, 1, 1, N'Entities.App.Edms.Project', @Title, NULL, NULL, r.Id,
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

    DECLARE @MapUsername NVARCHAR(200);
    DECLARE @MapUserId BIGINT;
    DECLARE @Assigned INT = 0;
    DECLARE spec_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT v.Username
        FROM (VALUES (N'Techwin.Hanwha'), (N'Kwangshin.co'), (N'kavosh.sanat')) AS v(Username);
    OPEN spec_cur;
    FETCH NEXT FROM spec_cur INTO @MapUsername;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @MapUserId = NULL;
        SELECT TOP 1 @MapUserId = Id
        FROM system.[User]
        WHERE LOWER(Username) = LOWER(@MapUsername)
          AND ISNULL(IsActive, 0) = 1;

        IF @MapUserId IS NULL
            PRINT N'  MISSING USER (skip): ' + @MapUsername;
        ELSE IF EXISTS (
            SELECT 1 FROM system.[User] u
            CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
            WHERE u.Id = @MapUserId AND TRY_CAST(j.[value] AS bigint) = @SpecRoleId
        )
            PRINT N'  already assigned: ' + @MapUsername;
        ELSE
        BEGIN
            UPDATE system.[User]
            SET RoleIds = CASE
                    WHEN RoleIds IS NULL OR LTRIM(RTRIM(RoleIds)) = N'' OR RoleIds = N'[]'
                        THEN N'[' + CAST(@SpecRoleId AS nvarchar(20)) + N']'
                    ELSE STUFF(RoleIds, LEN(RoleIds), 1, N',' + CAST(@SpecRoleId AS nvarchar(20)) + N']')
                END,
                Roles = CASE
                    WHEN ISJSON(Roles) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(Roles) j WHERE j.[value] = @SpecRoleName) THEN Roles
                    WHEN Roles IS NULL OR LTRIM(RTRIM(Roles)) = N'' OR Roles = N'[]'
                        THEN N'["' + @SpecRoleName + N'"]'
                    ELSE STUFF(Roles, LEN(Roles), 1, N',"' + @SpecRoleName + N'"]')
                END,
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Id = @MapUserId;
            SET @Assigned = @Assigned + 1;
        END
        FETCH NEXT FROM spec_cur INTO @MapUsername;
    END
    CLOSE spec_cur;
    DEALLOCATE spec_cur;
    PRINT N'  Spec users newly assigned: ' + CAST(@Assigned AS nvarchar(20));

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
             N'{"text":"آرشیو اسناد ورودی پروژه","icon":"ki-folder ki-outline","iconColor":"#93C5FD","path":"/panel/edms/project/projectattachmentarchive","a_attr":{"href":"/panel/edms/project/projectattachmentarchive"},"data":{"iconColor":"#93C5FD","path":"/panel/edms/project/projectattachmentarchive"},"children":[]}'),
            (N'eng', N'پروژه', NULL,
             N'{"text":"آرشیو اسناد ورودی پروژه","icon":"bi-folder2-open bi","iconColor":"#1fe57f","path":"/panel/edms/project/projectattachmentarchive","a_attr":{"href":"/panel/edms/project/projectattachmentarchive"},"data":{"iconColor":"#1fe57f","path":"/panel/edms/project/projectattachmentarchive"},"children":[]}')
        ) AS m(MenuName, RootText, MidText, Leaf);
    OPEN menu_cur;
    FETCH NEXT FROM menu_cur INTO @MenuName, @RootText, @MidText, @Leaf;
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
        ELSE IF ISJSON(@Content) <> 1
            THROW 51680, N'Content منو JSON معتبر نیست', 1;
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
        FETCH NEXT FROM menu_cur INTO @MenuName, @RootText, @MidText, @Leaf;
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
WHERE q.Name IN (N'Edms_Project_Attachment_Archive', N'projectattachment_listinfo');

SELECT JSON_VALUE(j.[value], '$.Alliance') AS Alliance,
       JSON_VALUE(j.[value], '$.DisplayName') AS DisplayName
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Edms_Project_Attachment_Archive'
ORDER BY CAST(j.[key] AS int);
