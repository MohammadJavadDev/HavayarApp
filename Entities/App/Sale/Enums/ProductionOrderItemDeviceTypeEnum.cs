using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sale.Enums
{
	public enum ProductionOrderItemDeviceTypeEnum
	{
		[Display(Name = "کمپرسور اسکرو")]
		ScrewCompressor = 194,
		[Display(Name = "دستگاه مولد گاز")]
		GasGeneratorDevice = 195,
		[Display(Name = "سانتریفیوژ")]
		Centrifuge = 2064,
		[Display(Name = "درایر")]
		Dryer = 2065,
		[Display(Name = "مخازن")]
		Warehouse = 2066,
		[Display(Name = "تله آبگیر")]
		DrainTrap = 2067,
		[Display(Name = "فیلتراسیون")]
		Filtration = 2068,
		[Display(Name = "COALTOWER")]
		Coaltower = 2069,
		[Display(Name = "سکوئنسر")]
		Sequencer = 2211,
		[Display(Name = "تابلو اینورتر")]
		InverterPanel = 2212,
		[Display(Name = "تجهیزات CNG")]
		CngEquipment = 2232,
		[Display(Name = "قطعات یدکی")]
		SparePart = 2235,
		[Display(Name = "کمپرسور رفت و برگشتی")]
		ReciprocatingCompressor = 2255,
		[Display(Name = "تابلو کنترل")]
		ControlPanel = 2403,
		[Display(Name = "تابلو برق")]
		ElectricalPanel = 2404,
		[Display(Name = "چیلر")]
		Chiller = 2405,
		[Display(Name = "مجموعه پایپینگ")]
		PipeCollection = 2406,
		[Display(Name = "Heat exchanger")]
		HeatExchanger = 2407,
		[Display(Name = "کمپرسور پیستونی یا اویل فری هوایاری")]
		AirplanePistonOrOilFreeCompressor = 2409,
		[Display(Name = "کمپرسور پیستونی یا اویل فری غیرهویاری (خرید)")]
		NonContactOilFreePistonCompressorPurchase = 2410,
		[Display(Name = "نامشخص")]
		Unknown = 2455,
	}
}
