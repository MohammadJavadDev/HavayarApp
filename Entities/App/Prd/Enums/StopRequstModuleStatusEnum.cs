using System.ComponentModel.DataAnnotations;

namespace Entities.App.Prd.Enums
{
	/// <summary>
	/// وضعیت‌های workflow ماژول توقف — شناسه‌ها مطابق HTS (StopModuleStatus) حفظ شده‌اند.
	/// </summary>
	public enum StopRequstModuleStatusEnum
	{
		[Display(Name = "ثبت اولیه")]
		PreRegister = 2687,

		[Display(Name = "ویرایش شده")]
		Edited = 2688,

		[Display(Name = "حذف شده")]
		Deleted = 2689,

		[Display(Name = "عدم تایید کارشناس تولید")]
		ProductionExpertDisApprove = 2690,

		[Display(Name = "تایید کارشناس تولید")]
		ProductionExpertApprove = 2691,

		[Display(Name = "عدم تایید مسئول عامل توقف")]
		StopingFactorDisApprove = 2692,

		[Display(Name = "تایید مسئول عامل توقف")]
		StopingFactorApprove = 2693,

		[Display(Name = "ارسال به کارتابل مسئول عامل توقف")]
		SendToStopingFactorExpert = 2694,

		[Display(Name = "ارسال به کارتابل تیم CFT")]
		SendToCFTTeam = 2695,

		[Display(Name = "درخواست بررسی اثر بخش بودن اقدامات")]
		EffectivityCheckRequest = 2696,

		[Display(Name = "عدم تایید اثر بخش بودن اقدامات")]
		EffectivenessMeasuresDisApproved = 2697,

		[Display(Name = "تایید اثر بخش بودن اقدامات")]
		EffectivenessMeasuresApproved = 2698,

		[Display(Name = "درخواست توقف در تست تولید")]
		RegisteredInProductTest = 2699,

		[Display(Name = "تایید مدیر CFT")]
		CftLeadApproved = 2701,

		[Display(Name = "بررسی توسط مدیر کنترل کیفیت")]
		CheckByQcManager = 2737,
	}
}
