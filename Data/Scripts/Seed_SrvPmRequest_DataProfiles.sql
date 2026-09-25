/*
Seed_SrvPmRequest_DataProfiles.sql
نمایه داده درخواست PM (HTS صفحه 399 / Srv_PmRequest). UTF-8 BOM. Idempotent.
شش نمایه با ستون‌های یکسان؛ فیلتر نقش در CustomQuery. بدون دکمه چاپ.
sqlcmd -S PortalSRV\PORTAL -d HavayarApp -C -b -I -x -f 65001 -i "Data\Scripts\Seed_SrvPmRequest_DataProfiles.sql"
(-x disables sqlcmd dollar substitution; jQuery alias rebuilt via CHAR(36)+CHAR(36).)
Requires roles from Seed_SrvPmRequest_RolesAndAccess.sql.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-srv-pmrequest-dataprofile';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.srv.pmrequest';
    DECLARE @Pascal NVARCHAR(200) = N'Entities.App.Srv.PmRequest';
    DECLARE @Cols NVARCHAR(MAX) = N'[{"TableName":"Srv.PmRequest","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":70,"ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SystemType":6},{"TableName":"Srv.PmRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":110,"ColumnName":"t1_StatusTitle","DisplayName":"وضعیت","Alliance":"t1_StatusTitle","Address":"CASE [t1].[StatusId] WHEN 1032 THEN Nدر حال بررسی WHEN 1033 THEN Nتاییده شده WHEN 1034 THEN Nرد شده WHEN 1047 THEN Nانجام شد WHEN 0 THEN N0 ELSE CAST([t1].[StatusId] AS nvarchar(10)) END","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.PmRequest","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":70,"ColumnName":"t1_StatusId","DisplayName":"کد وضعیت","Alliance":"t1_StatusId","Address":"[t1].[StatusId]","SystemTypeName":"Int","SystemType":7},{"TableName":"Srv.PmRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Srv.Enums.PmRequestTypeEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":120,"ColumnName":"t1_TypeId","DisplayName":"نوع","Alliance":"t1_TypeId","Address":"[t1].[TypeId]","SystemTypeName":"Select","SystemType":8},{"TableName":"Hrm.OrgUnit","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":160,"ColumnName":"t7_OrgUnitTitle","DisplayName":"واحد سازمانی","Alliance":"t7_OrgUnitTitle","Address":"COALESCE([t7].[Title], [t1].[CreatedOrganizationUnitTitle])","SystemTypeName":"String","SystemType":0},{"TableName":"system.User","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":130,"ColumnName":"t6_CreatedByName","DisplayName":"ایجادکننده","Alliance":"t6_CreatedByName","Address":"[t6].[Name]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.PmRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":220,"ColumnName":"t1_RequestText","DisplayName":"متن درخواست","Alliance":"t1_RequestText","Address":"[t1].[RequestText]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.PmRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":120,"ColumnName":"t1_ApproximateExecutionDate","DisplayName":"تاریخ تقریبی اجرا","Alliance":"t1_ApproximateExecutionDate","Address":"[t1].[ApproximateExecutionDate]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.PmRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":180,"ColumnName":"t1_LastComment","DisplayName":"آخرین کامنت","Alliance":"t1_LastComment","Address":"[t1].[LastComment]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.PmRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":120,"ColumnName":"t1_CreatedDateInText","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedDateInText","Address":"[t1].[CreatedDateInText]","SystemTypeName":"String","SystemType":0}]';
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
    var $panel = __DD__(''#srvPmRequestComments'');
    if (!$panel || !$panel.length) $panel = $(''#srvPmRequestComments'');
    if (!$panel || !$panel.length) return;
    var $tbody = $panel.find(''tbody'');
    if (!$tbody.length) {
        $panel.html(''<table class="table table-striped table-row-bordered gy-2 w-100 mb-0"><thead><tr class="fw-bold fs-6 text-muted"><th>وضعیت</th><th>توضیح</th><th>تاریخ تقریبی اجرا</th><th>ایجادکننده</th><th>تاریخ</th></tr></thead><tbody></tbody></table>'');
        $tbody = $panel.find(''tbody'');
    }
    var row = (!ctx.isDeselect && ctx.selectedRow) ? ctx.selectedRow : null;
    var $edit = ctx.dataActionBtns && ctx.dataActionBtns.edit;
    var $del = ctx.dataActionBtns && ctx.dataActionBtns.delete;
    if (!row) {
        $tbody.html(''<tr><td colspan="5" class="text-muted">یک ردیف را انتخاب کنید</td></tr>'');
        if ($edit && $edit.length) $edit.removeAttr(''forcedisabled'');
        if ($del && $del.length) $del.removeAttr(''forcedisabled'');
        return;
    }
    var status = row.t1_StatusId;
    var isDone = (status == 1047 || status === ''1047'');
    if ($edit && $edit.length) {
        if (isDone) { $edit.attr(''forcedisabled'', ''true'').prop(''disabled'', true).removeClass(''btn-color-warning''); }
        else { $edit.removeAttr(''forcedisabled'').prop(''disabled'', false).addClass(''btn-color-warning''); }
    }
    if ($del && $del.length) {
        if (isDone) { $del.attr(''forcedisabled'', ''true'').prop(''disabled'', true).removeClass(''btn-color-danger''); }
        else { $del.removeAttr(''forcedisabled'').prop(''disabled'', false).addClass(''btn-color-danger''); }
    }
    var id = row.t1_Id || row.id || ctx.primaryKeyValue;
    if (!id) return;
    $tbody.html(''<tr><td colspan="5" class="text-muted">در حال بارگذاری...</td></tr>'');
    __DD__.get(''/Panel/Srv/PmRequest/comments?pmRequestId='' + id, function (r) {
        if (!r || r.isSuccess === false) {
            $tbody.html(''<tr><td colspan="5" class="text-danger">'' + ((r && r.message) || ''خطا در دریافت کامنت‌ها'') + ''</td></tr>'');
            return;
        }
        var items = (r.data) ? r.data : (Array.isArray(r) ? r : []);
        if (!items.length) {
            $tbody.html(''<tr><td colspan="5" class="text-muted">کامنتی ثبت نشده است</td></tr>'');
            return;
        }
        var html = '''';
        items.forEach(function (it) {
            html += ''<tr><td>'' + (it.statusTitle || ''-'') + ''</td><td>'' + (it.comment || ''-'') + ''</td><td>'' + (it.approximateExecutionDate || ''-'') + ''</td><td>'' + (it.createdByName || ''-'') + ''</td><td>'' + (it.createdDateInText || ''-'') + ''</td></tr>'';
        });
        $tbody.html(html);
    });
}';
    DECLARE @OnSel NVARCHAR(MAX) = REPLACE(@OnSelTpl, N'__DD__', @DD);
    DECLARE @Ev NVARCHAR(MAX) = (SELECT @OnSel AS onSelectedRow, @OnRow AS onRowAdded FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    DECLARE @BtnFullTpl NVARCHAR(MAX) = N'[{"id":"srv_pmrequest_attachment","title":"پیوست مدارک","dataActionName":"attachment","colorClass":"btn-color-info","iconClass":"ki-outline ki-folder","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var getter = (typeof page !== ''undefined'' && page.getPartialView) ? page.getPartialView.bind(page) : null;\n    if (!getter) { toastr.error(''امکان باز کردن مودال پیوست وجود ندارد''); return; }\n    var $btn = ctx.$btn ? $(ctx.$btn.element || ctx.$btn).block() : null;\n    getter(''/Panel/Srv/PmRequest/Attachments?id='' + id, function (r) {\n        if ($btn) $btn.block(false);\n        if (!r) { toastr.error(''خطا در دریافت فرم'', ''خطا''); return; }\n        if (r.isSuccess === false) { toastr.error(r.message || ''خطا'', ''خطا''); return; }\n        var decoded = (window.appController && appController.decodePartialResponse) ? appController.decodePartialResponse(r) : { html: (typeof r === ''string'' ? r : ''''), scripts: [] };\n        var $bodyCn = $(''<div style=\"overflow-x:hidden;overflow-y:auto;max-height:75vh;\"></div>'').append(decoded.html || '''');\n        $.confirm({\n            title: ''پیوست مدارک'',\n            columnClass: ''col-md-10'',\n            content: $bodyCn,\n            rtl: true,\n            buttons: { cancel: { text: ''بستن'', btnClass: ''btn-secondary'' } },\n            onContentReady: function () {\n                var scripts = decoded.scripts || [];\n                scripts.forEach(function (code) {\n                    try { new Function(''page'', ''__DD__'', ''self'', code)(page, (typeof __DD__ !== ''undefined'' ? __DD__ : jQuery), self); } catch (e) { console.error(e); }\n                });\n            }\n        });\n    });\n}"},{"id":"srv_pmrequest_confirm","title":"بررسی / تایید","dataActionName":"confirm","colorClass":"btn-color-success","iconClass":"ki-outline ki-check","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var $form = $(''<div class=\"row g-3\"></div>'');\n    $form.append(''<div class=\"col-md-12 form-group-inline\"><label class=\"form-label fw-bold required\">وضعیت</label><select class=\"form-control\" data-bind=\"statusId\" data-required=\"true\" id=\"srvPmStatus\"><option value=\"\">انتخاب کنید</option><option value=\"1032\">در حال بررسی</option><option value=\"1033\">تاییده شده</option><option value=\"1034\">رد شده</option><option value=\"1047\">انجام شد</option></select><div data-invalidmessagespan class=\"my-1 mx-1\"><span class=\"text-danger\"></span></div></div>'');\n    $form.append(''<div class=\"col-md-12 form-group-inline\"><label class=\"form-label fw-bold\">تاریخ تقریبی اجرا</label><input class=\"form-control\" data-bind=\"approximateExecutionDate\" data-persionDatePicker=\"true\" id=\"srvPmApproxDate\" /><div data-invalidmessagespan class=\"my-1 mx-1\"><span class=\"text-danger\"></span></div></div>'');\n    $form.append(''<div class=\"col-md-12 form-group-inline\"><label class=\"form-label fw-bold required\">توضیحات</label><textarea class=\"form-control\" data-bind=\"comment\" data-required=\"true\" rows=\"3\"></textarea><div data-invalidmessagespan class=\"my-1 mx-1\"><span class=\"text-danger\"></span></div></div>'');\n    $.confirm({\n        title: ''بررسی / تایید'',\n        columnClass: ''col-md-8'',\n        content: $form,\n        rtl: true,\n        buttons: {\n            save: {\n                text: ''ثبت'',\n                btnClass: ''btn-primary'',\n                action: function () {\n                    var $content = this.$content;\n                    if (validateError($content)) return false;\n                    var comment = ($content.find(''[data-bind=\"comment\"]'').val() || '''').trim();\n                    var statusId = $content.find(''#srvPmStatus'').val();\n                    var approx = ($content.find(''[data-bind=\"approximateExecutionDate\"]'').val() || '''').trim();\n                    if (!statusId) { toastr.error(''وضعیت اجباری است'', ''خطا''); return false; }\n                    if (!comment) { toastr.error(''توضیحات اجباری است'', ''خطا''); return false; }\n                    var selfConfirm = this;\n                    var $saveBtn = $(this.buttons.save.el).block();\n                    __DD__.post(''/Panel/Srv/PmRequest/confirm'', {\n                        id: id,\n                        statusId: parseInt(statusId, 10),\n                        approximateExecutionDate: approx || null,\n                        comment: comment\n                    }, function (resp) {\n                        $saveBtn.block(false);\n                        if (!resp || !resp.isSuccess) { toastr.error((resp && resp.message) || ''ثبت انجام نشد'', ''خطا''); return; }\n                        toastr.success(''بررسی / تایید با موفقیت ثبت شد'');\n                        selfConfirm.close();\n                        if (ctx.table && ctx.table.ajax) ctx.table.ajax.reload(null, false);\n                        else if (typeof appController !== ''undefined'' && appController.refreshCurrentPage) appController.refreshCurrentPage();\n                    });\n                    return false;\n                }\n            },\n            cancel: { text: ''انصراف'', btnClass: ''btn-secondary'' }\n        },\n        onContentReady: function () {\n            if (typeof initPersionDatePicker === ''function'') initPersionDatePicker(this.$content);\n        }\n    });\n}"}]';
    DECLARE @BtnNoAttachTpl NVARCHAR(MAX) = N'[{"id":"srv_pmrequest_confirm","title":"بررسی / تایید","dataActionName":"confirm","colorClass":"btn-color-success","iconClass":"ki-outline ki-check","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var $form = $(''<div class=\"row g-3\"></div>'');\n    $form.append(''<div class=\"col-md-12 form-group-inline\"><label class=\"form-label fw-bold required\">وضعیت</label><select class=\"form-control\" data-bind=\"statusId\" data-required=\"true\" id=\"srvPmStatus\"><option value=\"\">انتخاب کنید</option><option value=\"1032\">در حال بررسی</option><option value=\"1033\">تاییده شده</option><option value=\"1034\">رد شده</option><option value=\"1047\">انجام شد</option></select><div data-invalidmessagespan class=\"my-1 mx-1\"><span class=\"text-danger\"></span></div></div>'');\n    $form.append(''<div class=\"col-md-12 form-group-inline\"><label class=\"form-label fw-bold\">تاریخ تقریبی اجرا</label><input class=\"form-control\" data-bind=\"approximateExecutionDate\" data-persionDatePicker=\"true\" id=\"srvPmApproxDate\" /><div data-invalidmessagespan class=\"my-1 mx-1\"><span class=\"text-danger\"></span></div></div>'');\n    $form.append(''<div class=\"col-md-12 form-group-inline\"><label class=\"form-label fw-bold required\">توضیحات</label><textarea class=\"form-control\" data-bind=\"comment\" data-required=\"true\" rows=\"3\"></textarea><div data-invalidmessagespan class=\"my-1 mx-1\"><span class=\"text-danger\"></span></div></div>'');\n    $.confirm({\n        title: ''بررسی / تایید'',\n        columnClass: ''col-md-8'',\n        content: $form,\n        rtl: true,\n        buttons: {\n            save: {\n                text: ''ثبت'',\n                btnClass: ''btn-primary'',\n                action: function () {\n                    var $content = this.$content;\n                    if (validateError($content)) return false;\n                    var comment = ($content.find(''[data-bind=\"comment\"]'').val() || '''').trim();\n                    var statusId = $content.find(''#srvPmStatus'').val();\n                    var approx = ($content.find(''[data-bind=\"approximateExecutionDate\"]'').val() || '''').trim();\n                    if (!statusId) { toastr.error(''وضعیت اجباری است'', ''خطا''); return false; }\n                    if (!comment) { toastr.error(''توضیحات اجباری است'', ''خطا''); return false; }\n                    var selfConfirm = this;\n                    var $saveBtn = $(this.buttons.save.el).block();\n                    __DD__.post(''/Panel/Srv/PmRequest/confirm'', {\n                        id: id,\n                        statusId: parseInt(statusId, 10),\n                        approximateExecutionDate: approx || null,\n                        comment: comment\n                    }, function (resp) {\n                        $saveBtn.block(false);\n                        if (!resp || !resp.isSuccess) { toastr.error((resp && resp.message) || ''ثبت انجام نشد'', ''خطا''); return; }\n                        toastr.success(''بررسی / تایید با موفقیت ثبت شد'');\n                        selfConfirm.close();\n                        if (ctx.table && ctx.table.ajax) ctx.table.ajax.reload(null, false);\n                        else if (typeof appController !== ''undefined'' && appController.refreshCurrentPage) appController.refreshCurrentPage();\n                    });\n                    return false;\n                }\n            },\n            cancel: { text: ''انصراف'', btnClass: ''btn-secondary'' }\n        },\n        onContentReady: function () {\n            if (typeof initPersionDatePicker === ''function'') initPersionDatePicker(this.$content);\n        }\n    });\n}"}]';
    DECLARE @BtnNoConfirmTpl NVARCHAR(MAX) = N'[{"id":"srv_pmrequest_attachment","title":"پیوست مدارک","dataActionName":"attachment","colorClass":"btn-color-info","iconClass":"ki-outline ki-folder","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var getter = (typeof page !== ''undefined'' && page.getPartialView) ? page.getPartialView.bind(page) : null;\n    if (!getter) { toastr.error(''امکان باز کردن مودال پیوست وجود ندارد''); return; }\n    var $btn = ctx.$btn ? $(ctx.$btn.element || ctx.$btn).block() : null;\n    getter(''/Panel/Srv/PmRequest/Attachments?id='' + id, function (r) {\n        if ($btn) $btn.block(false);\n        if (!r) { toastr.error(''خطا در دریافت فرم'', ''خطا''); return; }\n        if (r.isSuccess === false) { toastr.error(r.message || ''خطا'', ''خطا''); return; }\n        var decoded = (window.appController && appController.decodePartialResponse) ? appController.decodePartialResponse(r) : { html: (typeof r === ''string'' ? r : ''''), scripts: [] };\n        var $bodyCn = $(''<div style=\"overflow-x:hidden;overflow-y:auto;max-height:75vh;\"></div>'').append(decoded.html || '''');\n        $.confirm({\n            title: ''پیوست مدارک'',\n            columnClass: ''col-md-10'',\n            content: $bodyCn,\n            rtl: true,\n            buttons: { cancel: { text: ''بستن'', btnClass: ''btn-secondary'' } },\n            onContentReady: function () {\n                var scripts = decoded.scripts || [];\n                scripts.forEach(function (code) {\n                    try { new Function(''page'', ''__DD__'', ''self'', code)(page, (typeof __DD__ !== ''undefined'' ? __DD__ : jQuery), self); } catch (e) { console.error(e); }\n                });\n            }\n        });\n    });\n}"}]';
    DECLARE @BtnNone NVARCHAR(MAX) = N'[]';
    DECLARE @BtnFull NVARCHAR(MAX) = REPLACE(@BtnFullTpl, N'__DD__', @DD);
    DECLARE @BtnNoAttach NVARCHAR(MAX) = REPLACE(@BtnNoAttachTpl, N'__DD__', @DD);
    DECLARE @BtnNoConfirm NVARCHAR(MAX) = REPLACE(@BtnNoConfirmTpl, N'__DD__', @DD);

    IF OBJECT_ID('tempdb..#PmProfiles') IS NOT NULL DROP TABLE #PmProfiles;
    CREATE TABLE #PmProfiles (
        Name NVARCHAR(100) NOT NULL PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        CustomQuery NVARCHAR(MAX) NOT NULL,
        ActionOptions NVARCHAR(MAX) NOT NULL,
        CustomButtons NVARCHAR(MAX) NOT NULL,
        RoleCsv NVARCHAR(500) NOT NULL,
        SavedQueryId BIGINT NULL
    );
    INSERT INTO #PmProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv)
    VALUES (N'Srv_PmRequest_All', N'درخواست PM — همه', N'--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    CASE [t1].[StatusId]
        WHEN 1032 THEN N''''در حال بررسی''''
        WHEN 1033 THEN N''''تاییده شده''''
        WHEN 1034 THEN N''''رد شده''''
        WHEN 1047 THEN N''''انجام شد''''
        WHEN 0 THEN N''''0''''
        ELSE CAST([t1].[StatusId] AS nvarchar(10))
    END AS [t1_StatusTitle],
    [t1].[StatusId] AS [t1_StatusId],
    [t1].[TypeId] AS [t1_TypeId],
    COALESCE([t7].[Title], [t1].[CreatedOrganizationUnitTitle]) AS [t7_OrgUnitTitle],
    [t6].[Name] AS [t6_CreatedByName],
    [t1].[RequestText] AS [t1_RequestText],
    [t1].[ApproximateExecutionDate] AS [t1_ApproximateExecutionDate],
    [t1].[LastComment] AS [t1_LastComment],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[PmRequest] AS [t1]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[CreatedById]
LEFT JOIN [Hcm].[Personel] AS [t8] ON [t8].[PartyId] = [t6].[PartyId]
LEFT JOIN [Hrm].[OrgUnit] AS [t7] ON [t7].[Id] = [t1].[OrganizationUnitId]', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', @BtnFull, N'ShowAllMenus');
    INSERT INTO #PmProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv)
    VALUES (N'Srv_PmRequest_Support', N'درخواست PM — پشتیبانی', N'DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
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
        WHEN 1032 THEN N''''در حال بررسی''''
        WHEN 1033 THEN N''''تاییده شده''''
        WHEN 1034 THEN N''''رد شده''''
        WHEN 1047 THEN N''''انجام شد''''
        WHEN 0 THEN N''''0''''
        ELSE CAST([t1].[StatusId] AS nvarchar(10))
    END AS [t1_StatusTitle],
    [t1].[StatusId] AS [t1_StatusId],
    [t1].[TypeId] AS [t1_TypeId],
    COALESCE([t7].[Title], [t1].[CreatedOrganizationUnitTitle]) AS [t7_OrgUnitTitle],
    [t6].[Name] AS [t6_CreatedByName],
    [t1].[RequestText] AS [t1_RequestText],
    [t1].[ApproximateExecutionDate] AS [t1_ApproximateExecutionDate],
    [t1].[LastComment] AS [t1_LastComment],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[PmRequest] AS [t1]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[CreatedById]
LEFT JOIN [Hcm].[Personel] AS [t8] ON [t8].[PartyId] = [t6].[PartyId]
LEFT JOIN [Hrm].[OrgUnit] AS [t7] ON [t7].[Id] = [t1].[OrganizationUnitId]
WHERE [t8].[BranchCode] = @BranchCode OR ([t8].[BranchCode] <> 2 AND @BranchCode = 1)', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', @BtnFull, N'Srv.PmRequest.Support');
    INSERT INTO #PmProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv)
    VALUES (N'Srv_PmRequest_RequesterConfirm', N'درخواست PM — درخواست‌کننده و تایید', N'--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    CASE [t1].[StatusId]
        WHEN 1032 THEN N''''در حال بررسی''''
        WHEN 1033 THEN N''''تاییده شده''''
        WHEN 1034 THEN N''''رد شده''''
        WHEN 1047 THEN N''''انجام شد''''
        WHEN 0 THEN N''''0''''
        ELSE CAST([t1].[StatusId] AS nvarchar(10))
    END AS [t1_StatusTitle],
    [t1].[StatusId] AS [t1_StatusId],
    [t1].[TypeId] AS [t1_TypeId],
    COALESCE([t7].[Title], [t1].[CreatedOrganizationUnitTitle]) AS [t7_OrgUnitTitle],
    [t6].[Name] AS [t6_CreatedByName],
    [t1].[RequestText] AS [t1_RequestText],
    [t1].[ApproximateExecutionDate] AS [t1_ApproximateExecutionDate],
    [t1].[LastComment] AS [t1_LastComment],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[PmRequest] AS [t1]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[CreatedById]
LEFT JOIN [Hcm].[Personel] AS [t8] ON [t8].[PartyId] = [t6].[PartyId]
LEFT JOIN [Hrm].[OrgUnit] AS [t7] ON [t7].[Id] = [t1].[OrganizationUnitId]
WHERE [t1].[CreatedById] = TRY_CAST(@CurrentUserId AS BIGINT)
   OR (
        [t1].[OrganizationUnitId] IS NOT NULL
        AND [t1].[OrganizationUnitId] = (
            SELECT TOP 1 p.OrgUnitId
            FROM [system].[User] u
            INNER JOIN [Hcm].[Personel] p ON p.PartyId = u.PartyId
            WHERE u.Id = TRY_CAST(@CurrentUserId AS BIGINT)
        )
        AND [t8].[BranchCode] = (
            SELECT TOP 1 p.BranchCode
            FROM [system].[User] u
            INNER JOIN [Hcm].[Personel] p ON p.PartyId = u.PartyId
            WHERE u.Id = TRY_CAST(@CurrentUserId AS BIGINT)
        )
   )', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', @BtnFull, N'Srv.PmRequest.RequesterConfirm');
    INSERT INTO #PmProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv)
    VALUES (N'Srv_PmRequest_HavayarControl', N'درخواست PM — هوایار کنترل', N'--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    CASE [t1].[StatusId]
        WHEN 1032 THEN N''''در حال بررسی''''
        WHEN 1033 THEN N''''تاییده شده''''
        WHEN 1034 THEN N''''رد شده''''
        WHEN 1047 THEN N''''انجام شد''''
        WHEN 0 THEN N''''0''''
        ELSE CAST([t1].[StatusId] AS nvarchar(10))
    END AS [t1_StatusTitle],
    [t1].[StatusId] AS [t1_StatusId],
    [t1].[TypeId] AS [t1_TypeId],
    COALESCE([t7].[Title], [t1].[CreatedOrganizationUnitTitle]) AS [t7_OrgUnitTitle],
    [t6].[Name] AS [t6_CreatedByName],
    [t1].[RequestText] AS [t1_RequestText],
    [t1].[ApproximateExecutionDate] AS [t1_ApproximateExecutionDate],
    [t1].[LastComment] AS [t1_LastComment],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[PmRequest] AS [t1]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[CreatedById]
LEFT JOIN [Hcm].[Personel] AS [t8] ON [t8].[PartyId] = [t6].[PartyId]
LEFT JOIN [Hrm].[OrgUnit] AS [t7] ON [t7].[Id] = [t1].[OrganizationUnitId]
WHERE [t1].[CreatedById] = TRY_CAST(@CurrentUserId AS BIGINT)
   OR (
        [t1].[OrganizationUnitId] IS NOT NULL
        AND [t1].[OrganizationUnitId] = (
            SELECT TOP 1 p.OrgUnitId
            FROM [system].[User] u
            INNER JOIN [Hcm].[Personel] p ON p.PartyId = u.PartyId
            WHERE u.Id = TRY_CAST(@CurrentUserId AS BIGINT)
        )
        AND [t8].[BranchCode] = (
            SELECT TOP 1 p.BranchCode
            FROM [system].[User] u
            INNER JOIN [Hcm].[Personel] p ON p.PartyId = u.PartyId
            WHERE u.Id = TRY_CAST(@CurrentUserId AS BIGINT)
        )
   )', N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', @BtnNoAttach, N'Srv.PmRequest.HavayarControl');
    INSERT INTO #PmProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv)
    VALUES (N'Srv_PmRequest_Requester', N'درخواست PM — درخواست‌کننده', N'--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    CASE [t1].[StatusId]
        WHEN 1032 THEN N''''در حال بررسی''''
        WHEN 1033 THEN N''''تاییده شده''''
        WHEN 1034 THEN N''''رد شده''''
        WHEN 1047 THEN N''''انجام شد''''
        WHEN 0 THEN N''''0''''
        ELSE CAST([t1].[StatusId] AS nvarchar(10))
    END AS [t1_StatusTitle],
    [t1].[StatusId] AS [t1_StatusId],
    [t1].[TypeId] AS [t1_TypeId],
    COALESCE([t7].[Title], [t1].[CreatedOrganizationUnitTitle]) AS [t7_OrgUnitTitle],
    [t6].[Name] AS [t6_CreatedByName],
    [t1].[RequestText] AS [t1_RequestText],
    [t1].[ApproximateExecutionDate] AS [t1_ApproximateExecutionDate],
    [t1].[LastComment] AS [t1_LastComment],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[PmRequest] AS [t1]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[CreatedById]
LEFT JOIN [Hcm].[Personel] AS [t8] ON [t8].[PartyId] = [t6].[PartyId]
LEFT JOIN [Hrm].[OrgUnit] AS [t7] ON [t7].[Id] = [t1].[OrganizationUnitId]
WHERE [t1].[CreatedById] = TRY_CAST(@CurrentUserId AS BIGINT)
   OR (
        [t1].[OrganizationUnitId] IS NOT NULL
        AND [t1].[OrganizationUnitId] = (
            SELECT TOP 1 p.OrgUnitId
            FROM [system].[User] u
            INNER JOIN [Hcm].[Personel] p ON p.PartyId = u.PartyId
            WHERE u.Id = TRY_CAST(@CurrentUserId AS BIGINT)
        )
        AND [t8].[BranchCode] = (
            SELECT TOP 1 p.BranchCode
            FROM [system].[User] u
            INNER JOIN [Hcm].[Personel] p ON p.PartyId = u.PartyId
            WHERE u.Id = TRY_CAST(@CurrentUserId AS BIGINT)
        )
   )', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', @BtnNoConfirm, N'Srv.PmRequest.Requester');
    INSERT INTO #PmProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv)
    VALUES (N'Srv_PmRequest_View', N'درخواست PM — مشاهده', N'--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    CASE [t1].[StatusId]
        WHEN 1032 THEN N''''در حال بررسی''''
        WHEN 1033 THEN N''''تاییده شده''''
        WHEN 1034 THEN N''''رد شده''''
        WHEN 1047 THEN N''''انجام شد''''
        WHEN 0 THEN N''''0''''
        ELSE CAST([t1].[StatusId] AS nvarchar(10))
    END AS [t1_StatusTitle],
    [t1].[StatusId] AS [t1_StatusId],
    [t1].[TypeId] AS [t1_TypeId],
    COALESCE([t7].[Title], [t1].[CreatedOrganizationUnitTitle]) AS [t7_OrgUnitTitle],
    [t6].[Name] AS [t6_CreatedByName],
    [t1].[RequestText] AS [t1_RequestText],
    [t1].[ApproximateExecutionDate] AS [t1_ApproximateExecutionDate],
    [t1].[LastComment] AS [t1_LastComment],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[PmRequest] AS [t1]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[CreatedById]
LEFT JOIN [Hcm].[Personel] AS [t8] ON [t8].[PartyId] = [t6].[PartyId]
LEFT JOIN [Hrm].[OrgUnit] AS [t7] ON [t7].[Id] = [t1].[OrganizationUnitId]
WHERE [t1].[CreatedById] = TRY_CAST(@CurrentUserId AS BIGINT)
   OR (
        [t1].[OrganizationUnitId] IS NOT NULL
        AND [t1].[OrganizationUnitId] = (
            SELECT TOP 1 p.OrgUnitId
            FROM [system].[User] u
            INNER JOIN [Hcm].[Personel] p ON p.PartyId = u.PartyId
            WHERE u.Id = TRY_CAST(@CurrentUserId AS BIGINT)
        )
        AND [t8].[BranchCode] = (
            SELECT TOP 1 p.BranchCode
            FROM [system].[User] u
            INNER JOIN [Hcm].[Personel] p ON p.PartyId = u.PartyId
            WHERE u.Id = TRY_CAST(@CurrentUserId AS BIGINT)
        )
   )', N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', @BtnNone, N'Srv.PmRequest.View');

    DECLARE @MissingRoles NVARCHAR(MAX) = NULL;
    ;WITH NeedRoles AS (
        SELECT DISTINCT LTRIM(RTRIM(value)) AS RoleName
        FROM #PmProfiles CROSS APPLY STRING_SPLIT(RoleCsv, N',')
    )
    SELECT @MissingRoles = STRING_AGG(nr.RoleName, N', ')
    FROM NeedRoles nr
    WHERE NOT EXISTS (SELECT 1 FROM system.Role r WHERE r.Name = nr.RoleName);
    IF @MissingRoles IS NOT NULL AND LEN(@MissingRoles) > 0
    BEGIN
        SET @MissingRoles = N'نقش‌های درخواست PM یافت نشد. ابتدا Seed_SrvPmRequest_RolesAndAccess.sql را اجرا کنید. نقش‌های مفقود: ' + @MissingRoles;
        THROW 51730, @MissingRoles, 1;
    END

    DECLARE @Name NVARCHAR(100), @Title NVARCHAR(200), @Sel NVARCHAR(MAX);
    DECLARE @Act NVARCHAR(MAX), @Btn NVARCHAR(MAX), @RoleCsv NVARCHAR(500);
    DECLARE @Qj NVARCHAR(MAX), @Pid BIGINT;

    DECLARE pc CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv FROM #PmProfiles;
    OPEN pc;
    FETCH NEXT FROM pc INTO @Name, @Title, @Sel, @Act, @Btn, @RoleCsv;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Qj = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @Sel);
        IF ISJSON(@Cols) <> 1 OR ISJSON(@Qj) <> 1 OR ISJSON(@Act) <> 1 OR ISJSON(@Btn) <> 1 OR ISJSON(@Ev) <> 1
            THROW 51731, N'JSON نمایه درخواست PM معتبر نیست.', 1;

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

        UPDATE #PmProfiles SET SavedQueryId = @Pid WHERE Name = @Name;

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
    PRINT N'=== DONE Seed_SrvPmRequest_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name LIKE N'Srv_PmRequest_%' ORDER BY Name;
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Srv_PmRequest_All'
ORDER BY CAST(j.[key] AS int);
