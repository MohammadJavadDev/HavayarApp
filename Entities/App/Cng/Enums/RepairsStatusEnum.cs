using System.ComponentModel.DataAnnotations;

namespace Entities.App.Cng.Enums
{
	/// <summary>وضعیت تعمیرات CNG — Gnr_Lookup LookupType_FK = 141.</summary>
	public enum RepairsStatusEnum
	{
		[Display(Name = "در حال اجرا")]
		Running = 872,

		[Display(Name = "غیرقابل تعمیر")]
		Unrepairable = 873,

		[Display(Name = "نزد CNC")]
		AtCnc = 874,

		[Display(Name = "تکمیل شده")]
		Finished = 875
	}
}
