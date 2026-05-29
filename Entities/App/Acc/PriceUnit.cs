using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Acc
{
	[Display(Name = "واحد ارز")]
	[Table("PriceUnit", Schema = "Acc")]
	public class PriceUnit : BaseEntity
	{
		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(200)]
		public string? Title { get; set; }


		[DisplayName("نماد")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(10)]
		public string? Symbol { get; set; }


		[DisplayName("مقدار")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Value { get; set; }


	}
}
