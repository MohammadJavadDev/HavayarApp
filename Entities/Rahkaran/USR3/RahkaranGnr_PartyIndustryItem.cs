using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Rahkaran.USR3
{
    [Table(name: "Gnr_PartyIndustryItem", Schema = "USR3")]
	public class RahkaranGnr_PartyIndustryItem
	{
		[Key]
		public long Gnr_PartyIndustryItemID { get; set; }

		public long _MasterRef { get; set; }

		public decimal? Code { get; set; }

		[MaxLength(512)]
		public string? Title { get; set; }

		public long Creator { get; set; }

		public DateTime CreationDate { get; set; }

		public long LastModifier { get; set; }

		public DateTime LastModificationDate { get; set; }

	 

	}
}
