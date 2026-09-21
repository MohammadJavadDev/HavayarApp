using Entities.App.Sale.Enums;

namespace Entities.App.Sale
{
	/// <summary>
	/// قواعد مشترک گردش‌کار قلم سفارش ساخت که هم در WebApp (اکشن‌های کاربر) و هم در App.BackgroundJob
	/// (جاب‌های مهلت/ارجاع) استفاده می‌شوند. معادل قواعد ثابت HTS در
	/// ProductionOrderItemCommentService.AddCommentAndSendNotification و ProductionOrderItemService.SendExpiredOrdersNotification.
	/// این کلاس فقط قاعده است؛ هیچ دسترسی به دیتابیس ندارد.
	/// </summary>
	public static class ProductionOrderItemWorkflowRules
	{
		/// <summary>
		/// نوع دستگاه‌هایی که رشته مهندسی آن‌ها «برق» است (بقیه «مکانیک»). تشخیص کارتابل مهندسی برق/مکانیک و
		/// گیرنده اعلان (Pln.ProductionOrderEquipmentConfirmer.ElectricalUserId / MechanicalUserId) از همین لیست است.
		/// </summary>
		public static readonly HashSet<ProductionOrderItemDeviceTypeEnum> ElectricalDeviceTypes = new()
		{
			ProductionOrderItemDeviceTypeEnum.ControlPanel,
			ProductionOrderItemDeviceTypeEnum.ElectricalPanel,
			ProductionOrderItemDeviceTypeEnum.InverterPanel,
			ProductionOrderItemDeviceTypeEnum.Sequencer,
		};

		/// <summary>
		/// معادل HTS: specialPartCodePrefixes — کالاهایی که با این پیشوندها شروع می‌شوند پس از تایید مهندسی
		/// (یا انقضای مهلت مهندسی) مستقیم به کارتابل صنایع (۱۹۰۴) می‌روند و به کمیته تامین (۲۲۰۲) نمی‌رسند.
		/// </summary>
		public static readonly string[] BuildInsidePartCodePrefixes =
		{
			"1807101", "180229", "180211", "1808101", "1809101", "1810101", "1811101"
		};

		/// <summary>
		/// وضعیت‌های بررسی که قلم را «در کارتابل صنایع» قرار می‌دهند (داخلی ۱۹۰۴ / خارجی ۱۹۰۵).
		/// </summary>
		public static readonly ProductionOrderItemCheckStatusEnum[] IndustrialDashboardStatuses =
		{
			ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal,
			ProductionOrderItemCheckStatusEnum.ForeignIndustryCatalog
		};

		public static bool IsElectrical(ProductionOrderItemDeviceTypeEnum? deviceType) =>
			deviceType.HasValue && ElectricalDeviceTypes.Contains(deviceType.Value);

		public static bool IsIndustrialDashboard(ProductionOrderItemCheckStatusEnum? checkStatus) =>
			checkStatus.HasValue && IndustrialDashboardStatuses.Contains(checkStatus.Value);

