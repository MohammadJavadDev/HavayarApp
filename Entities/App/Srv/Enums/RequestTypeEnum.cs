using System.ComponentModel.DataAnnotations;

namespace Entities.App.Srv.Enums
{
	/// <summary>نوع درخواست بلیط و هتل — Gnr_Lookup LookupType_FK = 193.</summary>
	public enum RequestTypeEnum
	{
		[Display(Name = "خدمات پس از فروش")]
		AfterSales = 1290,

		[Display(Name = "بازاریابی و فروش")]
		MarketingAndSales = 1291,

		[Display(Name = "امور حقوقی")]
		LegalAffairs = 1307,

		[Display(Name = "امور مالی و اداری")]
		FinancialAndAdmin = 1308,

		[Display(Name = "سایر")]
		Other = 2463
	}
}
