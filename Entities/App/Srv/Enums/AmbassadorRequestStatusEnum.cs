using System.ComponentModel.DataAnnotations;

namespace Entities.App.Srv.Enums
{
	/// <summary>
	/// وضعیت‌های شناخته‌شده درخواست پیک (Gnr_Lookup).
	/// ستون موجودیت <c>AmbassadorRequest.StatusId</c> از نوع <c>short</c> است تا مقدارهای تاریخی نیز ذخیره شود؛
	/// این enum را روی ستون نگاشت نکنید.
	/// </summary>
	public enum AmbassadorRequestStatusEnum
	{
		[Display(Name = "در حال بررسی")]
		UnderReview = 1032,

		[Display(Name = "تاییده شده")]
		Approved = 1033,

		[Display(Name = "رد شده")]
		Rejected = 1034,

		[Display(Name = "انجام شد")]
		Done = 1047
	}
}
