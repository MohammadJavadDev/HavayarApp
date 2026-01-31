using Common.Attributes;
using Entities.App.Inv;
using Entities.App.Sale.Enums;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
	[Display(Name = "اقلام سفارش ساخت")]
	[Table("ProductionOrderItem", Schema = "Sale")]
	public class ProductionOrderItem : BaseEntity
	{
		[DisplayName("سفارش ساخت")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public ProductionOrder ProductionOrder { get; set; }

		public long ProductionOrderId { get; set; }


		[DisplayName("ساخت داخل")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsBuildInside { get; set; } = false;


		[DisplayName("نوع دستگاه")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProductionOrderItemDeviceTypeEnum? DeviceType { get; set; }


		[DisplayName("نوع تجهیز")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProductionOrderItemEquipmentTypeEnum? EquipmentType { get; set; }


		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Part? Part { get; set; }

		public long? PartId { get; set; }


		[DisplayName("تعداد / مقدار")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? Amount { get; set; }


		[DisplayName("تاریخ تحویل توافقی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? AgreedDeliverDate { get; set; }


		[DisplayName("مدل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? PartModel { get; set; }


		[DisplayName("ایرند / طبقه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? AirendOrCategory { get; set; }


		[DisplayName("فشار ورودی")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? InputPressureBar { get; set; }


		[DisplayName("فشار خروجی")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? OutputPressureBar { get; set; }


		[DisplayName("ظرفیت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Capacity { get; set; }


		[DisplayName("مقیاس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? Scale { get; set; }


		[DisplayName("نوع گاز")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? GasType { get; set; }


		[DisplayName("دمای بیشینه")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? MaximumTemperature { get; set; }


		[DisplayName("درصد خلوص")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? PurityPercentage { get; set; }


		[DisplayName("رطوبت نسبی")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? RelativeHumidity { get; set; }


		[DisplayName("فشار بارومتریک")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? BarometricPressure { get; set; }


		[DisplayName("نوع محرک")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? MovingType { get; set; }


		[DisplayName("بازرسی حین ساخت")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool HasInspection { get; set; } = false;


		[DisplayName("ملاحظات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? SalesConsideration { get; set; }


		[DisplayName("نسخه")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? Revision { get; set; }


		[DisplayName("آخرین نسخه")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsLatestVersion { get; set; } = false;


		[DisplayName("وضعیت بررسی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProductionOrderItemCheckStatusEnum? CheckStatus { get; set; }


		[DisplayName("ورژن")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? Version { get; set; }


		[DisplayName("کد کالا")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? PartCode { get; set; }

		public long? HamkaranId { get; set; }

		[DisplayName("مرحله ساخت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProductionOrderItemProductionStepEnum ProductionStep { get; set; }


		[DisplayName("شماره برنامه ریزی")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long PlanningNumber { get; set; }

		[DisplayName("سریال")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? Serial { get; set; }


		[DisplayName("نوع سریال")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProductionOrderItemSerialTypeEnum SerialType { get; set; }



		[DisplayName("تاریخ میلادی شروع تولید")]
		[DisplayInfo(null, false, SystemType.Date, systemProprty: true)]
		public DateTime? ProductionStartMiladiDate { get; set; } = null;

		[DisplayName("تاریخ شمسی شروع تولید")]
		[MaxLength(30)]
		[DisplayInfo("ProductionStartMiladiDate", true, SystemType.DateShamsi, systemProprty: true)]
		public string? ProductionStartShamsiDate { get; set; } = null;



		[DisplayName("تاریخ میلادی پایان تولید")]
		[DisplayInfo(null, false, SystemType.Date, systemProprty: true)]
		public DateTime? ProductionEndMiladiDate { get; set; } = null;

		[DisplayName("تاریخ شمسی پایان تولید")]
		[MaxLength(30)]
		[DisplayInfo("ProductionEndMiladiDate", true, SystemType.DateShamsi, systemProprty: true)]
		public string? ProductionEndShamsiDate { get; set; } = null;



		[DisplayName("تاریخ میلادی پایان تست")]
		[DisplayInfo(null, false, SystemType.Date, systemProprty: true)]
		public DateTime? TestingEndMiladiDate { get; set; } = null;

		[DisplayName("تاریخ شمسی پایان تست")]
		[MaxLength(30)]
		[DisplayInfo("TestingEndMiladiDate", true, SystemType.DateShamsi, systemProprty: true)]
		public string? TestingEndShamsiDate { get; set; } = null;


		[DisplayName("تاریخ میلادی آماده سازی")]
		[DisplayInfo(null, false, SystemType.Date, systemProprty: true)]
		public DateTime? PreparationMiladiDate { get; set; } = null;

		[DisplayName("تاریخ شمسی آماده سازی")]
		[MaxLength(30)]
		[DisplayInfo("PreparationMiladiDate", true, SystemType.DateShamsi, systemProprty: true)]
		public string? PreparationShamsiDate { get; set; } = null;




		[DisplayName("تاریخ میلادی تحویل")]
		[DisplayInfo(null, false, SystemType.Date, systemProprty: true)]
		public DateTime? DeliveryMiladiDate { get; set; } = null;

		[DisplayName("تاریخ شمسی تحویل")]
		[MaxLength(30)]
		[DisplayInfo("DeliveryMiladiDate", true, SystemType.DateShamsi, systemProprty: true)]
		public string? DeliveryShamsiDate { get; set; } = null;

		 

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProductionOrderItemStatusEnum Status { get; set; }



		[DisplayName("وضعیت تولید")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProductionOrderItemProductionStatusEnum ProductionStatus { get; set; }


		


		[DisplayName("Bom اقلام سفارش ساخت")]
		[DisplayInfo(null, true, type: SystemType.ListEntity)]
		public List<ProductionOrderItemBom> ProductionOrderItemBom { get; set; } = new();



		[DisplayName(" اقلام سفارش ساخت کامنت ها")]
		[DisplayInfo(null, true, type: SystemType.ListEntity)]
		public List<ProductionOrderItemComment> ProductionOrderItemComments { get; set; } = new();


	}

	[Display(Name = " اقلام سفارش ساخت کامنت ها")]
	[Table("ProductionOrderItemComment", Schema = "Sale")]
	public class ProductionOrderItemComment : BaseEntity
	{
		public long ProductionOrderItemId { get; set; }
		public ProductionOrderItem ProductionOrderItem { get; set; }

		[DisplayName("وضعیت تولید")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProductionOrderItemProductionStatusEnum ProductionStatus { get; set; }


		[DisplayName("تاریخ میلادی شروع")]
		[DisplayInfo(null, false, SystemType.DateTime, systemProprty: true)]
		public DateTime? StartMiladiDateTime { get; set; } = null;

		[DisplayName("تاریخ شمسی شروع")]
		[MaxLength(30)]
		[DisplayInfo("StartMiladiDateTime", true, SystemType.DateShamsi, systemProprty: true)]
		public string? StartShamsiDate { get; set; } = null;

		[DisplayName("تاریخ میلادی پایان")]
		[DisplayInfo(null, false, SystemType.DateTime, systemProprty: true)]
		public DateTime? EndMiladiDateTime { get; set; } = null;

		[DisplayName("تاریخ شمسی پایان")]
		[MaxLength(30)]
		[DisplayInfo("EndMiladiDateTime", true, SystemType.DateShamsi, systemProprty: true)]
		public string? EndShamsiDate { get; set; } = null;

		[DisplayName("شناسه های علت های خرابی")]
		[DisplayInfo("FailureTypeIds", true, SystemType.ListLong)]
		public string? FailureTypeIds { get; set; }

		[DisplayName("عنوان های علت های خرابی")]
		[DisplayInfo("FailureTypeNames", true, SystemType.ListString)]
		public string? FailureTypeNames { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo("Comment", true, SystemType.String)]
		public string? Comment { get; set; }


		[DisplayName("شناسه های دریافت کنندگاه")]
		[DisplayInfo("CcReciversIds", true, SystemType.ListLong)]
		public string? CcReciversIds { get; set; }

		[DisplayName("نام های دریافت کنندگان")]
		[DisplayInfo("CcReciversNames", true, SystemType.ListString)]
		public string? CcReciversNames { get; set; }
	}


		[Display(Name = "Bom اقلام سفارش ساخت")]
	[Table("ProductionOrderItemBom", Schema = "Sale")]
	public class ProductionOrderItemBom : BaseEntity
	{
		public long ProductionOrderItemId { get; set; }
		public ProductionOrderItem ProductionOrderItem { get; set; }

		[DisplayName("نسخه")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Revision { get; set; }


		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public Part Part { get; set; }

		public long PartId { get; set; }


		[DisplayName("تعداد/مقدار")]
		[DisplayInfo(null, true, type: SystemType.Decimal, required: true)]
		public decimal Amount { get; set; }


		[DisplayName("آخرین نسخه هست")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsLatest { get; set; } = false;


		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Description { get; set; }


		[DisplayName("توضیحات واحد فروش")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? SaleUnitDetails { get; set; }


		[DisplayName("نیاز به AVL دارد")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool NeedsAVL { get; set; } = false;

		[DisplayName("مرحله ساخت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProductionOrderItemBomProductionStepEnum ProductionStep { get; set; }

		[DisplayName("تعداد تامین شده")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? NumberSupplied { get; set; }


		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProductionOrderItemBomEnum Status { get; set; }


	}
}
