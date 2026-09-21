using System.ComponentModel.DataAnnotations;

namespace Entities.App.Trn.Enums
{
	/// <summary>
	/// وضعیت حضور — معادل legacy Gnr_Lookup با LookupType_FK = 322 (وضعیت حضور).
	/// مقادیر از دیتابیس legacy HTS تأیید شده است (2026-09-09).
	/// </summary>
	public enum CourseAttendanceStatusEnum
	{
		[Display(Name = "اعلام عدم نیاز")]
		NoNeedDeclared = 2398,

		[Display(Name = "عدم حضور")]
		Absent = 2399,

		[Display(Name = "حضور قطعی")]
		ConfirmedAttendance = 2400,

		[Display(Name = "عدم پاسخگویی")]
		NoResponse = 2401
	}
}
