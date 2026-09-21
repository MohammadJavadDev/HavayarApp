using System.ComponentModel.DataAnnotations;

namespace Entities.App.Crm.Enums
{
	public enum CustomerSatisfactionSurveyTypeEnum : short
	{
		[Display(Name = "بعد از فروش")]
		AfterSales = 1008,
		[Display(Name = "بعد از فروش CNG")]
		CngAfterSales = 1942,
		[Display(Name = "خدمات هوای فشرده")]
		CompressedAirServices = 2332
	}
}
