using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Bom
{
	[Display(Name = "گروه بندی محصولات")]
	[Table("ProductGroup", Schema = "Bom")]
	public class ProductGroup : BaseEntity
	{
		[DisplayName("نام")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string?  Name { get; set; }


	}
}
