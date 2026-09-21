using System.ComponentModel.DataAnnotations;

namespace Entities.App.Bpm.Enums
{
	/// <summary>
	/// نوع درخواست — معادل Gnr_Lookup LookupType 229 (Bpm_RequestType).
	/// مقدار = Lookup_ID زنده TotalSystem.
	/// </summary>
	public enum ProcessDocumentRequestTypeEnum
	{
		[Display(Name = "ایجاد")]
		Create = 1719,

		[Display(Name = "حذف")]
		Delete = 1720,

		[Display(Name = "بازنگری")]
		Review = 1721,

		[Display(Name = "مکانیزاسیون فرآیند")]
		ProcessAutomation = 1722,

		[Display(Name = "تغییر فرآیند مکانیزه")]
		MechanizedProcessChange = 1723
	}
}
