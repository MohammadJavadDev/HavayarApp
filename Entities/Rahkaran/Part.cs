using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Rahkaran
{
	[Table(name: "Part", Schema = "LGS3")]
	public class RahkaranPart
	{
		[Key]
		public long PartID { get; set; }

		[Required]
		[MaxLength(64)]
		public string? Code { get; set; }

		[Required]
		[MaxLength(256)]
		public string? Name { get; set; }

		[MaxLength(256)]
		public string? LatinName { get; set; }

		[MaxLength(256)]
		public string? TechnicalSpecification { get; set; }

		public int State { get; set; }

		public decimal? Height { get; set; }

		public decimal? Weight { get; set; }

		public decimal? Length { get; set; }

		public decimal? Width { get; set; }

		public long? ProducerRef { get; set; }

		public bool? IsInputDocumentSuspended { get; set; }

		public bool? IsOutputDocumentSuspended { get; set; }

		[MaxLength(2000)]
		public string? PropertiesComment { get; set; }

		public byte[]? Version { get; set; }

		public int? PartType { get; set; }

		public int? QuantityControlType { get; set; }

		public long? CodeTemplateRef { get; set; }

		public long? CategoryRef { get; set; }

		public int? PartNature { get; set; }

		public long? MajorUnitRef { get; set; }

		public long? MajorHomogeneousPartRef { get; set; }

		[MaxLength(50)]
		public string? CodeTemplateName { get; set; }

		public int? SecondType { get; set; }

		public DateTime? CreationDate { get; set; }

		public DateTime? LastModificationDate { get; set; }

		public long? Creator { get; set; }

		public long? LastModifier { get; set; }

		public int? ReservationLevel { get; set; }

		public bool? ReserveWithTrackingFactor { get; set; }

		public bool? IsTaxFree { get; set; }

		[MaxLength(2000)]
		public string? AdditionalField1 { get; set; }

		[MaxLength(2000)]
		public string? AdditionalField2 { get; set; }

		[MaxLength(13)]
		public string? TaxId { get; set; }

		[MaxLength(2000)]
		public string? TaxTitle { get; set; }

	}
}
