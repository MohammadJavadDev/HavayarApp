using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sale
{
	[Display(Name = "تنظیمات قیمت فروش")]
	[Table("PriceConfig", Schema = "Sale")]
	public class PriceConfig : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(400)]
		public string Title { get; set; } = "";

		[DisplayName("سود")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public byte Profit { get; set; }

		[DisplayName("سربار")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public byte Overhead { get; set; }

		[DisplayName("حمل")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public byte Shipping { get; set; }

		[DisplayName("گمرک")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public byte Gomrok { get; set; }
	}

	public class PriceConfigConfiguration : IEntityTypeConfiguration<PriceConfig>
	{
		public void Configure(EntityTypeBuilder<PriceConfig> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Sale_PriceConfig_HtsId");
		}
	}
}
