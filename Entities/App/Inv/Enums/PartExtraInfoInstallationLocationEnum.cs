using System.ComponentModel.DataAnnotations;

namespace Entities.App.Inv.Enums
{
	public enum PartExtraInfoInstallationLocationEnum
	{
		[Display(Name = "Indoor")]
		Indoor = 658,
		[Display(Name = "Outdoor Under Shelter")]
		OutdoorUnderShelter = 659,
		[Display(Name = "Indoor/ Outdoor Under Shelter")]
		IndoorOutdoorUnderShelter = 3148,
	}
}
