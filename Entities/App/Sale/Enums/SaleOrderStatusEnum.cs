using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sale.Enums
{
	public enum SaleOrderStatusEnum
	{
		[Display(Name = "ثبت اولیه")]
		Initial = 92,

		[Display(Name = "تایید شده")]
		Approved = 93,

		[Display(Name = "ابطال شده")]
		Cancelled = 94,

		[Display(Name = "اختتام شده")]
		Closed = 95,

		[Display(Name = "در حال استفاده")]
		InUse = 2546,

		[Display(Name = "معلق")]
		Suspended = 2547,

		[Display(Name = "وضعیت ندارد")]
		NoStatus = 2548,

		[Display(Name = "حذف شده")]
		Deleted = 2713
	}
}
