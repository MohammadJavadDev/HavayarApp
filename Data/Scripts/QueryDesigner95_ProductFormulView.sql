-- QueryDesigner Id = 95
-- معادل صفحه مدارک محصولات سیستم قدیمی با منابع داده سیستم جدید
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
      AND price.CreatedOnMiladiDateTime >= '2023-03-21'
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
      AND price.CreatedOnMiladiDateTime >= '2023-03-21'
      -- مطابق HTS، قیمت ریالی فقط تا پایان سال 1403 معتبر است.
      AND price.CreatedOnMiladiDateTime < '2025-03-21'
),
ListData AS
(
    SELECT
        bomItem.Id AS ProductFormulId,
        product.Id AS ProductId,
        CAST(CASE
            -- مانند سیستم قدیمی، وجود قیمت ارزی معتبر بر قیمت ریالی اولویت دارد.
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
    -- View مدارک برای بعضی محصولات چند ردیف دارد؛ صفحه باید برای هر محصول فقط یک ردیف بدهد.
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
    CAST(ROUND(ISNULL(price.ExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0)
         + ISNULL(internalPrice.InternalTotalBuyPrice, 0) AS DECIMAL(28, 5)) AS BuyPrice,
    CAST(ROUND(ISNULL(price.TotalBuyPriceInEuro, 0), 0) AS DECIMAL(28, 5)) AS BuyPriceInEuro,
    CAST(ROUND(ROUND(ISNULL(price.TotalBuyPriceInEuro, 0), 0) * @CurrencyPrice, 0) AS DECIMAL(28, 5)) AS SalePriceRateAzad,
    @CurrencyPrice AS CurrencyRateAzad,
    CAST(CASE
        WHEN (ROUND(ISNULL(price.ExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0)
              + ISNULL(internalPrice.InternalTotalBuyPrice, 0)) * ISNULL(base.SaleRate, 1)
             > ROUND(ISNULL(price.SalePriceFromRuro, 0) * @CurrencyPrice, 0)
            -- در سیستم قدیمی این شاخه پیش از نمایش به long تبدیل می‌شد.
            THEN CAST(
                (ROUND(ISNULL(price.ExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0)
                 + ISNULL(internalPrice.InternalTotalBuyPrice, 0)) * ISNULL(base.SaleRate, 1)
                AS BIGINT)
        ELSE ROUND(ISNULL(price.SalePriceFromRuro, 0) * @CurrencyPrice, 0)
    END AS DECIMAL(28, 5)) AS SalePrice,
    price.TotalBuyPriceInEuro,
    price.SalePriceFromRuro,
    price.ExternalTotalBuyPriceInEuro
FROM BaseQuery AS base
LEFT JOIN InternalPrice AS internalPrice ON internalPrice.ProductId = base.ProductId
LEFT JOIN BomState AS bom ON bom.ProductFormulId = base.Id
LEFT JOIN DocumentCount AS document ON document.PartId = base.ProductId
LEFT JOIN FinalPrice AS price ON price.ProductFormulId = base.Id;
