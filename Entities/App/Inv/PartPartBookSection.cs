using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Inv
{
	[Display(Name = "دفترچه قطعات")]
	[Table("PartPartBookSection", Schema = "Inv")]
	public class PartPartBookSection : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("محصول")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public Part? Part { get; set; }
		public long PartId { get; set; }

		[DisplayName("دسته")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public PartBookSection? PartBookSection { get; set; }
		public long PartBookSectionId { get; set; }

		[DisplayName("ترتیب اتصال")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Order { get; set; }
	}

	public class PartPartBookSectionConfiguration : IEntityTypeConfiguration<PartPartBookSection>
	{
		public void Configure(EntityTypeBuilder<PartPartBookSection> builder)
		{
			builder.HasIndex(x => x.HtsId)
				.IsUnique()
				.HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Inv_PartPartBookSection_HtsId");

			builder.HasIndex(x => new { x.PartId, x.PartBookSectionId })
				.IsUnique()
				.HasDatabaseName("IX_Inv_PartPartBookSection_Part_Section");

			builder.HasOne(x => x.Part)
				.WithMany()
				.HasForeignKey(x => x.PartId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.PartBookSection)
				.WithMany(x => x.ProductLinks)
				.HasForeignKey(x => x.PartBookSectionId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
