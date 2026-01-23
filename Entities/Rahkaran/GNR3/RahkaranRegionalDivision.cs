using Entities.App.Gnr.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Rahkaran.GNR3
{
	[Table(name: "RegionalDivision", Schema = "GNR3")]
	public class RahkaranRegionalDivision
	{
		[Key]
		public long RegionalDivisionID { get; set; }

		[Required]
		[MaxLength(128)]
		public string? Name { get; set; }

		[MaxLength(128)]
		public string? Title_EN { get; set; }

		public long? ParentRef { get; set; }

		public RegionTypeEnum? Type { get; set; }

		[MaxLength(64)]
		public string? Code { get; set; }

		[MaxLength(32)]
		public string? Abbreviation { get; set; }

		[MaxLength(32)]
		public string? LocalCode { get; set; }

		[MaxLength(32)]
		public string? FinanceCode { get; set; }

		public byte[] Version { get; set; }

		public int? Left { get; set; }

		public int? Right { get; set; }
	}

}
