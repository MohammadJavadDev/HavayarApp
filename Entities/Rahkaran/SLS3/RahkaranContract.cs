using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Rahkaran.SLS3
{
	[Table(name: "Contract", Schema = "SLS3")]
	public class RahkaranContract
	{
	 

		[Key]
		public long ContractID { get; set; }

		[Required]
		[MaxLength(256)]
		public string? Title { get; set; }

 

	 



		public int? State { get; set; }

		[Required]
		[MaxLength(50)]
		public string? Number { get; set; }

 

		public long? CustomerRef { get; set; }


		[Required]
		[MaxLength(128)]
		public string? ContractNumber { get; set; }

 

		public decimal? NetPrice { get; set; }



		[MaxLength(4000)]
		public string? Description { get; set; }



		public DateTime? Date { get; set; }

	

		public long? SalesTypeRef { get; set; }

	 

	}
}
