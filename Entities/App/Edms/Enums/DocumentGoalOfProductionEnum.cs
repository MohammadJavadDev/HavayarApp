using System.ComponentModel.DataAnnotations;

namespace Entities.App.Edms.Enums
{
	public enum DocumentGoalOfProductionEnum
	{
		[Display(Name = "IFR")]
		IFR = 0,
		[Display(Name = "IFA")]
		IFA = 1,
		[Display(Name = "IFI")]
		IFI = 2,
		[Display(Name = "AFC")]
		AFC = 3,
		[Display(Name = "AB")]
		AB = 4,
	}
}
