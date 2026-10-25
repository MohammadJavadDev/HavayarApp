using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>گرایش رشته تحصیلی — معادل HTS <c>HRM_TrendOfStudyCourse</c>. صفحه جدا در منو ندارد.</summary>
	[Display(Name = "گرایش رشته تحصیلی")]
	[Table("TrendOfStudyCourse", Schema = "Hrm")]
	public class TrendOfStudyCourse : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("رشته تحصیلی")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long StudyCourseId { get; set; }

		[DisplayName("رشته تحصیلی")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		[ForeignKey(nameof(StudyCourseId))]
		public virtual StudyCourse? StudyCourse { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(100)]
		public string? Title { get; set; }
	}

	public class TrendOfStudyCourseConfiguration : IEntityTypeConfiguration<TrendOfStudyCourse>
	{
		public void Configure(EntityTypeBuilder<TrendOfStudyCourse> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_TrendOfStudyCourse_HtsId");
			builder.HasOne(x => x.StudyCourse)
				.WithMany()
				.HasForeignKey(x => x.StudyCourseId)
				.OnDelete(DeleteBehavior.Restrict);
			builder.HasIndex(x => x.StudyCourseId)
				.HasDatabaseName("IX_Hrm_TrendOfStudyCourse_StudyCourseId");
		}
	}
}
