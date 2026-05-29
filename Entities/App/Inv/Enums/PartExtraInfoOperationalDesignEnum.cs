using System.ComponentModel.DataAnnotations;

namespace Entities.App.Inv.Enums
{
	public enum PartExtraInfoOperationalDesignEnum
	{
		[Display(Name = "Industrial")]
		Industrial = 853,
		[Display(Name = "Medical")]
		Medical = 854,

		[Display(Name = "Low Purity")]
		LowPurity = 855,

		[Display(Name = "High Purity")]
		HighPurity = 856,
	}
}
