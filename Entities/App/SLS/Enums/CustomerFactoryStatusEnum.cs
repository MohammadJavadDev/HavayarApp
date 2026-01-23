using System.ComponentModel.DataAnnotations;

namespace Entities.App.SLS.Enums
{
	public enum CustomerFactoryStatusEnum
	{
		[Display(Name = "در حال کار")]
		InProgress = 158,
		[Display(Name = "تعطیل")]
		Holiday = 159,
		[Display(Name = "افزایش ظرفیت")]
		CapacityIncrease = 160,
	}
}
