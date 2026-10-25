/*
================================================================================
Seed_ProductionOrderItem_AwaitingBom_DataProfile.sql
================================================================================
نمایه «منتظر افزودن BOM» روی اقلام سفارش ساخت.

شرط عین دکمه‌ای که در HTS صفحه BOM را باز می‌کرد:
  حذف‌نشده، آخرین نسخه، وضعیت خرید ۱۹۰۴ (کارتابل صنایع داخلی) یا ۱۹۰۵ (خارجی).

ستون‌ها، دیاگرام و اسکریپت ردیف از POI_All_Active کپی می‌شود.
دکمه‌های جدید/ویرایش/حذف قلم خاموش است؛ دکمه «Bom محصول» همان BomListBy را باز می‌کند.
دسترسی نمایه و باز کردن لیست برای نقش Sale.ProductionOrderItemBom
(گروه کارشناسان Bom اقلام سفارش ساخت، معادل گروه ۴۵۱ سیستم قبلی).

Idempotent. UTF-8 with BOM.
Apply:
  sqlcmd -S 172.20.40.42 -d HavayarApp -C -b -I -f 65001 -i thisfile.sql
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-poi-awaiting-bom';
    DECLARE @Entity NVARCHAR(200) = N'Entities.App.Sale.ProductionOrderItem';
    DECLARE @EntityLower NVARCHAR(200) = N'entities.app.sale.productionorderitem';
    DECLARE @ProfileName NVARCHAR(100) = N'POI_AwaitingBom';
    DECLARE @ProfileTitle NVARCHAR(200) = N'منتظر افزودن BOM';
    DECLARE @RoleName NVARCHAR(200) = N'Sale.ProductionOrderItemBom';

    DECLARE @BaseCq NVARCHAR(MAX);
    SELECT TOP 1 @BaseCq = j.CustomQuery
    FROM system.SavedQuery q
    CROSS APPLY OPENJSON(q.QueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') j
    WHERE q.Name = N'POI_All_Active'
      AND j.CustomQuery IS NOT NULL;

    IF @BaseCq IS NULL OR LEN(@BaseCq) < 50
        THROW 52011, N'CustomQuery نمایه POI_All_Active یافت نشد.', 1;

    IF CHARINDEX(N'WHERE', @BaseCq) > 0
        SET @BaseCq = LEFT(@BaseCq, CHARINDEX(N'WHERE', @BaseCq) - 1);
    SET @BaseCq = RTRIM(@BaseCq);

    DECLARE @Cols NVARCHAR(MAX), @Diag NVARCHAR(MAX), @Ev NVARCHAR(MAX);
    SELECT TOP 1
        @Cols = ColumnsJson,
        @Diag = DiagramJson,
        @Ev = EventScriptsJson
    FROM system.SavedQuery
    WHERE Name = N'POI_All_Active';

    IF @Cols IS NULL OR LEN(@Cols) < 20
        THROW 52012, N'ColumnsJson نمایه POI_All_Active یافت نشد.', 1;

    -- ستون اول: دارای BOM = بله اگر حداقل یک رکورد در Sale.ProductionOrderItemBom باشد، وگرنه خیر
    DECLARE @HasBomExpr NVARCHAR(MAX) = N'CASE WHEN EXISTS (SELECT 1 FROM [Sale].[ProductionOrderItemBom] AS [bom] WHERE [bom].[ProductionOrderItemId] = [t1].[Id]) THEN N''بله'' ELSE N''خیر'' END';
    IF CHARINDEX(N'AS [tHasBom]', @BaseCq) = 0
    BEGIN
        DECLARE @IdToken NVARCHAR(80) = N'[t1].[Id] AS [t1_Id]';
        DECLARE @IdPos INT = CHARINDEX(@IdToken, @BaseCq);
        IF @IdPos = 0
            THROW 52014, N'جای درج ستون دارای BOM در SELECT پیدا نشد.', 1;
        SET @BaseCq = STUFF(@BaseCq, @IdPos, 0, @HasBomExpr + N' AS [tHasBom], ');
    END

    IF CHARINDEX(N'"Alliance":"tHasBom"', @Cols) = 0
    BEGIN
        DECLARE @HasBomCol NVARCHAR(MAX) = N'{"TableName":"Sale.ProductionOrderItem","ColumnName":"tHasBom","DisplayName":"دارای BOM","Alliance":"tHasBom","Address":"'
            + REPLACE(@HasBomExpr, N'"', N'\"')
            + N'","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""}';
        IF LEFT(LTRIM(@Cols), 1) <> N'['
            THROW 52015, N'ColumnsJson آرایه نیست.', 1;
        SET @Cols = STUFF(LTRIM(@Cols), 2, 0, @HasBomCol + N',');
    END

    IF @Ev IS NULL SET @Ev = N'{"onSelectedRow":"","onRowAdded":""}';
    IF @Diag IS NULL SET @Diag = N'{}';

    DECLARE @WhereSql NVARCHAR(MAX) = N'
WHERE [t1].[IsDeleted] = 0
  AND [t1].[IsLatestVersion] = 1
  AND [t1].[CheckStatus] IN (1904, 1905)';

    DECLARE @ActView NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"},{"dataActionName":"defaultMultiSelect","enable":false,"title":"انتخاب چندتایی پیش‌فرض"}]';

    DECLARE @BomBtn NVARCHAR(MAX) = N'[{"id":"poi_open_bom","title":"Bom محصول","dataActionName":"viewBom","colorClass":"btn-color-primary","iconClass":"fa fa-sitemap","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx){var id=ctx&&(ctx.primaryKeyValue||(ctx.selectedRow&&(ctx.selectedRow.t1_Id||ctx.selectedRow.Id)));if(!id){toastr.error(''یک ردیف را انتخاب کنید'');return;}appController.addPage(''/Panel/ProductionOrderItem/BomListBy?productionOrderItemId=''+id,true,''Bom محصول'');}"}]';

    DECLARE @NewQj NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @NewQj = JSON_MODIFY(@NewQj, '$.CustomQuery', @BaseCq + @WhereSql);

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @ProfileName)
    BEGIN
        UPDATE system.SavedQuery
        SET Title = @ProfileTitle,
            QueryJson = @NewQj,
            ColumnsJson = @Cols,
            DiagramJson = @Diag,
            ActionOptions = @ActView,
            CustomActionButtonsJson = @BomBtn,
            EventScriptsJson = @Ev,
            EntityFullName = @EntityLower,
            Type = 1,
            Mode = 1,
            IsActive = 1,
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = @ProfileName;
        PRINT N'  UPDATED ' + @ProfileName;
    END
    ELSE
    BEGIN
        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime,
             IsActive, EntityFullName, Type, Mode, ActionOptions)
        VALUES
            (@ProfileName, @ProfileTitle, @NewQj, @Cols, @Diag, @BomBtn, @Ev,
             1, 1, @SeedUser, @SeedUser,
             @Now, @Now, @NowShamsi, @NowShamsi,
             1, @EntityLower, 1, 1, @ActView);
        PRINT N'  CREATED ' + @ProfileName;
    END

    DECLARE @QueryId BIGINT = (SELECT Id FROM system.SavedQuery WHERE Name = @ProfileName);
    DECLARE @RoleId BIGINT = (SELECT TOP 1 Id FROM system.Role WHERE Name = @RoleName AND IsActive = 1);
    IF @RoleId IS NULL
        THROW 52013, N'نقش Sale.ProductionOrderItemBom یافت نشد.', 1;

    DELETE ra
    FROM system.RoleAccess ra
    WHERE ra.ActionAccessType = 3
      AND ra.RowId = @QueryId;
    PRINT N'  Cleared profile RoleAccess: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName,
         EntityId, RowId, RoleId, CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    VALUES
        (N'dataProfile_' + CAST(@QueryId AS NVARCHAR(20)),
         3, 7, @Entity, @ProfileTitle,
         NULL, @QueryId, @RoleId, 1, 1, @SeedUser, @SeedUser,
         @Now, @NowShamsi, @Now, @NowShamsi, 1);
    PRINT N'  Granted profile to ' + @RoleName;

    DECLARE @PageRows TABLE (Path NVARCHAR(400), AccessType INT, ItemType INT);
    INSERT INTO @PageRows (Path, AccessType, ItemType) VALUES
        (N'/panel/productionorderitem/list', 1, 1),
        (N'/panel/productionorderitem/fetchdata', 2, 2),
        (N'/panel/productionorderitem/exporttoexcel', 2, 0);

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT x.Path, x.AccessType, x.ItemType, @Entity, N'', NULL, NULL, @RoleId,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM @PageRows x
    WHERE NOT EXISTS (
        SELECT 1 FROM system.RoleAccess ra
        WHERE ra.RoleId = @RoleId AND ra.Path = x.Path AND ra.ActionAccessType = x.AccessType);
    PRINT N'  Inserted list/fetch/export RoleAccess: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_ProductionOrderItem_AwaitingBom_DataProfile ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH

SELECT q.Id, q.Name, q.Title, q.IsActive,
       RIGHT(j.CustomQuery, 180) AS WhereTail
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.QueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') j
WHERE q.Name = N'POI_AwaitingBom';

SELECT r.Name AS RoleName, r.Title AS RoleTitle, ra.Path, ra.ActionAccessType
FROM system.RoleAccess ra
JOIN system.Role r ON r.Id = ra.RoleId
JOIN system.SavedQuery q ON q.Id = ra.RowId
WHERE q.Name = N'POI_AwaitingBom' AND ra.ActionAccessType = 3;

SELECT JSON_VALUE(c.value, '$.Alliance') AS Alliance,
       JSON_VALUE(c.value, '$.DisplayName') AS DisplayName
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) c
WHERE q.Name = N'POI_AwaitingBom'
ORDER BY TRY_CAST(c.[key] AS INT);
