using System.ComponentModel.DataAnnotations;

namespace Entities.App.Trn.Enums
{
	/// <summary>
	/// وضعیت گواهی — معادل legacy Gnr_Lookup با LookupType_FK = 320 (وضعیت گواهینامه پایان دوره آموزشی).
	/// مقادیر از دیتابیس legacy HTS تأیید شده است (2026-09-09).
	/// </summary>
	public enum CourseCertificateStatusEnum
	{
		[Display(Name = "صدور و ارسال")]
		IssuedAndSent = 2393,

		[Display(Name = "عدم صدور")]
		NotIssued = 2394,

		[Display(Name = "صدور")]
		Issued = 2395
	}
}
