using System.ComponentModel.DataAnnotations;

namespace Entities.App.SLS.Enums
{
	/// <summary>گرید مشتری — Gnr_Lookup LookupType_FK = 62. Confirmed TotalSystem 2026-09-15.</summary>
	public enum CustomerGradeEnum
	{
		[Display(Name = "A")]
		A = 239,

		[Display(Name = "B")]
		B = 240,

		[Display(Name = "C")]
		C = 242,

		[Display(Name = "تعطیل")]
		Closed = 243
	}
}
