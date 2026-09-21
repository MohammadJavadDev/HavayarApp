using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sale.Enums
{
	public enum WebShopPartTypeEnum
	{
		[Display(Name = "روغن")]
		Oil = 277,
		[Display(Name = "برد")]
		Board = 278,
		[Display(Name = "المنت فیلتر")]
		FilterElement = 279,
		[Display(Name = "فیلتر هوا")]
		AirFilter = 280,
		[Display(Name = "فیلتر سپراتور")]
		SeparatorFilter = 281,
		[Display(Name = "فیلتر روغن")]
		OilFilter = 282,
		[Display(Name = "کیت")]
		Kit = 283
	}

	public enum PriceUnitEnum
	{
		[Display(Name = "ریال")]
		Rial = 36,
		[Display(Name = "دلار")]
		Usd = 37,
		[Display(Name = "درهم")]
		Dirham = 38,
		[Display(Name = "یورو")]
		Euro = 39,
		[Display(Name = "یوان")]
		Yuan = 475
	}

	public enum TenderStatusEnum
	{
		[Display(Name = "در انتظار پاسخ فنی متره")]
		AwaitingTechnicalLegacy = 892,
		[Display(Name = "پیشنهاد مالی ارسال شد")]
		FinancialSentLegacy = 895,
		[Display(Name = "در انتظار جلسه")]
		AwaitingMeetingLegacy = 898,
		[Display(Name = "هولد شده است")]
		HoldLegacy = 899,
		[Display(Name = "نامشخص")]
		UnknownLegacy = 900,
		[Display(Name = "برنده مناقصه")]
		WinnerLegacy = 904,
		[Display(Name = "بازنده مناقصه")]
		LoserLegacy = 905,
		[Display(Name = "در انتظار دریافت اسناد")]
		AwaitingDocuments = 2308,
		[Display(Name = "در انتظار پاسخ فنی متره")]
		AwaitingTechnical = 2309,
		[Display(Name = "پیشنهاد فنی ارسال شده است")]
		TechnicalSent = 2310,
		[Display(Name = "در انتظار بازدید")]
		AwaitingVisit = 2311,
		[Display(Name = "پیشنهاد مالی ارسال شد")]
		FinancialSent = 2312,
		[Display(Name = "پیشنهاد فنی و مالی ارسال شد")]
		TechnicalAndFinancialSent = 2313,
		[Display(Name = "در انتظار پاسخ مناقصه")]
		AwaitingTenderResponse = 2314,
		[Display(Name = "در انتظار جلسه")]
		AwaitingMeeting = 2315,
		[Display(Name = "هولد شده است")]
		OnHold = 2316,
		[Display(Name = "نامشخص")]
		Unknown = 2317,
		[Display(Name = "ارسال اسناد ارزیابی کیفی")]
		QualitativeDocsSent = 2318,
		[Display(Name = "تایید ارزیابی کیفی")]
		QualitativeApproved = 2319,
		[Display(Name = "تایید پیشنهاد فنی")]
		TechnicalApproved = 2320,
		[Display(Name = "برنده مناقصه")]
		Winner = 2321,
		[Display(Name = "بازنده مناقصه")]
		Loser = 2322,
		[Display(Name = "در حال پیگیری")]
		FollowingUp = 2324,
		[Display(Name = "تجدید مناقصه")]
		Retendered = 2565
	}

	public enum TenderFinalResultEnum
	{
		[Display(Name = "خرید از هوایار")]
		BuyFromHavayar = 876,
		[Display(Name = "تعویض کارشناس فروش")]
		ChangeSalesExpert = 877,
		[Display(Name = "خرید منتفی شد")]
		Cancelled = 878,
		[Display(Name = "عدم تامین بودجه کارفرما")]
		NoBudget = 879,
		[Display(Name = "خرید از رقبا(قیمت بالا)")]
		CompetitorPrice = 880,
		[Display(Name = "خرید از رقبا(زمان تحویل)")]
		CompetitorDelivery = 881,
		[Display(Name = "خرید از رقبا(عدم تایید فنی)")]
		CompetitorTechnical = 882,
		[Display(Name = "خرید از رقبا(روابط)")]
		CompetitorRelations = 883,
		[Display(Name = "خرید از رقبا(مشتری قدیم رقیب)")]
		CompetitorOldCustomer = 884,
		[Display(Name = "محصول غیرهوایاری")]
		NonHavayarProduct = 885,
		[Display(Name = "باخت استعلام گیر")]
		InquiryLoser = 886,
		[Display(Name = "سایر")]
		Other = 887,
		[Display(Name = "عقد قرارداد")]
		ContractSigned = 906,
		[Display(Name = "عدم رضایت مشتری")]
		CustomerUnsatisfied = 1073,
		[Display(Name = "خرید از رقبا(شرایط پرداخت)")]
		CompetitorPayment = 1116,
		[Display(Name = "خرید محصول دست دوم")]
		SecondHand = 1117,
		[Display(Name = "خرید خارجی")]
		ForeignPurchase = 2257
	}

	public enum TenderOrganizationEnum
	{
		[Display(Name = "خدمات پس از فروش")]
		AfterSales = 929,
		[Display(Name = "خدمات پس از فروش هوای فشرده")]
		CompressedAir = 931,
		[Display(Name = "خدمات پس از فروش نفت گاز و پتروشیمی")]
		OilGasPetrochemical = 1670
	}

	public enum TenderTypeEnum
	{
		[Display(Name = "مناقصه یک مرحله‌ای")]
		OneStage = 2325,
		[Display(Name = "مناقصه دو مرحله‌ای")]
		TwoStage = 2326,
		[Display(Name = "مناقصه یک مرحله‌ای با ارزیابی کیفی")]
		OneStageWithQualitative = 2327
	}

	public enum TenderInquiryTypeEnum
	{
		[Display(Name = "استعلام")]
		Inquiry = 907,
		[Display(Name = "مناقصه")]
		Tender = 908,
		[Display(Name = "بازاریابی")]
		Marketing = 909,
		[Display(Name = "درخواست تلفنی")]
		TelephoneRequest = 910,
		[Display(Name = "درخواست مستقیم")]
		DirectRequest = 911,
		[Display(Name = "ایمیل Info")]
		EmailInfo = 912,
		[Display(Name = "مشتری قدیمی")]
		OldCustomer = 913,
		[Display(Name = "سایت هوایار")]
		HavayarSite = 914,
		[Display(Name = "معرفی شده کارشناس فروش")]
		IntroducedSalesExpert = 915,
		[Display(Name = "معرفی شده توسط خدمات پس از فروش")]
		IntroducedByAfterSales = 1052,
		[Display(Name = "معرفی شده توسط نماینده فروش")]
		IntroducedBySalesAgent = 1053,
		[Display(Name = "نماینده هوایار")]
		HavayarAgent = 1054
	}

	public enum TenderSalesAgentEnum
	{
		[Display(Name = "آرتان هوا صنعت سایا")]
		Artan = 2213,
		[Display(Name = "پایا صنعت هوا سپهر")]
		Paya = 2214,
		[Display(Name = "هواکار صنعت آندیا")]
		Havakar = 2215,
		[Display(Name = "یارا صنعت هوای فشرده")]
		Yara = 2216,
		[Display(Name = "امداد کمپرسور")]
		Emdad = 2217,
		[Display(Name = "آذر دقیق درخشان طب آیدین")]
		Azar = 2218,
		[Display(Name = "اطلس کمپرسور ایرانیان")]
		Atlas = 2219,
		[Display(Name = "آروین صنعت هوا گستر")]
		Arvin = 2220,
		[Display(Name = "آروند صنعت بیستون")]
		Arvand = 2221,
		[Display(Name = "پدیده سیال اسپادانا")]
		Padideh = 2222,
		[Display(Name = "دهقانی زاده")]
		Dehghanizade = 2223,
		[Display(Name = "بهین تجهیز امین")]
		Behin = 2224,
		[Display(Name = "بدون نماینده(مستقیم)")]
		Direct = 2225,
		[Display(Name = "صبادم امین")]
		Sabadam = 2745
	}

	public enum ProjectFinancialValueEnum
	{
		[Display(Name = "کوچک")]
		Small = 406,
		[Display(Name = "متوسط")]
		Medium = 407,
		[Display(Name = "بزرگ")]
		Large = 408
	}

	public enum WarrantyTypeEnum
	{
		[Display(Name = "بانکی")]
		Bank = 2328,
		[Display(Name = "چک")]
		Cheque = 2329
	}

	public enum WarrantyStatusEnum
	{
		[Display(Name = "ابطال")]
		Cancelled = 2330,
		[Display(Name = "تمدید")]
		Extended = 2331
	}

	public enum TenderFailureCauseEnum
	{
		[Display(Name = "رد شدن در ارزیابی کیفی")]
		QualitativeReject = 2551,
		[Display(Name = "عدم تأیید پیشنهاد فنی")]
		TechnicalReject = 2552,
		[Display(Name = "عدم تأیید پیشنهاد مالی")]
		FinancialReject = 2553,
		[Display(Name = "تاخیر بازرگانی")]
		CommerceDelay = 2554,
		[Display(Name = "تاخیر تدارکات")]
		ProcurementDelay = 2555,
		[Display(Name = "تاخیر مالی")]
		FinanceDelay = 2556,
		[Display(Name = "تاخیر خدمات")]
		ServiceDelay = 2557,
		[Display(Name = "داشتن الزام ساخت داخل")]
		LocalContent = 2558,
		[Display(Name = "ابطال مناقصه")]
		TenderCancelled = 2559,
		[Display(Name = "قیمت قطعه")]
		PartPrice = 2560,
		[Display(Name = "کیفیت خدمات")]
		ServiceQuality = 2561,
		[Display(Name = "کیفیت فروش")]
		SaleQuality = 2562,
		[Display(Name = "کیفیت قطعه")]
		PartQuality = 2563,
		[Display(Name = "کیفیت محصول")]
		ProductQuality = 2564
	}

	public enum ProductActivityTypeEnum
	{
		[Display(Name = "تعویض قطعه")]
		ReplacePart = 203,
		[Display(Name = "سرویس")]
		Service = 204,
		[Display(Name = "اورهال")]
		Overhaul = 229
	}

	public enum ActivityExecutorEnum
	{
		[Display(Name = "هوایار")]
		Havayar = 205,
		[Display(Name = "کارفرما")]
		Employer = 206,
		[Display(Name = "هوایار-کارفرما")]
		Both = 207
	}

	public enum ServiceRequestTypeEnum
	{
		[Display(Name = "آموزش")]
		Training = 1,
		[Display(Name = "اورهال")]
		Overhaul = 2,
		[Display(Name = "بازدید و مشاوره")]
		VisitConsult = 3,
		[Display(Name = "تعمیرات گارانتی")]
		GuaranteeRepair = 4,
		[Display(Name = "تعمیرات وارانتی")]
		WarrantyRepair = 5,
		[Display(Name = "راه اندازی اولیه")]
		InitialCommissioning = 6,
		[Display(Name = "پایپینگ")]
		Piping = 7,
		[Display(Name = "سرویس دوره ای")]
		PeriodicService = 8,
		[Display(Name = "سایر موارد")]
		Other = 9,
		[Display(Name = "OPI")]
		Opi = 10,
		[Display(Name = "راه اندازی تجهیز تعمیری")]
		RepairedCommissioning = 11,
		[Display(Name = "پیش راه اندازی")]
		PreCommissioning = 12,
		[Display(Name = "نصب")]
		Install = 13
	}

	public enum MissionActionTypeEnum
	{
		[Display(Name = "تعویض قطعه")]
		ReplacePart = 1,
		[Display(Name = "سرویس")]
		Service = 2
	}

	public enum MissionResultEnum
	{
		[Display(Name = "انجام شده")]
		Done = 140,
		[Display(Name = "انجام نشده")]
		NotDone = 141,
		[Display(Name = "انجام شده با نقص")]
		DoneWithDefect = 142
	}

	public enum AfterSalesServiceTypeEnum
	{
		[Display(Name = "مصرف پروژه")]
		ProjectConsume = 148,
		[Display(Name = "گارانتی")]
		Guarantee = 149,
		[Display(Name = "وارانتی")]
		Warranty = 150,
		[Display(Name = "گارانتی مشروط")]
		ConditionalGuarantee = 151,
		[Display(Name = "تخفیفی")]
		Discounted = 157,
		[Display(Name = "مصرف پروژه گارانتی")]
		ProjectConsumeGuarantee = 185
	}

	public enum DamagedPartTypeEnum
	{
		[Display(Name = "سالم")]
		Healthy = 785,
		[Display(Name = "معیوب")]
		Defective = 786
	}
}
