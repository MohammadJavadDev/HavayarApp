using System.ComponentModel.DataAnnotations;

namespace Entities.App.Rpr.Enums
{
	/// <summary>نوع تجهیز درخواست تعمیر خدمات پس از فروش.</summary>
	public enum RepairEquipmentTypeEnum
	{
		[Display(Name = "کمپرسور")]
		Compressor = 1,

		[Display(Name = "درایر")]
		Dryer = 2,

		[Display(Name = "ایراند")]
		AirEnd = 3,

		[Display(Name = "الکتروموتور")]
		ElectricMotor = 4,

		[Display(Name = "برد")]
		Board = 5,

		[Display(Name = "رادیاتور")]
		Radiator = 6,

		[Display(Name = "سکوئنسر")]
		Sequencer = 7,

		[Display(Name = "اینورتر")]
		Inverter = 8,

		[Display(Name = "تابلو اکسیژن ساز")]
		OxygenGeneratorPanel = 9,

		[Display(Name = "مخزن")]
		Tank = 10,

		[Display(Name = "بوستر")]
		Booster = 11,

		[Display(Name = "وکیوم پمپ")]
		VacuumPump = 12,

		[Display(Name = "آنلودر")]
		Unloader = 13,

		[Display(Name = "مینیمم پرشر ولو")]
		MinimumPressureValve = 14,

		[Display(Name = "سافت استارتر")]
		SoftStarter = 15,

		[Display(Name = "ایراند HP")]
		AirEndHp = 16,

		[Display(Name = "ایراند LP")]
		AirEndLp = 17,

		[Display(Name = "کمپرسور اویل فری")]
		CompressorOilFree = 18,

		[Display(Name = "بلوئر")]
		Blower = 19,

		[Display(Name = "اکسیژن ساز")]
		OxygenGenerator = 20,

		[Display(Name = "تابلو نیتروژن ساز")]
		NitrogenGeneratorPanel = 21,

		[Display(Name = "کنترل پنل")]
		ControlPanel = 22,

		[Display(Name = "هیدروژن ساز")]
		HydrogenGenerator = 23,

		[Display(Name = "نیتروژن ساز")]
		NitrogenGenerator = 24,

		[Display(Name = "سایر")]
		Other = 25
	}
}
