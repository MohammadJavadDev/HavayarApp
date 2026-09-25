using System.ComponentModel.DataAnnotations;

namespace Entities.App.Cng.Enums
{
	/// <summary>نوع خدمات گزارش کار CNG — Gnr_Lookup LookupType_FK = 194.</summary>
	public enum WorkReportServiceTypeEnum
	{
		[Display(Name = "گارانتی")]
		Guarantee = 1292,

		[Display(Name = "وارانتی")]
		Warranty = 1293,

		[Display(Name = "سایر")]
		Other = 1294
	}
}
