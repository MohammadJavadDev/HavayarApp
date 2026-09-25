/*
Seed_SrvCarRequest_DataProfiles.sql
نمایه داده درخواست خودرو (HTS صفحه 398 / Srv_CarRequest). UTF-8 BOM. Idempotent.
شش نمایه با ستون‌های یکسان؛ فیلتر نقش در CustomQuery.
sqlcmd -S <server> -d HavayarApp -C -b -I -x -f 65001 -i "Data\Scripts\Seed_SrvCarRequest_DataProfiles.sql"
(-x disables sqlcmd $ variable substitution; $$.post is also rebuilt via CHAR(36).)
Requires roles from Seed_SrvCarRequest_RolesAndAccess.sql.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-srv-carrequest-dataprofile';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';
    DECLARE @Ent NVARCHAR(200) = N'entities.app.srv.carrequest';
    DECLARE @Pascal NVARCHAR(200) = N'Entities.App.Srv.CarRequest';
    DECLARE @Cols NVARCHAR(MAX) = N'[{"TableName":"Srv.CarRequest","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":70,"ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SystemType":6},{"TableName":"Srv.CarRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":110,"ColumnName":"t1_StatusTitle","DisplayName":"وضعیت","Alliance":"t1_StatusTitle","Address":"CASE [t1].[StatusId] WHEN 1032 THEN N''در حال بررسی'' WHEN 1033 THEN N''تایید شده'' WHEN 1034 THEN N''رد شده'' WHEN 1047 THEN N''انجام شد'' WHEN 0 THEN N''0'' ELSE CAST([t1].[StatusId] AS nvarchar(10)) END","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.CarRequest","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":70,"ColumnName":"t1_StatusId","DisplayName":"کد وضعیت","Alliance":"t1_StatusId","Address":"[t1].[StatusId]","SystemTypeName":"Int","SystemType":7},{"TableName":"Srv.CarRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":120,"ColumnName":"t1_BranchTitle","DisplayName":"محل خدمت / شعبه","Alliance":"t1_BranchTitle","Address":"CASE [t1].[BranchId] WHEN 43 THEN N''دفتر مرکزی'' WHEN 44 THEN N''کارخانه'' ELSE CAST([t1].[BranchId] AS nvarchar(10)) END","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.CarRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Srv.Enums.CarRequestTypeEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":160,"ColumnName":"t1_TypeId","DisplayName":"نوع","Alliance":"t1_TypeId","Address":"[t1].[TypeId]","SystemTypeName":"Select","SystemType":8},{"TableName":"Srv.CarRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":100,"ColumnName":"t1_NeedDateInText","DisplayName":"تاریخ نیاز","Alliance":"t1_NeedDateInText","Address":"[t1].[NeedDateInText]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.CarRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":80,"ColumnName":"t1_NeedTime","DisplayName":"ساعت نیاز","Alliance":"t1_NeedTime","Address":"[t1].[NeedTime]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.CarRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":90,"ColumnName":"t1_WaitingTime","DisplayName":"مدت انتظار","Alliance":"t1_WaitingTime","Address":"[t1].[WaitingTime]","SystemTypeName":"Decimal","SystemType":14},{"TableName":"Gnr.CostCenter","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":140,"ColumnName":"t3_CostCenterTitle","DisplayName":"مرکز هزینه","Alliance":"t3_CostCenterTitle","Address":"[t3].[Title]","SystemTypeName":"String","SystemType":0},{"TableName":"Hcm.Personel","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":140,"ColumnName":"t2_PassengerName","DisplayName":"مسافر","Alliance":"t2_PassengerName","Address":"LTRIM(RTRIM(ISNULL([t2].[Name], N'''') + N'' '' + ISNULL([t2].[FamilyDisplay], ISNULL([t2].[Family], N''''))))","SystemTypeName":"String","SystemType":0},{"TableName":"Hrm.OrgUnit","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":140,"ColumnName":"t4_OrgUnitTitle","DisplayName":"واحد سازمانی","Alliance":"t4_OrgUnitTitle","Address":"[t4].[Title]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.CarRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":90,"ColumnName":"t1_CountPeople","DisplayName":"تعداد نفرات","Alliance":"t1_CountPeople","Address":"[t1].[CountPeople]","SystemTypeName":"Int","SystemType":7},{"TableName":"Hcm.Personel","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":140,"ColumnName":"t5_DriverName","DisplayName":"راننده داخلی","Alliance":"t5_DriverName","Address":"LTRIM(RTRIM(ISNULL([t5].[Name], N'''') + N'' '' + ISNULL([t5].[FamilyDisplay], ISNULL([t5].[Family], N''''))))","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.CarRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Srv.Enums.OutdoorCarServiceEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":120,"ColumnName":"t1_OutdoorCarServiceId","DisplayName":"سرویس بیرونی","Alliance":"t1_OutdoorCarServiceId","Address":"[t1].[OutdoorCarServiceId]","SystemTypeName":"Select","SystemType":8},{"TableName":"Srv.CarRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":160,"ColumnName":"t1_Destination","DisplayName":"مقصد","Alliance":"t1_Destination","Address":"[t1].[Destination]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.CarRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":180,"ColumnName":"t1_LastComment","DisplayName":"آخرین توضیح","Alliance":"t1_LastComment","Address":"[t1].[LastComment]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.CarRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":120,"ColumnName":"t1_DoneDateTimeInText","DisplayName":"زمان انجام","Alliance":"t1_DoneDateTimeInText","Address":"[t1].[DoneDateTimeInText]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.CarRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":180,"ColumnName":"t1_Description","DisplayName":"توضیحات","Alliance":"t1_Description","Address":"[t1].[Description]","SystemTypeName":"String","SystemType":0},{"TableName":"system.User","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":130,"ColumnName":"t6_CreatedByName","DisplayName":"ایجادکننده","Alliance":"t6_CreatedByName","Address":"[t6].[Name]","SystemTypeName":"String","SystemType":0},{"TableName":"Srv.CarRequest","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Filterable":true,"Sortable":true,"ClassName":"","Width":120,"ColumnName":"t1_CreatedDateInText","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedDateInText","Address":"[t1].[CreatedDateInText]","SystemTypeName":"String","SystemType":0}]';
    -- sqlcmd treats $$ as escape; build $$ via CHAR so $$.post / $$.get survive apply
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
    var $panel = __DD__(''#srvCarRequestComments'');
    if (!$panel || !$panel.length) $panel = $(''#srvCarRequestComments'');
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
    __DD__.get(''/Panel/Srv/CarRequest/comments?carRequestId='' + id, function (r) {
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
    DECLARE @BtnFullTpl NVARCHAR(MAX) = N'[{"id":"srv_carrequest_attachment","title":"پیوست مدارک","dataActionName":"attachment","colorClass":"btn-color-info","iconClass":"ki-outline ki-folder","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var getter = (typeof page !== ''undefined'' && page.getPartialView) ? page.getPartialView.bind(page) : null;\n    if (!getter) { toastr.error(''امکان باز کردن مودال پیوست وجود ندارد''); return; }\n    var $btn = ctx.$btn ? $(ctx.$btn.element || ctx.$btn).block() : null;\n    getter(''/Panel/Srv/CarRequest/Attachments?id='' + id, function (r) {\n        if ($btn) $btn.block(false);\n        if (!r) { toastr.error(''خطا در دریافت فرم'', ''خطا''); return; }\n        if (r.isSuccess === false) { toastr.error(r.message || ''خطا'', ''خطا''); return; }\n        var decoded = (window.appController && appController.decodePartialResponse) ? appController.decodePartialResponse(r) : { html: (typeof r === ''string'' ? r : ''''), scripts: [] };\n        var $bodyCn = $(''<div style=\"overflow-x:hidden;overflow-y:auto;max-height:75vh;\"></div>'').append(decoded.html || '''');\n        $.confirm({\n            title: ''پیوست مدارک'',\n            columnClass: ''col-md-10'',\n            content: $bodyCn,\n            rtl: true,\n            buttons: { cancel: { text: ''بستن'', btnClass: ''btn-secondary'' } },\n            onContentReady: function () {\n                var scripts = decoded.scripts || [];\n                scripts.forEach(function (code) {\n                    try { new Function(''page'', ''$$'', ''self'', code)(page, (typeof $$ !== ''undefined'' ? $$ : jQuery), self); } catch (e) { console.error(e); }\n                });\n            }\n        });\n    });\n}"},{"id":"srv_carrequest_confirm","title":"بررسی و تایید","dataActionName":"confirm","colorClass":"btn-color-success","iconClass":"ki-outline ki-check","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var $form = $(''<div class=\"row g-3\"></div>'');\n    $form.append(''<div class=\"col-md-12 form-group-inline\"><label class=\"form-label fw-bold\">راننده داخلی</label><select class=\"form-control\" data-bind=\"driverId\" id=\"srvCrDriver\"><option value=\"\">—</option></select></div>'');\n    $form.append(''<div class=\"col-md-12 form-group-inline\"><label class=\"form-label fw-bold\">سرویس بیرونی</label><select class=\"form-control\" data-bind=\"outdoorCarServiceId\" id=\"srvCrOutdoor\"><option value=\"\">—</option><option value=\"1042\">سرویس اینترنتی</option><option value=\"1043\">سرویس محلی</option></select></div>'');\n    $form.append(''<div class=\"col-md-12 form-group-inline\"><label class=\"form-label fw-bold required\">وضعیت</label><select class=\"form-control\" data-bind=\"statusId\" data-required=\"true\" id=\"srvCrStatus\"><option value=\"\">انتخاب کنید</option><option value=\"1032\">در حال بررسی</option><option value=\"1033\">تایید شده</option><option value=\"1034\">رد شده</option><option value=\"1047\">انجام شد</option></select><div data-invalidmessagespan class=\"my-1 mx-1\"><span class=\"text-danger\"></span></div></div>'');\n    $form.append(''<div class=\"col-md-12 form-group-inline\"><label class=\"form-label fw-bold required\">توضیحات</label><textarea class=\"form-control\" data-bind=\"comment\" data-required=\"true\" rows=\"3\"></textarea><div data-invalidmessagespan class=\"my-1 mx-1\"><span class=\"text-danger\"></span></div></div>'');\n    function syncExclusive($root) {\n        var d = $root.find(''#srvCrDriver'').val();\n        var o = $root.find(''#srvCrOutdoor'').val();\n        if (d) { $root.find(''#srvCrOutdoor'').val('''').prop(''disabled'', true); $root.find(''#srvCrDriver'').prop(''disabled'', false); }\n        else if (o) { $root.find(''#srvCrDriver'').val('''').prop(''disabled'', true); $root.find(''#srvCrOutdoor'').prop(''disabled'', false); }\n        else { $root.find(''#srvCrDriver'').prop(''disabled'', false); $root.find(''#srvCrOutdoor'').prop(''disabled'', false); }\n    }\n    __DD__.get(''/Panel/Srv/CarRequest/drivers'', function (r) {\n        var items = (r && r.data) ? r.data : (Array.isArray(r) ? r : []);\n        var $drv = $form.find(''#srvCrDriver'');\n        items.forEach(function (it) { $drv.append($(''<option></option>'').val(it.id).text(it.title || it.Title || '''')); });\n        $.confirm({\n            title: ''بررسی و تایید'',\n            columnClass: ''col-md-8'',\n            content: $form,\n            rtl: true,\n            buttons: {\n                save: {\n                    text: ''ثبت'',\n                    btnClass: ''btn-primary'',\n                    action: function () {\n                        var $content = this.$content;\n                        if (validateError($content)) return false;\n                        var comment = ($content.find(''[data-bind=\"comment\"]'').val() || '''').trim();\n                        var statusId = $content.find(''#srvCrStatus'').val();\n                        if (!statusId) { toastr.error(''وضعیت اجباری است'', ''خطا''); return false; }\n                        if (!comment) { toastr.error(''توضیحات اجباری است'', ''خطا''); return false; }\n                        var driverId = $content.find(''#srvCrDriver'').val() || null;\n                        var outdoor = $content.find(''#srvCrOutdoor'').val() || null;\n                        if (driverId && outdoor) { toastr.error(''راننده داخلی و سرویس بیرونی همزمان قابل انتخاب نیستند'', ''خطا''); return false; }\n                        var selfConfirm = this;\n                        var $saveBtn = $(this.buttons.save.el).block();\n                        __DD__.post(''/Panel/Srv/CarRequest/confirm'', {\n                            id: id,\n                            driverId: driverId ? parseInt(driverId, 10) : null,\n                            outdoorCarServiceId: outdoor ? parseInt(outdoor, 10) : null,\n                            statusId: parseInt(statusId, 10),\n                            comment: comment\n                        }, function (resp) {\n                            $saveBtn.block(false);\n                            if (!resp || !resp.isSuccess) { toastr.error((resp && resp.message) || ''ثبت انجام نشد'', ''خطا''); return; }\n                            toastr.success(''بررسی و تایید با موفقیت ثبت شد'');\n                            selfConfirm.close();\n                            if (ctx.table && ctx.table.ajax) ctx.table.ajax.reload(null, false);\n                            else if (typeof appController !== ''undefined'' && appController.refreshCurrentPage) appController.refreshCurrentPage();\n                        });\n                        return false;\n                    }\n                },\n                cancel: { text: ''انصراف'', btnClass: ''btn-secondary'' }\n            },\n            onContentReady: function () {\n                var $c = this.$content;\n                $c.on(''change'', ''#srvCrDriver, #srvCrOutdoor'', function () { syncExclusive($c); });\n            }\n        });\n    });\n}"},{"id":"srv_carrequest_print","title":"چاپ","dataActionName":"print","colorClass":"btn-color-primary","iconClass":"ki-outline ki-printer","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/System/ReportBuilder/ViewReportByName/SrvCarRequest?carRequestId={\\''values\\'':[\\'''' + id + ''\\''],\\''condition\\'':\\''=\\''}&autoRun=True'');\n}"}]';
    DECLARE @BtnNoAttachTpl NVARCHAR(MAX) = N'[{"id":"srv_carrequest_confirm","title":"بررسی و تایید","dataActionName":"confirm","colorClass":"btn-color-success","iconClass":"ki-outline ki-check","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var $form = $(''<div class=\"row g-3\"></div>'');\n    $form.append(''<div class=\"col-md-12 form-group-inline\"><label class=\"form-label fw-bold\">راننده داخلی</label><select class=\"form-control\" data-bind=\"driverId\" id=\"srvCrDriver\"><option value=\"\">—</option></select></div>'');\n    $form.append(''<div class=\"col-md-12 form-group-inline\"><label class=\"form-label fw-bold\">سرویس بیرونی</label><select class=\"form-control\" data-bind=\"outdoorCarServiceId\" id=\"srvCrOutdoor\"><option value=\"\">—</option><option value=\"1042\">سرویس اینترنتی</option><option value=\"1043\">سرویس محلی</option></select></div>'');\n    $form.append(''<div class=\"col-md-12 form-group-inline\"><label class=\"form-label fw-bold required\">وضعیت</label><select class=\"form-control\" data-bind=\"statusId\" data-required=\"true\" id=\"srvCrStatus\"><option value=\"\">انتخاب کنید</option><option value=\"1032\">در حال بررسی</option><option value=\"1033\">تایید شده</option><option value=\"1034\">رد شده</option><option value=\"1047\">انجام شد</option></select><div data-invalidmessagespan class=\"my-1 mx-1\"><span class=\"text-danger\"></span></div></div>'');\n    $form.append(''<div class=\"col-md-12 form-group-inline\"><label class=\"form-label fw-bold required\">توضیحات</label><textarea class=\"form-control\" data-bind=\"comment\" data-required=\"true\" rows=\"3\"></textarea><div data-invalidmessagespan class=\"my-1 mx-1\"><span class=\"text-danger\"></span></div></div>'');\n    function syncExclusive($root) {\n        var d = $root.find(''#srvCrDriver'').val();\n        var o = $root.find(''#srvCrOutdoor'').val();\n        if (d) { $root.find(''#srvCrOutdoor'').val('''').prop(''disabled'', true); $root.find(''#srvCrDriver'').prop(''disabled'', false); }\n        else if (o) { $root.find(''#srvCrDriver'').val('''').prop(''disabled'', true); $root.find(''#srvCrOutdoor'').prop(''disabled'', false); }\n        else { $root.find(''#srvCrDriver'').prop(''disabled'', false); $root.find(''#srvCrOutdoor'').prop(''disabled'', false); }\n    }\n    __DD__.get(''/Panel/Srv/CarRequest/drivers'', function (r) {\n        var items = (r && r.data) ? r.data : (Array.isArray(r) ? r : []);\n        var $drv = $form.find(''#srvCrDriver'');\n        items.forEach(function (it) { $drv.append($(''<option></option>'').val(it.id).text(it.title || it.Title || '''')); });\n        $.confirm({\n            title: ''بررسی و تایید'',\n            columnClass: ''col-md-8'',\n            content: $form,\n            rtl: true,\n            buttons: {\n                save: {\n                    text: ''ثبت'',\n                    btnClass: ''btn-primary'',\n                    action: function () {\n                        var $content = this.$content;\n                        if (validateError($content)) return false;\n                        var comment = ($content.find(''[data-bind=\"comment\"]'').val() || '''').trim();\n                        var statusId = $content.find(''#srvCrStatus'').val();\n                        if (!statusId) { toastr.error(''وضعیت اجباری است'', ''خطا''); return false; }\n                        if (!comment) { toastr.error(''توضیحات اجباری است'', ''خطا''); return false; }\n                        var driverId = $content.find(''#srvCrDriver'').val() || null;\n                        var outdoor = $content.find(''#srvCrOutdoor'').val() || null;\n                        if (driverId && outdoor) { toastr.error(''راننده داخلی و سرویس بیرونی همزمان قابل انتخاب نیستند'', ''خطا''); return false; }\n                        var selfConfirm = this;\n                        var $saveBtn = $(this.buttons.save.el).block();\n                        __DD__.post(''/Panel/Srv/CarRequest/confirm'', {\n                            id: id,\n                            driverId: driverId ? parseInt(driverId, 10) : null,\n                            outdoorCarServiceId: outdoor ? parseInt(outdoor, 10) : null,\n                            statusId: parseInt(statusId, 10),\n                            comment: comment\n                        }, function (resp) {\n                            $saveBtn.block(false);\n                            if (!resp || !resp.isSuccess) { toastr.error((resp && resp.message) || ''ثبت انجام نشد'', ''خطا''); return; }\n                            toastr.success(''بررسی و تایید با موفقیت ثبت شد'');\n                            selfConfirm.close();\n                            if (ctx.table && ctx.table.ajax) ctx.table.ajax.reload(null, false);\n                            else if (typeof appController !== ''undefined'' && appController.refreshCurrentPage) appController.refreshCurrentPage();\n                        });\n                        return false;\n                    }\n                },\n                cancel: { text: ''انصراف'', btnClass: ''btn-secondary'' }\n            },\n            onContentReady: function () {\n                var $c = this.$content;\n                $c.on(''change'', ''#srvCrDriver, #srvCrOutdoor'', function () { syncExclusive($c); });\n            }\n        });\n    });\n}"},{"id":"srv_carrequest_print","title":"چاپ","dataActionName":"print","colorClass":"btn-color-primary","iconClass":"ki-outline ki-printer","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/System/ReportBuilder/ViewReportByName/SrvCarRequest?carRequestId={\\''values\\'':[\\'''' + id + ''\\''],\\''condition\\'':\\''=\\''}&autoRun=True'');\n}"}]';
    DECLARE @BtnNoConfirmTpl NVARCHAR(MAX) = N'[{"id":"srv_carrequest_attachment","title":"پیوست مدارک","dataActionName":"attachment","colorClass":"btn-color-info","iconClass":"ki-outline ki-folder","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    var getter = (typeof page !== ''undefined'' && page.getPartialView) ? page.getPartialView.bind(page) : null;\n    if (!getter) { toastr.error(''امکان باز کردن مودال پیوست وجود ندارد''); return; }\n    var $btn = ctx.$btn ? $(ctx.$btn.element || ctx.$btn).block() : null;\n    getter(''/Panel/Srv/CarRequest/Attachments?id='' + id, function (r) {\n        if ($btn) $btn.block(false);\n        if (!r) { toastr.error(''خطا در دریافت فرم'', ''خطا''); return; }\n        if (r.isSuccess === false) { toastr.error(r.message || ''خطا'', ''خطا''); return; }\n        var decoded = (window.appController && appController.decodePartialResponse) ? appController.decodePartialResponse(r) : { html: (typeof r === ''string'' ? r : ''''), scripts: [] };\n        var $bodyCn = $(''<div style=\"overflow-x:hidden;overflow-y:auto;max-height:75vh;\"></div>'').append(decoded.html || '''');\n        $.confirm({\n            title: ''پیوست مدارک'',\n            columnClass: ''col-md-10'',\n            content: $bodyCn,\n            rtl: true,\n            buttons: { cancel: { text: ''بستن'', btnClass: ''btn-secondary'' } },\n            onContentReady: function () {\n                var scripts = decoded.scripts || [];\n                scripts.forEach(function (code) {\n                    try { new Function(''page'', ''$$'', ''self'', code)(page, (typeof $$ !== ''undefined'' ? $$ : jQuery), self); } catch (e) { console.error(e); }\n                });\n            }\n        });\n    });\n}"},{"id":"srv_carrequest_print","title":"چاپ","dataActionName":"print","colorClass":"btn-color-primary","iconClass":"ki-outline ki-printer","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/System/ReportBuilder/ViewReportByName/SrvCarRequest?carRequestId={\\''values\\'':[\\'''' + id + ''\\''],\\''condition\\'':\\''=\\''}&autoRun=True'');\n}"}]';
    DECLARE @BtnPrintTpl NVARCHAR(MAX) = N'[{"id":"srv_carrequest_print","title":"چاپ","dataActionName":"print","colorClass":"btn-color-primary","iconClass":"ki-outline ki-printer","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/System/ReportBuilder/ViewReportByName/SrvCarRequest?carRequestId={\\''values\\'':[\\'''' + id + ''\\''],\\''condition\\'':\\''=\\''}&autoRun=True'');\n}"}]';
    DECLARE @BtnFull NVARCHAR(MAX) = REPLACE(@BtnFullTpl, N'__DD__', @DD);
    DECLARE @BtnNoAttach NVARCHAR(MAX) = REPLACE(@BtnNoAttachTpl, N'__DD__', @DD);
    DECLARE @BtnNoConfirm NVARCHAR(MAX) = REPLACE(@BtnNoConfirmTpl, N'__DD__', @DD);
    DECLARE @BtnPrint NVARCHAR(MAX) = REPLACE(@BtnPrintTpl, N'__DD__', @DD);

    IF OBJECT_ID('tempdb..#CrProfiles') IS NOT NULL DROP TABLE #CrProfiles;
    CREATE TABLE #CrProfiles (
        Name NVARCHAR(100) NOT NULL PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        CustomQuery NVARCHAR(MAX) NOT NULL,
        ActionOptions NVARCHAR(MAX) NOT NULL,
        CustomButtons NVARCHAR(MAX) NOT NULL,
        RoleCsv NVARCHAR(500) NOT NULL,
        SavedQueryId BIGINT NULL
    );

    INSERT INTO #CrProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv)
    VALUES (N'Srv_CarRequest_All', N'درخواست خودرو — همه', N'--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    CASE [t1].[StatusId]
        WHEN 1032 THEN N''در حال بررسی''
        WHEN 1033 THEN N''تایید شده''
        WHEN 1034 THEN N''رد شده''
        WHEN 1047 THEN N''انجام شد''
        WHEN 0 THEN N''0''
        ELSE CAST([t1].[StatusId] AS nvarchar(10))
    END AS [t1_StatusTitle],
    [t1].[StatusId] AS [t1_StatusId],
    CASE [t1].[BranchId]
        WHEN 43 THEN N''دفتر مرکزی''
        WHEN 44 THEN N''کارخانه''
        ELSE CAST([t1].[BranchId] AS nvarchar(10))
    END AS [t1_BranchTitle],
    [t1].[TypeId] AS [t1_TypeId],
    [t1].[NeedDateInText] AS [t1_NeedDateInText],
    [t1].[NeedTime] AS [t1_NeedTime],
    [t1].[WaitingTime] AS [t1_WaitingTime],
    [t3].[Title] AS [t3_CostCenterTitle],
    LTRIM(RTRIM(ISNULL([t2].[Name], N'''') + N'' '' + ISNULL([t2].[FamilyDisplay], ISNULL([t2].[Family], N'''')))) AS [t2_PassengerName],
    [t4].[Title] AS [t4_OrgUnitTitle],
    [t1].[CountPeople] AS [t1_CountPeople],
    LTRIM(RTRIM(ISNULL([t5].[Name], N'''') + N'' '' + ISNULL([t5].[FamilyDisplay], ISNULL([t5].[Family], N'''')))) AS [t5_DriverName],
    [t1].[OutdoorCarServiceId] AS [t1_OutdoorCarServiceId],
    [t1].[Destination] AS [t1_Destination],
    [t1].[LastComment] AS [t1_LastComment],
    [t1].[DoneDateTimeInText] AS [t1_DoneDateTimeInText],
    [t1].[Description] AS [t1_Description],
    [t6].[Name] AS [t6_CreatedByName],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[CarRequest] AS [t1]
LEFT JOIN [Hcm].[Personel] AS [t2] ON [t2].[Id] = [t1].[PassengerId]
LEFT JOIN [Gnr].[CostCenter] AS [t3] ON [t3].[Id] = [t1].[CostCenterId]
LEFT JOIN [Hrm].[OrgUnit] AS [t4] ON [t4].[Id] = [t2].[OrgUnitId]
LEFT JOIN [Hcm].[Personel] AS [t5] ON [t5].[Id] = [t1].[DriverId]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[CreatedById]', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', @BtnFull, N'ShowAllMenus');
    INSERT INTO #CrProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv)
    VALUES (N'Srv_CarRequest_Support', N'درخواست خودرو — پشتیبانی', N'DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
DECLARE @BranchCode INT = (
    SELECT TOP 1 p.BranchCode
    FROM [system].[User] u
    INNER JOIN [Hcm].[Personel] p ON p.PartyId = u.PartyId
    WHERE u.Id = @CuId
);
DECLARE @BranchId INT = CASE WHEN @BranchCode = 1 THEN 43 ELSE 44 END;
--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    CASE [t1].[StatusId]
        WHEN 1032 THEN N''در حال بررسی''
        WHEN 1033 THEN N''تایید شده''
        WHEN 1034 THEN N''رد شده''
        WHEN 1047 THEN N''انجام شد''
        WHEN 0 THEN N''0''
        ELSE CAST([t1].[StatusId] AS nvarchar(10))
    END AS [t1_StatusTitle],
    [t1].[StatusId] AS [t1_StatusId],
    CASE [t1].[BranchId]
        WHEN 43 THEN N''دفتر مرکزی''
        WHEN 44 THEN N''کارخانه''
        ELSE CAST([t1].[BranchId] AS nvarchar(10))
    END AS [t1_BranchTitle],
    [t1].[TypeId] AS [t1_TypeId],
    [t1].[NeedDateInText] AS [t1_NeedDateInText],
    [t1].[NeedTime] AS [t1_NeedTime],
    [t1].[WaitingTime] AS [t1_WaitingTime],
    [t3].[Title] AS [t3_CostCenterTitle],
    LTRIM(RTRIM(ISNULL([t2].[Name], N'''') + N'' '' + ISNULL([t2].[FamilyDisplay], ISNULL([t2].[Family], N'''')))) AS [t2_PassengerName],
    [t4].[Title] AS [t4_OrgUnitTitle],
    [t1].[CountPeople] AS [t1_CountPeople],
    LTRIM(RTRIM(ISNULL([t5].[Name], N'''') + N'' '' + ISNULL([t5].[FamilyDisplay], ISNULL([t5].[Family], N'''')))) AS [t5_DriverName],
    [t1].[OutdoorCarServiceId] AS [t1_OutdoorCarServiceId],
    [t1].[Destination] AS [t1_Destination],
    [t1].[LastComment] AS [t1_LastComment],
    [t1].[DoneDateTimeInText] AS [t1_DoneDateTimeInText],
    [t1].[Description] AS [t1_Description],
    [t6].[Name] AS [t6_CreatedByName],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[CarRequest] AS [t1]
LEFT JOIN [Hcm].[Personel] AS [t2] ON [t2].[Id] = [t1].[PassengerId]
LEFT JOIN [Gnr].[CostCenter] AS [t3] ON [t3].[Id] = [t1].[CostCenterId]
LEFT JOIN [Hrm].[OrgUnit] AS [t4] ON [t4].[Id] = [t2].[OrgUnitId]
LEFT JOIN [Hcm].[Personel] AS [t5] ON [t5].[Id] = [t1].[DriverId]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[CreatedById]
WHERE [t1].[BranchId] = @BranchId', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', @BtnFull, N'Srv.CarRequest.Support');
    INSERT INTO #CrProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv)
    VALUES (N'Srv_CarRequest_RequesterConfirm', N'درخواست خودرو — درخواست‌کننده و تایید', N'--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    CASE [t1].[StatusId]
        WHEN 1032 THEN N''در حال بررسی''
        WHEN 1033 THEN N''تایید شده''
        WHEN 1034 THEN N''رد شده''
        WHEN 1047 THEN N''انجام شد''
        WHEN 0 THEN N''0''
        ELSE CAST([t1].[StatusId] AS nvarchar(10))
    END AS [t1_StatusTitle],
    [t1].[StatusId] AS [t1_StatusId],
    CASE [t1].[BranchId]
        WHEN 43 THEN N''دفتر مرکزی''
        WHEN 44 THEN N''کارخانه''
        ELSE CAST([t1].[BranchId] AS nvarchar(10))
    END AS [t1_BranchTitle],
    [t1].[TypeId] AS [t1_TypeId],
    [t1].[NeedDateInText] AS [t1_NeedDateInText],
    [t1].[NeedTime] AS [t1_NeedTime],
    [t1].[WaitingTime] AS [t1_WaitingTime],
    [t3].[Title] AS [t3_CostCenterTitle],
    LTRIM(RTRIM(ISNULL([t2].[Name], N'''') + N'' '' + ISNULL([t2].[FamilyDisplay], ISNULL([t2].[Family], N'''')))) AS [t2_PassengerName],
    [t4].[Title] AS [t4_OrgUnitTitle],
    [t1].[CountPeople] AS [t1_CountPeople],
    LTRIM(RTRIM(ISNULL([t5].[Name], N'''') + N'' '' + ISNULL([t5].[FamilyDisplay], ISNULL([t5].[Family], N'''')))) AS [t5_DriverName],
    [t1].[OutdoorCarServiceId] AS [t1_OutdoorCarServiceId],
    [t1].[Destination] AS [t1_Destination],
    [t1].[LastComment] AS [t1_LastComment],
    [t1].[DoneDateTimeInText] AS [t1_DoneDateTimeInText],
    [t1].[Description] AS [t1_Description],
    [t6].[Name] AS [t6_CreatedByName],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[CarRequest] AS [t1]
LEFT JOIN [Hcm].[Personel] AS [t2] ON [t2].[Id] = [t1].[PassengerId]
LEFT JOIN [Gnr].[CostCenter] AS [t3] ON [t3].[Id] = [t1].[CostCenterId]
LEFT JOIN [Hrm].[OrgUnit] AS [t4] ON [t4].[Id] = [t2].[OrgUnitId]
LEFT JOIN [Hcm].[Personel] AS [t5] ON [t5].[Id] = [t1].[DriverId]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[CreatedById]
WHERE [t1].[CreatedById] = TRY_CAST(@CurrentUserId AS BIGINT)', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', @BtnFull, N'Srv.CarRequest.RequesterConfirm');
    INSERT INTO #CrProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv)
    VALUES (N'Srv_CarRequest_HavayarControl', N'درخواست خودرو — هوایار کنترل', N'--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    CASE [t1].[StatusId]
        WHEN 1032 THEN N''در حال بررسی''
        WHEN 1033 THEN N''تایید شده''
        WHEN 1034 THEN N''رد شده''
        WHEN 1047 THEN N''انجام شد''
        WHEN 0 THEN N''0''
        ELSE CAST([t1].[StatusId] AS nvarchar(10))
    END AS [t1_StatusTitle],
    [t1].[StatusId] AS [t1_StatusId],
    CASE [t1].[BranchId]
        WHEN 43 THEN N''دفتر مرکزی''
        WHEN 44 THEN N''کارخانه''
        ELSE CAST([t1].[BranchId] AS nvarchar(10))
    END AS [t1_BranchTitle],
    [t1].[TypeId] AS [t1_TypeId],
    [t1].[NeedDateInText] AS [t1_NeedDateInText],
    [t1].[NeedTime] AS [t1_NeedTime],
    [t1].[WaitingTime] AS [t1_WaitingTime],
    [t3].[Title] AS [t3_CostCenterTitle],
    LTRIM(RTRIM(ISNULL([t2].[Name], N'''') + N'' '' + ISNULL([t2].[FamilyDisplay], ISNULL([t2].[Family], N'''')))) AS [t2_PassengerName],
    [t4].[Title] AS [t4_OrgUnitTitle],
    [t1].[CountPeople] AS [t1_CountPeople],
    LTRIM(RTRIM(ISNULL([t5].[Name], N'''') + N'' '' + ISNULL([t5].[FamilyDisplay], ISNULL([t5].[Family], N'''')))) AS [t5_DriverName],
    [t1].[OutdoorCarServiceId] AS [t1_OutdoorCarServiceId],
    [t1].[Destination] AS [t1_Destination],
    [t1].[LastComment] AS [t1_LastComment],
    [t1].[DoneDateTimeInText] AS [t1_DoneDateTimeInText],
    [t1].[Description] AS [t1_Description],
    [t6].[Name] AS [t6_CreatedByName],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[CarRequest] AS [t1]
LEFT JOIN [Hcm].[Personel] AS [t2] ON [t2].[Id] = [t1].[PassengerId]
LEFT JOIN [Gnr].[CostCenter] AS [t3] ON [t3].[Id] = [t1].[CostCenterId]
LEFT JOIN [Hrm].[OrgUnit] AS [t4] ON [t4].[Id] = [t2].[OrgUnitId]
LEFT JOIN [Hcm].[Personel] AS [t5] ON [t5].[Id] = [t1].[DriverId]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[CreatedById]
WHERE [t1].[CreatedById] = TRY_CAST(@CurrentUserId AS BIGINT)', N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', @BtnNoAttach, N'Srv.CarRequest.HavayarControl');
    INSERT INTO #CrProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv)
    VALUES (N'Srv_CarRequest_Requester', N'درخواست خودرو — درخواست‌کننده', N'--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    CASE [t1].[StatusId]
        WHEN 1032 THEN N''در حال بررسی''
        WHEN 1033 THEN N''تایید شده''
        WHEN 1034 THEN N''رد شده''
        WHEN 1047 THEN N''انجام شد''
        WHEN 0 THEN N''0''
        ELSE CAST([t1].[StatusId] AS nvarchar(10))
    END AS [t1_StatusTitle],
    [t1].[StatusId] AS [t1_StatusId],
    CASE [t1].[BranchId]
        WHEN 43 THEN N''دفتر مرکزی''
        WHEN 44 THEN N''کارخانه''
        ELSE CAST([t1].[BranchId] AS nvarchar(10))
    END AS [t1_BranchTitle],
    [t1].[TypeId] AS [t1_TypeId],
    [t1].[NeedDateInText] AS [t1_NeedDateInText],
    [t1].[NeedTime] AS [t1_NeedTime],
    [t1].[WaitingTime] AS [t1_WaitingTime],
    [t3].[Title] AS [t3_CostCenterTitle],
    LTRIM(RTRIM(ISNULL([t2].[Name], N'''') + N'' '' + ISNULL([t2].[FamilyDisplay], ISNULL([t2].[Family], N'''')))) AS [t2_PassengerName],
    [t4].[Title] AS [t4_OrgUnitTitle],
    [t1].[CountPeople] AS [t1_CountPeople],
    LTRIM(RTRIM(ISNULL([t5].[Name], N'''') + N'' '' + ISNULL([t5].[FamilyDisplay], ISNULL([t5].[Family], N'''')))) AS [t5_DriverName],
    [t1].[OutdoorCarServiceId] AS [t1_OutdoorCarServiceId],
    [t1].[Destination] AS [t1_Destination],
    [t1].[LastComment] AS [t1_LastComment],
    [t1].[DoneDateTimeInText] AS [t1_DoneDateTimeInText],
    [t1].[Description] AS [t1_Description],
    [t6].[Name] AS [t6_CreatedByName],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[CarRequest] AS [t1]
LEFT JOIN [Hcm].[Personel] AS [t2] ON [t2].[Id] = [t1].[PassengerId]
LEFT JOIN [Gnr].[CostCenter] AS [t3] ON [t3].[Id] = [t1].[CostCenterId]
LEFT JOIN [Hrm].[OrgUnit] AS [t4] ON [t4].[Id] = [t2].[OrgUnitId]
LEFT JOIN [Hcm].[Personel] AS [t5] ON [t5].[Id] = [t1].[DriverId]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[CreatedById]
WHERE [t1].[CreatedById] = TRY_CAST(@CurrentUserId AS BIGINT)', N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', @BtnNoConfirm, N'Srv.CarRequest.Requester');
    INSERT INTO #CrProfiles (Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv)
    VALUES (N'Srv_CarRequest_View', N'درخواست خودرو — مشاهده', N'--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    CASE [t1].[StatusId]
        WHEN 1032 THEN N''در حال بررسی''
        WHEN 1033 THEN N''تایید شده''
        WHEN 1034 THEN N''رد شده''
        WHEN 1047 THEN N''انجام شد''
        WHEN 0 THEN N''0''
        ELSE CAST([t1].[StatusId] AS nvarchar(10))
    END AS [t1_StatusTitle],
    [t1].[StatusId] AS [t1_StatusId],
    CASE [t1].[BranchId]
        WHEN 43 THEN N''دفتر مرکزی''
        WHEN 44 THEN N''کارخانه''
        ELSE CAST([t1].[BranchId] AS nvarchar(10))
    END AS [t1_BranchTitle],
    [t1].[TypeId] AS [t1_TypeId],
    [t1].[NeedDateInText] AS [t1_NeedDateInText],
    [t1].[NeedTime] AS [t1_NeedTime],
    [t1].[WaitingTime] AS [t1_WaitingTime],
    [t3].[Title] AS [t3_CostCenterTitle],
    LTRIM(RTRIM(ISNULL([t2].[Name], N'''') + N'' '' + ISNULL([t2].[FamilyDisplay], ISNULL([t2].[Family], N'''')))) AS [t2_PassengerName],
    [t4].[Title] AS [t4_OrgUnitTitle],
    [t1].[CountPeople] AS [t1_CountPeople],
    LTRIM(RTRIM(ISNULL([t5].[Name], N'''') + N'' '' + ISNULL([t5].[FamilyDisplay], ISNULL([t5].[Family], N'''')))) AS [t5_DriverName],
    [t1].[OutdoorCarServiceId] AS [t1_OutdoorCarServiceId],
    [t1].[Destination] AS [t1_Destination],
    [t1].[LastComment] AS [t1_LastComment],
    [t1].[DoneDateTimeInText] AS [t1_DoneDateTimeInText],
    [t1].[Description] AS [t1_Description],
    [t6].[Name] AS [t6_CreatedByName],
    [t1].[CreatedDateInText] AS [t1_CreatedDateInText]
FROM [Srv].[CarRequest] AS [t1]
LEFT JOIN [Hcm].[Personel] AS [t2] ON [t2].[Id] = [t1].[PassengerId]
LEFT JOIN [Gnr].[CostCenter] AS [t3] ON [t3].[Id] = [t1].[CostCenterId]
LEFT JOIN [Hrm].[OrgUnit] AS [t4] ON [t4].[Id] = [t2].[OrgUnitId]
LEFT JOIN [Hcm].[Personel] AS [t5] ON [t5].[Id] = [t1].[DriverId]
LEFT JOIN [system].[User] AS [t6] ON [t6].[Id] = [t1].[CreatedById]
WHERE [t1].[CreatedById] = TRY_CAST(@CurrentUserId AS BIGINT)', N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]', @BtnPrint, N'Srv.CarRequest.View');

    -- Require CarRequest roles (and ShowAllMenus for All profile)
    DECLARE @MissingRoles NVARCHAR(MAX) = NULL;
    ;WITH NeedRoles AS (
        SELECT DISTINCT LTRIM(RTRIM(value)) AS RoleName
        FROM #CrProfiles CROSS APPLY STRING_SPLIT(RoleCsv, N',')
    )
    SELECT @MissingRoles = STRING_AGG(nr.RoleName, N', ')
    FROM NeedRoles nr
    WHERE NOT EXISTS (SELECT 1 FROM system.Role r WHERE r.Name = nr.RoleName);
    IF @MissingRoles IS NOT NULL AND LEN(@MissingRoles) > 0
    BEGIN
        SET @MissingRoles = N'نقش‌های درخواست خودرو یافت نشد. ابتدا Seed_SrvCarRequest_RolesAndAccess.sql را اجرا کنید. نقش‌های مفقود: ' + @MissingRoles;
        THROW 51720, @MissingRoles, 1;
    END

    DECLARE @Name NVARCHAR(100), @Title NVARCHAR(200), @Sel NVARCHAR(MAX);
    DECLARE @Act NVARCHAR(MAX), @Btn NVARCHAR(MAX), @RoleCsv NVARCHAR(500);
    DECLARE @Qj NVARCHAR(MAX), @Pid BIGINT;

    DECLARE pc CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name, Title, CustomQuery, ActionOptions, CustomButtons, RoleCsv FROM #CrProfiles;
    OPEN pc;
    FETCH NEXT FROM pc INTO @Name, @Title, @Sel, @Act, @Btn, @RoleCsv;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Qj = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @Sel);
        IF ISJSON(@Cols) <> 1 OR ISJSON(@Qj) <> 1 OR ISJSON(@Act) <> 1 OR ISJSON(@Btn) <> 1 OR ISJSON(@Ev) <> 1
            THROW 51721, N'JSON نمایه درخواست خودرو معتبر نیست.', 1;

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

        UPDATE #CrProfiles SET SavedQueryId = @Pid WHERE Name = @Name;

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
    PRINT N'=== DONE Seed_SrvCarRequest_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name LIKE N'Srv_CarRequest_%' ORDER BY Name;
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Srv_CarRequest_All'
ORDER BY CAST(j.[key] AS int);
