using System.ComponentModel.DataAnnotations;

namespace Entities.App.Trn.Enums
{
	/// <summary>
	/// نوع دوره در بانک دوره‌ها — معادل legacy Gnr_Lookup با LookupType_FK = 122 (نوع دوره).
	/// مقادیر از دیتابیس legacy HTS تأیید شده است.
	/// </summary>
	public enum CourseBankTypeEnum
	{
		[Display(Name = "اپراتوری")]
		Operator = 698,

		[Display(Name = "تکنسینی")]
		Technician = 699,

		[Display(Name = "غیر سی ان جی")]
		NonCng = 700
	}
}
