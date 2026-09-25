using System.ComponentModel.DataAnnotations;

namespace Entities.App.Srv.Enums
{
	/// <summary>نوع خدمات بلیط و هتل — Gnr_Lookup LookupType_FK = 194.</summary>
	public enum ServiceTypeEnum
	{
		[Display(Name = "گارانتی")]
		Warranty = 1292,

		[Display(Name = "وارانتی")]
		Guaranty = 1293,

		[Display(Name = "سایر")]
		Other = 1294
	}
}
