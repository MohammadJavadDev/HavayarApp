-- ============================================================================
-- نمایه داده پارت لیست (یک ردیف به‌ازای هر محصول) + پارت لیست مصرفی
-- UTF-8 with BOM required
-- ============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-eng-partlist';

    /* ========== Eng_PartListProduct_ByProduct ========== */
    DECLARE @PName NVARCHAR(200) = N'Eng_PartListProduct_ByProduct';
    DECLARE @PTitle NVARCHAR(200) = N'پارت لیست';
    DECLARE @PEntity NVARCHAR(200) = N'entities.app.eng.partlistproductsection';
    DECLARE @PPascal NVARCHAR(200) = N'Entities.App.Eng.PartListProductSection';

    DECLARE @PQuery NVARCHAR(MAX) = N'SELECT
    CAST([x].[ProductId] AS BIGINT) AS [t1_Id],
    [x].[ProductId] AS [t1_ProductId],
    [p].[Code] AS [t2_Code],
    [p].[Name] AS [t2_Name],
    [x].[CategoryCount] AS [t1_CategoryCount],
    [x].[PartCount] AS [t1_PartCount]
FROM (
    SELECT [s].[ProductId],
           COUNT(*) AS [CategoryCount],
           (SELECT COUNT(*) FROM [Eng].[PartListProductBom] [b] WHERE [b].[ProductId] = [s].[ProductId]) AS [PartCount]
    FROM [Eng].[PartListProductSection] [s]
    GROUP BY [s].[ProductId]
) [x]
INNER JOIN [Inv].[Part] [p] ON [p].[Id] = [x].[ProductId]';

    DECLARE @PQueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @PQueryJson = JSON_MODIFY(@PQueryJson, '$.CustomQuery', @PQuery);

    DECLARE @PCols NVARCHAR(MAX) = N'[
{"TableName":"Eng.PartListProductSection","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[x].[ProductId]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":1,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"t2_Code","DisplayName":"کد محصول","Alliance":"t2_Code","Address":"[p].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"t2_Name","DisplayName":"نام محصول","Alliance":"t2_Name","Address":"[p].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":350,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Eng.PartListProductSection","ColumnName":"t1_CategoryCount","DisplayName":"تعداد دسته‌بندی","Alliance":"t1_CategoryCount","Address":"[x].[CategoryCount]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Eng.PartListProductSection","ColumnName":"t1_PartCount","DisplayName":"تعداد قطعات","Alliance":"t1_PartCount","Address":"[x].[PartCount]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""}
]';

    DECLARE @PActions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';

    DECLARE @PBtns NVARCHAR(MAX) = N'[{"id":"eng_partlist_import_excel","title":"ورود از اکسل","dataActionName":"importExcel","colorClass":"btn-color-success","iconClass":"ki-outline ki-file-up","requiresSelection":false,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    appController.addPage(''/panel/importData/list'', true, ''ورود اطلاعات'');\n}"}]';

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @PName)
        UPDATE system.SavedQuery SET
            Title = @PTitle, EntityFullName = @PEntity, Mode = 1, Type = 1,
            QueryJson = @PQueryJson, ColumnsJson = @PCols, ActionOptions = @PActions,
            CustomActionButtonsJson = @PBtns, DiagramJson = N'{}',
            ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = @PName;
    ELSE
        INSERT INTO system.SavedQuery
        (Name, Title, EntityFullName, Mode, Type, QueryJson, ColumnsJson, ActionOptions, CustomActionButtonsJson, EventScriptsJson, DiagramJson,
         CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive)
        VALUES
        (@PName, @PTitle, @PEntity, 1, 1, @PQueryJson, @PCols, @PActions, @PBtns, N'{"onSelectedRow":"","onRowAdded":""}', N'{}',
         @SeedUser, @Now, @NowShamsi, 1);

    /* ========== Eng_PartListDlBom_List ========== */
    DECLARE @DName NVARCHAR(200) = N'Eng_PartListDlBom_List';
    DECLARE @DTitle NVARCHAR(200) = N'پارت لیست مصرفی';
    DECLARE @DEntity NVARCHAR(200) = N'entities.app.eng.partlistdlbom';
    DECLARE @DPascal NVARCHAR(200) = N'Entities.App.Eng.PartListDlBom';

    DECLARE @DQuery NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t2].[Title] AS [t2_Title],
    [t3].[Code] AS [t3_Code],
    [t3].[Name] AS [t3_Name],
    [t4].[Title] AS [t4_Title],
    [t5].[Code] AS [t5_Code],
    [t5].[Name] AS [t5_Name],
    [t1].[Qty] AS [t1_Qty],
    [t1].[Order] AS [t1_Order],
    [t1].[IsExistInOriginalBom] AS [t1_IsExistInOriginalBom],
    [t1].[IsIgnored] AS [t1_IsIgnored],
    [t1].[IsOnlyExistInOriginalBom] AS [t1_IsOnlyExistInOriginalBom],
    [t1].[IsReadFromSimilarPart] AS [t1_IsReadFromSimilarPart],
    [t1].[Comment] AS [t1_Comment],
    [t1].[DlId] AS [t1_DlId],
    [t1].[ProductId] AS [t1_ProductId]
