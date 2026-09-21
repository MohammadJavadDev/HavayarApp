using System.ComponentModel.DataAnnotations;

namespace Entities.App.Bpm.Enums
{
	/// <summary>
	/// فوریت درخواست — معادل Gnr_Lookup LookupType 233 (Bpm_RequestPrority).
	/// </summary>
	public enum ProcessDocumentPriorityEnum
	{
		[Display(Name = "آنی/ بحرانی")]
		InstantCritical = 1747,

		[Display(Name = "نرمال")]
		Normal = 1748,

		[Display(Name = "فوری")]
		Urgent = 1749
	}
}
