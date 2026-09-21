using Entities.App.Sale.Enums;

namespace Entities.App.Sale.DTO
{
	public class ServiceRequestPartPieceDto
	{
		public long Id { get; set; }
		public string? Date { get; set; }
		public string? ProductTitle { get; set; }
		public decimal? Value { get; set; }
		public long? Partref { get; set; }
		public long? MunitRef { get; set; }
		public decimal? LastBuyPrice { get; set; }
		public long? HavayarPartId { get; set; }
		public long? HavayarOrderDetailId { get; set; }
		public bool FromSaleOrder { get; set; }
	}

	public class ServiceRequestPartSaveRequest
	{
		public long? Id { get; set; }
		public long? ServiceRequestId { get; set; }
		public long? ServiceRequestDetailId { get; set; }
		public long? OrderDetailId { get; set; }
		public long? ReplacePartId { get; set; }
		public AfterSalesServiceTypeEnum ServiceType { get; set; }
		public decimal Mount { get; set; }
		public long? UnitPrice { get; set; }
		public bool IsRegisteredManually { get; set; }
		public int? HtsProjectVchItemId { get; set; }
		public long? HtsProjectVchPartId { get; set; }
		public short? ProjectVchNum { get; set; }
		public short? ProjectVchYear { get; set; }
		public string? ProjectVchShamsiDate { get; set; }
		public DateTime? ProjectVchMiladiDate { get; set; }
		public long? PartUnitId { get; set; }
		public long? CostCenterId { get; set; }
		public long? OtherCostCenterPartyId { get; set; }
		public bool? ReturnLicence { get; set; }
		public bool? ReturnDamagedPart { get; set; }
		public DamagedPartTypeEnum? DamagedPartType { get; set; }
		public string? DamagedPartIds { get; set; }
		public string? DamagedParts { get; set; }
		public string? DamagedPartDescription { get; set; }
		public bool? HasFailure { get; set; }
		public string? FailurePartIds { get; set; }
		public string? FailureParts { get; set; }
		public string? FailureDescription { get; set; }
		public string? Comment { get; set; }
		public long? SaleCenterId { get; set; }
		public List<ServiceRequestPartPieceDto>? Pieces { get; set; }
	}
}
