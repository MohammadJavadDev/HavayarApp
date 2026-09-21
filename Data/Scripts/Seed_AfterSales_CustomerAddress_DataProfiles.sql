/*
  Seed_AfterSales_CustomerAddress_DataProfiles.sql
  نمایه AfterSales_CustomerAddress_List مطابق گرید HTS صفحه 234 سایت های مشتریان.
  New/Delete خاموش. ستون‌ها: کد مشتری، مشتری، منطقه، استان، گرید، نام جایگاه، آدرس،
  تفصیل نمایندگی، کاربر ویرایشگر، زمان ویرایش.
  Encoding: UTF-8 with BOM (sqlcmd -f 65001). Idempotent.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-aftersales-customeraddress-profile';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    DECLARE @PName NVARCHAR(100) = N'AfterSales_CustomerAddress_List';
    DECLARE @PTitle NVARCHAR(200) = N'سایت های مشتریان';
    DECLARE @PEnt NVARCHAR(200) = N'entities.app.sls.customeraddress';
    DECLARE @PPascal NVARCHAR(200) = N'Entities.App.SLS.CustomerAddress';
    DECLARE @PSelect NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[HtsId] AS [t1_HtsId],
    [t2].[Code] AS [t2_Code],
    [t3].[FullName] AS [t3_FullName],
    [t4].[Title] AS [t4_Title],
    [t5].[Name] AS [t5_Name],
    [t1].[Grade] AS [t1_Grade],
    [t1].[Title] AS [t1_Title],
    [t1].[Address] AS [t1_Address],
    [t7].[Title] AS [t7_Title],
    [t1].[ModifiedByName] AS [t1_ModifiedByName],
    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime]
FROM [SLS].[CustomerAddress] AS [t1]
LEFT JOIN [SLS].[Customer] AS [t2] ON [t1].[CustomerId] = [t2].[Id]
LEFT JOIN [Gnr].[Party] AS [t3] ON [t2].[PartyId] = [t3].[Id]
LEFT JOIN [Crm].[Zone] AS [t4] ON [t1].[ZoneId] = [t4].[Id]
LEFT JOIN [Gnr].[Region] AS [t5] ON [t1].[ProvinceId] = [t5].[Id]
LEFT JOIN [FIN].[DL] AS [t7] ON [t1].[AgencyDlId] = [t7].[Id]';
    DECLARE @PCols NVARCHAR(MAX) = N'[{"TableName":"SLS.CustomerAddress","ColumnName":"Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.CustomerAddress","ColumnName":"HtsId","DisplayName":"شناسه HTS","Alliance":"t1_HtsId","Address":"[t1].[HtsId]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.Customer","ColumnName":"Code","DisplayName":"کد مشتری","Alliance":"t2_Code","Address":"[t2].[Code]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Gnr.Party","ColumnName":"FullName","DisplayName":"مشتری","Alliance":"t3_FullName","Address":"[t3].[FullName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Crm.Zone","ColumnName":"Title","DisplayName":"منطقه","Alliance":"t4_Title","Address":"[t4].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Gnr.Region","ColumnName":"Name","DisplayName":"استان","Alliance":"t5_Name","Address":"[t5].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.CustomerAddress","ColumnName":"Grade","DisplayName":"گرید","Alliance":"t1_Grade","Address":"[t1].[Grade]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.SLS.Enums.CustomerGradeEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.CustomerAddress","ColumnName":"Title","DisplayName":"نام جایگاه","Alliance":"t1_Title","Address":"[t1].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.CustomerAddress","ColumnName":"Address","DisplayName":"آدرس","Alliance":"t1_Address","Address":"[t1].[Address]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"FIN.DL","ColumnName":"Title","DisplayName":"تفصیل نمایندگی","Alliance":"t7_Title","Address":"[t7].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.CustomerAddress","ColumnName":"ModifiedByName","DisplayName":"کاربر ویرایشگر","Alliance":"t1_ModifiedByName","Address":"[t1].[ModifiedByName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"SLS.CustomerAddress","ColumnName":"ModifiedDateShamsiDateTime","DisplayName":"زمان ویرایش","Alliance":"t1_ModifiedDateShamsiDateTime","Address":"[t1].[ModifiedDateShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""}]';
    DECLARE @PAct NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @PBtn NVARCHAR(MAX) = N'[{"id":"as_edit_customeraddress","title":"ویرایش","dataActionName":"editRow","colorClass":"btn-color-primary","iconClass":"ki-pencil ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/SLS/CustomerAddress/Edit?id='' + id, true, ''ویرایش'');\n}"}]';
    DECLARE @PQueryJson NVARCHAR(MAX) = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @PSelect);
    DECLARE @PId BIGINT;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @PName)
    BEGIN
        UPDATE system.SavedQuery
        SET Title = @PTitle,
            QueryJson = @PQueryJson,
            ColumnsJson = @PCols,
            EntityFullName = @PEnt,
            Mode = 1,
            Type = 1,
            ActionOptions = @PAct,
            CustomActionButtonsJson = @PBtn,
            EventScriptsJson = N'{"onSelectedRow":"","onRowAdded":""}',
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
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
        N'ShowAllMenus',
        N'Sale.AfterSales.CustomerAddress.Manage',
        N'Sale.AfterSales.CustomerAddress.View'
    );
    PRINT N'  Profile RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_AfterSales_CustomerAddress_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    PRINT N'=== ERROR rolled back ===';
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name = N'AfterSales_CustomerAddress_List';
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName,
       JSON_VALUE(j.value, '$.Visible') AS Visible
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'AfterSales_CustomerAddress_List';
