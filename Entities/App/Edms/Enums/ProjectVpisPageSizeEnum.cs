using System.ComponentModel.DataAnnotations;

namespace Entities.App.Edms.Enums
{
	public enum ProjectVpisPageSizeEnum
	{
		[Display(Name = "A4")]
		A4 = 0,
		[Display(Name = "A5")]
		A5 = 1,
		[Display(Name = "A6")]
		A6 = 2,
		[Display(Name = "A1")]
		A1 = 3,
		[Display(Name = "A2")]
		A2 = 4,
		[Display(Name = "A3")]
		A3 = 5,
	}
}
