using System.ComponentModel.DataAnnotations;

namespace Entities.App.Trn.Enums
{
	/// <summary>
	/// وضعیت ارسال دعوت‌نامه — معادل legacy Gnr_Lookup با LookupType_FK = 319 (نحوه ارسال دعوت نامه).
	/// مقادیر از دیتابیس legacy HTS تأیید شده است (2026-09-09).
	/// </summary>
	public enum CourseSendInvitationEnum
	{
		[Display(Name = "پست الکترونیک")]
		Email = 2390,

		[Display(Name = "نمابر")]
		Fax = 2391,

		[Display(Name = "شبکه های اجتماعی")]
		SocialNetworks = 2392
	}
}
