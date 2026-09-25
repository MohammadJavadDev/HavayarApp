using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Inv
{
	[Display(Name = "BOM سفارشی دفترچه قطعات")]
	[Table("PartCustomBom", Schema = "Inv")]
	public class PartCustomBom : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("محصول")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public Part? Product { get; set; }
		public long ProductId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public Part? Part { get; set; }
		public long PartId { get; set; }

		[DisplayName("مقدار مصرف")]
		[DisplayInfo(null, true, type: SystemType.Decimal, required: true)]
		public decimal UsingRate { get; set; }

		[DisplayName("ترتیب")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int Order { get; set; }

		[DisplayName("غیرفعال")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsDisabled { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Comment { get; set; }
	}

	public class PartCustomBomConfiguration : IEntityTypeConfiguration<PartCustomBom>
	{
		public void Configure(EntityTypeBuilder<PartCustomBom> builder)
		{
			builder.HasIndex(x => x.HtsId)
				.IsUnique()
				.HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Inv_PartCustomBom_HtsId");

			builder.HasOne(x => x.Product)
				.WithMany()
				.HasForeignKey(x => x.ProductId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.Part)
				.WithMany()
				.HasForeignKey(x => x.PartId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Property(x => x.UsingRate).HasPrecision(18, 4);
		}
	}
}
