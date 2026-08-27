using Common.Attributes;
using Entities.App.Edms.Enums;
using Entities.Auth;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Edms
{
	[Display(Name = "مدارک مهندسی")]
	[Table("Document", Schema = "Edms")]
	public class Document : BaseEntity
	{
		[DisplayName("پروژه")]
		[DisplayInfo(null, true, type: SystemType.Entity, showInRelationData: true)]
		public Project? Project { get; set; }

		public long? ProjectId { get; set; }


		[DisplayName("سند")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public ProjectVpis? DocumentVpis { get; set; }

		public long? DocumentVpisId { get; set; }


		[DisplayName("هدف از تولید مدرک")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public DocumentGoalOfProductionEnum? GoalOfProduction { get; set; }


		[DisplayName("نفر ساعت")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? HourPrePerson { get; set; }


		[DisplayName("تاریخ انتشار/ارسال")]
		[DisplayInfo(null, false, type: SystemType.DateShamsi)]
		public string? PublicationShamsiDate { get; set; }


		[DisplayName("تاریخ انتشار/ارسال")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? PublicationMiladiDate { get; set; }


		[DisplayName("تاریخ تایید شمسی")]
		[DisplayInfo(null, false, type: SystemType.DateTimeShamsi)]
		public string? ApprovedShamsiDateTime { get; set; }


		[DisplayName("تاریخ تایید میلادی")]
		[DisplayInfo(null, false, type: SystemType.DateTime)]
		public DateTime? ApprovedMiladiDateTime { get; set; }


		[DisplayName("تاریخ بررسی شمسی")]
		[DisplayInfo(null, false, type: SystemType.DateTimeShamsi)]
		public string? RevieweShamsiDateTime { get; set; }


		[DisplayName("تاریخ بررسی میلادی")]
		[DisplayInfo(null, false, type: SystemType.DateTime)]
		public DateTime? RevieweMiladiDateTime { get; set; }



		[DisplayName("بررسی کننده")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public User? Reviewer { get; set; }

		public long? ReviewerId { get; set; }


		[DisplayName("تایید کننده")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public User? Approver { get; set; }

		public long? ApproverId { get; set; }


		[DisplayName("توضیحات")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(2000)]
		public string? Comment { get; set; }


		[DisplayName("فایل اصلی")]
		[DisplayInfo(null, false, type: SystemType.File, fileTypes: ".rar,.zip,.pdf,.excel,.word,.docx")]
		public FileEntity? MainFile { get; set; }

		public long? MainFileId { get; set; }


		[DisplayName("فایل مادر")]
		[DisplayInfo(null, false, type: SystemType.File, fileTypes: ".rar,.zip,.pdf,.excel,.word,.docx")]
		public FileEntity? MotherFile { get; set; }

		public long? MotherFileId { get; set; }


		[DisplayName("فایل ثانوی")]
		[DisplayInfo(null, false, type: SystemType.File, fileTypes: ".rar,.zip,.pdf,.excel,.word,.docx")]
		public FileEntity? SecondaryFile { get; set; }

		public long? SecondaryFileId { get; set; }


		[DisplayName("فایل ReplySheet")]
		[DisplayInfo(null, false, type: SystemType.File, fileTypes: ".rar,.zip,.pdf,.excel,.word,.docx")]
		public FileEntity? ReplySheet { get; set; }

		public long? ReplySheetId { get; set; }


		[DisplayName("فایل ReplySheet دوم")]
		[DisplayInfo(null, false, type: SystemType.File, fileTypes: ".rar,.zip,.pdf,.excel,.word,.docx")]
		public FileEntity? SecondReplySheet { get; set; }

		public long? SecondReplySheetId { get; set; }


		[DisplayName("وضعیت مدرک")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public DocumentStatusEnums? Status { get; set; }


		[DisplayName("بازنگری")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int Revision { get; set; }

		[DisplayName("آخرین رویژن")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool IsLatest { get; set; }

		[DisplayName("کامنتها")]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]
 
		public List<DocumentComment> Comments { get; set; }=new List<DocumentComment>();

		/// <summary>
		/// شناسه مدرک در سیستم قدیم (TotalSystem / HTS) برای همگام‌سازی یکتا
		/// </summary>
		public long HtsId { get; set; }
	}
	[Display(Name = "کامنت های مدارک مهندسی")]
	[Table("DocumentComment", Schema = "Edms")]
	public class DocumentComment:BaseEntity
	{

		public Document Document { get; set; }
		public long DocumentId { get; set; }

		[DisplayName("پیوست")]
		[DisplayInfo(null, false, type: SystemType.File, fileTypes: ".rar,.zip,.pdf,.excel,.word")]
		public FileEntity? Attachment { get; set; }

		public long? AttachmentId { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(2000)]
		public string? Comment { get; set; }

		[DisplayName("وضعیت مدرک")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public DocumentStatusEnums? Status { get; set; }

		[DisplayName("متولی Hold")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public DocumentLegalHolderEnum? LegalHolder { get; set; }


		[DisplayName("مسئول Hold")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public User? HOLDOwner { get; set; }

		public long? HOLDOwnerId { get; set; }


		[DisplayName("توضیحات Hold")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(2000)]
		public string? HoldDetails { get; set; }

		/// <summary>
		/// شناسه کامنت مدرک در سیستم قدیم (TotalSystem / HTS) برای همگام‌سازی یکتا
		/// </summary>
		public long HtsId { get; set; }
	}


}
