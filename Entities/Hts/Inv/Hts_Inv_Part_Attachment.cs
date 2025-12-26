using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Hts.Inv
{
	[Table("Inv_Part_Attachment")]
    public class Hts_Inv_Part_Attachment
    {

		[Key]
		public int Part_Attachment_ID { get; set; }

		public long Part_FK { get; set; }

		[StringLength(800)]
		public string? Attachment_FileName { get; set; }

		public byte[]? Attachment_FileContent { get; set; }

		public short? CreatedUser_FK { get; set; }

		[StringLength(10)]
		public string? CreatedDate { get; set; }

		[StringLength(5)]
		public string? CreatedTime { get; set; }

		public short? Project_Document_Type_FK { get; set; }

		[StringLength(2048)]
		public string? DocumentCode { get; set; }

		[StringLength(4000)]
		public string? DocumentName { get; set; }

		public bool? IsMain { get; set; }

		[StringLength(10)]
		public string? UpdatedDate { get; set; }

		[StringLength(4000)]
		public string? Comment { get; set; }

		[StringLength(1024)]
		public string? AttachmentFilePath { get; set; }

		public long? Attachment_FileSize { get; set; }

	}
}
