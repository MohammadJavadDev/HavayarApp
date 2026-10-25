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
	[Display(Name = "نرم‌افزار رزومه")]
	[Table("ResumeSoftware", Schema = "Hrm")]
	public class ResumeSoftware : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[ForeignKey(nameof(ResumeId))]
		public virtual Resume? Resume { get; set; }

		public long ResumeId { get; set; }

		[DisplayName("نرم‌افزار")]
		[DisplayInfo(null, true, type: SystemType.Entity, showInRelationData: true)]
		[ForeignKey(nameof(SoftwareId))]
		public virtual TrainingSoftware? Software { get; set; }

		public long? SoftwareId { get; set; }

		[DisplayName("سایر نرم‌افزار")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? OtherSoftware { get; set; }

		[DisplayName("سطح آشنایی")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ResumeKnowledgeLevelEnum? KnowledgeLevel { get; set; }

		[DisplayName("نحوه یادگیری")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ResumeSoftwareLearnTypeEnum? LearnType { get; set; }
	}

	public class ResumeSoftwareConfiguration : IEntityTypeConfiguration<ResumeSoftware>
	{
		public void Configure(EntityTypeBuilder<ResumeSoftware> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hrm_ResumeSoftware_HtsId");
			builder.HasOne(x => x.Resume).WithMany(x => x.Softwares).HasForeignKey(x => x.ResumeId).OnDelete(DeleteBehavior.Cascade);
			builder.HasOne(x => x.Software).WithMany().HasForeignKey(x => x.SoftwareId).OnDelete(DeleteBehavior.Restrict);
		}
	}
}
