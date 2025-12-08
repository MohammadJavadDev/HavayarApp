using Common.Attributes;
using Entities.App.Inv;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Eng
{
	[Display(Name = "دسته بندی پارت لیست")]
	[Table("PartListGroup", Schema = "Eng")]
	public class PartListGroup : BaseEntity
	{
		[DisplayName("محصول")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Part? Product { get; set; }

		public long? ProductId { get; set; }


		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Title { get; set; }


		[DisplayName("ترتیب")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Order { get; set; }


		[DisplayName("تصویر")]
		[DisplayInfo(null, true, type: SystemType.File, fileTypes: ".jpg,.png")]
		public FileEntity? Picture { get; set; }

		public long? PictureId { get; set; }


		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Description { get; set; }


	}
}
