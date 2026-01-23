using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Rahkaran.USR3
{

	[Table(name: "Gnr_PartyIndustry", Schema = "USR3")]
	public class RahkaranGnr_PartyIndustry
	{
		[Key]
		public long Gnr_PartyIndustryID { get; set; }

		public decimal? Code { get; set; }

		[MaxLength(512)]
		public string? Title { get; set; }

		public long Creator { get; set; }

		public DateTime CreationDate { get; set; }

		public long LastModifier { get; set; }

		public DateTime LastModificationDate { get; set; }

	 

	}
}
