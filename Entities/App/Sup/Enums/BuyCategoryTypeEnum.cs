using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sup.Enums
{
	/// <summary>
	/// نوع دسته خرید. مقدار از راهکاران (USR3.Sup_BuyCategory.TypeRef = 1/2) می‌آید.
	/// نگاشت به HTS (Gnr_Lookup نوع 162 «Supp_BuyCategoryType»): 1040 خرید → Purchase (1) · 1041 ساخت → Build (2). (D15)
	/// </summary>
	public enum BuyCategoryTypeEnum
	{
		[Display(Name = "خرید")]
		Purchase = 1,
		[Display(Name = "ساخت")]
		Build = 2,
	}
}
