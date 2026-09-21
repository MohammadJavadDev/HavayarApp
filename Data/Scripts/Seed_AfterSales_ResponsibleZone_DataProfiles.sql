/*
  Seed_AfterSales_ResponsibleZone_DataProfiles.sql
  نمایه مسئولین مناطق مطابق گرید HTS صفحه 356 Sale_ResponsibleZone
  و نمایه مشتریان مطابق گرید فرزند HTS (کد مشتری / مشتری).
  Encoding: UTF-8 with BOM (sqlcmd -f 65001). Idempotent.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-aftersales-responsiblezone-profile';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    DECLARE @PName NVARCHAR(100) = N'AfterSales_ResponsibleZone_List';
    DECLARE @PTitle NVARCHAR(200) = N'مسئولین مناطق';
    DECLARE @PEnt NVARCHAR(200) = N'entities.app.sale.responsiblezone';
    DECLARE @PPascal NVARCHAR(200) = N'Entities.App.Sale.ResponsibleZone';
    DECLARE @PSelect NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[HtsId] AS [t1_HtsId],
    [t2].[Name] AS [t2_Name],
    [t3].[Name] AS [t3_Name],
    [t4].[Title] AS [t4_Title],
    [t1].[CustomersTitle] AS [t1_CustomersTitle],
    [t1].[Comment] AS [t1_Comment],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime]
FROM [Sale].[ResponsibleZone] AS [t1]
LEFT JOIN [system].[User] AS [t2] ON [t1].[ResponsibleId] = [t2].[Id]
LEFT JOIN [Gnr].[Region] AS [t3] ON [t1].[ProvinceId] = [t3].[Id]
LEFT JOIN [Crm].[Zone] AS [t4] ON [t1].[ZoneId] = [t4].[Id]';
    DECLARE @PCols NVARCHAR(MAX) = N'[
{"TableName":"Sale.ResponsibleZone","ColumnName":"Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ResponsibleZone","ColumnName":"HtsId","DisplayName":"شناسه HTS","Alliance":"t1_HtsId","Address":"[t1].[HtsId]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"system.User","ColumnName":"Name","DisplayName":"مسئول","Alliance":"t2_Name","Address":"[t2].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Region","ColumnName":"Name","DisplayName":"استان","Alliance":"t3_Name","Address":"[t3].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Crm.Zone","ColumnName":"Title","DisplayName":"منطقه","Alliance":"t4_Title","Address":"[t4].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ResponsibleZone","ColumnName":"CustomersTitle","DisplayName":"عنوان مشتریان","Alliance":"t1_CustomersTitle","Address":"[t1].[CustomersTitle]","SystemTypeName":"String","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ResponsibleZone","ColumnName":"Comment","DisplayName":"توضیحات","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":250,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ResponsibleZone","ColumnName":"CreatedByName","DisplayName":"ایجادکننده","Alliance":"t1_CreatedByName","Address":"[t1].[CreatedByName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ResponsibleZone","ColumnName":"CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""}]';
    DECLARE @PAct NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @PBtn NVARCHAR(MAX) = N'[{"id":"as_edit_responsiblezone","title":"ویرایش","dataActionName":"editRow","colorClass":"btn-color-primary","iconClass":"ki-pencil ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Sale/ResponsibleZone/Edit?id='' + id, true, ''ویرایش'');\n}"},{"id":"as_rz_cust","title":"مشتریان","dataActionName":"zoneCustomers","colorClass":"btn-color-info","iconClass":"ki-people ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && ctx.selectedRow.t1_Id));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Sale/ResponsibleZoneCustomer/ListByParentId?responsibleZoneId='' + id, true, ''مشتریان مسئول منطقه'');\n}"}]';
    DECLARE @PQueryJson NVARCHAR(MAX) = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @PSelect);
    DECLARE @PId BIGINT;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @PName)
    BEGIN
        UPDATE system.SavedQuery
        SET Title = @PTitle, QueryJson = @PQueryJson, ColumnsJson = @PCols, EntityFullName = @PEnt,
            Mode = 1, Type = 1, ActionOptions = @PAct, CustomActionButtonsJson = @PBtn,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
        WHERE Name = @PName;
        SELECT @PId = Id FROM system.SavedQuery WHERE Name = @PName;
        PRINT N'  UPDATED SavedQuery Id=' + CAST(@PId AS nvarchar(20));
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@PName, @PTitle, @PQueryJson, @PCols, N'{}', @PBtn, N'{"onSelectedRow":"","onRowAdded":""}',
             1, 1, @PEnt, @PAct,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @PId = SCOPE_IDENTITY();
        PRINT N'  CREATED SavedQuery Id=' + CAST(@PId AS nvarchar(20));
    END

    DELETE ra FROM system.RoleAccess ra WHERE ra.ActionAccessType = 3 AND ra.RowId = @PId;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        N'dataProfile_' + CAST(@PId AS nvarchar(20)),
        3, 7, @PPascal, @PTitle, NULL, @PId, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (
        N'Sale.AfterSales', N'ShowAllMenus',
        N'Sale.AfterSales.ResponsibleZone.Manage',
        N'Sale.AfterSales.ResponsibleZone.View'
    );
    PRINT N'  Profile 195 RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    DECLARE @CName NVARCHAR(100) = N'AfterSales_ResponsibleZoneCustomer_List';
    DECLARE @CTitle NVARCHAR(200) = N'مشتریان مسئول منطقه';
    DECLARE @CEnt NVARCHAR(200) = N'entities.app.sale.responsiblezonecustomer';
    DECLARE @CPascal NVARCHAR(200) = N'Entities.App.Sale.ResponsibleZoneCustomer';
    DECLARE @CSelect NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[HtsId] AS [t1_HtsId],
    [t1].[ResponsibleZoneId] AS [t1_ResponsibleZoneId],
    [t2].[Code] AS [t2_Code],
    [t3].[FullName] AS [t3_FullName],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime]
FROM [Sale].[ResponsibleZoneCustomer] AS [t1]
LEFT JOIN [SLS].[Customer] AS [t2] ON [t1].[CustomerId] = [t2].[Id]
LEFT JOIN [Gnr].[Party] AS [t3] ON [t2].[PartyId] = [t3].[Id]';
    DECLARE @CCols NVARCHAR(MAX) = N'[
{"TableName":"Sale.ResponsibleZoneCustomer","ColumnName":"Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ResponsibleZoneCustomer","ColumnName":"HtsId","DisplayName":"شناسه HTS","Alliance":"t1_HtsId","Address":"[t1].[HtsId]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ResponsibleZoneCustomer","ColumnName":"ResponsibleZoneId","DisplayName":"مسئول منطقه","Alliance":"t1_ResponsibleZoneId","Address":"[t1].[ResponsibleZoneId]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"SLS.Customer","ColumnName":"Code","DisplayName":"کد مشتری","Alliance":"t2_Code","Address":"[t2].[Code]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Party","ColumnName":"FullName","DisplayName":"مشتری","Alliance":"t3_FullName","Address":"[t3].[FullName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":400,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ResponsibleZoneCustomer","ColumnName":"CreatedByName","DisplayName":"ایجادکننده","Alliance":"t1_CreatedByName","Address":"[t1].[CreatedByName]","SystemTypeName":"String","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ResponsibleZoneCustomer","ColumnName":"CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""}]';
    DECLARE @CAct NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @CBtn NVARCHAR(MAX) = N'[{"id":"as_edit_responsiblezonecustomer","title":"ویرایش","dataActionName":"editRow","colorClass":"btn-color-primary","iconClass":"ki-pencil ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Sale/ResponsibleZoneCustomer/Edit?id='' + id, true, ''ویرایش'');\n}"}]';
    DECLARE @CQueryJson NVARCHAR(MAX) = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @CSelect);
    DECLARE @CId BIGINT;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @CName)
    BEGIN
        UPDATE system.SavedQuery
        SET Title = @CTitle, QueryJson = @CQueryJson, ColumnsJson = @CCols, EntityFullName = @CEnt,
            Mode = 1, Type = 1, ActionOptions = @CAct, CustomActionButtonsJson = @CBtn,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi, IsActive = 1
        WHERE Name = @CName;
        SELECT @CId = Id FROM system.SavedQuery WHERE Name = @CName;
        PRINT N'  UPDATED customer SavedQuery Id=' + CAST(@CId AS nvarchar(20));
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        VALUES
            (@CName, @CTitle, @CQueryJson, @CCols, N'{}', @CBtn, N'{"onSelectedRow":"","onRowAdded":""}',
             1, 1, @CEnt, @CAct,
             1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @CId = SCOPE_IDENTITY();
        PRINT N'  CREATED customer SavedQuery Id=' + CAST(@CId AS nvarchar(20));
    END

    DELETE ra FROM system.RoleAccess ra WHERE ra.ActionAccessType = 3 AND ra.RowId = @CId;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT
        N'dataProfile_' + CAST(@CId AS nvarchar(20)),
        3, 7, @CPascal, @CTitle, NULL, @CId, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (
        N'Sale.AfterSales', N'ShowAllMenus',
        N'Sale.AfterSales.ResponsibleZone.Manage',
        N'Sale.AfterSales.ResponsibleZone.View'
    );
    PRINT N'  Profile 222 RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_AfterSales_ResponsibleZone_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    PRINT N'=== ERROR rolled back ===';
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name IN (N'AfterSales_ResponsibleZone_List', N'AfterSales_ResponsibleZoneCustomer_List');
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName,
       JSON_VALUE(j.value, '$.Visible') AS Visible
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'AfterSales_ResponsibleZone_List';
