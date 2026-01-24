using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entities.App.Prd.Enums
{
    public enum StopRequstStopReasonEnum
    {

		[Display(Name = "کسری قطعات")]
		PartsShortage = 2579,
    
    [Display(Name = "مشکلات ساختی")]
		StructuralProblems = 2580,
    
    [Display(Name = "تغییرات مهندسی")]
		EngineeringChanges = 2581,
    
    [Display(Name = "نشتی از شلنگ رفت به ایراند")]
		AirEndInletHoseLeak = 2582,
    
    [Display(Name = "نشتی از شلنگ هوا و روغن")]
		AirAndOilHoseLeak = 2583,
    
    [Display(Name = "عمل نکردن شیر برقی")]
		SolenoidValveMalfunction = 2584,
    
    [Display(Name = "بالا بودن دمای کارکرد")]
		HighOperatingTemperature = 2585,
    
    [Display(Name = "مشکل برقی")]
		ElectricalProblem = 2586,
    
    [Display(Name = "ایراد در انلودر")]
		UnloaderDefect = 2587,
    
    [Display(Name = "لرزش شدید دستگاه")]
		SevereMachineVibration = 2588,
    
    [Display(Name = "ایراد ظاهری")]
		CosmeticDefect = 2589,
    
    [Display(Name = "صدای غیر عادی")]
		AbnormalSound = 2590,
    
    [Display(Name = "ایراد در سنسورها")]
		SensorDefect = 2591,
    
    [Display(Name = "ایراد در مخزن سپراتور")]
		SeparatorTankDefect = 2592,
    
    [Display(Name = "ایراد در MPV")]
		MPVDefect = 2593,
    
    [Display(Name = "ایراد در عملکرد دستگاه")]
		MachinePerformanceDefect = 2594,
    
    [Display(Name = "ایراد در کابینت")]
		CabinetDefect = 2595,
    
    [Display(Name = "شل بسته شدن اتصالات")]
		LooseConnections = 2596,
    
    [Display(Name = "خرابی اتصالات و قطعات")]
		ConnectionAndPartsFailure = 2597,
    
    [Display(Name = "خوردگی تسمه ها ، لرزش تسمه ها")]
		BeltCorrosionAndVibration = 2598,
    
    [Display(Name = "عدم استفاده از قطعات مناسب")]
		UseOfInappropriateParts = 2599,
    
    [Display(Name = "کسری قطعه")]
		PartShortage = 2600,
    
    [Display(Name = "ایراد در الکتروموتور")]
		ElectricMotorDefect = 2661,
    
    [Display(Name = "مشکل همراستایی پولی ها و کوپلینگ")]
		PulleyAndCouplingMisalignment = 2662,
    
    [Display(Name = "وجود روغن در هوای خروجی")]
		OilInOutletAir = 2663,
    
    [Display(Name = "ایراد در بلوکه هواساز")]
		AirBlockDefect = 2664,
    
    [Display(Name = "برعکس بسته شدن شیر یکطرفه")]
		CheckValveInstalledBackwards = 2665,
    
    [Display(Name = "عدم نصب صحیح قطعات")]
		IncorrectPartsInstallation = 2666,
    
    [Display(Name = "خرابی اتودرین")]
		AutodrainFailure = 2667,
    
    [Display(Name = "نشتی از رفت روغن به رادیاتور")]
		OilToRadiatorInletLeak = 2668,
    
    [Display(Name = "نشتی از برگشت روغن از رادیاتور")]
		OilFromRadiatorReturnLeak = 2669,
    
    [Display(Name = "کم بودن سطح روغن")]
		LowOilLevel = 2671,
    
    [Display(Name = "نشتی هوا")]
		AirLeakage = 2672,
    
    [Display(Name = "روشنایی داخل کابینت")]
		CabinetInternalLighting = 2673,
    
    [Display(Name = "ایراد در طراحی قطعات")]
		PartsDesignDefect = 2674,
    
    [Display(Name = "شل بسته شدن تسمه")]
		LooseBelt = 2675,
    
    [Display(Name = "پودر شدن مواد")]
		MaterialPowdering = 2676,
    
    [Display(Name = "عدم هم راستا بودن پولی ها")]
		PulleyMisalignment = 2677,
    
    [Display(Name = "قطع بودن برق منطقه تست")]
		TestAreaPowerOutage = 2678,
    
    [Display(Name = "در انتظار بازدید")]
		AwaitingInspection = 2679,
    
    [Display(Name = "نبود ظرفیت تست")]
		NoTestCapacity = 2680,
    
    [Display(Name = "بالا بودن نطقه شبنم")]
		HighDewPoint = 2681,
    
    [Display(Name = "نشتی از نماینگر سطح روغن")]
		OilLevelIndicatorLeak = 2682,
    
    [Display(Name = "ایراد در برد کنترلی")]
		ControlBoardDefect = 2683,
    
    [Display(Name = "نشتی از اتصالات مخزن سپراتور")]
		SeparatorTankConnectionLeak = 2684,
    
    [Display(Name = "ایراد کوپلینگ/خرد شدن لرزگیر")]
		CouplingDefectOrDamperCrushed = 2685,
    
    [Display(Name = "سایر")]
		Other = 2686
    }
}
