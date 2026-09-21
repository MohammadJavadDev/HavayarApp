using Common.Attributes;
using Entities.App.Crm.Enums;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Crm
{
	[Display(Name = "منطقه خدمات پس از فروش")]
	[Table("Zone", Schema = "Crm")]
	public class Zone : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(400)]
		public string Title { get; set; } = "";

		[DisplayName("مسئول منطقه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(SupervisorUserId))]
		public virtual User? Supervisor { get; set; }
		public long? SupervisorUserId { get; set; }

		[DisplayName("نوع منطقه")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public ZoneTypeEnum? ZoneType { get; set; }
	}

	public class ZoneConfiguration : IEntityTypeConfiguration<Zone>
	{
		public void Configure(EntityTypeBuilder<Zone> builder)
		{
			builder.HasOne(x => x.Supervisor)
				.WithMany()
				.HasForeignKey(x => x.SupervisorUserId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.HtsId)
				.IsUnique()
				.HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Crm_Zone_HtsId");
		}
	}
}
