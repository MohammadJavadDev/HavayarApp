using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Sup
{
	[Table("Sup_OpenOrderRequest_Attachment")]
	public class Hts_Sup_OpenOrderRequest_Attachment
	{
		[Key]
		public int OpenOrderRequest_Attachment_ID { get; set; }

		public int OpenOrderRequest_FK { get; set; }

		[StringLength(800)]
		public string? Attachment_FileName { get; set; }

		[StringLength(1024)]
		public string? AttachmentFilePath { get; set; }

		public long? Attachment_FileSize { get; set; }

		public byte[]? Attachment_FileContent { get; set; }

		[StringLength(2400)]
		public string? Attachment_Comment { get; set; }

		public short? DocumentTypeId { get; set; }
	}
}
