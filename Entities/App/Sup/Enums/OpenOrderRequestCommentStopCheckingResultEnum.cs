using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sup.Enums
{
    public enum OpenOrderRequestCommentStopCheckingResultEnum
    {
		[Display(Name = "نياز به راه اندازي مجدد")]
		NeedsRestart = 2799,

		[Display(Name = "نياز به اصلاح BOM")]
		NeedsBOMCorrection = 2800,

		[Display(Name = "نياز به اصلاح مدارك/شرح كالا و غیره")]
		NeedsDocumentOrItemDescriptionCorrection = 2801,

		[Display(Name = "حذف درخواست مرتبط با واحد صنایع")]
		DeleteIndustrialUnitRelatedRequest = 2802,

		[Display(Name = "در انتظار مدير پروژه / كارفرما")]
		AwaitingProjectManagerOrEmployer = 2803
	}
}