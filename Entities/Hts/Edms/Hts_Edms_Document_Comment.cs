using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Edms
{
	[Table("Edms_Document_Comment")]
	public class Hts_Edms_Document_Comment
	{
		[Key]
		public int Document_Comment_ID { get; set; }

		public int Document_FK { get; set; }

		public byte Document_Status_FK { get; set; }

		[StringLength(800)]
		public string? Attachment_FileName { get; set; }

		[StringLength(1024)]
		public string? Attachment_FilePath { get; set; }

		public short CreatedUser_FK { get; set; }

		[StringLength(10)]
		public string? CreatedDate_Shamsi { get; set; }

		[Column(TypeName = "date")]
		public DateTime CreatedDate { get; set; }

		[StringLength(5)]
		public string? CreatedTime { get; set; }

		[StringLength(4000)]
		public string? Comment { get; set; }

		public bool IsForDcc { get; set; }

		[StringLength(1024)]
		public string? ReceivedTransmittalNumber { get; set; }

		public short? HoldCause_FK { get; set; }

		public short? HoldResponsibleId { get; set; }

		public DateTime? CreationDateTime { get; set; }

		[StringLength(16)]
		public string? CreationDateTimeInText { get; set; }
	}
}