		/// <summary>
		/// معادل HTS پس از «تایید مهندسی مکانیک (۲۱۹۷)» و همچنین انقضای مهلت مهندسی:
		/// اگر (غیرروتین و ساخت داخل) یا کد کالا با یکی از پیشوندهای ساخت داخل شروع شود → صنایع داخلی (۱۹۰۴)،
		/// وگرنه → در انتظار بررسی رئیس کمیته تامین (۲۲۰۲).
		/// </summary>
		public static ProductionOrderItemCheckStatusEnum ResolveAfterEngineeringApproval(bool isRoutine, bool isBuildInside, string? partCode)
		{
			var startsWithAny = !string.IsNullOrWhiteSpace(partCode)
				&& BuildInsidePartCodePrefixes.Any(prefix => partCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

			return (!isRoutine && isBuildInside) || startsWithAny
				? ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal
				: ProductionOrderItemCheckStatusEnum.CommitteeHeadReviewPending;
		}

		/// <summary>
		/// معادل HTS پس از «تایید مدیر پروژه (۲۲۵۹)»: قلم به کارتابل مهندسی مکانیک (۲۱۹۵) می‌رود.
		/// </summary>
		public static ProductionOrderItemCheckStatusEnum AfterProjectManagerApproval =>
			ProductionOrderItemCheckStatusEnum.MechanicalEngineeringApprovalPending;

		/// <summary>
		/// معادل HTS پس از «عدم تایید مهندسی (۲۱۹۸)»: عدم تایید مهندسی و نیاز به بازنگری (۲۲۰۱).
		/// </summary>
		public static ProductionOrderItemCheckStatusEnum AfterEngineeringReject =>
			ProductionOrderItemCheckStatusEnum.EngineeringReassessmentNeeded;

		/// <summary>
		/// معادل HTS پس از «عدم تایید مدیر پروژه (۲۲۶۰)»: عدم تایید مدیر پروژه و نیاز به بازنگری (۲۲۶۱).
		/// </summary>
		public static ProductionOrderItemCheckStatusEnum AfterProjectManagerReject =>
			ProductionOrderItemCheckStatusEnum.ProjectManagerApprovalPending;

		/// <summary>
		/// مقصدهایی که رئیس کمیته تامین (روی ۲۲۰۲/۱۹۰۹) می‌تواند انتخاب کند — عین کمبوی HTS
		/// (productionOrderItemOriginal_filterStatus): خرید داخلی، خرید خارجی، نیاز به استعلام، نیاز به بررسی وزارت صنایع.
		/// </summary>
		public static readonly ProductionOrderItemCheckStatusEnum[] CommitteeDecisionDestinations =
		{
			ProductionOrderItemCheckStatusEnum.IndustrialDashboardInternal,
			ProductionOrderItemCheckStatusEnum.ForeignIndustryCatalog,
			ProductionOrderItemCheckStatusEnum.InquiryNeeded,
			ProductionOrderItemCheckStatusEnum.IndustrialMinistryReviewRequired
		};

		/// <summary>
		/// وضعیت‌های «در کارتابل رئیس کمیته تامین». HTS دو کد را استفاده می‌کند: ۲۲۰۲ (پس از مهندسی) و
		/// ۱۹۰۹ (بازگشت نتیجه استعلام). در سیستم جدید هر دو یک معنی دارند.
		/// </summary>
		public static readonly ProductionOrderItemCheckStatusEnum[] CommitteeChairStatuses =
		{
			ProductionOrderItemCheckStatusEnum.CommitteeHeadReviewPending,
			ProductionOrderItemCheckStatusEnum.SendToSupplyCommitteeChair
		};

		/// <summary>
		/// وضعیت‌هایی که مسئول استعلام دارند و LeadTime برای آن‌ها معنا دارد (۱۹۰۶ و ۱۹۰۸).
		/// </summary>
		public static readonly ProductionOrderItemCheckStatusEnum[] InquiryStatuses =
		{
			ProductionOrderItemCheckStatusEnum.InquiryNeeded,
			ProductionOrderItemCheckStatusEnum.IndustrialMinistryReviewRequired
		};

		public static bool IsInquiryStatus(ProductionOrderItemCheckStatusEnum? status) =>
			status.HasValue && InquiryStatuses.Contains(status.Value);

		/// <summary>
		/// وضعیت‌های تولید (LookupType 238 در HTS) که در DoChangeProductionStatusOperationOriginal تاریخ‌های قلم را پر می‌کنند.
		/// ۱۷۸۹ پایان تولید → ProductionEndDate؛ ۱۸۴۳ پایان بازرسی نهایی → TestingEndDate؛ ۱۷۹۱ پایان بسته‌بندی → PreparationDate.
		/// </summary>
		public static bool TryGetDateFieldForProductionStatus(ProductionOrderItemProductionStatusEnum status, out ProductionStatusDateField field)
		{
			switch (status)
			{
				case ProductionOrderItemProductionStatusEnum.ProductionCompleted:
					field = ProductionStatusDateField.ProductionEnd;
					return true;
				case ProductionOrderItemProductionStatusEnum.FinalInspectionCompleted:
					field = ProductionStatusDateField.TestingEnd;
					return true;
				case ProductionOrderItemProductionStatusEnum.PackagingCompleted:
					field = ProductionStatusDateField.Preparation;
					return true;
				default:
					field = ProductionStatusDateField.None;
					return false;
			}
		}
	}

	public enum ProductionStatusDateField
	{
		None = 0,
		ProductionEnd = 1,
		TestingEnd = 2,
		Preparation = 3
	}
}
