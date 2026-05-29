using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.SLS
{
	[Display(Name = "مرکز فروش")]
	[Table("SalesOffice", Schema = "SLS")]
	public class SalesOffice : BaseEntity
	{
		[DisplayName("نام")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Name { get; set; }


		[DisplayName("کد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? Code { get; set; }

		public long? HamkaranId { get; set; }


	}
}
