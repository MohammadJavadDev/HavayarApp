using System.ComponentModel.DataAnnotations;

namespace Entities.App.Pln.Enums
{
	/// <summary>
	/// واحد تامین‌کننده «زمان در راه» — مقادیر عیناً شناسه HRM_OrgUnit در HTS هستند
	/// (Pln_LeadTime.SupplierUnitId ∈ {6, 51}) و در مهاجرت/ورود اکسل بدون نگاشت استفاده می‌شوند.
	/// با BuyCategoryItemSupplierEnum (1/2 = کانال قلم دسته خرید از راهکاران) مفهوم مستقلی دارد — D27.
	/// </summary>
	public enum LeadTimeSupplierEnum
	{
		[Display(Name = "بازرگانی خارجی")]
		ForeignTrade = 6,
		[Display(Name = "تامین و خرید")]
		SupplyAndPurchase = 51,
	}
}
