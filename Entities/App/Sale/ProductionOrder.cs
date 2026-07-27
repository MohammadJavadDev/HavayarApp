using Common.Attributes;
using Entities.App.FIN;
using Entities.App.Gnr;
using Entities.App.Sale.Enums;
using Entities.App.SLS;
using Entities.Auth;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
	[Display(Name = "سفارش ساخت")]
	[Table("ProductionOrder", Schema = "Sale")]
	public class ProductionOrder : BaseEntity
	{
		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public ProductionOrderStateEnum State { get; set; }


		[DisplayName("قرارداد")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Contract? Contract { get; set; }

		public long? ContractId { get; set; }


		[DisplayName("تفصیل پروژه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public DL? ProjectDl { get; set; }

		public long? ProjectDlId { get; set; }


		[DisplayName("کاربر نهایی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? EndUser { get; set; }


		[DisplayName("تاریخ شروع پروژه")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? ProjectStartDate { get; set; }


		[DisplayName("تاریخ متره")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? MetreDate { get; set; }


		[DisplayName("شماره متر")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? MetreNumber { get; set; }


		[DisplayName("تاریخ تحویل موافقت ‌شده")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? AgreedDeliveryDate { get; set; }


		[DisplayName("مدیر فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? SalesManager { get; set; }

		public long? SalesManagerId { get; set; }


		[DisplayName("مدیر پروژه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? ProjectManager { get; set; }

		public long? ProjectManagerId { get; set; }


		[DisplayName("کارشناس فروش جایگزین")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? AlternativeExpert { get; set; }

		public long? AlternativeExpertId { get; set; }


		[DisplayName("نمایندگی فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Customer? SalesAgency { get; set; }

		public long? SalesAgencyId { get; set; }


		[DisplayName("دپارتمان فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Branch? Branch { get; set; }

		public long? BranchId { get; set; }

		

		[DisplayName("شهر نصب")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Region? InstallationCity { get; set; }

		public long? InstallationCityId { get; set; }


		[DisplayName("نیاز به بازرسی قبل از بسته‌بندی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsNeedInspectionBeforePacking { get; set; } = false;


		[DisplayName("دارای پیمانکار")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool HasContractor { get; set; } = false;


		[DisplayName("کمپرسورخانه آماده است")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool CompressorHouseIsReady { get; set; } = false;


		[DisplayName("نصب بعهده هوایار")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool InstallationIsByHy { get; set; } = false;


		[DisplayName("دارای تعهدات مالی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool HasFinancialGuarantee { get; set; } = false;


		[DisplayName("توضیحات فروش")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? SalesComment { get; set; }


		[DisplayName("توضیحات ")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }


		[DisplayName("کمپرسور سانتریفیوژ دارد")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool CentrifugeHasCompressor { get; set; } = false;


		[DisplayName("تعداد")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? CentrifugeCompressorCount { get; set; }


		[DisplayName("ولتاژ الکتروموتور")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? CentrifugeElectromotorVoltage { get; set; }


		[DisplayName("پیوست تایید مدیریت")]
		[DisplayInfo(null, true, type: SystemType.File)]

		[ForeignKey(nameof(ManagementConfirmationAttachmentId))]
		public FileEntity? ManagementConfirmationAttachmentFile { get; set; }

		public long? ManagementConfirmationAttachmentId { get; set; }


		[DisplayName("پیوست فاکتور پیش پرداخت")]
		[DisplayInfo(null, true, type: SystemType.File)]
		[ForeignKey(nameof(DepositFactorAttachmentId))]
		public FileEntity? DepositFactorAttachmentFile { get; set; }

		public long? DepositFactorAttachmentId { get; set; }


		[DisplayName("پیوست پیش فاکتور")]
		[DisplayInfo(null, true, type: SystemType.File)]
		[ForeignKey(nameof(PreFactorAttachmentId))]
		public FileEntity? PreFactorAttachmentFile { get; set; }

		public long? PreFactorAttachmentId { get; set; }


		[DisplayName("شماره سفارش ساخت")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(255)]
		public string Number { get; set; }


		[DisplayName("پیوست قرارداد")]
		[DisplayInfo(null, true, type: SystemType.File)]
		[ForeignKey(nameof(ContractAttachmentId))]
		public FileEntity? ContractAttachmentFile { get; set; }

		public long? ContractAttachmentId { get; set; }


		[DisplayName("GA")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool HasGA { get; set; } = false;


		[DisplayName("نوع بسته بندی")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? PackingType { get; set; }


		[DisplayName("نحوه تحویل")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? DeliveryType { get; set; }


		[DisplayName("نوع محصول")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? ProductType { get; set; }


		[DisplayName("نحوه راه اندازی")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? CentrifugeSetupType { get; set; }


		[DisplayName("نیاز به مدیر پروژه")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsNeedProjectManager { get; set; } = false;


		[DisplayName("شماره سفارش ساخت")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? ProductionOrderNumber { get; set; }


		[DisplayName("نسخه")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Revision { get; set; }


		[DisplayName("کاربر کارشناس فروش")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? SalesExpert { get; set; }

		public long? SalesExpertId { get; set; }


		[DisplayName("صنعت مشتری")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? CustomerIndustry { get; set; }


		[DisplayName("کد تفصیل پروژه")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? ProjectDlCode { get; set; }


		[DisplayName("پروژه مهندسی")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? EdmsProject { get; set; }


		[DisplayName("کارشناس معرف")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? IntroducerExpert { get; set; }

		public long? IntroducerExpertId { get; set; }
		public long? HamkaranId { get; set; }


		[DisplayName("تایید کننده مالی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? FinancialConfirmedBy { get; set; }

		public long? FinancialConfirmedById { get; set; }


		[DisplayName("تاریخ میلادی تایید مالی")]
		[DisplayInfo(null, false, SystemType.DateTime, systemProprty: true)]
		public DateTime? FinancialConfirmedOnMiladiDate { get; set; } = null;

		[DisplayName("تاریخ شمسی تایید مالی")]
		[MaxLength(30)]
		[DisplayInfo("FinancialConfirmedOnMiladiDate", true, SystemType.DateShamsi, systemProprty: true)]
		public string? FinancialConfirmedOnShamsiDate { get; set; } = null;


		[DisplayName("منسوخ کننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? ObsoletedBy { get; set; }

		public long? ObsoletedById { get; set; }


		[DisplayName("تاریخ میلادی منسوخ‌سازی")]
		[DisplayInfo(null, false, SystemType.DateTime, systemProprty: true)]
		public DateTime? ObsoletedOnMiladiDate { get; set; } = null;

		[DisplayName("تاریخ شمسی منسوخ‌سازی")]
		[MaxLength(30)]
		[DisplayInfo("ObsoletedOnMiladiDate", true, SystemType.DateShamsi, systemProprty: true)]
		public string? ObsoletedOnShamsiDate { get; set; } = null;


		[DisplayName("غیرفعال برای گزارش تحویل به‌موقع")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsDisableForTimelyDeliveryReport { get; set; } = false;


		[DisplayName("توضیح غیرفعال‌سازی گزارش تحویل به‌موقع")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? DisableForTimelyDeliveryReportComment { get; set; }


		[DisplayName("کامنت های سفارش ساخت")]
		[DisplayInfo(null, true, type: SystemType.ListEntity)]
		public List<ProductionOrderComment> Comments { get; set; } = new();


	}

	[Display(Name = "کامنت های سفارش ساخت")]
	[Table("ProductionOrderComment", Schema = "Sale")]
	public class ProductionOrderComment : BaseEntity
	{
		public long ProductionOrderId { get; set; }
		public ProductionOrder ProductionOrder { get; set; }

		[DisplayName("وضعیت قبلی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProductionOrderStateEnum? PreviousState { get; set; }


		[DisplayName("وضعیت جدید")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProductionOrderStateEnum? NewState { get; set; }


		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }

	}
}
