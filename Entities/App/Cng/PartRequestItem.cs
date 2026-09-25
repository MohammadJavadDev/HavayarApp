using Common.Attributes;
using Entities.App.Cng.Enums;
using Entities.App.Inv;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Cng
{
	/// <summary>قلم درخواست کالا CNG — معادل HTS <c>Cng_PartRequestItem</c>.</summary>
	[Display(Name = "اقلام درخواست کالا")]
	[Table("PartRequestItem", Schema = "Cng")]
	public class PartRequestItem : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("درخواست کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(PartRequestId))]
		public virtual PartRequest? PartRequest { get; set; }
		public long PartRequestId { get; set; }

		[DisplayName("محل مصرف")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public PartRequestUsagePlaceEnum UsagePlaceId { get; set; }

		[DisplayName("جایگاه")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		[ForeignKey(nameof(StationId))]
		public virtual StationInfo? Station { get; set; }
		public long? StationId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(PartId))]
		public virtual Part? Part { get; set; }
		public long PartId { get; set; }

		[DisplayName("تعداد")]
		[DisplayInfo(null, true, type: SystemType.Decimal, required: true)]
		public decimal Mount { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? Comment { get; set; }
	}

	public class PartRequestItemConfiguration : IEntityTypeConfiguration<PartRequestItem>
	{
		public void Configure(EntityTypeBuilder<PartRequestItem> builder)
		{
			builder.HasOne(x => x.PartRequest)
				.WithMany()
				.HasForeignKey(x => x.PartRequestId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.Station)
				.WithMany()
				.HasForeignKey(x => x.StationId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.Part)
				.WithMany()
				.HasForeignKey(x => x.PartId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Property(x => x.Mount).HasPrecision(18, 4);

			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Cng_PartRequestItem_HtsId");
		}
	}
}
