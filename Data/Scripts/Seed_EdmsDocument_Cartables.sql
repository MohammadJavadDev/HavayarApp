/*
  کارتابل‌های مدرک. کارتابل‌های قبلی همین موجودیت حذف می‌شوند.
  UTF-8 with BOM. sqlcmd -f 65001 -x
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-edms-document-cartables';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.edms.document';
    DECLARE @Pascal NVARCHAR(200) = N'Entities.App.Edms.Document';
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
    DECLARE @OnRow NVARCHAR(MAX) = N'function(ctx) {
    var data = ctx.rowData || {};
    var status = parseInt(data.t1_StatusId, 10);
    var color = null;
    var text = "#111111";
    if (status === 14) color = "#0FF135";
    else if (status === 13) { color = "#077319"; text = "#ffffff"; }
    else if (status === 4 || status === 7 || status === 9) { color = "#6A935B"; text = "#ffffff"; }
    else if (status === 5 || status === 8 || status === 11 || status === 15 || status === 25 || status === 34) { color = "#6414CE"; text = "#ffffff"; }
    else if (status === 21) color = "#F573CA";
    else if (status === 3 || status === 6 || status === 10 || status === 12 || status === 26) { color = "#E01629"; text = "#ffffff"; }
    else if (status === 2 || status === 30) color = "#70C8F0";
    if (!color || !ctx.table) return;
    var colIdx = -1;
    ctx.table.columns().every(function (i) {
        if (this.dataSrc() === "_rowNumber") colIdx = i;
    });
    var row = ctx.$row;
    var node = colIdx >= 0 ? ctx.table.cell(row, colIdx).node() : (row && row.find ? row.find("td").get(0) : null);
    if (!node) return;
    node.style.setProperty("background-color", color, "important");
    node.style.setProperty("box-shadow", "inset 0 0 0 9999px " + color, "important");
    node.style.setProperty("color", text, "important");
}';
    DECLARE @EventScripts NVARCHAR(MAX) = (SELECT N'' AS onSelectedRow, @OnRow AS onRowAdded FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    DECLARE @Name NVARCHAR(100);
    DECLARE @Title NVARCHAR(200);
    DECLARE @Qj NVARCHAR(MAX);
    DECLARE @Btn NVARCHAR(MAX);
    DECLARE @Act NVARCHAR(MAX);
    DECLARE @Pid BIGINT;

    DELETE ra
    FROM system.RoleAccess ra
    INNER JOIN system.SavedQuery q ON q.Id = ra.RowId
    WHERE ra.ActionAccessType = 3
      AND q.EntityFullName = @Ent
      AND q.Name IN (N'vw_DocumentMyCartabl', N'vw_DocumentMyArchive', N'vw_DocumentMyActions', N'vw_DocumentAllProjectInfo', N'vw_DocumentDCC', N'vw_DocumentReciveAndSend', N'vw_DocumentAllProjectInfonew', N'vw_DocumentAllProjectInfonewnew');

    DELETE FROM system.SavedQuery
    WHERE EntityFullName = @Ent
      AND Name IN (N'vw_DocumentMyCartabl', N'vw_DocumentMyArchive', N'vw_DocumentMyActions', N'vw_DocumentAllProjectInfo', N'vw_DocumentDCC', N'vw_DocumentReciveAndSend', N'vw_DocumentAllProjectInfonew', N'vw_DocumentAllProjectInfonewnew');

    
    SET @Name = N'Edms_Document_ProducerCartable';
    SET @Title = N'کارتابل تهیه‌کننده';
    SET @Qj = N'{"Tables": [], "Relations": [], "Filters": [], "CustomConditions": [], "CustomQuery": "DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);\n--!--mainsection\nSELECT\n    CAST(CASE WHEN [t1].[RevieweMiladiDateTime] IS NOT NULL THEN 1 ELSE 0 END AS bit) AS [IsChecked],\n    [t2].[Code] AS [t2_Code],\n    [t2].[Name] AS [t2_Name],\n    [t3].[Code] AS [t3_Code],\n    [t3].[Title] AS [t3_Title],\n    [t1].[Revision] AS [t1_Revision],\n    [t1].[Status] AS [t1_Status],\n    [t1].[Status] AS [t1_StatusId],\n    [t1].[IsLatest] AS [IsLatest],\n    [t1].[GoalOfProduction] AS [t1_GoalOfProduction],\n    [t3].[Displaying] AS [t3_Displaying],\n    [t4].[OriginalName] AS [t4_OriginalName],\n    [t1].[Comment] AS [t1_Comment],\n    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],\n    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],\n    [t6].[Name] AS [t6_Name],\n    [t5].[Name] AS [t5_Name],\n    [t7].[Name] AS [t7_Name],\n    [t1].[Id] AS [t1_Id],\n    [t1].[ProjectId] AS [t1_ProjectId]\nFROM [Edms].[Document] AS [t1]\nLEFT JOIN [Edms].[Project] AS [t2] ON [t1].[ProjectId] = [t2].[Id]\nLEFT JOIN [Edms].[ProjectVpis] AS [t3] ON [t1].[DocumentVpisId] = [t3].[Id]\nLEFT JOIN [dbo].[FileEntity] AS [t4] ON [t4].[Id] = [t1].[MainFileId]\nLEFT JOIN [system].[User] AS [t5] ON [t1].[ReviewerId] = [t5].[Id]\nLEFT JOIN [system].[User] AS [t6] ON [t3].[ProducerId] = [t6].[Id]\nLEFT JOIN [system].[User] AS [t7] ON [t1].[ApproverId] = [t7].[Id]\nWHERE [t1].[IsLatest] = 1\nAND [t3].[ProducerId] = @CuId\nAND (\n    (([t1].[Status] = 1 AND [t2].[Status] = 1) OR [t1].[Status] IN (5, 8, 10, 11, 12, 14, 15, 16, 17, 21))\n    OR ([t1].[Status] = 13 AND ISNULL([t2].[ProjectIsVendoriType], 0) = 1 AND [t1].[ApprovedMiladiDateTime] IS NOT NULL)\n) AND ISNULL([t2].[Status], -1) NOT IN (2, 4)", "Parameters": [], "Selects": null}';
    SET @Btn = N'[{"id": "edms_open_document", "title": "باز کردن مدرک", "dataActionName": "openDocument", "colorClass": "btn-color-primary", "iconClass": "ki-eye ki-outline", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var row = (ctx && ctx.selectedRow) || {};\n    var id = ctx && (ctx.primaryKeyValue || row.t1_Id);\n    if (!id) { toastr.error(''یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Edms/Document/EditAsGroup?id='' + id, true, ''مدرک'');\n}"}]';
    SET @Act = N'[{"dataActionName": "new", "enable": false, "title": "جدید"}, {"dataActionName": "edit", "enable": false, "title": "ویرایش"}, {"dataActionName": "delete", "enable": false, "title": "حذف"}, {"dataActionName": "exportExcell", "enable": true, "title": "خروجی اکسل"}]';
    IF ISJSON(@Qj) <> 1 OR ISJSON(@Cols) <> 1 OR ISJSON(@Btn) <> 1 OR ISJSON(@Act) <> 1
        THROW 51640, N'JSON یکی از نمایه‌های مدرک معتبر نیست.', 1;
    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
    BEGIN
        UPDATE system.SavedQuery SET Title=@Title, QueryJson=@Qj, ColumnsJson=@Cols, EntityFullName=@Ent, Mode=1, Type=1,
            ActionOptions=@Act, CustomActionButtonsJson=@Btn, EventScriptsJson=@EventScripts, DiagramJson=N'{}',
            ModifiedById=1, ModifiedByName=@SeedUser, ModifiedDateMiladiDateTime=@Now, ModifiedDateShamsiDateTime=@NowShamsi, IsActive=1
        WHERE Name=@Name;
        SELECT @Pid = Id FROM system.SavedQuery WHERE Name=@Name;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@Name, @Title, @Qj, @Cols, N'{}', @Btn, @EventScripts,
             1, 1, @Ent, @Act, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @Pid = SCOPE_IDENTITY();
    END
    DELETE ra FROM system.RoleAccess ra WHERE ra.ActionAccessType = 3 AND ra.RowId = @Pid;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, @Pascal, @Title, NULL, @Pid, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'admin', N'ShowAllMenus', N'کارشناسان بارگذاری اسناد EDMS', N'کارشناسان بارگذاری/چک/تایید اسناد EDMS', N'کارشناسان (خارجی) بارگذاری اسناد EDMS', N'مدیران پروژه در EDMS', N'Edms.Document.EditRev');


    /* Issue (۲) برای بررسی‌کننده و تاییدکننده همین VPIS.
       کامنت بررسی‌کننده (۵) تسک تهیه‌کننده است و در این کارتابل نمی‌ماند.
       IssueForClient (۳۰) فقط برای همان بررسی‌کننده یا تاییدکننده‌ای می‌ماند که آخرین کامنت را خودش گذاشته (HTS LastStatusUser).
       ApproveByClient (۱۳) فقط پروژه وندوری، تا قبل از مهر تایید، و فقط وقتی آخرین کامنت از تاییدکننده نیست.
       ReplaySheet (۲۸) برای بررسی‌کننده یا تاییدکننده VPIS، تا وقتی آخرین کامنت مال خودشان نیست. */
    SET @Name = N'Edms_Document_ReviewApproveCartable';
    SET @Title = N'کارتابل بررسی و تایید';
    SET @Qj = N'{"Tables": [], "Relations": [], "Filters": [], "CustomConditions": [], "CustomQuery": "DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);\n--!--mainsection\nSELECT\n    CAST(CASE WHEN [t1].[RevieweMiladiDateTime] IS NOT NULL THEN 1 ELSE 0 END AS bit) AS [IsChecked],\n    [t2].[Code] AS [t2_Code],\n    [t2].[Name] AS [t2_Name],\n    [t3].[Code] AS [t3_Code],\n    [t3].[Title] AS [t3_Title],\n    [t1].[Revision] AS [t1_Revision],\n    [t1].[Status] AS [t1_Status],\n    [t1].[Status] AS [t1_StatusId],\n    [t1].[IsLatest] AS [IsLatest],\n    [t1].[GoalOfProduction] AS [t1_GoalOfProduction],\n    [t3].[Displaying] AS [t3_Displaying],\n    [t4].[OriginalName] AS [t4_OriginalName],\n    [t1].[Comment] AS [t1_Comment],\n    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],\n    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],\n    [t6].[Name] AS [t6_Name],\n    [t5].[Name] AS [t5_Name],\n    [t7].[Name] AS [t7_Name],\n    [t1].[Id] AS [t1_Id],\n    [t1].[ProjectId] AS [t1_ProjectId]\nFROM [Edms].[Document] AS [t1]\nLEFT JOIN [Edms].[Project] AS [t2] ON [t1].[ProjectId] = [t2].[Id]\nLEFT JOIN [Edms].[ProjectVpis] AS [t3] ON [t1].[DocumentVpisId] = [t3].[Id]\nLEFT JOIN [dbo].[FileEntity] AS [t4] ON [t4].[Id] = [t1].[MainFileId]\nLEFT JOIN [system].[User] AS [t5] ON [t1].[ReviewerId] = [t5].[Id]\nLEFT JOIN [system].[User] AS [t6] ON [t3].[ProducerId] = [t6].[Id]\nLEFT JOIN [system].[User] AS [t7] ON [t1].[ApproverId] = [t7].[Id]\nOUTER APPLY (\n    SELECT TOP (1) c.CreatedById\n    FROM [Edms].[DocumentComment] AS c\n    WHERE c.DocumentId = [t1].[Id]\n    ORDER BY c.Id DESC\n) AS lastComment\nWHERE [t1].[IsLatest] = 1\nAND [t1].[Status] <> 5\nAND (\n    ([t1].[Status] = 2 AND N'','' + ISNULL([t3].[ReviewersId], N'''') + N'','' LIKE N''%,'' + CAST(@CuId AS nvarchar(20)) + N'',%'')\n    OR ([t1].[Status] = 2 AND [t3].[ApproverId] = @CuId)\n    OR ([t1].[Status] = 4 AND [t3].[ApproverId] = @CuId)\n    OR (\n        [t1].[Status] = 30\n        AND ISNULL(lastComment.CreatedById, 0) = @CuId\n        AND (\n            N'','' + ISNULL([t3].[ReviewersId], N'''') + N'','' LIKE N''%,'' + CAST(@CuId AS nvarchar(20)) + N'',%''\n            OR [t3].[ApproverId] = @CuId\n        )\n    )\n    OR (\n        ISNULL([t2].[ProjectIsVendoriType], 0) = 1\n        AND [t1].[Status] = 13\n        AND [t1].[ApprovedMiladiDateTime] IS NULL\n        AND [t3].[ApproverId] = @CuId\n        AND ISNULL(lastComment.CreatedById, 0) <> @CuId\n    )\n    OR (\n        [t1].[Status] = 28\n        AND ISNULL(lastComment.CreatedById, 0) <> @CuId\n        AND (\n            N'','' + ISNULL([t3].[ReviewersId], N'''') + N'','' LIKE N''%,'' + CAST(@CuId AS nvarchar(20)) + N'',%''\n            OR [t3].[ApproverId] = @CuId\n        )\n    )\n) AND ISNULL([t2].[Status], -1) NOT IN (2, 4)", "Parameters": [], "Selects": null}';
    SET @Btn = N'[{"id": "edms_open_document", "title": "باز کردن مدرک", "dataActionName": "openDocument", "colorClass": "btn-color-primary", "iconClass": "ki-eye ki-outline", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var row = (ctx && ctx.selectedRow) || {};\n    var id = ctx && (ctx.primaryKeyValue || row.t1_Id);\n    if (!id) { toastr.error(''یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Edms/Document/EditAsGroup?id='' + id, true, ''مدرک'');\n}"}]';
    SET @Act = N'[{"dataActionName": "new", "enable": false, "title": "جدید"}, {"dataActionName": "edit", "enable": false, "title": "ویرایش"}, {"dataActionName": "delete", "enable": false, "title": "حذف"}, {"dataActionName": "exportExcell", "enable": true, "title": "خروجی اکسل"}]';
    IF ISJSON(@Qj) <> 1 OR ISJSON(@Cols) <> 1 OR ISJSON(@Btn) <> 1 OR ISJSON(@Act) <> 1
        THROW 51640, N'JSON یکی از نمایه‌های مدرک معتبر نیست.', 1;
    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
    BEGIN
        UPDATE system.SavedQuery SET Title=@Title, QueryJson=@Qj, ColumnsJson=@Cols, EntityFullName=@Ent, Mode=1, Type=1,
            ActionOptions=@Act, CustomActionButtonsJson=@Btn, EventScriptsJson=@EventScripts, DiagramJson=N'{}',
            ModifiedById=1, ModifiedByName=@SeedUser, ModifiedDateMiladiDateTime=@Now, ModifiedDateShamsiDateTime=@NowShamsi, IsActive=1
        WHERE Name=@Name;
        SELECT @Pid = Id FROM system.SavedQuery WHERE Name=@Name;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@Name, @Title, @Qj, @Cols, N'{}', @Btn, @EventScripts,
             1, 1, @Ent, @Act, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @Pid = SCOPE_IDENTITY();
    END
    DELETE ra FROM system.RoleAccess ra WHERE ra.ActionAccessType = 3 AND ra.RowId = @Pid;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, @Pascal, @Title, NULL, @Pid, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'admin', N'ShowAllMenus', N'کارشناسان بارگذاری/چک/تایید اسناد EDMS', N'کارشناسان چک/تایید اسناد EDMS (خروجی اکسل)', N'مدیران پروژه در EDMS');


    SET @Name = N'Edms_Document_DccApproveCartable';
    SET @Title = N'کارتابل تایید DCC';
    SET @Qj = N'{"Tables": [], "Relations": [], "Filters": [], "CustomConditions": [], "CustomQuery": "DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);\n--!--mainsection\nSELECT\n    CAST(CASE WHEN [t1].[RevieweMiladiDateTime] IS NOT NULL THEN 1 ELSE 0 END AS bit) AS [IsChecked],\n    [t2].[Code] AS [t2_Code],\n    [t2].[Name] AS [t2_Name],\n    [t3].[Code] AS [t3_Code],\n    [t3].[Title] AS [t3_Title],\n    [t1].[Revision] AS [t1_Revision],\n    [t1].[Status] AS [t1_Status],\n    [t1].[Status] AS [t1_StatusId],\n    [t1].[IsLatest] AS [IsLatest],\n    [t1].[GoalOfProduction] AS [t1_GoalOfProduction],\n    [t3].[Displaying] AS [t3_Displaying],\n    [t4].[OriginalName] AS [t4_OriginalName],\n    [t1].[Comment] AS [t1_Comment],\n    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],\n    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],\n    [t6].[Name] AS [t6_Name],\n    [t5].[Name] AS [t5_Name],\n    [t7].[Name] AS [t7_Name],\n    [t1].[Id] AS [t1_Id],\n    [t1].[ProjectId] AS [t1_ProjectId]\nFROM [Edms].[Document] AS [t1]\nLEFT JOIN [Edms].[Project] AS [t2] ON [t1].[ProjectId] = [t2].[Id]\nLEFT JOIN [Edms].[ProjectVpis] AS [t3] ON [t1].[DocumentVpisId] = [t3].[Id]\nLEFT JOIN [dbo].[FileEntity] AS [t4] ON [t4].[Id] = [t1].[MainFileId]\nLEFT JOIN [system].[User] AS [t5] ON [t1].[ReviewerId] = [t5].[Id]\nLEFT JOIN [system].[User] AS [t6] ON [t3].[ProducerId] = [t6].[Id]\nLEFT JOIN [system].[User] AS [t7] ON [t1].[ApproverId] = [t7].[Id]\nWHERE [t1].[IsLatest] = 1 AND [t1].[Status] = 7 AND NOT ([t1].[ReplySheetId] IS NULL AND [t1].[SecondReplySheetId] IS NOT NULL) AND ISNULL([t2].[Status], -1) NOT IN (2, 4)", "Parameters": [], "Selects": null}';
    SET @Btn = N'[{"id": "edms_open_document", "title": "باز کردن مدرک", "dataActionName": "openDocument", "colorClass": "btn-color-primary", "iconClass": "ki-eye ki-outline", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var row = (ctx && ctx.selectedRow) || {};\n    var id = ctx && (ctx.primaryKeyValue || row.t1_Id);\n    if (!id) { toastr.error(''یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Edms/Document/EditAsGroup?id='' + id, true, ''مدرک'');\n}"}]';
    SET @Act = N'[{"dataActionName": "new", "enable": false, "title": "جدید"}, {"dataActionName": "edit", "enable": false, "title": "ویرایش"}, {"dataActionName": "delete", "enable": false, "title": "حذف"}, {"dataActionName": "exportExcell", "enable": true, "title": "خروجی اکسل"}]';
    IF ISJSON(@Qj) <> 1 OR ISJSON(@Cols) <> 1 OR ISJSON(@Btn) <> 1 OR ISJSON(@Act) <> 1
        THROW 51640, N'JSON یکی از نمایه‌های مدرک معتبر نیست.', 1;
    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
    BEGIN
        UPDATE system.SavedQuery SET Title=@Title, QueryJson=@Qj, ColumnsJson=@Cols, EntityFullName=@Ent, Mode=1, Type=1,
            ActionOptions=@Act, CustomActionButtonsJson=@Btn, EventScriptsJson=@EventScripts, DiagramJson=N'{}',
            ModifiedById=1, ModifiedByName=@SeedUser, ModifiedDateMiladiDateTime=@Now, ModifiedDateShamsiDateTime=@NowShamsi, IsActive=1
        WHERE Name=@Name;
        SELECT @Pid = Id FROM system.SavedQuery WHERE Name=@Name;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@Name, @Title, @Qj, @Cols, N'{}', @Btn, @EventScripts,
             1, 1, @Ent, @Act, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @Pid = SCOPE_IDENTITY();
    END
    DELETE ra FROM system.RoleAccess ra WHERE ra.ActionAccessType = 3 AND ra.RowId = @Pid;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, @Pascal, @Title, NULL, @Pid, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'admin', N'ShowAllMenus', N'EdmsDocumentsDccUsers');


    SET @Name = N'Edms_Document_ReadyTransmittal';
    SET @Title = N'کارتابل آماده ترانسمیتال';
    SET @Qj = N'{"Tables": [], "Relations": [], "Filters": [], "CustomConditions": [], "CustomQuery": "DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);\n--!--mainsection\nSELECT\n    CAST(CASE WHEN [t1].[RevieweMiladiDateTime] IS NOT NULL THEN 1 ELSE 0 END AS bit) AS [IsChecked],\n    [t2].[Code] AS [t2_Code],\n    [t2].[Name] AS [t2_Name],\n    [t3].[Code] AS [t3_Code],\n    [t3].[Title] AS [t3_Title],\n    [t1].[Revision] AS [t1_Revision],\n    [t1].[Status] AS [t1_Status],\n    [t1].[Status] AS [t1_StatusId],\n    [t1].[IsLatest] AS [IsLatest],\n    [t1].[GoalOfProduction] AS [t1_GoalOfProduction],\n    [t3].[Displaying] AS [t3_Displaying],\n    [t4].[OriginalName] AS [t4_OriginalName],\n    [t1].[Comment] AS [t1_Comment],\n    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],\n    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],\n    [t6].[Name] AS [t6_Name],\n    [t5].[Name] AS [t5_Name],\n    [t7].[Name] AS [t7_Name],\n    [t1].[Id] AS [t1_Id],\n    [t1].[ProjectId] AS [t1_ProjectId]\nFROM [Edms].[Document] AS [t1]\nLEFT JOIN [Edms].[Project] AS [t2] ON [t1].[ProjectId] = [t2].[Id]\nLEFT JOIN [Edms].[ProjectVpis] AS [t3] ON [t1].[DocumentVpisId] = [t3].[Id]\nLEFT JOIN [dbo].[FileEntity] AS [t4] ON [t4].[Id] = [t1].[MainFileId]\nLEFT JOIN [system].[User] AS [t5] ON [t1].[ReviewerId] = [t5].[Id]\nLEFT JOIN [system].[User] AS [t6] ON [t3].[ProducerId] = [t6].[Id]\nLEFT JOIN [system].[User] AS [t7] ON [t1].[ApproverId] = [t7].[Id]\nWHERE [t1].[IsLatest] = 1 AND [t1].[Status] = 9 AND ISNULL([t2].[Status], -1) NOT IN (2, 4)", "Parameters": [], "Selects": null}';
    SET @Btn = N'[{"id": "edms_open_document", "title": "باز کردن مدرک", "dataActionName": "openDocument", "colorClass": "btn-color-primary", "iconClass": "ki-eye ki-outline", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var row = (ctx && ctx.selectedRow) || {};\n    var id = ctx && (ctx.primaryKeyValue || row.t1_Id);\n    if (!id) { toastr.error(''یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Edms/Document/EditAsGroup?id='' + id, true, ''مدرک'');\n}"}, {"id": "edms_new_transmittal", "title": "ترانسمیتال جدید", "dataActionName": "newTransmittal", "colorClass": "btn-color-warning", "iconClass": "ki-send ki-outline", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    appController.addPage(''/Panel/Edms/Transmital/New'', true, ''ترانسمیتال'');\n}"}]';
    SET @Act = N'[{"dataActionName": "new", "enable": false, "title": "جدید"}, {"dataActionName": "edit", "enable": false, "title": "ویرایش"}, {"dataActionName": "delete", "enable": false, "title": "حذف"}, {"dataActionName": "exportExcell", "enable": true, "title": "خروجی اکسل"}]';
    IF ISJSON(@Qj) <> 1 OR ISJSON(@Cols) <> 1 OR ISJSON(@Btn) <> 1 OR ISJSON(@Act) <> 1
        THROW 51640, N'JSON یکی از نمایه‌های مدرک معتبر نیست.', 1;
    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
    BEGIN
        UPDATE system.SavedQuery SET Title=@Title, QueryJson=@Qj, ColumnsJson=@Cols, EntityFullName=@Ent, Mode=1, Type=1,
            ActionOptions=@Act, CustomActionButtonsJson=@Btn, EventScriptsJson=@EventScripts, DiagramJson=N'{}',
            ModifiedById=1, ModifiedByName=@SeedUser, ModifiedDateMiladiDateTime=@Now, ModifiedDateShamsiDateTime=@NowShamsi, IsActive=1
        WHERE Name=@Name;
        SELECT @Pid = Id FROM system.SavedQuery WHERE Name=@Name;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@Name, @Title, @Qj, @Cols, N'{}', @Btn, @EventScripts,
             1, 1, @Ent, @Act, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @Pid = SCOPE_IDENTITY();
    END
    DELETE ra FROM system.RoleAccess ra WHERE ra.ActionAccessType = 3 AND ra.RowId = @Pid;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, @Pascal, @Title, NULL, @Pid, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'admin', N'ShowAllMenus', N'EdmsDocumentsDccUsers');


    SET @Name = N'Edms_Document_ClientReceive';
    SET @Title = N'بررسی دریافت و ارسال اسناد';
    SET @Qj = N'{"Tables": [], "Relations": [], "Filters": [], "CustomConditions": [], "CustomQuery": "DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);\n--!--mainsection\nSELECT\n    CAST(CASE WHEN [t1].[RevieweMiladiDateTime] IS NOT NULL THEN 1 ELSE 0 END AS bit) AS [IsChecked],\n    [t2].[Code] AS [t2_Code],\n    [t2].[Name] AS [t2_Name],\n    [t3].[Code] AS [t3_Code],\n    [t3].[Title] AS [t3_Title],\n    [t1].[Revision] AS [t1_Revision],\n    [t1].[Status] AS [t1_Status],\n    [t1].[Status] AS [t1_StatusId],\n    [t1].[IsLatest] AS [IsLatest],\n    [t1].[GoalOfProduction] AS [t1_GoalOfProduction],\n    [t3].[Displaying] AS [t3_Displaying],\n    [t4].[OriginalName] AS [t4_OriginalName],\n    [t1].[Comment] AS [t1_Comment],\n    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],\n    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],\n    [t6].[Name] AS [t6_Name],\n    [t5].[Name] AS [t5_Name],\n    [t7].[Name] AS [t7_Name],\n    [t1].[Id] AS [t1_Id],\n    [t1].[ProjectId] AS [t1_ProjectId]\nFROM [Edms].[Document] AS [t1]\nLEFT JOIN [Edms].[Project] AS [t2] ON [t1].[ProjectId] = [t2].[Id]\nLEFT JOIN [Edms].[ProjectVpis] AS [t3] ON [t1].[DocumentVpisId] = [t3].[Id]\nLEFT JOIN [dbo].[FileEntity] AS [t4] ON [t4].[Id] = [t1].[MainFileId]\nLEFT JOIN [system].[User] AS [t5] ON [t1].[ReviewerId] = [t5].[Id]\nLEFT JOIN [system].[User] AS [t6] ON [t3].[ProducerId] = [t6].[Id]\nLEFT JOIN [system].[User] AS [t7] ON [t1].[ApproverId] = [t7].[Id]\nWHERE [t1].[IsLatest] = 1 AND [t1].[Status] = 18 AND ISNULL([t2].[Status], -1) NOT IN (2, 4)", "Parameters": [], "Selects": null}';
    SET @Btn = N'[{"id": "edms_open_document", "title": "باز کردن مدرک", "dataActionName": "openDocument", "colorClass": "btn-color-primary", "iconClass": "ki-eye ki-outline", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var row = (ctx && ctx.selectedRow) || {};\n    var id = ctx && (ctx.primaryKeyValue || row.t1_Id);\n    if (!id) { toastr.error(''یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Edms/Document/EditAsGroup?id='' + id, true, ''مدرک'');\n}"}, {"id": "edms_client_comment", "title": "نظر کارفرما", "dataActionName": "clientComment", "colorClass": "btn-color-success", "iconClass": "ki-message-text ki-outline", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var row = (ctx && ctx.selectedRow) || {};\n    var id = ctx && (ctx.primaryKeyValue || row.t1_Id);\n    var projectId = row.t1_ProjectId || 0;\n    if (!id) { toastr.error(''یک ردیف را انتخاب کنید''); return; }\n    $.ajax({\n        url: ''/Panel/Edms/Document/DocumentCommentPartialEdit?documentId='' + id + ''&projectId='' + projectId + ''&currentStatus=18&canEdit=true&allowCommentDate=true'',\n        headers: { ''X-Requested-With'': ''XMLHttpRequest'', ''X-Partial-Request'': ''true'' },\n        success: function (html) {\n            var content = $(''<div></div>'').html(html);\n            var box = $.confirm({\n                title: ''نظر کارفرما'',\n                content: content,\n                rtl: true,\n                columnClass: ''col-md-12'',\n                buttons: {\n                    save: {\n                        text: ''ثبت'',\n                        btnClass: ''btn-success'',\n                        action: function () {\n                            var bound = this.$content.dataBind();\n                            var model = bound.comments && bound.comments[0];\n                            if (!model) { toastr.error(''فرم نظر خوانده نشد''); return false; }\n                            post(''/Panel/Edms/Document/AddNewComment'', model, function (r) {\n                                if (!r.isSuccess) { toastr.error(r.message || ''خطا''); return; }\n                                toastr.success(''نظر کارفرما ثبت شد'');\n                                if (ctx.draw) ctx.draw();\n                                box.close();\n                            });\n                            return false;\n                        }\n                    },\n                    cancel: { text: ''بستن'' }\n                }\n            });\n            if (typeof initFileUploaders === ''function'') initFileUploaders(content);\n            if (typeof initPersionDatePicker === ''function'') initPersionDatePicker(content);\n        }\n    });\n}"}]';
    SET @Act = N'[{"dataActionName": "new", "enable": false, "title": "جدید"}, {"dataActionName": "edit", "enable": false, "title": "ویرایش"}, {"dataActionName": "delete", "enable": false, "title": "حذف"}, {"dataActionName": "exportExcell", "enable": true, "title": "خروجی اکسل"}]';
    IF ISJSON(@Qj) <> 1 OR ISJSON(@Cols) <> 1 OR ISJSON(@Btn) <> 1 OR ISJSON(@Act) <> 1
        THROW 51640, N'JSON یکی از نمایه‌های مدرک معتبر نیست.', 1;
    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
    BEGIN
        UPDATE system.SavedQuery SET Title=@Title, QueryJson=@Qj, ColumnsJson=@Cols, EntityFullName=@Ent, Mode=1, Type=1,
            ActionOptions=@Act, CustomActionButtonsJson=@Btn, EventScriptsJson=@EventScripts, DiagramJson=N'{}',
            ModifiedById=1, ModifiedByName=@SeedUser, ModifiedDateMiladiDateTime=@Now, ModifiedDateShamsiDateTime=@NowShamsi, IsActive=1
        WHERE Name=@Name;
        SELECT @Pid = Id FROM system.SavedQuery WHERE Name=@Name;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@Name, @Title, @Qj, @Cols, N'{}', @Btn, @EventScripts,
             1, 1, @Ent, @Act, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @Pid = SCOPE_IDENTITY();
    END
    DELETE ra FROM system.RoleAccess ra WHERE ra.ActionAccessType = 3 AND ra.RowId = @Pid;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, @Pascal, @Title, NULL, @Pid, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'admin', N'ShowAllMenus', N'EdmsDocumentsDccUsers');


    SET @Name = N'Edms_Document_AllRevisions';
    SET @Title = N'فهرست مدارک';
    SET @Qj = N'{"Tables": [], "Relations": [], "Filters": [], "CustomConditions": [], "CustomQuery": "DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);\n--!--mainsection\nSELECT\n    CAST(CASE WHEN [t1].[RevieweMiladiDateTime] IS NOT NULL THEN 1 ELSE 0 END AS bit) AS [IsChecked],\n    [t2].[Code] AS [t2_Code],\n    [t2].[Name] AS [t2_Name],\n    [t3].[Code] AS [t3_Code],\n    [t3].[Title] AS [t3_Title],\n    [t1].[Revision] AS [t1_Revision],\n    [t1].[Status] AS [t1_Status],\n    [t1].[Status] AS [t1_StatusId],\n    [t1].[IsLatest] AS [IsLatest],\n    [t1].[GoalOfProduction] AS [t1_GoalOfProduction],\n    [t3].[Displaying] AS [t3_Displaying],\n    [t4].[OriginalName] AS [t4_OriginalName],\n    [t1].[Comment] AS [t1_Comment],\n    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],\n    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],\n    [t6].[Name] AS [t6_Name],\n    [t5].[Name] AS [t5_Name],\n    [t7].[Name] AS [t7_Name],\n    [t1].[Id] AS [t1_Id],\n    [t1].[ProjectId] AS [t1_ProjectId]\nFROM [Edms].[Document] AS [t1]\nLEFT JOIN [Edms].[Project] AS [t2] ON [t1].[ProjectId] = [t2].[Id]\nLEFT JOIN [Edms].[ProjectVpis] AS [t3] ON [t1].[DocumentVpisId] = [t3].[Id]\nLEFT JOIN [dbo].[FileEntity] AS [t4] ON [t4].[Id] = [t1].[MainFileId]\nLEFT JOIN [system].[User] AS [t5] ON [t1].[ReviewerId] = [t5].[Id]\nLEFT JOIN [system].[User] AS [t6] ON [t3].[ProducerId] = [t6].[Id]\nLEFT JOIN [system].[User] AS [t7] ON [t1].[ApproverId] = [t7].[Id]\nWHERE 1 = 1", "Parameters": [], "Selects": null}';
    SET @Btn = N'[{"id": "edms_open_document", "title": "باز کردن مدرک", "dataActionName": "openDocument", "colorClass": "btn-color-primary", "iconClass": "ki-eye ki-outline", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var row = (ctx && ctx.selectedRow) || {};\n    var id = ctx && (ctx.primaryKeyValue || row.t1_Id);\n    if (!id) { toastr.error(''یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Edms/Document/EditAsGroup?id='' + id, true, ''مدرک'');\n}"}]';
    SET @Act = N'[{"dataActionName": "new", "enable": false, "title": "جدید"}, {"dataActionName": "edit", "enable": false, "title": "ویرایش"}, {"dataActionName": "delete", "enable": false, "title": "حذف"}, {"dataActionName": "exportExcell", "enable": true, "title": "خروجی اکسل"}]';
    IF ISJSON(@Qj) <> 1 OR ISJSON(@Cols) <> 1 OR ISJSON(@Btn) <> 1 OR ISJSON(@Act) <> 1
        THROW 51640, N'JSON یکی از نمایه‌های مدرک معتبر نیست.', 1;
    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
    BEGIN
        UPDATE system.SavedQuery SET Title=@Title, QueryJson=@Qj, ColumnsJson=@Cols, EntityFullName=@Ent, Mode=1, Type=1,
            ActionOptions=@Act, CustomActionButtonsJson=@Btn, EventScriptsJson=@EventScripts, DiagramJson=N'{}',
            ModifiedById=1, ModifiedByName=@SeedUser, ModifiedDateMiladiDateTime=@Now, ModifiedDateShamsiDateTime=@NowShamsi, IsActive=1
        WHERE Name=@Name;
        SELECT @Pid = Id FROM system.SavedQuery WHERE Name=@Name;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES (@Name, @Title, @Qj, @Cols, N'{}', @Btn, @EventScripts,
             1, 1, @Ent, @Act, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @Pid = SCOPE_IDENTITY();
    END
    DELETE ra FROM system.RoleAccess ra WHERE ra.ActionAccessType = 3 AND ra.RowId = @Pid;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, @Pascal, @Title, NULL, @Pid, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'admin', N'ShowAllMenus', N'EdmsDocumentsDccUsers', N'Edms.Document.ChangeStatus', N'مدیران پروژه در EDMS', N'کارشناسان آرشیو کلی اسناد EDMS', N'کارشناسان مشاهده کلی در آرشیو کلی اسناد EDMS');


    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery
WHERE EntityFullName = N'entities.app.edms.document' AND Type = 1 AND IsActive = 1
ORDER BY Id;

SELECT q.Name, JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Edms_Document_ProducerCartable'
ORDER BY CAST(j.[key] AS int);
