using System.ComponentModel.DataAnnotations;

namespace Entities.App.SLS.Enums
{
	public enum ContractSalesTypeEnum
	{
		[Display(Name = "نقدی")]
		Cash = 3,
		[Display(Name = "هدیه")]
		Gift = 6,
		[Display(Name = "تخفیفی")]
		Discounted = 7,
		[Display(Name = "برگشتی به سال قبل 9%")]
		ReturnToPreviousYearPercentage = 10,
		[Display(Name = "اعتباری")]
		Credit = 13,
	}
}
