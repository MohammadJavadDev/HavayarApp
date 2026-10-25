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
	/// <summary>لاگ فرم ورود اضطراری — ردیف‌های HTS <c>Enex_FormLog</c> که <c>EntryFormId</c> دارند.</summary>
	[Display(Name = "لاگ ورود اضطراری")]
	[Table("EnexFormLog", Schema = "Hrm")]
	public class EnexFormLog : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("فرم ورود")]
		[DisplayInfo(null, false, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(EntryFormId))]
		public virtual EnexEntryForm? EntryForm { get; set; }
		public long EntryFormId { get; set; }

		[DisplayName("وضعیت")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public EnexEntryFormStatusEnum StatusId { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class EnexFormLogConfiguration : IEntityTypeConfiguration<EnexFormLog>
	{
		public void Configure(EntityTypeBuilder<EnexFormLog> builder)
		{
			builder.HasOne(x => x.EntryForm).WithMany().HasForeignKey(x => x.EntryFormId).OnDelete(DeleteBehavior.Restrict);
			builder.HasIndex(x => x.EntryFormId).HasDatabaseName("IX_Hrm_EnexFormLog_EntryFormId");
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_EnexFormLog_HtsId");
		}
	}
}
