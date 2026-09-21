using System.ComponentModel.DataAnnotations;

namespace Entities.App.Bpm.Enums
{
	/// <summary>
	/// سطح دسترسی سند — معادل Gnr_Lookup LookupType 231 (Bpm_PermissionType).
	/// </summary>
	public enum ProcessDocumentAccessLevelEnum
	{
		[Display(Name = "محرمانه")]
		Secret = 1737,

		[Display(Name = "محدود")]
		Limit = 1738,

		[Display(Name = "عمومی")]
		Public = 1739
	}
}
