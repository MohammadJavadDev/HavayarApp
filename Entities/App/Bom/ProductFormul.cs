using Common.Attributes;
using Entities.App.Inv;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Bom
{
	[Display(Name = "Bom محصول")]
	[Table("ProductFormul", Schema = "Bom")]
	public class ProductFormul : BaseEntity
	{
		[DisplayName("محصول")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true, showInRelationData: true)]
		public Part Product { get; set; }

		public long ProductId { get; set; }

		 

		[DisplayName("گروه محصول")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public ProductGroup? ProductNameGroup { get; set; }

		public long? ProductNameGroupId { get; set; }

 

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string Comment { get; set; }


		[DisplayName("افلام")]
		[DisplayInfo(null, false, type: SystemType.ListEntity)]
		public List<ProductFormulItem> Items { get; set; } = new();


	}

	[Display(Name = "اقلام Bom محصول")]
	[Table("ProductFormulItem", Schema = "Bom")]
	public class ProductFormulItem : BaseEntity
	{
		public long ProductFormulId { get; set; }
		public ProductFormul ProductFormul { get; set; }

		[DisplayName("قطعه")]
		[DisplayInfo(null, false, type: SystemType.Entity, required: true)]
		public Part Part { get; set; }

		public long PartId { get; set; }

		[DisplayName("ضریب مصرفی")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]
		public decimal? UsingRate { get; set; }


	}

	public class ProductFormulItemConfiguration : IEntityTypeConfiguration<ProductFormulItem>
	{
		public void Configure(EntityTypeBuilder<ProductFormulItem> builder)
		{
			builder.HasOne(x => x.Part)
				.WithMany()
				.HasForeignKey(x => x.PartId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}

}
