using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sale.Enums
{
	public enum SaleOrderTypeEnum
	{
		[Display(Name = "نقدی")]
		Cash = 97,

		[Display(Name = "هدیه")]
		Gift = 98,

		[Display(Name = "تخفیفی")]
		Discount = 99,

		[Display(Name = "برگشتی به سال قبل 9%")]
		ReturnPreviousYear = 3172,

		[Display(Name = "اعتباری")]
		Credit = 3173,

		[Display(Name = "امانی")]
		Trust = 3174
	}
}
