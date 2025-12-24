using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Rahkaran.GNR3
{
	[Table(name: "Unit", Schema = "GNR3")]
	public class RahkaranUnit
	{
		[Key]
		public long UnitID { get; set; }

		[Required]
		[MaxLength(256)]
		public string Name { get; set; }

		[MaxLength(256)]
		public string? AbbreviatedName { get; set; }

		public int? Dimension { get; set; }

		public int? State { get; set; }

		public byte[]? Version { get; set; }

	}

}
