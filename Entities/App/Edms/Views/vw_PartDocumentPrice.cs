using Entities.App.Inv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.App.Edms.Views
{
	[Keyless]
    public class vw_PartDocumentPrice
    {
		public long Id { get; set; }
		public long? ProductId { get; set; }
		public long? InternalTotalBuyPrice { get; set; }
		public long? ExternalTotalBuyPrice { get; set; }
		public long? BuyPrice { get; set; }
	}

	public class vw_PartDocumentPriceConfiguration : IEntityTypeConfiguration<vw_PartDocumentPrice>
	{
		public void Configure(EntityTypeBuilder<vw_PartDocumentPrice> builder)
		{
			builder.ToView("vw_PartDocumentPrice", "Edms");
			builder.HasNoKey();

		}
	}
}
