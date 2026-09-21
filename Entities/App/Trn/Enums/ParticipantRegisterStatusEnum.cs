using System.ComponentModel.DataAnnotations;

namespace Entities.App.Trn.Enums
{
	/// <summary>
	/// وضعیت ثبت‌نام — معادل legacy Gnr_Lookup با LookupType_FK = 110 (وضعیت ثبت نام).
	/// مقادیر از دیتابیس legacy HTS تأیید شده است (2026-09-09).
	/// </summary>
	public enum ParticipantRegisterStatusEnum
	{
		[Display(Name = "رزرو")]
		Reserved = 624,

		[Display(Name = "قطعی")]
		Confirmed = 625
	}
}
