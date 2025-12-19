using System.ComponentModel.DataAnnotations;

namespace Entities.App.Inv.Enums
{
	public enum PartTypeEnum
	{
		[Display(Name = "محصول")]
		Product = 8,
		[Display(Name = "نیمه ساخته")]
		HalfBuilt = 9,
		[Display(Name = "مواد")]
		Materials = 10,
		[Display(Name = "دارایی ثابت")]
		FixedAsset = 11,
		[Display(Name = "سایر")]
		Other = 12,
	}
}
