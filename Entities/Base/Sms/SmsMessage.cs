using Common.Attributes;
using Entities.Base.Sms.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Base.Sms
{
	/// <summary>
	/// صف و لاگ پیامک. هر ردیف = یک پیامک به یک گیرنده.
	/// ثبت پیامک فقط از طریق <c>ISmsService.Enqueue*</c> انجام می‌شود (ارسال مستقیم به پنل ممنوع)؛
	/// ارسال واقعی را job پس‌زمینه انجام می‌دهد و وضعیت/شناسه پنل/خطا را همین‌جا ثبت می‌کند.
	/// </summary>
	[Display(Name = "پیامک")]
	[Table("SmsMessage", Schema = "system")]
	[Index(nameof(Status), nameof(ScheduledOnMiladiDateTime))]
	[Index(nameof(MobileNumber))]
	[Index(nameof(EventKey))]
	[Index(nameof(SourceEntityType), nameof(SourceEntityId))]
	[Index(nameof(ProviderMessageId))]
	public class SmsMessage : BaseEntity
	{
		[DisplayName("رویداد")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(100)]
		public string EventKey { get; set; } = string.Empty;

		[DisplayName("دسته")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public SmsCategoryEnum Category { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public SmsStatusEnum Status { get; set; } = SmsStatusEnum.Pending;

		[DisplayName("شماره گیرنده")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(20)]
		public string MobileNumber { get; set; } = string.Empty;

		[DisplayName("نام گیرنده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(200)]
		public string? RecipientName { get; set; }

		[DisplayName("متن پیامک")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(2000)]
		public string Body { get; set; } = string.Empty;

		/// <summary>اگر true باشد (مثل کد تأیید) متن بعد از ارسال موفق از دیتابیس پاک می‌شود.</summary>
		[DisplayName("متن حساس")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool IsSensitive { get; set; }

		[DisplayName("تعداد بخش پیامک")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int SegmentCount { get; set; } = 1;

		[DisplayName("نوع موجودیت مبدأ")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(150)]
		public string? SourceEntityType { get; set; }

		[DisplayName("شناسه موجودیت مبدأ")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? SourceEntityId { get; set; }

		// ---------- زمان‌بندی ----------

		[DisplayName("زمان ارسال مجاز (میلادی)")]
		[DisplayInfo(null, false, SystemType.DateTime)]
		public DateTime? ScheduledOnMiladiDateTime { get; set; }

		[DisplayName("زمان ارسال مجاز")]
		[MaxLength(30)]
		[DisplayInfo("ScheduledOnMiladiDateTime", true, SystemType.DateTimeShamsi)]
		public string? ScheduledOnShamsiDateTime { get; set; }

		[DisplayName("آخرین تلاش")]
		[DisplayInfo(null, false, SystemType.DateTime)]
		public DateTime? LastAttemptOnMiladiDateTime { get; set; }

		[DisplayName("تعداد تلاش")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int AttemptCount { get; set; }

		// ---------- نتیجه ----------

		[DisplayName("زمان ارسال (میلادی)")]
		[DisplayInfo(null, false, SystemType.DateTime)]
		public DateTime? SentOnMiladiDateTime { get; set; }

		[DisplayName("زمان ارسال")]
		[MaxLength(30)]
		[DisplayInfo("SentOnMiladiDateTime", true, SystemType.DateTimeShamsi)]
		public string? SentOnShamsiDateTime { get; set; }

		[DisplayName("زمان تحویل (میلادی)")]
		[DisplayInfo(null, false, SystemType.DateTime)]
		public DateTime? DeliveredOnMiladiDateTime { get; set; }

		[DisplayName("زمان تحویل")]
		[MaxLength(30)]
		[DisplayInfo("DeliveredOnMiladiDateTime", true, SystemType.DateTimeShamsi)]
		public string? DeliveredOnShamsiDateTime { get; set; }

		/// <summary>کد وضعیت دلیوری پنل (sms.ir: 1 رسیده به گوشی، 2 نرسیده به گوشی، 3 پردازش در مخابرات، 4 نرسیده به مخابرات، 5 رسیده به مخابرات، 6 خطا، 7 لیست سیاه).</summary>
		[DisplayName("کد وضعیت دلیوری")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? DeliveryStateCode { get; set; }

		[DisplayName("تعداد استعلام دلیوری")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int DeliveryCheckCount { get; set; }

		[DisplayName("آخرین استعلام دلیوری")]
		[DisplayInfo(null, false, SystemType.DateTime)]
		public DateTime? LastDeliveryCheckOnMiladiDateTime { get; set; }

		[DisplayName("پنل پیامکی")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(50)]
		public string? ProviderName { get; set; }

		[DisplayName("شماره خط")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(30)]
		public string? LineNumber { get; set; }

		[DisplayName("شناسه پیام در پنل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(100)]
		public string? ProviderMessageId { get; set; }

		[DisplayName("پیام خطا")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2000)]
		public string? ErrorMessage { get; set; }

		[DisplayName("سابقه تلاش‌ها")]
		public virtual ICollection<SmsMessageAttempt> Attempts { get; set; } = [];
	}
}
