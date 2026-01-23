using Common.Attributes;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.FIN
{
	[Display(Name = "حساب تفصیلی")]
	[Table("DL", Schema = "FIN")]
	public class DL : BaseEntity
	{
		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(512)]
		public string Title { get; set; }


		[DisplayName("کد")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(64)]
		public string Code { get; set; }


		[DisplayName("عنوان به انگیلیسی")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(512)]
		public string? TitleEnglish { get; set; }


		[DisplayName("نوع")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public DLType? Type { get; set; }

		public long? TypeId { get; set; }

		public long? HamkaranId { get; set; }


	}
}
