using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sale.Enums
{
	public enum SaleOrderPayMethodEnum
	{
		[Display(Name = "نقدی")]
		Cash = 111,

		[Display(Name = "اعتباری دائم")]
		PermanentCredit = 113,

		[Display(Name = "اعتباری موقت")]
		TemporaryCredit = 114
	}
}
