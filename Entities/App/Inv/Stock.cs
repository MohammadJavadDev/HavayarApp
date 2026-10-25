using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Inv
{
	/// <summary>
	/// انبار — معادل HTS <c>dbo.Inv_Stock</c>.
	/// <c>HamkaranId</c> همان <c>Hamkaran_Stock_FK</c> (Store راهکاران) و <c>HtsId</c> همان <c>Stock_ID</c> است.
	/// </summary>
	[Display(Name = "انبار")]
	[Table("Stock", Schema = "Inv")]
	public class Stock : BaseEntity
	{
		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(512)]
		public string Title { get; set; } = string.Empty;

		[DisplayName("شناسه راهکاران")]
		[DisplayInfo(null, true, type: SystemType.Long, required: true)]
		public long HamkaranId { get; set; }

		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int HtsId { get; set; }
	}

	public class StockConfiguration : IEntityTypeConfiguration<Stock>
	{
		public void Configure(EntityTypeBuilder<Stock> builder)
		{
			builder.Property(x => x.Title).IsRequired();
			builder.HasIndex(x => x.HamkaranId)
				.IsUnique()
				.HasDatabaseName("IX_Inv_Stock_HamkaranId");
			builder.HasIndex(x => x.HtsId)
				.IsUnique()
				.HasFilter("[HtsId] <> 0")
				.HasDatabaseName("IX_Inv_Stock_HtsId");
		}
	}
}
