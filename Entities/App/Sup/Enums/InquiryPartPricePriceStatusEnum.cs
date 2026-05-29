using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sup.Enums
{
	public enum InquiryPartPricePriceStatusEnum
	{
		[Display(Name = "خرید")]
		Purchase = 2831,
		[Display(Name = "استعلام روتین")]
		RoutineQuery = 2832,
		[Display(Name = "بروزرسانی")]
		Update = 2833,
		[Display(Name = "پرایس لیست")]
		PriceList = 2834,
	}
}
