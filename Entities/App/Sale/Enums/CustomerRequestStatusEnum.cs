using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sale.Enums
{
	/// <summary>وضعیت درخواست مشتری — Gnr_Lookup LookupType_FK = 201.</summary>
	public enum CustomerRequestStatusEnum
	{
		[Display(Name = "در حال بررسی")]
		UnderReview = 1381,

		[Display(Name = "تامین قطعه")]
		SupplyPart = 1382,

		[Display(Name = "ارسال کارشناس")]
		SendExpert = 1383,

		[Display(Name = "حسابداری")]
		Accounting = 1384,

		[Display(Name = "انجام شد")]
		Done = 1385,

		[Display(Name = "رد درخواست")]
		Rejected = 1386
	}
}
