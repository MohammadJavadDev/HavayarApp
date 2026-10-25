using System.ComponentModel.DataAnnotations;

namespace Entities.App.Hrm.Enums
{
	/// <summary>نوع وسیله نقلیه راننده حراست — Gnr_Lookup LookupType_FK = 206.</summary>
	public enum GuardVehicleTypeEnum
	{
		[Display(Name = "شخصی")]
		Personal = 1405,

		[Display(Name = "کامیون")]
		Truck = 1406,

		[Display(Name = "جرثقیل")]
		Crane = 1407,

		[Display(Name = "موتور")]
		Motorcycle = 1408,

		[Display(Name = "آمبولانس")]
		Ambulance = 1409,

		[Display(Name = "دولتی")]
		Government = 1447
	}
}
