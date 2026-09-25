/*
  Seed_ListDocumentProduct_ProfilesAndAccess.sql
  نمایه‌های مدارک محصولات (unicode listdocumentproduct) هم‌تراز HTS 107/108
  + نقش‌ها و RoleAccess و اعطای کاربر از TMS.TotalSystem
  UTF-8 BOM. Idempotent.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-listdocumentproduct-profiles';
    DECLARE @EntityFullName NVARCHAR(200) = N'listdocumentproduct';
    DECLARE @PagePath NVARCHAR(300) = N'/panel/edms/document/documentproducts/listdocumentproduct';
    DECLARE @PageEntity NVARCHAR(200) = N'Entities.App.Edms.Document';
    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    PRINT N'=== [1] Upsert SavedQuery profiles ===';
    IF OBJECT_ID('tempdb..#LdpProfiles') IS NOT NULL DROP TABLE #LdpProfiles;
    CREATE TABLE #LdpProfiles (
        Name NVARCHAR(100) NOT NULL PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        CustomQuery NVARCHAR(MAX) NOT NULL,
        ColumnsJson NVARCHAR(MAX) NOT NULL,
        ActionOptions NVARCHAR(MAX) NOT NULL,
        CustomButtons NVARCHAR(MAX) NOT NULL,
        SavedQueryId BIGINT NULL
    );

    INSERT INTO #LdpProfiles (Name, Title, CustomQuery, ColumnsJson, ActionOptions, CustomButtons) VALUES
        (N'ListDocumentProduct_NoPrice', N'لیست اطلاعات بدون قیمت', N';WITH LatestPartExtraInfo AS
(
    SELECT
        extraInfo.ProductId,
        extraInfo.Revision,
        extraInfo.InfoType,
        ROW_NUMBER() OVER
        (
            PARTITION BY extraInfo.ProductId
            ORDER BY extraInfo.Revision DESC, extraInfo.Id DESC
        ) AS RowNumber
    FROM Inv.PartExtraInfo AS extraInfo
),
BaseQuery AS
(
    SELECT
        formula.Id,
        formula.ProductId,
        formula.ProductNameGroupId,
        formula.CreatedOnMiladiDateTime,
        formula.CreatedOnShamsiDateTime,
        formula.Comment,
        product.Code AS PartCode,
        product.Name AS PartName,
        product.DesignTypeIsRoutine AS IsRoutine,
        product.[Foreign],
        product.EngineeringRoutine,
        product.DataSheetUsage,
        product.DataSheet,
        product.BrandInDataSheet,
        product.PreciseTool,
        product.SaleRate,
        extraInfo.Revision AS PartExteraInfoRev,
        extraInfo.InfoType AS PartExteraInfoType,
        productGroup.Name AS ProductNameGroupName
    FROM Bom.ProductFormul AS formula
    INNER JOIN Inv.Part AS product ON product.Id = formula.ProductId
    LEFT JOIN LatestPartExtraInfo AS extraInfo
        ON extraInfo.ProductId = product.Id
       AND extraInfo.RowNumber = 1
    LEFT JOIN Bom.ProductGroup AS productGroup
        ON productGroup.Id = formula.ProductNameGroupId
),
DocumentCount AS
(
    SELECT
        document.PartId,
        COUNT(*) AS DocumentCount
    FROM Inv.PartDocument AS document
    WHERE document.Main = 1
    GROUP BY document.PartId
),
BomState AS
(
    SELECT DISTINCT item.ProductFormulId
    FROM Bom.vw_ProductItem AS item
)
SELECT
    base.Id AS ProductFormulId,
    ISNULL(document.DocumentCount, 0) AS DocumentCount,
    CAST(CASE WHEN bom.ProductFormulId IS NULL THEN 0 ELSE 1 END AS BIT) AS HasBom,
    base.PartCode,
    base.PartName,
    base.ProductNameGroupName,
    base.IsRoutine,
    base.CreatedOnShamsiDateTime,
    base.Comment,
    base.ProductNameGroupId,
    base.CreatedOnMiladiDateTime AS CreatedDate,
    base.PartExteraInfoRev,
    base.PartExteraInfoType AS PartExteraInfoTypeName,
    base.PartExteraInfoType,
    base.ProductId AS PartId
FROM BaseQuery AS base
LEFT JOIN BomState AS bom ON bom.ProductFormulId = base.Id
LEFT JOIN DocumentCount AS document ON document.PartId = base.ProductId
', N'[{"TableName":null,"ColumnName":"DocumentCount","DisplayName":"تعداد مدرک","Alliance":"DocumentCount","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"HasBom","DisplayName":"دارای Bom","Alliance":"HasBom","Address":null,"SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartCode","DisplayName":"کد کالا","Alliance":"PartCode","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartName","DisplayName":"نام کالا","Alliance":"PartName","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductNameGroupName","DisplayName":"گروه کالا","Alliance":"ProductNameGroupName","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoTypeName","DisplayName":"نوع دیتا شیت","Alliance":"PartExteraInfoTypeName","Address":null,"SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Inv.Enums.PartExtraInfoInfoTypeEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoRev","DisplayName":"نسخه دیتاشیت","Alliance":"PartExteraInfoRev","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"IsRoutine","DisplayName":"روتین هست","Alliance":"IsRoutine","Address":null,"SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد Bom","Alliance":"CreatedOnShamsiDateTime","Address":null,"SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"Comment","DisplayName":"کامنت","Alliance":"Comment","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartId","DisplayName":"PartId","Alliance":"PartId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoType","DisplayName":"PartExteraInfoType","Alliance":"PartExteraInfoType","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"CreatedDate","DisplayName":"CreatedDate","Alliance":"CreatedDate","Address":null,"SystemTypeName":"DateTime","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":2,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductNameGroupId","DisplayName":"ProductNameGroupId","Alliance":"ProductNameGroupId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductFormulId","DisplayName":"ProductFormulId","Alliance":"ProductFormulId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""}]', N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"},{"dataActionName":"defaultMultiSelect","enable":false,"title":"انتخاب چندتایی پیش‌فرض"}]', N'[{"id":"id_ldp_datasheet","title":"نمایش دیتاشیت","dataActionName":"showDataSheet","colorClass":"btn-color-primary","iconClass":"fa fa-note","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    if (!ctx.selectedRow) {\n        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');\n        return;\n    }\n    if (!ctx.selectedRow.partExteraInfoType || ctx.selectedRow.partExteraInfoType == 0) {\n        toastr.warning(''هیچ دیتاشیتی جهت نمایش یافت نشد.'');\n        return;\n    }\n    var optionsEl = '''';\n    for (var i = ctx.selectedRow.partExteraInfoRev; i >= 0; i--) {\n        optionsEl += '' <option value=\"'' + i + ''\">'' + i + ''</option> '';\n    }\n    var $modalContetn = ''<div><select class=\"form-select\" id=\"ldpDatasheetRevSelect\">'' + optionsEl + ''</select></div>'';\n    $.confirm({\n        title: ''انتخاب نسخه دیتاشیت'',\n        content: $modalContetn,\n        type: ''green'',\n        columnClass: ''col-md-6'',\n        typeAnimated: true,\n        buttons: {\n            acc: {\n                text: ''نمایش'',\n                btnClass: ''btn btn-success'',\n                action: function () {\n                    var selectedRev = this.$content.find(''#ldpDatasheetRevSelect'').val();\n                    if (selectedRev === undefined || selectedRev === null || selectedRev === '''') {\n                        selectedRev = ctx.selectedRow.partExteraInfoRev;\n                    }\n                    var partId = ctx.selectedRow.partId;\n                    var revQ = \"rev={''values'':[''\" + selectedRev + \"''],''condition'':''=''}\";\n                    var productQ = \"productId={''values'':[''\" + partId + \"''],''condition'':''=''}\";\n                    switch (ctx.selectedRow.partExteraInfoType) {\n                        case 1:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/partexterainfooilinject?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 2:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoOilFree?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 3:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoDryer?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 4:\n                        case 5:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoPsa?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                    }\n                }\n            },\n            close: {\n                text: ''بستن'',\n                btnClass: ''btn btn-danger'',\n                action: function () {}\n            }\n        }\n    });\n}"},{"id":"id_ldp_techdocs","title":"پیوست مدارک فنی","dataActionName":"showDocuments","colorClass":"btn-color-success","iconClass":"bi-filetype-doc bi","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"async function(ctx) {\n    if (!ctx.selectedRow) {\n        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');\n        return;\n    }\n    var response = await get(''/Panel/Inv/Part/PartDocumentInfo?id='' + ctx.selectedRow.partId);\n    if (!response.isSuccess)\n        return toastr.error(response.message);\n    var rowsHtml = (response.data || []).map(function (x) {\n        return ''<tr>''\n            + ''<td>'' + (x.originalName || '''') + ''</td>''\n            + ''<td>'' + (x.size || '''') + ''</td>''\n            + ''<td>'' + (x.type || '''') + ''</td>''\n            + ''<td>'' + (x.main || '''') + ''</td>''\n            + ''<td>'' + (x.createdByName || '''') + ''</td>''\n            + ''<td>'' + (x.createdOnShamsiDateTime || '''') + ''</td>''\n            + ''<td>'' + (x.modifiedByName || '''') + ''</td>''\n            + ''<td>'' + (x.modifiedDateShamsiDateTime || '''') + ''</td>''\n            + ''</tr>'';\n    }).join('''');\n    var $modalContetn = $(''<table class=\"table table-bordered w-100\">''\n        + ''<thead><tr>''\n        + ''<th>نام مدرک</th><th>سایز</th><th>نوع</th><th>اصلی</th>''\n        + ''<th>ایجاد کننده</th><th>زمان ایجاد</th><th>ویرایش کننده</th><th>زمان ویرایش</th>''\n        + ''</tr></thead><tbody>'' + rowsHtml + ''</tbody></table>'');\n    $.confirm({\n        title: ''پیوست مدارک'',\n        content: $modalContetn,\n        type: ''green'',\n        columnClass: ''col-md-12'',\n        typeAnimated: true,\n        buttons: {\n            close: {\n                text: ''بستن'',\n                btnClass: ''btn btn-danger'',\n                action: function () {}\n            }\n        }\n    });\n}"}]'),
        (N'vw_ListDocumentProductPricenew', N'لیست اطلاعات با قیمت خرید', N' 
 DECLARE @CurrencyPrice DECIMAL(18, 2);

SELECT TOP (1)
    @CurrencyPrice = CAST(fieldValue.Number1 AS DECIMAL(18, 2))
FROM ERPS.erps.GNR3.Currency AS currency
LEFT JOIN ERPS.erps.SYS3.FieldValueContainer AS fieldValue
    ON fieldValue.RecordID = currency.CurrencyID
   AND fieldValue.EntityCode = 240
WHERE currency.CurrencyID = 257;

SET @CurrencyPrice = ISNULL(@CurrencyPrice, 0);

;WITH LatestPartExtraInfo AS
(
    SELECT
        extraInfo.ProductId,
        extraInfo.Revision,
        extraInfo.InfoType,
        ROW_NUMBER() OVER
        (
            PARTITION BY extraInfo.ProductId
            ORDER BY extraInfo.Revision DESC, extraInfo.Id DESC
        ) AS RowNumber
    FROM Inv.PartExtraInfo AS extraInfo
),
BaseQuery AS
(
    SELECT
        formula.Id,
        formula.ProductId,
        formula.ProductNameGroupId,
        formula.CreatedOnMiladiDateTime,
        formula.CreatedOnShamsiDateTime,
        formula.Comment,
        product.Code AS PartCode,
        product.Name AS PartName,
        product.DesignTypeIsRoutine AS IsRoutine,
        product.[Foreign],
        product.EngineeringRoutine,
        product.DataSheetUsage,
        product.DataSheet,
        product.BrandInDataSheet,
        product.PreciseTool,
        product.SaleRate,
        extraInfo.Revision AS PartExteraInfoRev,
        extraInfo.InfoType AS PartExteraInfoType,
        productGroup.Name AS ProductNameGroupName
    FROM Bom.ProductFormul AS formula
    INNER JOIN Inv.Part AS product ON product.Id = formula.ProductId
    LEFT JOIN LatestPartExtraInfo AS extraInfo
        ON extraInfo.ProductId = product.Id
       AND extraInfo.RowNumber = 1
    LEFT JOIN Bom.ProductGroup AS productGroup
        ON productGroup.Id = formula.ProductNameGroupId
),
LatestForeignPrice AS
(
    SELECT
        price.PartId,
        price.PriceUnitValue + ISNULL(price.TransportationCost, 0) AS PriceValue,
        ROW_NUMBER() OVER
        (
            PARTITION BY price.PartId
            ORDER BY price.CreatedOnMiladiDateTime DESC, price.Id DESC
        ) AS RowNumber
    FROM Sup.InquiryPartPrice AS price
    WHERE price.Type <> 1675
      AND ISNULL(price.PriceStatus, 1) <> 2834
      AND ISNULL(price.PriceUnitId, 1) <> 1
      AND price.CreatedOnMiladiDateTime >= ''2023-03-21''
),
LatestRialPrice AS
(
    SELECT
        price.PartId,
        ISNULL(price.UnitPrice, 0) AS PriceValue,
        price.CreatedOnMiladiDateTime AS PriceDate,
        ROW_NUMBER() OVER
        (
            PARTITION BY price.PartId
            ORDER BY price.CreatedOnMiladiDateTime DESC, price.Id DESC
        ) AS RowNumber
    FROM Sup.InquiryPartPrice AS price
    WHERE price.Type <> 1675
      AND ISNULL(price.PriceStatus, 1) <> 2834
      AND ISNULL(price.PriceUnitId, 1) = 1
      AND price.CreatedOnMiladiDateTime >= ''2023-03-21''
       AND price.CreatedOnMiladiDateTime < ''2025-03-21''
),
ListData AS
(
    SELECT
        bomItem.Id AS ProductFormulId,
        product.Id AS ProductId,
        CAST(CASE
             WHEN foreignPrice.PriceValue IS NOT NULL AND foreignPrice.PriceValue > 1
                THEN foreignPrice.PriceValue * ISNULL(bomItem.UsingRate, 1)
            ELSE (rialPrice.PriceValue * ISNULL(bomItem.UsingRate, 1))
                 / NULLIF(exchangeRate.FinalPrice, 0)
        END AS DECIMAL(28, 8)) AS TotalBuyPriceInEuro,
        CAST(CASE
            WHEN foreignPrice.PriceValue IS NOT NULL AND foreignPrice.PriceValue > 1
                THEN foreignPrice.PriceValue * ISNULL(bomItem.UsingRate, 1)
                     * ISNULL(component.SaleRate, 1)
            ELSE ((rialPrice.PriceValue * ISNULL(bomItem.UsingRate, 1))
                  / NULLIF(exchangeRate.FinalPrice, 0))
                 * ISNULL(component.SaleRate, 1)
        END AS DECIMAL(28, 8)) AS SalePriceFromEuro,
        ISNULL(product.SaleRate, 1) AS ProductSaleRate,
        component.[Foreign] AS IsForeign
    FROM Inv.Part AS product
    INNER JOIN dbo.vw_BomProductItem AS bomItem
        ON bomItem.ProductId = product.Id
    INNER JOIN Inv.Part AS component
        ON component.Id = bomItem.PartItemId
    LEFT JOIN LatestForeignPrice AS foreignPrice
        ON foreignPrice.PartId = component.Id
       AND foreignPrice.RowNumber = 1
    LEFT JOIN LatestRialPrice AS rialPrice
        ON rialPrice.PartId = component.Id
       AND rialPrice.RowNumber = 1
    OUTER APPLY
    (
        SELECT TOP (1) rate.FinalPrice
        FROM Acc.ExchangeRateArchive AS rate
        WHERE rate.PriceUnitId = 4
          AND rate.CreatedOnMiladiDateTime <= rialPrice.PriceDate
        ORDER BY rate.CreatedOnMiladiDateTime DESC, rate.Id DESC
    ) AS exchangeRate
),
FinalPrice AS
(
    SELECT
        data.ProductFormulId,
        data.ProductId,
        CAST(SUM(data.TotalBuyPriceInEuro) AS DECIMAL(28, 5)) AS TotalBuyPriceInEuro,
        CAST(SUM(data.SalePriceFromEuro * data.ProductSaleRate) AS DECIMAL(28, 5)) AS SalePriceFromRuro,
        CAST(SUM(CASE
            WHEN data.IsForeign = 1 THEN data.TotalBuyPriceInEuro
            ELSE 0
        END) AS DECIMAL(28, 5)) AS ExternalTotalBuyPriceInEuro
    FROM ListData AS data
    GROUP BY data.ProductFormulId, data.ProductId
),
InternalPrice AS
(
     SELECT
        documentPrice.ProductId,
        MAX(CAST(ISNULL(documentPrice.InternalTotalBuyPrice, 0) AS DECIMAL(28, 5))) AS InternalTotalBuyPrice
    FROM Edms.vw_PartDocumentPrice AS documentPrice
    GROUP BY documentPrice.ProductId
),
DocumentCount AS
(
    SELECT
        document.PartId,
        COUNT(*) AS DocumentCount
    FROM Inv.PartDocument AS document
    WHERE document.Main = 1
    GROUP BY document.PartId
),
BomState AS
(
    SELECT DISTINCT item.ProductFormulId
    FROM Bom.vw_ProductItem AS item
)
--!--mainsection
SELECT
    base.Id AS ProductFormulId,
    ISNULL(document.DocumentCount, 0) AS DocumentCount,
    CAST(CASE WHEN bom.ProductFormulId IS NULL THEN 0 ELSE 1 END AS BIT) AS HasBom,
    base.PartCode,
    base.PartName,
    base.ProductNameGroupName,
    base.IsRoutine,
    base.CreatedOnShamsiDateTime,
    base.Comment,
    base.ProductNameGroupId,
    base.CreatedOnMiladiDateTime AS CreatedDate,
    base.PartExteraInfoRev,
    base.PartExteraInfoType AS PartExteraInfoTypeName,
    base.PartExteraInfoType,
    base.ProductId AS PartId,
    ISNULL(internalPrice.InternalTotalBuyPrice, 0) AS Internal_TotalBuyPrice,
    CAST(ROUND(ISNULL(price.ExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0) AS DECIMAL(28, 5)) AS External_TotalBuyPrice,
    CAST(ROUND(ISNULL(price.ExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0) + ISNULL(internalPrice.InternalTotalBuyPrice, 0) AS DECIMAL(28, 5)) AS BuyPrice,
    CAST(ROUND(ISNULL(price.TotalBuyPriceInEuro, 0), 0) AS DECIMAL(28, 5)) AS BuyPriceInEuro,
    CAST(ROUND(ROUND(ISNULL(price.TotalBuyPriceInEuro, 0), 0) * @CurrencyPrice, 0) AS DECIMAL(28, 5)) AS SalePriceRateAzad
FROM BaseQuery AS base
LEFT JOIN InternalPrice AS internalPrice ON internalPrice.ProductId = base.ProductId
LEFT JOIN BomState AS bom ON bom.ProductFormulId = base.Id
LEFT JOIN DocumentCount AS document ON document.PartId = base.ProductId
LEFT JOIN FinalPrice AS price ON price.ProductFormulId = base.Id
', N'[{"TableName":null,"ColumnName":"DocumentCount","DisplayName":"تعداد مدرک","Alliance":"DocumentCount","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"HasBom","DisplayName":"دارای Bom","Alliance":"HasBom","Address":null,"SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartCode","DisplayName":"کد کالا","Alliance":"PartCode","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartName","DisplayName":"نام کالا","Alliance":"PartName","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductNameGroupName","DisplayName":"گروه کالا","Alliance":"ProductNameGroupName","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"Internal_TotalBuyPrice","DisplayName":"قیمت خرید داخلی","Alliance":"Internal_TotalBuyPrice","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"External_TotalBuyPrice","DisplayName":"قیمت خرید خارجی","Alliance":"External_TotalBuyPrice","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"BuyPrice","DisplayName":"قیمت خرید","Alliance":"BuyPrice","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"BuyPriceInEuro","DisplayName":"قیمت خرید (یورو)","Alliance":"BuyPriceInEuro","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"SalePriceRateAzad","DisplayName":"مجموع قیمت(نرخ آزاد)","Alliance":"SalePriceRateAzad","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoTypeName","DisplayName":"نوع دیتا شیت","Alliance":"PartExteraInfoTypeName","Address":null,"SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Inv.Enums.PartExtraInfoInfoTypeEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoRev","DisplayName":"نسخه دیتاشیت","Alliance":"PartExteraInfoRev","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"IsRoutine","DisplayName":"روتین هست","Alliance":"IsRoutine","Address":null,"SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد Bom","Alliance":"CreatedOnShamsiDateTime","Address":null,"SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"Comment","DisplayName":"کامنت","Alliance":"Comment","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartId","DisplayName":"PartId","Alliance":"PartId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoType","DisplayName":"PartExteraInfoType","Alliance":"PartExteraInfoType","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"CreatedDate","DisplayName":"CreatedDate","Alliance":"CreatedDate","Address":null,"SystemTypeName":"DateTime","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":2,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductNameGroupId","DisplayName":"ProductNameGroupId","Alliance":"ProductNameGroupId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductFormulId","DisplayName":"ProductFormulId","Alliance":"ProductFormulId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""}]', N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"},{"dataActionName":"defaultMultiSelect","enable":false,"title":"انتخاب چندتایی پیش‌فرض"}]', N'[{"id":"id_ldp_datasheet","title":"نمایش دیتاشیت","dataActionName":"showDataSheet","colorClass":"btn-color-primary","iconClass":"fa fa-note","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    if (!ctx.selectedRow) {\n        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');\n        return;\n    }\n    if (!ctx.selectedRow.partExteraInfoType || ctx.selectedRow.partExteraInfoType == 0) {\n        toastr.warning(''هیچ دیتاشیتی جهت نمایش یافت نشد.'');\n        return;\n    }\n    var optionsEl = '''';\n    for (var i = ctx.selectedRow.partExteraInfoRev; i >= 0; i--) {\n        optionsEl += '' <option value=\"'' + i + ''\">'' + i + ''</option> '';\n    }\n    var $modalContetn = ''<div><select class=\"form-select\" id=\"ldpDatasheetRevSelect\">'' + optionsEl + ''</select></div>'';\n    $.confirm({\n        title: ''انتخاب نسخه دیتاشیت'',\n        content: $modalContetn,\n        type: ''green'',\n        columnClass: ''col-md-6'',\n        typeAnimated: true,\n        buttons: {\n            acc: {\n                text: ''نمایش'',\n                btnClass: ''btn btn-success'',\n                action: function () {\n                    var selectedRev = this.$content.find(''#ldpDatasheetRevSelect'').val();\n                    if (selectedRev === undefined || selectedRev === null || selectedRev === '''') {\n                        selectedRev = ctx.selectedRow.partExteraInfoRev;\n                    }\n                    var partId = ctx.selectedRow.partId;\n                    var revQ = \"rev={''values'':[''\" + selectedRev + \"''],''condition'':''=''}\";\n                    var productQ = \"productId={''values'':[''\" + partId + \"''],''condition'':''=''}\";\n                    switch (ctx.selectedRow.partExteraInfoType) {\n                        case 1:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/partexterainfooilinject?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 2:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoOilFree?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 3:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoDryer?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 4:\n                        case 5:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoPsa?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                    }\n                }\n            },\n            close: {\n                text: ''بستن'',\n                btnClass: ''btn btn-danger'',\n                action: function () {}\n            }\n        }\n    });\n}"},{"id":"id_ldp_techdocs","title":"پیوست مدارک فنی","dataActionName":"showDocuments","colorClass":"btn-color-success","iconClass":"bi-filetype-doc bi","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"async function(ctx) {\n    if (!ctx.selectedRow) {\n        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');\n        return;\n    }\n    var response = await get(''/Panel/Inv/Part/PartDocumentInfo?id='' + ctx.selectedRow.partId);\n    if (!response.isSuccess)\n        return toastr.error(response.message);\n    var rowsHtml = (response.data || []).map(function (x) {\n        return ''<tr>''\n            + ''<td>'' + (x.originalName || '''') + ''</td>''\n            + ''<td>'' + (x.size || '''') + ''</td>''\n            + ''<td>'' + (x.type || '''') + ''</td>''\n            + ''<td>'' + (x.main || '''') + ''</td>''\n            + ''<td>'' + (x.createdByName || '''') + ''</td>''\n            + ''<td>'' + (x.createdOnShamsiDateTime || '''') + ''</td>''\n            + ''<td>'' + (x.modifiedByName || '''') + ''</td>''\n            + ''<td>'' + (x.modifiedDateShamsiDateTime || '''') + ''</td>''\n            + ''</tr>'';\n    }).join('''');\n    var $modalContetn = $(''<table class=\"table table-bordered w-100\">''\n        + ''<thead><tr>''\n        + ''<th>نام مدرک</th><th>سایز</th><th>نوع</th><th>اصلی</th>''\n        + ''<th>ایجاد کننده</th><th>زمان ایجاد</th><th>ویرایش کننده</th><th>زمان ویرایش</th>''\n        + ''</tr></thead><tbody>'' + rowsHtml + ''</tbody></table>'');\n    $.confirm({\n        title: ''پیوست مدارک'',\n        content: $modalContetn,\n        type: ''green'',\n        columnClass: ''col-md-12'',\n        typeAnimated: true,\n        buttons: {\n            close: {\n                text: ''بستن'',\n                btnClass: ''btn btn-danger'',\n                action: function () {}\n            }\n        }\n    });\n}"}]'),
        (N'vw_ListDocumentProductPriceSale', N'لیست اطلاعات با قیمت فروش', N' 
 DECLARE @CurrencyPrice DECIMAL(18, 2);

SELECT TOP (1)
    @CurrencyPrice = CAST(fieldValue.Number1 AS DECIMAL(18, 2))
FROM ERPS.erps.GNR3.Currency AS currency
LEFT JOIN ERPS.erps.SYS3.FieldValueContainer AS fieldValue
    ON fieldValue.RecordID = currency.CurrencyID
   AND fieldValue.EntityCode = 240
WHERE currency.CurrencyID = 257;

SET @CurrencyPrice = ISNULL(@CurrencyPrice, 0);

;WITH LatestPartExtraInfo AS
(
    SELECT
        extraInfo.ProductId,
        extraInfo.Revision,
        extraInfo.InfoType,
        ROW_NUMBER() OVER
        (
            PARTITION BY extraInfo.ProductId
            ORDER BY extraInfo.Revision DESC, extraInfo.Id DESC
        ) AS RowNumber
    FROM Inv.PartExtraInfo AS extraInfo
),
BaseQuery AS
(
    SELECT
        formula.Id,
        formula.ProductId,
        formula.ProductNameGroupId,
        formula.CreatedOnMiladiDateTime,
        formula.CreatedOnShamsiDateTime,
        formula.Comment,
        product.Code AS PartCode,
        product.Name AS PartName,
        product.DesignTypeIsRoutine AS IsRoutine,
        product.[Foreign],
        product.EngineeringRoutine,
        product.DataSheetUsage,
        product.DataSheet,
        product.BrandInDataSheet,
        product.PreciseTool,
        product.SaleRate,
        extraInfo.Revision AS PartExteraInfoRev,
        extraInfo.InfoType AS PartExteraInfoType,
        productGroup.Name AS ProductNameGroupName
    FROM Bom.ProductFormul AS formula
    INNER JOIN Inv.Part AS product ON product.Id = formula.ProductId
    LEFT JOIN LatestPartExtraInfo AS extraInfo
        ON extraInfo.ProductId = product.Id
       AND extraInfo.RowNumber = 1
    LEFT JOIN Bom.ProductGroup AS productGroup
        ON productGroup.Id = formula.ProductNameGroupId
),
LatestForeignPrice AS
(
    SELECT
        price.PartId,
        price.PriceUnitValue + ISNULL(price.TransportationCost, 0) AS PriceValue,
        ROW_NUMBER() OVER
        (
            PARTITION BY price.PartId
            ORDER BY price.CreatedOnMiladiDateTime DESC, price.Id DESC
        ) AS RowNumber
    FROM Sup.InquiryPartPrice AS price
    WHERE price.Type <> 1675
      AND ISNULL(price.PriceStatus, 1) <> 2834
      AND ISNULL(price.PriceUnitId, 1) <> 1
      AND price.CreatedOnMiladiDateTime >= ''2023-03-21''
),
LatestRialPrice AS
(
    SELECT
        price.PartId,
        ISNULL(price.UnitPrice, 0) AS PriceValue,
        price.CreatedOnMiladiDateTime AS PriceDate,
        ROW_NUMBER() OVER
        (
            PARTITION BY price.PartId
            ORDER BY price.CreatedOnMiladiDateTime DESC, price.Id DESC
        ) AS RowNumber
    FROM Sup.InquiryPartPrice AS price
    WHERE price.Type <> 1675
      AND ISNULL(price.PriceStatus, 1) <> 2834
      AND ISNULL(price.PriceUnitId, 1) = 1
      AND price.CreatedOnMiladiDateTime >= ''2023-03-21''
       AND price.CreatedOnMiladiDateTime < ''2025-03-21''
),
ListData AS
(
    SELECT
        bomItem.Id AS ProductFormulId,
        product.Id AS ProductId,
        CAST(CASE
             WHEN foreignPrice.PriceValue IS NOT NULL AND foreignPrice.PriceValue > 1
                THEN foreignPrice.PriceValue * ISNULL(bomItem.UsingRate, 1)
            ELSE (rialPrice.PriceValue * ISNULL(bomItem.UsingRate, 1))
                 / NULLIF(exchangeRate.FinalPrice, 0)
        END AS DECIMAL(28, 8)) AS TotalBuyPriceInEuro,
        CAST(CASE
            WHEN foreignPrice.PriceValue IS NOT NULL AND foreignPrice.PriceValue > 1
                THEN foreignPrice.PriceValue * ISNULL(bomItem.UsingRate, 1)
                     * ISNULL(component.SaleRate, 1)
            ELSE ((rialPrice.PriceValue * ISNULL(bomItem.UsingRate, 1))
                  / NULLIF(exchangeRate.FinalPrice, 0))
                 * ISNULL(component.SaleRate, 1)
        END AS DECIMAL(28, 8)) AS SalePriceFromEuro,
        ISNULL(product.SaleRate, 1) AS ProductSaleRate,
        component.[Foreign] AS IsForeign
    FROM Inv.Part AS product
    INNER JOIN dbo.vw_BomProductItem AS bomItem
        ON bomItem.ProductId = product.Id
    INNER JOIN Inv.Part AS component
        ON component.Id = bomItem.PartItemId
    LEFT JOIN LatestForeignPrice AS foreignPrice
        ON foreignPrice.PartId = component.Id
       AND foreignPrice.RowNumber = 1
    LEFT JOIN LatestRialPrice AS rialPrice
        ON rialPrice.PartId = component.Id
       AND rialPrice.RowNumber = 1
    OUTER APPLY
    (
        SELECT TOP (1) rate.FinalPrice
        FROM Acc.ExchangeRateArchive AS rate
        WHERE rate.PriceUnitId = 4
          AND rate.CreatedOnMiladiDateTime <= rialPrice.PriceDate
        ORDER BY rate.CreatedOnMiladiDateTime DESC, rate.Id DESC
    ) AS exchangeRate
),
FinalPrice AS
(
    SELECT
        data.ProductFormulId,
        data.ProductId,
        CAST(SUM(data.TotalBuyPriceInEuro) AS DECIMAL(28, 5)) AS TotalBuyPriceInEuro,
        CAST(SUM(data.SalePriceFromEuro * data.ProductSaleRate) AS DECIMAL(28, 5)) AS SalePriceFromRuro,
        CAST(SUM(CASE
            WHEN data.IsForeign = 1 THEN data.TotalBuyPriceInEuro
            ELSE 0
        END) AS DECIMAL(28, 5)) AS ExternalTotalBuyPriceInEuro
    FROM ListData AS data
    GROUP BY data.ProductFormulId, data.ProductId
),
InternalPrice AS
(
     SELECT
        documentPrice.ProductId,
        MAX(CAST(ISNULL(documentPrice.InternalTotalBuyPrice, 0) AS DECIMAL(28, 5))) AS InternalTotalBuyPrice
    FROM Edms.vw_PartDocumentPrice AS documentPrice
    GROUP BY documentPrice.ProductId
),
DocumentCount AS
(
    SELECT
        document.PartId,
        COUNT(*) AS DocumentCount
    FROM Inv.PartDocument AS document
    WHERE document.Main = 1
    GROUP BY document.PartId
),
BomState AS
(
    SELECT DISTINCT item.ProductFormulId
    FROM Bom.vw_ProductItem AS item
)
--!--mainsection
SELECT
    base.Id AS ProductFormulId,
    ISNULL(document.DocumentCount, 0) AS DocumentCount,
    CAST(CASE WHEN bom.ProductFormulId IS NULL THEN 0 ELSE 1 END AS BIT) AS HasBom,
    base.PartCode,
    base.PartName,
    base.ProductNameGroupName,
    base.IsRoutine,
    base.CreatedOnShamsiDateTime,
    base.Comment,
    base.ProductNameGroupId,
    base.CreatedOnMiladiDateTime AS CreatedDate,
    base.PartExteraInfoRev,
    base.PartExteraInfoType AS PartExteraInfoTypeName,
    base.PartExteraInfoType,
    base.ProductId AS PartId,
    CAST(CASE
        WHEN (ROUND(ISNULL(price.ExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0)
              + ISNULL(internalPrice.InternalTotalBuyPrice, 0)) * ISNULL(base.SaleRate, 1)
             > ROUND(ISNULL(price.SalePriceFromRuro, 0) * @CurrencyPrice, 0)
            THEN CAST(
                (ROUND(ISNULL(price.ExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0)
                 + ISNULL(internalPrice.InternalTotalBuyPrice, 0)) * ISNULL(base.SaleRate, 1)
                AS BIGINT)
        ELSE ROUND(ISNULL(price.SalePriceFromRuro, 0) * @CurrencyPrice, 0)
    END AS DECIMAL(28, 5)) AS SalePrice,
    @CurrencyPrice AS CurrencyRateAzad
FROM BaseQuery AS base
LEFT JOIN InternalPrice AS internalPrice ON internalPrice.ProductId = base.ProductId
LEFT JOIN BomState AS bom ON bom.ProductFormulId = base.Id
LEFT JOIN DocumentCount AS document ON document.PartId = base.ProductId
LEFT JOIN FinalPrice AS price ON price.ProductFormulId = base.Id
', N'[{"TableName":null,"ColumnName":"DocumentCount","DisplayName":"تعداد مدرک","Alliance":"DocumentCount","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"HasBom","DisplayName":"دارای Bom","Alliance":"HasBom","Address":null,"SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartCode","DisplayName":"کد کالا","Alliance":"PartCode","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartName","DisplayName":"نام کالا","Alliance":"PartName","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductNameGroupName","DisplayName":"گروه کالا","Alliance":"ProductNameGroupName","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"SalePrice","DisplayName":"مجموع قیمت فروش(نرخ آزاد)","Alliance":"SalePrice","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"CurrencyRateAzad","DisplayName":"نرخ ارز آزاد","Alliance":"CurrencyRateAzad","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoTypeName","DisplayName":"نوع دیتا شیت","Alliance":"PartExteraInfoTypeName","Address":null,"SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Inv.Enums.PartExtraInfoInfoTypeEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoRev","DisplayName":"نسخه دیتاشیت","Alliance":"PartExteraInfoRev","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"IsRoutine","DisplayName":"روتین هست","Alliance":"IsRoutine","Address":null,"SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد Bom","Alliance":"CreatedOnShamsiDateTime","Address":null,"SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"Comment","DisplayName":"کامنت","Alliance":"Comment","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartId","DisplayName":"PartId","Alliance":"PartId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoType","DisplayName":"PartExteraInfoType","Alliance":"PartExteraInfoType","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"CreatedDate","DisplayName":"CreatedDate","Alliance":"CreatedDate","Address":null,"SystemTypeName":"DateTime","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":2,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductNameGroupId","DisplayName":"ProductNameGroupId","Alliance":"ProductNameGroupId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductFormulId","DisplayName":"ProductFormulId","Alliance":"ProductFormulId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""}]', N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"},{"dataActionName":"defaultMultiSelect","enable":false,"title":"انتخاب چندتایی پیش‌فرض"}]', N'[{"id":"id_ldp_datasheet","title":"نمایش دیتاشیت","dataActionName":"showDataSheet","colorClass":"btn-color-primary","iconClass":"fa fa-note","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    if (!ctx.selectedRow) {\n        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');\n        return;\n    }\n    if (!ctx.selectedRow.partExteraInfoType || ctx.selectedRow.partExteraInfoType == 0) {\n        toastr.warning(''هیچ دیتاشیتی جهت نمایش یافت نشد.'');\n        return;\n    }\n    var optionsEl = '''';\n    for (var i = ctx.selectedRow.partExteraInfoRev; i >= 0; i--) {\n        optionsEl += '' <option value=\"'' + i + ''\">'' + i + ''</option> '';\n    }\n    var $modalContetn = ''<div><select class=\"form-select\" id=\"ldpDatasheetRevSelect\">'' + optionsEl + ''</select></div>'';\n    $.confirm({\n        title: ''انتخاب نسخه دیتاشیت'',\n        content: $modalContetn,\n        type: ''green'',\n        columnClass: ''col-md-6'',\n        typeAnimated: true,\n        buttons: {\n            acc: {\n                text: ''نمایش'',\n                btnClass: ''btn btn-success'',\n                action: function () {\n                    var selectedRev = this.$content.find(''#ldpDatasheetRevSelect'').val();\n                    if (selectedRev === undefined || selectedRev === null || selectedRev === '''') {\n                        selectedRev = ctx.selectedRow.partExteraInfoRev;\n                    }\n                    var partId = ctx.selectedRow.partId;\n                    var revQ = \"rev={''values'':[''\" + selectedRev + \"''],''condition'':''=''}\";\n                    var productQ = \"productId={''values'':[''\" + partId + \"''],''condition'':''=''}\";\n                    switch (ctx.selectedRow.partExteraInfoType) {\n                        case 1:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/partexterainfooilinject?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 2:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoOilFree?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 3:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoDryer?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 4:\n                        case 5:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoPsa?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                    }\n                }\n            },\n            close: {\n                text: ''بستن'',\n                btnClass: ''btn btn-danger'',\n                action: function () {}\n            }\n        }\n    });\n}"},{"id":"id_ldp_techdocs","title":"پیوست مدارک فنی","dataActionName":"showDocuments","colorClass":"btn-color-success","iconClass":"bi-filetype-doc bi","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"async function(ctx) {\n    if (!ctx.selectedRow) {\n        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');\n        return;\n    }\n    var response = await get(''/Panel/Inv/Part/PartDocumentInfo?id='' + ctx.selectedRow.partId);\n    if (!response.isSuccess)\n        return toastr.error(response.message);\n    var rowsHtml = (response.data || []).map(function (x) {\n        return ''<tr>''\n            + ''<td>'' + (x.originalName || '''') + ''</td>''\n            + ''<td>'' + (x.size || '''') + ''</td>''\n            + ''<td>'' + (x.type || '''') + ''</td>''\n            + ''<td>'' + (x.main || '''') + ''</td>''\n            + ''<td>'' + (x.createdByName || '''') + ''</td>''\n            + ''<td>'' + (x.createdOnShamsiDateTime || '''') + ''</td>''\n            + ''<td>'' + (x.modifiedByName || '''') + ''</td>''\n            + ''<td>'' + (x.modifiedDateShamsiDateTime || '''') + ''</td>''\n            + ''</tr>'';\n    }).join('''');\n    var $modalContetn = $(''<table class=\"table table-bordered w-100\">''\n        + ''<thead><tr>''\n        + ''<th>نام مدرک</th><th>سایز</th><th>نوع</th><th>اصلی</th>''\n        + ''<th>ایجاد کننده</th><th>زمان ایجاد</th><th>ویرایش کننده</th><th>زمان ویرایش</th>''\n        + ''</tr></thead><tbody>'' + rowsHtml + ''</tbody></table>'');\n    $.confirm({\n        title: ''پیوست مدارک'',\n        content: $modalContetn,\n        type: ''green'',\n        columnClass: ''col-md-12'',\n        typeAnimated: true,\n        buttons: {\n            close: {\n                text: ''بستن'',\n                btnClass: ''btn btn-danger'',\n                action: function () {}\n            }\n        }\n    });\n}"}]'),
        (N'vw_ListDocumentProductPrice', N'لیست اطلاعات با قیمت (خرید و فروش)', N' 
 DECLARE @CurrencyPrice DECIMAL(18, 2);

SELECT TOP (1)
    @CurrencyPrice = CAST(fieldValue.Number1 AS DECIMAL(18, 2))
FROM ERPS.erps.GNR3.Currency AS currency
LEFT JOIN ERPS.erps.SYS3.FieldValueContainer AS fieldValue
    ON fieldValue.RecordID = currency.CurrencyID
   AND fieldValue.EntityCode = 240
WHERE currency.CurrencyID = 257;

SET @CurrencyPrice = ISNULL(@CurrencyPrice, 0);

;WITH LatestPartExtraInfo AS
(
    SELECT
        extraInfo.ProductId,
        extraInfo.Revision,
        extraInfo.InfoType,
        ROW_NUMBER() OVER
        (
            PARTITION BY extraInfo.ProductId
            ORDER BY extraInfo.Revision DESC, extraInfo.Id DESC
        ) AS RowNumber
    FROM Inv.PartExtraInfo AS extraInfo
),
BaseQuery AS
(
    SELECT
        formula.Id,
        formula.ProductId,
        formula.ProductNameGroupId,
        formula.CreatedOnMiladiDateTime,
        formula.CreatedOnShamsiDateTime,
        formula.Comment,
        product.Code AS PartCode,
        product.Name AS PartName,
        product.DesignTypeIsRoutine AS IsRoutine,
        product.[Foreign],
        product.EngineeringRoutine,
        product.DataSheetUsage,
        product.DataSheet,
        product.BrandInDataSheet,
        product.PreciseTool,
        product.SaleRate,
        extraInfo.Revision AS PartExteraInfoRev,
        extraInfo.InfoType AS PartExteraInfoType,
        productGroup.Name AS ProductNameGroupName
    FROM Bom.ProductFormul AS formula
    INNER JOIN Inv.Part AS product ON product.Id = formula.ProductId
    LEFT JOIN LatestPartExtraInfo AS extraInfo
        ON extraInfo.ProductId = product.Id
       AND extraInfo.RowNumber = 1
    LEFT JOIN Bom.ProductGroup AS productGroup
        ON productGroup.Id = formula.ProductNameGroupId
),
LatestForeignPrice AS
(
    SELECT
        price.PartId,
        price.PriceUnitValue + ISNULL(price.TransportationCost, 0) AS PriceValue,
        ROW_NUMBER() OVER
        (
            PARTITION BY price.PartId
            ORDER BY price.CreatedOnMiladiDateTime DESC, price.Id DESC
        ) AS RowNumber
    FROM Sup.InquiryPartPrice AS price
    WHERE price.Type <> 1675
      AND ISNULL(price.PriceStatus, 1) <> 2834
      AND ISNULL(price.PriceUnitId, 1) <> 1
      AND price.CreatedOnMiladiDateTime >= ''2023-03-21''
),
LatestRialPrice AS
(
    SELECT
        price.PartId,
        ISNULL(price.UnitPrice, 0) AS PriceValue,
        price.CreatedOnMiladiDateTime AS PriceDate,
        ROW_NUMBER() OVER
        (
            PARTITION BY price.PartId
            ORDER BY price.CreatedOnMiladiDateTime DESC, price.Id DESC
        ) AS RowNumber
    FROM Sup.InquiryPartPrice AS price
    WHERE price.Type <> 1675
      AND ISNULL(price.PriceStatus, 1) <> 2834
      AND ISNULL(price.PriceUnitId, 1) = 1
      AND price.CreatedOnMiladiDateTime >= ''2023-03-21''
       AND price.CreatedOnMiladiDateTime < ''2025-03-21''
),
ListData AS
(
    SELECT
        bomItem.Id AS ProductFormulId,
        product.Id AS ProductId,
        CAST(CASE
             WHEN foreignPrice.PriceValue IS NOT NULL AND foreignPrice.PriceValue > 1
                THEN foreignPrice.PriceValue * ISNULL(bomItem.UsingRate, 1)
            ELSE (rialPrice.PriceValue * ISNULL(bomItem.UsingRate, 1))
                 / NULLIF(exchangeRate.FinalPrice, 0)
        END AS DECIMAL(28, 8)) AS TotalBuyPriceInEuro,
        CAST(CASE
            WHEN foreignPrice.PriceValue IS NOT NULL AND foreignPrice.PriceValue > 1
                THEN foreignPrice.PriceValue * ISNULL(bomItem.UsingRate, 1)
                     * ISNULL(component.SaleRate, 1)
            ELSE ((rialPrice.PriceValue * ISNULL(bomItem.UsingRate, 1))
                  / NULLIF(exchangeRate.FinalPrice, 0))
                 * ISNULL(component.SaleRate, 1)
        END AS DECIMAL(28, 8)) AS SalePriceFromEuro,
        ISNULL(product.SaleRate, 1) AS ProductSaleRate,
        component.[Foreign] AS IsForeign
    FROM Inv.Part AS product
    INNER JOIN dbo.vw_BomProductItem AS bomItem
        ON bomItem.ProductId = product.Id
    INNER JOIN Inv.Part AS component
        ON component.Id = bomItem.PartItemId
    LEFT JOIN LatestForeignPrice AS foreignPrice
        ON foreignPrice.PartId = component.Id
       AND foreignPrice.RowNumber = 1
    LEFT JOIN LatestRialPrice AS rialPrice
        ON rialPrice.PartId = component.Id
       AND rialPrice.RowNumber = 1
    OUTER APPLY
    (
        SELECT TOP (1) rate.FinalPrice
        FROM Acc.ExchangeRateArchive AS rate
        WHERE rate.PriceUnitId = 4
          AND rate.CreatedOnMiladiDateTime <= rialPrice.PriceDate
        ORDER BY rate.CreatedOnMiladiDateTime DESC, rate.Id DESC
    ) AS exchangeRate
),
FinalPrice AS
(
    SELECT
        data.ProductFormulId,
        data.ProductId,
        CAST(SUM(data.TotalBuyPriceInEuro) AS DECIMAL(28, 5)) AS TotalBuyPriceInEuro,
        CAST(SUM(data.SalePriceFromEuro * data.ProductSaleRate) AS DECIMAL(28, 5)) AS SalePriceFromRuro,
        CAST(SUM(CASE
            WHEN data.IsForeign = 1 THEN data.TotalBuyPriceInEuro
            ELSE 0
        END) AS DECIMAL(28, 5)) AS ExternalTotalBuyPriceInEuro
    FROM ListData AS data
    GROUP BY data.ProductFormulId, data.ProductId
),
InternalPrice AS
(
     SELECT
        documentPrice.ProductId,
        MAX(CAST(ISNULL(documentPrice.InternalTotalBuyPrice, 0) AS DECIMAL(28, 5))) AS InternalTotalBuyPrice
    FROM Edms.vw_PartDocumentPrice AS documentPrice
    GROUP BY documentPrice.ProductId
),
DocumentCount AS
(
    SELECT
        document.PartId,
        COUNT(*) AS DocumentCount
    FROM Inv.PartDocument AS document
    WHERE document.Main = 1
    GROUP BY document.PartId
),
BomState AS
(
    SELECT DISTINCT item.ProductFormulId
    FROM Bom.vw_ProductItem AS item
)
--!--mainsection
SELECT
    base.Id AS ProductFormulId,
    ISNULL(document.DocumentCount, 0) AS DocumentCount,
    CAST(CASE WHEN bom.ProductFormulId IS NULL THEN 0 ELSE 1 END AS BIT) AS HasBom,
    base.PartCode,
    base.PartName,
    base.ProductNameGroupName,
    base.IsRoutine,
    base.CreatedOnShamsiDateTime,
    base.Comment,
    base.ProductNameGroupId,
    base.CreatedOnMiladiDateTime AS CreatedDate,
    base.PartExteraInfoRev,
    base.PartExteraInfoType AS PartExteraInfoTypeName,
    base.PartExteraInfoType,
    base.ProductId AS PartId,
    ISNULL(internalPrice.InternalTotalBuyPrice, 0) AS Internal_TotalBuyPrice,
    CAST(ROUND(ISNULL(price.ExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0) AS DECIMAL(28, 5)) AS External_TotalBuyPrice,
    CAST(ROUND(ISNULL(price.ExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0) + ISNULL(internalPrice.InternalTotalBuyPrice, 0) AS DECIMAL(28, 5)) AS BuyPrice,
    CAST(ROUND(ISNULL(price.TotalBuyPriceInEuro, 0), 0) AS DECIMAL(28, 5)) AS BuyPriceInEuro,
    CAST(ROUND(ROUND(ISNULL(price.TotalBuyPriceInEuro, 0), 0) * @CurrencyPrice, 0) AS DECIMAL(28, 5)) AS SalePriceRateAzad,
    CAST(CASE
        WHEN (ROUND(ISNULL(price.ExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0)
              + ISNULL(internalPrice.InternalTotalBuyPrice, 0)) * ISNULL(base.SaleRate, 1)
             > ROUND(ISNULL(price.SalePriceFromRuro, 0) * @CurrencyPrice, 0)
            THEN CAST(
                (ROUND(ISNULL(price.ExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0)
                 + ISNULL(internalPrice.InternalTotalBuyPrice, 0)) * ISNULL(base.SaleRate, 1)
                AS BIGINT)
        ELSE ROUND(ISNULL(price.SalePriceFromRuro, 0) * @CurrencyPrice, 0)
    END AS DECIMAL(28, 5)) AS SalePrice,
    @CurrencyPrice AS CurrencyRateAzad
FROM BaseQuery AS base
LEFT JOIN InternalPrice AS internalPrice ON internalPrice.ProductId = base.ProductId
LEFT JOIN BomState AS bom ON bom.ProductFormulId = base.Id
LEFT JOIN DocumentCount AS document ON document.PartId = base.ProductId
LEFT JOIN FinalPrice AS price ON price.ProductFormulId = base.Id
', N'[{"TableName":null,"ColumnName":"DocumentCount","DisplayName":"تعداد مدرک","Alliance":"DocumentCount","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"HasBom","DisplayName":"دارای Bom","Alliance":"HasBom","Address":null,"SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartCode","DisplayName":"کد کالا","Alliance":"PartCode","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartName","DisplayName":"نام کالا","Alliance":"PartName","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductNameGroupName","DisplayName":"گروه کالا","Alliance":"ProductNameGroupName","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"Internal_TotalBuyPrice","DisplayName":"قیمت خرید داخلی","Alliance":"Internal_TotalBuyPrice","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"External_TotalBuyPrice","DisplayName":"قیمت خرید خارجی","Alliance":"External_TotalBuyPrice","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"BuyPrice","DisplayName":"قیمت خرید","Alliance":"BuyPrice","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"BuyPriceInEuro","DisplayName":"قیمت خرید (یورو)","Alliance":"BuyPriceInEuro","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"SalePriceRateAzad","DisplayName":"مجموع قیمت(نرخ آزاد)","Alliance":"SalePriceRateAzad","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"SalePrice","DisplayName":"مجموع قیمت فروش(نرخ آزاد)","Alliance":"SalePrice","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"CurrencyRateAzad","DisplayName":"نرخ ارز آزاد","Alliance":"CurrencyRateAzad","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoTypeName","DisplayName":"نوع دیتا شیت","Alliance":"PartExteraInfoTypeName","Address":null,"SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Inv.Enums.PartExtraInfoInfoTypeEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoRev","DisplayName":"نسخه دیتاشیت","Alliance":"PartExteraInfoRev","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"IsRoutine","DisplayName":"روتین هست","Alliance":"IsRoutine","Address":null,"SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"CreatedOnShamsiDateTime","DisplayName":"تاریخ ایجاد Bom","Alliance":"CreatedOnShamsiDateTime","Address":null,"SystemTypeName":"DateTimeShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":4,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"Comment","DisplayName":"کامنت","Alliance":"Comment","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartId","DisplayName":"PartId","Alliance":"PartId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoType","DisplayName":"PartExteraInfoType","Alliance":"PartExteraInfoType","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"CreatedDate","DisplayName":"CreatedDate","Alliance":"CreatedDate","Address":null,"SystemTypeName":"DateTime","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":2,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductNameGroupId","DisplayName":"ProductNameGroupId","Alliance":"ProductNameGroupId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductFormulId","DisplayName":"ProductFormulId","Alliance":"ProductFormulId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""}]', N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"},{"dataActionName":"defaultMultiSelect","enable":false,"title":"انتخاب چندتایی پیش‌فرض"}]', N'[{"id":"id_ldp_datasheet","title":"نمایش دیتاشیت","dataActionName":"showDataSheet","colorClass":"btn-color-primary","iconClass":"fa fa-note","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    if (!ctx.selectedRow) {\n        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');\n        return;\n    }\n    if (!ctx.selectedRow.partExteraInfoType || ctx.selectedRow.partExteraInfoType == 0) {\n        toastr.warning(''هیچ دیتاشیتی جهت نمایش یافت نشد.'');\n        return;\n    }\n    var optionsEl = '''';\n    for (var i = ctx.selectedRow.partExteraInfoRev; i >= 0; i--) {\n        optionsEl += '' <option value=\"'' + i + ''\">'' + i + ''</option> '';\n    }\n    var $modalContetn = ''<div><select class=\"form-select\" id=\"ldpDatasheetRevSelect\">'' + optionsEl + ''</select></div>'';\n    $.confirm({\n        title: ''انتخاب نسخه دیتاشیت'',\n        content: $modalContetn,\n        type: ''green'',\n        columnClass: ''col-md-6'',\n        typeAnimated: true,\n        buttons: {\n            acc: {\n                text: ''نمایش'',\n                btnClass: ''btn btn-success'',\n                action: function () {\n                    var selectedRev = this.$content.find(''#ldpDatasheetRevSelect'').val();\n                    if (selectedRev === undefined || selectedRev === null || selectedRev === '''') {\n                        selectedRev = ctx.selectedRow.partExteraInfoRev;\n                    }\n                    var partId = ctx.selectedRow.partId;\n                    var revQ = \"rev={''values'':[''\" + selectedRev + \"''],''condition'':''=''}\";\n                    var productQ = \"productId={''values'':[''\" + partId + \"''],''condition'':''=''}\";\n                    switch (ctx.selectedRow.partExteraInfoType) {\n                        case 1:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/partexterainfooilinject?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 2:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoOilFree?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 3:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoDryer?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 4:\n                        case 5:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoPsa?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                    }\n                }\n            },\n            close: {\n                text: ''بستن'',\n                btnClass: ''btn btn-danger'',\n                action: function () {}\n            }\n        }\n    });\n}"},{"id":"id_ldp_techdocs","title":"پیوست مدارک فنی","dataActionName":"showDocuments","colorClass":"btn-color-success","iconClass":"bi-filetype-doc bi","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"async function(ctx) {\n    if (!ctx.selectedRow) {\n        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');\n        return;\n    }\n    var response = await get(''/Panel/Inv/Part/PartDocumentInfo?id='' + ctx.selectedRow.partId);\n    if (!response.isSuccess)\n        return toastr.error(response.message);\n    var rowsHtml = (response.data || []).map(function (x) {\n        return ''<tr>''\n            + ''<td>'' + (x.originalName || '''') + ''</td>''\n            + ''<td>'' + (x.size || '''') + ''</td>''\n            + ''<td>'' + (x.type || '''') + ''</td>''\n            + ''<td>'' + (x.main || '''') + ''</td>''\n            + ''<td>'' + (x.createdByName || '''') + ''</td>''\n            + ''<td>'' + (x.createdOnShamsiDateTime || '''') + ''</td>''\n            + ''<td>'' + (x.modifiedByName || '''') + ''</td>''\n            + ''<td>'' + (x.modifiedDateShamsiDateTime || '''') + ''</td>''\n            + ''</tr>'';\n    }).join('''');\n    var $modalContetn = $(''<table class=\"table table-bordered w-100\">''\n        + ''<thead><tr>''\n        + ''<th>نام مدرک</th><th>سایز</th><th>نوع</th><th>اصلی</th>''\n        + ''<th>ایجاد کننده</th><th>زمان ایجاد</th><th>ویرایش کننده</th><th>زمان ویرایش</th>''\n        + ''</tr></thead><tbody>'' + rowsHtml + ''</tbody></table>'');\n    $.confirm({\n        title: ''پیوست مدارک'',\n        content: $modalContetn,\n        type: ''green'',\n        columnClass: ''col-md-12'',\n        typeAnimated: true,\n        buttons: {\n            close: {\n                text: ''بستن'',\n                btnClass: ''btn btn-danger'',\n                action: function () {}\n            }\n        }\n    });\n}"}]'),
        (N'ListDocumentProduct_Bom_NoPrice', N'لیست اقلام BOM بدون قیمت', N'-- تعریف متغیر قیمت ارز (CurrencyID = 257)

DECLARE @CurrencyPrice DECIMAL(18, 2);



SELECT @CurrencyPrice = CAST(Number1 AS DECIMAL(18, 2))

FROM ERPS.erps.GNR3.Currency AS Currency

    LEFT JOIN ERPS.erps.SYS3.FieldValueContainer AS FieldValueContainer

        ON FieldValueContainer.RecordID = Currency.CurrencyID

        AND FieldValueContainer.EntityCode = 240

WHERE CurrencyID = 257;



WITH BaseQuery AS (

    SELECT

        pf.Id,

        pf.ProductId,

        pf.ProductNameGroupId,

        pf.CreatedOnMiladiDateTime,

        pf.CreatedOnShamsiDateTime,

        pf.Comment,

        pg.Name AS ProductNameGroupName,

        product.Code AS ProductCode,

        product.Name AS ProductName

    FROM Bom.ProductFormul pf

    INNER JOIN Inv.Part product ON pf.ProductId = product.Id

    LEFT JOIN Bom.ProductGroup pg ON pf.ProductNameGroupId = pg.Id

),

-- آخرین قیمت هر قطعه (داخلی یا خارجی بر اساس Foreign)

lastPrice AS (

    SELECT

        ipp.PartId,

        ipp.UnitPrice,

        ipp.CreatedOnMiladiDateTime,

        p.[Foreign],

        ROW_NUMBER() OVER(PARTITION BY ipp.PartId ORDER BY ipp.CreatedOnMiladiDateTime DESC) AS rn

    FROM Sup.InquiryPartPrice ipp

    INNER JOIN Inv.Part p ON ipp.PartId = p.Id

    WHERE ipp.Type <> 1675

      AND ipp.PriceStatus <> 2834

      AND ipp.PriceUnitId = CASE

                                WHEN ISNULL(p.[Foreign], 0) = 0 THEN 1   -- داخلی → ریال

                                ELSE 4                                   -- خارجی → یورو

                            END

),

-- محاسبه قیمت هر قطعه BOM (بدون جمع‌زدن روی محصول مادر)

BOMPrice AS (

    SELECT

        Product.Id AS ParentProductId,

        Part.Id AS BOMPartId,

        Part.Code AS BOMPartCode,

        Part.Name AS BOMPartName,

        Part.SaleRate AS BOMSaleRate,

        Part.[Foreign] AS BOMForeign,

        Part.DesignTypeIsRoutine AS BOMIsRoutine,

        Part.EngineeringRoutine AS BOMEngineeringRoutine,

        Part.DataSheetUsage AS BOMDataSheetUsage,

        Part.DataSheet AS BOMDataSheet,

        Part.BrandInDataSheet AS BOMBrandInDataSheet,

        Part.PreciseTool AS BOMPreciseTool,

        Vw_Bom_Product_Items.UsingRate BomUsingRate,

        CASE

            WHEN Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value IS NOT NULL

                 AND ISNULL(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value, 0) > 1

            THEN CAST(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value * ISNULL(Vw_Bom_Product_Items.UsingRate, 1) AS DECIMAL(18,5))

            ELSE CAST(((Vw_Sup_PartPrice_HistoryRial.PartPrice * ISNULL(Vw_Bom_Product_Items.UsingRate, 1)) / Acc_ExchangeRateArchive.FinalPrice) AS DECIMAL(18,5))

        END AS BOMTotalBuyPriceInEuro,

        -- قیمت فروش از روی نرخ فروش قطعه (SalePriceFromRuro)

        CASE

            WHEN Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value IS NOT NULL

                 AND ISNULL(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value, 0) > 1

            THEN CAST(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value * ISNULL(Vw_Bom_Product_Items.UsingRate, 1) AS DECIMAL(18,5)) * ISNULL(Part.SaleRate, 1)

            ELSE CAST(((Vw_Sup_PartPrice_HistoryRial.PartPrice * ISNULL(Vw_Bom_Product_Items.UsingRate, 1)) / Acc_ExchangeRateArchive.FinalPrice) AS DECIMAL(18,5)) * ISNULL(Part.SaleRate, 1)

        END AS BOMSalePriceFromRuro,

        -- قیمت خارجی (در صورت خارجی بودن قطعه)

        IIF(Part.[Foreign] = 0, NULL,

            CASE

                WHEN Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value IS NOT NULL

                     AND ISNULL(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value, 0) > 1

                THEN CAST(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value * ISNULL(Vw_Bom_Product_Items.UsingRate, 1) AS DECIMAL(18,5))

                ELSE CAST(((Vw_Sup_PartPrice_HistoryRial.PartPrice * ISNULL(Vw_Bom_Product_Items.UsingRate, 1)) / Acc_ExchangeRateArchive.FinalPrice) AS DECIMAL(18,5))

            END

        ) AS BOMExternalTotalBuyPriceInEuro,

        -- قیمت واحد داخلی (از lastPrice)

        lp.UnitPrice AS InternalUnitPrice,

        lp.CreatedOnMiladiDateTime AS InternalPriceDate

    FROM Inv.Part AS Product

    INNER JOIN dbo.vw_BomProductItem AS Vw_Bom_Product_Items

        ON Vw_Bom_Product_Items.ProductId = Product.Id

    INNER JOIN Inv.Part AS Part

        ON Part.Id = Vw_Bom_Product_Items.PartItemId

    LEFT JOIN (

        SELECT

            Vw_Sup_PartPrice_History.PriceUnitValue + ISNULL(Vw_Sup_PartPrice_History.TransportationCost, 0) AS PriceUnit_Value,

            Vw_Sup_PartPrice_History.PartId,

            RowNum = ROW_NUMBER() OVER(PARTITION BY Vw_Sup_PartPrice_History.PartId ORDER BY Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime DESC)

        FROM Sup.InquiryPartPrice AS Vw_Sup_PartPrice_History

        WHERE Vw_Sup_PartPrice_History.Type NOT IN (1675)

          AND ISNULL(Vw_Sup_PartPrice_History.PriceStatus, 1) <> 2834

          AND ISNULL(Vw_Sup_PartPrice_History.PriceUnitId, 1) <> 1

          AND Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime >= ''2023-03-21''

    ) AS Vw_Sup_PartPrice_HistoryEurope

        ON Vw_Sup_PartPrice_HistoryEurope.PartId = Part.Id

        AND Vw_Sup_PartPrice_HistoryEurope.RowNum = 1

    LEFT JOIN (

        SELECT

            ISNULL(Vw_Sup_PartPrice_History.UnitPrice, 0) AS PartPrice,

            Vw_Sup_PartPrice_History.PartId,

            Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime,

            RowNum = ROW_NUMBER() OVER(PARTITION BY Vw_Sup_PartPrice_History.PartId ORDER BY Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime DESC)

        FROM Sup.InquiryPartPrice AS Vw_Sup_PartPrice_History

        WHERE Vw_Sup_PartPrice_History.Type NOT IN (1675)

          AND ISNULL(Vw_Sup_PartPrice_History.PriceStatus, 1) <> 2834

          AND ISNULL(Vw_Sup_PartPrice_History.PriceUnitId, 1) = 1

          AND Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime BETWEEN ''2023-03-21'' AND ''2025-03-20''

    ) AS Vw_Sup_PartPrice_HistoryRial

        ON Vw_Sup_PartPrice_HistoryRial.PartId = Part.Id

        AND Vw_Sup_PartPrice_HistoryRial.RowNum = 1

    OUTER APPLY (

        SELECT TOP (1) B.FinalPrice, B.CreatedOnMiladiDateTime

        FROM Acc.ExchangeRateArchive B

        WHERE B.PriceUnitId = 4

          AND B.CreatedOnMiladiDateTime <= Vw_Sup_PartPrice_HistoryRial.CreatedOnMiladiDateTime

        ORDER BY B.CreatedOnMiladiDateTime DESC

    ) AS Acc_ExchangeRateArchive

    LEFT JOIN lastPrice lp ON Part.Id = lp.PartId AND lp.rn = 1

),

-- تعداد اسناد هر قطعه

DocumentCounts AS (

    SELECT PartId, COUNT(*) AS DocumentCount

    FROM [Inv].[PartDocument]

    WHERE Main = 1

    GROUP BY PartId

),

-- اطلاعات اضافی قطعه (اختیاری)

PartExtraInfo AS (

    SELECT

        pe.ProductId,

        pe.Revision AS PartExteraInfoRev,

        pe.InfoType AS PartExteraInfoType

    FROM Inv.PartExtraInfo pe

    INNER JOIN (

        SELECT ProductId, MAX(Revision) AS MaxRevision

        FROM Inv.PartExtraInfo

        GROUP BY ProductId

    ) latest ON pe.ProductId = latest.ProductId AND pe.Revision = latest.MaxRevision

)
--!--mainsection
SELECT
    bq.Id AS ProductFormulId,
    bq.ProductCode,
    bq.ProductName,
    bom.BOMPartCode AS PartCode,
    bom.BOMPartName AS PartName,
    ISNULL(BomUsingRate, 1) AS BomUsingRate,
    pe.PartExteraInfoRev,
    pe.PartExteraInfoType AS PartExteraInfoTypeName,
    pe.PartExteraInfoType,
    bom.BOMIsRoutine AS IsRoutine,
    bq.Comment,
    bom.BOMPartId AS PartId,
    bq.ProductId
FROM BaseQuery bq
INNER JOIN BOMPrice bom ON bq.ProductId = bom.ParentProductId
LEFT JOIN DocumentCounts dc ON bom.BOMPartId = dc.PartId
LEFT JOIN PartExtraInfo pe ON bom.BOMPartId = pe.ProductId
', N'[{"TableName":null,"ColumnName":"ProductFormulId","DisplayName":"ProductFormulId","Alliance":"ProductFormulId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductCode","DisplayName":"کد محصول","Alliance":"ProductCode","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductName","DisplayName":"نام محصول","Alliance":"ProductName","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartCode","DisplayName":"کد قطعه","Alliance":"PartCode","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartName","DisplayName":"نام قطعه","Alliance":"PartName","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"BomUsingRate","DisplayName":"ضریب مصرف","Alliance":"BomUsingRate","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoTypeName","DisplayName":"نوع دیتا شیت","Alliance":"PartExteraInfoTypeName","Address":null,"SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Inv.Enums.PartExtraInfoInfoTypeEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoRev","DisplayName":"نسخه دیتاشیت","Alliance":"PartExteraInfoRev","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"IsRoutine","DisplayName":"روتین هست","Alliance":"IsRoutine","Address":null,"SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"Comment","DisplayName":"کامنت","Alliance":"Comment","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartId","DisplayName":"PartId","Alliance":"PartId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoType","DisplayName":"PartExteraInfoType","Alliance":"PartExteraInfoType","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductId","DisplayName":"ProductId","Alliance":"ProductId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""}]', N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"},{"dataActionName":"defaultMultiSelect","enable":false,"title":"انتخاب چندتایی پیش‌فرض"}]', N'[{"id":"id_ldp_datasheet","title":"نمایش دیتاشیت","dataActionName":"showDataSheet","colorClass":"btn-color-primary","iconClass":"fa fa-note","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    if (!ctx.selectedRow) {\n        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');\n        return;\n    }\n    if (!ctx.selectedRow.partExteraInfoType || ctx.selectedRow.partExteraInfoType == 0) {\n        toastr.warning(''هیچ دیتاشیتی جهت نمایش یافت نشد.'');\n        return;\n    }\n    var optionsEl = '''';\n    for (var i = ctx.selectedRow.partExteraInfoRev; i >= 0; i--) {\n        optionsEl += '' <option value=\"'' + i + ''\">'' + i + ''</option> '';\n    }\n    var $modalContetn = ''<div><select class=\"form-select\" id=\"ldpDatasheetRevSelect\">'' + optionsEl + ''</select></div>'';\n    $.confirm({\n        title: ''انتخاب نسخه دیتاشیت'',\n        content: $modalContetn,\n        type: ''green'',\n        columnClass: ''col-md-6'',\n        typeAnimated: true,\n        buttons: {\n            acc: {\n                text: ''نمایش'',\n                btnClass: ''btn btn-success'',\n                action: function () {\n                    var selectedRev = this.$content.find(''#ldpDatasheetRevSelect'').val();\n                    if (selectedRev === undefined || selectedRev === null || selectedRev === '''') {\n                        selectedRev = ctx.selectedRow.partExteraInfoRev;\n                    }\n                    var partId = ctx.selectedRow.partId;\n                    var revQ = \"rev={''values'':[''\" + selectedRev + \"''],''condition'':''=''}\";\n                    var productQ = \"productId={''values'':[''\" + partId + \"''],''condition'':''=''}\";\n                    switch (ctx.selectedRow.partExteraInfoType) {\n                        case 1:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/partexterainfooilinject?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 2:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoOilFree?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 3:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoDryer?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 4:\n                        case 5:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoPsa?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                    }\n                }\n            },\n            close: {\n                text: ''بستن'',\n                btnClass: ''btn btn-danger'',\n                action: function () {}\n            }\n        }\n    });\n}"},{"id":"id_ldp_techdocs","title":"پیوست مدارک فنی","dataActionName":"showDocuments","colorClass":"btn-color-success","iconClass":"bi-filetype-doc bi","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"async function(ctx) {\n    if (!ctx.selectedRow) {\n        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');\n        return;\n    }\n    var response = await get(''/Panel/Inv/Part/PartDocumentInfo?id='' + ctx.selectedRow.partId);\n    if (!response.isSuccess)\n        return toastr.error(response.message);\n    var rowsHtml = (response.data || []).map(function (x) {\n        return ''<tr>''\n            + ''<td>'' + (x.originalName || '''') + ''</td>''\n            + ''<td>'' + (x.size || '''') + ''</td>''\n            + ''<td>'' + (x.type || '''') + ''</td>''\n            + ''<td>'' + (x.main || '''') + ''</td>''\n            + ''<td>'' + (x.createdByName || '''') + ''</td>''\n            + ''<td>'' + (x.createdOnShamsiDateTime || '''') + ''</td>''\n            + ''<td>'' + (x.modifiedByName || '''') + ''</td>''\n            + ''<td>'' + (x.modifiedDateShamsiDateTime || '''') + ''</td>''\n            + ''</tr>'';\n    }).join('''');\n    var $modalContetn = $(''<table class=\"table table-bordered w-100\">''\n        + ''<thead><tr>''\n        + ''<th>نام مدرک</th><th>سایز</th><th>نوع</th><th>اصلی</th>''\n        + ''<th>ایجاد کننده</th><th>زمان ایجاد</th><th>ویرایش کننده</th><th>زمان ویرایش</th>''\n        + ''</tr></thead><tbody>'' + rowsHtml + ''</tbody></table>'');\n    $.confirm({\n        title: ''پیوست مدارک'',\n        content: $modalContetn,\n        type: ''green'',\n        columnClass: ''col-md-12'',\n        typeAnimated: true,\n        buttons: {\n            close: {\n                text: ''بستن'',\n                btnClass: ''btn btn-danger'',\n                action: function () {}\n            }\n        }\n    });\n}"}]'),
        (N'ListDocumentProduct_Bom_Buy', N'لیست اقلام BOM با قیمت خرید', N'-- تعریف متغیر قیمت ارز (CurrencyID = 257)

DECLARE @CurrencyPrice DECIMAL(18, 2);



SELECT @CurrencyPrice = CAST(Number1 AS DECIMAL(18, 2))

FROM ERPS.erps.GNR3.Currency AS Currency

    LEFT JOIN ERPS.erps.SYS3.FieldValueContainer AS FieldValueContainer

        ON FieldValueContainer.RecordID = Currency.CurrencyID

        AND FieldValueContainer.EntityCode = 240

WHERE CurrencyID = 257;



WITH BaseQuery AS (

    SELECT

        pf.Id,

        pf.ProductId,

        pf.ProductNameGroupId,

        pf.CreatedOnMiladiDateTime,

        pf.CreatedOnShamsiDateTime,

        pf.Comment,

        pg.Name AS ProductNameGroupName,

        product.Code AS ProductCode,

        product.Name AS ProductName

    FROM Bom.ProductFormul pf

    INNER JOIN Inv.Part product ON pf.ProductId = product.Id

    LEFT JOIN Bom.ProductGroup pg ON pf.ProductNameGroupId = pg.Id

),

-- آخرین قیمت هر قطعه (داخلی یا خارجی بر اساس Foreign)

lastPrice AS (

    SELECT

        ipp.PartId,

        ipp.UnitPrice,

        ipp.CreatedOnMiladiDateTime,

        p.[Foreign],

        ROW_NUMBER() OVER(PARTITION BY ipp.PartId ORDER BY ipp.CreatedOnMiladiDateTime DESC) AS rn

    FROM Sup.InquiryPartPrice ipp

    INNER JOIN Inv.Part p ON ipp.PartId = p.Id

    WHERE ipp.Type <> 1675

      AND ipp.PriceStatus <> 2834

      AND ipp.PriceUnitId = CASE

                                WHEN ISNULL(p.[Foreign], 0) = 0 THEN 1   -- داخلی → ریال

                                ELSE 4                                   -- خارجی → یورو

                            END

),

-- محاسبه قیمت هر قطعه BOM (بدون جمع‌زدن روی محصول مادر)

BOMPrice AS (

    SELECT

        Product.Id AS ParentProductId,

        Part.Id AS BOMPartId,

        Part.Code AS BOMPartCode,

        Part.Name AS BOMPartName,

        Part.SaleRate AS BOMSaleRate,

        Part.[Foreign] AS BOMForeign,

        Part.DesignTypeIsRoutine AS BOMIsRoutine,

        Part.EngineeringRoutine AS BOMEngineeringRoutine,

        Part.DataSheetUsage AS BOMDataSheetUsage,

        Part.DataSheet AS BOMDataSheet,

        Part.BrandInDataSheet AS BOMBrandInDataSheet,

        Part.PreciseTool AS BOMPreciseTool,

        Vw_Bom_Product_Items.UsingRate BomUsingRate,

        CASE

            WHEN Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value IS NOT NULL

                 AND ISNULL(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value, 0) > 1

            THEN CAST(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value * ISNULL(Vw_Bom_Product_Items.UsingRate, 1) AS DECIMAL(18,5))

            ELSE CAST(((Vw_Sup_PartPrice_HistoryRial.PartPrice * ISNULL(Vw_Bom_Product_Items.UsingRate, 1)) / Acc_ExchangeRateArchive.FinalPrice) AS DECIMAL(18,5))

        END AS BOMTotalBuyPriceInEuro,

        -- قیمت فروش از روی نرخ فروش قطعه (SalePriceFromRuro)

        CASE

            WHEN Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value IS NOT NULL

                 AND ISNULL(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value, 0) > 1

            THEN CAST(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value * ISNULL(Vw_Bom_Product_Items.UsingRate, 1) AS DECIMAL(18,5)) * ISNULL(Part.SaleRate, 1)

            ELSE CAST(((Vw_Sup_PartPrice_HistoryRial.PartPrice * ISNULL(Vw_Bom_Product_Items.UsingRate, 1)) / Acc_ExchangeRateArchive.FinalPrice) AS DECIMAL(18,5)) * ISNULL(Part.SaleRate, 1)

        END AS BOMSalePriceFromRuro,

        -- قیمت خارجی (در صورت خارجی بودن قطعه)

        IIF(Part.[Foreign] = 0, NULL,

            CASE

                WHEN Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value IS NOT NULL

                     AND ISNULL(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value, 0) > 1

                THEN CAST(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value * ISNULL(Vw_Bom_Product_Items.UsingRate, 1) AS DECIMAL(18,5))

                ELSE CAST(((Vw_Sup_PartPrice_HistoryRial.PartPrice * ISNULL(Vw_Bom_Product_Items.UsingRate, 1)) / Acc_ExchangeRateArchive.FinalPrice) AS DECIMAL(18,5))

            END

        ) AS BOMExternalTotalBuyPriceInEuro,

        -- قیمت واحد داخلی (از lastPrice)

        lp.UnitPrice AS InternalUnitPrice,

        lp.CreatedOnMiladiDateTime AS InternalPriceDate

    FROM Inv.Part AS Product

    INNER JOIN dbo.vw_BomProductItem AS Vw_Bom_Product_Items

        ON Vw_Bom_Product_Items.ProductId = Product.Id

    INNER JOIN Inv.Part AS Part

        ON Part.Id = Vw_Bom_Product_Items.PartItemId

    LEFT JOIN (

        SELECT

            Vw_Sup_PartPrice_History.PriceUnitValue + ISNULL(Vw_Sup_PartPrice_History.TransportationCost, 0) AS PriceUnit_Value,

            Vw_Sup_PartPrice_History.PartId,

            RowNum = ROW_NUMBER() OVER(PARTITION BY Vw_Sup_PartPrice_History.PartId ORDER BY Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime DESC)

        FROM Sup.InquiryPartPrice AS Vw_Sup_PartPrice_History

        WHERE Vw_Sup_PartPrice_History.Type NOT IN (1675)

          AND ISNULL(Vw_Sup_PartPrice_History.PriceStatus, 1) <> 2834

          AND ISNULL(Vw_Sup_PartPrice_History.PriceUnitId, 1) <> 1

          AND Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime >= ''2023-03-21''

    ) AS Vw_Sup_PartPrice_HistoryEurope

        ON Vw_Sup_PartPrice_HistoryEurope.PartId = Part.Id

        AND Vw_Sup_PartPrice_HistoryEurope.RowNum = 1

    LEFT JOIN (

        SELECT

            ISNULL(Vw_Sup_PartPrice_History.UnitPrice, 0) AS PartPrice,

            Vw_Sup_PartPrice_History.PartId,

            Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime,

            RowNum = ROW_NUMBER() OVER(PARTITION BY Vw_Sup_PartPrice_History.PartId ORDER BY Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime DESC)

        FROM Sup.InquiryPartPrice AS Vw_Sup_PartPrice_History

        WHERE Vw_Sup_PartPrice_History.Type NOT IN (1675)

          AND ISNULL(Vw_Sup_PartPrice_History.PriceStatus, 1) <> 2834

          AND ISNULL(Vw_Sup_PartPrice_History.PriceUnitId, 1) = 1

          AND Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime BETWEEN ''2023-03-21'' AND ''2025-03-20''

    ) AS Vw_Sup_PartPrice_HistoryRial

        ON Vw_Sup_PartPrice_HistoryRial.PartId = Part.Id

        AND Vw_Sup_PartPrice_HistoryRial.RowNum = 1

    OUTER APPLY (

        SELECT TOP (1) B.FinalPrice, B.CreatedOnMiladiDateTime

        FROM Acc.ExchangeRateArchive B

        WHERE B.PriceUnitId = 4

          AND B.CreatedOnMiladiDateTime <= Vw_Sup_PartPrice_HistoryRial.CreatedOnMiladiDateTime

        ORDER BY B.CreatedOnMiladiDateTime DESC

    ) AS Acc_ExchangeRateArchive

    LEFT JOIN lastPrice lp ON Part.Id = lp.PartId AND lp.rn = 1

),

-- تعداد اسناد هر قطعه

DocumentCounts AS (

    SELECT PartId, COUNT(*) AS DocumentCount

    FROM [Inv].[PartDocument]

    WHERE Main = 1

    GROUP BY PartId

),

-- اطلاعات اضافی قطعه (اختیاری)

PartExtraInfo AS (

    SELECT

        pe.ProductId,

        pe.Revision AS PartExteraInfoRev,

        pe.InfoType AS PartExteraInfoType

    FROM Inv.PartExtraInfo pe

    INNER JOIN (

        SELECT ProductId, MAX(Revision) AS MaxRevision

        FROM Inv.PartExtraInfo

        GROUP BY ProductId

    ) latest ON pe.ProductId = latest.ProductId AND pe.Revision = latest.MaxRevision

)
--!--mainsection
SELECT
    bq.Id AS ProductFormulId,
    bq.ProductCode,
    bq.ProductName,
    bom.BOMPartCode AS PartCode,
    bom.BOMPartName AS PartName,
    ISNULL(BomUsingRate, 1) AS BomUsingRate,
    CASE WHEN bom.BOMForeign = 1 THEN 0 ELSE ISNULL(bom.InternalUnitPrice, 0) END AS Internal_TotalBuyPrice,
    ROUND(ISNULL(bom.BOMExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0) AS External_TotalBuyPrice,
    CASE
          WHEN bom.BOMForeign = 1 THEN
          ROUND(ISNULL(bom.BOMExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0)
          ELSE ISNULL(bom.InternalUnitPrice, 0)
      END AS BuyPrice,
    ROUND(ISNULL(bom.BOMTotalBuyPriceInEuro, 0), 0) AS BuyPriceInEuro,
    bom.BOMTotalBuyPriceInEuro AS bomTotalBuyPriceInEuro,
    pe.PartExteraInfoRev,
    pe.PartExteraInfoType AS PartExteraInfoTypeName,
    pe.PartExteraInfoType,
    bom.BOMIsRoutine AS IsRoutine,
    bq.Comment,
    bom.BOMPartId AS PartId,
    bq.ProductId
FROM BaseQuery bq
INNER JOIN BOMPrice bom ON bq.ProductId = bom.ParentProductId
LEFT JOIN DocumentCounts dc ON bom.BOMPartId = dc.PartId
LEFT JOIN PartExtraInfo pe ON bom.BOMPartId = pe.ProductId
', N'[{"TableName":null,"ColumnName":"ProductFormulId","DisplayName":"ProductFormulId","Alliance":"ProductFormulId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductCode","DisplayName":"کد محصول","Alliance":"ProductCode","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductName","DisplayName":"نام محصول","Alliance":"ProductName","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartCode","DisplayName":"کد قطعه","Alliance":"PartCode","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartName","DisplayName":"نام قطعه","Alliance":"PartName","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"BomUsingRate","DisplayName":"ضریب مصرف","Alliance":"BomUsingRate","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"BuyPrice","DisplayName":"قیمت واحد","Alliance":"BuyPrice","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"Internal_TotalBuyPrice","DisplayName":"مجموعه قیمت داخلی","Alliance":"Internal_TotalBuyPrice","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"External_TotalBuyPrice","DisplayName":"مجموعه قیمت خارجی","Alliance":"External_TotalBuyPrice","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"BuyPriceInEuro","DisplayName":"قیمت واحد خرید (یورو)","Alliance":"BuyPriceInEuro","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"bomTotalBuyPriceInEuro","DisplayName":"مجموع قیمت خرید (یورو)","Alliance":"bomTotalBuyPriceInEuro","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoTypeName","DisplayName":"نوع دیتا شیت","Alliance":"PartExteraInfoTypeName","Address":null,"SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Inv.Enums.PartExtraInfoInfoTypeEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoRev","DisplayName":"نسخه دیتاشیت","Alliance":"PartExteraInfoRev","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"IsRoutine","DisplayName":"روتین هست","Alliance":"IsRoutine","Address":null,"SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"Comment","DisplayName":"کامنت","Alliance":"Comment","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartId","DisplayName":"PartId","Alliance":"PartId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoType","DisplayName":"PartExteraInfoType","Alliance":"PartExteraInfoType","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductId","DisplayName":"ProductId","Alliance":"ProductId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""}]', N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"},{"dataActionName":"defaultMultiSelect","enable":false,"title":"انتخاب چندتایی پیش‌فرض"}]', N'[{"id":"id_ldp_datasheet","title":"نمایش دیتاشیت","dataActionName":"showDataSheet","colorClass":"btn-color-primary","iconClass":"fa fa-note","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    if (!ctx.selectedRow) {\n        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');\n        return;\n    }\n    if (!ctx.selectedRow.partExteraInfoType || ctx.selectedRow.partExteraInfoType == 0) {\n        toastr.warning(''هیچ دیتاشیتی جهت نمایش یافت نشد.'');\n        return;\n    }\n    var optionsEl = '''';\n    for (var i = ctx.selectedRow.partExteraInfoRev; i >= 0; i--) {\n        optionsEl += '' <option value=\"'' + i + ''\">'' + i + ''</option> '';\n    }\n    var $modalContetn = ''<div><select class=\"form-select\" id=\"ldpDatasheetRevSelect\">'' + optionsEl + ''</select></div>'';\n    $.confirm({\n        title: ''انتخاب نسخه دیتاشیت'',\n        content: $modalContetn,\n        type: ''green'',\n        columnClass: ''col-md-6'',\n        typeAnimated: true,\n        buttons: {\n            acc: {\n                text: ''نمایش'',\n                btnClass: ''btn btn-success'',\n                action: function () {\n                    var selectedRev = this.$content.find(''#ldpDatasheetRevSelect'').val();\n                    if (selectedRev === undefined || selectedRev === null || selectedRev === '''') {\n                        selectedRev = ctx.selectedRow.partExteraInfoRev;\n                    }\n                    var partId = ctx.selectedRow.partId;\n                    var revQ = \"rev={''values'':[''\" + selectedRev + \"''],''condition'':''=''}\";\n                    var productQ = \"productId={''values'':[''\" + partId + \"''],''condition'':''=''}\";\n                    switch (ctx.selectedRow.partExteraInfoType) {\n                        case 1:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/partexterainfooilinject?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 2:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoOilFree?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 3:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoDryer?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 4:\n                        case 5:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoPsa?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                    }\n                }\n            },\n            close: {\n                text: ''بستن'',\n                btnClass: ''btn btn-danger'',\n                action: function () {}\n            }\n        }\n    });\n}"},{"id":"id_ldp_techdocs","title":"پیوست مدارک فنی","dataActionName":"showDocuments","colorClass":"btn-color-success","iconClass":"bi-filetype-doc bi","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"async function(ctx) {\n    if (!ctx.selectedRow) {\n        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');\n        return;\n    }\n    var response = await get(''/Panel/Inv/Part/PartDocumentInfo?id='' + ctx.selectedRow.partId);\n    if (!response.isSuccess)\n        return toastr.error(response.message);\n    var rowsHtml = (response.data || []).map(function (x) {\n        return ''<tr>''\n            + ''<td>'' + (x.originalName || '''') + ''</td>''\n            + ''<td>'' + (x.size || '''') + ''</td>''\n            + ''<td>'' + (x.type || '''') + ''</td>''\n            + ''<td>'' + (x.main || '''') + ''</td>''\n            + ''<td>'' + (x.createdByName || '''') + ''</td>''\n            + ''<td>'' + (x.createdOnShamsiDateTime || '''') + ''</td>''\n            + ''<td>'' + (x.modifiedByName || '''') + ''</td>''\n            + ''<td>'' + (x.modifiedDateShamsiDateTime || '''') + ''</td>''\n            + ''</tr>'';\n    }).join('''');\n    var $modalContetn = $(''<table class=\"table table-bordered w-100\">''\n        + ''<thead><tr>''\n        + ''<th>نام مدرک</th><th>سایز</th><th>نوع</th><th>اصلی</th>''\n        + ''<th>ایجاد کننده</th><th>زمان ایجاد</th><th>ویرایش کننده</th><th>زمان ویرایش</th>''\n        + ''</tr></thead><tbody>'' + rowsHtml + ''</tbody></table>'');\n    $.confirm({\n        title: ''پیوست مدارک'',\n        content: $modalContetn,\n        type: ''green'',\n        columnClass: ''col-md-12'',\n        typeAnimated: true,\n        buttons: {\n            close: {\n                text: ''بستن'',\n                btnClass: ''btn btn-danger'',\n                action: function () {}\n            }\n        }\n    });\n}"}]'),
        (N'ListDocumentProduct_Bom_Sale', N'لیست اقلام BOM با قیمت فروش', N'-- تعریف متغیر قیمت ارز (CurrencyID = 257)

DECLARE @CurrencyPrice DECIMAL(18, 2);



SELECT @CurrencyPrice = CAST(Number1 AS DECIMAL(18, 2))

FROM ERPS.erps.GNR3.Currency AS Currency

    LEFT JOIN ERPS.erps.SYS3.FieldValueContainer AS FieldValueContainer

        ON FieldValueContainer.RecordID = Currency.CurrencyID

        AND FieldValueContainer.EntityCode = 240

WHERE CurrencyID = 257;



WITH BaseQuery AS (

    SELECT

        pf.Id,

        pf.ProductId,

        pf.ProductNameGroupId,

        pf.CreatedOnMiladiDateTime,

        pf.CreatedOnShamsiDateTime,

        pf.Comment,

        pg.Name AS ProductNameGroupName,

        product.Code AS ProductCode,

        product.Name AS ProductName

    FROM Bom.ProductFormul pf

    INNER JOIN Inv.Part product ON pf.ProductId = product.Id

    LEFT JOIN Bom.ProductGroup pg ON pf.ProductNameGroupId = pg.Id

),

-- آخرین قیمت هر قطعه (داخلی یا خارجی بر اساس Foreign)

lastPrice AS (

    SELECT

        ipp.PartId,

        ipp.UnitPrice,

        ipp.CreatedOnMiladiDateTime,

        p.[Foreign],

        ROW_NUMBER() OVER(PARTITION BY ipp.PartId ORDER BY ipp.CreatedOnMiladiDateTime DESC) AS rn

    FROM Sup.InquiryPartPrice ipp

    INNER JOIN Inv.Part p ON ipp.PartId = p.Id

    WHERE ipp.Type <> 1675

      AND ipp.PriceStatus <> 2834

      AND ipp.PriceUnitId = CASE

                                WHEN ISNULL(p.[Foreign], 0) = 0 THEN 1   -- داخلی → ریال

                                ELSE 4                                   -- خارجی → یورو

                            END

),

-- محاسبه قیمت هر قطعه BOM (بدون جمع‌زدن روی محصول مادر)

BOMPrice AS (

    SELECT

        Product.Id AS ParentProductId,

        Part.Id AS BOMPartId,

        Part.Code AS BOMPartCode,

        Part.Name AS BOMPartName,

        Part.SaleRate AS BOMSaleRate,

        Part.[Foreign] AS BOMForeign,

        Part.DesignTypeIsRoutine AS BOMIsRoutine,

        Part.EngineeringRoutine AS BOMEngineeringRoutine,

        Part.DataSheetUsage AS BOMDataSheetUsage,

        Part.DataSheet AS BOMDataSheet,

        Part.BrandInDataSheet AS BOMBrandInDataSheet,

        Part.PreciseTool AS BOMPreciseTool,

        Vw_Bom_Product_Items.UsingRate BomUsingRate,

        CASE

            WHEN Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value IS NOT NULL

                 AND ISNULL(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value, 0) > 1

            THEN CAST(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value * ISNULL(Vw_Bom_Product_Items.UsingRate, 1) AS DECIMAL(18,5))

            ELSE CAST(((Vw_Sup_PartPrice_HistoryRial.PartPrice * ISNULL(Vw_Bom_Product_Items.UsingRate, 1)) / Acc_ExchangeRateArchive.FinalPrice) AS DECIMAL(18,5))

        END AS BOMTotalBuyPriceInEuro,

        -- قیمت فروش از روی نرخ فروش قطعه (SalePriceFromRuro)

        CASE

            WHEN Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value IS NOT NULL

                 AND ISNULL(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value, 0) > 1

            THEN CAST(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value * ISNULL(Vw_Bom_Product_Items.UsingRate, 1) AS DECIMAL(18,5)) * ISNULL(Part.SaleRate, 1)

            ELSE CAST(((Vw_Sup_PartPrice_HistoryRial.PartPrice * ISNULL(Vw_Bom_Product_Items.UsingRate, 1)) / Acc_ExchangeRateArchive.FinalPrice) AS DECIMAL(18,5)) * ISNULL(Part.SaleRate, 1)

        END AS BOMSalePriceFromRuro,

        -- قیمت خارجی (در صورت خارجی بودن قطعه)

        IIF(Part.[Foreign] = 0, NULL,

            CASE

                WHEN Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value IS NOT NULL

                     AND ISNULL(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value, 0) > 1

                THEN CAST(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value * ISNULL(Vw_Bom_Product_Items.UsingRate, 1) AS DECIMAL(18,5))

                ELSE CAST(((Vw_Sup_PartPrice_HistoryRial.PartPrice * ISNULL(Vw_Bom_Product_Items.UsingRate, 1)) / Acc_ExchangeRateArchive.FinalPrice) AS DECIMAL(18,5))

            END

        ) AS BOMExternalTotalBuyPriceInEuro,

        -- قیمت واحد داخلی (از lastPrice)

        lp.UnitPrice AS InternalUnitPrice,

        lp.CreatedOnMiladiDateTime AS InternalPriceDate

    FROM Inv.Part AS Product

    INNER JOIN dbo.vw_BomProductItem AS Vw_Bom_Product_Items

        ON Vw_Bom_Product_Items.ProductId = Product.Id

    INNER JOIN Inv.Part AS Part

        ON Part.Id = Vw_Bom_Product_Items.PartItemId

    LEFT JOIN (

        SELECT

            Vw_Sup_PartPrice_History.PriceUnitValue + ISNULL(Vw_Sup_PartPrice_History.TransportationCost, 0) AS PriceUnit_Value,

            Vw_Sup_PartPrice_History.PartId,

            RowNum = ROW_NUMBER() OVER(PARTITION BY Vw_Sup_PartPrice_History.PartId ORDER BY Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime DESC)

        FROM Sup.InquiryPartPrice AS Vw_Sup_PartPrice_History

        WHERE Vw_Sup_PartPrice_History.Type NOT IN (1675)

          AND ISNULL(Vw_Sup_PartPrice_History.PriceStatus, 1) <> 2834

          AND ISNULL(Vw_Sup_PartPrice_History.PriceUnitId, 1) <> 1

          AND Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime >= ''2023-03-21''

    ) AS Vw_Sup_PartPrice_HistoryEurope

        ON Vw_Sup_PartPrice_HistoryEurope.PartId = Part.Id

        AND Vw_Sup_PartPrice_HistoryEurope.RowNum = 1

    LEFT JOIN (

        SELECT

            ISNULL(Vw_Sup_PartPrice_History.UnitPrice, 0) AS PartPrice,

            Vw_Sup_PartPrice_History.PartId,

            Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime,

            RowNum = ROW_NUMBER() OVER(PARTITION BY Vw_Sup_PartPrice_History.PartId ORDER BY Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime DESC)

        FROM Sup.InquiryPartPrice AS Vw_Sup_PartPrice_History

        WHERE Vw_Sup_PartPrice_History.Type NOT IN (1675)

          AND ISNULL(Vw_Sup_PartPrice_History.PriceStatus, 1) <> 2834

          AND ISNULL(Vw_Sup_PartPrice_History.PriceUnitId, 1) = 1

          AND Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime BETWEEN ''2023-03-21'' AND ''2025-03-20''

    ) AS Vw_Sup_PartPrice_HistoryRial

        ON Vw_Sup_PartPrice_HistoryRial.PartId = Part.Id

        AND Vw_Sup_PartPrice_HistoryRial.RowNum = 1

    OUTER APPLY (

        SELECT TOP (1) B.FinalPrice, B.CreatedOnMiladiDateTime

        FROM Acc.ExchangeRateArchive B

        WHERE B.PriceUnitId = 4

          AND B.CreatedOnMiladiDateTime <= Vw_Sup_PartPrice_HistoryRial.CreatedOnMiladiDateTime

        ORDER BY B.CreatedOnMiladiDateTime DESC

    ) AS Acc_ExchangeRateArchive

    LEFT JOIN lastPrice lp ON Part.Id = lp.PartId AND lp.rn = 1

),

-- تعداد اسناد هر قطعه

DocumentCounts AS (

    SELECT PartId, COUNT(*) AS DocumentCount

    FROM [Inv].[PartDocument]

    WHERE Main = 1

    GROUP BY PartId

),

-- اطلاعات اضافی قطعه (اختیاری)

PartExtraInfo AS (

    SELECT

        pe.ProductId,

        pe.Revision AS PartExteraInfoRev,

        pe.InfoType AS PartExteraInfoType

    FROM Inv.PartExtraInfo pe

    INNER JOIN (

        SELECT ProductId, MAX(Revision) AS MaxRevision

        FROM Inv.PartExtraInfo

        GROUP BY ProductId

    ) latest ON pe.ProductId = latest.ProductId AND pe.Revision = latest.MaxRevision

)
--!--mainsection
SELECT
    bq.Id AS ProductFormulId,
    bq.ProductCode,
    bq.ProductName,
    bom.BOMPartCode AS PartCode,
    bom.BOMPartName AS PartName,
    ISNULL(BomUsingRate, 1) AS BomUsingRate,
    ROUND(ROUND(ISNULL(bom.BOMTotalBuyPriceInEuro, 0), 0) * @CurrencyPrice, 0) AS SalePriceRateAzad,
    @CurrencyPrice AS CurrencyRateAzad,
    CASE
        WHEN (ROUND(ISNULL(bom.BOMExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0) + ISNULL(bom.InternalUnitPrice, 0)) * ISNULL(bom.BOMSaleRate, 1) > ROUND(ISNULL(bom.BOMSalePriceFromRuro, 0) * @CurrencyPrice, 0)
        THEN (ROUND(ISNULL(bom.BOMExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0) + ISNULL(bom.InternalUnitPrice, 0)) * ISNULL(bom.BOMSaleRate, 1)
        ELSE ROUND(ISNULL(bom.BOMSalePriceFromRuro, 0) * @CurrencyPrice, 0)
    END AS SalePrice,
    pe.PartExteraInfoRev,
    pe.PartExteraInfoType AS PartExteraInfoTypeName,
    pe.PartExteraInfoType,
    bom.BOMIsRoutine AS IsRoutine,
    bq.Comment,
    bom.BOMPartId AS PartId,
    bq.ProductId
FROM BaseQuery bq
INNER JOIN BOMPrice bom ON bq.ProductId = bom.ParentProductId
LEFT JOIN DocumentCounts dc ON bom.BOMPartId = dc.PartId
LEFT JOIN PartExtraInfo pe ON bom.BOMPartId = pe.ProductId
', N'[{"TableName":null,"ColumnName":"ProductFormulId","DisplayName":"ProductFormulId","Alliance":"ProductFormulId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductCode","DisplayName":"کد محصول","Alliance":"ProductCode","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductName","DisplayName":"نام محصول","Alliance":"ProductName","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartCode","DisplayName":"کد قطعه","Alliance":"PartCode","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartName","DisplayName":"نام قطعه","Alliance":"PartName","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"BomUsingRate","DisplayName":"ضریب مصرف","Alliance":"BomUsingRate","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"SalePriceRateAzad","DisplayName":"مجموع قیمت(نرخ آزاد)","Alliance":"SalePriceRateAzad","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"CurrencyRateAzad","DisplayName":"نرخ ارز آزاد","Alliance":"CurrencyRateAzad","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"SalePrice","DisplayName":"مجموع قیمت فروش(نرخ آزاد)","Alliance":"SalePrice","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoTypeName","DisplayName":"نوع دیتا شیت","Alliance":"PartExteraInfoTypeName","Address":null,"SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Inv.Enums.PartExtraInfoInfoTypeEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoRev","DisplayName":"نسخه دیتاشیت","Alliance":"PartExteraInfoRev","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"IsRoutine","DisplayName":"روتین هست","Alliance":"IsRoutine","Address":null,"SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"Comment","DisplayName":"کامنت","Alliance":"Comment","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartId","DisplayName":"PartId","Alliance":"PartId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoType","DisplayName":"PartExteraInfoType","Alliance":"PartExteraInfoType","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductId","DisplayName":"ProductId","Alliance":"ProductId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""}]', N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"},{"dataActionName":"defaultMultiSelect","enable":false,"title":"انتخاب چندتایی پیش‌فرض"}]', N'[{"id":"id_ldp_datasheet","title":"نمایش دیتاشیت","dataActionName":"showDataSheet","colorClass":"btn-color-primary","iconClass":"fa fa-note","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    if (!ctx.selectedRow) {\n        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');\n        return;\n    }\n    if (!ctx.selectedRow.partExteraInfoType || ctx.selectedRow.partExteraInfoType == 0) {\n        toastr.warning(''هیچ دیتاشیتی جهت نمایش یافت نشد.'');\n        return;\n    }\n    var optionsEl = '''';\n    for (var i = ctx.selectedRow.partExteraInfoRev; i >= 0; i--) {\n        optionsEl += '' <option value=\"'' + i + ''\">'' + i + ''</option> '';\n    }\n    var $modalContetn = ''<div><select class=\"form-select\" id=\"ldpDatasheetRevSelect\">'' + optionsEl + ''</select></div>'';\n    $.confirm({\n        title: ''انتخاب نسخه دیتاشیت'',\n        content: $modalContetn,\n        type: ''green'',\n        columnClass: ''col-md-6'',\n        typeAnimated: true,\n        buttons: {\n            acc: {\n                text: ''نمایش'',\n                btnClass: ''btn btn-success'',\n                action: function () {\n                    var selectedRev = this.$content.find(''#ldpDatasheetRevSelect'').val();\n                    if (selectedRev === undefined || selectedRev === null || selectedRev === '''') {\n                        selectedRev = ctx.selectedRow.partExteraInfoRev;\n                    }\n                    var partId = ctx.selectedRow.partId;\n                    var revQ = \"rev={''values'':[''\" + selectedRev + \"''],''condition'':''=''}\";\n                    var productQ = \"productId={''values'':[''\" + partId + \"''],''condition'':''=''}\";\n                    switch (ctx.selectedRow.partExteraInfoType) {\n                        case 1:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/partexterainfooilinject?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 2:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoOilFree?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 3:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoDryer?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 4:\n                        case 5:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoPsa?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                    }\n                }\n            },\n            close: {\n                text: ''بستن'',\n                btnClass: ''btn btn-danger'',\n                action: function () {}\n            }\n        }\n    });\n}"},{"id":"id_ldp_techdocs","title":"پیوست مدارک فنی","dataActionName":"showDocuments","colorClass":"btn-color-success","iconClass":"bi-filetype-doc bi","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"async function(ctx) {\n    if (!ctx.selectedRow) {\n        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');\n        return;\n    }\n    var response = await get(''/Panel/Inv/Part/PartDocumentInfo?id='' + ctx.selectedRow.partId);\n    if (!response.isSuccess)\n        return toastr.error(response.message);\n    var rowsHtml = (response.data || []).map(function (x) {\n        return ''<tr>''\n            + ''<td>'' + (x.originalName || '''') + ''</td>''\n            + ''<td>'' + (x.size || '''') + ''</td>''\n            + ''<td>'' + (x.type || '''') + ''</td>''\n            + ''<td>'' + (x.main || '''') + ''</td>''\n            + ''<td>'' + (x.createdByName || '''') + ''</td>''\n            + ''<td>'' + (x.createdOnShamsiDateTime || '''') + ''</td>''\n            + ''<td>'' + (x.modifiedByName || '''') + ''</td>''\n            + ''<td>'' + (x.modifiedDateShamsiDateTime || '''') + ''</td>''\n            + ''</tr>'';\n    }).join('''');\n    var $modalContetn = $(''<table class=\"table table-bordered w-100\">''\n        + ''<thead><tr>''\n        + ''<th>نام مدرک</th><th>سایز</th><th>نوع</th><th>اصلی</th>''\n        + ''<th>ایجاد کننده</th><th>زمان ایجاد</th><th>ویرایش کننده</th><th>زمان ویرایش</th>''\n        + ''</tr></thead><tbody>'' + rowsHtml + ''</tbody></table>'');\n    $.confirm({\n        title: ''پیوست مدارک'',\n        content: $modalContetn,\n        type: ''green'',\n        columnClass: ''col-md-12'',\n        typeAnimated: true,\n        buttons: {\n            close: {\n                text: ''بستن'',\n                btnClass: ''btn btn-danger'',\n                action: function () {}\n            }\n        }\n    });\n}"}]'),
        (N'vw_ListDocumentProductPriceBom', N'لیست اقلام BOM با قیمت (خرید و فروش)', N'-- تعریف متغیر قیمت ارز (CurrencyID = 257)

DECLARE @CurrencyPrice DECIMAL(18, 2);



SELECT @CurrencyPrice = CAST(Number1 AS DECIMAL(18, 2))

FROM ERPS.erps.GNR3.Currency AS Currency

    LEFT JOIN ERPS.erps.SYS3.FieldValueContainer AS FieldValueContainer

        ON FieldValueContainer.RecordID = Currency.CurrencyID

        AND FieldValueContainer.EntityCode = 240

WHERE CurrencyID = 257;



WITH BaseQuery AS (

    SELECT

        pf.Id,

        pf.ProductId,

        pf.ProductNameGroupId,

        pf.CreatedOnMiladiDateTime,

        pf.CreatedOnShamsiDateTime,

        pf.Comment,

        pg.Name AS ProductNameGroupName,

        product.Code AS ProductCode,

        product.Name AS ProductName

    FROM Bom.ProductFormul pf

    INNER JOIN Inv.Part product ON pf.ProductId = product.Id

    LEFT JOIN Bom.ProductGroup pg ON pf.ProductNameGroupId = pg.Id

),

-- آخرین قیمت هر قطعه (داخلی یا خارجی بر اساس Foreign)

lastPrice AS (

    SELECT

        ipp.PartId,

        ipp.UnitPrice,

        ipp.CreatedOnMiladiDateTime,

        p.[Foreign],

        ROW_NUMBER() OVER(PARTITION BY ipp.PartId ORDER BY ipp.CreatedOnMiladiDateTime DESC) AS rn

    FROM Sup.InquiryPartPrice ipp

    INNER JOIN Inv.Part p ON ipp.PartId = p.Id

    WHERE ipp.Type <> 1675

      AND ipp.PriceStatus <> 2834

      AND ipp.PriceUnitId = CASE

                                WHEN ISNULL(p.[Foreign], 0) = 0 THEN 1   -- داخلی → ریال

                                ELSE 4                                   -- خارجی → یورو

                            END

),

-- محاسبه قیمت هر قطعه BOM (بدون جمع‌زدن روی محصول مادر)

BOMPrice AS (

    SELECT

        Product.Id AS ParentProductId,

        Part.Id AS BOMPartId,

        Part.Code AS BOMPartCode,

        Part.Name AS BOMPartName,

        Part.SaleRate AS BOMSaleRate,

        Part.[Foreign] AS BOMForeign,

        Part.DesignTypeIsRoutine AS BOMIsRoutine,

        Part.EngineeringRoutine AS BOMEngineeringRoutine,

        Part.DataSheetUsage AS BOMDataSheetUsage,

        Part.DataSheet AS BOMDataSheet,

        Part.BrandInDataSheet AS BOMBrandInDataSheet,

        Part.PreciseTool AS BOMPreciseTool,

        Vw_Bom_Product_Items.UsingRate BomUsingRate,

        CASE

            WHEN Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value IS NOT NULL

                 AND ISNULL(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value, 0) > 1

            THEN CAST(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value * ISNULL(Vw_Bom_Product_Items.UsingRate, 1) AS DECIMAL(18,5))

            ELSE CAST(((Vw_Sup_PartPrice_HistoryRial.PartPrice * ISNULL(Vw_Bom_Product_Items.UsingRate, 1)) / Acc_ExchangeRateArchive.FinalPrice) AS DECIMAL(18,5))

        END AS BOMTotalBuyPriceInEuro,

        -- قیمت فروش از روی نرخ فروش قطعه (SalePriceFromRuro)

        CASE

            WHEN Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value IS NOT NULL

                 AND ISNULL(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value, 0) > 1

            THEN CAST(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value * ISNULL(Vw_Bom_Product_Items.UsingRate, 1) AS DECIMAL(18,5)) * ISNULL(Part.SaleRate, 1)

            ELSE CAST(((Vw_Sup_PartPrice_HistoryRial.PartPrice * ISNULL(Vw_Bom_Product_Items.UsingRate, 1)) / Acc_ExchangeRateArchive.FinalPrice) AS DECIMAL(18,5)) * ISNULL(Part.SaleRate, 1)

        END AS BOMSalePriceFromRuro,

        -- قیمت خارجی (در صورت خارجی بودن قطعه)

        IIF(Part.[Foreign] = 0, NULL,

            CASE

                WHEN Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value IS NOT NULL

                     AND ISNULL(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value, 0) > 1

                THEN CAST(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value * ISNULL(Vw_Bom_Product_Items.UsingRate, 1) AS DECIMAL(18,5))

                ELSE CAST(((Vw_Sup_PartPrice_HistoryRial.PartPrice * ISNULL(Vw_Bom_Product_Items.UsingRate, 1)) / Acc_ExchangeRateArchive.FinalPrice) AS DECIMAL(18,5))

            END

        ) AS BOMExternalTotalBuyPriceInEuro,

        -- قیمت واحد داخلی (از lastPrice)

        lp.UnitPrice AS InternalUnitPrice,

        lp.CreatedOnMiladiDateTime AS InternalPriceDate

    FROM Inv.Part AS Product

    INNER JOIN dbo.vw_BomProductItem AS Vw_Bom_Product_Items

        ON Vw_Bom_Product_Items.ProductId = Product.Id

    INNER JOIN Inv.Part AS Part

        ON Part.Id = Vw_Bom_Product_Items.PartItemId

    LEFT JOIN (

        SELECT

            Vw_Sup_PartPrice_History.PriceUnitValue + ISNULL(Vw_Sup_PartPrice_History.TransportationCost, 0) AS PriceUnit_Value,

            Vw_Sup_PartPrice_History.PartId,

            RowNum = ROW_NUMBER() OVER(PARTITION BY Vw_Sup_PartPrice_History.PartId ORDER BY Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime DESC)

        FROM Sup.InquiryPartPrice AS Vw_Sup_PartPrice_History

        WHERE Vw_Sup_PartPrice_History.Type NOT IN (1675)

          AND ISNULL(Vw_Sup_PartPrice_History.PriceStatus, 1) <> 2834

          AND ISNULL(Vw_Sup_PartPrice_History.PriceUnitId, 1) <> 1

          AND Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime >= ''2023-03-21''

    ) AS Vw_Sup_PartPrice_HistoryEurope

        ON Vw_Sup_PartPrice_HistoryEurope.PartId = Part.Id

        AND Vw_Sup_PartPrice_HistoryEurope.RowNum = 1

    LEFT JOIN (

        SELECT

            ISNULL(Vw_Sup_PartPrice_History.UnitPrice, 0) AS PartPrice,

            Vw_Sup_PartPrice_History.PartId,

            Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime,

            RowNum = ROW_NUMBER() OVER(PARTITION BY Vw_Sup_PartPrice_History.PartId ORDER BY Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime DESC)

        FROM Sup.InquiryPartPrice AS Vw_Sup_PartPrice_History

        WHERE Vw_Sup_PartPrice_History.Type NOT IN (1675)

          AND ISNULL(Vw_Sup_PartPrice_History.PriceStatus, 1) <> 2834

          AND ISNULL(Vw_Sup_PartPrice_History.PriceUnitId, 1) = 1

          AND Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime BETWEEN ''2023-03-21'' AND ''2025-03-20''

    ) AS Vw_Sup_PartPrice_HistoryRial

        ON Vw_Sup_PartPrice_HistoryRial.PartId = Part.Id

        AND Vw_Sup_PartPrice_HistoryRial.RowNum = 1

    OUTER APPLY (

        SELECT TOP (1) B.FinalPrice, B.CreatedOnMiladiDateTime

        FROM Acc.ExchangeRateArchive B

        WHERE B.PriceUnitId = 4

          AND B.CreatedOnMiladiDateTime <= Vw_Sup_PartPrice_HistoryRial.CreatedOnMiladiDateTime

        ORDER BY B.CreatedOnMiladiDateTime DESC

    ) AS Acc_ExchangeRateArchive

    LEFT JOIN lastPrice lp ON Part.Id = lp.PartId AND lp.rn = 1

),

-- تعداد اسناد هر قطعه

DocumentCounts AS (

    SELECT PartId, COUNT(*) AS DocumentCount

    FROM [Inv].[PartDocument]

    WHERE Main = 1

    GROUP BY PartId

),

-- اطلاعات اضافی قطعه (اختیاری)

PartExtraInfo AS (

    SELECT

        pe.ProductId,

        pe.Revision AS PartExteraInfoRev,

        pe.InfoType AS PartExteraInfoType

    FROM Inv.PartExtraInfo pe

    INNER JOIN (

        SELECT ProductId, MAX(Revision) AS MaxRevision

        FROM Inv.PartExtraInfo

        GROUP BY ProductId

    ) latest ON pe.ProductId = latest.ProductId AND pe.Revision = latest.MaxRevision

)
--!--mainsection
SELECT
    bq.Id AS ProductFormulId,
    bq.ProductCode,
    bq.ProductName,
    bom.BOMPartCode AS PartCode,
    bom.BOMPartName AS PartName,
    ISNULL(BomUsingRate, 1) AS BomUsingRate,
    CASE WHEN bom.BOMForeign = 1 THEN 0 ELSE ISNULL(bom.InternalUnitPrice, 0) END AS Internal_TotalBuyPrice,
    ROUND(ISNULL(bom.BOMExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0) AS External_TotalBuyPrice,
    CASE
          WHEN bom.BOMForeign = 1 THEN
          ROUND(ISNULL(bom.BOMExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0)
          ELSE ISNULL(bom.InternalUnitPrice, 0)
      END AS BuyPrice,
    ROUND(ISNULL(bom.BOMTotalBuyPriceInEuro, 0), 0) AS BuyPriceInEuro,
    bom.BOMTotalBuyPriceInEuro AS bomTotalBuyPriceInEuro,
    ROUND(ROUND(ISNULL(bom.BOMTotalBuyPriceInEuro, 0), 0) * @CurrencyPrice, 0) AS SalePriceRateAzad,
    @CurrencyPrice AS CurrencyRateAzad,
    CASE
        WHEN (ROUND(ISNULL(bom.BOMExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0) + ISNULL(bom.InternalUnitPrice, 0)) * ISNULL(bom.BOMSaleRate, 1) > ROUND(ISNULL(bom.BOMSalePriceFromRuro, 0) * @CurrencyPrice, 0)
        THEN (ROUND(ISNULL(bom.BOMExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0) + ISNULL(bom.InternalUnitPrice, 0)) * ISNULL(bom.BOMSaleRate, 1)
        ELSE ROUND(ISNULL(bom.BOMSalePriceFromRuro, 0) * @CurrencyPrice, 0)
    END AS SalePrice,
    pe.PartExteraInfoRev,
    pe.PartExteraInfoType AS PartExteraInfoTypeName,
    pe.PartExteraInfoType,
    bom.BOMIsRoutine AS IsRoutine,
    bq.Comment,
    bom.BOMPartId AS PartId,
    bq.ProductId
FROM BaseQuery bq
INNER JOIN BOMPrice bom ON bq.ProductId = bom.ParentProductId
LEFT JOIN DocumentCounts dc ON bom.BOMPartId = dc.PartId
LEFT JOIN PartExtraInfo pe ON bom.BOMPartId = pe.ProductId
', N'[{"TableName":null,"ColumnName":"ProductFormulId","DisplayName":"ProductFormulId","Alliance":"ProductFormulId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":true,"SystemType":6,"SortDirection":1,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductCode","DisplayName":"کد محصول","Alliance":"ProductCode","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductName","DisplayName":"نام محصول","Alliance":"ProductName","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartCode","DisplayName":"کد قطعه","Alliance":"PartCode","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartName","DisplayName":"نام قطعه","Alliance":"PartName","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"BomUsingRate","DisplayName":"ضریب مصرف","Alliance":"BomUsingRate","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"BuyPrice","DisplayName":"قیمت واحد","Alliance":"BuyPrice","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"Internal_TotalBuyPrice","DisplayName":"مجموعه قیمت داخلی","Alliance":"Internal_TotalBuyPrice","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"External_TotalBuyPrice","DisplayName":"مجموعه قیمت خارجی","Alliance":"External_TotalBuyPrice","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":130,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"BuyPriceInEuro","DisplayName":"قیمت واحد خرید (یورو)","Alliance":"BuyPriceInEuro","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"bomTotalBuyPriceInEuro","DisplayName":"مجموع قیمت خرید (یورو)","Alliance":"bomTotalBuyPriceInEuro","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"SalePriceRateAzad","DisplayName":"مجموع قیمت(نرخ آزاد)","Alliance":"SalePriceRateAzad","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":150,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"CurrencyRateAzad","DisplayName":"نرخ ارز آزاد","Alliance":"CurrencyRateAzad","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"SalePrice","DisplayName":"مجموع قیمت فروش(نرخ آزاد)","Alliance":"SalePrice","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":160,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoTypeName","DisplayName":"نوع دیتا شیت","Alliance":"PartExteraInfoTypeName","Address":null,"SystemTypeName":"Select","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":8,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":{"ListOptions":[],"TypeOption":3,"SystemTypeName":"Entities.App.Inv.Enums.PartExtraInfoInfoTypeEnum"},"Aggregate":null,"Render":"function(data, type, row) { return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":110,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoRev","DisplayName":"نسخه دیتاشیت","Alliance":"PartExteraInfoRev","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":100,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"IsRoutine","DisplayName":"روتین هست","Alliance":"IsRoutine","Address":null,"SystemTypeName":"Boolean","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":1,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":90,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"Comment","DisplayName":"کامنت","Alliance":"Comment","Address":null,"SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":200,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartId","DisplayName":"PartId","Alliance":"PartId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"PartExteraInfoType","DisplayName":"PartExteraInfoType","Alliance":"PartExteraInfoType","Address":null,"SystemTypeName":"Int","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":7,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""},{"TableName":null,"ColumnName":"ProductId","DisplayName":"ProductId","Alliance":"ProductId","Address":null,"SystemTypeName":"Long","SelectIndex":0,"Visible":false,"PrimaryKey":false,"SystemType":6,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":80,"Filterable":true,"Sortable":true,"ClassName":""}]', N'[{"dataActionName":"new","enable":false,"title":"جدید"},{"dataActionName":"edit","enable":false,"title":"ویرایش"},{"dataActionName":"delete","enable":false,"title":"حذف"},{"dataActionName":"exportExcell","enable":true,"title":"خروجی اکسل"},{"dataActionName":"defaultMultiSelect","enable":false,"title":"انتخاب چندتایی پیش‌فرض"}]', N'[{"id":"id_ldp_datasheet","title":"نمایش دیتاشیت","dataActionName":"showDataSheet","colorClass":"btn-color-primary","iconClass":"fa fa-note","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"function(ctx) {\n    if (!ctx.selectedRow) {\n        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');\n        return;\n    }\n    if (!ctx.selectedRow.partExteraInfoType || ctx.selectedRow.partExteraInfoType == 0) {\n        toastr.warning(''هیچ دیتاشیتی جهت نمایش یافت نشد.'');\n        return;\n    }\n    var optionsEl = '''';\n    for (var i = ctx.selectedRow.partExteraInfoRev; i >= 0; i--) {\n        optionsEl += '' <option value=\"'' + i + ''\">'' + i + ''</option> '';\n    }\n    var $modalContetn = ''<div><select class=\"form-select\" id=\"ldpDatasheetRevSelect\">'' + optionsEl + ''</select></div>'';\n    $.confirm({\n        title: ''انتخاب نسخه دیتاشیت'',\n        content: $modalContetn,\n        type: ''green'',\n        columnClass: ''col-md-6'',\n        typeAnimated: true,\n        buttons: {\n            acc: {\n                text: ''نمایش'',\n                btnClass: ''btn btn-success'',\n                action: function () {\n                    var selectedRev = this.$content.find(''#ldpDatasheetRevSelect'').val();\n                    if (selectedRev === undefined || selectedRev === null || selectedRev === '''') {\n                        selectedRev = ctx.selectedRow.partExteraInfoRev;\n                    }\n                    var partId = ctx.selectedRow.partId;\n                    var revQ = \"rev={''values'':[''\" + selectedRev + \"''],''condition'':''=''}\";\n                    var productQ = \"productId={''values'':[''\" + partId + \"''],''condition'':''=''}\";\n                    switch (ctx.selectedRow.partExteraInfoType) {\n                        case 1:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/partexterainfooilinject?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 2:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoOilFree?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 3:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoDryer?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                        case 4:\n                        case 5:\n                            appController.addPage(''/System/ReportBuilder/ViewReportByName/PartExteraInfoPsa?'' + productQ + ''&'' + revQ + ''&autoRun=True'');\n                            break;\n                    }\n                }\n            },\n            close: {\n                text: ''بستن'',\n                btnClass: ''btn btn-danger'',\n                action: function () {}\n            }\n        }\n    });\n}"},{"id":"id_ldp_techdocs","title":"پیوست مدارک فنی","dataActionName":"showDocuments","colorClass":"btn-color-success","iconClass":"bi-filetype-doc bi","requiresSelection":true,"requiresMultiSelection":false,"useHtml":false,"html":"","actionScript":"async function(ctx) {\n    if (!ctx.selectedRow) {\n        toastr.warning(''لطفاً یک ردیف انتخاب کنید'');\n        return;\n    }\n    var response = await get(''/Panel/Inv/Part/PartDocumentInfo?id='' + ctx.selectedRow.partId);\n    if (!response.isSuccess)\n        return toastr.error(response.message);\n    var rowsHtml = (response.data || []).map(function (x) {\n        return ''<tr>''\n            + ''<td>'' + (x.originalName || '''') + ''</td>''\n            + ''<td>'' + (x.size || '''') + ''</td>''\n            + ''<td>'' + (x.type || '''') + ''</td>''\n            + ''<td>'' + (x.main || '''') + ''</td>''\n            + ''<td>'' + (x.createdByName || '''') + ''</td>''\n            + ''<td>'' + (x.createdOnShamsiDateTime || '''') + ''</td>''\n            + ''<td>'' + (x.modifiedByName || '''') + ''</td>''\n            + ''<td>'' + (x.modifiedDateShamsiDateTime || '''') + ''</td>''\n            + ''</tr>'';\n    }).join('''');\n    var $modalContetn = $(''<table class=\"table table-bordered w-100\">''\n        + ''<thead><tr>''\n        + ''<th>نام مدرک</th><th>سایز</th><th>نوع</th><th>اصلی</th>''\n        + ''<th>ایجاد کننده</th><th>زمان ایجاد</th><th>ویرایش کننده</th><th>زمان ویرایش</th>''\n        + ''</tr></thead><tbody>'' + rowsHtml + ''</tbody></table>'');\n    $.confirm({\n        title: ''پیوست مدارک'',\n        content: $modalContetn,\n        type: ''green'',\n        columnClass: ''col-md-12'',\n        typeAnimated: true,\n        buttons: {\n            close: {\n                text: ''بستن'',\n                btnClass: ''btn btn-danger'',\n                action: function () {}\n            }\n        }\n    });\n}"}]');

    DECLARE @PName NVARCHAR(100), @PTitle NVARCHAR(200);
    DECLARE @PCols NVARCHAR(MAX), @PSelect NVARCHAR(MAX), @PAct NVARCHAR(MAX), @PBtn NVARCHAR(MAX);
    DECLARE @PQueryJson NVARCHAR(MAX), @PId BIGINT;

    DECLARE ldp_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Name, Title, ColumnsJson, CustomQuery, ActionOptions, CustomButtons FROM #LdpProfiles;
    OPEN ldp_cur;
    FETCH NEXT FROM ldp_cur INTO @PName, @PTitle, @PCols, @PSelect, @PAct, @PBtn;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @PQueryJson = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @PSelect);
        IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @PName)
        BEGIN
            UPDATE system.SavedQuery
            SET Title = @PTitle,
                QueryJson = @PQueryJson,
                ColumnsJson = @PCols,
                EntityFullName = @EntityFullName,
                Mode = 1,
                Type = 1,
                ActionOptions = @PAct,
                CustomActionButtonsJson = @PBtn,
                DiagramJson = N'{}',
                EventScriptsJson = N'{"onSelectedRow":"","onRowAdded":""}',
                ModifiedById = 1,
                ModifiedByName = @SeedUser,
                ModifiedDateMiladiDateTime = @Now,
                ModifiedDateShamsiDateTime = @NowShamsi,
                IsActive = 1
            WHERE Name = @PName;
            SELECT @PId = Id FROM system.SavedQuery WHERE Name = @PName;
            PRINT N'  UPDATED ' + @PName + N' Id=' + CAST(@PId AS nvarchar(20));
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
                 1, 1, @EntityFullName, @PAct,
                 1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1);
            SET @PId = SCOPE_IDENTITY();
            PRINT N'  CREATED ' + @PName + N' Id=' + CAST(@PId AS nvarchar(20));
        END
        UPDATE #LdpProfiles SET SavedQueryId = @PId WHERE Name = @PName;
        FETCH NEXT FROM ldp_cur INTO @PName, @PTitle, @PCols, @PSelect, @PAct, @PBtn;
    END
    CLOSE ldp_cur; DEALLOCATE ldp_cur;

    PRINT N'=== [2] Roles ===';
    IF OBJECT_ID('tempdb..#LdpRoles') IS NOT NULL DROP TABLE #LdpRoles;
    CREATE TABLE #LdpRoles (WantId BIGINT NOT NULL, Name NVARCHAR(200) NOT NULL, Title NVARCHAR(200) NOT NULL);
    INSERT INTO #LdpRoles (WantId, Name, Title) VALUES
        (600020, N'Sale.DocumentProduct.View', N'فروش - مدارک محصولات - مشاهده بدون قیمت'),
        (600021, N'Sale.DocumentProduct.BuyPrice', N'فروش - مدارک محصولات - قیمت خرید'),
        (600022, N'Sale.DocumentProduct.SalePrice', N'فروش - مدارک محصولات - قیمت فروش'),
        (600023, N'Sale.DocumentProduct.BuyAndSale', N'فروش - مدارک محصولات - قیمت خرید و فروش'),
        (600024, N'Sale.DocumentProduct.Items', N'فروش - مدارک محصولات - اقلام BOM'),
        (600025, N'Sale.DocumentProduct.ItemsBuy', N'فروش - مدارک محصولات - اقلام BOM قیمت خرید'),
        (600026, N'Sale.DocumentProduct.ItemsSale', N'فروش - مدارک محصولات - اقلام BOM قیمت فروش'),
        (600027, N'Sale.DocumentProduct.ItemsBoth', N'فروش - مدارک محصولات - اقلام BOM خرید و فروش');
    DECLARE @Rid BIGINT, @RName NVARCHAR(200), @RTitle NVARCHAR(200);
    DECLARE rc CURSOR LOCAL FAST_FORWARD FOR SELECT WantId, Name, Title FROM #LdpRoles;
    OPEN rc; FETCH NEXT FROM rc INTO @Rid, @RName, @RTitle;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Name = @RName)
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM system.Role WHERE Id = @Rid)
            BEGIN
                SET IDENTITY_INSERT system.Role ON;
                INSERT INTO system.Role (Id, Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                    CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
                VALUES (@Rid, @RName, @RTitle, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
                SET IDENTITY_INSERT system.Role OFF;
                PRINT N'  CREATED ' + @RName;
            END
            ELSE
            BEGIN
                INSERT INTO system.Role (Name, Title, CreatedById, CreatedByName, ModifiedById, ModifiedByName,
                    CreatedOnMiladiDateTime, ModifiedDateMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateShamsiDateTime, IsActive)
                VALUES (@RName, @RTitle, 1, @SeedUser, 1, @SeedUser, @Now, @Now, @NowShamsi, @NowShamsi, 1);
                PRINT N'  CREATED (new id) ' + @RName;
            END
        END
        ELSE PRINT N'  EXISTS ' + @RName;
        FETCH NEXT FROM rc INTO @Rid, @RName, @RTitle;
    END
    CLOSE rc; DEALLOCATE rc;

    PRINT N'=== [3] Page RoleAccess ===';
    -- Rebuild page path only for our roles (keep ShowAllMenus as-is if already present)
    DELETE ra FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    WHERE ra.Path = @PagePath
      AND r.Name IN (
            N'Sale.DocumentProduct.View',
            N'Sale.DocumentProduct.BuyPrice',
            N'Sale.DocumentProduct.SalePrice',
            N'Sale.DocumentProduct.BuyAndSale',
            N'Sale.DocumentProduct.Items',
            N'Sale.DocumentProduct.ItemsBuy',
            N'Sale.DocumentProduct.ItemsSale',
            N'Sale.DocumentProduct.ItemsBoth',
            N'Sale.Document.Pricing'
      );
    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT @PagePath, 1, 1, @PageEntity, N'لیست مدارک محصولات', NULL, NULL, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM system.Role r
    WHERE r.Name IN (
            N'Sale.DocumentProduct.View',
            N'Sale.DocumentProduct.BuyPrice',
            N'Sale.DocumentProduct.SalePrice',
            N'Sale.DocumentProduct.BuyAndSale',
            N'Sale.DocumentProduct.Items',
            N'Sale.DocumentProduct.ItemsBuy',
            N'Sale.DocumentProduct.ItemsSale',
            N'Sale.DocumentProduct.ItemsBoth',
            N'Sale.Document.Pricing'
      );
    PRINT N'  Page RoleAccess rows: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    PRINT N'=== [4] DataProfile RoleAccess ===';
    -- Clear profile grants for our profiles + broken 48 for Sale.Document.Pricing / new roles / ShowAllMenus
    DELETE ra FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    WHERE ra.ActionAccessType = 3
      AND (
            ra.RowId IN (SELECT SavedQueryId FROM #LdpProfiles)
         OR ra.RowId = 48
         OR ra.Path LIKE N'dataProfile_48'
      )
      AND r.Name IN (
            N'Sale.Document.Pricing',
            N'Sale.DocumentProduct.BuyAndSale',
            N'Sale.DocumentProduct.BuyPrice',
            N'Sale.DocumentProduct.Items',
            N'Sale.DocumentProduct.ItemsBoth',
            N'Sale.DocumentProduct.ItemsBuy',
            N'Sale.DocumentProduct.ItemsSale',
            N'Sale.DocumentProduct.SalePrice',
            N'Sale.DocumentProduct.View',
            N'ShowAllMenus'
      );

    IF OBJECT_ID('tempdb..#LdpProfileRole') IS NOT NULL DROP TABLE #LdpProfileRole;
    CREATE TABLE #LdpProfileRole (ProfileName NVARCHAR(100) NOT NULL, RoleName NVARCHAR(200) NOT NULL);
    INSERT INTO #LdpProfileRole (ProfileName, RoleName) VALUES
        (N'ListDocumentProduct_NoPrice', N'Sale.DocumentProduct.View'),
        (N'ListDocumentProduct_NoPrice', N'ShowAllMenus'),
        (N'vw_ListDocumentProductPricenew', N'Sale.DocumentProduct.BuyPrice'),
        (N'vw_ListDocumentProductPriceSale', N'Sale.DocumentProduct.SalePrice'),
        (N'vw_ListDocumentProductPrice', N'Sale.DocumentProduct.BuyAndSale'),
        (N'vw_ListDocumentProductPrice', N'Sale.Document.Pricing'),
        (N'ListDocumentProduct_Bom_NoPrice', N'Sale.DocumentProduct.Items'),
        (N'ListDocumentProduct_Bom_Buy', N'Sale.DocumentProduct.ItemsBuy'),
        (N'ListDocumentProduct_Bom_Sale', N'Sale.DocumentProduct.ItemsSale'),
        (N'vw_ListDocumentProductPriceBom', N'Sale.DocumentProduct.ItemsBoth'),
        (N'vw_ListDocumentProductPriceBom', N'Sale.Document.Pricing');

    INSERT INTO system.RoleAccess
        (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
         CreatedById, ModifiedById, CreatedByName, ModifiedByName,
         CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
    SELECT DISTINCT
        N'dataProfile_' + CAST(q.SavedQueryId AS nvarchar(20)),
        3, 7, @EntityFullName, q.Title, NULL, q.SavedQueryId, r.Id,
        1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
    FROM #LdpProfileRole map
    INNER JOIN #LdpProfiles q ON q.Name = map.ProfileName
    INNER JOIN system.Role r ON r.Name = map.RoleName;
    PRINT N'  DataProfile RoleAccess: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    -- Ensure Sale.Document.Pricing has no grant to broken 48 or single-purpose profiles
    DELETE ra FROM system.RoleAccess ra
    INNER JOIN system.Role r ON r.Id = ra.RoleId
    WHERE r.Name = N'Sale.Document.Pricing'
      AND ra.ActionAccessType = 3
      AND (
            ra.RowId = 48 OR ra.Path = N'dataProfile_48'
         OR ra.RowId IN (
                SELECT Id FROM system.SavedQuery
                WHERE Name IN (
                    N'vw_ListDocumentProductPricenew', N'vw_ListDocumentProductPriceSale',
                    N'ListDocumentProduct_Bom_Buy', N'ListDocumentProduct_Bom_Sale',
                    N'ListDocumentProduct_Bom_NoPrice', N'ListDocumentProduct_NoPrice'
                )
           )
      );

    PRINT N'=== [5] Menu AccessRoleIds merge (Id 9 Sell, Id 13 AllMenus) ===';
    DECLARE @MenuId BIGINT;
    DECLARE mc CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM system.SystemMenu WHERE Id IN (9, 13);
    OPEN mc; FETCH NEXT FROM mc INTO @MenuId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF OBJECT_ID('tempdb..#MenuRoles') IS NOT NULL DROP TABLE #MenuRoles;
        CREATE TABLE #MenuRoles (RoleName NVARCHAR(200) NOT NULL PRIMARY KEY);
        INSERT INTO #MenuRoles (RoleName)
        SELECT DISTINCT LTRIM(RTRIM(j.value))
        FROM system.SystemMenu m CROSS APPLY OPENJSON(ISNULL(m.AccessRoles, N'[]')) j
        WHERE m.Id = @MenuId AND LTRIM(RTRIM(j.value)) <> N'';

        INSERT INTO #MenuRoles (RoleName)
        SELECT v.RoleName FROM (VALUES
            (N'Sale.DocumentProduct.View'),
            (N'Sale.DocumentProduct.BuyPrice'),
            (N'Sale.DocumentProduct.SalePrice'),
            (N'Sale.DocumentProduct.BuyAndSale'),
            (N'Sale.DocumentProduct.Items'),
            (N'Sale.DocumentProduct.ItemsBuy'),
            (N'Sale.DocumentProduct.ItemsSale'),
            (N'Sale.DocumentProduct.ItemsBoth'),
            (N'Sale.Document.Pricing'),
            (N'ShowAllMenus')
        ) v(RoleName)
        WHERE NOT EXISTS (SELECT 1 FROM #MenuRoles x WHERE x.RoleName = v.RoleName);

        DECLARE @AccessRoles NVARCHAR(MAX);
        DECLARE @AccessRoleIds NVARCHAR(MAX);
        SELECT @AccessRoles = N'[' + STUFF((
            SELECT N',"' + RoleName + N'"' FROM #MenuRoles ORDER BY RoleName
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';
        SELECT @AccessRoleIds = N'[' + STUFF((
            SELECT N',' + CAST(r.Id AS nvarchar(20))
            FROM #MenuRoles m INNER JOIN system.Role r ON r.Name = m.RoleName
            ORDER BY r.Id
            FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') + N']';

        UPDATE system.SystemMenu
        SET AccessRoles = @AccessRoles, AccessRoleIds = @AccessRoleIds,
            ModifiedById = 1, ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Id = @MenuId;
        PRINT N'  Menu Id=' + CAST(@MenuId AS nvarchar(20)) + N' merged';
        FETCH NEXT FROM mc INTO @MenuId;
    END
    CLOSE mc; DEALLOCATE mc;

    PRINT N'=== [6] Assign HTS users (pages 107/108) ===';
    IF EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
    BEGIN
        IF OBJECT_ID('tempdb..#HtsPerm') IS NOT NULL DROP TABLE #HtsPerm;
        CREATE TABLE #HtsPerm (
            Username NVARCHAR(200) NOT NULL,
            PageId INT NOT NULL,
            PermissionId INT NOT NULL,
            PRIMARY KEY (Username, PageId, PermissionId)
        );

        -- Direct + group grants via Vw_Permission
        INSERT INTO #HtsPerm (Username, PageId, PermissionId)
        SELECT DISTINCT
            LTRIM(RTRIM(hu.Username)),
            p.Page_ID,
            p.Permission_ID
        FROM [TMS].[TotalSystem].[dbo].[Vw_Permission] p
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] hu ON hu.User_ID = p.User_FK
        WHERE p.Page_ID IN (107, 108)
          AND p.IsActive = 1
          AND hu.IsActive = 1
          AND p.User_FK IS NOT NULL
          AND NULLIF(LTRIM(RTRIM(hu.Username)), N'') IS NOT NULL;

        -- Current group members for group-level grants
        INSERT INTO #HtsPerm (Username, PageId, PermissionId)
        SELECT DISTINCT
            LTRIM(RTRIM(u.Username)),
            gp.Page_ID,
            gp.Permission_ID
        FROM (
            SELECT DISTINCT Page_ID, Permission_ID, UserGroup_ID
            FROM [TMS].[TotalSystem].[dbo].[Vw_Permission]
            WHERE Page_ID IN (107, 108)
              AND IsActive = 1
              AND UserGroup_ID IS NOT NULL
        ) gp
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_UserGroupMember] m ON m.UserGroup_FK = gp.UserGroup_ID
        INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] u ON u.User_ID = m.User_FK
        WHERE u.IsActive = 1
          AND NULLIF(LTRIM(RTRIM(u.Username)), N'') IS NOT NULL
          AND NOT EXISTS (
              SELECT 1 FROM #HtsPerm x
              WHERE x.Username = LTRIM(RTRIM(u.Username))
                AND x.PageId = gp.Page_ID AND x.PermissionId = gp.Permission_ID
          );

        IF OBJECT_ID('tempdb..#Flags') IS NOT NULL DROP TABLE #Flags;
        CREATE TABLE #Flags (
            Username NVARCHAR(200) NOT NULL PRIMARY KEY,
            CanView BIT NOT NULL,
            CanBuy BIT NOT NULL,
            CanSale BIT NOT NULL,
            CanItems BIT NOT NULL,
            CanItemBuy BIT NOT NULL,
            CanItemSale BIT NOT NULL
        );

        INSERT INTO #Flags (Username, CanView, CanBuy, CanSale, CanItems, CanItemBuy, CanItemSale)
        SELECT
            u.Username,
            CASE WHEN EXISTS (SELECT 1 FROM #HtsPerm p WHERE p.Username = u.Username AND p.PageId = 107 AND p.PermissionId IN (2, 3)) THEN 1 ELSE 0 END,
            CASE WHEN EXISTS (SELECT 1 FROM #HtsPerm p WHERE p.Username = u.Username AND p.PageId = 107 AND p.PermissionId IN (2, 8)) THEN 1 ELSE 0 END,
            CASE WHEN EXISTS (SELECT 1 FROM #HtsPerm p WHERE p.Username = u.Username AND p.PageId IN (107, 108) AND p.PermissionId IN (2, 182)) THEN 1 ELSE 0 END,
            CASE WHEN EXISTS (
                SELECT 1 FROM #HtsPerm p WHERE p.Username = u.Username AND (
                    (p.PageId = 107 AND p.PermissionId IN (2, 31))
                 OR (p.PageId = 108 AND p.PermissionId IN (2, 3))
                )) THEN 1 ELSE 0 END,
            CASE WHEN EXISTS (SELECT 1 FROM #HtsPerm p WHERE p.Username = u.Username AND p.PageId = 108 AND p.PermissionId IN (2, 8)) THEN 1 ELSE 0 END,
            CASE WHEN EXISTS (SELECT 1 FROM #HtsPerm p WHERE p.Username = u.Username AND p.PageId = 108 AND p.PermissionId IN (2, 182)) THEN 1 ELSE 0 END
        FROM (SELECT DISTINCT Username FROM #HtsPerm) u;

        IF OBJECT_ID('tempdb..#UserRoleMap') IS NOT NULL DROP TABLE #UserRoleMap;
        CREATE TABLE #UserRoleMap (Username NVARCHAR(200) NOT NULL, RoleName NVARCHAR(200) NOT NULL, PRIMARY KEY (Username, RoleName));

        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT Username, N'Sale.DocumentProduct.View' FROM #Flags WHERE CanView = 1;

        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT Username, N'Sale.DocumentProduct.BuyAndSale' FROM #Flags WHERE CanBuy = 1 AND CanSale = 1;

        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT Username, N'Sale.DocumentProduct.BuyPrice' FROM #Flags WHERE CanBuy = 1 AND CanSale = 0;

        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT Username, N'Sale.DocumentProduct.SalePrice' FROM #Flags WHERE CanSale = 1 AND CanBuy = 0;

        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT Username, N'Sale.DocumentProduct.Items' FROM #Flags WHERE CanItems = 1;

        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT Username, N'Sale.DocumentProduct.ItemsBoth' FROM #Flags WHERE CanItemBuy = 1 AND CanItemSale = 1;

        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT Username, N'Sale.DocumentProduct.ItemsBuy' FROM #Flags WHERE CanItemBuy = 1 AND CanItemSale = 0;

        INSERT INTO #UserRoleMap (Username, RoleName)
        SELECT Username, N'Sale.DocumentProduct.ItemsSale' FROM #Flags WHERE CanItemSale = 1 AND CanItemBuy = 0;

        DECLARE @Assigned INT = 0, @Missing INT = 0;
        DECLARE @UName NVARCHAR(200), @RoleN NVARCHAR(200), @UserId BIGINT, @RoleId BIGINT;
        DECLARE @RolesJson NVARCHAR(MAX), @RoleIdsJson NVARCHAR(MAX);

        DECLARE uc CURSOR LOCAL FAST_FORWARD FOR SELECT Username, RoleName FROM #UserRoleMap;
        OPEN uc; FETCH NEXT FROM uc INTO @UName, @RoleN;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SELECT TOP 1 @UserId = u.Id
            FROM system.[User] u
            WHERE u.IsActive = 1
              AND LOWER(u.Username) = LOWER(@UName);
            SELECT TOP 1 @RoleId = r.Id FROM system.Role r WHERE r.Name = @RoleN;
            IF @UserId IS NULL OR @RoleId IS NULL
                SET @Missing = @Missing + 1;
            ELSE
            BEGIN
                SELECT @RolesJson = ISNULL(Roles, N'[]'), @RoleIdsJson = ISNULL(RoleIds, N'[]')
                FROM system.[User] WHERE Id = @UserId;
                IF NOT EXISTS (SELECT 1 FROM OPENJSON(@RolesJson) WHERE LOWER([value]) = LOWER(@RoleN))
                    SET @RolesJson = JSON_MODIFY(@RolesJson, N'append $', @RoleN);
                IF NOT EXISTS (SELECT 1 FROM OPENJSON(@RoleIdsJson) WHERE TRY_CAST([value] AS BIGINT) = @RoleId)
                    SET @RoleIdsJson = JSON_MODIFY(@RoleIdsJson, N'append $', @RoleId);
                UPDATE system.[User] SET Roles = @RolesJson, RoleIds = @RoleIdsJson,
                    ModifiedById = 1, ModifiedByName = @SeedUser,
                    ModifiedDateMiladiDateTime = @Now, ModifiedDateShamsiDateTime = @NowShamsi
                WHERE Id = @UserId;
                SET @Assigned = @Assigned + 1;
            END
            SET @UserId = NULL; SET @RoleId = NULL;
            FETCH NEXT FROM uc INTO @UName, @RoleN;
        END
        CLOSE uc; DEALLOCATE uc;
        PRINT N'  User-role assigns touched: ' + CAST(@Assigned AS nvarchar(20))
            + N', missing user/role: ' + CAST(@Missing AS nvarchar(20));
    END
    ELSE PRINT N'  WARN: Linked Server TMS missing — user assign skipped';

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_ListDocumentProduct_ProfilesAndAccess ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

-- Verification (outside transaction)
SELECT Id, Name, Title,
       JSON_VALUE(ActionOptions, '$[3].enable') AS ExportExcell,
       CASE WHEN CustomActionButtonsJson LIKE N'%debugger%' THEN 1 ELSE 0 END AS HasDebugger
FROM system.SavedQuery
WHERE EntityFullName = N'listdocumentproduct'
ORDER BY Id;

SELECT q.Name, JSON_VALUE(j.value, '$.Alliance') Alliance, JSON_VALUE(j.value, '$.DisplayName') DisplayName
FROM system.SavedQuery q
CROSS APPLY OPENJSON(q.ColumnsJson) j
WHERE q.Name IN (
    N'ListDocumentProduct_NoPrice', N'vw_ListDocumentProductPricenew',
    N'vw_ListDocumentProductPriceSale', N'vw_ListDocumentProductPrice',
    N'ListDocumentProduct_Bom_NoPrice', N'ListDocumentProduct_Bom_Buy',
    N'ListDocumentProduct_Bom_Sale', N'vw_ListDocumentProductPriceBom'
)
ORDER BY q.Name, CAST(j.[key] AS INT);

SELECT COUNT(*) AS RoleAccessOn48
FROM system.RoleAccess WHERE RowId = 48 OR Path = N'dataProfile_48';

SELECT r.Name, COUNT(*) AS UserCount
FROM system.Role r
INNER JOIN system.[User] u ON u.IsActive = 1
CROSS APPLY OPENJSON(ISNULL(u.RoleIds, N'[]')) j
WHERE TRY_CAST(j.value AS BIGINT) = r.Id
  AND r.Name LIKE N'Sale.DocumentProduct.%'
GROUP BY r.Name
ORDER BY r.Name;

