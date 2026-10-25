using System.ComponentModel.DataAnnotations;

namespace Entities.Base.Sms.Enums
{
	/// <summary>
	/// چرخه عمر یک پیامک در صف/لاگ.
	/// Pending -> Sending -> Sent -> Delivered، یا Pending -> Failed / InvalidMobile / Cancelled.
	/// پس از خطای موقت (کمتر از سقف تلاش) پیامک دوباره به Pending برمی‌گردد و ScheduledOn عقب می‌افتد.
	/// </summary>
	public enum SmsStatusEnum
	{
		[Display(Name = "در صف ارسال")]
		Pending = 0,

		[Display(Name = "در حال ارسال")]
		Sending = 1,

		[Display(Name = "تحویل پنل پیامکی")]
		Sent = 2,

		[Display(Name = "تحویل به گیرنده")]
		Delivered = 3,

		[Display(Name = "ناموفق")]
		Failed = 4,

		[Display(Name = "شماره نامعتبر")]
		InvalidMobile = 5,

		[Display(Name = "لغو شده")]
		Cancelled = 6,

		/// <summary>پنل پیامک را پذیرفت ولی طبق گزارش دلیوری به گوشی نرسید (نرسیده به گوشی/مخابرات، خطا، لیست سیاه).</summary>
		[Display(Name = "تحویل نشد")]
		NotDelivered = 7
	}
}
