/*
  نمایه داده «آرشیو شخصی» روی موجودیت مدرک (معادل HTS: PersonalDocumentArchive، صفحه Edms_Personal_Document_Archive = 233).

  فیلتر HTS (PersonalDocumentArchiveController.GetPersonalDocumentArchiveGridData):
    1) FullAccess / ShowAll روی Edms_Document  → همه مدارک، بدون فیلتر
       (HTS: فقط گروه ۵ «مدیر سیستم کنترل مستندات»، در هاویار همان نقش + admin).
    2) سایر کاربران:
       (پروژه محرمانه نیست یا کاربر در کاربران محرمانهٔ پروژه است)
       و (مسئول اصلی VPIS = کاربر  یا  تهیه‌کننده = کاربر  یا  چک‌کننده = کاربر  یا  تاییدکننده = کاربر)
    3) اگر مورد ۲ هیچ سطری نداد و کاربر عضو پروژه‌ای است (Edms_Project_User)
       → مدارک آن پروژه‌ها با همان شرط محرمانگی.
    همهٔ ریویژن‌ها نمایش داده می‌شود (HTS فیلتر IsLatest ندارد).

  نگاشت HTS → هاویار:
    Vpis_Responsible_FK (IsMain) → Edms.ProjectVpis.ProducerId
    PreparedUser_FK → Edms.Document.PreparedUserId
    CheckedUser_FK  → Edms.Document.ReviewerId
    ApprovedUser_FK → Edms.Document.ApproverId
    Edms_ConfidentialProject_User → Edms.Project.SensitiveUsersIds
    Edms_Project_User → Edms.ProjectUser

  دسترسی (HTS گروه‌های دارای صفحه ۲۳۳): ۸۲، ۸۳، ۱۳۵، ۳۵۰، ۴۸۸ + سه کاربر مستقیم (نقش ۲۶۹۰۰۱).
  UTF-8 with BOM. sqlcmd -S PortalSRV\PORTAL -d HavayarApp -C -b -I -f 65001 -i Data\Scripts\Seed_EdmsDocument_PersonalArchive_DataProfile.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-edms-document-personal-archive';
    DECLARE @Name NVARCHAR(100) = N'Edms_Document_PersonalArchive';
    DECLARE @Title NVARCHAR(200) = N'آرشیو شخصی';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.edms.document';
    DECLARE @Pascal NVARCHAR(200) = N'Entities.App.Edms.Document';
    DECLARE @Qj NVARCHAR(MAX) = N'{"Tables": [], "Relations": [], "Filters": [], "CustomConditions": [], "CustomQuery": "DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);\nDECLARE @CuText NVARCHAR(20) = CAST(@CuId AS NVARCHAR(20));\nDECLARE @HasFull BIT = CASE WHEN EXISTS (\n    SELECT 1\n    FROM [system].[User] u\n    CROSS APPLY OPENJSON(CASE WHEN ISJSON(u.[RoleIds]) = 1 THEN u.[RoleIds] ELSE N''[]'' END) j\n    INNER JOIN [system].[Role] r ON r.[Id] = TRY_CAST(j.[value] AS BIGINT)\n    WHERE u.[Id] = @CuId AND r.[Name] IN (N''admin'', N''مدیر سیستم کنترل مستندات'')\n) THEN 1 ELSE 0 END;\nDECLARE @HasOwn BIT = CASE WHEN EXISTS (\n    SELECT 1\n    FROM [Edms].[Document] [t1]\n    INNER JOIN [Edms].[ProjectVpis] [t3] ON [t1].[DocumentVpisId] = [t3].[Id]\n    INNER JOIN [Edms].[Project] [t2] ON [t1].[ProjectId] = [t2].[Id]\n    WHERE ([t3].[ProducerId] = @CuId OR [t1].[PreparedUserId] = @CuId OR [t1].[ReviewerId] = @CuId OR [t1].[ApproverId] = @CuId)\n      AND (ISNULL([t2].[IsConfidential], 0) = 0 OR N'','' + REPLACE(ISNULL([t2].[SensitiveUsersIds], N''''), N'' '', N'''') + N'','' LIKE N''%,'' + @CuText + N'',%'')\n) THEN 1 ELSE 0 END;\n--!--mainsection\nSELECT\n    CAST(CASE WHEN [t1].[RevieweMiladiDateTime] IS NOT NULL THEN 1 ELSE 0 END AS bit) AS [IsChecked],\n    [t2].[Code] AS [t2_Code],\n    [t2].[Name] AS [t2_Name],\n    [t3].[Code] AS [t3_Code],\n    [t3].[Title] AS [t3_Title],\n    [t1].[Revision] AS [t1_Revision],\n    [t1].[Status] AS [t1_Status],\n    [t1].[Status] AS [t1_StatusId],\n    [t1].[IsLatest] AS [IsLatest],\n    [t1].[GoalOfProduction] AS [t1_GoalOfProduction],\n    [t3].[Displaying] AS [t3_Displaying],\n    [t4].[OriginalName] AS [t4_OriginalName],\n    [t1].[Comment] AS [t1_Comment],\n    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],\n    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],\n    [t6].[Name] AS [t6_Name],\n    [t5].[Name] AS [t5_Name],\n    [t7].[Name] AS [t7_Name],\n    [t1].[Id] AS [t1_Id],\n    [t1].[ProjectId] AS [t1_ProjectId]\nFROM [Edms].[Document] AS [t1]\nINNER JOIN [Edms].[Project] AS [t2] ON [t1].[ProjectId] = [t2].[Id]\nINNER JOIN [Edms].[ProjectVpis] AS [t3] ON [t1].[DocumentVpisId] = [t3].[Id]\nLEFT JOIN [dbo].[FileEntity] AS [t4] ON [t4].[Id] = [t1].[MainFileId]\nLEFT JOIN [system].[User] AS [t5] ON [t1].[ReviewerId] = [t5].[Id]\nLEFT JOIN [system].[User] AS [t6] ON [t3].[ProducerId] = [t6].[Id]\nLEFT JOIN [system].[User] AS [t7] ON [t1].[ApproverId] = [t7].[Id]\nWHERE @HasFull = 1\n   OR (\n        (\n            (@HasOwn = 1 AND ([t3].[ProducerId] = @CuId OR [t1].[PreparedUserId] = @CuId OR [t1].[ReviewerId] = @CuId OR [t1].[ApproverId] = @CuId))\n            OR (@HasOwn = 0 AND [t1].[ProjectId] IN (SELECT pu.[ProjectId] FROM [Edms].[ProjectUser] pu WHERE pu.[UserId] = @CuId AND ISNULL(pu.[IsActive], 1) = 1))\n        )\n        AND (ISNULL([t2].[IsConfidential], 0) = 0 OR N'','' + REPLACE(ISNULL([t2].[SensitiveUsersIds], N''''), N'' '', N'''') + N'','' LIKE N''%,'' + @CuText + N'',%'')\n   )", "Parameters": [], "Selects": null}';
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
    DECLARE @Act NVARCHAR(MAX) = N'[{"dataActionName": "new", "enable": false, "title": "جدید"}, {"dataActionName": "edit", "enable": false, "title": "ویرایش"}, {"dataActionName": "delete", "enable": false, "title": "حذف"}, {"dataActionName": "exportExcell", "enable": true, "title": "خروجی اکسل"}]';
    DECLARE @Btn NVARCHAR(MAX) = N'[{"id": "edms_open_document", "title": "باز کردن مدرک", "dataActionName": "openDocument", "colorClass": "btn-color-primary", "iconClass": "ki-eye ki-outline", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var row = (ctx && ctx.selectedRow) || {};\n    var id = ctx && (ctx.primaryKeyValue || row.t1_Id);\n    if (!id) { toastr.error(''یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Edms/Document/EditAsGroup?id='' + id, true, ''مدرک'');\n}"}]';
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
    DECLARE @Pid BIGINT;

    IF ISJSON(@Cols) <> 1 OR ISJSON(@Qj) <> 1 OR ISJSON(@Act) <> 1 OR ISJSON(@Btn) <> 1 OR ISJSON(@EventScripts) <> 1
        THROW 51650, N'JSON نمایه Edms_Document_PersonalArchive معتبر نیست.', 1;

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
    WHERE r.Name IN (N'admin', N'کارشناسان بارگذاری اسناد EDMS', N'کارشناسان بارگذاری/چک/تایید اسناد EDMS', N'کارشناسان (خارجی) بارگذاری اسناد EDMS', N'مشاهده کلی HTS', N'کارشناسان سیستم مهندسی دارای دسترسی های عمومی سیستم مهندسی', N'اعطا مستقیم - آرشیو شخصی اسناد');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name = N'Edms_Document_PersonalArchive';
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Edms_Document_PersonalArchive';
SELECT r.Id AS RoleId, r.Name AS RoleName
FROM system.SavedQuery q
JOIN system.RoleAccess ra ON ra.RowId = q.Id AND ra.ActionAccessType = 3
JOIN system.Role r ON r.Id = ra.RoleId
WHERE q.Name = N'Edms_Document_PersonalArchive'
ORDER BY r.Id;
