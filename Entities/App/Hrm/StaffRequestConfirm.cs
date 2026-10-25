using Common.Attributes;
using Entities.App.Hcm;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	[Display(Name = "تایید درخواست نیرو")]
	[Table("StaffRequestConfirm", Schema = "Hrm")]
	public class StaffRequestConfirm : BaseEntity
	{
		public long HtsId { get; set; }

		[ForeignKey(nameof(StaffRequestId))]
		public virtual StaffRequest? StaffRequest { get; set; }

		public long StaffRequestId { get; set; }

		[DisplayName("تاییدکننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(ConfirmedPersonelId))]
		public virtual Personel? ConfirmedPersonel { get; set; }

		public long? ConfirmedPersonelId { get; set; }

		public long? HtsConfirmedPersonelId { get; set; }

		[DisplayName("وضعیت تایید")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool ConfirmStatus { get; set; }

		[DisplayName("توضیح")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1600)]
		public string? Comment { get; set; }
	}

	public class StaffRequestConfirmConfiguration : IEntityTypeConfiguration<StaffRequestConfirm>
	{
		public void Configure(EntityTypeBuilder<StaffRequestConfirm> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)").HasDatabaseName("IX_Hrm_StaffRequestConfirm_HtsId");
			builder.HasOne(x => x.StaffRequest).WithMany(x => x.Confirms).HasForeignKey(x => x.StaffRequestId).OnDelete(DeleteBehavior.Cascade);
			builder.HasOne(x => x.ConfirmedPersonel).WithMany().HasForeignKey(x => x.ConfirmedPersonelId).OnDelete(DeleteBehavior.Restrict);
		}
	}
}
