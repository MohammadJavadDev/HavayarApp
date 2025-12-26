using System.ComponentModel.DataAnnotations;

namespace Entities.App.Gnr.Enums
{
	public enum PartyGenderEnum
	{
		[Display(Name = "مرد")]
		Men = 1,
		[Display(Name = "زن")]
		Women = 2,
	}
}
