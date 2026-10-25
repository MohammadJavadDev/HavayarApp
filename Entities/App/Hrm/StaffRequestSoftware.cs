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
	[Display(Name = "نرم‌افزار درخواست نیرو")]
	[Table("StaffRequestSoftware", Schema = "Hrm")]
	public class StaffRequestSoftware : BaseEntity
	{
		public long HtsId { get; set; }

		[ForeignKey(nameof(StaffRequestId))]
		public virtual StaffRequest? StaffRequest { get; set; }

		public long StaffRequestId { get; set; }

		[DisplayName("نرم‌افزار")]
		[DisplayInfo(null, true, type: SystemType.Entity, showInRelationData: true)]
		[ForeignKey(nameof(SoftwareId))]
		public virtual TrainingSoftware? Software { get; set; }

		public long? SoftwareId { get; set; }

		[DisplayName("سطح")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ResumeKnowledgeLevelEnum? KnowledgeLevel { get; set; }
	}

	public class StaffRequestSoftwareConfiguration : IEntityTypeConfiguration<StaffRequestSoftware>
	{
		public void Configure(EntityTypeBuilder<StaffRequestSoftware> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)").HasDatabaseName("IX_Hrm_StaffRequestSoftware_HtsId");
			builder.HasOne(x => x.StaffRequest).WithMany(x => x.Softwares).HasForeignKey(x => x.StaffRequestId).OnDelete(DeleteBehavior.Cascade);
			builder.HasOne(x => x.Software).WithMany().HasForeignKey(x => x.SoftwareId).OnDelete(DeleteBehavior.Restrict);
		}
	}
}
