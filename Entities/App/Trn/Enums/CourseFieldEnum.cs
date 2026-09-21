using System.ComponentModel.DataAnnotations;

namespace Entities.App.Trn.Enums
{
	/// <summary>
	/// زمینه دوره — معادل legacy Gnr_Lookup با LookupType_FK = 99 (زمینه دوره).
	/// مقادیر از دیتابیس legacy HTS تأیید شده است.
	/// </summary>
	public enum CourseFieldEnum
	{
		[Display(Name = "سی ان جی")]
		Cng = 502,

		[Display(Name = "فنی مهندسی")]
		TechnicalEngineering = 504,

		[Display(Name = "سایر")]
		Other = 505
	}
}
