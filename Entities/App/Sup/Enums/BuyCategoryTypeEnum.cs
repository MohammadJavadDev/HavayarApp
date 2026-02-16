using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sup.Enums
{
	public enum BuyCategoryTypeEnum
	{
		[Display(Name = "خرید")]
		Purchase = 1,
		[Display(Name = "ساخت")]
		Build = 2,
	}
}
