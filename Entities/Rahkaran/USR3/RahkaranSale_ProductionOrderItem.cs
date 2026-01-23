using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Rahkaran.USR3
{
	[Table(name: "Sale_ProductionOrderItem", Schema = "USR3")]
	public class RahkaranSale_ProductionOrderItem
	{
		[Key]
		public long Sale_ProductionOrderItemID { get; set; }

		public long _MasterRef { get; set; }

		public bool? IsBuildInside { get; set; }

		public int? DeviceTypeId { get; set; }

		public int? EquipmentTypeId { get; set; }

		public long? PartIdRef { get; set; }

		public decimal? Amount { get; set; }

		public DateTime? AgreedDeliverDate { get; set; }

		[MaxLength(128)]
		public string? PartModel { get; set; }

		[MaxLength(128)]
		public string? AirendOrCategory { get; set; }

		public decimal? InputPressureBar { get; set; }

		public decimal? OutputPressureBar { get; set; }

		[MaxLength(50)]
		public string? Capacity { get; set; }

		[MaxLength(50)]
		public string? Scale { get; set; }

		[MaxLength(50)]
		public string? GasType { get; set; }

		public decimal? MaximumTemperature { get; set; }

		public decimal? PurityPercentage { get; set; }

		public decimal? RelativeHumidity { get; set; }

		public decimal? BarometricPressure { get; set; }

		[MaxLength(128)]
		public string? MovingType { get; set; }

		public bool? HasInspection { get; set; }

		[MaxLength(2048)]
		public string? SalesConsideration { get; set; }

		public decimal? Revision { get; set; }

		public bool? IsLatestVersion { get; set; }

		public int? CheckStatusId { get; set; }

		public long Creator { get; set; }

		public DateTime CreationDate { get; set; }

		public long LastModifier { get; set; }

		public DateTime LastModificationDate { get; set; }

		public byte[]? Version { get; set; }

		[MaxLength(20)]
		public string? PartCode { get; set; }

	}
}
