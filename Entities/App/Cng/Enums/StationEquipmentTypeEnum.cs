using System.ComponentModel.DataAnnotations;

namespace Entities.App.Cng.Enums
{
	/// <summary>نوع تجهیز جایگاه CNG — Gnr_Lookup LookupType_FK = 219.</summary>
	public enum StationEquipmentTypeEnum
	{
		[Display(Name = "500/250")]
		Type500_250 = 1510,

		[Display(Name = "1500/250")]
		Type1500_250 = 1511,

		[Display(Name = "500/60")]
		Type500_60 = 1512,

		[Display(Name = "2000/250")]
		Type2000_250 = 1513,

		[Display(Name = "1200/250")]
		Type1200_250 = 1514,

		[Display(Name = "1000/250")]
		Type1000_250 = 1515,

		[Display(Name = "1000/60")]
		Type1000_60 = 1516,

		[Display(Name = "570/250")]
		Type570_250 = 1517,

		[Display(Name = "NC")]
		NC = 1518
	}
}
