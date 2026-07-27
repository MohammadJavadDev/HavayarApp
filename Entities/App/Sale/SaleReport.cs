using Common.Attributes;
using Entities.App.Sale.Enums;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
	[Display(Name = "گزارش کار")]
	[Table("Report", Schema = "Sale")]
	public class SaleReport : BaseEntity
	{

		[DisplayName("مدیریت ماموریت ها")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public SaleMission SaleMission { get; set; }
		public long? SaleMissionId { get; set; }

		[DisplayName("درخواست خدمات")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public ServiceRequest ServiceRequest { get; set; }
		public long? ServiceRequestId { get; set; }



		[DisplayName("جزئیات درخواست سرویس")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public ServiceRequestDetail ServiceRequestDetail { get; set; }
		public long? ServiceRequestDetailId { get; set; }


		[DisplayName("شماره گزارش")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? ReportNumber { get; set; }

		[DisplayName("تاریخ شروع سرویس میلادی")]
		[DisplayInfo(null, true, type: SystemType.DateTime)]
		public DateTime? ServiceStartMiladiDate { get; set; } = null;


		[DisplayName("تاریخ شروع سرویس شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateTimeShamsi)]
		public string? ServiceStartShamsiDate { get; set; }



		[DisplayName("زمان شروع")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? ServiceStartTime { get; set; }



		[DisplayName("تاریخ پایان سرویس میلادی")]
		[DisplayInfo(null, true, type: SystemType.DateTime)]
		public DateTime? ServiceEndMiladiDate { get; set; } = null;


		[DisplayName("تاریخ پایان سرویس شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateTimeShamsi)]
		public string? ServiceEndShamsiDate { get; set; }


		[DisplayName("زمان خانمه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? ServiceEndTime { get; set; }


		[DisplayName("شرح خرابی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public FailureTypeSale? FailureType { get; set; }
		public long? FailureTypeId { get; set; }



		[DisplayName("کد های عملیاتی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public RepairsType? RepairsType { get; set; }
		public string? RepairsTypeId { get; set; }
		//public long? RepairsTypeId { get; set; }


		[DisplayName("شرح موانع اجرایی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? ExecutiveBarriersDescription { get; set; }


		[DisplayName("ساعت کارکرد")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? TotalRunTime { get; set; }

		[DisplayName("ساعت کارکرد زیر بار")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? WorkHoursUnderload { get; set; }


		[DisplayName("شدت جریان زیر بار ")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? FlowIntensityUnderload { get; set; }

		[DisplayName("شدت جریان بدون بار ")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? FlowIntensityWithoutload { get; set; }

		[DisplayName("حداکثر فشار کاری")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? MaximumWorkingPressure { get; set; }

		[DisplayName("حداقل فشار کاری")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? MinimumWorkingPressure { get; set; }


		[DisplayName("دمای محیط")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? EnvironmentTemperature { get; set; }

		[DisplayName("دمای دستگاه")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? DeviceTemperature { get; set; }

		[DisplayName("ولتاژ تابلو برق")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? VoltagePowerGrid { get; set; }

		[DisplayName("مقدار دیوپوینت درایر")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? AmountDryerDyupoint { get; set; }


		[DisplayName("وضعیت کمپرسورخانه")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool CompressorHouseStatus { get; set; }

		[DisplayName("وضعیت تابلو برق")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool ElectricalPanelStatus { get; set; }


		[DisplayName("وضعیت پایپینگ")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool PipingStatus { get; set; }



		[DisplayName("نتیجه")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public MissionResultEnum MissionResult { get; set; }


		[DisplayName("تعویض برد")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? OldRunTime { get; set; }


		[DisplayName("توضیحات گارانتی کارشناس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? ExpertGuaranteeDateShamsi { get; set; }


		[DisplayName("توضیحات کارشناس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? ExpertGuaranteeComment { get; set; }



		[DisplayName("عنوان فایل")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".jpg,.png,.pdf,.rar,.zip,.excel,.word")]
		public FileEntity? SaleReportAttachment { get; set; }
		public long? SaleReportAttachmentId { get; set; }

		//رسیدگی

		[DisplayName("کنترل سطح روغن")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool OilLevelCheck { get; set; }


		[DisplayName("بازديد كليه اتصالات")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool InspectionOfAllConnections { get; set; }


		[DisplayName("بازديد مكش روغن")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool OilSuctionInspection { get; set; }



		[DisplayName("بازديد غبار گير")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool DustCollectorInspection { get; set; }

		[DisplayName("بازديد پولی و تسمه")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool InspectionOfTheseRpentinebeltTimingBelt { get; set; }


		[DisplayName("تعويض فيلتر روغن")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool OilFilterReplacement { get; set; }



		[DisplayName("تعويض فيلتر سپراتور")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool SeparatFilterReplacement { get; set; }


		[DisplayName("تعويض فيلتر هوا")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool AirFilterReplacement { get; set; }


		[DisplayName("تعويض كيت آنلودر")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool ReplacingTheunLoaderkit { get; set; }


		[DisplayName("تعويض كيت مينيمم")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool MinimumKitReplacement { get; set; }

		[DisplayName("تعويض كيت ترموستات")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool Replacingthethermostatkit { get; set; }


		[DisplayName("تعويض كيت ايراند")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IrankitReplacement { get; set; }


		[DisplayName("گريس كاري الكترموتور")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool ElectricMotorGreasing { get; set; }


		[DisplayName("تميز كردن ظاهر دستگاه")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool CleaningExteriorDevice { get; set; }


		[DisplayName("وضعيت كمپرسور خانه")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool StatusCompressor { get; set; }

		[DisplayName("سيستم خنك كننده")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool CoolingSystem { get; set; }

		[DisplayName("كنترل تابلو تغذيه كمپرسورخانه")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool CompressorRoomPowerSupplyPanelControl { get; set; }

		[DisplayName("(سيستم برق كمپرسور(تابلو برق)")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool CompressorElectricalSystemElectricalPanel { get; set; }


		[DisplayName("وضعيت تجهيزات جانبی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool StatusPeripheralEquipment { get; set; }


		[DisplayName("وضعيت پايپينگ")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool PipingCondition { get; set; }


		[DisplayName("دفترچه سرويس و نگهداری")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool ServiceMaintenanceManual { get; set; }


		[DisplayName("موارد فوق مورد تایید مشتری می باشد")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool TheAboveItemsApprovedCustomer { get; set; }


		[DisplayName("توضیحات سرپرست فنی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? TechnicalSupervisorsRemarks { get; set; }


		[DisplayName("توضیحات مسئول منطقه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? ExplanationByThereGionalOfficial { get; set; }

		[DisplayName("نظر سرپرست خدمات پس از فروش")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? OpinionofAfterSalesServiceSupervisor { get; set; }

		[DisplayName("نظر مدير خدمات پس از فروش")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? AfterSalesServiceManagersOpinion { get; set; }

	}
}


