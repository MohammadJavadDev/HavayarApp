using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Cng
{
	/// <summary>پارامتر فنی / خطای گزارش کار CNG — معادل HTS <c>Cng_WorkReportFailure</c>.
	/// ردیف‌های بدون مقدار = خطا (FailureInfo.Group خالی)؛ با مقدار/گروه = پارامتر فنی.</summary>
	[Display(Name = "پارامترهای فنی گزارش کار")]
	[Table("WorkReportFailure", Schema = "Cng")]
	public class WorkReportFailure : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("گزارش کار")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(WorkReportId))]
		public virtual WorkReport? WorkReport { get; set; }
		public long WorkReportId { get; set; }

		[DisplayName("پارامتر فنی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(FailureInfoId))]
		public virtual FailureInfo? FailureInfo { get; set; }
		public long FailureInfoId { get; set; }

		[DisplayName("مقدار")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? Value { get; set; }

		[DisplayName("ست‌پوینت")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? SetPointValue { get; set; }

		[DisplayName("توضیح")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class WorkReportFailureConfiguration : IEntityTypeConfiguration<WorkReportFailure>
	{
		public void Configure(EntityTypeBuilder<WorkReportFailure> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Cng_WorkReportFailure_HtsId");

			builder.HasOne(x => x.WorkReport)
				.WithMany()
				.HasForeignKey(x => x.WorkReportId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.FailureInfo)
				.WithMany()
				.HasForeignKey(x => x.FailureInfoId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Property(x => x.Value).HasPrecision(18, 4);
			builder.Property(x => x.SetPointValue).HasPrecision(18, 4);
		}
	}
}
