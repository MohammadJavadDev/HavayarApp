using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.FIN
{
	[Display(Name = "نوع تفصیل")]
	[Table("DLType", Schema = "FIN")]
	public class DLType : BaseEntity
	{
		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(128)]
		public string Title { get; set; }


		[DisplayName("عنوان به انگیلیسی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(128)]
		public string? TitleInEnglish { get; set; }


		[DisplayName("شناسه راهکاران")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? HamkaranId { get; set; }


	}
}
