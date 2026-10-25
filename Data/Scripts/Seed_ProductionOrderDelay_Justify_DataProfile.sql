/*
Seed_ProductionOrderDelay_Justify_DataProfile.sql
نمایه «کارتابل توجیه» روی همان لیست تاخیر سفارش ساخت.
فقط تاخیرهایی که کاربر جاری در کاربران همان عامل توقف است.
دکمه «ثبت توجیه» مودال باز می‌کند؛ صفحه جدا ندارد.
Encoding: UTF-8 with BOM
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
BEGIN TRANSACTION;

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'seed-pod-justify-profile';
DECLARE @Name NVARCHAR(100) = N'Pln_ProductionOrderDelay_JustifyCartable';
DECLARE @Title NVARCHAR(200) = N'کارتابل توجیه تاخیر';
DECLARE @Entity NVARCHAR(300) = N'entities.app.pln.productionorderdelay';
DECLARE @EntityPascal NVARCHAR(300) = N'Entities.App.Pln.ProductionOrderDelay';
DECLARE @RoleName NVARCHAR(200) = N'Pln.ProductionOrderDelay.Justify';

DECLARE @Query NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [po].[ProductionOrderNumber] AS [ProductionOrderNumber],
    CASE [t1].[DelayResponsible]
        WHEN 752 THEN N''توقفات صنایع''
        WHEN 753 THEN N''توقفات مهندسی''
        WHEN 754 THEN N''توقفات تدارکات''
        WHEN 755 THEN N''توقفات تولید''
        WHEN 756 THEN N''توقفات تست''
        WHEN 757 THEN N''توقفات پیش بینی نشده معاونت اجرایی''
        WHEN 758 THEN N''توقفات شناسایی نشده معاونت اجرایی''
        WHEN 759 THEN N''توقفات فروش (منسوخ شده)''
        WHEN 760 THEN N''توقفات بازرگانی''
        WHEN 761 THEN N''توقفات مالی''
        WHEN 2376 THEN N''توقفات تولید ناشی از تغذیه نامناسب خطوط''
        WHEN 2422 THEN N''توقفات انبار''
        WHEN 2423 THEN N''توقفات کمپرسورهای مهندسی''
        WHEN 2424 THEN N''توقفات فروش صنعتی 1''
        WHEN 2425 THEN N''توقفات فروش صنعتی 2''
        WHEN 2426 THEN N''توقفات فروش صنعتی 3''
        WHEN 2427 THEN N''توقفات فروش گازهای صنعتی''
        WHEN 2428 THEN N''توقفات فروش تجهیزات پزشکی''
        WHEN 2429 THEN N''توقفات فروش توربو ماشین''
        WHEN 2430 THEN N''توقفات فروش CNG''
        WHEN 2431 THEN N''توقفات فروش هوای فشرده''
        WHEN 2540 THEN N''توقفات فروش صنعتی''
        ELSE CAST([t1].[DelayResponsible] AS nvarchar(20))
    END AS [t1_DelayResponsibleTitle],
    [t1].[DelayDays] AS [t1_DelayDays],
    [t1].[DelayStartShamsiDate] AS [t1_DelayStartShamsiDate],
    [t1].[DelayEndShamsiDate] AS [t1_DelayEndShamsiDate],
    [t1].[DelayReason] AS [t1_DelayReason],
    [t1].[JustificationComment] AS [t1_JustificationComment],
    [t1].[TrusteeName] AS [t1_TrusteeName],
    [t1].[JustificationShamsiDateTime] AS [t1_JustificationShamsiDateTime],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime]
FROM [Pln].[ProductionOrderDelay] AS [t1]
LEFT JOIN [Sale].[ProductionOrder] AS [po] ON [po].[Id] = [t1].[ProductionOrderId]
WHERE EXISTS (
    SELECT 1
    FROM [Pln].[ProductionOrderDelayResponsibleUser] AS [ru]
    WHERE [ru].[DelayResponsible] = [t1].[DelayResponsible]
      AND EXISTS (
          SELECT 1
          FROM OPENJSON([ru].[UserIds]) AS [j]
          WHERE TRY_CAST([j].[value] AS bigint) = TRY_CAST(@CurrentUserId AS bigint)
      )
)';

DECLARE @QueryJson NVARCHAR(MAX) = (
    SELECT
        JSON_QUERY(N'[]') AS Tables,
        JSON_QUERY(N'[]') AS Relations,
        JSON_QUERY(N'[]') AS Filters,
        JSON_QUERY(N'[]') AS CustomConditions,
        @Query AS CustomQuery,
        JSON_QUERY(N'[]') AS Parameters,
        CAST(NULL AS nvarchar(max)) AS Selects
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES
);

DECLARE @ColumnsJson NVARCHAR(MAX) = N'[
{"TableName":"Pln.ProductionOrderDelay","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"ProductionOrderNumber","DisplayName":"شماره سفارش ساخت","Alliance":"ProductionOrderNumber","Address":"[po].[ProductionOrderNumber]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Pln.ProductionOrderDelay","ColumnName":"t1_DelayResponsibleTitle","DisplayName":"عامل توقف","Alliance":"t1_DelayResponsibleTitle","Address":"[t1].[DelayResponsible]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Pln.ProductionOrderDelay","ColumnName":"t1_DelayDays","DisplayName":"تعداد روز تاخیر","Alliance":"t1_DelayDays","Address":"[t1].[DelayDays]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Pln.ProductionOrderDelay","ColumnName":"t1_DelayStartShamsiDate","DisplayName":"تاریخ شروع تاخیر","Alliance":"t1_DelayStartShamsiDate","Address":"[t1].[DelayStartShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Pln.ProductionOrderDelay","ColumnName":"t1_DelayEndShamsiDate","DisplayName":"تاریخ پایان تاخیر","Alliance":"t1_DelayEndShamsiDate","Address":"[t1].[DelayEndShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Pln.ProductionOrderDelay","ColumnName":"t1_DelayReason","DisplayName":"علت تاخیر","Alliance":"t1_DelayReason","Address":"[t1].[DelayReason]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":260,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Pln.ProductionOrderDelay","ColumnName":"t1_JustificationComment","DisplayName":"توضیحات توجیه","Alliance":"t1_JustificationComment","Address":"[t1].[JustificationComment]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":260,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Pln.ProductionOrderDelay","ColumnName":"t1_TrusteeName","DisplayName":"متولی توجیه","Alliance":"t1_TrusteeName","Address":"[t1].[TrusteeName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Pln.ProductionOrderDelay","ColumnName":"t1_JustificationShamsiDateTime","DisplayName":"تاریخ توجیه","Alliance":"t1_JustificationShamsiDateTime","Address":"[t1].[JustificationShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Pln.ProductionOrderDelay","ColumnName":"t1_CreatedByName","DisplayName":"نام ایجاد کننده","Alliance":"t1_CreatedByName","Address":"[t1].[CreatedByName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Pln.ProductionOrderDelay","ColumnName":"t1_CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":1,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":170,"Filterable":true,"Sortable":true,"ClassName":""}
]';

DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';

DECLARE @Script NVARCHAR(MAX) = N'function(ctx) {
    var row = (ctx && ctx.selectedRow) || {};
    var id = ctx && (ctx.primaryKeyValue || row.t1_Id);
    if (!id) { toastr.error(''یک ردیف را انتخاب کنید''); return; }
    function esc(v) { return jQuery(''<div>'').text(v == null || v === '''' ? ''-'' : String(v)).html(); }
    function field(label, value) {
        return ''<div class="col-md-6 form-group-inline"><label>'' + label + ''</label><input class="form-control" readonly value="'' + esc(value) + ''" /></div>'';
    }
    var html = ''<div class="row g-3">''
        + field(''شماره سفارش ساخت'', row.ProductionOrderNumber)
        + field(''عامل توقف'', row.t1_DelayResponsibleTitle)
        + field(''تعداد روز تاخیر'', row.t1_DelayDays)
        + field(''تاریخ شروع'', row.t1_DelayStartShamsiDate)
        + field(''تاریخ پایان'', row.t1_DelayEndShamsiDate)
        + ''<div class="col-md-12 form-group-inline"><label>علت تاخیر</label><textarea class="form-control" rows="2" readonly>'' + esc(row.t1_DelayReason) + ''</textarea></div>''
        + ''<div class="col-md-12 form-group-inline"><label>توضیحات توجیه</label><textarea class="form-control" id="podJustifyComment" rows="4" maxlength="1024" data-required="true">'' + esc(row.t1_JustificationComment) + ''</textarea><div data-invalidmessagespan class="my-1 mx-1"><span class="text-danger"></span></div></div>''
        + ''</div>'';
    $.confirm({
        title: ''ثبت توجیه'',
        columnClass: ''col-md-8'',
        content: html,
        rtl: true,
        buttons: {
            save: {
                text: ''ذخیره'',
                btnClass: ''btn-primary'',
                action: function () {
                    var $content = this.$content;
                    if (typeof validateError === ''function'' && validateError($content)) return false;
                    var comment = ($content.find(''#podJustifyComment'').val() || '''').trim();
                    if (!comment) { toastr.error(''توضیحات توجیه را وارد کنید'', ''خطا''); return false; }
                    var dlg = this;
                    var $saveBtn = jQuery(this.buttons.save.el).block();
                    post(''/Panel/ProductionOrderDelay/SaveJustification'', { id: id, justificationComment: comment }, function (resp) {
                        $saveBtn.block(false);
                        if (!resp || !resp.isSuccess) { toastr.error((resp && resp.message) || ''ثبت انجام نشد'', ''خطا''); return; }
                        toastr.success(''توجیه با موفقیت ذخیره شد'');
                        dlg.close();
                        if (ctx.table && ctx.table.ajax) ctx.table.ajax.reload(null, false);
                    });
                    return false;
                }
            },
            cancel: { text: ''انصراف'', btnClass: ''btn-secondary'' }
        }
    });
}';

DECLARE @Buttons NVARCHAR(MAX) = (
    SELECT
        N'pod_justify' AS id,
        N'ثبت توجیه' AS title,
        N'justify' AS dataActionName,
        N'btn-color-success' AS colorClass,
        N'ki-message-text ki-outline' AS iconClass,
        CAST(1 AS bit) AS requiresSelection,
        CAST(0 AS bit) AS requiresMultiSelection,
        CAST(0 AS bit) AS useHtml,
        N'' AS html,
        @Script AS actionScript
    FOR JSON PATH
);

DECLARE @Events NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';

IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
BEGIN
    UPDATE system.SavedQuery
    SET Title = @Title,
        QueryJson = @QueryJson,
        ColumnsJson = @ColumnsJson,
        DiagramJson = N'{}',
        CustomActionButtonsJson = @Buttons,
        EventScriptsJson = @Events,
        ActionOptions = @ActionOptions,
        EntityFullName = @Entity,
        Mode = 1,
        Type = 1,
        IsActive = 1,
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = @Name;
END
ELSE
BEGIN
    INSERT INTO system.SavedQuery
        (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
         ActionOptions, EntityFullName, Mode, Type, IsActive,
         CreatedById, CreatedByName, ModifiedById, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    VALUES
        (@Name, @Title, @QueryJson, @ColumnsJson, N'{}', @Buttons, @Events,
         @ActionOptions, @Entity, 1, 1, 1,
         1, @SeedUser, 1, @SeedUser,
         @Now, @NowShamsi, @Now, @NowShamsi);
END

DECLARE @ProfileId BIGINT = (SELECT Id FROM system.SavedQuery WHERE Name = @Name);

DELETE ra
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.ActionAccessType = 3
  AND ra.RowId = @ProfileId
  AND r.Name IN (@RoleName, N'ShowAllMenus');

INSERT INTO system.RoleAccess
    (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
     CreatedById, ModifiedById, CreatedByName, ModifiedByName,
     CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
SELECT
    N'dataProfile_' + CAST(@ProfileId AS nvarchar(20)),
    3, 7, @EntityPascal, @Title, NULL, @ProfileId, r.Id,
    1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
FROM system.Role r
WHERE r.Name IN (@RoleName, N'ShowAllMenus');

IF ISJSON(@ColumnsJson) <> 1 OR ISJSON(@QueryJson) <> 1 OR ISJSON(@Buttons) <> 1
    THROW 51010, N'JSON نمایه توجیه تاخیر معتبر نیست.', 1;

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH

SELECT Id, Name, Title, Mode, Type
FROM system.SavedQuery
WHERE Name = N'Pln_ProductionOrderDelay_JustifyCartable';

SELECT r.Name, ra.Path, ra.ActionAccessType, ra.RowId
FROM system.RoleAccess ra
INNER JOIN system.Role r ON r.Id = ra.RoleId
WHERE ra.RowId = (SELECT Id FROM system.SavedQuery WHERE Name = N'Pln_ProductionOrderDelay_JustifyCartable')
   OR (r.Name = N'Pln.ProductionOrderDelay.Justify' AND ra.ActionAccessType <> 3);
