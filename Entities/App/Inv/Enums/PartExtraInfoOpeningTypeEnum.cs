using System.ComponentModel.DataAnnotations;

namespace Entities.App.Inv.Enums
{
	public enum PartExtraInfoOpeningTypeEnum
	{
		[Display(Name = "Hand Hole")]
		HandHole = 809,
		[Display(Name = "Man Hole")]
		ManHole = 810,

		[Display(Name = "N/A")]
		NA = 3018,

	}
}
