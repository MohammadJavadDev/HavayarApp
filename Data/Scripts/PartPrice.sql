-- تعریف متغیر قیمت ارز (CurrencyID = 257)
DECLARE @CurrencyPrice DECIMAL(18, 2);

SELECT @CurrencyPrice = CAST(Number1 AS DECIMAL(18, 2))
FROM ERPS.erps.GNR3.Currency AS Currency
    LEFT JOIN ERPS.erps.SYS3.FieldValueContainer AS FieldValueContainer
        ON FieldValueContainer.RecordID = Currency.CurrencyID
        AND FieldValueContainer.EntityCode = 240
WHERE CurrencyID = 257;

-- کوئری اصلی با محاسبات جدید
WITH BaseQuery AS (
    SELECT
        pf.Id,
        pf.ProductId,
        pf.ProductNameGroupId,
        pf.CreatedOnMiladiDateTime,
        pf.CreatedOnShamsiDateTime,
        pf.Comment,
        part.Code AS PartCode,
        part.Name AS PartName,
        part.DesignTypeIsRoutine AS IsRoutine,
        part.[Foreign],
        part.EngineeringRoutine,
        part.DataSheetUsage,
        part.DataSheet,
        part.BrandInDataSheet,
        part.PreciseTool,
        part.SaleRate,  -- اضافه شده
        pe.Revision AS PartExteraInfoRev,
        pe.InfoType AS PartExteraInfoType,
        pg.Name AS ProductNameGroupName
    FROM Bom.ProductFormul pf
    INNER JOIN Inv.Part part ON pf.ProductId = part.Id
    LEFT JOIN (
        SELECT pe.ProductId, pe.Revision, pe.InfoType
        FROM Inv.PartExtraInfo pe
        INNER JOIN (
            SELECT ProductId, MAX(Revision) AS MaxRevision
            FROM Inv.PartExtraInfo
            GROUP BY ProductId
        ) latest ON pe.ProductId = latest.ProductId AND pe.Revision = latest.MaxRevision
    ) pe ON part.Id = pe.ProductId
    LEFT JOIN Bom.ProductGroup pg ON pf.ProductNameGroupId = pg.Id
),
ListData AS (
    SELECT
        Product.Id AS ProductId,
        CASE
            WHEN Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value IS NOT NULL
                 AND ISNULL(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value, 0) > 1
            THEN CAST(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value * ISNULL(Vw_Bom_Product_Items.UsingRate, 1) AS DECIMAL(18,5))
            ELSE CAST(((Vw_Sup_PartPrice_HistoryRial.PartPrice * ISNULL(Vw_Bom_Product_Items.UsingRate, 1)) / Acc_ExchangeRateArchive.FinalPrice) AS DECIMAL(18,5))
        END AS TotalBuyPriceInEuro,
        CASE
            WHEN Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value IS NOT NULL
                 AND ISNULL(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value, 0) > 1
            THEN CAST(Vw_Sup_PartPrice_HistoryEurope.PriceUnit_Value * ISNULL(Vw_Bom_Product_Items.UsingRate, 1) AS DECIMAL(18,5)) * ISNULL(Part.SaleRate, 1)
            ELSE CAST(((Vw_Sup_PartPrice_HistoryRial.PartPrice * ISNULL(Vw_Bom_Product_Items.UsingRate, 1)) / Acc_ExchangeRateArchive.FinalPrice) AS DECIMAL(18,5)) * ISNULL(Part.SaleRate, 1)
        END AS SalePriceFromRuro,
        Product.SaleRate,
        Part.[Foreign]
    FROM Inv.Part AS Product
    LEFT JOIN dbo.vw_BomProductItem AS Vw_Bom_Product_Items
        ON Vw_Bom_Product_Items.ProductId = Product.Id
    JOIN inv.Part AS Part
        ON Part.Id = Vw_Bom_Product_Items.PartItemId
    LEFT JOIN (
        SELECT
            Vw_Sup_PartPrice_History.PriceUnitValue + ISNULL(Vw_Sup_PartPrice_History.TransportationCost, 0) AS PriceUnit_Value,
            Vw_Sup_PartPrice_History.PartId,
            RowNum = ROW_NUMBER() OVER(PARTITION BY Vw_Sup_PartPrice_History.PartId ORDER BY Vw_Sup_PartPrice_History.CreatedOnMiladiDateTime DESC)
        FROM Sup.InquiryPartPrice AS Vw_Sup_PartPrice_History
        WHERE Vw_Sup_PartPrice_History.Type NOT IN (1675)
          AND ISNULL(Vw_Sup_PartPrice_History.PriceStatus, 1) <> 2834
          AND Vw_Sup_PartPrice_History.PriceUnitId <> 1
          AND Vw_Sup_PartPrice_History.CreatedOnShamsiDateTime >= '1402/01/01'
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
          AND Vw_Sup_PartPrice_History.PriceUnitId = 1
          AND Vw_Sup_PartPrice_History.CreatedOnShamsiDateTime BETWEEN '1402/01/01' AND '1403/12/30'
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
),
finalPrice AS (
    SELECT
        A.ProductId,
        CAST(SUM(A.TotalBuyPriceInEuro) AS DECIMAL(18,5)) AS TotalBuyPriceInEuro,          -- تغییر نوع به عددی
        CAST(SUM(A.SalePriceFromRuro * ISNULL(A.SaleRate, 1)) AS DECIMAL(18,5)) AS SalePriceFromRuro,
        CAST(SUM(A.ExternalTotalBuyPriceInEuro) AS DECIMAL(18,5)) AS ExternalTotalBuyPriceInEuro
    FROM (
        SELECT
            *,
            IIF(l.[Foreign] = 0, NULL, l.TotalBuyPriceInEuro) AS ExternalTotalBuyPriceInEuro
        FROM ListData l
    ) AS A
    GROUP BY A.ProductId
)

-- بخش اصلی SELECT با محاسبات جدید
SELECT
    p.Id ProductFormulId,
    ISNULL([pd].[DocumentCount], 0) AS [DocumentCount],
    CAST(CASE WHEN bom.ProductFormulId IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasBom,
    p.PartCode,
    p.PartName,
    p.ProductNameGroupName,
    price.InternalTotalBuyPrice,
    price.ExternalTotalBuyPrice,
    price.BuyPrice AS BuyPrice,
    lastPrice.CurrencyActualPrice AS BuyPriceInEuro,
    price.BuyPrice AS SalePrice,
    p.IsRoutine,
    p.CreatedOnShamsiDateTime,
    p.Comment,
    p.ProductNameGroupId,
    p.CreatedOnMiladiDateTime AS CreatedDate,
    p.PartExteraInfoRev,
    p.PartExteraInfoType PartExteraInfoTypeName,
    p.PartExteraInfoType,
    p.ProductId AS PartId,
    -- فیلدهای جدید محاسبه‌شده بر اساس منطق سی‌شارپ
    ROUND(ISNULL(fp.TotalBuyPriceInEuro, 0), 0) AS BuyPriceInEuro,   -- قیمت به یورو (گردشده)
    ROUND(ROUND(ISNULL(fp.TotalBuyPriceInEuro, 0), 0) * @CurrencyPrice, 0) AS SalePriceRateAzad,
    ROUND(ISNULL(fp.ExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0) AS External_TotalBuyPrice,
    ISNULL(price.InternalTotalBuyPrice, 0) AS Internal_TotalBuyPrice,
    -- قیمت خرید = خارجی + داخلی
    ROUND(ISNULL(fp.ExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0) + ISNULL(price.InternalTotalBuyPrice, 0) AS BuyPrice,
    -- قیمت فروش بر اساس نرخ فروش محصول
    (ROUND(ISNULL(fp.ExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0) + ISNULL(price.InternalTotalBuyPrice, 0)) * ISNULL(p.SaleRate, 1) AS SalePriceCandidate,
    -- قیمت فروش آزاد (از روی SalePriceFromRuro)
    ROUND(ISNULL(fp.SalePriceFromRuro, 0) * @CurrencyPrice, 0) AS SalePriceAzade,
    -- قیمت فروش نهایی: اگر قیمت کاندید بزرگتر از آزاد باشد، همان کاندید؛ در غیر این صورت آزاد
    CASE
        WHEN ((ROUND(ISNULL(fp.ExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0) + ISNULL(price.InternalTotalBuyPrice, 0)) * ISNULL(p.SaleRate, 1))
             > ROUND(ISNULL(fp.SalePriceFromRuro, 0) * @CurrencyPrice, 0)
        THEN ((ROUND(ISNULL(fp.ExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0) + ISNULL(price.InternalTotalBuyPrice, 0)) * ISNULL(p.SaleRate, 1))
        ELSE ROUND(ISNULL(fp.SalePriceFromRuro, 0) * @CurrencyPrice, 0)
    END AS SalePrice,
    @CurrencyPrice AS CurrencyRateAzad,
    fp.TotalBuyPriceInEuro,
    fp.SalePriceFromRuro,
    fp.ExternalTotalBuyPriceInEuro

FROM BaseQuery p

LEFT JOIN (
    SELECT
        ProductId,
        BuyPrice,
        InternalTotalBuyPrice,
        ExternalTotalBuyPrice,
        ROW_NUMBER() OVER (PARTITION BY ProductId ORDER BY (SELECT NULL)) AS rn
    FROM Edms.vw_PartDocumentPrice
    WHERE ProductId IN (SELECT ProductId FROM BaseQuery)
) price ON p.ProductId = price.ProductId AND price.rn = 1

LEFT JOIN (
    SELECT
        PartId,
        CurrencyActualPrice,
        ROW_NUMBER() OVER (PARTITION BY PartId ORDER BY CreatedOnMiladiDateTime DESC) AS rn
    FROM Sup.InquiryPartPrice
    WHERE Type != 1675
      AND PartId IN (SELECT ProductId FROM BaseQuery)
) lastPrice ON p.ProductId = lastPrice.PartId AND lastPrice.rn = 1

LEFT JOIN (
    SELECT DISTINCT ProductFormulId
    FROM Bom.vw_ProductItem
    WHERE ProductFormulId IN (SELECT Id FROM BaseQuery)
) bom ON p.Id = bom.ProductFormulId

LEFT JOIN (
    SELECT PartId, COUNT(*) AS DocumentCount
    FROM [Inv].[PartDocument]
    WHERE Main = 1
    GROUP BY PartId
) AS [pd] ON [pd].[PartId] = p.ProductId

LEFT JOIN finalPrice fp ON fp.ProductId = p.ProductId
