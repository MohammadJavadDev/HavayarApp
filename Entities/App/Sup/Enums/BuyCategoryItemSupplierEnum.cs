using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entities.App.Sup.Enums
{
    public enum BuyCategoryItemSupplierEnum
    {

		[Display(Name = "بازرگانی خارجی")]
		ForeignTrade = 1,
		[Display(Name = "تامین و خرید")]
		SupplyAndPurchase = 2,
	}
}
