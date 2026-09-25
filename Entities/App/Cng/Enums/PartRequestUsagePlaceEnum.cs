using System.ComponentModel.DataAnnotations;

namespace Entities.App.Cng.Enums
{
	/// <summary>محل مصرف قلم درخواست کالا CNG — Gnr_Lookup LookupType_FK = 236. مقدار ۰ در DB مجاز است.</summary>
	public enum PartRequestUsagePlaceEnum
	{
		[Display(Name = "شارژ انبار")]
		WarehouseCharge = 1778,

		[Display(Name = "جایگاه")]
		Station = 1779
	}
}
