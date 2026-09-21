/*
  Seed_AfterSales_ProjectUtilizedMaterial_DataProfiles.sql
  نمایه AfterSales_ProjectUtilizedMaterial_List مطابق گرید HTS صفحه 408
  (اقلام مصرفی در پروژه) + جدول سوابق تغییرات + دسترسی لیست وابسته.
  Encoding: UTF-8 with BOM (sqlcmd -f 65001). Idempotent.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-aftersales-projectutilizedmaterial';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    IF OBJECT_ID(N'Sale.ProjectUtilizedMaterialChange', N'U') IS NULL
    BEGIN
        CREATE TABLE [Sale].[ProjectUtilizedMaterialChange]
        (
            [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [HtsId] BIGINT NOT NULL CONSTRAINT [DF_Sale_ProjectUtilizedMaterialChange_HtsId] DEFAULT (0),
            [ProjectUtilizedMaterialId] BIGINT NULL,
            [Comment] NVARCHAR(2048) NULL,
            [CreatedById] BIGINT NULL,
            [ModifiedById] BIGINT NULL,
            [CreatedByName] NVARCHAR(150) NULL,
            [ModifiedByName] NVARCHAR(150) NULL,
            [ModifiedDateMiladiDateTime] DATETIME2 NULL,
            [ModifiedDateShamsiDateTime] NVARCHAR(30) NULL,
            [CreatedOnMiladiDateTime] DATETIME2 NULL,
            [CreatedOnShamsiDateTime] NVARCHAR(30) NULL,
            [IsActive] INT NULL
        );
        ALTER TABLE [Sale].[ProjectUtilizedMaterialChange]
            ADD CONSTRAINT [FK_Sale_ProjectUtilizedMaterialChange_Parent]
            FOREIGN KEY ([ProjectUtilizedMaterialId]) REFERENCES [Sale].[ProjectUtilizedMaterial]([Id]);
        CREATE UNIQUE INDEX [IX_Sale_ProjectUtilizedMaterialChange_HtsId]
            ON [Sale].[ProjectUtilizedMaterialChange]([HtsId])
            WHERE [HtsId] <> CAST(0 AS bigint);
        PRINT N'  CREATED Sale.ProjectUtilizedMaterialChange';
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sale_ProjectUtilizedMaterial_HtsId' AND object_id = OBJECT_ID(N'Sale.ProjectUtilizedMaterial'))
    BEGIN
        CREATE UNIQUE INDEX [IX_Sale_ProjectUtilizedMaterial_HtsId]
            ON [Sale].[ProjectUtilizedMaterial]([HtsId])
            WHERE [HtsId] <> CAST(0 AS bigint);
        PRINT N'  CREATED IX_Sale_ProjectUtilizedMaterial_HtsId';
    END

    DECLARE @PName NVARCHAR(100) = N'AfterSales_ProjectUtilizedMaterial_List';
    DECLARE @PTitle NVARCHAR(200) = N'اقلام مصرفی در پروژه';
    DECLARE @PEnt NVARCHAR(200) = N'entities.app.sale.projectutilizedmaterial';
    DECLARE @PPascal NVARCHAR(200) = N'Entities.App.Sale.ProjectUtilizedMaterial';
    DECLARE @PSelect NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    [t1].[HtsId] AS [t1_HtsId],
    [t1].[ReplacedPartId] AS [t1_ReplacedPartId],
    CONCAT(ISNULL([t2].[Code], N''''), N'' | '', ISNULL([t2].[Title], N'''')) AS [t2_Dl],
    [t1].[VchTypeId] AS [t1_VchTypeId],
    [t3].[Code] AS [t3_Code],
    [t3].[Name] AS [t3_Name],
    [t1].[Qty] AS [t1_Qty],
    [t1].[Year] AS [t1_Year],
    [t4].[Code] AS [t4_Code],
    [t4].[Name] AS [t4_Name],
    [t1].[ReplacedQty] AS [t1_ReplacedQty],
    [t5].[SparePartsNames] AS [t5_SparePartsNames],
    [t1].[Comment] AS [t1_Comment],
    [t1].[VchNum] AS [t1_VchNum],
    [t1].[VchDateShamsiDate] AS [t1_VchDateShamsiDate]
FROM [Sale].[ProjectUtilizedMaterial] AS [t1]
LEFT JOIN [FIN].[DL] AS [t2] ON [t1].[DLId] = [t2].[Id]
LEFT JOIN [Inv].[Part] AS [t3] ON [t1].[PartId] = [t3].[Id]
LEFT JOIN [Inv].[Part] AS [t4] ON [t1].[ReplacedPartId] = [t4].[Id]
OUTER APPLY (
    SELECT STRING_AGG(CAST([sp].[Name] AS nvarchar(max)), N''، '') AS [SparePartsNames]
    FROM [Inv].[PartSparePart] AS [psp]
    INNER JOIN [Inv].[Part] AS [sp] ON [sp].[Id] = [psp].[SparePartId]
    WHERE [psp].[PartId] = [t1].[PartId]
) AS [t5]';

    DECLARE @PCols NVARCHAR(MAX) = N'[
{"TableName":"Sale.ProjectUtilizedMaterial","ColumnName":"Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProjectUtilizedMaterial","ColumnName":"HtsId","DisplayName":"شناسه HTS","Alliance":"t1_HtsId","Address":"[t1].[HtsId]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProjectUtilizedMaterial","ColumnName":"ReplacedPartId","DisplayName":"شناسه کالای جایگزین","Alliance":"t1_ReplacedPartId","Address":"[t1].[ReplacedPartId]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"FIN.DL","ColumnName":"Title","DisplayName":"تفصیل","Alliance":"t2_Dl","Address":"CONCAT(ISNULL([t2].[Code], N''''), N'' | '', ISNULL([t2].[Title], N''''))","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":250,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProjectUtilizedMaterial","ColumnName":"VchTypeId","DisplayName":"نوع رسید","Alliance":"t1_VchTypeId","Address":"[t1].[VchTypeId]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"Code","DisplayName":"کد کالا","Alliance":"t3_Code","Address":"[t3].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"Name","DisplayName":"نام کالا","Alliance":"t3_Name","Address":"[t3].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":300,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProjectUtilizedMaterial","ColumnName":"Qty","DisplayName":"مقدار","Alliance":"t1_Qty","Address":"[t1].[Qty]","SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProjectUtilizedMaterial","ColumnName":"Year","DisplayName":"سال","Alliance":"t1_Year","Address":"[t1].[Year]","SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"Code","DisplayName":"کد کالای جایگزین","Alliance":"t4_Code","Address":"[t4].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"Name","DisplayName":"عنوان کالای جایگزین","Alliance":"t4_Name","Address":"[t4].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":300,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProjectUtilizedMaterial","ColumnName":"ReplacedQty","DisplayName":"تعداد/مقدار جایگزین","Alliance":"t1_ReplacedQty","Address":"[t1].[ReplacedQty]","SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"Name","DisplayName":"قطعات یدکی","Alliance":"t5_SparePartsNames","Address":"[t5].[SparePartsNames]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":300,"Filterable":true,"Sortable":false,"ClassName":""},
{"TableName":"Sale.ProjectUtilizedMaterial","ColumnName":"Comment","DisplayName":"توضیحات","Alliance":"t1_Comment","Address":"[t1].[Comment]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":300,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProjectUtilizedMaterial","ColumnName":"VchNum","DisplayName":"شماره سند","Alliance":"t1_VchNum","Address":"[t1].[VchNum]","SystemTypeName":"Int","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProjectUtilizedMaterial","ColumnName":"VchDateShamsiDate","DisplayName":"تاریخ سند","Alliance":"t1_VchDateShamsiDate","Address":"[t1].[VchDateShamsiDate]","SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""}]';

    DECLARE @PAct NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":true,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @PBtn NVARCHAR(MAX) = N'[{"id":"as_edit_projectutilizedmaterial","title":"ویرایش","dataActionName":"editRow","colorClass":"btn-color-primary","iconClass":"ki-pencil ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Sale/ProjectUtilizedMaterial/Edit?id='' + id, true, ''ویرایش'');\n}"},{"id":"as_pum_changes","title":"سوابق تغییرات","dataActionName":"materialChanges","colorClass":"btn-color-info","iconClass":"ki-time ki-outline","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var id = ctx && (ctx.primaryKeyValue || (ctx.primaryKeyValues && ctx.primaryKeyValues[0]) || (ctx.selectedRow && (ctx.selectedRow.t1_Id || ctx.selectedRow.id || ctx.selectedRow.Id)));\n    if (!id) { toastr.error(''لطفاً یک ردیف را انتخاب کنید''); return; }\n    appController.addPage(''/Panel/Sale/ProjectUtilizedMaterialChange/ListByParentId?projectUtilizedMaterialId='' + id, true, ''سوابق تغییرات اقلام مصرفی'');\n}"}]';

    DECLARE @OnRowAdded NVARCHAR(MAX) = N'function(ctx) {
    var data = ctx.rowData || {};
    var $cell = ctx.$row.find(''td'').eq(1);
    var replaced = data.t1_ReplacedPartId;
    if (replaced !== null && replaced !== '''' && replaced !== 0 && replaced !== ''0'') {
        $cell.css(''background-color'', ''chocolate'');
    }
}';
    DECLARE @PEvents NVARCHAR(MAX) = (
        SELECT
            CAST(N'' AS nvarchar(max)) AS onSelectedRow,
            @OnRowAdded AS onRowAdded
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );
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
            EventScriptsJson = @PEvents,
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
            (@PName, @PTitle, @PQueryJson, @PCols, N'{}', @PBtn, @PEvents,
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
    WHERE r.Name IN (N'Sale.AfterSales', N'ShowAllMenus');
    PRINT N'  Profile RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    DELETE ra FROM system.RoleAccess ra
    WHERE ra.Path LIKE N'/panel/sale/projectutilizedmaterialchange/%';

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT a.Path, a.AType, a.IType, N'Entities.App.Sale.ProjectUtilizedMaterialChange', NULL, NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM (VALUES
        (N'/panel/sale/projectutilizedmaterialchange/listbyparentid', 1, 1),
        (N'/panel/sale/projectutilizedmaterialchange/getlistbyparentid', 2, 2)
    ) a(Path, AType, IType)
    CROSS JOIN (VALUES (N'ShowAllMenus'), (N'Sale.AfterSales')) x(RoleName)
    INNER JOIN system.Role r ON r.Name = x.RoleName;
    PRINT N'  Change RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    UPDATE system.SystemMenu
    SET Content = REPLACE(Content, N'"text":"اقلام مصرفی پروژه"', N'"text":"اقلام مصرفی در پروژه"'),
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Content LIKE N'%"text":"اقلام مصرفی پروژه"%'
       OR Content LIKE N'%"path":"/panel/sale/projectutilizedmaterial/list"%';
    PRINT N'  Menu text rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_AfterSales_ProjectUtilizedMaterial_DataProfiles ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    PRINT N'=== ERROR rolled back ===';
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT Id, Name, Title FROM system.SavedQuery WHERE Name = N'AfterSales_ProjectUtilizedMaterial_List';
SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName,
       JSON_VALUE(j.value, '$.Visible') AS Visible
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'AfterSales_ProjectUtilizedMaterial_List';
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = N'Sale' AND TABLE_NAME = N'ProjectUtilizedMaterialChange';
