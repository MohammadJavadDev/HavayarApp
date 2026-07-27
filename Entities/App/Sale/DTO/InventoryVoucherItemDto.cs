using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.App.Sale.DTO
{
	public record InventoryVoucherItemDto
	{
		public bool RowSelector { get; set; }

		public long VchItmID { get; set; }

		public string? VchDate { get; set; }

		public long PartRef { get; set; }

		public string? PartName { get; set; }

		public long? MunitRef { get; set; }

		public decimal Qty { get; set; }

		public decimal? LastBuyPrice { get; set; }
	}

}
