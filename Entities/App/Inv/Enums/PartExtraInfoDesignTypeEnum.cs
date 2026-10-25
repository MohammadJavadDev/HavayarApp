using System.ComponentModel.DataAnnotations;

namespace Entities.App.Inv.Enums
{
	public enum PartExtraInfoDesignTypeEnum
	{
		// شناسه لوکاپ HTS. کمپرسور و PSA از Inv_DesignType؛ درایر از Inv_Dryer_Design.
		// 656 شناسه Direct Coupling است و نباید Asme باشد.
		[Display(Name = "Routine")]
		Routine = 654,
		[Display(Name = "Special")]
		Special = 655,
		[Display(Name = "Routine")]
		DryerRoutine = 708,
		[Display(Name = "Asme")]
		Asme = 709,
		[Display(Name = "Special")]
		DryerSpecial = 3056,
	}
}
