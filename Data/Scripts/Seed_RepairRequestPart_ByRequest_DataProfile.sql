/*
  Seed_RepairRequestPart_ByRequest_DataProfile.sql
  نمایه «اقلام مصرفی» (AfterSales_RepairRequestPart_ByRequest) برای جدول صفحهٔ اقلام مصرفی درخواست تعمیر.

  صفحه: /Panel/Rpr/RepairRequestPart/ListByParentId?repairRequestId=<id>
  موجودیت: Entities.App.Rpr.RepairRequestPart

  - @RepairRequestId از data-query-params صفحه می‌آید (DECLARE نمی‌شود).
  - مجوز ردیف در SQL: admin یا نقشی که /panel/rpr/repairrequestpart/listbyparentid را در RoleAccess دارد.
  - RoleAccess نمایه: همهٔ نقش‌هایی که همین الان به صفحه دسترسی دارند.

  تولید: python _tools/DataProfile/gen_rpr_repairrequestpart_byrequest_seed.py   (utf-8-sig)
  Apply: sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i Data\Scripts\Seed_RepairRequestPart_ByRequest_DataProfile.sql
  بعد از اجرا WebApp را ری‌استارت کنید (کش Redis نمایه‌ها).
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-rr-part-by-request';

    DECLARE @Name NVARCHAR(200) = N'AfterSales_RepairRequestPart_ByRequest';
    DECLARE @Title NVARCHAR(200) = N'اقلام مصرفی';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.rpr.repairrequestpart';
    DECLARE @EntityPascal NVARCHAR(200) = N'Entities.App.Rpr.RepairRequestPart';

    DECLARE @CustomQuery NVARCHAR(MAX) = N'DECLARE @CuId BIGINT = TRY_CAST(CONVERT(NVARCHAR(30), @CurrentUserId) AS BIGINT);
DECLARE @ReqId BIGINT = TRY_CAST(CONVERT(NVARCHAR(30), @RepairRequestId) AS BIGINT);
DECLARE @CuRoles NVARCHAR(MAX);
SELECT @CuRoles = Roles FROM [system].[User] WHERE Id = @CuId;
DECLARE @CanRead BIT = CASE
    WHEN @ReqId IS NULL OR @CuId IS NULL THEN 0
    WHEN EXISTS (
        SELECT 1 FROM OPENJSON(@CuRoles)
        WHERE [value] = N''admin''
    ) THEN 1
    WHEN EXISTS (
        SELECT 1
        FROM OPENJSON(@CuRoles) AS [o]
        INNER JOIN [system].[Role] AS [r] ON [r].[Name] = [o].[value]
        INNER JOIN [system].[RoleAccess] AS [ra] ON [ra].[RoleId] = [r].[Id]
        WHERE [ra].[Path] = N''/panel/rpr/repairrequestpart/listbyparentid''
    ) THEN 1
    ELSE 0 END;
--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    [t2].[IdNumber] AS [t2_IdNumber],
    COALESCE([t4].[Code], [t5].[Code]) AS [t1_ProjectCode],
    [t1].[RahkaranRequestNo] AS [t1_RahkaranRequestNo],
    [t3].[Code] AS [t3_Code],
    [t3].[Name] AS [t3_Name],
    [t1].[StockName] AS [t1_StockName],
    COALESCE([t1].[UnitName], [t6].[Title]) AS [t1_UnitName],
    [t1].[DamagedPart] AS [t1_DamagedPart],
    [t1].[ReplacementPartDescription] AS [t1_ReplacementPartDescription],
    [t1].[BuyPrice] AS [t1_BuyPrice],
    [t1].[SalePrice] AS [t1_SalePrice],
    [t1].[UsedMount] AS [t1_UsedMount],
    [t1].[ReturnMount] AS [t1_ReturnMount],
    CASE WHEN [t1].[SalePrice] IS NULL THEN NULL
         ELSE CAST([t1].[SalePrice] AS DECIMAL(28, 4)) * ([t1].[UsedMount] - [t1].[ReturnMount]) END AS [t1_TotalPrice],
    CASE WHEN [t1].[BuyPrice] IS NULL THEN NULL
         ELSE CAST([t1].[BuyPrice] AS DECIMAL(28, 4)) * ([t1].[UsedMount] - [t1].[ReturnMount]) END AS [t1_TotalBuyPrice],
    [t7].[Name] AS [t7_Name],
    [t1].[Description] AS [t1_Description],
    [t1].[RahkaranDescription] AS [t1_RahkaranDescription],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime]
FROM [Rpr].[RepairRequestPart] AS [t1]
LEFT JOIN [Rpr].[RepairRequest] AS [t2] ON [t2].[Id] = [t1].[RepairRequestId]
LEFT JOIN [Inv].[Part] AS [t3] ON [t3].[Id] = [t1].[PartId]
LEFT JOIN [FIN].[DL] AS [t4] ON [t4].[Id] = [t1].[DlId]
LEFT JOIN [FIN].[DL] AS [t5] ON [t5].[Id] = [t2].[DlId]
LEFT JOIN [Inv].[PartUnit] AS [t6] ON [t6].[Id] = [t1].[PartUnitId]
LEFT JOIN [Inv].[Part] AS [t7] ON [t7].[Id] = [t2].[ProductId]
WHERE @CanRead = 1
  AND [t1].[RepairRequestId] = @ReqId';
    DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.Parameters', JSON_QUERY(N'[{"Id":"repairRequestId","Name":"RepairRequestId","DisplayName":"شناسه درخواست تعمیر","DefaultValue":"","DataType":"string"}]'));

    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[{"TableName":"Rpr.RepairRequestPart","ColumnName":"Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequest","ColumnName":"IdNumber","DisplayName":"شماره شناسنامه","Alliance":"t2_IdNumber","Address":"[t2].[IdNumber]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"FIN.DL","ColumnName":"Code","DisplayName":"تفضیل","Alliance":"t1_ProjectCode","Address":"COALESCE([t4].[Code], [t5].[Code])","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestPart","ColumnName":"RahkaranRequestNo","DisplayName":"شماره درخواست","Alliance":"t1_RahkaranRequestNo","Address":"[t1].[RahkaranRequestNo]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"Code","DisplayName":"کد کالا","Alliance":"t3_Code","Address":"[t3].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"Name","DisplayName":"عنوان کالا","Alliance":"t3_Name","Address":"[t3].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":260,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestPart","ColumnName":"StockName","DisplayName":"انبار","Alliance":"t1_StockName","Address":"[t1].[StockName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestPart","ColumnName":"UnitName","DisplayName":"واحد","Alliance":"t1_UnitName","Address":"COALESCE([t1].[UnitName], [t6].[Title])","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestPart","ColumnName":"DamagedPart","DisplayName":"داغی","Alliance":"t1_DamagedPart","Address":"[t1].[DamagedPart]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestPart","ColumnName":"ReplacementPartDescription","DisplayName":"توضیحات قطعه داغی","Alliance":"t1_ReplacementPartDescription","Address":"[t1].[ReplacementPartDescription]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestPart","ColumnName":"BuyPrice","DisplayName":"مبلغ خرید","Alliance":"t1_BuyPrice","Address":"[t1].[BuyPrice]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestPart","ColumnName":"SalePrice","DisplayName":"قیمت فروش","Alliance":"t1_SalePrice","Address":"[t1].[SalePrice]","SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestPart","ColumnName":"UsedMount","DisplayName":"مقدار مصرفی","Alliance":"t1_UsedMount","Address":"[t1].[UsedMount]","SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestPart","ColumnName":"ReturnMount","DisplayName":"مقدار برگشتی","Alliance":"t1_ReturnMount","Address":"[t1].[ReturnMount]","SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestPart","ColumnName":"TotalPrice","DisplayName":"مجموع قیمت","Alliance":"t1_TotalPrice","Address":"CASE WHEN [t1].[SalePrice] IS NULL THEN NULL ELSE CAST([t1].[SalePrice] AS DECIMAL(28, 4)) * ([t1].[UsedMount] - [t1].[ReturnMount]) END","SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestPart","ColumnName":"TotalBuyPrice","DisplayName":"مجموع مبلغ خرید","Alliance":"t1_TotalBuyPrice","Address":"CASE WHEN [t1].[BuyPrice] IS NULL THEN NULL ELSE CAST([t1].[BuyPrice] AS DECIMAL(28, 4)) * ([t1].[UsedMount] - [t1].[ReturnMount]) END","SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"Name","DisplayName":"عنوان محصول","Alliance":"t7_Name","Address":"[t7].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":240,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestPart","ColumnName":"Description","DisplayName":"توضیحات","Alliance":"t1_Description","Address":"[t1].[Description]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":240,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestPart","ColumnName":"RahkaranDescription","DisplayName":"توضیحات راهکاران","Alliance":"t1_RahkaranDescription","Address":"[t1].[RahkaranDescription]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":240,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Rpr.RepairRequestPart","ColumnName":"CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":1,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""}]';
    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"},{"dataActionName":"defaultMultiSelect","enable":false,"title":"انتخاب چندتایی پیش‌فرض"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @CustomButtons NVARCHAR(MAX) = N'[{"id":"rr_part_add","title":"افزودن","dataActionName":"rrPartAdd","colorClass":"btn-color-primary","iconClass":"fa fa-plus-circle","requiresSelection":false,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) { ctx.$btn.trigger(''rrPartAction'', [''add'', ctx]); }"},{"id":"rr_part_edit","title":"ویرایش","dataActionName":"rrPartEdit","colorClass":"btn-color-warning","iconClass":"fa fa-pencil","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) { ctx.$btn.trigger(''rrPartAction'', [''edit'', ctx]); }"},{"id":"rr_part_delete","title":"حذف","dataActionName":"rrPartDelete","colorClass":"btn-color-danger","iconClass":"fa fa-trash","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) { ctx.$btn.trigger(''rrPartAction'', [''delete'', ctx]); }"}]';

    IF ISJSON(@ColumnsJson) <> 1 OR ISJSON(@QueryJson) <> 1 OR ISJSON(@ActionOptions) <> 1
       OR ISJSON(@EventScripts) <> 1 OR ISJSON(@CustomButtons) <> 1
        THROW 51631, N'ColumnsJson / QueryJson / ActionOptions / EventScriptsJson / CustomActionButtonsJson معتبر نیست.', 1;

    DECLARE @ProfileId BIGINT;
    SELECT @ProfileId = Id FROM [system].[SavedQuery] WHERE Name = @Name;

    IF @ProfileId IS NOT NULL
    BEGIN
        UPDATE [system].[SavedQuery]
        SET Title = @Title,
            QueryJson = @QueryJson,
            ColumnsJson = @ColumnsJson,
            EntityFullName = @EntityFullName,
            Mode = 1,
            [Type] = 1,
            ActionOptions = @ActionOptions,
            CustomActionButtonsJson = @CustomButtons,
            EventScriptsJson = @EventScripts,
            DiagramJson = N'{}',
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Id = @ProfileId;
        PRINT N'SavedQuery updated: Id=' + CAST(@ProfileId AS NVARCHAR(20));
    END
    ELSE
    BEGIN
        INSERT INTO [system].[SavedQuery]
        (
            Name, Title, QueryJson, ColumnsJson, DiagramJson,
            CustomActionButtonsJson, EventScriptsJson,
            Mode, [Type], EntityFullName, ActionOptions,
            CreatedById, ModifiedById, CreatedByName, ModifiedByName,
            CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime,
            IsActive
        )
        VALUES
        (
            @Name, @Title, @QueryJson, @ColumnsJson, N'{}',
            @CustomButtons, @EventScripts,
            1, 1, @EntityFullName, @ActionOptions,
            1, 1, @SeedUser, @SeedUser,
            @Now, @NowShamsi, @Now, @NowShamsi,
            1
        );
        SET @ProfileId = SCOPE_IDENTITY();
        PRINT N'SavedQuery inserted: Id=' + CAST(@ProfileId AS NVARCHAR(20));
    END

    DELETE ra
    FROM [system].[RoleAccess] ra
    WHERE ra.RowId = @ProfileId
      AND ra.ActionAccessType = 3
      AND ra.EntityName = @EntityPascal;

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT N'dataProfile_' + CAST(@ProfileId AS NVARCHAR(20)), 3, 7, @EntityPascal, @Title, NULL, @ProfileId, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM [system].[Role] r
    WHERE EXISTS (
            SELECT 1 FROM [system].[RoleAccess] page_ra
            WHERE page_ra.RoleId = r.Id
              AND page_ra.Path = N'/panel/rpr/repairrequestpart/listbyparentid'
       );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;

SELECT Id, Name, Title, EntityFullName, Mode, [Type]
FROM [system].[SavedQuery]
WHERE Name = N'AfterSales_RepairRequestPart_ByRequest';

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName,
       JSON_VALUE(j.value, '$.Visible') AS Visible,
       JSON_VALUE(j.value, '$.SortDirection') AS SortDirection
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'AfterSales_RepairRequestPart_ByRequest';

SELECT JSON_VALUE(j.value, '$.title') AS ButtonTitle, JSON_VALUE(j.value, '$.dataActionName') AS DataAction
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.CustomActionButtonsJson) j
WHERE q.Name = N'AfterSales_RepairRequestPart_ByRequest';

SELECT r.Name AS RoleName, ra.Path, ra.ActionAccessType, ra.RowId
FROM [system].[RoleAccess] ra
INNER JOIN [system].[Role] r ON r.Id = ra.RoleId
WHERE ra.ActionAccessType = 3
  AND ra.EntityName = N'Entities.App.Rpr.RepairRequestPart'
  AND ra.RowId = (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'AfterSales_RepairRequestPart_ByRequest')
ORDER BY r.Name;
