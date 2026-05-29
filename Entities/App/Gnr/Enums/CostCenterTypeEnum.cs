using System.ComponentModel.DataAnnotations;

namespace Entities.App.Gnr.Enums
{
	public enum CostCenterTypeEnum
	{
		[Display(Name = "تولیدی")]
		Production = 0,
		[Display(Name = "خدماتی")]
		Services = 1,
		[Display(Name = "اداری")]
		Offises = 2,

	}
}
