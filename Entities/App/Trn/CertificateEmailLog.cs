using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Trn
{
	[Display(Name = "لاگ ارسال ایمیل گواهی")]
	[Table("CertificateEmailLog", Schema = "Trn")]
	public class CertificateEmailLog : BaseEntity
	{
		public long CourseId { get; set; }
		public virtual Course? Course { get; set; }

		public long? CertificateId { get; set; }
		public virtual Certificate? Certificate { get; set; }

		[DisplayName("گیرنده")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(256)]
		public string? RecipientEmail { get; set; }

		[DisplayName("ارسال موفق بود")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsSuccess { get; set; }

		[DisplayName("پیام نتیجه")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? ResultMessage { get; set; }
	}
}
