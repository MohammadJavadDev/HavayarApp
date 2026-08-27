namespace WebApp.ViewModels.Bom;

public sealed class ProductPriceCompareRequest
{
	public string? SourceDate { get; set; }
}

public sealed class ProductPriceCompareDetailsRequest
{
	public long ProductFormulId { get; set; }
	public string? SourceDate { get; set; }
}

public sealed class ProductPriceCompareEmailRequest
{
	public List<long> ProductFormulIds { get; set; } = [];
	public string? SourceDate { get; set; }
	public string? Comment { get; set; }
}

public sealed class ProductPriceCompareRow
{
	public long ProductFormulId { get; set; }
	public long PartId { get; set; }
	public string? PartCode { get; set; }
	public string? PartName { get; set; }
	public string? ProductNameGroupName { get; set; }
	public bool? IsRoutine { get; set; }
	public string? Comment { get; set; }
	public DateTime? CreatedDate { get; set; }
	public string? CreatedOnShamsiDateTime { get; set; }
	public int DocumentCount { get; set; }
	public bool HasBom { get; set; }
	public int ComponentCount { get; set; }
	public int CurrentPricedComponentCount { get; set; }
	public int PreviousPricedComponentCount { get; set; }
	public bool SnapshotExists { get; set; }
	public DateTime? SnapshotDate { get; set; }
	public DateTime? EmailSentOnMiladiDateTime { get; set; }
	public decimal CurrencyRate { get; set; }
	public decimal CurrentInternalBuyPrice { get; set; }
	public decimal PreviousInternalBuyPrice { get; set; }
	public decimal CurrentExternalBuyPrice { get; set; }
	public decimal PreviousExternalBuyPrice { get; set; }
	public decimal CurrentBuyPrice { get; set; }
	public decimal PreviousBuyPrice { get; set; }
	public decimal? BuyPriceDifferencePercent { get; set; }
	public decimal CurrentSalePrice { get; set; }
	public decimal PreviousSalePrice { get; set; }
	public decimal? SalePriceDifferencePercent { get; set; }
	public decimal CurrentBuyPriceInEuro { get; set; }
	public decimal PreviousBuyPriceInEuro { get; set; }
	public decimal? EuroDifferencePercent { get; set; }
}

public sealed class ProductPriceCompareDetailRow
{
	public long PartId { get; set; }
	public string? PartCode { get; set; }
	public string? PartName { get; set; }
	public decimal UsingRate { get; set; }
	public bool IsForeign { get; set; }
	public decimal CurrentUnitPrice { get; set; }
	public decimal PreviousUnitPrice { get; set; }
	public decimal CurrentTotalPrice { get; set; }
	public decimal PreviousTotalPrice { get; set; }
	public decimal CurrentUnitPriceInEuro { get; set; }
	public decimal PreviousUnitPriceInEuro { get; set; }
	public decimal? PriceDifferencePercent { get; set; }
	public decimal? EuroDifferencePercent { get; set; }
	public DateTime? CurrentPriceDate { get; set; }
	public DateTime? PreviousPriceDate { get; set; }
	public int? CurrentPriceType { get; set; }
	public int? PreviousPriceType { get; set; }
	public string? CurrentCompanyName { get; set; }
	public string? PreviousCompanyName { get; set; }
}
