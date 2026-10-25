/*
  Query_ProductItems_PriceInfo.sql
  اقلام BOM یک محصول (بر اساس کد کالای محصول) با:
    - کد کالا / نام کالا
    - قیمت یورویی (همان منطق نمایه vw_ListDocumentProductPrice: آخرین قیمت ارزی، در غیر این صورت آخرین قیمت ریالی تقسیم بر نرخ یورو)
    - قیمت و تاریخ آخرین خرید   (Sup.InquiryPartPrice با Type = 1676 فاکتور خرید)
    - قیمت و تاریخ آخرین استعلام (Sup.InquiryPartPrice با Type = 289/290/291، بدون پرایس‌لیست 2834 و بدون استعلام خدمات پس از فروش 1675)
  UTF-8 BOM.
*/
DECLARE @ProductCode NVARCHAR(50) = N'1801120111';

WITH LatestForeignPrice AS
(
    SELECT price.PartId,
           price.PriceUnitValue + ISNULL(price.TransportationCost, 0) AS PriceValue,
           ROW_NUMBER() OVER (PARTITION BY price.PartId ORDER BY price.CreatedOnMiladiDateTime DESC, price.Id DESC) AS RowNumber
    FROM Sup.InquiryPartPrice AS price
    WHERE price.Type <> 1675
      AND ISNULL(price.PriceStatus, 1) <> 2834
      AND ISNULL(price.PriceUnitId, 1) <> 1
      AND price.CreatedOnMiladiDateTime >= '2023-03-21'
),
LatestRialPrice AS
(
    SELECT price.PartId,
           ISNULL(price.UnitPrice, 0) AS PriceValue,
           price.CreatedOnMiladiDateTime AS PriceDate,
           ROW_NUMBER() OVER (PARTITION BY price.PartId ORDER BY price.CreatedOnMiladiDateTime DESC, price.Id DESC) AS RowNumber
    FROM Sup.InquiryPartPrice AS price
    WHERE price.Type <> 1675
      AND ISNULL(price.PriceStatus, 1) <> 2834
      AND ISNULL(price.PriceUnitId, 1) = 1
      AND price.CreatedOnMiladiDateTime >= '2023-03-21'
      AND price.CreatedOnMiladiDateTime < '2025-03-21'
),
LastBuy AS
(
    SELECT price.PartId,
           price.UnitPrice,
           price.CreatedOnShamsiDateTime,
           ROW_NUMBER() OVER (PARTITION BY price.PartId ORDER BY price.CreatedOnMiladiDateTime DESC, price.Id DESC) AS RowNumber
    FROM Sup.InquiryPartPrice AS price
    WHERE price.Type = 1676
),
LastInquiry AS
(
    SELECT price.PartId,
           price.UnitPrice,
           price.CreatedOnShamsiDateTime,
           ROW_NUMBER() OVER (PARTITION BY price.PartId ORDER BY price.CreatedOnMiladiDateTime DESC, price.Id DESC) AS RowNumber
    FROM Sup.InquiryPartPrice AS price
    WHERE price.Type IN (289, 290, 291)
      AND ISNULL(price.PriceStatus, 1) <> 2834
)
SELECT
    component.Code AS PartCode,
    component.Name AS PartName,
    bomItem.UsingRate,
    CAST(CASE
            WHEN foreignPrice.PriceValue IS NOT NULL AND foreignPrice.PriceValue > 1 THEN foreignPrice.PriceValue
            ELSE rialPrice.PriceValue / NULLIF(exchangeRate.FinalPrice, 0)
         END AS DECIMAL(28, 5)) AS EuroUnitPrice,
    CAST(CASE
            WHEN foreignPrice.PriceValue IS NOT NULL AND foreignPrice.PriceValue > 1 THEN foreignPrice.PriceValue * ISNULL(bomItem.UsingRate, 1)
            ELSE (rialPrice.PriceValue * ISNULL(bomItem.UsingRate, 1)) / NULLIF(exchangeRate.FinalPrice, 0)
         END AS DECIMAL(28, 5)) AS EuroTotalPrice,
    lastBuy.UnitPrice AS LastBuyPrice,
    LEFT(lastBuy.CreatedOnShamsiDateTime, 10) AS LastBuyDate,
    lastInquiry.UnitPrice AS LastInquiryPrice,
    LEFT(lastInquiry.CreatedOnShamsiDateTime, 10) AS LastInquiryDate
FROM Inv.Part AS product
INNER JOIN dbo.vw_BomProductItem AS bomItem ON bomItem.ProductId = product.Id
INNER JOIN Inv.Part AS component ON component.Id = bomItem.PartItemId
LEFT JOIN LatestForeignPrice AS foreignPrice ON foreignPrice.PartId = component.Id AND foreignPrice.RowNumber = 1
LEFT JOIN LatestRialPrice AS rialPrice ON rialPrice.PartId = component.Id AND rialPrice.RowNumber = 1
OUTER APPLY
(
    SELECT TOP (1) rate.FinalPrice
    FROM Acc.ExchangeRateArchive AS rate
    WHERE rate.PriceUnitId = 4
      AND rate.CreatedOnMiladiDateTime <= rialPrice.PriceDate
    ORDER BY rate.CreatedOnMiladiDateTime DESC, rate.Id DESC
) AS exchangeRate
LEFT JOIN LastBuy AS lastBuy ON lastBuy.PartId = component.Id AND lastBuy.RowNumber = 1
LEFT JOIN LastInquiry AS lastInquiry ON lastInquiry.PartId = component.Id AND lastInquiry.RowNumber = 1
WHERE product.Code = @ProductCode
ORDER BY component.Code;
