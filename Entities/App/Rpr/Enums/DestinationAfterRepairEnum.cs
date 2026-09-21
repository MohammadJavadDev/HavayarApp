using System.ComponentModel.DataAnnotations;

namespace Entities.App.Rpr.Enums
{
	/// <summary>مقصد پس از تعمیر — Gnr_Lookup LookupType_FK = 69.</summary>
	public enum DestinationAfterRepairEnum
	{
		[Display(Name = "مشتری")]
		Customer = 293,
		[Display(Name = "نمایندگی")]
		Agency = 294,
		[Display(Name = "هوایار (داغی)")]
		HavayarUsedPart = 295,
		[Display(Name = "هوایار (برگشت امانی)")]
		HavayarLoanReturn = 296,
		[Display(Name = "هوایار")]
		Havayar = 2700
	}
}
