using Common.Attributes;
using Entities.App.Gnr;
using Entities.App.Hcm;
using Entities.App.Inv;
using Entities.App.Sale.Enum;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{

	[Display(Name = "تحویل محصول به مشتری")]
	[Table("ProductDeliveryCustomer", Schema = "Sale")]
	public class ProductDeliveryCustomer : BaseEntity
	{

		[DisplayName("نام شرکت")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Party? Company { get; set; }
		public long? CompanyId { get; set; }


		[DisplayName("نام مدیر")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Personel? ManagerNames { get; set; }
		public long? ManagerNamesId { get; set; }


		[DisplayName("نام مسئول فنی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Personel? TechnicalManagerName { get; set; }
		public long? TechnicalManagerId { get; set; }


		[DisplayName("نام و سمت تحویل گیرنده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? NameRecipient { get; set; }


		[DisplayName("تاریخ راه اندازی میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? LaunchMiladiDate { get; set; } = null;


		[DisplayName("تاریخ راه اندازی شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? LaunchShamsiDate { get; set; } = null;

		[DisplayName("آدرس")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Address { get; set; }


		[DisplayName("تلفن")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Phone { get; set; }



		[DisplayName("نام و نام خانوادگی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Personel? FullName { get; set; }
		public long? FullNameId { get; set; }


		[DisplayName("پرسنل واحد خدمات پس از فروش")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool AfterSalesServicePersonnel { get; set; } = default;


		[DisplayName("نمایندگی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public bool Agency { get; set; } = default;


		[DisplayName("نام دستگاه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? DeviceName { get; set; }


		[DisplayName("مدل دستگاه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? DeviceModel { get; set; }


		[DisplayName("سریال ساخت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public string? ProductionSeries { get; set; }




		[DisplayName("فشار کاری")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? WorkPressure { get; set; }


		[DisplayName("جریان کاری")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? StartingCurrent { get; set; }


		[DisplayName("نوع راه اندازی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public ProductionSeriesEnum StartupType { get; set; }


		[DisplayName("جریان زیر بار")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? LoadCurrent { get; set; }


		[DisplayName("جریان بدون بار")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? NoLoadCurrent { get; set; }


		[DisplayName("ولتاژ شبکه در هنگام راه اندازی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? MainsVoltageDuringStartup { get; set; }


		[DisplayName("آيا شرايط استاندارد در هنگام راه اندازي وجود دارد?")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public bool StandardConditionsDuringStartup { get; set; } = default;


		#region TechnicalDocument

		[DisplayName("تجهیزات جانبی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Part? Accessories { get; set; }
		public string? AccessoriesId { get; set; }


		[DisplayName("دفترچه سرويس و نگهداري ")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool ServiceMaintenanceManual { get; set; } = default;


		[DisplayName("دفترچه راهبري براي كنترلر")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool ControllerInstructionManual { get; set; } = default;


		[DisplayName("دفترچه گزارش خدمات كمپرسور")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool CompressorServiceLogbook { get; set; } = default;


		[DisplayName("نقشه برق كمپرسور و در صورت موجود بودن تجهيزات جانبي نقشه برق آن")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool CompElecSchem { get; set; } = default;


		[DisplayName("تاریخ  ضمانت دستگاه میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? DeviceWarrantyMiladiDate { get; set; } = null;


		[DisplayName("تاریخ  ضمانت دستگاه شمسی")]
		[DisplayInfo(null, true, type: SystemType.DateShamsi)]
		public string? DeviceWarrantyShamsiDate { get; set; } = null;


		#endregion





	}
}
