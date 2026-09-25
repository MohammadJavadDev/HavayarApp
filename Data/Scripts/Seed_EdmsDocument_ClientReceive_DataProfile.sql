/*
  نمایه داده بررسی دریافت و ارسال اسناد روی موجودیت مدرک.
  فقط ردیف‌های NotReview. UTF-8 with BOM.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-edms-document-client-receive';
    DECLARE @Name NVARCHAR(100) = N'Edms_Document_ClientReceive';
    DECLARE @Title NVARCHAR(200) = N'بررسی دریافت و ارسال اسناد';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.edms.document';
    DECLARE @Pascal NVARCHAR(200) = N'Entities.App.Edms.Document';
    DECLARE @Qj NVARCHAR(MAX) = N'{"Tables": [], "Relations": [], "Filters": [], "CustomConditions": [], "CustomQuery": "SELECT\n    [t1].[Id] AS [t1_Id],\n    [t2].[Name] AS [t2_Name],\n    [t3].[Code] AS [t3_Code],\n    [t3].[Title] AS [t3_Title],\n    [t1].[Revision] AS [t1_Revision],\n    [t1].[Status] AS [t1_Status],\n    [t1].[ProjectId] AS [t1_ProjectId],\n    [t1].[PublicationShamsiDate] AS [t1_PublicationShamsiDate]\nFROM [Edms].[Document] AS [t1]\nLEFT JOIN [Edms].[Project] AS [t2] ON [t1].[ProjectId] = [t2].[Id]\nLEFT JOIN [Edms].[ProjectVpis] AS [t3] ON [t1].[DocumentVpisId] = [t3].[Id]\nWHERE [t1].[Status] = 18 AND [t1].[IsLatest] = 1", "Parameters": [], "Selects": null}';
    DECLARE @Cols NVARCHAR(MAX) = N'[{"TableName": "Edms.Document", "ColumnName": "t1_Id", "DisplayName": "شناسه", "Alliance": "t1_Id", "Address": "[t1].[Id]", "SystemTypeName": "Long", "SelectIndex": 0, "Visible": false, "PrimaryKey": true, "SystemType": 6, "SortDirection": 1, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 70, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.Project", "ColumnName": "t2_Name", "DisplayName": "پروژه", "Alliance": "t2_Name", "Address": "[t2].[Name]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 180, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.ProjectVpis", "ColumnName": "t3_Code", "DisplayName": "کد سند", "Alliance": "t3_Code", "Address": "[t3].[Code]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 140, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.ProjectVpis", "ColumnName": "t3_Title", "DisplayName": "عنوان سند", "Alliance": "t3_Title", "Address": "[t3].[Title]", "SystemTypeName": "String", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 0, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 260, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.Document", "ColumnName": "t1_Revision", "DisplayName": "ریویژن", "Alliance": "t1_Revision", "Address": "[t1].[Revision]", "SystemTypeName": "Int", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 7, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 80, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.Document", "ColumnName": "t1_Status", "DisplayName": "وضعیت", "Alliance": "t1_Status", "Address": "[t1].[Status]", "SystemTypeName": "Select", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 8, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": {"ListOptions": [], "TypeOption": 3, "SystemTypeName": "Entities.App.Edms.Enums.DocumentStatusEnums"}, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 160, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.Document", "ColumnName": "t1_ProjectId", "DisplayName": "شناسه پروژه", "Alliance": "t1_ProjectId", "Address": "[t1].[ProjectId]", "SystemTypeName": "Long", "SelectIndex": 0, "Visible": false, "PrimaryKey": false, "SystemType": 6, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 80, "Filterable": true, "Sortable": true, "ClassName": ""}, {"TableName": "Edms.Document", "ColumnName": "t1_PublicationShamsiDate", "DisplayName": "تاریخ انتشار", "Alliance": "t1_PublicationShamsiDate", "Address": "[t1].[PublicationShamsiDate]", "SystemTypeName": "DateShamsi", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SystemType": 5, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Width": 120, "Filterable": true, "Sortable": true, "ClassName": ""}]';
    DECLARE @Act NVARCHAR(MAX) = N'[{"dataActionName": "new", "enable": false, "title": "جدید"}, {"dataActionName": "edit", "enable": false, "title": "ویرایش"}, {"dataActionName": "delete", "enable": false, "title": "حذف"}, {"dataActionName": "exportExcell", "enable": true, "title": "خروجی اکسل"}]';
    DECLARE @Btn NVARCHAR(MAX) = N'[{"id": "edms_client_comment", "title": "نظر کارفرما", "dataActionName": "clientComment", "colorClass": "btn-color-primary", "iconClass": "ki-message-text ki-outline", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var row = (ctx && ctx.selectedRow) || {};\n    var id = ctx && (ctx.primaryKeyValue || row.t1_Id);\n    var projectId = row.t1_ProjectId || 0;\n    if (!id) { toastr.error(''یک ردیف را انتخاب کنید''); return; }\n    $.ajax({\n        url: ''/Panel/Edms/Document/DocumentCommentPartialEdit?documentId='' + id + ''&projectId='' + projectId + ''&currentStatus=18&canEdit=true'',\n        headers: { ''X-Requested-With'': ''XMLHttpRequest'', ''X-Partial-Request'': ''true'' },\n        success: function (html) {\n            var content = $(''<div></div>'').html(html);\n            var box = $.confirm({\n                title: ''نظر کارفرما'',\n                content: content,\n                rtl: true,\n                columnClass: ''col-md-12'',\n                buttons: {\n                    save: {\n                        text: ''ثبت'',\n                        btnClass: ''btn-success'',\n                        action: function () {\n                            var bound = this.$content.dataBind();\n                            var model = bound.comments && bound.comments[0];\n                            if (!model) { toastr.error(''فرم نظر خوانده نشد''); return false; }\n                            post(''/Panel/Edms/Document/AddNewComment'', model, function (r) {\n                                if (!r.isSuccess) { toastr.error(r.message || ''خطا''); return; }\n                                toastr.success(''نظر کارفرما ثبت شد'');\n                                if (ctx.draw) ctx.draw();\n                                box.close();\n                            });\n                            return false;\n                        }\n                    },\n                    cancel: { text: ''بستن'' }\n                }\n            });\n            if (typeof initFileUploaders === ''function'') initFileUploaders(content);\n        }\n    });\n}"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @Pid BIGINT;

    IF ISJSON(@Cols) <> 1 OR ISJSON(@Qj) <> 1 OR ISJSON(@Act) <> 1 OR ISJSON(@Btn) <> 1 OR ISJSON(@EventScripts) <> 1
        THROW 51630, N'JSON نمایه Edms_Document_ClientReceive معتبر نیست.', 1;

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

    DELETE ra FROM system.RoleAccess ra WHERE ra.ActionAccessType=3 AND ra.RowId=@Pid;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, @Pascal, @Title, NULL, @Pid, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'EdmsDocumentsDccUsers', N'ShowAllMenus');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name = N'Edms_Document_ClientReceive';
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Edms_Document_ClientReceive';
