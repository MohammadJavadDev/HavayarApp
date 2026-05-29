using Entities.App.Inv.Enums;

namespace WebApp.ViewModels.Edms;

public class DocumentProductListDto
{
	public long? Id { get; set; }
	public long? ProductFormulId { get; set; }
	public long? ProductFormul_ID { get; set; }
	public bool HaveAttachment { get; set; }
	public long PartId { get; set; }
	public long? ProductNameGroupId { get; set; }
	public string? ProductNameGroupName { get; set; }
	public DateTime? CreatedDate { get; set; }
	public string? Comment { get; set; }
	public string? PartCode { get; set; }
	public string? PartName { get; set; }
	public bool? IsRoutine { get; set; }
	public decimal? SalePrice { get; set; }
	public decimal? BuyPrice { get; set; }
	public long? InternalTotalBuyPrice { get; set; }
	public long? ExternalTotalBuyPrice { get; set; }
	public decimal? BuyPriceInEuro { get; set; }
	public int BomProductFormulAttachmentCount { get; set; }
	public bool HasBom { get; set; }
	public bool Foreign { get; set; }
	public bool EngineeringRoutine { get; set; }
	public bool? DataSheetUsage { get; set; }
	public PartDataSheetEnum? DataSheet { get; set; }
	public string? BrandInDataSheet { get; set; }
	public bool PreciseTool { get; set; }
	public int? PartExteraInfoRev { get; set; }
	public PartExtraInfoInfoTypeEnum? PartExteraInfoType { get; set; }
}
