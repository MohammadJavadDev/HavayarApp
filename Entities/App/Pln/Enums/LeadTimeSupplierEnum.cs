using System.ComponentModel.DataAnnotations;

namespace Entities.App.Pln.Enums
{
	public enum LeadTimeSupplierEnum
	{
		[Display(Name = "بازرگانی خارجی")]
		ForeignTrade = 6,
		[Display(Name = "تامین و خرید")]
		SupplyAndPurchase = 51,
	}
}
