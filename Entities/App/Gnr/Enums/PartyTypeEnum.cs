using System.ComponentModel.DataAnnotations;

namespace Entities.App.Gnr.Enums
{
	public enum PartyTypeEnum
	{
		[Display(Name = "حقیقی")]
		Real = 0,
		[Display(Name = "حقوقی")]
		Legal = 1,
	}
}
