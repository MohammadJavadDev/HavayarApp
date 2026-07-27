namespace Entities.App.Sale.DTO
{
	public record SalePrefactorPartDto
	{
		public int OrderDetailId { get; set; }

		public string VchDate { get; set; }

		public long PartRef { get; set; }

		public string PartName { get; set; }

		public decimal Qty { get; set; }

		public int BranchFk { get; set; }

		public decimal LastBuyPrice { get; set; }
		public long PartId { get; set; }
	}

}
