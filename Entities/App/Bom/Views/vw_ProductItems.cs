using Entities.App.Sale.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.App.Bom.Views
{

	public class vw_ProductItems
	{
		public long ProductFormulId { get; set; }
		public string PardCode { get; set; }
		public string PartName { get; set; }
		public decimal UsingRate { get; set; }
		public long PartId { get; set; }
		public long PartItemId { get; set; }
	}

	public class vw_ProductItemsConfiguration : IEntityTypeConfiguration<vw_ProductItems>
	{
		public void Configure(EntityTypeBuilder<vw_ProductItems> builder)
		{
			builder.ToView("vw_ProductItem", "Bom");
			builder.HasNoKey();
		}
	}
}

 
