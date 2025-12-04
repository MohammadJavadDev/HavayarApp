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
	}
}
