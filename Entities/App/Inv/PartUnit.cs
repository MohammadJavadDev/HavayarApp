using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Inv
{
	[Display(Name = "واحد اندازه گیری")]
	[Table("PartUnit", Schema = "Inv")]
	public class PartUnit : BaseEntity
	{
		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(300)]
		public string? Title { get; set; }


		[DisplayName("شناسه در همکاران")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? HamkaranId { get; set; }


	}
}
