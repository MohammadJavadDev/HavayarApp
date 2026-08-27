using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Bom;

[Display(Name = "تاریخچه روزانه قیمت محصول")]
[Table("ProductPriceHistory", Schema = "Bom")]
public class ProductPriceHistory : BaseEntity
{
	[DisplayName("فرمول محصول")]
	public long ProductFormulId { get; set; }

	public ProductFormul? ProductFormul { get; set; }

	[DisplayName("محصول")]
	public long ProductId { get; set; }

	[DisplayName("کد محصول")]
	[MaxLength(450)]
	public string? PartCode { get; set; }

	[DisplayName("نام محصول")]
	public string? PartName { get; set; }

	[DisplayName("تاریخ snapshot")]
	[Column(TypeName = "date")]
	public DateTime SnapshotDate { get; set; }

	[DisplayName("تاریخ شمسی snapshot")]
	[MaxLength(10)]
	public string SnapshotShamsiDate { get; set; } = string.Empty;

	[Column(TypeName = "decimal(18,2)")]
	public decimal CurrencyRate { get; set; }

	[Column(TypeName = "decimal(28,5)")]
	public decimal InternalBuyPrice { get; set; }

	[Column(TypeName = "decimal(28,5)")]
	public decimal ExternalBuyPrice { get; set; }

	[Column(TypeName = "decimal(28,5)")]
	public decimal BuyPrice { get; set; }

	[Column(TypeName = "decimal(28,5)")]
	public decimal BuyPriceInEuro { get; set; }

	[Column(TypeName = "decimal(28,5)")]
	public decimal SalePrice { get; set; }

	public int ComponentCount { get; set; }

	public int PricedComponentCount { get; set; }

	public DateTime? EmailSentOnMiladiDateTime { get; set; }

	[MaxLength(30)]
	public string? EmailSentOnShamsiDateTime { get; set; }
}

public sealed class ProductPriceHistoryConfiguration : IEntityTypeConfiguration<ProductPriceHistory>
{
	public void Configure(EntityTypeBuilder<ProductPriceHistory> builder)
	{
		builder.HasIndex(x => new { x.SnapshotDate, x.ProductFormulId }).IsUnique();
		builder.HasIndex(x => new { x.ProductFormulId, x.SnapshotDate });
		builder.HasIndex(x => x.ProductId);

		builder.HasOne(x => x.ProductFormul)
			.WithMany()
			.HasForeignKey(x => x.ProductFormulId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
