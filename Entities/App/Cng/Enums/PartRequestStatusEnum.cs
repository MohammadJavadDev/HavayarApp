using System.ComponentModel.DataAnnotations;

namespace Entities.App.Cng.Enums
{
	/// <summary>وضعیت درخواست کالا CNG — Gnr_Lookup LookupType_FK = 237. مقدار ۰ در DB مجاز است.</summary>
	public enum PartRequestStatusEnum
	{
		[Display(Name = "استعلام")]
		Inquiry = 1780,

		[Display(Name = "ویرایش")]
		Edit = 1781,

		[Display(Name = "عدم تایید")]
		Rejected = 1782,

		[Display(Name = "تایید شده")]
		Approved = 1783,

		[Display(Name = "درخواست ارسال اقلام")]
		SendItems = 1784,

		[Display(Name = "پیش فاکتور")]
		Prefactor = 1785,

		[Display(Name = "حواله گارانتی")]
		GuaranteeVoucher = 1892,

		[Display(Name = "بسته شده")]
		Closed = 1893
	}
}
