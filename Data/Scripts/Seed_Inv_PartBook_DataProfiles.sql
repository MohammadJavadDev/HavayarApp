/*
  Seed_Inv_PartBook_DataProfiles.sql
  نمایه دفترچه قطعات + BOM سفارشی
  UTF-8 with BOM. Apply: sqlcmd -C -b -I -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-inv-partbook';

    /* ========== Inv_PartBook_ByProduct ========== */
    DECLARE @PName NVARCHAR(200) = N'Inv_PartBook_ByProduct';
    DECLARE @PTitle NVARCHAR(200) = N'دفترچه قطعات';
    DECLARE @PEntity NVARCHAR(200) = N'entities.app.inv.partpartbooksection';
    DECLARE @PPascal NVARCHAR(200) = N'Entities.App.Inv.PartPartBookSection';

    DECLARE @PQuery NVARCHAR(MAX) = N'SELECT
    CAST([x].[ProductId] AS BIGINT) AS [t1_Id],
    [x].[ProductId] AS [t1_ProductId],
    [p].[Code] AS [t2_Code],
    [p].[Name] AS [t2_Name],
    [x].[CategoryCount] AS [t1_CategoryCount],
    [x].[PartCount] AS [t1_PartCount]
FROM (
    SELECT [l].[PartId] AS [ProductId],
           COUNT(*) AS [CategoryCount],
           (
               SELECT COUNT(*)
               FROM [Bom].[vw_ProductItem] [bom]
               INNER JOIN [Inv].[PartPartBookSection] [li] ON [li].[PartId] = [bom].[PartItemId]
               WHERE [bom].[PartId] = [l].[PartId]
                 AND [li].[PartBookSectionId] IN (
                     SELECT [l2].[PartBookSectionId] FROM [Inv].[PartPartBookSection] [l2] WHERE [l2].[PartId] = [l].[PartId]
                 )
           )
           +
           (
               SELECT COUNT(*)
               FROM [Inv].[PartCustomBom] [cb]
               WHERE [cb].[ProductId] = [l].[PartId] AND [cb].[IsDisabled] = 0
           ) AS [PartCount]
    FROM [Inv].[PartPartBookSection] [l]
    GROUP BY [l].[PartId]
) [x]
INNER JOIN [Inv].[Part] [p] ON [p].[Id] = [x].[ProductId]';

    DECLARE @PQueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @PQueryJson = JSON_MODIFY(@PQueryJson, '$.CustomQuery', @PQuery);

    DECLARE @PCols NVARCHAR(MAX) = N'[
{"TableName":"Inv.PartPartBookSection","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[x].[ProductId]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":1,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"t2_Code","DisplayName":"کد محصول","Alliance":"t2_Code","Address":"[p].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"t2_Name","DisplayName":"نام محصول","Alliance":"t2_Name","Address":"[p].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":350,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.PartPartBookSection","ColumnName":"t1_CategoryCount","DisplayName":"تعداد دسته","Alliance":"t1_CategoryCount","Address":"[x].[CategoryCount]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.PartPartBookSection","ColumnName":"t1_PartCount","DisplayName":"تعداد قطعات","Alliance":"t1_PartCount","Address":"[x].[PartCount]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""}
]';

    DECLARE @PActions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @PBtns NVARCHAR(MAX) = N'[{"id":"inv_partbook_print","title":"مشاهده","dataActionName":"printPartBook","colorClass":"btn-color-info","iconClass":"ki-outline ki-eye","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {
    var row = ctx && (ctx.selectedRow || {});
    var partId = row.t1_ProductId || row.t1_productId || row.PartId || row.partId;
    if (!partId) { toastr.error(''محصول انتخاب نشده''); return; }
    appController.addPage(''/Panel/Inv/PartBook/Print?partId='' + partId, true, ''چاپ دفترچه قطعات'');
}"}]';

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

    /* RoleAccess for profile */
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT
        N'/savedquery/' + LOWER(@PName), 2, 2, @PPascal, NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Id IN (200052, 260239, 260240, 260350, 260016, 260058)
       OR EXISTS (SELECT 1 FROM system.RoleAccess ra WHERE ra.RoleId = r.Id AND ra.Path = N'/panel/inv/part/list')
    AND NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = r.Id AND x.Path = N'/savedquery/' + LOWER(@PName)
    );

    /* ========== Inv_PartCustomBom_List ========== */
    DECLARE @CName NVARCHAR(200) = N'Inv_PartCustomBom_List';
    DECLARE @CTitle NVARCHAR(200) = N'BOM سفارشی دفترچه قطعات';
    DECLARE @CEntity NVARCHAR(200) = N'entities.app.inv.partcustombom';
    DECLARE @CPascal NVARCHAR(200) = N'Entities.App.Inv.PartCustomBom';

    DECLARE @CQuery NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t2].[Code] AS [t2_Code],
    [t2].[Name] AS [t2_Name],
    [t3].[Code] AS [t3_Code],
    [t3].[Name] AS [t3_Name],
    [t1].[UsingRate] AS [t1_UsingRate],
    [t1].[Order] AS [t1_Order],
    [t1].[IsDisabled] AS [t1_IsDisabled],
    [t1].[Comment] AS [t1_Comment],
    [t1].[ProductId] AS [t1_ProductId],
    [t1].[PartId] AS [t1_PartId]
