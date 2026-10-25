using Common.Attributes;
using Entities.Base.Sms.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Base.Sms
{
	/// <summary>
	/// تاریخچه تلاش‌های ارسال یک پیامک (هر تلاش یک ردیف؛ هرگز بازنویسی نمی‌شود).
	/// وضعیت نهایی روی <see cref="SmsMessage"/> نگه‌داری می‌شود.
	/// </summary>
	[Display(Name = "تلاش ارسال پیامک")]
	[Table("SmsMessageAttempt", Schema = "system")]
	[Index(nameof(SmsMessageId))]
	public class SmsMessageAttempt : BaseEntity
	{
		public long SmsMessageId { get; set; }

		[ForeignKey(nameof(SmsMessageId))]
		public virtual SmsMessage? SmsMessage { get; set; }

		[DisplayName("شماره تلاش")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int AttemptNumber { get; set; }

		[DisplayName("نتیجه")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public SmsStatusEnum ResultStatus { get; set; }

		[DisplayName("شناسه پیام در پنل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(100)]
		public string? ProviderMessageId { get; set; }

		[DisplayName("کد پاسخ پنل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? ProviderStatusCode { get; set; }

		[DisplayName("پیام خطا")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2000)]
		public string? ErrorMessage { get; set; }

		[DisplayName("مدت پاسخ (میلی‌ثانیه)")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? DurationMs { get; set; }
	}
}
