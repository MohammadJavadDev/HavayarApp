using System.ComponentModel.DataAnnotations;

namespace Entities.App.Rpr.Enums
{
	/// <summary>شرح کار تعمیر — Gnr_Lookup LookupType_FK = 221.</summary>
	public enum RepairJobDescriptionEnum
	{
		[Display(Name = "اورهال کامل")]
		FullOverhaul = 1547,
		[Display(Name = "اورهال ایرند")]
		AirEndOverhaul = 1548,
		[Display(Name = "اورهال موتور")]
		MotorOverhaul = 1549,
		[Display(Name = "تعمیر برد")]
		BoardRepair = 1550,
		[Display(Name = "تعمیر رادیاتور")]
		RadiatorRepair = 1551,
		[Display(Name = "تعمیر")]
		Repair = 1922
	}
}
