using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Rahkaran.HCM3
{
	[Table(name: "Department", Schema = "HCM3")]
	public class RahkaranDepartment
	{
		[Key]
		public long DepartmentID { get; set; }

		[MaxLength(100)]
		public string? Code { get; set; }

		[MaxLength(100)]
		public string? UniqueCode { get; set; }

		[Required]
		[MaxLength(200)]
		public string? Title { get; set; }

		[MaxLength(50)]
		public string? AbbrSign { get; set; }

		public int? BusinessDomainCode { get; set; }

		public int? DepartmentTypeCode { get; set; }

		public long? RegionalDivisionRef { get; set; }

	 
		public string? Description { get; set; }

		public int? Status { get; set; }

		public DateTime? StatusChangeDate { get; set; }

		public byte[]? Version { get; set; }

		public DateTime CreationDate { get; set; }

		public long Creator { get; set; }

		public DateTime LastModificationDate { get; set; }

		public long LastModifier { get; set; }

		[MaxLength(50)]
		public string? AbbrName { get; set; }

		public int? Extra1Code { get; set; }

		public int? Extra2Code { get; set; }

		public int? Extra3Code { get; set; }

	}
}
