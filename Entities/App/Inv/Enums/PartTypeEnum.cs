using System.ComponentModel.DataAnnotations;

namespace Entities.App.Inv.Enums
{
	public enum PartTypeEnum
	{
		[Display(Name = "محصول")]
		Product = 2,
		[Display(Name = "نیمه ساخته")]
		HalfBuilt = 3,
		[Display(Name = "مواد")]
		Materials = 1,
		[Display(Name = "دارایی ثابت")]
		FixedAsset = 4,
		[Display(Name = "سایر")]
		Other =5,
	}
}
