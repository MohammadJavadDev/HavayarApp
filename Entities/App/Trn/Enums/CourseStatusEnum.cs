using System.ComponentModel.DataAnnotations;

namespace Entities.App.Trn.Enums
{
	/// <summary>
	/// وضعیت دوره — معادل legacy Gnr_Lookup با LookupType_FK = 108 (وضعیت دوره).
	/// مقادیر از دیتابیس legacy HTS تأیید شده است (2026-09-09).
	/// </summary>
	public enum CourseStatusEnum
	{
		[Display(Name = "برنامه ریزی")]
		Planned = 617,

		[Display(Name = "بازاریابی")]
		Marketing = 618,

		[Display(Name = "در حال برگزاری")]
		InProgress = 619,

		[Display(Name = "اجرا شده")]
		Executed = 620,

		[Display(Name = "به تعویق افتاده")]
		Postponed = 621
	}
}
