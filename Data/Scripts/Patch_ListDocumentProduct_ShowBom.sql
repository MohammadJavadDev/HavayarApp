/*
  Patch_ListDocumentProduct_ShowBom.sql
  دکمه «نمایش Bom» روی نمایه‌های محصول مدارک محصولات
  و فیلتر @ParentProductId روی نمایه‌های BOM.
  نمایه‌های BOM در صفحه اصلی بدون پارامتر همان لیست کامل را برمی‌گردانند.
  UTF-8 BOM. Idempotent. سید کامل را دوباره اجرا نکنید.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-listdocumentproduct-showbom';

    DECLARE @ShowBomBtn NVARCHAR(MAX) = N'{"id":"id_ldp_showbom","title":"نمایش Bom","dataActionName":"showProductBom","colorClass":"btn-color-info","iconClass":"fa fa-sitemap","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    var row = ctx.selectedRow;\n    if (!row || !row.partId) {\n        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');\n        return;\n    }\n    var code = row.partCode || '''';\n    var name = row.partName || '''';\n    var title = ''BOM '' + (code || row.partId);\n    appController.addPage(''/Panel/Edms/Document/DocumentProducts/Bom?productId='' + encodeURIComponent(row.partId) + ''&partCode='' + encodeURIComponent(code) + ''&partName='' + encodeURIComponent(name), true, title);\n}"}';
    DECLARE @ParentParam NVARCHAR(MAX) = N'{"Id":"ldpParentProductId","Name":"ParentProductId","DisplayName":"شناسه محصول","DefaultValue":"","DataType":"long"}';

    IF ISJSON(@ShowBomBtn) <> 1 OR ISJSON(@ParentParam) <> 1
        THROW 50021, 'Show Bom JSON is invalid', 1;

    PRINT N'=== [1] Product profile button ===';
    UPDATE system.SavedQuery
    SET CustomActionButtonsJson = JSON_MODIFY(CustomActionButtonsJson, 'append $', JSON_QUERY(@ShowBomBtn)),
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name IN (
            N'ListDocumentProduct_NoPrice',
            N'vw_ListDocumentProductPricenew',
            N'vw_ListDocumentProductPriceSale',
            N'vw_ListDocumentProductPrice'
        )
      AND ISNULL(CustomActionButtonsJson, N'') NOT LIKE N'%showProductBom%'
      AND ISJSON(CustomActionButtonsJson) = 1;
    PRINT N'  Buttons appended: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== [2] BOM ParentProductId filter ===';
    IF OBJECT_ID('tempdb..#LdpBomPatch') IS NOT NULL DROP TABLE #LdpBomPatch;
    SELECT q.Name, j.CustomQuery
    INTO #LdpBomPatch
    FROM system.SavedQuery AS q
    CROSS APPLY OPENJSON(q.QueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') AS j
    WHERE q.Name IN (
            N'ListDocumentProduct_Bom_NoPrice',
            N'ListDocumentProduct_Bom_Buy',
            N'ListDocumentProduct_Bom_Sale',
            N'vw_ListDocumentProductPriceBom'
        );

    DECLARE @BomName NVARCHAR(200);
    DECLARE @BomQuery NVARCHAR(MAX);
    DECLARE @MarkerPos INT;
    DECLARE @MainSql NVARCHAR(MAX);

    WHILE EXISTS (SELECT 1 FROM #LdpBomPatch)
    BEGIN
        SELECT TOP (1) @BomName = Name, @BomQuery = CustomQuery FROM #LdpBomPatch;

        IF CHARINDEX(N'@ParentProductId', @BomQuery) = 0
        BEGIN
            SET @MarkerPos = CHARINDEX(N'--!--mainsection', @BomQuery);
            SET @MainSql = CASE
                WHEN @MarkerPos > 0 THEN SUBSTRING(@BomQuery, @MarkerPos + LEN(N'--!--mainsection'), LEN(@BomQuery))
                ELSE @BomQuery
            END;
            IF CHARINDEX(N'WHERE', @MainSql) > 0
                THROW 50022, 'BOM main select already has WHERE', 1;

            SET @BomQuery = @BomQuery + NCHAR(10)
                + N'WHERE (@ParentProductId IS NULL OR bq.ProductId = TRY_CAST(@ParentProductId AS BIGINT))';

            UPDATE system.SavedQuery
            SET QueryJson = JSON_MODIFY(QueryJson, '$.CustomQuery', @BomQuery),
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi
            WHERE Name = @BomName;
        END

        UPDATE system.SavedQuery
        SET QueryJson = JSON_MODIFY(QueryJson, 'append $.Parameters', JSON_QUERY(@ParentParam)),
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = @BomName
          AND ISNULL(JSON_QUERY(QueryJson, '$.Parameters'), N'') NOT LIKE N'%"Name":"ParentProductId"%';

        DELETE FROM #LdpBomPatch WHERE Name = @BomName;
    END

    IF (SELECT COUNT(*) FROM system.SavedQuery
        WHERE Name IN (
            N'ListDocumentProduct_NoPrice',
            N'vw_ListDocumentProductPricenew',
            N'vw_ListDocumentProductPriceSale',
            N'vw_ListDocumentProductPrice'
        )
        AND CustomActionButtonsJson LIKE N'%showProductBom%') <> 4
        THROW 50023, 'Show Bom button missing on a product profile', 1;

    IF EXISTS (
        SELECT 1
        FROM system.SavedQuery AS q
        CROSS APPLY OPENJSON(q.CustomActionButtonsJson) AS btn
        WHERE q.Name IN (
                N'ListDocumentProduct_NoPrice',
                N'vw_ListDocumentProductPricenew',
                N'vw_ListDocumentProductPriceSale',
                N'vw_ListDocumentProductPrice'
            )
          AND JSON_VALUE(btn.value, '$.dataActionName') = N'showProductBom'
          AND JSON_VALUE(btn.value, '$.title') <> N'نمایش Bom'
    )
        THROW 50024, 'Show Bom button title encoding is wrong', 1;

    IF EXISTS (
        SELECT 1
        FROM system.SavedQuery AS q
        WHERE q.Name IN (
                N'ListDocumentProduct_Bom_NoPrice',
                N'ListDocumentProduct_Bom_Buy',
                N'ListDocumentProduct_Bom_Sale',
                N'vw_ListDocumentProductPriceBom'
            )
          AND q.CustomActionButtonsJson LIKE N'%showProductBom%'
    )
        THROW 50025, 'Show Bom button must not be on a BOM profile', 1;

    IF EXISTS (
        SELECT 1
        FROM system.SavedQuery AS q
        CROSS APPLY OPENJSON(q.QueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') AS j
        WHERE q.Name IN (
                N'ListDocumentProduct_Bom_NoPrice',
                N'ListDocumentProduct_Bom_Buy',
                N'ListDocumentProduct_Bom_Sale',
                N'vw_ListDocumentProductPriceBom'
            )
          AND (
                j.CustomQuery NOT LIKE N'%WHERE (@ParentProductId IS NULL OR bq.ProductId = TRY_CAST(@ParentProductId AS BIGINT))%'
             OR ISNULL(JSON_QUERY(q.QueryJson, '$.Parameters'), N'') NOT LIKE N'%"Name":"ParentProductId"%'
             OR CHARINDEX(N'DECLARE @ParentProductId', j.CustomQuery) > 0
          )
    )
        THROW 50026, 'ParentProductId filter was not applied', 1;

    IF EXISTS (
        SELECT 1
        FROM system.SavedQuery AS q
        CROSS APPLY OPENJSON(q.QueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') AS j
        WHERE q.Name IN (
                N'ListDocumentProduct_Bom_Buy',
                N'ListDocumentProduct_Bom_Sale',
                N'vw_ListDocumentProductPriceBom'
            )
          AND j.CustomQuery NOT LIKE N'%LastBuyDate%'
    )
        THROW 50027, 'LastBuyDate was lost while patching BOM queries', 1;

    COMMIT TRANSACTION;
    PRINT N'=== DONE Patch_ListDocumentProduct_ShowBom ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT q.Name,
       CASE WHEN q.CustomActionButtonsJson LIKE N'%showProductBom%' THEN 1 ELSE 0 END AS HasShowBom,
       JSON_VALUE(btn.value, '$.title') AS ShowBomTitle,
       CASE WHEN j.CustomQuery LIKE N'%@ParentProductId%' THEN 1 ELSE 0 END AS HasFilter
FROM system.SavedQuery AS q
CROSS APPLY OPENJSON(q.QueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') AS j
OUTER APPLY OPENJSON(q.CustomActionButtonsJson) AS btn
WHERE q.Name IN (
        N'ListDocumentProduct_NoPrice',
        N'vw_ListDocumentProductPricenew',
        N'vw_ListDocumentProductPriceSale',
        N'vw_ListDocumentProductPrice',
        N'ListDocumentProduct_Bom_NoPrice',
        N'ListDocumentProduct_Bom_Buy',
        N'ListDocumentProduct_Bom_Sale',
        N'vw_ListDocumentProductPriceBom'
    )
  AND (
        JSON_VALUE(btn.value, '$.dataActionName') = N'showProductBom'
        OR btn.[key] IS NULL
        OR q.Name LIKE N'%Bom%'
        OR q.Name = N'vw_ListDocumentProductPriceBom'
      )
ORDER BY q.Name;
