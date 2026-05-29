using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sup.Enums
{
	public enum InquiryPartPriceTypeEnum
	{
		[Display(Name = "استعلام")]
		Inquiry = 289,
		[Display(Name = "تدارکات داخلی")]
		InternalProcurement = 290,
		[Display(Name = "بازرگانی خارجی")]
		ForeignTrade = 291,
		[Display(Name = "استعلام توسط خدمات پس از فروش")]
		AfterSalesInquiry = 1675,

		[Display(Name = "فاکتور خرید")]
		Invoice = 1676,
 
	}
}
