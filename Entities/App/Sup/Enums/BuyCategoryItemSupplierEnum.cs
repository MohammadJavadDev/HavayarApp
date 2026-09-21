using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entities.App.Sup.Enums
{
	/// <summary>
	/// کانال تامین قلم دسته خرید؛ مقدار از راهکاران (USR3.Sup_BuyCategoryItems.Supplier = 1/2) می‌آید.
	/// توجه (D27): با <c>LeadTimeSupplierEnum</c> (واحد تامین «زمان در راه» با کدهای 6/51 = HRM_OrgUnit در HTS) مفهوم مشترکی ندارد
	/// و نگاشت 1↔51 وجود ندارد — دو مفهوم مستقل‌اند.
	/// </summary>
	public enum BuyCategoryItemSupplierEnum
    {

		[Display(Name = "بازرگانی خارجی")]
		ForeignTrade = 1,
		[Display(Name = "تامین و خرید")]
		SupplyAndPurchase = 2,
	}
}
