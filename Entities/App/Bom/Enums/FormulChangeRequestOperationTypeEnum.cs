using System.ComponentModel.DataAnnotations;

namespace Entities.App.Bom.Enums
{
	public enum FormulChangeRequestOperationTypeEnum
	{
		[Display(Name = "افزودن")]
		Add = 0,
		[Display(Name = "جایگزینی")]
		Replacement = 1,
		[Display(Name = "حذف")]
		Deleted = 2,
	}
}