FROM [Eng].[PartListDlBom] AS [t1]
LEFT JOIN [FIN].[DL] AS [t2] ON [t1].[DlId] = [t2].[Id]
LEFT JOIN [Inv].[Part] AS [t3] ON [t1].[ProductId] = [t3].[Id]
LEFT JOIN [Eng].[PartListProductSection] AS [t4] ON [t1].[ProductSectionId] = [t4].[Id]
LEFT JOIN [Inv].[Part] AS [t5] ON [t1].[PartId] = [t5].[Id]';

    DECLARE @DQueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @DQueryJson = JSON_MODIFY(@DQueryJson, '$.CustomQuery', @DQuery);

    DECLARE @DCols NVARCHAR(MAX) = N'[
{"TableName":"Eng.PartListDlBom","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":1,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"FIN.DL","ColumnName":"t2_Title","DisplayName":"مرکز پروژه","Alliance":"t2_Title","Address":"[t2].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"t3_Code","DisplayName":"کد محصول","Alliance":"t3_Code","Address":"[t3].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"t3_Name","DisplayName":"نام محصول","Alliance":"t3_Name","Address":"[t3].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Eng.PartListProductSection","ColumnName":"t4_Title","DisplayName":"دسته","Alliance":"t4_Title","Address":"[t4].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"t5_Code","DisplayName":"کد کالا","Alliance":"t5_Code","Address":"[t5].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"t5_Name","DisplayName":"نام کالا","Alliance":"t5_Name","Address":"[t5].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Eng.PartListDlBom","ColumnName":"t1_Qty","DisplayName":"مقدار","Alliance":"t1_Qty","Address":"[t1].[Qty]","SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Eng.PartListDlBom","ColumnName":"t1_Order","DisplayName":"ترتیب","Alliance":"t1_Order","Address":"[t1].[Order]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":70,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Eng.PartListDlBom","ColumnName":"t1_IsExistInOriginalBom","DisplayName":"موجود در پارت لیست","Alliance":"t1_IsExistInOriginalBom","Address":"[t1].[IsExistInOriginalBom]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Eng.PartListDlBom","ColumnName":"t1_IsIgnored","DisplayName":"غیرفعال","Alliance":"t1_IsIgnored","Address":"[t1].[IsIgnored]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Eng.PartListDlBom","ColumnName":"t1_IsOnlyExistInOriginalBom","DisplayName":"فقط در پارت لیست","Alliance":"t1_IsOnlyExistInOriginalBom","Address":"[t1].[IsOnlyExistInOriginalBom]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Eng.PartListDlBom","ColumnName":"t1_IsReadFromSimilarPart","DisplayName":"از کالای مشابه","Alliance":"t1_IsReadFromSimilarPart","Address":"[t1].[IsReadFromSimilarPart]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Eng.PartListDlBom","ColumnName":"t1_Comment","DisplayName":"توضیحات","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Eng.PartListDlBom","ColumnName":"t1_DlId","DisplayName":"شناسه مرکز","Alliance":"t1_DlId","Address":"[t1].[DlId]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Eng.PartListDlBom","ColumnName":"t1_ProductId","DisplayName":"شناسه محصول","Alliance":"t1_ProductId","Address":"[t1].[ProductId]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""}
]';

    DECLARE @DActions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';

    DECLARE @DBtns NVARCHAR(MAX) = N'[{"id":"eng_dlbom_renew","title":"حذف و بازخوانی","dataActionName":"renew","colorClass":"btn-color-warning","iconClass":"ki-arrows-circle ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var row = ctx && (ctx.selectedRow || {});\n    var dlId = row.t1_DlId || row.t1_dlId;\n    var productId = row.t1_ProductId || row.t1_productId;\n    if ((!dlId || dlId <= 0) && (!productId || productId <= 0)) { toastr.error(''ردیف معتبر انتخاب نشده''); return; }\n    Swal.fire({ title: ''حذف و بازخوانی؟'', text: ''اسنپ‌شات فعلی پاک و دوباره ساخته می‌شود'', icon: ''warning'', showCancelButton: true, confirmButtonText: ''بله'', cancelButtonText: ''خیر'' }).then(function(res) {\n        if (!res.isConfirmed) return;\n        var body = (dlId && dlId > 0) ? { dlId: dlId } : { productId: productId };\n        post(''/Panel/Eng/PartListDlBom/Renew'', body, function(r) {\n            if (!r || !r.isSuccess) { toastr.error((r && r.message) || ''خطا''); return; }\n            toastr.success(''بازخوانی انجام شد'');\n            if (ctx && ctx.table && ctx.table.ajax) ctx.table.ajax.reload(null, false);\n        });\n    });\n}"},{"id":"eng_dlbom_view","title":"مشاهده","dataActionName":"viewPrint","colorClass":"btn-color-info","iconClass":"ki-eye ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var row = ctx && (ctx.selectedRow || {});\n    var dlId = row.t1_DlId || row.t1_dlId;\n    var productId = row.t1_ProductId || row.t1_productId;\n    if (!dlId || dlId <= 0) { toastr.error(''برای مشاهده، ردیف باید مرکز پروژه داشته باشد''); return; }\n    var url = ''/Panel/Eng/PartListDlBom/Print?dlId='' + dlId;\n    if (productId && productId > 0) url += ''&productId='' + productId;\n    appController.addPage(url, true, ''مشاهده پارت لیست مصرفی'');\n}"}]';

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @DName)
        UPDATE system.SavedQuery SET
            Title = @DTitle, EntityFullName = @DEntity, Mode = 1, Type = 1,
            QueryJson = @DQueryJson, ColumnsJson = @DCols, ActionOptions = @DActions,
            CustomActionButtonsJson = @DBtns, DiagramJson = N'{}',
            ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = @DName;
    ELSE
        INSERT INTO system.SavedQuery
        (Name, Title, EntityFullName, Mode, Type, QueryJson, ColumnsJson, ActionOptions, CustomActionButtonsJson, EventScriptsJson, DiagramJson,
         CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive)
        VALUES
        (@DName, @DTitle, @DEntity, 1, 1, @DQueryJson, @DCols, @DActions, @DBtns, N'{"onSelectedRow":"","onRowAdded":""}', N'{}',
         @SeedUser, @Now, @NowShamsi, 1);

    /* RoleAccess for profiles — ActionAccessType=3 DataProfile */
    DECLARE @PId BIGINT = (SELECT Id FROM system.SavedQuery WHERE Name = @PName);
    DECLARE @DId BIGINT = (SELECT Id FROM system.SavedQuery WHERE Name = @DName);

    INSERT INTO system.RoleAccess (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@PId AS NVARCHAR(20)), 3, 7, @PPascal, NULL, NULL, @PId, r.Id, @SeedUser, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE (
        EXISTS (SELECT 1 FROM system.RoleAccess ra WHERE ra.RoleId = r.Id AND (ra.Path LIKE N'/panel/eng/partlist%' OR ra.Path LIKE N'/panel/eng/partlistproduct%'))
        OR r.Name IN (N'ShowAllMenus', N'FullAccess')
    )
    AND NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = r.Id AND x.Path = N'dataProfile_' + CAST(@PId AS NVARCHAR(20)) AND x.ActionAccessType = 3
    );

    INSERT INTO system.RoleAccess (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@DId AS NVARCHAR(20)), 3, 7, @DPascal, NULL, NULL, @DId, r.Id, @SeedUser, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE (
        EXISTS (SELECT 1 FROM system.RoleAccess ra WHERE ra.RoleId = r.Id AND (ra.Path LIKE N'/panel/eng/projectutilizedmaterial%' OR ra.Path LIKE N'/panel/eng/partlistdlbom%'))
        OR r.Name IN (N'ShowAllMenus', N'FullAccess')
    )
    AND NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = r.Id AND x.Path = N'dataProfile_' + CAST(@DId AS NVARCHAR(20)) AND x.ActionAccessType = 3
    );

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_Eng_PartList_DataProfiles ===';
    SELECT Id, Name, Title FROM system.SavedQuery WHERE Name IN (@PName, @DName);
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH
