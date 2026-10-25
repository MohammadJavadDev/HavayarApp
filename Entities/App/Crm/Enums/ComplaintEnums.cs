using System.ComponentModel.DataAnnotations;

namespace Entities.App.Crm.Enums
{
	/// <summary>نوع مشتری — Gnr_Lookup نوع 30.</summary>
	public enum ComplaintCustomerTypeEnum : short
	{
		[Display(Name = "حقیقی")]
		Real = 106,
		[Display(Name = "حقوقی")]
		Legal = 107
	}

	/// <summary>فوریت رسیدگی — Gnr_Lookup نوع 172.</summary>
	public enum ComplaintPriorityEnum : short
	{
		[Display(Name = "فوری")]
		Urgent = 1122,
		[Display(Name = "غیر فوری")]
		NotUrgent = 1123
	}

	/// <summary>نوع خرابی کمپرسور — Gnr_Lookup نوع 175.</summary>
	public enum ComplaintCompressorFailureEnum : short
	{
		[Display(Name = "لرزش")]
		Vibration = 1157,
		[Display(Name = "صدای غیرعادی")]
		AbnormalNoise = 1158,
		[Display(Name = "نشتی روغن")]
		OilLeak = 1159,
		[Display(Name = "طراحی نامناسب")]
		ImproperDesign = 1160,
		[Display(Name = "خوردگی و آسیب خوردگی")]
		Corrosion = 1161,
		[Display(Name = "خرابی ایرند")]
		AirendFailure = 1329,
		[Display(Name = "خرابی آنلودر")]
		UnloaderFailure = 1402,
		[Display(Name = "بالا رفتن دمای کمپرسور")]
		TemperatureRise = 1473,
		[Display(Name = "خرابی مکرر")]
		RepeatedFailure = 1493,
		[Display(Name = "مشکل برقی")]
		Electrical = 1563,
		[Display(Name = "مشکل ریست شدن")]
		Reset = 1776,
		[Display(Name = "داغ شدن بلبرینگ")]
		BearingHeat = 1817,
		[Display(Name = "سوختگی عایق سیم پیچی")]
		WindingInsulation = 1818,
		[Display(Name = "مشکل الکتروموتور")]
		Electromotor = 1819,
		[Display(Name = "استفاده از روغن نامرغوب")]
		PoorOil = 1845
	}

	/// <summary>وضعیت شکایت — Gnr_Lookup نوع 178.</summary>
	public enum ComplaintStatusEnum : short
	{
		[Display(Name = "دریافت شکایت")]
		Received = 1168,
		[Display(Name = "رسیدگی اولیه")]
		InitialReview = 1169,
		[Display(Name = "تکمیل اطلاعات و رسیدگی نهایی")]
		FinalReview = 1170,
		[Display(Name = "اعلام نتیجه به مشتری")]
		ResultAnnounced = 1171,
		[Display(Name = "عدم جلب رضایت و بسته")]
		ClosedUnsatisfied = 1172,
		[Display(Name = "جلب رضایت و بسته")]
		ClosedSatisfied = 1173
	}

	/// <summary>اطلاع رسانی به شاکی — Gnr_Lookup نوع 180.</summary>
	public enum ComplaintInformComplainantEnum : short
	{
		[Display(Name = "انجام شد")]
		Done = 1178,
		[Display(Name = "انجام نشد")]
		NotDone = 1179
	}

	/// <summary>رضایت شاکی — Gnr_Lookup نوع 181.</summary>
	public enum ComplaintConsentEnum : short
	{
		[Display(Name = "حاصل شد")]
		Achieved = 1180,
		[Display(Name = "حاصل نشد")]
		NotAchieved = 1181
	}

	/// <summary>وضعیت نهایی شکایت — Gnr_Lookup نوع 182. 1182 دکمه پیام پایان و 1183 دکمه پیام ثبت را فعال می‌کند.</summary>
	public enum ComplaintFinalStatusEnum : short
	{
		[Display(Name = "بسته شد")]
		Closed = 1182,
		[Display(Name = "بسته نشد")]
		NotClosed = 1183
	}

	/// <summary>موضوع کلی شکایت — Gnr_Lookup نوع 216.</summary>
	public enum ComplaintGeneralSubjectEnum : short
	{
		[Display(Name = "محصول")]
		Product = 1495,
		[Display(Name = "فرآیند")]
		Process = 1496,
		[Display(Name = "سایر")]
		Other = 1497,
		[Display(Name = "محصول - فرآیند")]
		ProductAndProcess = 1775
	}

	/// <summary>نوع (هوایاری / غیر هوایاری) — Gnr_Lookup نوع 217.</summary>
	public enum ComplaintOwnerTypeEnum : short
	{
		[Display(Name = "هوایاری")]
		Havayar = 1499,
		[Display(Name = "غیر هوایاری")]
		NonHavayar = 1500
	}

	/// <summary>نوع شکایت فرم — Gnr_Lookup نوع 220. جدول Crm_ComplaintType جدا است و به این فیلد وصل نیست.</summary>
	public enum ComplaintImportanceEnum : short
	{
		[Display(Name = "بحرانی")]
		Critical = 1531,
		[Display(Name = "بسیار مهم")]
		VeryImportant = 1532,
		[Display(Name = "مهم")]
		Important = 1533,
		[Display(Name = "سایر")]
		Other = 2739
	}

	/// <summary>موضوع اصلی — Gnr_Lookup نوع 333.</summary>
	public enum ComplaintMainSubjectEnum : short
	{
		[Display(Name = "رد شدن در ارزیابی کیفی")]
		QualityEvaluationRejected = 2551,
		[Display(Name = "عدم تأیید پیشنهاد فنی")]
		TechnicalOfferRejected = 2552,
		[Display(Name = "عدم تأیید پیشنهاد مالی")]
		FinancialOfferRejected = 2553,
		[Display(Name = "تاخیر بازرگانی")]
		CommercialDelay = 2554,
		[Display(Name = "تاخیر تدارکات")]
		ProcurementDelay = 2555,
		[Display(Name = "تاخیر مالی")]
		FinancialDelay = 2556,
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
		SalesQuality = 2562,
		[Display(Name = "کیفیت قطعه")]
		PartQuality = 2563,
		[Display(Name = "کیفیت محصول")]
		ProductQuality = 2564
	}
}
