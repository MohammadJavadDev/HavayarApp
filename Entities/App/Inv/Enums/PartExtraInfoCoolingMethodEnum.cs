using System.ComponentModel.DataAnnotations;

namespace Entities.App.Inv.Enums
{
	public enum PartExtraInfoCoolingMethodEnum
	{
		[Display(Name = "Air Cooled")]
		AirCooled = 662,
		[Display(Name = "Water Cooled")]
		WaterCooled = 663,
	}
}
