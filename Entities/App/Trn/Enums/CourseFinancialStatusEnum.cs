using System.ComponentModel.DataAnnotations;

namespace Entities.App.Trn.Enums
{
	/// <summary>
	/// وضعیت مالی دوره — معادل legacy Gnr_Lookup با LookupType_FK = 109 (وضعیت مالی دوره).
	/// مقادیر از دیتابیس legacy HTS تأیید شده است (2026-09-09).
	/// </summary>
	public enum CourseFinancialStatusEnum
	{
		[Display(Name = "بدهکار")]
		Debtor = 622,

		[Display(Name = "تسویه")]
		Settled = 623
	}
}
