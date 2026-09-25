/*
Seed_CngPartPrice_DataProfiles.sql
نمایه داده مدیریت قیمت قطعات CNG. UTF-8 BOM. Idempotent.
sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Seed_CngPartPrice_DataProfiles.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-cng-partprice-dataprofile';
    DECLARE @Name NVARCHAR(100) = N'Cng_PartPrice_List';
    DECLARE @Title NVARCHAR(200) = N'مدیریت قیمت قطعات';
    DECLARE @EntityLower NVARCHAR(200) = N'entities.app.cng.partprice';
    DECLARE @EntityPascal NVARCHAR(200) = N'Entities.App.Cng.PartPrice';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    DECLARE @CustomQuery NVARCHAR(MAX) = N'DECLARE @ShamsiYear INT = TRY_CAST(LEFT(dbo.ToShamsiDate(CAST(GETDATE() AS DATE), N''/''), 4) AS INT);
DECLARE @HamkaranYear INT = CASE
    WHEN @ShamsiYear IS NULL THEN 0
    WHEN @ShamsiYear <= 1399 THEN @ShamsiYear % 100
    ELSE (@ShamsiYear - 1399) + 99
END;

--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    [t2].[Code] AS [t2_Code],
    [t2].[Name] AS [t2_Name],
    [t1].[Price] AS [t1_Price],
    [remain].[QtyBoth] AS [Remain_QtyBoth],
    [lastBuy].[UnitPrice] AS [LastBuy_UnitPrice],
    [lastBuy].[CreatedDate] AS [LastBuy_CreatedDate],
    [lastBuy].[CompanyName] AS [LastBuy_CompanyName],
    [t1].[UpdatedDateShamsi] AS [t1_UpdatedDateShamsi],
    [t1].[IsInternal] AS [t1_IsInternal],
    [t1].[IsExternal] AS [t1_IsExternal],
    [t1].[Comment] AS [t1_Comment],
    [t1].[Description] AS [t1_Description]
FROM [Cng].[PartPrice] AS [t1]
LEFT JOIN [Inv].[Part] AS [t2] ON [t1].[PartId] = [t2].[Id]
LEFT JOIN [TMS].[TotalSystem].[dbo].[Vw_Bom_PartLastPrice] AS [lastBuy]
    ON [lastBuy].[Part_FK] = [t2].[HtsId]
LEFT JOIN (
    SELECT
        Part.Code AS PartCode,
        (
            SELECT SUM(Cardex.XValue)
            FROM [ERPS].[Erps].[LGS3].[vwCardexPart] AS Cardex
            WHERE Cardex.PartRef = Part.PartID
              AND Cardex.StoreRef = S.StoreID
              AND Cardex.State <> 3
              AND Cardex.TypeOfEffectOnStock = 1
        ) AS QtyBoth
    FROM [ERPS].[Erps].[LGS3].[Part] AS Part
    INNER JOIN [ERPS].[Erps].[LGS3].[InventoryVoucherItem] AS i ON i.PartRef = Part.PartID
    INNER JOIN [ERPS].[Erps].[LGS3].[Store] AS S ON S.StoreID = i.StoreRef
    WHERE i.FiscalYearRef = @HamkaranYear
      AND S.Code = 17
    GROUP BY Part.PartID, Part.Code, S.StoreID
) AS [remain] ON [remain].[PartCode] COLLATE DATABASE_DEFAULT = [t2].[Code]';
    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[{"TableName":"Cng.PartPrice","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"t2_Code","DisplayName":"کد کالا","Alliance":"t2_Code","Address":"[t2].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"t2_Name","DisplayName":"نام کالا","Alliance":"t2_Name","Address":"[t2].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":280,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.PartPrice","ColumnName":"t1_Price","DisplayName":"قیمت","Alliance":"t1_Price","Address":"[t1].[Price]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''''''') return ''''''''; if (typeof MJUtil !== ''''undefined'''' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"Remain_QtyBoth","DisplayName":"مانده","Alliance":"Remain_QtyBoth","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"LastBuy_UnitPrice","DisplayName":"آخرین قیمت خرید","Alliance":"LastBuy_UnitPrice","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''''''') return ''''''''; if (typeof MJUtil !== ''''undefined'''' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"LastBuy_CreatedDate","DisplayName":"تاریخ آخرین خرید","Alliance":"LastBuy_CreatedDate","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"LastBuy_CompanyName","DisplayName":"نام شرکت","Alliance":"LastBuy_CompanyName","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.PartPrice","ColumnName":"t1_UpdatedDateShamsi","DisplayName":"تاریخ بروزرسانی","Alliance":"t1_UpdatedDateShamsi","Address":"[t1].[UpdatedDateShamsi]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.PartPrice","ColumnName":"t1_IsInternal","DisplayName":"داخلی","Alliance":"t1_IsInternal","Address":"[t1].[IsInternal]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":70,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.PartPrice","ColumnName":"t1_IsExternal","DisplayName":"خارجی","Alliance":"t1_IsExternal","Address":"[t1].[IsExternal]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":70,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.PartPrice","ColumnName":"t1_Comment","DisplayName":"کامنت","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Cng.PartPrice","ColumnName":"t1_Description","DisplayName":"توضیحات (شخصی)","Alliance":"t1_Description","Address":"[t1].[Description]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""}]';
    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":true,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":true,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @CustomButtons NVARCHAR(MAX) = N'[{"id":"cng_partprice_import_excel","title":"افزودن از اکسل","dataActionName":"importExcel","colorClass":"btn-color-success","iconClass":"ki-outline ki-file-up","requiresSelection":false,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    appController.addPage(''/panel/importData/list'', true, ''ورود اطلاعات'');\n}"},{"id":"cng_partprice_attachments","title":"فایل های ضمیمه شده","dataActionName":"attachments","colorClass":"btn-color-info","iconClass":"ki-outline ki-folder","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Cng/PartPriceAttachment/ListByParentId?partPriceId='' + id, true, ''فایل های ضمیمه شده'');\n}"}]';
    DECLARE @QueryJson NVARCHAR(MAX) = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @CustomQuery);
    DECLARE @Pid BIGINT;

    IF ISJSON(@ColumnsJson) <> 1 OR ISJSON(@QueryJson) <> 1 OR ISJSON(@ActionOptions) <> 1 OR ISJSON(@CustomButtons) <> 1
        THROW 51600, N'JSON نمایه قیمت قطعات معتبر نیست.', 1;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
    BEGIN
        UPDATE system.SavedQuery SET Title=@Title, QueryJson=@QueryJson, ColumnsJson=@ColumnsJson, EntityFullName=@EntityLower, Mode=1, Type=1,
            ActionOptions=@ActionOptions, CustomActionButtonsJson=@CustomButtons, EventScriptsJson=N'{"onSelectedRow":"","onRowAdded":""}',
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
        VALUES (@Name, @Title, @QueryJson, @ColumnsJson, N'{}', @CustomButtons, N'{"onSelectedRow":"","onRowAdded":""}',
             1, 1, @EntityLower, @ActionOptions, 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
        SET @Pid = SCOPE_IDENTITY();
        PRINT N'  CREATED ' + @Name;
    END

    DELETE ra FROM system.RoleAccess ra WHERE ra.ActionAccessType=3 AND ra.RowId=@Pid;
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), 3, 7, @EntityPascal, @Title, NULL, @Pid, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (N'ShowAllMenus', N'Cng.PartPrice.Manage', N'Cng.PartPrice.View', N'مشاهده کلی HTS');
    PRINT N'  Profile RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_CngPartPrice_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name = N'Cng_PartPrice_List';
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance, JSON_VALUE(j.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'Cng_PartPrice_List'
ORDER BY CAST(j.[key] AS int);
