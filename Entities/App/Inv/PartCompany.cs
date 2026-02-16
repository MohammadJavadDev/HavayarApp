using Common.Attributes;
using Entities.App.Gnr;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Inv
{
	[Display(Name = "تامین کنندگان محصولات")]
	[Table("PartCompany", Schema = "Inv")]
	public class PartCompany : BaseEntity
	{
		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Part? Part { get; set; }

		public long PartId { get; set; }


		[DisplayName("تامین کننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Supplier? Supplier { get; set; }

		public long SupplierId { get; set; }


	}
}
