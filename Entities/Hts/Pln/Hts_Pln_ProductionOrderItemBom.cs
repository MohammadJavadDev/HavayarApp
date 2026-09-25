using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Hts.Pln
{
	[Table("Pln_ProductionOrderItemBom")]
	public class Hts_Pln_ProductionOrderItemBom
    {
		public int Id { get; set; }

		public int ProductionOrderItemId { get; set; }
		public string? ProductionOrderItemPartCode { get; set; }
		public string? ProductionOrderItemBomPartCode { get; set; }
		public byte? ProductionOrderItemRevision { get; set; }
		public byte? ProductionOrderRevision { get; set; }
		public int ProductionOrderNumber { get; set; }

		/// <summary>
		/// HTS Pln_ProductionOrderItem.RahkaranId (= App Sale.ProductionOrderItem.RahkaranHistoryId).
		/// Filled only by sync FromSqlRaw projections — not a physical column on Pln_ProductionOrderItemBom.
		/// </summary>
		public long? ItemRahkaranHistoryId { get; set; }

		public short? Revision { get; set; }

		public long? PartId { get; set; }

		public decimal? Amount { get; set; }

		public bool? IsLatest { get; set; }

		public bool? HasNeedToBuyOrBuild { get; set; }

		public short? CreatedUserId { get; set; }

		public DateTime? CreatedDate { get; set; }

		[Required]
		[StringLength(16)]
		public string? CreatedDateInText { get; set; }

		public short? UpdatedUserId { get; set; }

		public DateTime? UpdatedDate { get; set; }

		[Required]
		[StringLength(16)]
		public string? UpdatedDateInText { get; set; }

		[StringLength(1024)]
		public string? Comment { get; set; }

		[StringLength(2048)]
		public string? SalesUnitComment { get; set; }

		public bool? HasNeedToAvl { get; set; }
	}
}
