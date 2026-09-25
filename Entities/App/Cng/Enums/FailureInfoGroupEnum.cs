using System.ComponentModel.DataAnnotations;

namespace Entities.App.Cng.Enums
{
	/// <summary>گروه پارامتر فنی CNG — Gnr_Lookup LookupType_FK = 218.</summary>
	public enum FailureInfoGroupEnum
	{
		[Display(Name = "Compressor")]
		Compressor = 1503,

		[Display(Name = "Dryer")]
		Dryer = 1504,

		[Display(Name = "StorageModule")]
		StorageModule = 1505,

		[Display(Name = "Air Compressor")]
		AirCompressor = 1506,

		[Display(Name = "F&G")]
		FG = 1507,

		[Display(Name = "Dispenser")]
		Dispenser = 1508
	}
}
