using Common.Attributes;
using Common.Utilities;
using Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Data;

namespace App.BackgroundJob.Jobs.Bom;

public sealed class ProductPriceSnapshotJob(ApplicationDbContext db)
{
	[JobHandler(
		"ثبت روزانه snapshot قیمت محصولات",
		"قیمت خرید، فروش، یورو و پوشش اقلام BOM را روزانه برای مقایسه تاریخی ذخیره می‌کند.")]
	public async Task CaptureDailyProductPriceSnapshot(
		IJobLogger? jobLogger = null,
		CancellationToken cancellationToken = default)
	{
		var snapshotDate = DateTime.Today;
		var snapshotShamsiDate = snapshotDate.ToShamsiDate();

		if (jobLogger != null)
		{
			await jobLogger.LogInfoAsync(
				$"شروع ثبت snapshot قیمت محصولات برای {snapshotShamsiDate}",
				cancellationToken);
		}

		db.Database.SetCommandTimeout(0);

		var affectedRows = await db.Database.ExecuteSqlRawAsync(
			SnapshotSql,
			[
				new SqlParameter("@SnapshotDate", SqlDbType.Date) { Value = snapshotDate },
				new SqlParameter("@SnapshotShamsiDate", snapshotShamsiDate),
				new SqlParameter("@CapturedAt", SqlDbType.DateTime2) { Value = DateTime.Now }
			],
			cancellationToken);

		var snapshotCount = await db.Set<Entities.App.Bom.ProductPriceHistory>()
			.AsNoTracking()
			.CountAsync(x => x.SnapshotDate == snapshotDate, cancellationToken);

		if (jobLogger != null)
		{
			await jobLogger.LogInfoAsync(
				$"snapshot روز {snapshotShamsiDate} با {snapshotCount:N0} محصول ثبت شد (تعداد عملیات SQL: {affectedRows:N0}).",
				cancellationToken);
		}
	}

