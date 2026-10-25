using System.ComponentModel.DataAnnotations;

namespace Entities.App.Hrm.Enums
{
	public enum StaffRequestSexEnum
	{
		[Display(Name = "تعیین نشده")]
		Unspecified = 1,

		[Display(Name = "مرد")]
		Male = 2,

		[Display(Name = "زن")]
		Female = 3
	}

	public enum StaffRequestMaritalEnum
	{
		[Display(Name = "مجرد")]
		Single = 1,

		[Display(Name = "متاهل")]
		Married = 2,

		[Display(Name = "تعیین نشده")]
		Unspecified = 3
	}

	public enum StaffRequestEmploymentKindEnum
	{
		[Display(Name = "نیروی جدید")]
		NewForce = 50,

		[Display(Name = "نیروی جایگزین")]
		Replacement = 51
	}

	public enum StaffRequestWorkplaceEnum
	{
		[Display(Name = "دفتر مرکزی")]
		HeadOffice = 43,

		[Display(Name = "کارخانه")]
		Factory = 44
	}

	public enum StaffRequestStatusEnum
	{
		[Display(Name = "در جریان")]
		InProgress = 76,

		[Display(Name = "انجام شده")]
		Done = 77,

		[Display(Name = "آرشیو")]
		Archive = 78,

		[Display(Name = "کنسل")]
		Cancelled = 80,

		[Display(Name = "در حال بررسی توسط منابع انسانی")]
		HrReview = 1773,

		[Display(Name = "هولد شده")]
		OnHold = 2539
	}
}
