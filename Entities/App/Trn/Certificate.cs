using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Trn
{
	[Display(Name = "گواهی‌نامه دوره")]
	[Table("Certificate", Schema = "Trn")]
	public class Certificate : BaseEntity
	{
		// Exactly one of these two is set; the other stays null (track determines which).
		public long? CourseParticipantId { get; set; }
		public virtual CourseParticipant? CourseParticipant { get; set; }

		public long? CourseCompanyParticipantId { get; set; }
		public virtual CourseCompanyParticipant? CourseCompanyParticipant { get; set; }

		[DisplayName("شماره گواهی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? CertificateNo { get; set; }  // auto-generated (R7), read-only in UI

		// Standard Shamsi/Miladi pair: Shamsi string bound via data-bind, Miladi via value="@Model?.IssueMiladiDate".
		[DisplayName("تاریخ صدور شمسی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? IssueShamsiDate { get; set; }

		[DisplayName("تاریخ صدور میلادی")]
		[DisplayInfo(null, true, type: SystemType.Date)]
		public DateTime? IssueMiladiDate { get; set; }

		[DisplayName("قالب گواهی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(50)]
		public string? TemplateKey { get; set; }  // which of the 11 templates was used (R8), stored at issue time

		[DisplayName("توضیحات")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? Description { get; set; }
	}
}
