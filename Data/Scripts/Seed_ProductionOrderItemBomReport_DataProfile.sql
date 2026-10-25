/*
  Seed_ProductionOrderItemBomReport_DataProfile.sql
  صفحه و نمایه «گزارش Bom اقلام سفارش ساخت»
  معادل HTS PlanningSystem/ProductionOrderItemBomReport و ویو Vw_Pln_ProductionOrderItemBoms.

  شرط‌ها همان ویو قدیمی: آخرین نسخه قلم (IsLatestVersion) و آخرین نسخه BOM (IsLatest).
  فیلتر تاریخ بالای صفحه عمداً نیست؛ ستون «زمان ویرایش» فیلترپذیر است.

  نام یکتا: POI_BomReport
  موجودیت: Entities.App.Sale.ProductionOrderItemBom
  صفحه: /panel/productionorderitem/bomreport

  Apply:
    sqlcmd -S <server> -d HavayarApp -C -b -I -f 65001 -i Data\Scripts\Seed_ProductionOrderItemBomReport_DataProfile.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-poi-bom-report';

    DECLARE @Name NVARCHAR(200) = N'POI_BomReport';
    DECLARE @Title NVARCHAR(200) = N'گزارش Bom اقلام سفارش ساخت';
    DECLARE @EntityFullName NVARCHAR(200) = N'entities.app.sale.productionorderitembom';
    DECLARE @EntityPascal NVARCHAR(200) = N'Entities.App.Sale.ProductionOrderItemBom';
    DECLARE @PagePath NVARCHAR(200) = N'/panel/productionorderitem/bomreport';
    DECLARE @AnchorPath NVARCHAR(200) = N'/panel/productionorderitem/list';

    DECLARE @CustomQuery NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    COALESCE(CONVERT(nvarchar(50), [t3].[ProductionOrderNumber]), [t3].[Number]) AS [t3_ProductionOrderNumber],
    COALESCE(NULLIF(LTRIM(RTRIM([t5].[FullName])), N''''), [t5].[CompanyName]) AS [t5_FullName],
    [t2].[Serial] AS [t2_Serial],
    [t2].[SerialType] AS [t2_SerialType],
    [t6].[Code] AS [t6_Code],
    [t6].[Name] AS [t6_Name],
    [t7].[Name] AS [t7_Name],
    [t7].[Code] AS [t7_Code],
    [t2].[ProductionStep] AS [t2_ProductionStep],
    [t2].[Status] AS [t2_Status],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],
    [t1].[Amount] AS [t1_Amount],
    [t8].[Title] AS [t8_Title],
    [t1].[Description] AS [t1_Description],
    COALESCE(NULLIF(LTRIM(RTRIM([t9].[NameFa])), N''''), NULLIF(LTRIM(RTRIM([t1].[CreatedByName])), N''''), [t9].[Name]) AS [t9_NameFa]
FROM [Sale].[ProductionOrderItemBom] AS [t1]
INNER JOIN [Sale].[ProductionOrderItem] AS [t2]
    ON [t2].[Id] = [t1].[ProductionOrderItemId]
LEFT JOIN [Sale].[ProductionOrder] AS [t3]
    ON [t3].[Id] = [t2].[ProductionOrderId]
LEFT JOIN [SLS].[Contract] AS [t4]
    ON [t4].[Id] = [t3].[ContractId]
LEFT JOIN [SLS].[Customer] AS [tCust]
    ON [tCust].[Id] = [t4].[CustomerId]
LEFT JOIN [Gnr].[Party] AS [t5]
    ON [t5].[Id] = [tCust].[PartyId]
LEFT JOIN [Inv].[Part] AS [t6]
    ON [t6].[Id] = [t1].[PartId]
LEFT JOIN [Inv].[Part] AS [t7]
    ON [t7].[Id] = [t2].[PartId]
LEFT JOIN [Inv].[PartUnit] AS [t8]
    ON [t8].[Id] = [t7].[UnitId]
LEFT JOIN [system].[User] AS [t9]
    ON [t9].[Id] = [t1].[CreatedById]
WHERE [t2].[IsLatestVersion] = 1
  AND [t1].[IsLatest] = 1';

    DECLARE @QueryJson NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":null,"Parameters":[],"Selects":null}';
    SET @QueryJson = JSON_MODIFY(@QueryJson, '$.CustomQuery', @CustomQuery);

    DECLARE @ColumnsJson NVARCHAR(MAX) = N'[
{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"t1_Id","DisplayName":"شناسه","Alliance":"t1_Id","Address":"[t1].[Id]","SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrder","ColumnName":"t3_ProductionOrderNumber","DisplayName":"شماره سفارش ساخت","Alliance":"t3_ProductionOrderNumber","Address":"COALESCE(CONVERT(nvarchar(50), [t3].[ProductionOrderNumber]), [t3].[Number])","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Gnr.Party","ColumnName":"t5_FullName","DisplayName":"مشتری","Alliance":"t5_FullName","Address":"COALESCE(NULLIF(LTRIM(RTRIM([t5].[FullName])), N''''), [t5].[CompanyName])","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":300,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrderItem","ColumnName":"t2_Serial","DisplayName":"سریال","Alliance":"t2_Serial","Address":"[t2].[Serial]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrderItem","ColumnName":"t2_SerialType","DisplayName":"نوع سریال","Alliance":"t2_SerialType","Address":"[t2].[SerialType]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Sale.Enums.ProductionOrderItemSerialTypeEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"t6_Code","DisplayName":"کد کالا","Alliance":"t6_Code","Address":"[t6].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"t6_Name","DisplayName":"نام کالا","Alliance":"t6_Name","Address":"[t6].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":300,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"t7_Name","DisplayName":"عنوان محصول","Alliance":"t7_Name","Address":"[t7].[Name]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":300,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.Part","ColumnName":"t7_Code","DisplayName":"کد محصول","Alliance":"t7_Code","Address":"[t7].[Code]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrderItem","ColumnName":"t2_ProductionStep","DisplayName":"مرحله ساخت","Alliance":"t2_ProductionStep","Address":"[t2].[ProductionStep]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Sale.Enums.ProductionOrderItemProductionStepEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrderItem","ColumnName":"t2_Status","DisplayName":"وضعیت","Alliance":"t2_Status","Address":"[t2].[Status]","SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Sale.Enums.ProductionOrderItemStatusEnum"},"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"t1_CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد","Alliance":"t1_CreatedOnShamsiDateTime","Address":"[t1].[CreatedOnShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":1,"SortOrder":0,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"t1_ModifiedDateShamsiDateTime","DisplayName":"زمان ویرایش","Alliance":"t1_ModifiedDateShamsiDateTime","Address":"[t1].[ModifiedDateShamsiDateTime]","SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"t1_Amount","DisplayName":"تعداد/مقدار","Alliance":"t1_Amount","Address":"[t1].[Amount]","SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Inv.PartUnit","ColumnName":"t8_Title","DisplayName":"واحد","Alliance":"t8_Title","Address":"[t8].[Title]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"Sale.ProductionOrderItemBom","ColumnName":"t1_Description","DisplayName":"کامنت","Alliance":"t1_Description","Address":"[t1].[Description]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":300,"Filterable":true,"Sortable":true,"ClassName":""},
{"TableName":"system.User","ColumnName":"t9_NameFa","DisplayName":"کاربر ایجادکننده","Alliance":"t9_NameFa","Address":"COALESCE(NULLIF(LTRIM(RTRIM([t9].[NameFa])), N''''), NULLIF(LTRIM(RTRIM([t1].[CreatedByName])), N''''), [t9].[Name])","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":180,"Filterable":true,"Sortable":true,"ClassName":""}
]';

    DECLARE @ActionOptions NVARCHAR(MAX) = N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"}]';
    DECLARE @EventScripts NVARCHAR(MAX) = N'{"onSelectedRow":"","onRowAdded":""}';
    DECLARE @CustomButtons NVARCHAR(MAX) = N'[]';

    IF ISJSON(@ColumnsJson) <> 1 OR ISJSON(@QueryJson) <> 1 OR ISJSON(@ActionOptions) <> 1 OR ISJSON(@EventScripts) <> 1
        THROW 51620, N'ColumnsJson / QueryJson / ActionOptions / EventScriptsJson معتبر نیست.', 1;

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
        N'تکمیل اطلاعات bom اقلام سفارش ساخت صنایع',
        N'ShowAllMenus'
    );

    INSERT INTO [system].[RoleAccess]
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT @PagePath, 1, 1, @EntityPascal, @Title, NULL, NULL, r.Id,
           1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM [system].[Role] r
    WHERE r.Name IN (
        N'Sale.ProductionOrderItemBom',
        N'Sale.ProductionOrderItemBom.View',
        N'تکمیل اطلاعات bom اقلام سفارش ساخت صنایع',
        N'ShowAllMenus'
    )
      AND NOT EXISTS (
          SELECT 1 FROM [system].[RoleAccess] ra
          WHERE ra.RoleId = r.Id AND ra.Path = @PagePath AND ra.ActionAccessType = 1
      );

    ---------------------------------------------------------------------------
    -- منو: برگ گزارش کنار «اقلام سفارش ساخت» در همان پوشه‌ای که لیست هست
    ---------------------------------------------------------------------------
    DECLARE @Leaf NVARCHAR(MAX) = N'{"text":"گزارش Bom اقلام سفارش ساخت","icon":"ki-element-11 ki-outline","iconColor":"#3699ff","path":"/panel/productionorderitem/bomreport","a_attr":{"href":"/panel/productionorderitem/bomreport"},"data":{"iconColor":"#3699ff","path":"/panel/productionorderitem/bomreport"},"children":[]}';
    IF ISJSON(@Leaf) <> 1
        THROW 51621, N'JSON برگ منو معتبر نیست.', 1;

    DECLARE @MenuName NVARCHAR(200);
    DECLARE @Content NVARCHAR(MAX);
    DECLARE @i INT;
    DECLARE @j INT;
    DECLARE @k INT;
    DECLARE @RootCount INT;
    DECLARE @ChildCount INT;
    DECLARE @GrandCount INT;
    DECLARE @ChildJson NVARCHAR(MAX);
    DECLARE @GrandJson NVARCHAR(MAX);
    DECLARE @GreatJson NVARCHAR(MAX);
    DECLARE @BestPath NVARCHAR(400);
    DECLARE @BestDepth INT;
    DECLARE @NodePath NVARCHAR(400);
    DECLARE @p1 NVARCHAR(200);
    DECLARE @p2 NVARCHAR(300);
    DECLARE @p3 NVARCHAR(400);
    DECLARE @Arr NVARCHAR(MAX);
    DECLARE @Rebuilt NVARCHAR(MAX);
    DECLARE @n INT;
    DECLARE @x INT;
    DECLARE @comma BIT;

    DECLARE menuCur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name
        FROM [system].[SystemMenu]
        WHERE CHARINDEX(@AnchorPath, ISNULL(Content, N'')) > 0;

    OPEN menuCur;
    FETCH NEXT FROM menuCur INTO @MenuName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @Content = Content FROM [system].[SystemMenu] WHERE Name = @MenuName;

        IF CHARINDEX(@PagePath, @Content) > 0
        BEGIN
            PRINT N'  menu already has report: ' + @MenuName;
        END
        ELSE IF ISJSON(@Content) <> 1
        BEGIN
            PRINT N'  WARN menu JSON invalid: ' + @MenuName;
        END
        ELSE
        BEGIN
            SET @BestPath = NULL;
            SET @BestDepth = -1;
            SET @RootCount = (SELECT COUNT(*) FROM OPENJSON(@Content));
            SET @i = 0;

            WHILE @i < @RootCount
            BEGIN
                SET @NodePath = JSON_VALUE(@Content, N'$[' + CAST(@i AS NVARCHAR(10)) + N'].path');
                IF @NodePath = @AnchorPath AND @BestDepth < 0
                BEGIN
                    SET @BestPath = N'$';
                    SET @BestDepth = 0;
                END

                SET @p1 = N'$[' + CAST(@i AS NVARCHAR(10)) + N'].children';
                SET @ChildJson = JSON_QUERY(@Content, @p1);
                SET @ChildCount = ISNULL((SELECT COUNT(*) FROM OPENJSON(@ChildJson)), 0);
                SET @j = 0;
                WHILE @j < @ChildCount
                BEGIN
                    IF JSON_VALUE(@ChildJson, N'$[' + CAST(@j AS NVARCHAR(10)) + N'].path') = @AnchorPath AND @BestDepth < 1
                    BEGIN
                        SET @BestPath = @p1;
                        SET @BestDepth = 1;
                    END

                    SET @p2 = @p1 + N'[' + CAST(@j AS NVARCHAR(10)) + N'].children';
                    SET @GrandJson = JSON_QUERY(@Content, @p2);
                    SET @GrandCount = ISNULL((SELECT COUNT(*) FROM OPENJSON(@GrandJson)), 0);
                    SET @k = 0;
                    WHILE @k < @GrandCount
                    BEGIN
                        IF JSON_VALUE(@GrandJson, N'$[' + CAST(@k AS NVARCHAR(10)) + N'].path') = @AnchorPath AND @BestDepth < 2
                        BEGIN
                            SET @BestPath = @p2;
                            SET @BestDepth = 2;
                        END

                        SET @p3 = @p2 + N'[' + CAST(@k AS NVARCHAR(10)) + N'].children';
                        SET @GreatJson = JSON_QUERY(@Content, @p3);
                        IF EXISTS (
                            SELECT 1 FROM OPENJSON(@GreatJson)
                            WHERE JSON_VALUE([value], '$.path') = @AnchorPath
                        ) AND @BestDepth < 3
                        BEGIN
                            SET @BestPath = @p3;
                            SET @BestDepth = 3;
                        END

                        SET @k = @k + 1;
                    END
                    SET @j = @j + 1;
                END
                SET @i = @i + 1;
            END

            IF @BestPath IS NULL
                PRINT N'  WARN menu not placed: ' + @MenuName;
            ELSE
            BEGIN
                SET @Arr = CASE WHEN @BestPath = N'$' THEN @Content ELSE JSON_QUERY(@Content, @BestPath) END;
                SET @Rebuilt = N'[';
                SET @n = ISNULL((SELECT COUNT(*) FROM OPENJSON(@Arr)), 0);
                SET @x = 0;
                SET @comma = 0;
                WHILE @x < @n
                BEGIN
                    IF @comma = 1 SET @Rebuilt += N',';
                    SET @comma = 1;
                    SET @Rebuilt += JSON_QUERY(@Arr, N'$[' + CAST(@x AS NVARCHAR(10)) + N']');
                    IF JSON_VALUE(@Arr, N'$[' + CAST(@x AS NVARCHAR(10)) + N'].path') = @AnchorPath
                        SET @Rebuilt += N',' + @Leaf;
                    SET @x += 1;
                END
                SET @Rebuilt += N']';

                IF @BestPath = N'$'
                    SET @Content = @Rebuilt;
                ELSE
                    SET @Content = JSON_MODIFY(@Content, @BestPath, JSON_QUERY(@Rebuilt));

                IF @Content IS NULL OR ISJSON(@Content) <> 1
                    THROW 51622, N'Menu JSON after insert is invalid.', 1;

                UPDATE [system].[SystemMenu]
                SET Content = @Content,
                    ModifiedById = 1,
                    ModifiedByName = @SeedUser,
                    ModifiedDateMiladiDateTime = @Now,
                    ModifiedDateShamsiDateTime = @NowShamsi
                WHERE Name = @MenuName;

                PRINT N'  menu added: ' + @MenuName + N' at ' + @BestPath;
            END
        END

        FETCH NEXT FROM menuCur INTO @MenuName;
    END
    CLOSE menuCur;
    DEALLOCATE menuCur;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF CURSOR_STATUS('local', 'menuCur') >= 0
    BEGIN
        CLOSE menuCur;
        DEALLOCATE menuCur;
    END
    ELSE IF CURSOR_STATUS('local', 'menuCur') = -1
        DEALLOCATE menuCur;
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@Err, 16, 1);
END CATCH;

SELECT Id, Name, Title, EntityFullName, Mode, [Type]
FROM [system].[SavedQuery]
WHERE Name = N'POI_BomReport';

SELECT JSON_VALUE(j.value, '$.Alliance') AS Alliance,
       JSON_VALUE(j.value, '$.DisplayName') AS DisplayName,
       JSON_VALUE(j.value, '$.Visible') AS Visible,
       JSON_VALUE(j.value, '$.SortDirection') AS SortDirection
FROM [system].[SavedQuery] q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name = N'POI_BomReport';

SELECT r.Name AS RoleName, ra.Path, ra.ActionAccessType, ra.RowId
FROM [system].[RoleAccess] ra
INNER JOIN [system].[Role] r ON r.Id = ra.RoleId
WHERE ra.Path = N'/panel/productionorderitem/bomreport'
   OR (ra.ActionAccessType = 3 AND ra.EntityName = N'Entities.App.Sale.ProductionOrderItemBom'
       AND ra.RowId = (SELECT Id FROM [system].[SavedQuery] WHERE Name = N'POI_BomReport'));

SELECT Name
FROM [system].[SystemMenu]
WHERE CHARINDEX(N'/panel/productionorderitem/bomreport', Content) > 0;
