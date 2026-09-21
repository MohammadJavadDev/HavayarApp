using System.ComponentModel.DataAnnotations;

namespace Entities.App.Crm.Enums
{
	/// <summary>Crm_ZoneType — not Gnr_Lookup. Confirmed TotalSystem 2026-09-15.</summary>
	public enum ZoneTypeEnum
	{
		[Display(Name = "هوای فشرده")]
		CompressedAir = 1,

		[Display(Name = "CNG")]
		Cng = 2,

		[Display(Name = "فرآیندی")]
		Process = 3
	}
}