FROM [Inv].[PartCustomBom] AS [t1]
LEFT JOIN [Inv].[Part] AS [t2] ON [t1].[ProductId] = [t2].[Id]
LEFT JOIN [Inv].[Part] AS [t3] ON [t1].[PartId] = [t3].[Id]';

    DECLARE @CQueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @CQueryJson = JSON_MODIFY(@CQueryJson, '$.CustomQuery', @CQuery);

    DECLARE @CCols NVARCHAR(MAX) = N'[
{"TableName":"Inv.PartCustomBom","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":1,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"t2_Code","DisplayName":"کد محصول","Alliance":"t2_Code","Address":"[t2].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"t2_Name","DisplayName":"نام محصول","Alliance":"t2_Name","Address":"[t2].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"t3_Code","DisplayName":"کد کالا","Alliance":"t3_Code","Address":"[t3].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"t3_Name","DisplayName":"نام کالا","Alliance":"t3_Name","Address":"[t3].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.PartCustomBom","ColumnName":"t1_UsingRate","DisplayName":"مقدار مصرف","Alliance":"t1_UsingRate","Address":"[t1].[UsingRate]","SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.PartCustomBom","ColumnName":"t1_Order","DisplayName":"ترتیب","Alliance":"t1_Order","Address":"[t1].[Order]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.PartCustomBom","ColumnName":"t1_IsDisabled","DisplayName":"غیرفعال","Alliance":"t1_IsDisabled","Address":"[t1].[IsDisabled]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.PartCustomBom","ColumnName":"t1_Comment","DisplayName":"توضیحات","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""}
]';

    DECLARE @CActions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @CBtns NVARCHAR(MAX) = N'[{"id":"inv_pcb_new","title":"جدید","dataActionName":"new","colorClass":"btn-color-primary","iconClass":"ki-outline ki-plus","requiresSelection":false,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) { if (window.__openPartCustomBomModal) window.__openPartCustomBomModal(null); }"},{"id":"inv_pcb_edit","title":"ویرایش","dataActionName":"edit","colorClass":"btn-color-warning","iconClass":"ki-outline ki-pencil","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {
  var row = ctx && (ctx.selectedRow || {});
  var id = row.t1_Id || row.t1_id || row.Id || row.id;
  if (!id) { toastr.error(''ردیف انتخاب نشده''); return; }
  if (window.__openPartCustomBomModal) window.__openPartCustomBomModal(id);
}"},{"id":"inv_pcb_delete","title":"حذف","dataActionName":"delete","colorClass":"btn-color-danger","iconClass":"ki-outline ki-trash","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {
  var row = ctx && (ctx.selectedRow || {});
  var id = row.t1_Id || row.t1_id || row.Id || row.id;
  if (!id) { toastr.error(''ردیف انتخاب نشده''); return; }
  Swal.fire({ title: ''حذف؟'', icon: ''warning'', showCancelButton: true, confirmButtonText: ''حذف'', cancelButtonText: ''انصراف'' })
  .then(function(res) {
    if (!res.isConfirmed) return;
    $$.get(''/Panel/Inv/PartCustomBom/Delete?id='' + id, function(r) {
      if (!r || r.isSuccess === false) { toastr.error((r && r.message) || ''خطا''); return; }
      toastr.success(''حذف شد'');
      if (self.table && self.table.ajax) self.table.ajax.reload(null, false);
    });
  });
}"}]';

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @CName)
        UPDATE system.SavedQuery SET
            Title = @CTitle, EntityFullName = @CEntity, Mode = 1, Type = 1,
            QueryJson = @CQueryJson, ColumnsJson = @CCols, ActionOptions = @CActions,
            CustomActionButtonsJson = @CBtns, DiagramJson = N'{}',
            ModifiedByName = @SeedUser, ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = @CName;
    ELSE
        INSERT INTO system.SavedQuery
        (Name, Title, EntityFullName, Mode, Type, QueryJson, ColumnsJson, ActionOptions, CustomActionButtonsJson, EventScriptsJson, DiagramJson,
         CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, IsActive)
        VALUES
        (@CName, @CTitle, @CEntity, 1, 1, @CQueryJson, @CCols, @CActions, @CBtns, N'{"onSelectedRow":"","onRowAdded":""}', N'{}',
         @SeedUser, @Now, @NowShamsi, 1);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT
        N'/savedquery/' + LOWER(@CName), 2, 2, @CPascal, NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Id IN (200052, 260259, 260350, 260016, 260058)
       OR EXISTS (SELECT 1 FROM system.RoleAccess ra WHERE ra.RoleId = r.Id AND ra.Path = N'/panel/inv/part/list')
    AND NOT EXISTS (
        SELECT 1 FROM system.RoleAccess x
        WHERE x.RoleId = r.Id AND x.Path = N'/savedquery/' + LOWER(@CName)
    );

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_Inv_PartBook_DataProfiles ===';

    SELECT Name, Title,
           CASE WHEN ColumnsJson LIKE N'%شناسه%' THEN N'OK' ELSE N'BAD' END AS PersianCheck
    FROM system.SavedQuery WHERE Name IN (@PName, @CName);
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
