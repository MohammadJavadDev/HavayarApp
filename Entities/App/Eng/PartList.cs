using Common.Attributes;
using Entities.App.FIN;
using Entities.App.Inv;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Eng
{
	[Display(Name = "پارت لیست")]
	[Table("PartList", Schema = "Eng")]
	public class PartList : BaseEntity
	{
		[DisplayName("دسته بندی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public PartListGroup? PartListGroup { get; set; }

		public long? PartListGroupId { get; set; }


		[DisplayName("محصول")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Part? Product { get; set; }

		public long? ProductId { get; set; }

        [DisplayName("تفصیل")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public DL? DL { get; set; }

        public long? DlId { get; set; }



        [DisplayName("دسته بندی محصولات")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public PartListProductSection? PartListProductSection { get; set; }

        public long? PartListProductSectionId { get; set; }


        [DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Part? Part { get; set; }

		public long? PartId { get; set; }


		[DisplayName("مقدار")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Qty { get; set; }


		[DisplayName("ترتیب")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Order { get; set; }


		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Description { get; set; }


	}
}
