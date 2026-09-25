using System.ComponentModel.DataAnnotations;

namespace Entities.App.Srv.Enums
{
	/// <summary>
	/// وضعیت‌های شناخته‌شده درخواست خودرو (Gnr_Lookup).
	/// ستون موجودیت <c>CarRequest.StatusId</c> از نوع <c>short</c> است تا مقدار تاریخی 0 نیز ذخیره شود؛
	/// این enum را روی ستون نگاشت نکنید.
	/// </summary>
	public enum CarRequestStatusEnum
	{
		[Display(Name = "در حال بررسی")]
		UnderReview = 1032,

		[Display(Name = "تایید شده")]
		Approved = 1033,

		[Display(Name = "رد شده")]
		Rejected = 1034,

		[Display(Name = "انجام شد")]
		Done = 1047
	}
}