	private const string SnapshotSql = """
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @CurrencyPrice DECIMAL(18, 2);

SELECT TOP (1) @CurrencyPrice = CAST(FieldValueContainer.Number1 AS DECIMAL(18, 2))
FROM ERPS.erps.GNR3.Currency AS Currency
LEFT JOIN ERPS.erps.SYS3.FieldValueContainer AS FieldValueContainer
    ON FieldValueContainer.RecordID = Currency.CurrencyID
   AND FieldValueContainer.EntityCode = 240
WHERE Currency.CurrencyID = 257;

SET @CurrencyPrice = ISNULL(@CurrencyPrice, 0);

;WITH BaseQuery AS (
    SELECT
        pf.Id AS ProductFormulId,
        pf.ProductId,
        product.Code AS PartCode,
        product.Name AS PartName,
        CAST(CASE
            WHEN product.SaleRate IS NULL OR product.SaleRate = 0 THEN 1
            ELSE product.SaleRate
        END AS DECIMAL(28, 8)) AS ProductSaleRate
    FROM Bom.ProductFormul AS pf
    INNER JOIN Inv.Part AS product ON product.Id = pf.ProductId
),
CurrentForeignPrice AS (
    SELECT
        ip.PartId,
        ip.PriceUnitValue + ISNULL(ip.TransportationCost, 0) AS PriceValue,
        ROW_NUMBER() OVER (
            PARTITION BY ip.PartId
            ORDER BY ip.CreatedOnMiladiDateTime DESC, ip.Id DESC
        ) AS RowNumber
    FROM Sup.InquiryPartPrice AS ip
    WHERE ip.Type <> 1675
      AND ISNULL(ip.PriceStatus, 1) <> 2834
      AND ISNULL(ip.PriceUnitId, 1) <> 1
      AND ip.CreatedOnMiladiDateTime >= '2023-03-21'
),
CurrentRialPrice AS (
    SELECT
        ip.PartId,
        ISNULL(ip.UnitPrice, 0) AS PriceValue,
        ip.CreatedOnMiladiDateTime AS PriceDate,
        ROW_NUMBER() OVER (
            PARTITION BY ip.PartId
            ORDER BY ip.CreatedOnMiladiDateTime DESC, ip.Id DESC
        ) AS RowNumber
    FROM Sup.InquiryPartPrice AS ip
    WHERE ip.Type <> 1675
      AND ISNULL(ip.PriceStatus, 1) <> 2834
      AND ISNULL(ip.PriceUnitId, 1) = 1
      AND ip.CreatedOnMiladiDateTime >= '2023-03-21'
      -- مطابق HTS، قیمت ریالی فقط تا پایان سال 1403 معتبر است.
      AND ip.CreatedOnMiladiDateTime < '2025-03-21'
),
ComponentPrices AS (
    SELECT
        bom.Id AS ProductFormulId,
        bom.ProductId,
        CAST(ISNULL(bom.UsingRate, 1) AS DECIMAL(28, 8)) AS UsingRate,
        component.[Foreign] AS IsForeign,
        ISNULL(component.SaleRate, 1) AS ComponentSaleRate,
        currentValue.UnitPriceInEuro
    FROM dbo.vw_BomProductItem AS bom
    INNER JOIN Inv.Part AS component ON component.Id = bom.PartItemId
    LEFT JOIN CurrentForeignPrice AS foreignPrice
        ON foreignPrice.PartId = component.Id
       AND foreignPrice.RowNumber = 1
    LEFT JOIN CurrentRialPrice AS rialPrice
        ON rialPrice.PartId = component.Id
       AND rialPrice.RowNumber = 1
    OUTER APPLY (
        SELECT TOP (1) rate.FinalPrice
        FROM Acc.ExchangeRateArchive AS rate
        WHERE rate.PriceUnitId = 4
          AND rate.CreatedOnMiladiDateTime <= rialPrice.PriceDate
        ORDER BY rate.CreatedOnMiladiDateTime DESC, rate.Id DESC
    ) AS exchangeRate
    OUTER APPLY (
        VALUES (
            CAST(CASE
                WHEN foreignPrice.PriceValue IS NOT NULL AND foreignPrice.PriceValue > 1
                    THEN foreignPrice.PriceValue
                ELSE rialPrice.PriceValue / NULLIF(exchangeRate.FinalPrice, 0)
            END AS DECIMAL(28, 8))
        )
    ) AS currentValue(UnitPriceInEuro)
),
AggregatedPrice AS (
    SELECT
        prices.ProductFormulId,
        prices.ProductId,
        COUNT(*) AS ComponentCount,
        SUM(CASE WHEN prices.UnitPriceInEuro IS NULL THEN 0 ELSE 1 END) AS PricedComponentCount,
        CAST(SUM(prices.UnitPriceInEuro * prices.UsingRate) AS DECIMAL(28, 5)) AS TotalBuyPriceInEuro,
        CAST(SUM(CASE
            WHEN prices.IsForeign = 1 THEN prices.UnitPriceInEuro * prices.UsingRate
            ELSE 0
        END) AS DECIMAL(28, 5)) AS ExternalTotalBuyPriceInEuro,
        CAST(SUM(prices.UnitPriceInEuro * prices.UsingRate * prices.ComponentSaleRate) AS DECIMAL(28, 5)) AS SalePriceFromEuro
    FROM ComponentPrices AS prices
    GROUP BY prices.ProductFormulId, prices.ProductId
),
InternalPrice AS (
    SELECT
        ProductId,
        MAX(CAST(ISNULL(InternalTotalBuyPrice, 0) AS DECIMAL(28, 5))) AS InternalTotalBuyPrice
    FROM Edms.vw_PartDocumentPrice
    GROUP BY ProductId
),
PriceStage AS (
    SELECT
        base.ProductFormulId,
        base.ProductId,
        base.PartCode,
        base.PartName,
        @CurrencyPrice AS CurrencyRate,
        CAST(ISNULL(internalPrice.InternalTotalBuyPrice, 0) AS DECIMAL(28, 5)) AS InternalBuyPrice,
        CAST(ROUND(ISNULL(aggregatePrice.ExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0) AS DECIMAL(28, 5)) AS ExternalBuyPrice,
        CAST(CEILING(ISNULL(aggregatePrice.TotalBuyPriceInEuro, 0)) AS DECIMAL(28, 5)) AS BuyPriceInEuro,
        CAST(ISNULL(aggregatePrice.SalePriceFromEuro, 0) * base.ProductSaleRate AS DECIMAL(28, 5)) AS SalePriceFromEuro,
        base.ProductSaleRate,
        CAST(ISNULL(aggregatePrice.ComponentCount, 0) AS INT) AS ComponentCount,
        CAST(ISNULL(aggregatePrice.PricedComponentCount, 0) AS INT) AS PricedComponentCount
    FROM BaseQuery AS base
    LEFT JOIN AggregatedPrice AS aggregatePrice ON aggregatePrice.ProductFormulId = base.ProductFormulId
    LEFT JOIN InternalPrice AS internalPrice ON internalPrice.ProductId = base.ProductId
),
SnapshotSource AS (
    SELECT
        stage.ProductFormulId,
        stage.ProductId,
        stage.PartCode,
        stage.PartName,
        stage.CurrencyRate,
        stage.InternalBuyPrice,
        stage.ExternalBuyPrice,
        CAST(CASE
            WHEN stage.BuyPriceInEuro * stage.CurrencyRate
                 > stage.InternalBuyPrice + stage.ExternalBuyPrice
                THEN stage.BuyPriceInEuro * stage.CurrencyRate
            ELSE stage.InternalBuyPrice + stage.ExternalBuyPrice
        END AS DECIMAL(28, 5)) AS BuyPrice,
        stage.BuyPriceInEuro,
        CAST((CASE
            WHEN stage.BuyPriceInEuro * stage.CurrencyRate
                 > stage.InternalBuyPrice + stage.ExternalBuyPrice
                THEN stage.BuyPriceInEuro * stage.CurrencyRate
            ELSE stage.InternalBuyPrice + stage.ExternalBuyPrice
        END) * stage.ProductSaleRate AS DECIMAL(28, 5)) AS SalePrice,
        stage.ComponentCount,
        stage.PricedComponentCount
    FROM PriceStage AS stage
)
MERGE Bom.ProductPriceHistory WITH (HOLDLOCK) AS target
USING SnapshotSource AS source
    ON target.SnapshotDate = @SnapshotDate
   AND target.ProductFormulId = source.ProductFormulId
WHEN MATCHED THEN
    UPDATE SET
        target.ProductId = source.ProductId,
        target.PartCode = source.PartCode,
        target.PartName = source.PartName,
        target.SnapshotShamsiDate = @SnapshotShamsiDate,
        target.CurrencyRate = source.CurrencyRate,
        target.InternalBuyPrice = source.InternalBuyPrice,
        target.ExternalBuyPrice = source.ExternalBuyPrice,
        target.BuyPrice = source.BuyPrice,
        target.BuyPriceInEuro = source.BuyPriceInEuro,
        target.SalePrice = source.SalePrice,
        target.ComponentCount = source.ComponentCount,
        target.PricedComponentCount = source.PricedComponentCount,
        target.ModifiedDateMiladiDateTime = @CapturedAt,
        target.ModifiedDateShamsiDateTime = @SnapshotShamsiDate,
        target.ModifiedByName = N'سیستم'
WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        ProductFormulId,
        ProductId,
        PartCode,
        PartName,
        SnapshotDate,
        SnapshotShamsiDate,
        CurrencyRate,
        InternalBuyPrice,
        ExternalBuyPrice,
        BuyPrice,
        BuyPriceInEuro,
        SalePrice,
        ComponentCount,
        PricedComponentCount,
        CreatedOnMiladiDateTime,
        CreatedOnShamsiDateTime,
        CreatedByName,
        IsActive
    )
    VALUES (
        source.ProductFormulId,
        source.ProductId,
        source.PartCode,
        source.PartName,
        @SnapshotDate,
        @SnapshotShamsiDate,
        source.CurrencyRate,
        source.InternalBuyPrice,
        source.ExternalBuyPrice,
        source.BuyPrice,
        source.BuyPriceInEuro,
        source.SalePrice,
        source.ComponentCount,
        source.PricedComponentCount,
        @CapturedAt,
        @SnapshotShamsiDate,
        N'سیستم',
        1
    )
WHEN NOT MATCHED BY SOURCE
     AND target.SnapshotDate = @SnapshotDate THEN
    DELETE;
""";
}
