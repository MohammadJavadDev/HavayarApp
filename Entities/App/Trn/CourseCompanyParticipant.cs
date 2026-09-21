using Common.Attributes;
using Entities.App.Trn.Enums;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Trn
{
	[Display(Name = "شرکت‌کننده دوره (اختصاصی)")]
	[Table("CourseCompanyParticipant", Schema = "Trn")]
	public class CourseCompanyParticipant : BaseEntity
	{
		public long CourseId { get; set; }
		public virtual Course? Course { get; set; }

		[DisplayName("شرکت‌کننده")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public long ParticipantId { get; set; }
		public virtual Participant? Participant { get; set; }

		[DisplayName("امتیاز")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Score { get; set; }

		[DisplayName("وضعیت ثبت‌نام")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ParticipantRegisterStatusEnum? ParticipantRegisterStatus { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? Description { get; set; }

		// 0..1 certificate issued for this enrollment row (Step 09).
		public virtual Certificate? Certificate { get; set; }
	}
}
