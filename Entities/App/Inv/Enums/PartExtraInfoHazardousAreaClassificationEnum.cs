using System.ComponentModel.DataAnnotations;

namespace Entities.App.Inv.Enums
{
	public enum PartExtraInfoHazardousAreaClassificationEnum
	{
		[Display(Name = "Safe Area")]
		SafeArea = 660,
		[Display(Name = "Hazard")]
		Hazard = 661,
	}
}
