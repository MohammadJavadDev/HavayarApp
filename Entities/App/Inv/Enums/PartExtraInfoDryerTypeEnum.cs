using System.ComponentModel.DataAnnotations;

namespace Entities.App.Inv.Enums
{
	public enum PartExtraInfoDryerTypeEnum
	{
		[Display(Name = "Externally Heated Desiccant Dryer")]
		ExternallyHeatedDesiccantDryer = 710,
		[Display(Name = "Heatless Desiccant Air Dryer")]
		HeatlessDesiccantAirDryer = 711,
		[Display(Name = "Blower Heated Dryer")]
		BlowerHeatedDryer = 3000,

	}
}
