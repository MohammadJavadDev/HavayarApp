using System.ComponentModel.DataAnnotations;

namespace Entities.App.Cng.Enums
{
	/// <summary>نوع درخواست گزارش کار CNG — Gnr_Lookup LookupType_FK = 200.</summary>
	public enum WorkReportRequestTypeEnum
	{
		[Display(Name = "خرید قطعات")]
		BuyParts = 1330,

		[Display(Name = "خدمات (مشاوره)")]
		Consulting = 1331,

		[Display(Name = "خدمات (درخواست اعزام کارشناس)")]
		ExpertDispatch = 1332,

		[Display(Name = "اورهال")]
		Overhaul = 1557,

		[Display(Name = "کالیبراسیون")]
		Calibration = 1558,

		[Display(Name = "نگهداشت")]
		Maintenance = 1559,

		[Display(Name = "سرویس دوره ای")]
		PeriodicService = 1560,

		[Display(Name = "استاندارد سازی")]
		Standardization = 1777,

		[Display(Name = "راه اندازی")]
		Commissioning = 2702,

		[Display(Name = "عقد قرارداد سرویس دوره ای")]
		PeriodicServiceContract = 2761,

		[Display(Name = "تعمیرات")]
		Repairs = 2762,

		[Display(Name = "سابر")]
		Other = 2763
	}
}
