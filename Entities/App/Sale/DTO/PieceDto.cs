namespace Entities.App.Sale.DTO
{
	public record PieceDto
	{
		public long Id { get; set; }

		public string Date { get; set; } = string.Empty;

		public string PartName { get; set; } = string.Empty;

		public decimal Qty { get; set; }

		public long PartRef { get; set; }

		public long? MunitRef { get; set; }

		public decimal? LastBuyPrice { get; set; }
	}

}
