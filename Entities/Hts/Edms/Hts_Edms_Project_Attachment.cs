using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Edms
{
	[Table("Edms_Project_Attachment")]
	public class Hts_Edms_Project_Attachment
	{
		[Key]
		public int Project_Attachment_ID { get; set; }

		public int Project_FK { get; set; }

		[StringLength(255)]
		public string? Attachment_FileName { get; set; }

		public byte[]? Attachment_FileContent { get; set; }

		[StringLength(4000)]
		public string? Attachment_Comment { get; set; }

		public short? Project_Document_Type_FK { get; set; }

		[StringLength(1024)]
		public string? AttachmentFilePath { get; set; }
	}
}
