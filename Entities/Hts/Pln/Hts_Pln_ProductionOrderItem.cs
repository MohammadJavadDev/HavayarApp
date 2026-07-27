using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Pln
{
	[Table("Pln_ProductionOrderItem")]
	public class Hts_Pln_ProductionOrderItem
	{
		[Key]
		public int Id { get; set; }

		public int ProductionOrderId { get; set; }

		public byte Revision { get; set; }

		public bool IsLatestVersion { get; set; }

		public long PartId { get; set; }

		public short? BuyStatusId { get; set; }

		public short? ProductionStepId { get; set; }

		public short? ProductionStatusId { get; set; }

		public short? StatusId { get; set; }

		public short? SerialTypeId { get; set; }

		public double? PlanningNumber { get; set; }

		[Column(TypeName = "date")]
		public DateTime? ReceivedDate { get; set; }

		[StringLength(10)]
		public string? ReceivedDateInText { get; set; }

		public short? FirstIndustrialChangeUserId { get; set; }

		[Column(TypeName = "date")]
		public DateTime? FirstIndustrialChangeDate { get; set; }

		[StringLength(10)]
		public string? FirstIndustrialChangeDateInText { get; set; }

		[Column(TypeName = "date")]
		public DateTime? DocumentPreparationDate { get; set; }

		[StringLength(10)]
		public string? DocumentPreparationDateInText { get; set; }

		[Column(TypeName = "date")]
		public DateTime? StandardDeliveryDate { get; set; }

		[StringLength(10)]
		public string? StandardDeliveryDateInText { get; set; }

		[StringLength(1024)]
		public string? EngineeringConsideration { get; set; }

		[StringLength(1024)]
		public string? PlanningConsideration { get; set; }

		public bool IsRoutine { get; set; }

		[Column(TypeName = "date")]
		public DateTime? ProductionStartDateInEurope { get; set; }

		[StringLength(10)]
		public string? ProductionStartDate { get; set; }

		[Column(TypeName = "date")]
		public DateTime? ProductionEndDateInEurope { get; set; }

		[StringLength(10)]
		public string? ProductionEndDate { get; set; }

		[Column(TypeName = "date")]
		public DateTime? TestingEndDateInEurope { get; set; }

		[StringLength(10)]
		public string? TestingEndDate { get; set; }

		[Column(TypeName = "date")]
		public DateTime? PreparationDate { get; set; }

		[StringLength(10)]
		public string? PreparationDateInText { get; set; }

		[Column(TypeName = "date")]
		public DateTime? DeliveryDate { get; set; }

		[StringLength(10)]
		public string? DeliveryDateInText { get; set; }

		[StringLength(128)]
		public string? Serial { get; set; }

		public long? RahkaranId { get; set; }

		public bool IsDeleted { get; set; }

		public DateTime? UpdatedDate { get; set; }

		[StringLength(16)]
		public string? UpdatedDateInText { get; set; }

		/// <summary>Populated via FromSqlRaw join (PO.Number).</summary>
		public int? ProductionOrderNumber { get; set; }

		/// <summary>Populated via FromSqlRaw join (Inv_Part.Part_Code).</summary>
		public string? PartCode { get; set; }

		/// <summary>Populated via FromSqlRaw join (PO header flag).</summary>
		public bool? HeaderIsDisableForTimelyDeliveryReport { get; set; }

		/// <summary>Populated via FromSqlRaw join (PO header comment).</summary>
		public string? HeaderDisableForTimelyDeliveryReportComment { get; set; }

		/// <summary>Populated via FromSqlRaw join (Gnr_User.Username).</summary>
		public string? FirstIndustrialChangeUserUsername { get; set; }
	}
}
