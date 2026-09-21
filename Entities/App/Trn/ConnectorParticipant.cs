using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Trn
{
	[Display(Name = "رابط شرکت")]
	[Table("ConnectorParticipant", Schema = "Trn")]
	public class ConnectorParticipant : BaseEntity
	{
		public long CompanyId { get; set; }
		public virtual Company? Company { get; set; }

		// FK to the Trn.Participant master (step 05). The legacy junction is (CompanyId + ParticipantId).
		[DisplayName("شرکت‌کننده (رابط)")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public long ParticipantId { get; set; }
		public virtual Participant? Participant { get; set; }

		[DisplayName("سمت در شرکت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Position { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? Description { get; set; }
	}
}
