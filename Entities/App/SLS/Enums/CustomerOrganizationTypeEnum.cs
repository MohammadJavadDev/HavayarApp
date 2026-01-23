using System.ComponentModel.DataAnnotations;

namespace Entities.App.SLS.Enums
{
	public enum CustomerOrganizationTypeEnum
	{
		[Display(Name = "دولتی")]
		Governmental = 103,
		[Display(Name = "غیر دولتی")]
		NonGovernmental = 105,
	}
}
