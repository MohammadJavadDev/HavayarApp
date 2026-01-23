using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sale.Enums
{
	public enum RequirementAdvertiseInquiryTypeEnum
	{
		[Display(Name = "مناقصه")]
		Tender = 1,
		[Display(Name = "بازاریابی")]
		Marketing = 2,
		[Display(Name = "درخواست تلفنی")]
		TelephoneRequest = 3,
		[Display(Name = "درخواست مستقیم")]
		DirectRequest = 4,
		[Display(Name = "نمایشگاه")]
		Exhibition = 5,
		[Display(Name = "ایمیل Info")]
		EmailInfo = 6,
		[Display(Name = "مشتری قدیمی")]
		OldCustomer = 7,
		[Display(Name = "سایت هوایار")]
		HavaYarSite = 8,
		[Display(Name = "معرفی شده کارشناس فروش")]
		IntroducedSalesExpert = 9,
		[Display(Name = "معرفی شده توسط خدمات پس از فروش")]
		IntroducedByAfterSalesService = 10,
		[Display(Name = "معرفی شده توسط نماینده فروش")]
		IntroducedBySalesRepresentative = 11,
		[Display(Name = "نماینده هوایار")]
		AirlineRepresentative = 12,
		[Display(Name = "ارجاع از نماینده خدمات پس از فروش")]
		AfterSalesServiceRepresentativeReferral = 13,
	}
}
