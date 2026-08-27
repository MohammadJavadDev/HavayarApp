using Common.Attributes;
using Common.Auth.Enums;
using Data;
using Data.Contracts;
using Entities.App.Bom;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;
using System.Net;
using System.Text;
using WebApp.ViewModels.Bom;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic;

[Route("Panel/Bom/[controller]")]
[ApiController]
[ApiResultFilter]
[ControllerInfo("مقایسه قیمت محصولات")]
public sealed class ProductPriceCompareController(
	IUnitOfWork unitOfWork,
	ApplicationDbContext db) : BaseController
{
	private static readonly string[] EmailRecipients =
	[
		"ahrary@havayar.com",
		"mehrafzoon@havayar.com",
		"dr.h-ghoroori@havayar.com",
		"mehrabi.h@havayar.com",
		"khodakarami.f@havayar.com",
		"mahdavirad.m@havayar.com",
		"aghamiri.s@havayar.com",
		"sohrabi.z@havayar.com",
		"faraji.p@havayar.com"
	];

	[HttpGet("[action]")]
	[ActionDisplayName("صفحه مقایسه قیمت محصولات", ActionAccessType.View, ActionAccessItemType.List)]
	public IActionResult List()
	{
		return View(@"\Views\Panel\Bom\ProductPriceCompare\List.cshtml");
	}

	[HttpGet("[action]")]
	[ActionDisplayName("دریافت تاریخ‌های snapshot قیمت", ActionAccessType.Api, ActionAccessItemType.FetchData)]
	public async Task<IActionResult> AvailableDates(CancellationToken cancellationToken)
	{
		var dates = await db.Set<ProductPriceHistory>()
			.AsNoTracking()
			.GroupBy(x => new { x.SnapshotDate, x.SnapshotShamsiDate })
			.Select(group => new
			{
				group.Key.SnapshotDate,
				group.Key.SnapshotShamsiDate,
				Count = group.Count()
			})
			.OrderByDescending(x => x.SnapshotDate)
			.Take(365)
			.ToListAsync(cancellationToken);

		return Ok(dates);
	}

	[HttpPost("[action]")]
	[ActionDisplayName("دریافت مقایسه قیمت محصولات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
	public async Task<IActionResult> FetchData(
		[FromBody] ProductPriceCompareRequest request,
		CancellationToken cancellationToken)
	{
		if (!TryParseSourceDate(request.SourceDate, out var sourceDate, out var error))
			return BadRequest(error);

		var rows = await LoadComparisonRowsAsync(sourceDate, cancellationToken);
		return Ok(rows);
	}

	[HttpPost("[action]")]
	[ActionDisplayName("دریافت جزئیات مقایسه قیمت BOM", ActionAccessType.Api, ActionAccessItemType.FetchData)]
	public async Task<IActionResult> Details(
		[FromBody] ProductPriceCompareDetailsRequest request,
		CancellationToken cancellationToken)
	{
		if (request.ProductFormulId <= 0)
			return BadRequest("شناسه فرمول محصول معتبر نیست.");

		if (!TryParseSourceDate(request.SourceDate, out var sourceDate, out var error))
			return BadRequest(error);

		var sourceDateParameter = CreateSourceDateParameter(sourceDate);
		var productFormulIdParameter = new SqlParameter("@ProductFormulId", request.ProductFormulId);
		var rows = await db.Database
			.SqlQueryRaw<ProductPriceCompareDetailRow>(DetailQuery, sourceDateParameter, productFormulIdParameter)
			.ToListAsync(cancellationToken);

		return Ok(rows);
	}

	[HttpPost("[action]")]
	[ActionDisplayName("ارسال ایمیل مقایسه قیمت محصولات", ActionAccessType.Api, ActionAccessItemType.Custom)]
	public async Task<IActionResult> SendEmail(
		[FromBody] ProductPriceCompareEmailRequest request,
		CancellationToken cancellationToken)
	{
		if (request.ProductFormulIds.Count == 0)
			return BadRequest("حداقل یک محصول را انتخاب کنید.");

		if (string.IsNullOrWhiteSpace(request.Comment))
			return BadRequest("متن ایمیل الزامی است.");

		if (!CurrentUserId.HasValue)
			return Unauthorized();

		if (!TryParseSourceDate(request.SourceDate, out var sourceDate, out var error))
			return BadRequest(error);

		var selectedIds = request.ProductFormulIds.Distinct().ToHashSet();
		var rows = (await LoadComparisonRowsAsync(sourceDate, cancellationToken))
			.Where(row => selectedIds.Contains(row.ProductFormulId))
			.OrderBy(row => row.PartCode)
			.ToList();

		if (rows.Count == 0)
			return BadRequest("محصول انتخاب‌شده در گزارش یافت نشد.");

		var notification = new Notification
		{
			Type = NotificationType.Email,
			Title = "اعلام تغییر قیمت محصول",
			Body = BuildEmailBody(rows, request.Comment, request.SourceDate),
			OwnerId = CurrentUserId.Value,
			ViewPath = "/Panel/Bom/ProductPriceCompare/List",
			IsRead = false,
			IsSend = false,
			ToEmails = EmailRecipients.ToList(),
			CcEmails = CurrentUserEmail is { Length: > 0 } ? [CurrentUserEmail] : []
		};

		await unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);

		var sentAt = DateTime.Now;
		var snapshotRows = await db.Set<ProductPriceHistory>()
			.Where(x => x.SnapshotDate == sourceDate.Date && selectedIds.Contains(x.ProductFormulId))
			.ToListAsync(cancellationToken);

		foreach (var snapshotRow in snapshotRows)
		{
			snapshotRow.EmailSentOnMiladiDateTime = sentAt;
			snapshotRow.EmailSentOnShamsiDateTime = ToShamsiDateTime(sentAt);
		}

		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Ok(new { Count = rows.Count });
	}

	private async Task<List<ProductPriceCompareRow>> LoadComparisonRowsAsync(
		DateTime sourceDate,
		CancellationToken cancellationToken)
	{
		var sourceDateParameter = CreateSourceDateParameter(sourceDate);
		return await db.Database
			.SqlQueryRaw<ProductPriceCompareRow>(MainQuery, sourceDateParameter)
			.ToListAsync(cancellationToken);
	}

	private static bool TryParseSourceDate(
		string? value,
		out DateTime sourceDate,
		out string? error)
	{
		sourceDate = DateTime.Today;
		error = null;

		if (string.IsNullOrWhiteSpace(value))
			return true;

		var normalized = NormalizeDigits(value.Trim());
		var datePart = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
		var parts = datePart.Split(['/', '-'], StringSplitOptions.RemoveEmptyEntries);

		if (parts.Length != 3
			|| !int.TryParse(parts[0], out var year)
			|| !int.TryParse(parts[1], out var month)
			|| !int.TryParse(parts[2], out var day))
		{
			error = "تاریخ مبنا معتبر نیست. قالب صحیح 1405/06/01 است.";
			return false;
		}

		try
		{
			sourceDate = year < 1700
				? new PersianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0)
				: new DateTime(year, month, day);
		}
		catch (ArgumentOutOfRangeException)
		{
			error = "تاریخ مبنا معتبر نیست.";
			return false;
		}

		if (sourceDate.Date > DateTime.Today)
		{
			error = "تاریخ مبنا نمی‌تواند بعد از امروز باشد.";
			return false;
		}

		// بخشی از جداول قدیمی SQL Server از نوع datetime هستند و تاریخ قبل از 1753 را نمی‌پذیرند.
		if (sourceDate.Date < System.Data.SqlTypes.SqlDateTime.MinValue.Value.Date)
		{
			error = "تاریخ مبنا خارج از بازه قابل قبول سیستم است.";
			return false;
		}

		return true;
	}

	private static SqlParameter CreateSourceDateParameter(DateTime sourceDate)
	{
		return new SqlParameter("@SourceDate", SqlDbType.Date)
		{
			Value = sourceDate.Date
		};
	}

	private static string NormalizeDigits(string value)
	{
		return value
			.Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
			.Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9')
			.Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
			.Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9');
	}

	private static string ToShamsiDateTime(DateTime value)
	{
		var calendar = new PersianCalendar();
		return $"{calendar.GetYear(value):0000}/{calendar.GetMonth(value):00}/{calendar.GetDayOfMonth(value):00} {value:HH:mm:ss}";
	}

	private static string BuildEmailBody(
		IReadOnlyCollection<ProductPriceCompareRow> rows,
		string comment,
		string? sourceDate)
	{
		var html = new StringBuilder();
		html.Append("<div style='direction:rtl;text-align:right;font-family:Tahoma'>")
			.Append("<p>با سلام</p><p>تغییر قیمت محصولات نسبت به تاریخ مبنا ")
			.Append(WebUtility.HtmlEncode(sourceDate))
			.Append(" به شرح زیر اعلام می‌شود:</p>")
			.Append("<table style='width:100%;border-collapse:collapse' border='1' cellpadding='6'>")
			.Append("<thead><tr style='background:#e9ecef'><th>کد کالا</th><th>عنوان کالا</th><th>قیمت خرید مبنا</th><th>قیمت خرید جاری</th><th>درصد تغییر</th></tr></thead><tbody>");

		foreach (var row in rows)
		{
			html.Append("<tr><td>").Append(WebUtility.HtmlEncode(row.PartCode))
				.Append("</td><td>").Append(WebUtility.HtmlEncode(row.PartName))
				.Append("</td><td>").Append(row.PreviousBuyPrice.ToString("N0"))
				.Append("</td><td>").Append(row.CurrentBuyPrice.ToString("N0"))
				.Append("</td><td>").Append(row.BuyPriceDifferencePercent?.ToString("N2") ?? "-")
				.Append("%</td></tr>");
		}

		html.Append("</tbody></table><p><strong>توضیحات:</strong> ")
			.Append(WebUtility.HtmlEncode(comment).Replace("\r\n", "<br>").Replace("\n", "<br>"))
			.Append("</p></div>");

		return html.ToString();
	}

	private const string PriceCtes = """
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
        pf.ProductNameGroupId,
        pf.CreatedOnMiladiDateTime,
        pf.CreatedOnShamsiDateTime,
        pf.Comment,
        product.Code AS PartCode,
        product.Name AS PartName,
        product.DesignTypeIsRoutine AS IsRoutine,
        CAST(CASE
            WHEN product.SaleRate IS NULL OR product.SaleRate = 0 THEN 1
            ELSE product.SaleRate
        END AS DECIMAL(28, 8)) AS ProductSaleRate,
        pg.Name AS ProductNameGroupName
    FROM Bom.ProductFormul AS pf
    INNER JOIN Inv.Part AS product ON product.Id = pf.ProductId
    LEFT JOIN Bom.ProductGroup AS pg ON pg.Id = pf.ProductNameGroupId
),
CurrentForeignPrice AS (
    SELECT
        ip.PartId,
        ip.PriceUnitValue + ISNULL(ip.TransportationCost, 0) AS PriceValue,
        ip.CreatedOnMiladiDateTime AS PriceDate,
        CAST(ip.Type AS INT) AS PriceType,
        ip.CompanyName,
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
        CAST(ip.Type AS INT) AS PriceType,
        ip.CompanyName,
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
PreviousForeignPrice AS (
    SELECT
        ip.PartId,
        ip.PriceUnitValue + ISNULL(ip.TransportationCost, 0) AS PriceValue,
        ip.CreatedOnMiladiDateTime AS PriceDate,
        CAST(ip.Type AS INT) AS PriceType,
        ip.CompanyName,
        ROW_NUMBER() OVER (
            PARTITION BY ip.PartId
            ORDER BY ip.CreatedOnMiladiDateTime DESC, ip.Id DESC
        ) AS RowNumber
    FROM Sup.InquiryPartPrice AS ip
    WHERE ip.Type <> 1675
      AND ISNULL(ip.PriceStatus, 1) <> 2834
      AND ISNULL(ip.PriceUnitId, 1) <> 1
      AND ip.CreatedOnMiladiDateTime >= '2023-03-21'
      AND ip.CreatedOnMiladiDateTime < DATEADD(DAY, 1, @SourceDate)
),
PreviousRialPrice AS (
    SELECT
        ip.PartId,
        ISNULL(ip.UnitPrice, 0) AS PriceValue,
        ip.CreatedOnMiladiDateTime AS PriceDate,
        CAST(ip.Type AS INT) AS PriceType,
        ip.CompanyName,
        ROW_NUMBER() OVER (
            PARTITION BY ip.PartId
            ORDER BY ip.CreatedOnMiladiDateTime DESC, ip.Id DESC
        ) AS RowNumber
    FROM Sup.InquiryPartPrice AS ip
    WHERE ip.Type <> 1675
      AND ISNULL(ip.PriceStatus, 1) <> 2834
      AND ISNULL(ip.PriceUnitId, 1) = 1
      AND ip.CreatedOnMiladiDateTime >= '2023-03-21'
      AND ip.CreatedOnMiladiDateTime < DATEADD(DAY, 1, @SourceDate)
      AND ip.CreatedOnMiladiDateTime < '2025-03-21'
),
ComponentPrices AS (
    SELECT
        bom.Id AS ProductFormulId,
        bom.ProductId,
        component.Id AS PartId,
        component.Code AS PartCode,
        component.Name AS PartName,
        CAST(ISNULL(bom.UsingRate, 1) AS DECIMAL(28, 8)) AS UsingRate,
        component.[Foreign] AS IsForeign,
        ISNULL(component.SaleRate, 1) AS ComponentSaleRate,
        currentValue.UnitPriceInEuro AS CurrentUnitPriceInEuro,
        previousValue.UnitPriceInEuro AS PreviousUnitPriceInEuro,
        CASE WHEN cf.PriceValue IS NOT NULL AND cf.PriceValue > 1 THEN cf.PriceDate ELSE cr.PriceDate END AS CurrentPriceDate,
        CASE WHEN pf.PriceValue IS NOT NULL AND pf.PriceValue > 1 THEN pf.PriceDate ELSE pr.PriceDate END AS PreviousPriceDate,
        CASE WHEN cf.PriceValue IS NOT NULL AND cf.PriceValue > 1 THEN cf.PriceType ELSE cr.PriceType END AS CurrentPriceType,
        CASE WHEN pf.PriceValue IS NOT NULL AND pf.PriceValue > 1 THEN pf.PriceType ELSE pr.PriceType END AS PreviousPriceType,
        CASE WHEN cf.PriceValue IS NOT NULL AND cf.PriceValue > 1 THEN cf.CompanyName ELSE cr.CompanyName END AS CurrentCompanyName,
        CASE WHEN pf.PriceValue IS NOT NULL AND pf.PriceValue > 1 THEN pf.CompanyName ELSE pr.CompanyName END AS PreviousCompanyName,
        CASE WHEN cf.PriceValue IS NOT NULL AND cf.PriceValue > 1 THEN cf.PriceValue * @CurrencyPrice ELSE cr.PriceValue END AS CurrentUnitPrice,
        CASE WHEN pf.PriceValue IS NOT NULL AND pf.PriceValue > 1 THEN pf.PriceValue * @CurrencyPrice ELSE pr.PriceValue END AS PreviousUnitPrice
    FROM dbo.vw_BomProductItem AS bom
    INNER JOIN Inv.Part AS component ON component.Id = bom.PartItemId
    LEFT JOIN CurrentForeignPrice AS cf ON cf.PartId = component.Id AND cf.RowNumber = 1
    LEFT JOIN CurrentRialPrice AS cr ON cr.PartId = component.Id AND cr.RowNumber = 1
    OUTER APPLY (
        SELECT TOP (1) rate.FinalPrice
        FROM Acc.ExchangeRateArchive AS rate
        WHERE rate.PriceUnitId = 4
          AND rate.CreatedOnMiladiDateTime <= cr.PriceDate
        ORDER BY rate.CreatedOnMiladiDateTime DESC, rate.Id DESC
    ) AS currentRate
    LEFT JOIN PreviousForeignPrice AS pf ON pf.PartId = component.Id AND pf.RowNumber = 1
    LEFT JOIN PreviousRialPrice AS pr ON pr.PartId = component.Id AND pr.RowNumber = 1
    OUTER APPLY (
        SELECT TOP (1) rate.FinalPrice
        FROM Acc.ExchangeRateArchive AS rate
        WHERE rate.PriceUnitId = 4
          AND rate.CreatedOnMiladiDateTime <= pr.PriceDate
        ORDER BY rate.CreatedOnMiladiDateTime DESC, rate.Id DESC
    ) AS previousRate
    OUTER APPLY (
        VALUES (
            CAST(CASE
                WHEN cf.PriceValue IS NOT NULL AND cf.PriceValue > 1 THEN cf.PriceValue
                ELSE cr.PriceValue / NULLIF(currentRate.FinalPrice, 0)
            END AS DECIMAL(28, 8))
        )
    ) AS currentValue(UnitPriceInEuro)
    OUTER APPLY (
        VALUES (
            CAST(CASE
                WHEN pf.PriceValue IS NOT NULL AND pf.PriceValue > 1 THEN pf.PriceValue
                ELSE pr.PriceValue / NULLIF(previousRate.FinalPrice, 0)
            END AS DECIMAL(28, 8))
        )
    ) AS previousValue(UnitPriceInEuro)
),
AggregatedPrice AS (
    SELECT
        prices.ProductFormulId,
        prices.ProductId,
        COUNT(*) AS ComponentCount,
        SUM(CASE WHEN prices.CurrentUnitPriceInEuro IS NULL THEN 0 ELSE 1 END) AS CurrentPricedComponentCount,
        SUM(CASE WHEN prices.PreviousUnitPriceInEuro IS NULL THEN 0 ELSE 1 END) AS PreviousPricedComponentCount,
        CAST(SUM(prices.CurrentUnitPriceInEuro * prices.UsingRate) AS DECIMAL(28, 5)) AS CurrentTotalBuyPriceInEuro,
        CAST(SUM(prices.PreviousUnitPriceInEuro * prices.UsingRate) AS DECIMAL(28, 5)) AS PreviousTotalBuyPriceInEuro,
        CAST(SUM(CASE WHEN prices.IsForeign = 1 THEN prices.CurrentUnitPriceInEuro * prices.UsingRate ELSE 0 END) AS DECIMAL(28, 5)) AS CurrentExternalTotalBuyPriceInEuro,
        CAST(SUM(CASE WHEN prices.IsForeign = 1 THEN prices.PreviousUnitPriceInEuro * prices.UsingRate ELSE 0 END) AS DECIMAL(28, 5)) AS PreviousExternalTotalBuyPriceInEuro,
        CAST(SUM(prices.CurrentUnitPriceInEuro * prices.UsingRate * prices.ComponentSaleRate) AS DECIMAL(28, 5)) AS CurrentSalePriceFromEuro,
        CAST(SUM(prices.PreviousUnitPriceInEuro * prices.UsingRate * prices.ComponentSaleRate) AS DECIMAL(28, 5)) AS PreviousSalePriceFromEuro
    FROM ComponentPrices AS prices
    GROUP BY prices.ProductFormulId, prices.ProductId
),
InternalPrice AS (
    SELECT
        ProductId,
        MAX(CAST(ISNULL(InternalTotalBuyPrice, 0) AS DECIMAL(28, 5))) AS InternalTotalBuyPrice
    FROM Edms.vw_PartDocumentPrice
    GROUP BY ProductId
)
""";

	private static readonly string MainQuery = PriceCtes + """
, PriceStage AS (
    SELECT
        base.ProductFormulId,
        base.ProductId AS PartId,
        base.PartCode,
        base.PartName,
        base.ProductNameGroupName,
        base.IsRoutine,
        base.Comment,
        base.CreatedOnMiladiDateTime AS CreatedDate,
        base.CreatedOnShamsiDateTime,
        CAST(ISNULL(document.DocumentCount, 0) AS INT) AS DocumentCount,
        CAST(CASE WHEN aggregatePrice.ProductId IS NULL THEN 0 ELSE 1 END AS BIT) AS HasBom,
        CAST(ISNULL(aggregatePrice.ComponentCount, 0) AS INT) AS ComponentCount,
        CAST(ISNULL(aggregatePrice.CurrentPricedComponentCount, 0) AS INT) AS CurrentPricedComponentCount,
        CAST(ISNULL(history.PricedComponentCount, 0) AS INT) AS PreviousPricedComponentCount,
        CAST(CASE WHEN history.Id IS NULL THEN 0 ELSE 1 END AS BIT) AS SnapshotExists,
        history.SnapshotDate,
        history.EmailSentOnMiladiDateTime,
        @CurrencyPrice AS CurrencyRate,
        CAST(ISNULL(internalPrice.InternalTotalBuyPrice, 0) AS DECIMAL(28, 5)) AS CurrentInternalBuyPrice,
        CAST(ISNULL(history.InternalBuyPrice, 0) AS DECIMAL(28, 5)) AS PreviousInternalBuyPrice,
        CAST(ROUND(ISNULL(aggregatePrice.CurrentExternalTotalBuyPriceInEuro, 0) * @CurrencyPrice, 0) AS DECIMAL(28, 5)) AS CurrentExternalBuyPrice,
        CAST(ISNULL(history.ExternalBuyPrice, 0) AS DECIMAL(28, 5)) AS PreviousExternalBuyPrice,
        CAST(CEILING(ISNULL(aggregatePrice.CurrentTotalBuyPriceInEuro, 0)) AS DECIMAL(28, 5)) AS CurrentBuyPriceInEuro,
        CAST(ISNULL(history.BuyPriceInEuro, 0) AS DECIMAL(28, 5)) AS PreviousBuyPriceInEuro,
        CAST(ISNULL(history.BuyPrice, 0) AS DECIMAL(28, 5)) AS PreviousBuyPrice,
        CAST(ISNULL(history.SalePrice, 0) AS DECIMAL(28, 5)) AS PreviousSalePrice,
        CAST(ISNULL(aggregatePrice.CurrentSalePriceFromEuro, 0) * base.ProductSaleRate AS DECIMAL(28, 5)) AS CurrentSalePriceFromEuro,
        base.ProductSaleRate
    FROM BaseQuery AS base
    LEFT JOIN AggregatedPrice AS aggregatePrice ON aggregatePrice.ProductFormulId = base.ProductFormulId
    LEFT JOIN InternalPrice AS internalPrice ON internalPrice.ProductId = base.ProductId
    LEFT JOIN Bom.ProductPriceHistory AS history
        ON history.ProductFormulId = base.ProductFormulId
       AND history.SnapshotDate = @SourceDate
    LEFT JOIN (
        SELECT PartId, COUNT(*) AS DocumentCount
        FROM Inv.PartDocument
        WHERE Main = 1
        GROUP BY PartId
    ) AS document ON document.PartId = base.ProductId
),
CalculatedPrice AS (
    SELECT
        stage.*,
        CAST(CASE
            WHEN stage.CurrentBuyPriceInEuro * stage.CurrencyRate
                 > stage.CurrentExternalBuyPrice + stage.CurrentInternalBuyPrice
                THEN stage.CurrentBuyPriceInEuro * stage.CurrencyRate
            ELSE stage.CurrentExternalBuyPrice + stage.CurrentInternalBuyPrice
        END AS DECIMAL(28, 5)) AS CurrentBuyPrice
    FROM PriceStage AS stage
),
FinalPrice AS (
    SELECT
        price.*,
        CAST(price.CurrentBuyPrice * price.ProductSaleRate AS DECIMAL(28, 5)) AS CurrentSalePrice
    FROM CalculatedPrice AS price
)
SELECT
    ProductFormulId,
    PartId,
    PartCode,
    PartName,
    ProductNameGroupName,
    IsRoutine,
    Comment,
    CreatedDate,
    CreatedOnShamsiDateTime,
    DocumentCount,
    HasBom,
    ComponentCount,
    CurrentPricedComponentCount,
    PreviousPricedComponentCount,
    SnapshotExists,
    SnapshotDate,
    EmailSentOnMiladiDateTime,
    CurrencyRate,
    CurrentInternalBuyPrice,
    PreviousInternalBuyPrice,
    CurrentExternalBuyPrice,
    PreviousExternalBuyPrice,
    CurrentBuyPrice,
    PreviousBuyPrice,
    CAST((CurrentBuyPrice - PreviousBuyPrice) * 100.0 / NULLIF(PreviousBuyPrice, 0) AS DECIMAL(18, 2)) AS BuyPriceDifferencePercent,
    CurrentSalePrice,
    PreviousSalePrice,
    CAST((CurrentSalePrice - PreviousSalePrice) * 100.0 / NULLIF(PreviousSalePrice, 0) AS DECIMAL(18, 2)) AS SalePriceDifferencePercent,
    CurrentBuyPriceInEuro,
    PreviousBuyPriceInEuro,
    CAST((CurrentBuyPriceInEuro - PreviousBuyPriceInEuro) * 100.0 / NULLIF(PreviousBuyPriceInEuro, 0) AS DECIMAL(18, 2)) AS EuroDifferencePercent
FROM FinalPrice
ORDER BY PartCode, ProductFormulId;
""";

	private static readonly string DetailQuery = PriceCtes + """
SELECT
    prices.PartId,
    prices.PartCode,
    prices.PartName,
    prices.UsingRate,
    prices.IsForeign,
    CAST(ISNULL(prices.CurrentUnitPrice, 0) AS DECIMAL(28, 5)) AS CurrentUnitPrice,
    CAST(ISNULL(prices.PreviousUnitPrice, 0) AS DECIMAL(28, 5)) AS PreviousUnitPrice,
    CAST(ISNULL(prices.CurrentUnitPrice, 0) * prices.UsingRate AS DECIMAL(28, 5)) AS CurrentTotalPrice,
    CAST(ISNULL(prices.PreviousUnitPrice, 0) * prices.UsingRate AS DECIMAL(28, 5)) AS PreviousTotalPrice,
    CAST(ISNULL(prices.CurrentUnitPriceInEuro, 0) AS DECIMAL(28, 5)) AS CurrentUnitPriceInEuro,
    CAST(ISNULL(prices.PreviousUnitPriceInEuro, 0) AS DECIMAL(28, 5)) AS PreviousUnitPriceInEuro,
    CAST((prices.CurrentUnitPrice - prices.PreviousUnitPrice) * 100.0 / NULLIF(prices.PreviousUnitPrice, 0) AS DECIMAL(18, 2)) AS PriceDifferencePercent,
    CAST((prices.CurrentUnitPriceInEuro - prices.PreviousUnitPriceInEuro) * 100.0 / NULLIF(prices.PreviousUnitPriceInEuro, 0) AS DECIMAL(18, 2)) AS EuroDifferencePercent,
    prices.CurrentPriceDate,
    prices.PreviousPriceDate,
    prices.CurrentPriceType,
    prices.PreviousPriceType,
    prices.CurrentCompanyName,
    prices.PreviousCompanyName
FROM ComponentPrices AS prices
WHERE prices.ProductFormulId = @ProductFormulId
ORDER BY prices.PartCode, prices.PartId;
""";
}
