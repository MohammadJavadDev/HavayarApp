using Common.Attributes;
using Entities.App.Inv;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
	[Display(Name = "قیمت مصوب کالا")]
	[Table("PartPrice", Schema = "Sale")]
	public class PartPrice : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual Part? Part { get; set; }
		public long? PartId { get; set; }

		[DisplayName("قیمت")]
		[DisplayInfo(null, true, type: SystemType.Long, required: true)]
		public long Price { get; set; }

		[DisplayName("ضریب فروش")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? SaleFactor { get; set; }

		[DisplayName("ضریب غیرروتین با حمل")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? NonRoutineWithTransferringFeeSaleFactor { get; set; }

		[DisplayName("ضریب غیرروتین بدون حمل")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? NonRoutineWithoutTransferringFeeSaleFactor { get; set; }
	}

	public class PartPriceConfiguration : IEntityTypeConfiguration<PartPrice>
	{
		public void Configure(EntityTypeBuilder<PartPrice> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_PartPrice_HtsId");
		}
	}
}
