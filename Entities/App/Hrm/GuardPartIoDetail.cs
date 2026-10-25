using Common.Attributes;
using Entities.App.Hrm.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>اقلام ورود و خروج کالا (حراست/دژبان) — معادل HTS <c>HRM_Guard_PartIODetail</c>.</summary>
	[Display(Name = "اقلام ورود و خروج کالا (حراست/دژبان)")]
	[Table("GuardPartIoDetail", Schema = "Hrm")]
	public class GuardPartIoDetail : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("ورود/خروج کالا")]
		[DisplayInfo(null, false, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(PartIoId))]
		public virtual GuardPartIo? PartIo { get; set; }
		public long PartIoId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(PartId))]
		public virtual GuardPart? Part { get; set; }
		public long PartId { get; set; }

		[DisplayName("مقدار")]
		[DisplayInfo(null, true, type: SystemType.Decimal, required: true)]
		[Column(TypeName = "decimal(18,2)")]
		public decimal Mount { get; set; }

		[DisplayName("علت")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public GuardIoReasonEnum IoReason { get; set; }

		[DisplayName("شماره برگه")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? PaperNumber { get; set; }

		[DisplayName("واحد مرتبط")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(RelatedUnitId))]
		public virtual Entities.App.Hrm.OrgUnit? RelatedUnit { get; set; }
		public long? RelatedUnitId { get; set; }

		[DisplayName("فایل پیوست")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".pdf,.zip,.rar,.png,.jpg,.jpeg,.doc,.docx,.xls,.xlsx,.txt", maxFileSize: 20)]
		[ForeignKey(nameof(FileId))]
		public virtual FileEntity? File { get; set; }
		public long? FileId { get; set; }

		/// <summary>مسیر فایل در HTS (AttachmentFilePath) — فقط منبع جاب انتقال یک‌باره فایل؛ بعد از انتقال دست‌نخورده می‌ماند.</summary>
		[DisplayName("مسیر پیوست HTS")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? HtsAttachmentPath { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class GuardPartIoDetailConfiguration : IEntityTypeConfiguration<GuardPartIoDetail>
	{
		public void Configure(EntityTypeBuilder<GuardPartIoDetail> builder)
		{
			builder.HasOne(x => x.PartIo).WithMany().HasForeignKey(x => x.PartIoId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Part).WithMany().HasForeignKey(x => x.PartId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.RelatedUnit).WithMany().HasForeignKey(x => x.RelatedUnitId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.File).WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.PartIoId).HasDatabaseName("IX_Hrm_GuardPartIoDetail_PartIoId");
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_GuardPartIoDetail_HtsId");
		}
	}
}
