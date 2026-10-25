/*
Seed_SrvStationeryRequest_DataProfiles.sql
نمایه داده درخواست لوازم التحریر (HTS صفحه 400). UTF-8 BOM. Idempotent.
شش نمایه با ستون‌های یکسان؛ فیلتر نقش در CustomQuery.
sqlcmd -S <server> -d HavayarApp -C -b -I -x -f 65001 -i "Data\Scripts\Seed_SrvStationeryRequest_DataProfiles.sql"
Requires roles from Seed_SrvStationeryRequest_RolesAndAccess.sql.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-srv-stationeryrequest-dataprofile';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.srv.stationeryrequest';
    DECLARE @Pascal NVARCHAR(200) = N'Entities.App.Srv.StationeryRequest';
    DECLARE @Cols NVARCHAR(MAX) = N'[{"TableName": "Srv.StationeryRequest", "SelectIndex": 0, "Visible": false, "PrimaryKey": true, "SortDirection": 1, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Filterable": true, "Sortable": true, "ClassName": "", "Width": 70, "ColumnName": "t1_Id", "DisplayName": "شناسه", "Alliance": "t1_Id", "Address": "[t1].[Id]", "SystemTypeName": "Long", "SystemType": 6}, {"TableName": "Srv.StationeryRequest", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Filterable": true, "Sortable": true, "ClassName": "", "Width": 110, "ColumnName": "t1_StatusTitle", "DisplayName": "وضعیت", "Alliance": "t1_StatusTitle", "Address": "CASE [t1].[StatusId]     WHEN 1032 THEN N''''در حال بررسی''     WHEN 1033 THEN N''''تاییده شده''     WHEN 1034 THEN N''''رد شده''     WHEN 1047 THEN N''''انجام شد''     ELSE CAST([t1].[StatusId] AS nvarchar(10))   END", "SystemTypeName": "String", "SystemType": 0}, {"TableName": "Srv.StationeryRequest", "SelectIndex": 0, "Visible": false, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Filterable": true, "Sortable": true, "ClassName": "", "Width": 70, "ColumnName": "t1_StatusId", "DisplayName": "کد وضعیت", "Alliance": "t1_StatusId", "Address": "[t1].[StatusId]", "SystemTypeName": "Int", "SystemType": 7}, {"TableName": "Hrm.OrgUnit", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Filterable": true, "Sortable": true, "ClassName": "", "Width": 140, "ColumnName": "t4_OrgUnitTitle", "DisplayName": "واحد سازمانی", "Alliance": "t4_OrgUnitTitle", "Address": "[t4].[Title]", "SystemTypeName": "String", "SystemType": 0}, {"TableName": "system.User", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Filterable": true, "Sortable": true, "ClassName": "", "Width": 130, "ColumnName": "t6_CreatedByName", "DisplayName": "ایجادکننده", "Alliance": "t6_CreatedByName", "Address": "[t6].[Name]", "SystemTypeName": "String", "SystemType": 0}, {"TableName": "Srv.StationeryRequest", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Filterable": true, "Sortable": true, "ClassName": "", "Width": 160, "ColumnName": "t1_TypeTitle", "DisplayName": "نوع لوازم", "Alliance": "t1_TypeTitle", "Address": "CASE [t1].[StationeryTypeId]     WHEN 1019 THEN N''''قیچی''     WHEN 1020 THEN N''''ماشین حساب''     WHEN 1021 THEN N''''گیره کاغذ''     WHEN 1022 THEN N''''کاغذ یادداشت''     WHEN 1023 THEN N''''ماژیک''     WHEN 1024 THEN N''''گیره پوشه''     WHEN 1025 THEN N''''ماژیک هایلایت''     WHEN 1026 THEN N''''چسب نواری''     WHEN 1027 THEN N''''کش پول''     WHEN 1028 THEN N''''پایه چسب''     WHEN 1029 THEN N''''زونکن''     WHEN 1030 THEN N''''کازیه''     WHEN 1048 THEN N''''خودکار آبی''     WHEN 1049 THEN N''''خودکار قرمز''     WHEN 1050 THEN N''''خودکار مشکی''     WHEN 1051 THEN N''''خودکار سبز''     WHEN 1058 THEN N''''برگه A4''     WHEN 1059 THEN N''''باطری قلمی''     WHEN 1060 THEN N''''باطری نیم قلمی''     WHEN 1061 THEN N''''چسب آبکی''     WHEN 1062 THEN N''''استیکر یادداشت رنگی''     WHEN 1063 THEN N''''استمپ''     WHEN 1064 THEN N''''جوهر استمپ''     WHEN 1065 THEN N''''تقویم رومیزی''     WHEN 1066 THEN N''''پانچ''     WHEN 1067 THEN N''''مداد''     WHEN 1068 THEN N''''مدادتراش''     WHEN 1069 THEN N''''مداد نوکی''     WHEN 1070 THEN N''''پاک کن''     WHEN 1071 THEN N''''دفترچه یادداشت''     WHEN 1078 THEN N''''منگنه''     WHEN 1079 THEN N''''سوزن منگنه''     WHEN 1080 THEN N''''سوزن کش''     WHEN 1112 THEN N''''چسب 5 سانتی''     WHEN 1113 THEN N''''ماژیک سی دی''     WHEN 1554 THEN N''''پاکت نامه''     WHEN 1555 THEN N''''پاکت نامه (A4)''     ELSE CAST([t1].[StationeryTypeId] AS nvarchar(10))   END", "SystemTypeName": "String", "SystemType": 0}, {"TableName": "Srv.StationeryRequest", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Filterable": true, "Sortable": true, "ClassName": "", "Width": 80, "ColumnName": "t1_Count", "DisplayName": "تعداد", "Alliance": "t1_Count", "Address": "[t1].[Count]", "SystemTypeName": "Int", "SystemType": 7}, {"TableName": "Srv.StationeryRequest", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Filterable": true, "Sortable": true, "ClassName": "", "Width": 180, "ColumnName": "t1_RequestDescription", "DisplayName": "متن درخواست", "Alliance": "t1_RequestDescription", "Address": "[t1].[RequestDescription]", "SystemTypeName": "String", "SystemType": 0}, {"TableName": "Srv.StationeryRequest", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Filterable": true, "Sortable": true, "ClassName": "", "Width": 180, "ColumnName": "t1_LastComment", "DisplayName": "کامنت آخر", "Alliance": "t1_LastComment", "Address": "[t1].[LastComment]", "SystemTypeName": "String", "SystemType": 0}, {"TableName": "Srv.StationeryRequest", "SelectIndex": 0, "Visible": true, "PrimaryKey": false, "SortDirection": null, "SortOrder": null, "GroupBy": false, "Options": [], "OptionSetting": null, "Aggregate": null, "Render": null, "IsCustom": false, "CustomColType": null, "HtmlTemplate": null, "BtnConfig": null, "InputConfig": null, "Filterable": true, "Sortable": true, "ClassName": "", "Width": 120, "ColumnName": "t1_CreatedDateInText", "DisplayName": "تاریخ ایجاد", "Alliance": "t1_CreatedDateInText", "Address": "[t1].[CreatedDateInText]", "SystemTypeName": "String", "SystemType": 0}]';
    DECLARE @DD NVARCHAR(10) = CHAR(36) + CHAR(36);
    DECLARE @OnRow NVARCHAR(MAX) = N'function(ctx) {
    var data = ctx.rowData || {};
    var status = data.t1_StatusId;
    var color = null;
    if (status == 1032 || status === ''1032'') color = ''yellow'';
    else if (status == 1033 || status === ''1033'') color = ''lightgreen'';
    else if (status == 1034 || status === ''1034'') color = ''red'';
    else if (status == 1047 || status === ''1047'') color = ''green'';
    if (!color || !ctx.table) return;
    var colIdx = -1;
    ctx.table.columns().every(function (i) {
        if (this.dataSrc() === ''t1_StatusTitle'') colIdx = i;
    });
    if (colIdx < 0) return;
    var node = ctx.table.cell(ctx.$row, colIdx).node();
    if (node) $(node).css(''background-color'', color);
}';
    DECLARE @OnSelTpl NVARCHAR(MAX) = N'function(ctx) {
    var $panel = __DD__(''#srvStationeryRequestComments'');
    if (!$panel || !$panel.length) $panel = $(''#srvStationeryRequestComments'');
    if (!$panel || !$panel.length) return;
    var $tbody = $panel.find(''tbody'');
    if (!$tbody.length) {
        $panel.html(''<table class="table table-striped table-row-bordered gy-2 w-100 mb-0"><thead><tr class="fw-bold fs-6 text-muted"><th>وضعیت</th><th>توضیح</th><th>ایجادکننده</th><th>تاریخ</th></tr></thead><tbody></tbody></table>'');
        $tbody = $panel.find(''tbody'');
    }
    var row = (!ctx.isDeselect && ctx.selectedRow) ? ctx.selectedRow : null;
    if (!row) {
        $tbody.html(''<tr><td colspan="4" class="text-muted">یک ردیف را انتخاب کنید</td></tr>'');
        return;
    }
    var id = row.t1_Id || row.id || ctx.primaryKeyValue;
    if (!id) return;
    $tbody.html(''<tr><td colspan="4" class="text-muted">در حال بارگذاری...</td></tr>'');
    __DD__.get(''/Panel/Srv/StationeryRequest/Comments?stationeryRequestId='' + id, function (r) {
        if (!r || r.isSuccess === false) {
            $tbody.html(''<tr><td colspan="4" class="text-danger">'' + ((r && r.message) || ''خطا در دریافت کامنت‌ها'') + ''</td></tr>'');
            return;
        }
        var items = (r.data) ? r.data : (Array.isArray(r) ? r : []);
        if (!items.length) {
            $tbody.html(''<tr><td colspan="4" class="text-muted">کامنتی ثبت نشده است</td></tr>'');
            return;
        }
        var html = '''';
        items.forEach(function (it) {
            html += ''<tr><td>'' + (it.statusTitle || ''-'') + ''</td><td>'' + (it.comment || ''-'') + ''</td><td>'' + (it.createdByName || ''-'') + ''</td><td>'' + (it.createdDateInText || ''-'') + ''</td></tr>'';
        });
        $tbody.html(html);
    });
}';
    DECLARE @OnSel NVARCHAR(MAX) = REPLACE(@OnSelTpl, N'__DD__', @DD);
    DECLARE @Ev NVARCHAR(MAX) = (SELECT @OnSel AS onSelectedRow, @OnRow AS onRowAdded FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    DECLARE @BtnFullTpl NVARCHAR(MAX) = N'[{"id": "srv_stationeryrequest_attachment", "title": "پیوست مدارک", "dataActionName": "attachment", "colorClass": "btn-color-info", "iconClass": "ki-outline ki-folder", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var pg = (window.appController && appController.activePage) ? appController.activePage : null; var getter = (pg && pg.getPartialView) ? pg.getPartialView.bind(pg) : null;\n    if (!getter) { toastr.error(''امکان باز کردن مودال پیوست وجود ندارد''); return; }\n    var $btn = ctx.$btn ? $(ctx.$btn.element || ctx.$btn).block() : null;\n    getter(''/Panel/Srv/StationeryRequest/Attachments?id='' + id, function (r) {\n        if ($btn) $btn.block(false);\n        if (!r) { toastr.error(''خطا در دریافت فرم'', ''خطا''); return; }\n        if (r.isSuccess === false) { toastr.error(r.message || ''خطا'', ''خطا''); return; }\n        var decoded = (window.appController && appController.decodePartialResponse) ? appController.decodePartialResponse(r) : { html: (typeof r === ''string'' ? r : ''''), scripts: [] };\n        var $bodyCn = $(''<div style=\\\"overflow-x:hidden;overflow-y:auto;max-height:75vh;\\\"></div>'').append(decoded.html || '''');\n        $.confirm({\n            title: ''پیوست مدارک'',\n            columnClass: ''col-md-10'',\n            content: $bodyCn,\n            rtl: true,\n            buttons: { cancel: { text: ''بستن'', btnClass: ''btn-secondary'' } },\n            onContentReady: function () {\n                var scripts = decoded.scripts || [];\n                scripts.forEach(function (code) {\n                    try { new Function(''page'', ''$$'', ''self'', code)(pg, (pg && pg.$$) ? pg.$$ : (typeof $$ !== ''undefined'' ? $$ : jQuery), pg ? pg.self : self); } catch (e) { console.error(e); }\n                });\n            }\n        });\n    });\n}"}, {"id": "srv_stationeryrequest_confirm", "title": "بررسی و تایید", "dataActionName": "confirm", "colorClass": "btn-color-success", "iconClass": "ki-outline ki-check", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var $form = $(''<div class=\\\"row g-3\\\"></div>'');\n    $form.append(''<div class=\\\"col-md-12 form-group-inline\\\"><label class=\\\"form-label fw-bold\\\">وضعیت</label><select class=\\\"form-control\\\" data-bind=\\\"statusId\\\" id=\\\"srvStStatus\\\"><option value=\\\"\\\">—</option><option value=\\\"1032\\\">در حال بررسی</option><option value=\\\"1033\\\">تاییده شده</option><option value=\\\"1034\\\">رد شده</option><option value=\\\"1047\\\">انجام شد</option></select></div>'');\n    $form.append(''<div class=\\\"col-md-12 form-group-inline\\\"><label class=\\\"form-label fw-bold required\\\">توضیحات</label><textarea class=\\\"form-control\\\" data-bind=\\\"comment\\\" data-required=\\\"true\\\" rows=\\\"3\\\"></textarea><div data-invalidmessagespan class=\\\"my-1 mx-1\\\"><span class=\\\"text-danger\\\"></span></div></div>'');\n    $.confirm({\n        title: ''بررسی و تایید'',\n        columnClass: ''col-md-8'',\n        content: $form,\n        rtl: true,\n        buttons: {\n            save: {\n                text: ''ثبت'',\n                btnClass: ''btn-primary'',\n                action: function () {\n                    var $content = this.$content;\n                    if (validateError($content)) return false;\n                    var comment = ($content.find(''[data-bind=\\\"comment\\\"]'').val() || '''').trim();\n                    var statusId = $content.find(''#srvStStatus'').val();\n                    if (!comment) { toastr.error(''توضیحات اجباری است'', ''خطا''); return false; }\n                    var selfConfirm = this;\n                    var $saveBtn = $(this.buttons.save.el).block();\n                    __DD__.post(''/Panel/Srv/StationeryRequest/Confirm'', {\n                        id: id,\n                        statusId: statusId ? parseInt(statusId, 10) : null,\n                        comment: comment\n                    }, function (resp) {\n                        $saveBtn.block(false);\n                        if (!resp || !resp.isSuccess) { toastr.error((resp && resp.message) || ''ثبت انجام نشد'', ''خطا''); return; }\n                        toastr.success(''بررسی و تایید با موفقیت ثبت شد'');\n                        selfConfirm.close();\n                        if (ctx.table && ctx.table.ajax) ctx.table.ajax.reload(null, false);\n                        else if (typeof appController !== ''undefined'' && appController.refreshCurrentPage) appController.refreshCurrentPage();\n                    });\n                    return false;\n                }\n            },\n            cancel: { text: ''انصراف'', btnClass: ''btn-secondary'' }\n        }\n    });\n}"}]';
    DECLARE @BtnAttachTpl NVARCHAR(MAX) = N'[{"id": "srv_stationeryrequest_attachment", "title": "پیوست مدارک", "dataActionName": "attachment", "colorClass": "btn-color-info", "iconClass": "ki-outline ki-folder", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var pg = (window.appController && appController.activePage) ? appController.activePage : null; var getter = (pg && pg.getPartialView) ? pg.getPartialView.bind(pg) : null;\n    if (!getter) { toastr.error(''امکان باز کردن مودال پیوست وجود ندارد''); return; }\n    var $btn = ctx.$btn ? $(ctx.$btn.element || ctx.$btn).block() : null;\n    getter(''/Panel/Srv/StationeryRequest/Attachments?id='' + id, function (r) {\n        if ($btn) $btn.block(false);\n        if (!r) { toastr.error(''خطا در دریافت فرم'', ''خطا''); return; }\n        if (r.isSuccess === false) { toastr.error(r.message || ''خطا'', ''خطا''); return; }\n        var decoded = (window.appController && appController.decodePartialResponse) ? appController.decodePartialResponse(r) : { html: (typeof r === ''string'' ? r : ''''), scripts: [] };\n        var $bodyCn = $(''<div style=\\\"overflow-x:hidden;overflow-y:auto;max-height:75vh;\\\"></div>'').append(decoded.html || '''');\n        $.confirm({\n            title: ''پیوست مدارک'',\n            columnClass: ''col-md-10'',\n            content: $bodyCn,\n            rtl: true,\n            buttons: { cancel: { text: ''بستن'', btnClass: ''btn-secondary'' } },\n            onContentReady: function () {\n                var scripts = decoded.scripts || [];\n                scripts.forEach(function (code) {\n                    try { new Function(''page'', ''$$'', ''self'', code)(pg, (pg && pg.$$) ? pg.$$ : (typeof $$ !== ''undefined'' ? $$ : jQuery), pg ? pg.self : self); } catch (e) { console.error(e); }\n                });\n            }\n        });\n    });\n}"}]';
    DECLARE @BtnConfirmTpl NVARCHAR(MAX) = N'[{"id": "srv_stationeryrequest_confirm", "title": "بررسی و تایید", "dataActionName": "confirm", "colorClass": "btn-color-success", "iconClass": "ki-outline ki-check", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var $form = $(''<div class=\\\"row g-3\\\"></div>'');\n    $form.append(''<div class=\\\"col-md-12 form-group-inline\\\"><label class=\\\"form-label fw-bold\\\">وضعیت</label><select class=\\\"form-control\\\" data-bind=\\\"statusId\\\" id=\\\"srvStStatus\\\"><option value=\\\"\\\">—</option><option value=\\\"1032\\\">در حال بررسی</option><option value=\\\"1033\\\">تاییده شده</option><option value=\\\"1034\\\">رد شده</option><option value=\\\"1047\\\">انجام شد</option></select></div>'');\n    $form.append(''<div class=\\\"col-md-12 form-group-inline\\\"><label class=\\\"form-label fw-bold required\\\">توضیحات</label><textarea class=\\\"form-control\\\" data-bind=\\\"comment\\\" data-required=\\\"true\\\" rows=\\\"3\\\"></textarea><div data-invalidmessagespan class=\\\"my-1 mx-1\\\"><span class=\\\"text-danger\\\"></span></div></div>'');\n    $.confirm({\n        title: ''بررسی و تایید'',\n        columnClass: ''col-md-8'',\n        content: $form,\n        rtl: true,\n        buttons: {\n            save: {\n                text: ''ثبت'',\n                btnClass: ''btn-primary'',\n                action: function () {\n                    var $content = this.$content;\n                    if (validateError($content)) return false;\n                    var comment = ($content.find(''[data-bind=\\\"comment\\\"]'').val() || '''').trim();\n                    var statusId = $content.find(''#srvStStatus'').val();\n                    if (!comment) { toastr.error(''توضیحات اجباری است'', ''خطا''); return false; }\n                    var selfConfirm = this;\n                    var $saveBtn = $(this.buttons.save.el).block();\n                    __DD__.post(''/Panel/Srv/StationeryRequest/Confirm'', {\n                        id: id,\n                        statusId: statusId ? parseInt(statusId, 10) : null,\n                        comment: comment\n                    }, function (resp) {\n                        $saveBtn.block(false);\n                        if (!resp || !resp.isSuccess) { toastr.error((resp && resp.message) || ''ثبت انجام نشد'', ''خطا''); return; }\n                        toastr.success(''بررسی و تایید با موفقیت ثبت شد'');\n                        selfConfirm.close();\n                        if (ctx.table && ctx.table.ajax) ctx.table.ajax.reload(null, false);\n                        else if (typeof appController !== ''undefined'' && appController.refreshCurrentPage) appController.refreshCurrentPage();\n                    });\n                    return false;\n                }\n            },\n            cancel: { text: ''انصراف'', btnClass: ''btn-secondary'' }\n        }\n    });\n}"}]';
    DECLARE @BtnFull NVARCHAR(MAX) = REPLACE(@BtnFullTpl, N'__DD__', @DD);
    DECLARE @BtnAttach NVARCHAR(MAX) = REPLACE(@BtnAttachTpl, N'__DD__', @DD);
    DECLARE @BtnConfirm NVARCHAR(MAX) = REPLACE(@BtnConfirmTpl, N'__DD__', @DD);

    IF OBJECT_ID('tempdb..#StProfiles') IS NOT NULL DROP TABLE #StProfiles;
    CREATE TABLE #StProfiles (
        Name NVARCHAR(100) NOT NULL PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        CustomQuery NVARCHAR(MAX) NOT NULL,
        ActionOptions NVARCHAR(MAX) NOT NULL,
        CustomButtons NVARCHAR(MAX) NOT NULL,
        RoleCsv NVARCHAR(500) NOT NULL,
        SavedQueryId BIGINT NULL
    );

    INSERT INTO #StProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv)
    VALUES (N'Srv_StationeryRequest_All', N'درخواست لوازم التحریر — همه', N'--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    CASE [t1].[StatusId]
        WHEN 1032 THEN N''در حال بررسی''
        WHEN 1033 THEN N''تاییده شده''
        WHEN 1034 THEN N''رد شده''
        WHEN 1047 THEN N''انجام شد''
        ELSE CAST([t1].[StatusId] AS nvarchar(10))
    END AS [t1_StatusTitle],
    [t1].[StatusId] AS [t1_StatusId],
    [t4].[Title] AS [t4_OrgUnitTitle],
    [t6].[Name] AS [t6_CreatedByName],
    CASE [t1].[StationeryTypeId]
        WHEN 1019 THEN N''قیچی''
        WHEN 1020 THEN N''ماشین حساب''
        WHEN 1021 THEN N''گیره کاغذ''
        WHEN 1022 THEN N''کاغذ یادداشت''
        WHEN 1023 THEN N''ماژیک''
        WHEN 1024 THEN N''گیره پوشه''
        WHEN 1025 THEN N''ماژیک هایلایت''
        WHEN 1026 THEN N''چسب نواری''
        WHEN 1027 THEN N''کش پول''
        WHEN 1028 THEN N''پایه چسب''
        WHEN 1029 THEN N''زونکن''
        WHEN 1030 THEN N''کازیه''
        WHEN 1048 THEN N''خودکار آبی''
        WHEN 1049 THEN N''خودکار قرمز''
        WHEN 1050 THEN N''خودکار مشکی''
        WHEN 1051 THEN N''خودکار سبز''
        WHEN 1058 THEN N''برگه A4''
        WHEN 1059 THEN N''باطری قلمی''
        WHEN 1060 THEN N''باطری نیم قلمی''
        WHEN 1061 THEN N''چسب آبکی''
        WHEN 1062 THEN N''استیکر یادداشت رنگی''
        WHEN 1063 THEN N''استمپ''
        WHEN 1064 THEN N''جوهر استمپ''
        WHEN 1065 THEN N''تقویم رومیزی''
        WHEN 1066 THEN N''پانچ''
        WHEN 1067 THEN N''مداد''
        WHEN 1068 THEN N''مدادتراش''
        WHEN 1069 THEN N''مداد نوکی''
        WHEN 1070 THEN N''پاک کن''
        WHEN 1071 THEN N''دفترچه یادداشت''
        WHEN 1078 THEN N''منگنه''
        WHEN 1079 THEN N''سوزن منگنه''
        WHEN 1080 THEN N''سوزن کش''
        WHEN 1112 THEN N''چسب 5 سانتی''
        WHEN 1113 THEN N''ماژیک سی دی''
        WHEN 1554 THEN N''پاکت نامه''
        WHEN 1555 THEN N''پاکت نامه (A4)''
        ELSE CAST([t1].[StationeryTypeId] AS nvarchar(10))
    END AS [t1_TypeTitle],
    [t1].[Count] AS [t1_Count],
    [t1].[RequestDescription] AS [t1_RequestDescription],
    [t1].[LastComment] AS [t1_LastComment],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[StationeryRequest] AS [t1]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[CreatedById]
LEFT JOIN [Hcm].[Personel] AS [t2] ON [t2].[PartyId] = [t6].[PartyId]
LEFT JOIN [Hrm].[OrgUnit] AS [t4] ON [t4].[Id] = [t2].[OrgUnitId]', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', N'[{"id": "srv_stationeryrequest_attachment", "title": "پیوست مدارک", "dataActionName": "attachment", "colorClass": "btn-color-info", "iconClass": "ki-outline ki-folder", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var pg = (window.appController && appController.activePage) ? appController.activePage : null; var getter = (pg && pg.getPartialView) ? pg.getPartialView.bind(pg) : null;\n    if (!getter) { toastr.error(''امکان باز کردن مودال پیوست وجود ندارد''); return; }\n    var $btn = ctx.$btn ? $(ctx.$btn.element || ctx.$btn).block() : null;\n    getter(''/Panel/Srv/StationeryRequest/Attachments?id='' + id, function (r) {\n        if ($btn) $btn.block(false);\n        if (!r) { toastr.error(''خطا در دریافت فرم'', ''خطا''); return; }\n        if (r.isSuccess === false) { toastr.error(r.message || ''خطا'', ''خطا''); return; }\n        var decoded = (window.appController && appController.decodePartialResponse) ? appController.decodePartialResponse(r) : { html: (typeof r === ''string'' ? r : ''''), scripts: [] };\n        var $bodyCn = $(''<div style=\\\"overflow-x:hidden;overflow-y:auto;max-height:75vh;\\\"></div>'').append(decoded.html || '''');\n        $.confirm({\n            title: ''پیوست مدارک'',\n            columnClass: ''col-md-10'',\n            content: $bodyCn,\n            rtl: true,\n            buttons: { cancel: { text: ''بستن'', btnClass: ''btn-secondary'' } },\n            onContentReady: function () {\n                var scripts = decoded.scripts || [];\n                scripts.forEach(function (code) {\n                    try { new Function(''page'', ''$$'', ''self'', code)(pg, (pg && pg.$$) ? pg.$$ : (typeof $$ !== ''undefined'' ? $$ : jQuery), pg ? pg.self : self); } catch (e) { console.error(e); }\n                });\n            }\n        });\n    });\n}"}, {"id": "srv_stationeryrequest_confirm", "title": "بررسی و تایید", "dataActionName": "confirm", "colorClass": "btn-color-success", "iconClass": "ki-outline ki-check", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var $form = $(''<div class=\\\"row g-3\\\"></div>'');\n    $form.append(''<div class=\\\"col-md-12 form-group-inline\\\"><label class=\\\"form-label fw-bold\\\">وضعیت</label><select class=\\\"form-control\\\" data-bind=\\\"statusId\\\" id=\\\"srvStStatus\\\"><option value=\\\"\\\">—</option><option value=\\\"1032\\\">در حال بررسی</option><option value=\\\"1033\\\">تاییده شده</option><option value=\\\"1034\\\">رد شده</option><option value=\\\"1047\\\">انجام شد</option></select></div>'');\n    $form.append(''<div class=\\\"col-md-12 form-group-inline\\\"><label class=\\\"form-label fw-bold required\\\">توضیحات</label><textarea class=\\\"form-control\\\" data-bind=\\\"comment\\\" data-required=\\\"true\\\" rows=\\\"3\\\"></textarea><div data-invalidmessagespan class=\\\"my-1 mx-1\\\"><span class=\\\"text-danger\\\"></span></div></div>'');\n    $.confirm({\n        title: ''بررسی و تایید'',\n        columnClass: ''col-md-8'',\n        content: $form,\n        rtl: true,\n        buttons: {\n            save: {\n                text: ''ثبت'',\n                btnClass: ''btn-primary'',\n                action: function () {\n                    var $content = this.$content;\n                    if (validateError($content)) return false;\n                    var comment = ($content.find(''[data-bind=\\\"comment\\\"]'').val() || '''').trim();\n                    var statusId = $content.find(''#srvStStatus'').val();\n                    if (!comment) { toastr.error(''توضیحات اجباری است'', ''خطا''); return false; }\n                    var selfConfirm = this;\n                    var $saveBtn = $(this.buttons.save.el).block();\n                    __DD__.post(''/Panel/Srv/StationeryRequest/Confirm'', {\n                        id: id,\n                        statusId: statusId ? parseInt(statusId, 10) : null,\n                        comment: comment\n                    }, function (resp) {\n                        $saveBtn.block(false);\n                        if (!resp || !resp.isSuccess) { toastr.error((resp && resp.message) || ''ثبت انجام نشد'', ''خطا''); return; }\n                        toastr.success(''بررسی و تایید با موفقیت ثبت شد'');\n                        selfConfirm.close();\n                        if (ctx.table && ctx.table.ajax) ctx.table.ajax.reload(null, false);\n                        else if (typeof appController !== ''undefined'' && appController.refreshCurrentPage) appController.refreshCurrentPage();\n                    });\n                    return false;\n                }\n            },\n            cancel: { text: ''انصراف'', btnClass: ''btn-secondary'' }\n        }\n    });\n}"}]', N'ShowAllMenus');
    INSERT INTO #StProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv)
    VALUES (N'Srv_StationeryRequest_Support', N'درخواست لوازم التحریر — پشتیبانی', N'DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
DECLARE @BranchCode INT = (
    SELECT TOP 1 p.BranchCode
    FROM [system].[User] u
    INNER JOIN [Hcm].[Personel] p ON p.PartyId = u.PartyId
    WHERE u.Id = @CuId
);
--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    CASE [t1].[StatusId]
        WHEN 1032 THEN N''در حال بررسی''
        WHEN 1033 THEN N''تاییده شده''
        WHEN 1034 THEN N''رد شده''
        WHEN 1047 THEN N''انجام شد''
        ELSE CAST([t1].[StatusId] AS nvarchar(10))
    END AS [t1_StatusTitle],
    [t1].[StatusId] AS [t1_StatusId],
    [t4].[Title] AS [t4_OrgUnitTitle],
    [t6].[Name] AS [t6_CreatedByName],
    CASE [t1].[StationeryTypeId]
        WHEN 1019 THEN N''قیچی''
        WHEN 1020 THEN N''ماشین حساب''
        WHEN 1021 THEN N''گیره کاغذ''
        WHEN 1022 THEN N''کاغذ یادداشت''
        WHEN 1023 THEN N''ماژیک''
        WHEN 1024 THEN N''گیره پوشه''
        WHEN 1025 THEN N''ماژیک هایلایت''
        WHEN 1026 THEN N''چسب نواری''
        WHEN 1027 THEN N''کش پول''
        WHEN 1028 THEN N''پایه چسب''
        WHEN 1029 THEN N''زونکن''
        WHEN 1030 THEN N''کازیه''
        WHEN 1048 THEN N''خودکار آبی''
        WHEN 1049 THEN N''خودکار قرمز''
        WHEN 1050 THEN N''خودکار مشکی''
        WHEN 1051 THEN N''خودکار سبز''
        WHEN 1058 THEN N''برگه A4''
        WHEN 1059 THEN N''باطری قلمی''
        WHEN 1060 THEN N''باطری نیم قلمی''
        WHEN 1061 THEN N''چسب آبکی''
        WHEN 1062 THEN N''استیکر یادداشت رنگی''
        WHEN 1063 THEN N''استمپ''
        WHEN 1064 THEN N''جوهر استمپ''
        WHEN 1065 THEN N''تقویم رومیزی''
        WHEN 1066 THEN N''پانچ''
        WHEN 1067 THEN N''مداد''
        WHEN 1068 THEN N''مدادتراش''
        WHEN 1069 THEN N''مداد نوکی''
        WHEN 1070 THEN N''پاک کن''
        WHEN 1071 THEN N''دفترچه یادداشت''
        WHEN 1078 THEN N''منگنه''
        WHEN 1079 THEN N''سوزن منگنه''
        WHEN 1080 THEN N''سوزن کش''
        WHEN 1112 THEN N''چسب 5 سانتی''
        WHEN 1113 THEN N''ماژیک سی دی''
        WHEN 1554 THEN N''پاکت نامه''
        WHEN 1555 THEN N''پاکت نامه (A4)''
        ELSE CAST([t1].[StationeryTypeId] AS nvarchar(10))
    END AS [t1_TypeTitle],
    [t1].[Count] AS [t1_Count],
    [t1].[RequestDescription] AS [t1_RequestDescription],
    [t1].[LastComment] AS [t1_LastComment],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[StationeryRequest] AS [t1]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[CreatedById]
LEFT JOIN [Hcm].[Personel] AS [t2] ON [t2].[PartyId] = [t6].[PartyId]
LEFT JOIN [Hrm].[OrgUnit] AS [t4] ON [t4].[Id] = [t2].[OrgUnitId]
WHERE @BranchCode IS NOT NULL
  AND (
        [t2].[BranchCode] = @BranchCode
        OR ([t2].[BranchCode] <> 2 AND @BranchCode = 1)
      )', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', N'[{"id": "srv_stationeryrequest_attachment", "title": "پیوست مدارک", "dataActionName": "attachment", "colorClass": "btn-color-info", "iconClass": "ki-outline ki-folder", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var pg = (window.appController && appController.activePage) ? appController.activePage : null; var getter = (pg && pg.getPartialView) ? pg.getPartialView.bind(pg) : null;\n    if (!getter) { toastr.error(''امکان باز کردن مودال پیوست وجود ندارد''); return; }\n    var $btn = ctx.$btn ? $(ctx.$btn.element || ctx.$btn).block() : null;\n    getter(''/Panel/Srv/StationeryRequest/Attachments?id='' + id, function (r) {\n        if ($btn) $btn.block(false);\n        if (!r) { toastr.error(''خطا در دریافت فرم'', ''خطا''); return; }\n        if (r.isSuccess === false) { toastr.error(r.message || ''خطا'', ''خطا''); return; }\n        var decoded = (window.appController && appController.decodePartialResponse) ? appController.decodePartialResponse(r) : { html: (typeof r === ''string'' ? r : ''''), scripts: [] };\n        var $bodyCn = $(''<div style=\\\"overflow-x:hidden;overflow-y:auto;max-height:75vh;\\\"></div>'').append(decoded.html || '''');\n        $.confirm({\n            title: ''پیوست مدارک'',\n            columnClass: ''col-md-10'',\n            content: $bodyCn,\n            rtl: true,\n            buttons: { cancel: { text: ''بستن'', btnClass: ''btn-secondary'' } },\n            onContentReady: function () {\n                var scripts = decoded.scripts || [];\n                scripts.forEach(function (code) {\n                    try { new Function(''page'', ''$$'', ''self'', code)(pg, (pg && pg.$$) ? pg.$$ : (typeof $$ !== ''undefined'' ? $$ : jQuery), pg ? pg.self : self); } catch (e) { console.error(e); }\n                });\n            }\n        });\n    });\n}"}, {"id": "srv_stationeryrequest_confirm", "title": "بررسی و تایید", "dataActionName": "confirm", "colorClass": "btn-color-success", "iconClass": "ki-outline ki-check", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var $form = $(''<div class=\\\"row g-3\\\"></div>'');\n    $form.append(''<div class=\\\"col-md-12 form-group-inline\\\"><label class=\\\"form-label fw-bold\\\">وضعیت</label><select class=\\\"form-control\\\" data-bind=\\\"statusId\\\" id=\\\"srvStStatus\\\"><option value=\\\"\\\">—</option><option value=\\\"1032\\\">در حال بررسی</option><option value=\\\"1033\\\">تاییده شده</option><option value=\\\"1034\\\">رد شده</option><option value=\\\"1047\\\">انجام شد</option></select></div>'');\n    $form.append(''<div class=\\\"col-md-12 form-group-inline\\\"><label class=\\\"form-label fw-bold required\\\">توضیحات</label><textarea class=\\\"form-control\\\" data-bind=\\\"comment\\\" data-required=\\\"true\\\" rows=\\\"3\\\"></textarea><div data-invalidmessagespan class=\\\"my-1 mx-1\\\"><span class=\\\"text-danger\\\"></span></div></div>'');\n    $.confirm({\n        title: ''بررسی و تایید'',\n        columnClass: ''col-md-8'',\n        content: $form,\n        rtl: true,\n        buttons: {\n            save: {\n                text: ''ثبت'',\n                btnClass: ''btn-primary'',\n                action: function () {\n                    var $content = this.$content;\n                    if (validateError($content)) return false;\n                    var comment = ($content.find(''[data-bind=\\\"comment\\\"]'').val() || '''').trim();\n                    var statusId = $content.find(''#srvStStatus'').val();\n                    if (!comment) { toastr.error(''توضیحات اجباری است'', ''خطا''); return false; }\n                    var selfConfirm = this;\n                    var $saveBtn = $(this.buttons.save.el).block();\n                    __DD__.post(''/Panel/Srv/StationeryRequest/Confirm'', {\n                        id: id,\n                        statusId: statusId ? parseInt(statusId, 10) : null,\n                        comment: comment\n                    }, function (resp) {\n                        $saveBtn.block(false);\n                        if (!resp || !resp.isSuccess) { toastr.error((resp && resp.message) || ''ثبت انجام نشد'', ''خطا''); return; }\n                        toastr.success(''بررسی و تایید با موفقیت ثبت شد'');\n                        selfConfirm.close();\n                        if (ctx.table && ctx.table.ajax) ctx.table.ajax.reload(null, false);\n                        else if (typeof appController !== ''undefined'' && appController.refreshCurrentPage) appController.refreshCurrentPage();\n                    });\n                    return false;\n                }\n            },\n            cancel: { text: ''انصراف'', btnClass: ''btn-secondary'' }\n        }\n    });\n}"}]', N'Srv.StationeryRequest.Support');
    INSERT INTO #StProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv)
    VALUES (N'Srv_StationeryRequest_RequesterConfirm', N'درخواست لوازم التحریر — درخواست‌کننده و تایید', N'--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    CASE [t1].[StatusId]
        WHEN 1032 THEN N''در حال بررسی''
        WHEN 1033 THEN N''تاییده شده''
        WHEN 1034 THEN N''رد شده''
        WHEN 1047 THEN N''انجام شد''
        ELSE CAST([t1].[StatusId] AS nvarchar(10))
    END AS [t1_StatusTitle],
    [t1].[StatusId] AS [t1_StatusId],
    [t4].[Title] AS [t4_OrgUnitTitle],
    [t6].[Name] AS [t6_CreatedByName],
    CASE [t1].[StationeryTypeId]
        WHEN 1019 THEN N''قیچی''
        WHEN 1020 THEN N''ماشین حساب''
        WHEN 1021 THEN N''گیره کاغذ''
        WHEN 1022 THEN N''کاغذ یادداشت''
        WHEN 1023 THEN N''ماژیک''
        WHEN 1024 THEN N''گیره پوشه''
        WHEN 1025 THEN N''ماژیک هایلایت''
        WHEN 1026 THEN N''چسب نواری''
        WHEN 1027 THEN N''کش پول''
        WHEN 1028 THEN N''پایه چسب''
        WHEN 1029 THEN N''زونکن''
        WHEN 1030 THEN N''کازیه''
        WHEN 1048 THEN N''خودکار آبی''
        WHEN 1049 THEN N''خودکار قرمز''
        WHEN 1050 THEN N''خودکار مشکی''
        WHEN 1051 THEN N''خودکار سبز''
        WHEN 1058 THEN N''برگه A4''
        WHEN 1059 THEN N''باطری قلمی''
        WHEN 1060 THEN N''باطری نیم قلمی''
        WHEN 1061 THEN N''چسب آبکی''
        WHEN 1062 THEN N''استیکر یادداشت رنگی''
        WHEN 1063 THEN N''استمپ''
        WHEN 1064 THEN N''جوهر استمپ''
        WHEN 1065 THEN N''تقویم رومیزی''
        WHEN 1066 THEN N''پانچ''
        WHEN 1067 THEN N''مداد''
        WHEN 1068 THEN N''مدادتراش''
        WHEN 1069 THEN N''مداد نوکی''
        WHEN 1070 THEN N''پاک کن''
        WHEN 1071 THEN N''دفترچه یادداشت''
        WHEN 1078 THEN N''منگنه''
        WHEN 1079 THEN N''سوزن منگنه''
        WHEN 1080 THEN N''سوزن کش''
        WHEN 1112 THEN N''چسب 5 سانتی''
        WHEN 1113 THEN N''ماژیک سی دی''
        WHEN 1554 THEN N''پاکت نامه''
        WHEN 1555 THEN N''پاکت نامه (A4)''
        ELSE CAST([t1].[StationeryTypeId] AS nvarchar(10))
    END AS [t1_TypeTitle],
    [t1].[Count] AS [t1_Count],
    [t1].[RequestDescription] AS [t1_RequestDescription],
    [t1].[LastComment] AS [t1_LastComment],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[StationeryRequest] AS [t1]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[CreatedById]
LEFT JOIN [Hcm].[Personel] AS [t2] ON [t2].[PartyId] = [t6].[PartyId]
LEFT JOIN [Hrm].[OrgUnit] AS [t4] ON [t4].[Id] = [t2].[OrgUnitId]
WHERE [t1].[CreatedById] = TRY_CAST(@CurrentUserId AS BIGINT)', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":false,"title":"خروجی اکسل"}]', N'[{"id": "srv_stationeryrequest_attachment", "title": "پیوست مدارک", "dataActionName": "attachment", "colorClass": "btn-color-info", "iconClass": "ki-outline ki-folder", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var pg = (window.appController && appController.activePage) ? appController.activePage : null; var getter = (pg && pg.getPartialView) ? pg.getPartialView.bind(pg) : null;\n    if (!getter) { toastr.error(''امکان باز کردن مودال پیوست وجود ندارد''); return; }\n    var $btn = ctx.$btn ? $(ctx.$btn.element || ctx.$btn).block() : null;\n    getter(''/Panel/Srv/StationeryRequest/Attachments?id='' + id, function (r) {\n        if ($btn) $btn.block(false);\n        if (!r) { toastr.error(''خطا در دریافت فرم'', ''خطا''); return; }\n        if (r.isSuccess === false) { toastr.error(r.message || ''خطا'', ''خطا''); return; }\n        var decoded = (window.appController && appController.decodePartialResponse) ? appController.decodePartialResponse(r) : { html: (typeof r === ''string'' ? r : ''''), scripts: [] };\n        var $bodyCn = $(''<div style=\\\"overflow-x:hidden;overflow-y:auto;max-height:75vh;\\\"></div>'').append(decoded.html || '''');\n        $.confirm({\n            title: ''پیوست مدارک'',\n            columnClass: ''col-md-10'',\n            content: $bodyCn,\n            rtl: true,\n            buttons: { cancel: { text: ''بستن'', btnClass: ''btn-secondary'' } },\n            onContentReady: function () {\n                var scripts = decoded.scripts || [];\n                scripts.forEach(function (code) {\n                    try { new Function(''page'', ''$$'', ''self'', code)(pg, (pg && pg.$$) ? pg.$$ : (typeof $$ !== ''undefined'' ? $$ : jQuery), pg ? pg.self : self); } catch (e) { console.error(e); }\n                });\n            }\n        });\n    });\n}"}, {"id": "srv_stationeryrequest_confirm", "title": "بررسی و تایید", "dataActionName": "confirm", "colorClass": "btn-color-success", "iconClass": "ki-outline ki-check", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var $form = $(''<div class=\\\"row g-3\\\"></div>'');\n    $form.append(''<div class=\\\"col-md-12 form-group-inline\\\"><label class=\\\"form-label fw-bold\\\">وضعیت</label><select class=\\\"form-control\\\" data-bind=\\\"statusId\\\" id=\\\"srvStStatus\\\"><option value=\\\"\\\">—</option><option value=\\\"1032\\\">در حال بررسی</option><option value=\\\"1033\\\">تاییده شده</option><option value=\\\"1034\\\">رد شده</option><option value=\\\"1047\\\">انجام شد</option></select></div>'');\n    $form.append(''<div class=\\\"col-md-12 form-group-inline\\\"><label class=\\\"form-label fw-bold required\\\">توضیحات</label><textarea class=\\\"form-control\\\" data-bind=\\\"comment\\\" data-required=\\\"true\\\" rows=\\\"3\\\"></textarea><div data-invalidmessagespan class=\\\"my-1 mx-1\\\"><span class=\\\"text-danger\\\"></span></div></div>'');\n    $.confirm({\n        title: ''بررسی و تایید'',\n        columnClass: ''col-md-8'',\n        content: $form,\n        rtl: true,\n        buttons: {\n            save: {\n                text: ''ثبت'',\n                btnClass: ''btn-primary'',\n                action: function () {\n                    var $content = this.$content;\n                    if (validateError($content)) return false;\n                    var comment = ($content.find(''[data-bind=\\\"comment\\\"]'').val() || '''').trim();\n                    var statusId = $content.find(''#srvStStatus'').val();\n                    if (!comment) { toastr.error(''توضیحات اجباری است'', ''خطا''); return false; }\n                    var selfConfirm = this;\n                    var $saveBtn = $(this.buttons.save.el).block();\n                    __DD__.post(''/Panel/Srv/StationeryRequest/Confirm'', {\n                        id: id,\n                        statusId: statusId ? parseInt(statusId, 10) : null,\n                        comment: comment\n                    }, function (resp) {\n                        $saveBtn.block(false);\n                        if (!resp || !resp.isSuccess) { toastr.error((resp && resp.message) || ''ثبت انجام نشد'', ''خطا''); return; }\n                        toastr.success(''بررسی و تایید با موفقیت ثبت شد'');\n                        selfConfirm.close();\n                        if (ctx.table && ctx.table.ajax) ctx.table.ajax.reload(null, false);\n                        else if (typeof appController !== ''undefined'' && appController.refreshCurrentPage) appController.refreshCurrentPage();\n                    });\n                    return false;\n                }\n            },\n            cancel: { text: ''انصراف'', btnClass: ''btn-secondary'' }\n        }\n    });\n}"}]', N'Srv.StationeryRequest.RequesterConfirm');
    INSERT INTO #StProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv)
    VALUES (N'Srv_StationeryRequest_HavayarControl', N'درخواست لوازم التحریر — هوایار کنترل', N'--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    CASE [t1].[StatusId]
        WHEN 1032 THEN N''در حال بررسی''
        WHEN 1033 THEN N''تاییده شده''
        WHEN 1034 THEN N''رد شده''
        WHEN 1047 THEN N''انجام شد''
        ELSE CAST([t1].[StatusId] AS nvarchar(10))
    END AS [t1_StatusTitle],
    [t1].[StatusId] AS [t1_StatusId],
    [t4].[Title] AS [t4_OrgUnitTitle],
    [t6].[Name] AS [t6_CreatedByName],
    CASE [t1].[StationeryTypeId]
        WHEN 1019 THEN N''قیچی''
        WHEN 1020 THEN N''ماشین حساب''
        WHEN 1021 THEN N''گیره کاغذ''
        WHEN 1022 THEN N''کاغذ یادداشت''
        WHEN 1023 THEN N''ماژیک''
        WHEN 1024 THEN N''گیره پوشه''
        WHEN 1025 THEN N''ماژیک هایلایت''
        WHEN 1026 THEN N''چسب نواری''
        WHEN 1027 THEN N''کش پول''
        WHEN 1028 THEN N''پایه چسب''
        WHEN 1029 THEN N''زونکن''
        WHEN 1030 THEN N''کازیه''
        WHEN 1048 THEN N''خودکار آبی''
        WHEN 1049 THEN N''خودکار قرمز''
        WHEN 1050 THEN N''خودکار مشکی''
        WHEN 1051 THEN N''خودکار سبز''
        WHEN 1058 THEN N''برگه A4''
        WHEN 1059 THEN N''باطری قلمی''
        WHEN 1060 THEN N''باطری نیم قلمی''
        WHEN 1061 THEN N''چسب آبکی''
        WHEN 1062 THEN N''استیکر یادداشت رنگی''
        WHEN 1063 THEN N''استمپ''
        WHEN 1064 THEN N''جوهر استمپ''
        WHEN 1065 THEN N''تقویم رومیزی''
        WHEN 1066 THEN N''پانچ''
        WHEN 1067 THEN N''مداد''
        WHEN 1068 THEN N''مدادتراش''
        WHEN 1069 THEN N''مداد نوکی''
        WHEN 1070 THEN N''پاک کن''
        WHEN 1071 THEN N''دفترچه یادداشت''
        WHEN 1078 THEN N''منگنه''
        WHEN 1079 THEN N''سوزن منگنه''
        WHEN 1080 THEN N''سوزن کش''
        WHEN 1112 THEN N''چسب 5 سانتی''
        WHEN 1113 THEN N''ماژیک سی دی''
        WHEN 1554 THEN N''پاکت نامه''
        WHEN 1555 THEN N''پاکت نامه (A4)''
        ELSE CAST([t1].[StationeryTypeId] AS nvarchar(10))
    END AS [t1_TypeTitle],
    [t1].[Count] AS [t1_Count],
    [t1].[RequestDescription] AS [t1_RequestDescription],
    [t1].[LastComment] AS [t1_LastComment],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[StationeryRequest] AS [t1]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[CreatedById]
LEFT JOIN [Hcm].[Personel] AS [t2] ON [t2].[PartyId] = [t6].[PartyId]
LEFT JOIN [Hrm].[OrgUnit] AS [t4] ON [t4].[Id] = [t2].[OrgUnitId]
WHERE [t1].[CreatedById] = TRY_CAST(@CurrentUserId AS BIGINT)', N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":false,"title":"خروجی اکسل"}]', N'[{"id": "srv_stationeryrequest_confirm", "title": "بررسی و تایید", "dataActionName": "confirm", "colorClass": "btn-color-success", "iconClass": "ki-outline ki-check", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var $form = $(''<div class=\\\"row g-3\\\"></div>'');\n    $form.append(''<div class=\\\"col-md-12 form-group-inline\\\"><label class=\\\"form-label fw-bold\\\">وضعیت</label><select class=\\\"form-control\\\" data-bind=\\\"statusId\\\" id=\\\"srvStStatus\\\"><option value=\\\"\\\">—</option><option value=\\\"1032\\\">در حال بررسی</option><option value=\\\"1033\\\">تاییده شده</option><option value=\\\"1034\\\">رد شده</option><option value=\\\"1047\\\">انجام شد</option></select></div>'');\n    $form.append(''<div class=\\\"col-md-12 form-group-inline\\\"><label class=\\\"form-label fw-bold required\\\">توضیحات</label><textarea class=\\\"form-control\\\" data-bind=\\\"comment\\\" data-required=\\\"true\\\" rows=\\\"3\\\"></textarea><div data-invalidmessagespan class=\\\"my-1 mx-1\\\"><span class=\\\"text-danger\\\"></span></div></div>'');\n    $.confirm({\n        title: ''بررسی و تایید'',\n        columnClass: ''col-md-8'',\n        content: $form,\n        rtl: true,\n        buttons: {\n            save: {\n                text: ''ثبت'',\n                btnClass: ''btn-primary'',\n                action: function () {\n                    var $content = this.$content;\n                    if (validateError($content)) return false;\n                    var comment = ($content.find(''[data-bind=\\\"comment\\\"]'').val() || '''').trim();\n                    var statusId = $content.find(''#srvStStatus'').val();\n                    if (!comment) { toastr.error(''توضیحات اجباری است'', ''خطا''); return false; }\n                    var selfConfirm = this;\n                    var $saveBtn = $(this.buttons.save.el).block();\n                    __DD__.post(''/Panel/Srv/StationeryRequest/Confirm'', {\n                        id: id,\n                        statusId: statusId ? parseInt(statusId, 10) : null,\n                        comment: comment\n                    }, function (resp) {\n                        $saveBtn.block(false);\n                        if (!resp || !resp.isSuccess) { toastr.error((resp && resp.message) || ''ثبت انجام نشد'', ''خطا''); return; }\n                        toastr.success(''بررسی و تایید با موفقیت ثبت شد'');\n                        selfConfirm.close();\n                        if (ctx.table && ctx.table.ajax) ctx.table.ajax.reload(null, false);\n                        else if (typeof appController !== ''undefined'' && appController.refreshCurrentPage) appController.refreshCurrentPage();\n                    });\n                    return false;\n                }\n            },\n            cancel: { text: ''انصراف'', btnClass: ''btn-secondary'' }\n        }\n    });\n}"}]', N'Srv.StationeryRequest.HavayarControl');
    INSERT INTO #StProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv)
    VALUES (N'Srv_StationeryRequest_Requester', N'درخواست لوازم التحریر — درخواست‌کننده', N'--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    CASE [t1].[StatusId]
        WHEN 1032 THEN N''در حال بررسی''
        WHEN 1033 THEN N''تاییده شده''
        WHEN 1034 THEN N''رد شده''
        WHEN 1047 THEN N''انجام شد''
        ELSE CAST([t1].[StatusId] AS nvarchar(10))
    END AS [t1_StatusTitle],
    [t1].[StatusId] AS [t1_StatusId],
    [t4].[Title] AS [t4_OrgUnitTitle],
    [t6].[Name] AS [t6_CreatedByName],
    CASE [t1].[StationeryTypeId]
        WHEN 1019 THEN N''قیچی''
        WHEN 1020 THEN N''ماشین حساب''
        WHEN 1021 THEN N''گیره کاغذ''
        WHEN 1022 THEN N''کاغذ یادداشت''
        WHEN 1023 THEN N''ماژیک''
        WHEN 1024 THEN N''گیره پوشه''
        WHEN 1025 THEN N''ماژیک هایلایت''
        WHEN 1026 THEN N''چسب نواری''
        WHEN 1027 THEN N''کش پول''
        WHEN 1028 THEN N''پایه چسب''
        WHEN 1029 THEN N''زونکن''
        WHEN 1030 THEN N''کازیه''
        WHEN 1048 THEN N''خودکار آبی''
        WHEN 1049 THEN N''خودکار قرمز''
        WHEN 1050 THEN N''خودکار مشکی''
        WHEN 1051 THEN N''خودکار سبز''
        WHEN 1058 THEN N''برگه A4''
        WHEN 1059 THEN N''باطری قلمی''
        WHEN 1060 THEN N''باطری نیم قلمی''
        WHEN 1061 THEN N''چسب آبکی''
        WHEN 1062 THEN N''استیکر یادداشت رنگی''
        WHEN 1063 THEN N''استمپ''
        WHEN 1064 THEN N''جوهر استمپ''
        WHEN 1065 THEN N''تقویم رومیزی''
        WHEN 1066 THEN N''پانچ''
        WHEN 1067 THEN N''مداد''
        WHEN 1068 THEN N''مدادتراش''
        WHEN 1069 THEN N''مداد نوکی''
        WHEN 1070 THEN N''پاک کن''
        WHEN 1071 THEN N''دفترچه یادداشت''
        WHEN 1078 THEN N''منگنه''
        WHEN 1079 THEN N''سوزن منگنه''
        WHEN 1080 THEN N''سوزن کش''
        WHEN 1112 THEN N''چسب 5 سانتی''
        WHEN 1113 THEN N''ماژیک سی دی''
        WHEN 1554 THEN N''پاکت نامه''
        WHEN 1555 THEN N''پاکت نامه (A4)''
        ELSE CAST([t1].[StationeryTypeId] AS nvarchar(10))
    END AS [t1_TypeTitle],
    [t1].[Count] AS [t1_Count],
    [t1].[RequestDescription] AS [t1_RequestDescription],
    [t1].[LastComment] AS [t1_LastComment],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[StationeryRequest] AS [t1]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[CreatedById]
LEFT JOIN [Hcm].[Personel] AS [t2] ON [t2].[PartyId] = [t6].[PartyId]
LEFT JOIN [Hrm].[OrgUnit] AS [t4] ON [t4].[Id] = [t2].[OrgUnitId]
WHERE [t1].[CreatedById] = TRY_CAST(@CurrentUserId AS BIGINT)', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":false,"title":"خروجی اکسل"}]', N'[{"id": "srv_stationeryrequest_attachment", "title": "پیوست مدارک", "dataActionName": "attachment", "colorClass": "btn-color-info", "iconClass": "ki-outline ki-folder", "requiresSelection": true, "requiresMultiSelection": false, "useHtml": false, "html": "", "actionScript": "function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var pg = (window.appController && appController.activePage) ? appController.activePage : null; var getter = (pg && pg.getPartialView) ? pg.getPartialView.bind(pg) : null;\n    if (!getter) { toastr.error(''امکان باز کردن مودال پیوست وجود ندارد''); return; }\n    var $btn = ctx.$btn ? $(ctx.$btn.element || ctx.$btn).block() : null;\n    getter(''/Panel/Srv/StationeryRequest/Attachments?id='' + id, function (r) {\n        if ($btn) $btn.block(false);\n        if (!r) { toastr.error(''خطا در دریافت فرم'', ''خطا''); return; }\n        if (r.isSuccess === false) { toastr.error(r.message || ''خطا'', ''خطا''); return; }\n        var decoded = (window.appController && appController.decodePartialResponse) ? appController.decodePartialResponse(r) : { html: (typeof r === ''string'' ? r : ''''), scripts: [] };\n        var $bodyCn = $(''<div style=\\\"overflow-x:hidden;overflow-y:auto;max-height:75vh;\\\"></div>'').append(decoded.html || '''');\n        $.confirm({\n            title: ''پیوست مدارک'',\n            columnClass: ''col-md-10'',\n            content: $bodyCn,\n            rtl: true,\n            buttons: { cancel: { text: ''بستن'', btnClass: ''btn-secondary'' } },\n            onContentReady: function () {\n                var scripts = decoded.scripts || [];\n                scripts.forEach(function (code) {\n                    try { new Function(''page'', ''$$'', ''self'', code)(pg, (pg && pg.$$) ? pg.$$ : (typeof $$ !== ''undefined'' ? $$ : jQuery), pg ? pg.self : self); } catch (e) { console.error(e); }\n                });\n            }\n        });\n    });\n}"}]', N'Srv.StationeryRequest.Requester');
    INSERT INTO #StProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv)
    VALUES (N'Srv_StationeryRequest_View', N'درخواست لوازم التحریر — مشاهده', N'--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    CASE [t1].[StatusId]
        WHEN 1032 THEN N''در حال بررسی''
        WHEN 1033 THEN N''تاییده شده''
        WHEN 1034 THEN N''رد شده''
        WHEN 1047 THEN N''انجام شد''
        ELSE CAST([t1].[StatusId] AS nvarchar(10))
    END AS [t1_StatusTitle],
    [t1].[StatusId] AS [t1_StatusId],
    [t4].[Title] AS [t4_OrgUnitTitle],
    [t6].[Name] AS [t6_CreatedByName],
    CASE [t1].[StationeryTypeId]
        WHEN 1019 THEN N''قیچی''
        WHEN 1020 THEN N''ماشین حساب''
        WHEN 1021 THEN N''گیره کاغذ''
        WHEN 1022 THEN N''کاغذ یادداشت''
        WHEN 1023 THEN N''ماژیک''
        WHEN 1024 THEN N''گیره پوشه''
        WHEN 1025 THEN N''ماژیک هایلایت''
        WHEN 1026 THEN N''چسب نواری''
        WHEN 1027 THEN N''کش پول''
        WHEN 1028 THEN N''پایه چسب''
        WHEN 1029 THEN N''زونکن''
        WHEN 1030 THEN N''کازیه''
        WHEN 1048 THEN N''خودکار آبی''
        WHEN 1049 THEN N''خودکار قرمز''
        WHEN 1050 THEN N''خودکار مشکی''
        WHEN 1051 THEN N''خودکار سبز''
        WHEN 1058 THEN N''برگه A4''
        WHEN 1059 THEN N''باطری قلمی''
        WHEN 1060 THEN N''باطری نیم قلمی''
        WHEN 1061 THEN N''چسب آبکی''
        WHEN 1062 THEN N''استیکر یادداشت رنگی''
        WHEN 1063 THEN N''استمپ''
        WHEN 1064 THEN N''جوهر استمپ''
        WHEN 1065 THEN N''تقویم رومیزی''
        WHEN 1066 THEN N''پانچ''
        WHEN 1067 THEN N''مداد''
        WHEN 1068 THEN N''مدادتراش''
        WHEN 1069 THEN N''مداد نوکی''
        WHEN 1070 THEN N''پاک کن''
        WHEN 1071 THEN N''دفترچه یادداشت''
        WHEN 1078 THEN N''منگنه''
        WHEN 1079 THEN N''سوزن منگنه''
        WHEN 1080 THEN N''سوزن کش''
        WHEN 1112 THEN N''چسب 5 سانتی''
        WHEN 1113 THEN N''ماژیک سی دی''
        WHEN 1554 THEN N''پاکت نامه''
        WHEN 1555 THEN N''پاکت نامه (A4)''
        ELSE CAST([t1].[StationeryTypeId] AS nvarchar(10))
    END AS [t1_TypeTitle],
    [t1].[Count] AS [t1_Count],
    [t1].[RequestDescription] AS [t1_RequestDescription],
    [t1].[LastComment] AS [t1_LastComment],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[StationeryRequest] AS [t1]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[CreatedById]
LEFT JOIN [Hcm].[Personel] AS [t2] ON [t2].[PartyId] = [t6].[PartyId]
LEFT JOIN [Hrm].[OrgUnit] AS [t4] ON [t4].[Id] = [t2].[OrgUnitId]
WHERE [t1].[CreatedById] = TRY_CAST(@CurrentUserId AS BIGINT)', N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":false,"title":"خروجی اکسل"}]', N'[]', N'Srv.StationeryRequest.View');

    DECLARE @MissingRoles NVARCHAR(MAX) = NULL;
    ;WITH NeedRoles AS (
        SELECT DISTINCT LTRIM(RTRIM(value)) AS RoleName
        FROM #StProfiles CROSS APPLY STRING_SPLIT(RoleCsv, N',')
    )
    SELECT @MissingRoles = STRING_AGG(nr.RoleName, N', ')
    FROM NeedRoles nr
    WHERE NOT EXISTS (SELECT 1 FROM system.Role r WHERE r.Name = nr.RoleName);
    IF @MissingRoles IS NOT NULL AND LEN(@MissingRoles) > 0
    BEGIN
        SET @MissingRoles = N'نقش‌های درخواست لوازم التحریر یافت نشد. ابتدا Seed_SrvStationeryRequest_RolesAndAccess.sql را اجرا کنید. نقش‌های مفقود: ' + @MissingRoles;
        THROW 51720, @MissingRoles, 1;
    END

    -- Rebind custom buttons from template vars (profiles already store final; replace __DD__ again for safety)
    UPDATE #StProfiles SET CustomButtons = @BtnFull
    WHERE Name IN (N'Srv_StationeryRequest_All', N'Srv_StationeryRequest_Support', N'Srv_StationeryRequest_RequesterConfirm');
    UPDATE #StProfiles SET CustomButtons = @BtnConfirm
    WHERE Name = N'Srv_StationeryRequest_HavayarControl';
    UPDATE #StProfiles SET CustomButtons = @BtnAttach
    WHERE Name = N'Srv_StationeryRequest_Requester';
    UPDATE #StProfiles SET CustomButtons = N'[]'
    WHERE Name = N'Srv_StationeryRequest_View';

    DECLARE @Name NVARCHAR(100), @Title NVARCHAR(200), @Sel NVARCHAR(MAX);
    DECLARE @Act NVARCHAR(MAX), @Btn NVARCHAR(MAX), @RoleCsv NVARCHAR(500);
    DECLARE @Qj NVARCHAR(MAX), @Pid BIGINT;

    DECLARE pc CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv FROM #StProfiles;
    OPEN pc;
    FETCH NEXT FROM pc INTO @Name, @Title, @Sel, @Act, @Btn, @RoleCsv;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Qj = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @Sel);
        IF ISJSON(@Cols) <> 1 OR ISJSON(@Qj) <> 1 OR ISJSON(@Act) <> 1 OR ISJSON(@Btn) <> 1 OR ISJSON(@Ev) <> 1
            THROW 51721, N'JSON نمایه درخواست لوازم التحریر معتبر نیست.', 1;

        IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
        BEGIN
            UPDATE system.SavedQuery SET Title=@Title, QueryJson=@Qj, ColumnsJson=@Cols, EntityFullName=@Ent, Mode=1, Type=1,
                ActionOptions=@Act, CustomActionButtonsJson=@Btn, EventScriptsJson=@Ev, DiagramJson=N'{}',
                ModifiedById=1, ModifiedByName=@SeedUser, ModifiedDateMiladiDateTime=@Now, ModifiedDateShamsiDateTime=@NowShamsi, IsActive=1
            WHERE Name=@Name;
            SELECT @Pid = Id FROM system.SavedQuery WHERE Name=@Name;
            PRINT N'  UPDATED ' + @Name;
        END
        ELSE
        BEGIN
            INSERT INTO system.SavedQuery
                (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
                 Mode, Type, EntityFullName, ActionOptions,
                 CreatedById, ModifiedById, CreatedByName, ModifiedByName,
                 CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
            VALUES (@Name, @Title, @Qj, @Cols, N'{}', @Btn, @Ev,
                 1, 1, @Ent, @Act, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
            SET @Pid = SCOPE_IDENTITY();
            PRINT N'  CREATED ' + @Name;
        END

        UPDATE #StProfiles SET SavedQueryId = @Pid WHERE Name = @Name;

        DELETE ra FROM system.RoleAccess ra WHERE ra.ActionAccessType=3 AND ra.RowId=@Pid;
        INSERT INTO system.RoleAccess
            (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, @Pascal, @Title, NULL, @Pid, r.Id,
            1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
        FROM system.Role r
        WHERE r.Name IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@RoleCsv, N','));
        PRINT N'  Profile RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20)) + N' for ' + @Name;

        FETCH NEXT FROM pc INTO @Name, @Title, @Sel, @Act, @Btn, @RoleCsv;
    END
    CLOSE pc; DEALLOCATE pc;

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_SrvStationeryRequest_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name LIKE N'Srv_StationeryRequest_%' ORDER BY Name;
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Srv_StationeryRequest_All'
ORDER BY CAST(j.[key] AS int);
