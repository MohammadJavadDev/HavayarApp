using System.ComponentModel.DataAnnotations;

namespace Entities.App.Srv.Enums
{
	/// <summary>
	/// وضعیت‌های شناخته‌شده درخواست PM (Gnr_Lookup type 161).
	/// ستون موجودیت <c>PmRequest.StatusId</c> از نوع <c>short</c> است؛
	/// این enum را روی ستون نگاشت نکنید.
	/// </summary>
	public enum PmRequestStatusEnum
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
