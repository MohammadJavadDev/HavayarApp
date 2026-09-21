using System.ComponentModel.DataAnnotations;

namespace Entities.App.Trn.Enums
{
	/// <summary>
	/// تحصیلات — معادل legacy Gnr_Lookup با LookupType_FK = 100 («تحصیلات»).
	/// اعضای تأییدشده از کوئری جدول TotalSystem.dbo.Gnr_Lookup در تاریخ 2026-09-09؛
	/// شناسه عددی هر عضو برابر Lookup_ID و برچسب فارسی برابر Lookup_Title_Fa است.
	/// به‌صورت مشترک در TeacherBank.Education و Participant.Education استفاده می‌شود.
	/// </summary>
	public enum EducationEnum
	{
		[Display(Name = "دیپلم")]
		Diploma = 506,

		[Display(Name = "فوق دیپلم")]
		AssociateDegree = 507,

		[Display(Name = "لیسانس")]
		Bachelor = 508,

		[Display(Name = "فوق لیسانس")]
		Master = 509,

		[Display(Name = "دکترا")]
		Doctorate = 510,

		[Display(Name = "فوق دکتری")]
		PostDoctorate = 2457,

		[Display(Name = "دانشجوی (فوق دیپلم)")]
		AssociateStudent = 2458,

		[Display(Name = "دانشجوی (لیسانس)")]
		BachelorStudent = 2459,

		[Display(Name = "دانشجوی (فوق لیسانس)")]
		MasterStudent = 2460,

		[Display(Name = "دانشجوی (دکتری)")]
		DoctorateStudent = 2461
	}
}
