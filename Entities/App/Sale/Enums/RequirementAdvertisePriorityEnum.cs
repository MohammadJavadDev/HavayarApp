using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sale.Enums
{
	public enum RequirementAdvertisePriorityEnum
	{
		[Display(Name = "ندارد")]
		None = 1,
		[Display(Name = "اول (سه ماهه)")]
		FirstTrimester = 2,
		[Display(Name = "دوم (شش ماهه)")]
		SecondSixMonth = 3,
		[Display(Name = "سوم (نه ماهه)")]
		ThirdOfSeptember = 4,
		[Display(Name = "چهارم (بیش از یکسال)")]
		FourthMoreThanOneYear = 5,
	}
}
