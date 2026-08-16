using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Pln
{
	[Table("Pln_ProductionOrder_Itm_New_Comment")]
	public class Hts_Pln_ProductionOrderItemComment
	{
		[Key]
		public int Id { get; set; }

		public int? ProductionOrderItemId { get; set; }

		public short StatusId { get; set; }

		[Column(TypeName = "date")]
		public DateTime? CommentDate { get; set; }

		[StringLength(10)]
		public string? CommentDateInText { get; set; }

		[Column(TypeName = "date")]
		public DateTime? CommentEndDate { get; set; }

		[StringLength(10)]
		public string? CommentEndDateInText { get; set; }

		[StringLength(5)]
		public string? CommentStartTime { get; set; }

		[StringLength(5)]
		public string? CommentEndTime { get; set; }

		[StringLength(50)]
		public string? FailureTypeIds { get; set; }

		[StringLength(255)]
		public string? FailureTypeInText { get; set; }

		public bool IsForProductionMode { get; set; }

		public short CreatedUserId { get; set; }

		public DateTime CreatedDate { get; set; }

		[StringLength(16)]
		public string? CreatedDateInText { get; set; }

		[StringLength(2400)]
		public string? Comment { get; set; }

		[StringLength(512)]
		public string? CcReciversIds { get; set; }

		[StringLength(2048)]
		public string? CcReciversInText { get; set; }

		public int? StopRequestId { get; set; }

		public bool IsForProductionStepStatus { get; set; }

		/// <summary>Populated via FromSqlRaw join (PO.Number).</summary>
		public int? ProductionOrderNumber { get; set; }

		/// <summary>Populated via FromSqlRaw join (Inv_Part.Part_Code).</summary>
		public string? PartCode { get; set; }

		/// <summary>Populated via FromSqlRaw join (POI.RahkaranId).</summary>
		public long? ItemRahkaranId { get; set; }

		/// <summary>Populated via FromSqlRaw join (Gnr_User.Username).</summary>
		public string? CreatedUserUsername { get; set; }
	}
}
