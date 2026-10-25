using Common.Attributes;
using Entities.App.Inv;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>قلم فرم ورود اضطراری — ردیف‌های HTS <c>Enex_FormPart</c> که <c>EntryFormId</c> دارند.</summary>
	[Display(Name = "قلم ورود اضطراری")]
	[Table("EnexFormPart", Schema = "Hrm")]
	public class EnexFormPart : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("فرم ورود")]
		[DisplayInfo(null, false, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(EntryFormId))]
		public virtual EnexEntryForm? EntryForm { get; set; }
		public long EntryFormId { get; set; }

		[DisplayName("فرم خروج")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(ExitFormId))]
		public virtual EnexExitForm? ExitForm { get; set; }
		public long? ExitFormId { get; set; }

		[DisplayName("قطعه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(EnexPartId))]
		public virtual EnexPart? EnexPart { get; set; }
		public long? EnexPartId { get; set; }

		[DisplayName("کالای انبار")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(PartId))]
		public virtual Part? Part { get; set; }
		public long? PartId { get; set; }

		[DisplayName("مقدار خروج")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		[Column(TypeName = "decimal(18,4)")]
		public decimal Mount { get; set; }

		[DisplayName("مقدار ورود")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		[Column(TypeName = "decimal(18,4)")]
		public decimal? EnterAmount { get; set; }

		[DisplayName("عدم تایید حراست")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool? IsSecurityDisApproved { get; set; }

		[DisplayName("تاریخ بازگشت")]
		[DisplayInfo("ReturnMiladiDate", true, type: SystemType.DateShamsi)]
		[MaxLength(16)]
		public string? ReturnShamsiDate { get; set; }

		[Column(TypeName = "date")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? ReturnMiladiDate { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }

		[DisplayName("فایل پیوست")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".pdf,.zip,.rar,.png,.jpg,.jpeg,.doc,.docx,.xls,.xlsx,.txt", maxFileSize: 20)]
		[ForeignKey(nameof(FileId))]
		public virtual FileEntity? File { get; set; }
		public long? FileId { get; set; }

		/// <summary>نام فایل در HTS وقتی محتوا داخل جدول بوده — جاب انتقال با همین پرچم سراغ TotalSystem می‌رود.</summary>
		[DisplayName("نام پیوست HTS")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? HtsAttachmentFileName { get; set; }
	}

	public class EnexFormPartConfiguration : IEntityTypeConfiguration<EnexFormPart>
	{
		public void Configure(EntityTypeBuilder<EnexFormPart> builder)
		{
			builder.HasOne(x => x.EntryForm).WithMany().HasForeignKey(x => x.EntryFormId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.ExitForm).WithMany().HasForeignKey(x => x.ExitFormId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.EnexPart).WithMany().HasForeignKey(x => x.EnexPartId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Part).WithMany().HasForeignKey(x => x.PartId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.File).WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
			builder.HasIndex(x => x.EntryFormId).HasDatabaseName("IX_Hrm_EnexFormPart_EntryFormId");
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_EnexFormPart_HtsId");
		}
	}
}
