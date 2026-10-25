using System.ComponentModel.DataAnnotations;

namespace Entities.Base.Sms.Enums
{
	/// <summary>دسته‌بندی کلی پیامک برای فیلتر و سیاست‌های خاص (مثلاً حذف متن کد تأیید).</summary>
	public enum SmsCategoryEnum
	{
		[Display(Name = "کد تأیید / احراز هویت")]
		Otp = 1,

		[Display(Name = "اطلاع‌رسانی به مشتری")]
		Customer = 2,

		[Display(Name = "اعلان داخلی")]
		Internal = 3,

		[Display(Name = "یادآوری / زمان‌بندی‌شده")]
		Reminder = 4,

		[Display(Name = "ارسال دستی")]
		Manual = 5
	}
}
