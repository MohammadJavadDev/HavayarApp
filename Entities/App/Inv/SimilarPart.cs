using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Inv
{
	[Display(Name = "کالای مشابه")]
	[Table("SimilarPart", Schema = "Inv")]
	public class SimilarPart : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("کالای پایه")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public Part? BasePart { get; set; }
		public long BasePartId { get; set; }

		[DisplayName("کالای مشابه")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public Part? Similar { get; set; }
		public long SimilarPartId { get; set; }

		[DisplayName("نسبت")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal Ratio { get; set; }

		[DisplayName("اولویت")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short Priority { get; set; }

		[DisplayName("شناسه همکاران")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HamkaranSimilarPartId { get; set; }

		[DisplayName("شناسه راهکاران")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? RahkaranId { get; set; }
	}

	public class SimilarPartConfiguration : IEntityTypeConfiguration<SimilarPart>
	{
		public void Configure(EntityTypeBuilder<SimilarPart> builder)
		{
			builder.HasIndex(x => x.HtsId)
				.IsUnique()
				.HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Inv_SimilarPart_HtsId");

			builder.HasOne(x => x.BasePart)
				.WithMany()
				.HasForeignKey(x => x.BasePartId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.Similar)
				.WithMany()
				.HasForeignKey(x => x.SimilarPartId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Property(x => x.Ratio).HasPrecision(18, 4);
		}
	}
}
