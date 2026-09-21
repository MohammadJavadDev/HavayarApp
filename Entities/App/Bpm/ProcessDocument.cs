using Common.Attributes;
using Entities.App.Bpm.Enums;
using Entities.App.Hrm;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Bpm
{
	[Display(Name = "سند فرآیندی")]
	[Table("ProcessDocument", Schema = "Bpm")]
	public class ProcessDocument : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("سند والد")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public virtual ProcessDocument? Parent { get; set; }
		public long? ParentId { get; set; }

		[DisplayName("بازنگری")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short Revision { get; set; }

		[DisplayName("نوع درخواست")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public ProcessDocumentRequestTypeEnum RequestType { get; set; }

		[DisplayName("نوع سند")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProcessDocumentTypeEnum? DocumentType { get; set; }

		[DisplayName("عنوان سند")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(128)]
		public string DocumentTitle { get; set; } = "";

		[DisplayName("شماره سند")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(20)]
		public string? DocumentNumber { get; set; }

		[DisplayName("سطح دسترسی")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public ProcessDocumentAccessLevelEnum AccessLevel { get; set; }

		[DisplayName("ورودی سند")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public ProcessDocumentSourceEnum Source { get; set; }

		[DisplayName("فوریت")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public ProcessDocumentPriorityEnum Priority { get; set; }

		[DisplayName("مجموعه فرآیندی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProcessDocumentProcessSetEnum? ProcessSet { get; set; }

		[DisplayName("عنوان زیرفرآیند")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(128)]
		public string? SubProcessTitle { get; set; }

		[DisplayName("مالک فرآیند")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ProcessOwnerId))]
		public virtual User? ProcessOwner { get; set; }
		public long? ProcessOwnerId { get; set; }

		[DisplayName("تایید کننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ApproverId))]
		public virtual User? Approver { get; set; }
		public long? ApproverId { get; set; }

		[DisplayName("تصویب کننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(FinalApproverId))]
		public virtual User? FinalApprover { get; set; }
		public long? FinalApproverId { get; set; }

		[DisplayName("تاریخ پیش‌بینی تکمیل")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? CompletionForecastMiladiDate { get; set; }

		[DisplayName("تاریخ پیش‌بینی تکمیل")]
		[DisplayInfo("CompletionForecastMiladiDate", true, type: SystemType.DateShamsi)]
		[MaxLength(30)]
		public string? CompletionForecastShamsiDate { get; set; }

		[DisplayName("تاریخ ابلاغ")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? NotificationMiladiDate { get; set; }

		[DisplayName("تاریخ ابلاغ")]
		[DisplayInfo("NotificationMiladiDate", true, type: SystemType.DateShamsi)]
		[MaxLength(30)]
		public string? NotificationShamsiDate { get; set; }

		[DisplayName("آخرین وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ProcessDocumentStatusEnum? LastStatus { get; set; }

		[DisplayName("کاربر آخرین وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(LastStatusUserId))]
		public virtual User? LastStatusUser { get; set; }
		public long? LastStatusUserId { get; set; }

		[DisplayName("فایل اصلی")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".pdf,.doc,.docx,.xls,.xlsx,.rar,.zip,.jpg,.jpeg,.png", maxFileSize: 20)]
		public virtual FileEntity? MainFile { get; set; }
		public long? MainFileId { get; set; }

		[DisplayName("فایل متفرقه")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".pdf,.doc,.docx,.xls,.xlsx,.rar,.zip,.jpg,.jpeg,.png", maxFileSize: 20)]
		public virtual FileEntity? TempFile { get; set; }
		public long? TempFileId { get; set; }

		[DisplayName("فایل نهایی")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".pdf,.doc,.docx,.xls,.xlsx,.rar,.zip,.jpg,.jpeg,.png", maxFileSize: 20)]
		public virtual FileEntity? FinalFile { get; set; }
		public long? FinalFileId { get; set; }

		[DisplayName("فایل PDF نهایی")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".pdf", maxFileSize: 20)]
		public virtual FileEntity? FinalPdfFile { get; set; }
		public long? FinalPdfFileId { get; set; }

		[DisplayName("فایل کامنت")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".pdf,.doc,.docx,.xls,.xlsx,.rar,.zip,.jpg,.jpeg,.png", maxFileSize: 20)]
		public virtual FileEntity? CommentFile { get; set; }
		public long? CommentFileId { get; set; }

		[DisplayName("اسناد مرتبط")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? RelatedDocumentsText { get; set; }

		[DisplayName("منسوخ")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsDeprecate { get; set; }

		[DisplayName("درخواست‌کننده")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(RequesterUserId))]
		public virtual User? RequesterUser { get; set; }
		public long RequesterUserId { get; set; }

		[DisplayName("واحد ایجادکننده")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(CreatedOrganizationUnitId))]
		public virtual OrgUnit? CreatedOrganizationUnit { get; set; }
		public long CreatedOrganizationUnitId { get; set; }

		[DisplayName("شرح درخواست")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(2048)]
		public string? Comment { get; set; }

		[DisplayName("گردش سند")]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]
		public virtual ICollection<ProcessDocumentStatusLog> StatusLogs { get; set; } = new List<ProcessDocumentStatusLog>();

		[DisplayName("واحدهای ویرایش‌کننده")]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]
		public virtual ICollection<ProcessDocumentEditingOrgUnit> EditingOrgUnits { get; set; } = new List<ProcessDocumentEditingOrgUnit>();

		[DisplayName("واحدهای مجری")]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]
		public virtual ICollection<ProcessDocumentExecuterOrgUnit> ExecuterOrgUnits { get; set; } = new List<ProcessDocumentExecuterOrgUnit>();

		[DisplayName("مجریان فرآیند")]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]
		public virtual ICollection<ProcessDocumentProcessExecuter> ProcessExecuters { get; set; } = new List<ProcessDocumentProcessExecuter>();

		[DisplayName("واحدهای ذینفع")]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]
		public virtual ICollection<ProcessDocumentBeneficiaryOrgUnit> BeneficiaryOrgUnits { get; set; } = new List<ProcessDocumentBeneficiaryOrgUnit>();

		[DisplayName("گیرندگان ابلاغ")]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]
		public virtual ICollection<ProcessDocumentNotificationRecipient> NotificationRecipients { get; set; } = new List<ProcessDocumentNotificationRecipient>();

		[DisplayName("واحدهای مرتبط")]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]
		public virtual ICollection<ProcessDocumentRelatedOrgUnit> RelatedOrgUnits { get; set; } = new List<ProcessDocumentRelatedOrgUnit>();

		/// <summary>
		/// فقط برای بایند فرم (EntitySelector چندانتخابی). ستون CSV در دیتابیس نیست.
		/// </summary>
		[NotMapped]
		public string? EditingOrgUnitIds { get; set; }

		[NotMapped]
		public string? EditingOrgUnitNames { get; set; }

		[NotMapped]
		public string? ExecuterOrgUnitIds { get; set; }

		[NotMapped]
		public string? ExecuterOrgUnitNames { get; set; }

		[NotMapped]
		public string? ProcessExecuterIds { get; set; }

		[NotMapped]
		public string? ProcessExecuterNames { get; set; }

		[NotMapped]
		public string? BeneficiaryOrgUnitIds { get; set; }

		[NotMapped]
		public string? BeneficiaryOrgUnitNames { get; set; }

		[NotMapped]
		public string? NotificationRecipientIds { get; set; }

		[NotMapped]
		public string? NotificationRecipientNames { get; set; }

		[NotMapped]
		public string? RelatedOrgUnitIds { get; set; }

		[NotMapped]
		public string? RelatedOrgUnitNames { get; set; }

		[NotMapped]
		public ProcessDocumentStatusEnum? NextStatus { get; set; }

		[NotMapped]
		public string? StatusLogComment { get; set; }

		[NotMapped]
		public DateTime? StatusLogSendMiladiDate { get; set; }

		[NotMapped]
		public string? StatusLogSendShamsiDate { get; set; }

		[NotMapped]
		public DateTime? StatusLogReceiveMiladiDate { get; set; }

		[NotMapped]
		public string? StatusLogReceiveShamsiDate { get; set; }

		[NotMapped]
		public bool IgnoreProcessAndForceNotification { get; set; }
	}

	public class ProcessDocumentConfiguration : IEntityTypeConfiguration<ProcessDocument>
	{
		public void Configure(EntityTypeBuilder<ProcessDocument> builder)
		{
			builder.HasOne(x => x.Parent)
				.WithMany()
				.HasForeignKey(x => x.ParentId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.HasOne(x => x.ProcessOwner)
				.WithMany()
				.HasForeignKey(x => x.ProcessOwnerId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.HasOne(x => x.Approver)
				.WithMany()
				.HasForeignKey(x => x.ApproverId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.HasOne(x => x.FinalApprover)
				.WithMany()
				.HasForeignKey(x => x.FinalApproverId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.HasOne(x => x.LastStatusUser)
				.WithMany()
				.HasForeignKey(x => x.LastStatusUserId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.HasOne(x => x.RequesterUser)
				.WithMany()
				.HasForeignKey(x => x.RequesterUserId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.HasOne(x => x.CreatedOrganizationUnit)
				.WithMany()
				.HasForeignKey(x => x.CreatedOrganizationUnitId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.HasOne(x => x.MainFile)
				.WithMany()
				.HasForeignKey(x => x.MainFileId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.HasOne(x => x.TempFile)
				.WithMany()
				.HasForeignKey(x => x.TempFileId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.HasOne(x => x.FinalFile)
				.WithMany()
				.HasForeignKey(x => x.FinalFileId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.HasOne(x => x.FinalPdfFile)
				.WithMany()
				.HasForeignKey(x => x.FinalPdfFileId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.HasOne(x => x.CommentFile)
				.WithMany()
				.HasForeignKey(x => x.CommentFileId)
				.OnDelete(DeleteBehavior.NoAction);

			builder.HasMany(x => x.StatusLogs)
				.WithOne(x => x.ProcessDocument)
				.HasForeignKey(x => x.ProcessDocumentId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.HasMany(x => x.EditingOrgUnits)
				.WithOne(x => x.ProcessDocument)
				.HasForeignKey(x => x.ProcessDocumentId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.HasMany(x => x.ExecuterOrgUnits)
				.WithOne(x => x.ProcessDocument)
				.HasForeignKey(x => x.ProcessDocumentId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.HasMany(x => x.ProcessExecuters)
				.WithOne(x => x.ProcessDocument)
				.HasForeignKey(x => x.ProcessDocumentId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.HasMany(x => x.BeneficiaryOrgUnits)
				.WithOne(x => x.ProcessDocument)
				.HasForeignKey(x => x.ProcessDocumentId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.HasMany(x => x.NotificationRecipients)
				.WithOne(x => x.ProcessDocument)
				.HasForeignKey(x => x.ProcessDocumentId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.HasMany(x => x.RelatedOrgUnits)
				.WithOne(x => x.ProcessDocument)
				.HasForeignKey(x => x.ProcessDocumentId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.HasIndex(x => x.HtsId)
				.IsUnique()
				.HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Bpm_ProcessDocument_HtsId");
		}
	}
}
