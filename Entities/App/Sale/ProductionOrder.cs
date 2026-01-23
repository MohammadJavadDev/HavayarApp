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


		[DisplayName("ManagementConfirmationAttachment")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? ManagementConfirmationAttachment { get; set; }


		[DisplayName("DepositFactorAttachment")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? DepositFactorAttachment { get; set; }


		[DisplayName("PreFactorAttachment")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? PreFactorAttachment { get; set; }


		[DisplayName("شماره سفارش ساخت")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(255)]
		public string Number { get; set; }


		[DisplayName("ContractAttachment")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? ContractAttachment { get; set; }


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
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? ProductionOrderNumber { get; set; }


		[DisplayName("نسخه")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? Revision { get; set; }


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


	}
}
