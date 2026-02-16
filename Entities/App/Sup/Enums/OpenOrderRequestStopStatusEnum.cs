using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sup.Enums
{
	public enum OpenOrderRequestStopStatusEnum
	{
		[Display(Name = "ثبت اولیه و ارسال به کارتابل مسئول توقف")]
		InitialSubmissionAndSendToStopBoard = 2807,
		[Display(Name = "بررسی عامل توقف _ نیاز به راه اندازی مجدد")]
		StopCauseRequiresRestart = 2808,
		[Display(Name = "بررسی عامل توقف _ نياز به اصلاح BOM")]
		CheckStopFactorNeedToFixBom = 2810,
		[Display(Name = "بررسی عامل توقف _نياز به اصلاح مدارك/شرح كالا و غیره")]
		NeedsDocumentRevision = 2811,
		[Display(Name = "بررسی عامل توقف _ حذف درخواست مرتبط با واحد صنایع")]
		IndustrialUnitStopCauseDeletionRequestCheck = 2812,
		[Display(Name = "بررسی عامل توقف _ در انتظار مدير پروژه / كارفرما")]
		ProjectManagerPendingStopReason = 2813,
		[Display(Name = "بررسی عامل توقف _ راه اندازی مجدد")]
		RestartStopCauseCheck = 2815,
		[Display(Name = "بررسی عامل توقف _ ثبت توقف مجدد")]
		CheckStopFactorReRegisterStop = 2816,
		[Display(Name = "بررسی عامل توقف _ حذف درخواست")]
		StoppingFactorForDeletionRequest = 2817,
		[Display(Name = "ارسال به کارتابل تدارکات بعلت عدم بررسی در زمان مشخص")]
		SendToProcurementDueToMissingReview = 2821,
	}
}
