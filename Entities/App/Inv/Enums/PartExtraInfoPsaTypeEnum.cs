using System.ComponentModel.DataAnnotations;

namespace Entities.App.Inv.Enums
{
	public enum PartExtraInfoPsaTypeEnum
	{
		[Display(Name = "Oxygen Generator")]
		OxygenGenerator = 811,
		[Display(Name = "Nitrogen Generator")]
		NitrogenGenerator = 812,

		[Display(Name = "Vessel")]
		Vessel = 3144,

		[Display(Name = "Filter")]
		Filter = 3145,

		[Display(Name = "Water Trap")]
		WaterTrap = 3146,

		[Display(Name = "Coal Tower")]
		CoalTower = 3147,

	}
}
