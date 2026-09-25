using System.ComponentModel.DataAnnotations;

namespace Entities.App.Srv.Enums
{
	/// <summary>طرف پرداخت کنسلی بلیط — Gnr_Lookup LookupType_FK = 198.
	/// در دیتابیس با پرچم‌های <c>IsPaidByApplicant</c> / <c>IsPaidByFinancialDepartment</c> ذخیره می‌شود.</summary>
	public enum PaySideEnum
	{
		[Display(Name = "پرداخت توسط متقاضی")]
		Applicant = 1305,

		[Display(Name = "پرداخت توسط امور مالی")]
		FinancialDepartment = 1306
	}
}
