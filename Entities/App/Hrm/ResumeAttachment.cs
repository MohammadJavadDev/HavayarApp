using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	[Display(Name = "پیوست رزومه")]
	[Table("ResumeAttachment", Schema = "Hrm")]
	public class ResumeAttachment : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[ForeignKey(nameof(ResumeId))]
		public virtual Resume? Resume { get; set; }

		public long ResumeId { get; set; }

		[DisplayName("نام فایل")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(400)]
		public string? FileName { get; set; }

		[DisplayName("فایل")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".pdf,.zip,.png,.jpg,.jpeg,.doc,.docx", maxFileSize: 20)]
		[ForeignKey(nameof(FileId))]
		public virtual FileEntity? File { get; set; }

		public long? FileId { get; set; }

		[DisplayName("مسیر فایل")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? AttachmentFilePath { get; set; }

		[DisplayName("حجم")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? FileSize { get; set; }

		[DisplayName("توضیح")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2400)]
		public string? Comment { get; set; }

		[DisplayName("تاریخ")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(10)]
		public string? CreatedShamsiDate { get; set; }

		[DisplayName("ساعت")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(5)]
		public string? CreatedTime { get; set; }

		[DisplayName("شناسه کاربر HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsCreatedUserId { get; set; }
	}

	public class ResumeAttachmentConfiguration : IEntityTypeConfiguration<ResumeAttachment>
	{
		public void Configure(EntityTypeBuilder<ResumeAttachment> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_ResumeAttachment_HtsId");
			builder.HasOne(x => x.Resume).WithMany(x => x.Attachments).HasForeignKey(x => x.ResumeId).OnDelete(DeleteBehavior.Cascade);
			builder.HasOne(x => x.File).WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
		}
	}
}
