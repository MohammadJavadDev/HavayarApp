using Entities.App.Sale.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.App.Bom.Views
{

	public class vw_ProductItems
	{
		public long ProductFormulId { get; set; }
		public string PardCode { get; set; }
		public string PartName { get; set; }
		public decimal UsingRate { get; set; }
		public long PartId { get; set; }
		public long PartItemId { get; set; }
	}

	public class vw_ProductItemsConfiguration : IEntityTypeConfiguration<vw_ProductItems>
	{
		public void Configure(EntityTypeBuilder<vw_ProductItems> builder)
		{
			builder.ToView("vw_ProductItems", "Bom");
			builder.HasNoKey();
		}
	}
}



public class Vw_ServiceRequest_Detail
{
	public long ServiceRequestDetailID { get; set; }
	public long ServiceRequestID { get; set; }

	public string? RequestTypeID { get; set; }
	public string? RequestTypeTitle { get; set; }

	public decimal? OrderDetailID { get; set; }


	public decimal? OrderDetailSerialID { get; set; }

	public string? Serial { get; set; }
	public string? Model { get; set; }
	public string? ProductionOrderNumber { get; set; }

	public string? NoticeDate { get; set; }
	public string? NoticeTime { get; set; }

	public string? GuaranteeTime { get; set; }
	public string? HoursDaily { get; set; }
	public long? SaleReportID { get; set; }
	public long? SaleMissionId { get; set; }
	public long? SaleReportServiceRequestId { get; set; }
	public long? SaleReportServiceRequestDetailId { get; set; }

	public int? ReportNumber { get; set; }

	public DateTime? ServiceStartMiladiDate { get; set; }
	public string? ServiceStartShamsiDate { get; set; }
	public string? ServiceStartTime { get; set; }
	public long? Partid { get; set; }
	public string? PartCode { get; set; }
	public string? PartName { get; set; }

	public DateTime? ServiceEndMiladiDate { get; set; }
	public string? ServiceEndShamsiDate { get; set; }
	public string? ServiceEndTime { get; set; }

	public long? FailureTypeId { get; set; }
	public string? FailureTypeTitle { get; set; }

	public string? ExpertNameId { get; set; }

	public string? ExpertName { get; set; }

	public string? RepairsTypeId { get; set; }

	public string? ExecutiveBarriersDescription { get; set; }

	public long? TotalRunTime { get; set; }
	public long? WorkHoursUnderload { get; set; }

	public decimal? FlowIntensityUnderload { get; set; }
	public decimal? FlowIntensityWithoutload { get; set; }

	public decimal? MaximumWorkingPressure { get; set; }
	public decimal? MinimumWorkingPressure { get; set; }

	public decimal? EnvironmentTemperature { get; set; }
	public decimal? DeviceTemperature { get; set; }

	public int? VoltagePowerGrid { get; set; }

	public decimal? AmountDryerDyupoint { get; set; }

	public bool? CompressorHouseStatus { get; set; }
	public bool? ElectricalPanelStatus { get; set; }
	public bool? PipingStatus { get; set; }

	public MissionResultEnum? MissionResult { get; set; }

	public int? OldRunTime { get; set; }

	public string? ExpertGuaranteeDateShamsi { get; set; }
	public string? ExpertGuaranteeComment { get; set; }

	public long? SaleReportAttachmentId { get; set; }

	public bool? OilLevelCheck { get; set; }
	public bool? InspectionOfAllConnections { get; set; }
	public bool? OilSuctionInspection { get; set; }
	public bool? DustCollectorInspection { get; set; }
	public bool? InspectionOfTheseRpentinebeltTimingBelt { get; set; }
	public bool? OilFilterReplacement { get; set; }
	public bool? SeparatFilterReplacement { get; set; }
	public bool? AirFilterReplacement { get; set; }
	public bool? ReplacingTheunLoaderkit { get; set; }
	public bool? MinimumKitReplacement { get; set; }
	public bool? Replacingthethermostatkit { get; set; }
	public bool? IrankitReplacement { get; set; }
	public bool? ElectricMotorGreasing { get; set; }
	public bool? CleaningExteriorDevice { get; set; }
	public bool? StatusCompressor { get; set; }
	public bool? CoolingSystem { get; set; }
	public bool? CompressorRoomPowerSupplyPanelControl { get; set; }
	public bool? CompressorElectricalSystemElectricalPanel { get; set; }
	public bool? StatusPeripheralEquipment { get; set; }
	public bool? PipingCondition { get; set; }
	public bool? ServiceMaintenanceManual { get; set; }
	public bool? TheAboveItemsApprovedCustomer { get; set; }

	public string? TechnicalSupervisorsRemarks { get; set; }
	public string? ExplanationByThereGionalOfficial { get; set; }
	public string? OpinionofAfterSalesServiceSupervisor { get; set; }
	public string? AfterSalesServiceManagersOpinion { get; set; }
	public string? RequestDescription { get; set; }
}

public class vw_ProductItemsConfiguration : IEntityTypeConfiguration<Vw_ServiceRequest_Detail>
{
	public void Configure(EntityTypeBuilder<Vw_ServiceRequest_Detail> builder)
	{
		builder.ToView("Vw_ServiceRequest_Detail", "Sale");
		builder.HasNoKey();


	}
}

