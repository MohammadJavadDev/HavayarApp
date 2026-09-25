using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sec
{
	/// <summary>لاگ ارسال ایمیل دبیرخانه — معادل HTS <c>Sec_EmailsLog</c>.</summary>
	[Display(Name = "لاگ ایمیل دبیرخانه")]
	[Table("EmailsLog", Schema = "Sec")]
	public class EmailsLog : BaseEntity
	{
		[DisplayName("پرسنل دبیرخانه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(PersonnelId))]
		public Personnel? Personnel { get; set; }

		public long PersonnelId { get; set; }

		[DisplayName("نوع ایمیل")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(EmailTypeId))]
		public EmailType? EmailType { get; set; }

		public long EmailTypeId { get; set; }

		[DisplayName("گروه گیرنده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ReciversGroupId))]
		public ReciversGroup? ReciversGroup { get; set; }

		/// <summary>در سیستم قدیم همیشه 1 (دفتر) ذخیره می‌شود حتی اگر گیرنده واقعی فرق کند.</summary>
		public long ReciversGroupId { get; set; }

		[DisplayName("تاریخ ارسال")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		[Column(TypeName = "date")]
		public DateTime SendDate { get; set; }

		[DisplayName("ساعت ارسال")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public TimeSpan SendTime { get; set; }
	}
}
