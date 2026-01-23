using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Rahkaran.FIN3
{
	[Table(name: "DL", Schema = "FIN3")]
	public class RahkaranDL
	{
		[Key]
		public long DLID { get; set; }

		public long DLTypeRef { get; set; }

		public long? ReferenceID { get; set; }

		[Required]
		[MaxLength(64)]
		public string Code { get; set; }

		[Required]
		[MaxLength(512)]
		public string Title { get; set; }

		[MaxLength(512)]
		public string? Title_En { get; set; }

		public long? CurrencyRef { get; set; }

		public int? EntityCode { get; set; }

		[MaxLength(64)]
		public string? Discriminator { get; set; }

		[MaxLength(512)]
		public string? Description { get; set; }

		public bool State { get; set; }

		[MaxLength(64)]
		public string? ClassificationNumber { get; set; }

		public long? ParentDLRef { get; set; }

		public byte[]? Version { get; set; }

	}

	[Table(name: "DLType", Schema = "FIN3")]
	public class RahkaranDLType
	{
		[Key]
		public long DLTypeID { get; set; }

		[Required]
		[MaxLength(128)]
		public string Title { get; set; }

		[MaxLength(128)]
		public string? Title_En { get; set; }

		[MaxLength(64)]
		public string? DefaultCode { get; set; }

		[MaxLength(512)]
		public string? Description { get; set; }

		public bool IsStatic { get; set; }

		public int? EntityCode { get; set; }

		[MaxLength(64)]
		public string? Discriminator { get; set; }

		public long? ParentDLTypeRef { get; set; }

		public bool IsParentRequired { get; set; }

		public int? ClassificationNumberLength { get; set; }

		public byte[]? Version { get; set; }

	}
}
