/*
  Seed_ProductionOrderItemBomByItem_DataProfile.sql
  نمایه «BOM قلم سفارش ساخت» (POI_BomByItem) برای جدول صفحهٔ BOM قلم سفارش ساخت.

  صفحه: /Panel/ProductionOrderItem/BomListBy?productionOrderItemId=<id>
  موجودیت: Entities.App.Sale.ProductionOrderItemBom

  - فقط آخرین نسخهٔ هر کالا (جدول اصلی)؛ تاریخچه از دکمهٔ «تاریخچه» و ProductionOrderItemBomHistory.
  - @ProductionOrderItemId از data-query-params صفحه می‌آید (DECLARE نمی‌شود).
  - مجوز ردیف در SQL: admin، نقش‌های BOM، یا ذی‌نفع همان سفارش (فروش/مدیر فروش/جایگزین/مدیر پروژه).
  - RoleAccess نمایه: نقش‌های BOM + هر نقشی که صفحهٔ ویرایش قلم (/panel/productionorderitem/edit) را دارد؛
    چون ذی‌نفع بدون نقش BOM فقط از همین نقش‌ها به صفحه می‌رسد. خود داده را شرط SQL بالا محافظت می‌کند.

  تولید: python _tools/DataProfile/gen_poi_bombyitem_seed.py   (utf-8-sig)
  Apply: sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i Data\Scripts\Seed_ProductionOrderItemBomByItem_DataProfile.sql
  بعد از اجرا WebApp را ری‌استارت کنید (کش Redis نمایه‌ها).
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-poi-bom-by-item';

    DECLARE @Name NVARCHAR(200) = N'POI_BomByItem';
    DECLARE @Title NVARCHAR(200) = N'BOM قلم سفارش ساخت';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.sale.productionorderitembom';
    DECLARE @EntityPascal NVARCHAR(200) = N'Entities.App.Sale.ProductionOrderItemBom';

    DECLARE @CustomQuery NVARCHAR(MAX) = N'DECLARE @CuId BIGINT = TRY_CAST(CONVERT(NVARCHAR(30), @CurrentUserId) AS BIGINT);
DECLARE @ItemId BIGINT = TRY_CAST(CONVERT(NVARCHAR(30), @ProductionOrderItemId) AS BIGINT);
DECLARE @CuRoles NVARCHAR(MAX);
SELECT @CuRoles = Roles FROM [system].[User] WHERE Id = @CuId;
DECLARE @CanRead BIT = CASE
    WHEN @ItemId IS NULL OR @CuId IS NULL THEN 0
    WHEN EXISTS (
        SELECT 1 FROM OPENJSON(@CuRoles)
        WHERE [value] IN (N''admin'', N''Sale.ProductionOrderItemBom'', N''Sale.ProductionOrderItemBom.View'', N''تکمیل اطلاعات bom اقلام سفارش ساخت صنایع'')
    ) THEN 1
    WHEN EXISTS (
        SELECT 1
        FROM [Sale].[ProductionOrderItem] AS [i]
        INNER JOIN [Sale].[ProductionOrder] AS [po] ON [po].[Id] = [i].[ProductionOrderId]
        WHERE [i].[Id] = @ItemId
          AND @CuId IN ([po].[SalesExpertId], [po].[SalesManagerId], [po].[AlternativeExpertId], [po].[ProjectManagerId])
    ) THEN 1
    ELSE 0 END;
--!--mainsection
SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[PartId] AS [t1_PartId],
    [t1].[Revision] AS [t1_Revision],
    [t2].[Code] AS [t2_Code],
    [t2].[Name] AS [t2_Name],
    [t1].[Amount] AS [t1_Amount],
    [t3].[Title] AS [t3_Title],
    [t1].[ProductionStep] AS [t1_ProductionStep],
    [t1].[NumberSupplied] AS [t1_NumberSupplied],
    [t1].[Status] AS [t1_Status],
    [t1].[Description] AS [t1_Description],
    [t1].[SaleUnitDetails] AS [t1_SaleUnitDetails],
    [t1].[NeedsAVL] AS [t1_NeedsAVL],
    [t1].[NeedsInspection] AS [t1_NeedsInspection],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[CreatedByName] AS [t1_CreatedByName],
    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],
    [t1].[ModifiedByName] AS [t1_ModifiedByName]
FROM [Sale].[ProductionOrderItemBom] AS [t1]
INNER JOIN [Inv].[Part] AS [t2]
    ON [t2].[Id] = [t1].[PartId]
LEFT JOIN [Inv].[PartUnit] AS [t3]
    ON [t3].[Id] = [t2].[UnitId]
WHERE @CanRead = 1
  AND [t1].[ProductionOrderItemId] = @ItemId';
    DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.Parameters', JSON_QUERY(N'[{"Id":"productionOrderItemId","Name":"ProductionOrderItemId","DisplayName":"شناسه قلم سفارش ساخت","DefaultValue":"","DataType":"string"}]'));

    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"PartId","DisplayName":"شناسه کالا","Alliance":"t1_PartId","Address":"[t1].[PartId]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"Revision","DisplayName":"نسخه","Alliance":"t1_Revision","Address":"[t1].[Revision]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":70,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"Code","DisplayName":"کد کالا","Alliance":"t2_Code","Address":"[t2].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.Part","ColumnName":"Name","DisplayName":"نام کالا","Alliance":"t2_Name","Address":"[t2].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":300,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"Amount","DisplayName":"تعداد/مقدار","Alliance":"t1_Amount","Address":"[t1].[Amount]","SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Inv.PartUnit","ColumnName":"Title","DisplayName":"واحد","Alliance":"t3_Title","Address":"[t3].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"ProductionStep","DisplayName":"مرحله ساخت","Alliance":"t1_ProductionStep","Address":"[t1].[ProductionStep]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Sale.Enums.ProductionOrderItemBomProductionStepEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"NumberSupplied","DisplayName":"تعداد تامین شده","Alliance":"t1_NumberSupplied","Address":"[t1].[NumberSupplied]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"Status","DisplayName":"وضعیت","Alliance":"t1_Status","Address":"[t1].[Status]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Sale.Enums.ProductionOrderItemBomEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"Description","DisplayName":"توضیحات","Alliance":"t1_Description","Address":"[t1].[Description]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":260,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"SaleUnitDetails","DisplayName":"توضیحات واحد فروش","Alliance":"t1_SaleUnitDetails","Address":"[t1].[SaleUnitDetails]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":260,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"NeedsAVL","DisplayName":"نیاز به AVL","Alliance":"t1_NeedsAVL","Address":"[t1].[NeedsAVL]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"NeedsInspection","DisplayName":"نیاز به بازرسی/مدارک بازرسی","Alliance":"t1_NeedsInspection","Address":"[t1].[NeedsInspection]","SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":170,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"CreatedOnShamsiDateTime","DisplayName":"زمان ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":1,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"CreatedByName","DisplayName":"ایجاد کننده","Alliance":"t1_CreatedByName","Address":"[t1].[CreatedByName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"ModifiedDateShamsiDateTime","DisplayName":"زمان ویرایش","Alliance":"t1_ModifiedDateShamsiDateTime","Address":"[t1].[ModifiedDateShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"ModifiedByName","DisplayName":"ویرایش کننده","Alliance":"t1_ModifiedByName","Address":"[t1].[ModifiedByName]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""}]';
    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"},{"dataActionName":"defaultMultiSelect","enable":false,"title":"انتخاب چندتایی پیش‌فرض"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @CustomButtons NVARCHAR(MAX) = N'[{"id":"poi_bom_consider","title":"ثبت ملاحظات (تعیین تکلیف)","dataActionName":"bomConsider","colorClass":"btn-color-danger","iconClass":"fa fa-clipboard-check","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) { ctx.$btn.trigger(''bomAction'', [''consider'', ctx]); }"},{"id":"poi_bom_add","title":"افزودن","dataActionName":"bomAdd","colorClass":"btn-color-primary","iconClass":"fa fa-plus-circle","requiresSelection":false,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) { ctx.$btn.trigger(''bomAction'', [''add'', ctx]); }"},{"id":"poi_bom_edit","title":"ویرایش","dataActionName":"bomEdit","colorClass":"btn-color-warning","iconClass":"fa fa-pencil","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) { ctx.$btn.trigger(''bomAction'', [''edit'', ctx]); }"},{"id":"poi_bom_delete","title":"حذف","dataActionName":"bomDelete","colorClass":"btn-color-danger","iconClass":"fa fa-trash","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) { ctx.$btn.trigger(''bomAction'', [''delete'', ctx]); }"},{"id":"poi_bom_formula","title":"افزودن BOM از روی فرمول محصول","dataActionName":"bomFormula","colorClass":"btn-color-info","iconClass":"fa fa-sitemap","requiresSelection":false,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) { ctx.$btn.trigger(''bomAction'', [''formula'', ctx]); }"},{"id":"poi_bom_import","title":"افزودن با اکسل","dataActionName":"bomImport","colorClass":"btn-color-success","iconClass":"fa fa-file-arrow-up","requiresSelection":false,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) { ctx.$btn.trigger(''bomAction'', [''import'', ctx]); }"},{"id":"poi_bom_history","title":"تاریخچه","dataActionName":"bomHistory","colorClass":"btn-color-primary","iconClass":"fa fa-history","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) { ctx.$btn.trigger(''bomAction'', [''history'', ctx]); }"}]';

    IF ISJSON(@ColumnsJson) <> 1 OR ISJSON(@QueryJson) <> 1 OR ISJSON(@ActionOptions) <> 1
       OR ISJSON(@EventScripts) <> 1 OR ISJSON(@CustomButtons) <> 1
        THROW 51630, N'ColumnsJson / QueryJson / ActionOptions / EventScriptsJson / CustomActionButtonsJson معتبر نیست.', 1;

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
    WHERE r.Name IN (
        N'Sale.ProductionOrderItemBom',
        N'Sale.ProductionOrderItemBom.View',
        N'تکمیل اطلاعات bom اقلام سفارش ساخت صنایع'
    )
       OR EXISTS (
            SELECT 1 FROM [system].[RoleAccess] edit_ra
            WHERE edit_ra.RoleId = r.Id
              AND edit_ra.Path LIKE N'/panel/productionorderitem/edit%'
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
WHERE Name = N'POI_BomByItem';

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName,
       JSON_VALUE(j.value, '$.Visible') AS Visible,
       JSON_VALUE(j.value, '$.SortDirection') AS SortDirection
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'POI_BomByItem';

SELECT JSON_VALUE(j.value, '$.title') AS ButtonTitle, JSON_VALUE(j.value, '$.dataActionName') AS DataAction
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.CustomActionButtonsJson) j
WHERE q.Name = N'POI_BomByItem';

SELECT r.Name AS RoleName, ra.Path, ra.ActionAccessType, ra.RowId
FROM [system].[RoleAccess] ra
INNER JOIN [system].[Role] r ON r.Id = ra.RoleId
WHERE ra.ActionAccessType = 3
  AND ra.EntityName = N'Entities.App.Sale.ProductionOrderItemBom'
  AND ra.RowId = (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'POI_BomByItem')
ORDER BY r.Name;
