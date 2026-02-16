using Common.Attributes;
using Entities.App.Inv;
using Entities.App.Sup.Enums;
using Entities.Auth;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Sup
{
	[Display(Name = "دسته های خرید")]
	[Table("BuyCategory", Schema = "Sup")]
	public class BuyCategory : BaseEntity
	{
		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		public string Title { get; set; }


		[DisplayName("متولی خرید")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? PurchaseResponsible { get; set; }

		public long? PurchaseResponsibleId { get; set; }


		[DisplayName("زمان در راه غیر روتین (روز)")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? NonRoutineLeadTimeInDay { get; set; }

		[DisplayName("زمان در راه  روتین (روز)")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? RoutineLeadTimeInDay { get; set; }



		[DisplayName("نوع")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public BuyCategoryTypeEnum? Type { get; set; }


		[DisplayName("اقلام")]
		[DisplayInfo(null, true, type: SystemType.ListEntity)]
		public List<BuyCategoryItem> Items { get; set; } = new();

		public long? HamkaranId { get; set; }

	}

	[Display(Name = "افلام دسته های خرید")]
	[Table("BuyCategoryItem", Schema = "Sup")]
	public class BuyCategoryItem : BaseEntity
	{
		public long BuyCategoryId { get; set; }
		[DisplayName("دسته خرید")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public BuyCategory BuyCategory { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Part? Part { get; set; }

		public long? PartId { get; set; }


		[DisplayName("زمان در راه")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? TimeInWay { get; set; }


		[DisplayName("تامین کننده")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public BuyCategoryItemSupplierEnum? Supplier { get; set; }


		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? Comments { get; set; }

		public long? HamkaranId { get; set; }

	}

}
