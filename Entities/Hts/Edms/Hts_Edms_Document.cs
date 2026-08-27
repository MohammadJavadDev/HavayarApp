using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Edms
{
	[Table("Edms_Document")]
	public class Hts_Edms_Document
	{
		[Key]
		public int Document_ID { get; set; }

		public int Project_Vpis_FK { get; set; }

		public byte DocPoi_FK { get; set; }

		[StringLength(2040)]
		public string? Document_FileName { get; set; }

		[StringLength(1024)]
		public string? Document_FilePath { get; set; }

		[StringLength(2040)]
		public string? Document_NativeFileName { get; set; }

		[StringLength(1024)]
		public string? Document_NativeFilePath { get; set; }

		[StringLength(2040)]
		public string? Document_SecondaryFileName { get; set; }

		[StringLength(1024)]
		public string? Document_SecondaryFilePath { get; set; }

		[StringLength(2040)]
		public string? Document_ReplySheetFileName { get; set; }

		[StringLength(1024)]
		public string? Document_ReplySheetFilePath { get; set; }

		[StringLength(2040)]
		public string? Document_SecondReplySheetFileName { get; set; }

		[StringLength(1024)]
		public string? Document_SecondReplySheetFilePath { get; set; }

		public long? Document_FileSize { get; set; }

		public long? Document_NativeFileSize { get; set; }

		public long? Document_SecondaryFileSize { get; set; }

		public long? Document_ReplySheetFileSize { get; set; }

		public long? Document_SecondReplySheetFileSize { get; set; }

		public byte? Revision { get; set; }

		[Column(TypeName = "date")]
		public DateTime Due_Date { get; set; }

		[StringLength(10)]
		public string? Due_Date_Shamsi { get; set; }

		public int? ConsumedManHours { get; set; }

		public byte? LastStatusId { get; set; }

		public short? LastStatusUserId { get; set; }

		[Column(TypeName = "date")]
		public DateTime? LastCommentDate { get; set; }

		[StringLength(10)]
		public string? LastCommentDateShamsi { get; set; }

		public bool HasClientComment { get; set; }

		public bool IsLatest { get; set; }

		public short CreatedUser_FK { get; set; }

		[Column(TypeName = "date")]
		public DateTime? CreatedDate { get; set; }

		[StringLength(10)]
		public string? CreatedDate_Shamsi { get; set; }

		[StringLength(5)]
		public string? CreatedTime { get; set; }

		public short PreparedUser_FK { get; set; }

		[Column(TypeName = "date")]
		public DateTime? PreparedDate { get; set; }

		[StringLength(10)]
		public string? PreparedDate_Shamsi { get; set; }

		public short? CheckedUser_FK { get; set; }

		[Column(TypeName = "date")]
		public DateTime? CheckedDate { get; set; }

		[StringLength(10)]
		public string? CheckedDate_Shamsi { get; set; }

		public short? ApprovedUser_FK { get; set; }

		[Column(TypeName = "date")]
		public DateTime? ApprovedDate { get; set; }

		[StringLength(10)]
		public string? ApprovedDate_Shamsi { get; set; }

		public short? NotificationMethodId { get; set; }
	}
}
