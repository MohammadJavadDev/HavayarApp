using Common.Attributes;
using Entities.App.Hcm;
using Entities.App.Hrm.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>ورود/خروج پرسنل/پیمانکار — معادل HTS <c>HRM_Guard_PersonIO</c> (صفحه ۴۲۷).</summary>
	[Display(Name = "ورود/خروج پرسنل/پیمانکار")]
	[Table("GuardPersonIo", Schema = "Hrm")]
	public class GuardPersonIo : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("نوع")]
		[DisplayInfo(null, true, type: SystemType.Select, required: true)]
		public GuardOperatorTypeEnum OperatorType { get; set; }

		[DisplayName("پرسنل")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(PersonId))]
		public virtual Personel? Person { get; set; }
		public long? PersonId { get; set; }

		[DisplayName("واحد")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(UnitId))]
		public virtual OrgUnit? Unit { get; set; }
		public long? UnitId { get; set; }

		[DisplayName("پیمانکار")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ContractorId))]
		public virtual GuardContractor? Contractor { get; set; }
		public long? ContractorId { get; set; }

		[DisplayName("تاریخ ورود میلادی")]
		[Column(TypeName = "date")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? EntryMiladiDate { get; set; }

		[DisplayName("تاریخ ورود")]
		[MaxLength(16)]
		[DisplayInfo(nameof(EntryMiladiDate), true, type: SystemType.DateShamsi)]
		public string? EntryShamsiDate { get; set; }

		[DisplayName("ساعت ورود")]
		[MaxLength(5)]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? EntryTime { get; set; }

		[DisplayName("تاریخ خروج میلادی")]
		[Column(TypeName = "date")]
		[DisplayInfo(null, false, type: SystemType.Date)]
		public DateTime? ExitMiladiDate { get; set; }

		[DisplayName("تاریخ خروج")]
		[MaxLength(16)]
		[DisplayInfo(nameof(ExitMiladiDate), true, type: SystemType.DateShamsi)]
		public string? ExitShamsiDate { get; set; }

		[DisplayName("ساعت خروج")]
		[MaxLength(5)]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? ExitTime { get; set; }

		[DisplayName("تصویر ورود پرسنل/ پیمانکار")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".jpg,.png,.jpeg,.rar,.zip,.pdf,.doc,.docx", maxFileSize: 5)]
		[ForeignKey(nameof(EntryFileId))]
		public virtual FileEntity? EntryFile { get; set; }
		public long? EntryFileId { get; set; }

		[DisplayName("تصویر خروج پرسنل/ پیمانکار")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".jpg,.png,.jpeg,.rar,.zip,.pdf,.doc,.docx", maxFileSize: 5)]
		[ForeignKey(nameof(ExitFileId))]
		public virtual FileEntity? ExitFile { get; set; }
		public long? ExitFileId { get; set; }

		/// <summary>مسیر فایل ورود در HTS — فقط منبع جاب انتقال یک‌باره؛ بعد از انتقال دست‌نخورده می‌ماند.</summary>
		[DisplayName("مسیر تصویر ورود HTS")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1024)]
		public string? HtsEntryFilePath { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class GuardPersonIoConfiguration : IEntityTypeConfiguration<GuardPersonIo>
	{
		public void Configure(EntityTypeBuilder<GuardPersonIo> builder)
		{
			builder.HasOne(x => x.Person).WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.Contractor).WithMany().HasForeignKey(x => x.ContractorId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.EntryFile).WithMany().HasForeignKey(x => x.EntryFileId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.ExitFile).WithMany().HasForeignKey(x => x.ExitFileId).OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_GuardPersonIo_HtsId");
		}
	}
}
