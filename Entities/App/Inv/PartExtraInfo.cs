using Common.Attributes;
using Entities.App.Inv.Enums;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Inv
{
	[Display(Name = "شناسنامه محصولات")]
	[Table("PartExtraInfo", Schema = "Inv")]
	public class PartExtraInfo : BaseEntity
	{
		[DisplayName("Product")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true, showInRelationData: true)]
		public Part Product { get; set; }

		public long ProductId { get; set; }


		[DisplayName("Info Type")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public PartExtraInfoInfoTypeEnum? InfoType { get; set; }



		[DisplayName("Design Type")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public PartExtraInfoDesignTypeEnum? DesignType { get; set; }

		[DisplayName("Dryer Type")]
		[DisplayInfo(null, true, type: SystemType.Select)]
		public PartExtraInfoDryerTypeEnum? DryerType { get; set; }

		

		[DisplayName("Revision")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Revision { get; set; }



		[DisplayName("مدل")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		public string Model { get; set; }

		[DisplayName("DewPoint")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[StringLength(400)]
		public string? DewPoint { get; set; }

		[DisplayName("ModelSuffix")]
		[DisplayInfo(null, true, type: SystemType.String )]
		public string? ModelSuffix { get; set; }


		[DisplayName("سایز")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(150)]
		public string? Size { get; set; }

 


		[DisplayName("Power KW")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(200)]
		public string? PowerKw { get; set; }


	 


		[DisplayName("Motor RPM")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(200)]
		public string? MotorRpm { get; set; }

 


		[DisplayName("Airend RPM")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? AirEndRPM { get; set; }


 


		[DisplayName("Model Motor")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(200)]
		public string? MotorModel { get; set; }


 


		[DisplayName("Pressure Working")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? WorkPressure { get; set; }


		[DisplayName("HydrotestPressure")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? HydrotestPressure { get; set; }

		[DisplayName("DesignPressure")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? DesignPressure { get; set; }



		[DisplayName("Flow NM3 H")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? FlowNM3H { get; set; }

		[DisplayName("In Out Nozzle")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(800)]
		public string? InOutNozzle { get; set; }


		[DisplayName("Mode Airend")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(200)]
		public string? AirEndMode { get; set; }


 


		[DisplayName("Class")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(200)]
		public string? ClassName { get; set; }

		[DisplayName("CycleTime")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(50)]
		public string? CycleTime { get; set; }




		[DisplayName("Capacity(M3/Min)")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]
		public decimal? Capacity { get; set; }


		[DisplayName("Gear Ratio")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? GearRatio { get; set; }


		[DisplayName("Inverter")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool Inverter { get; set; } = false;


		[DisplayName("PurityRate")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]
		public decimal? PurityRate { get; set; }

		[DisplayName("MeasureAerated")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]
		public decimal? MeasureAerated { get; set; }


		[DisplayName("Compressor Type")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(200)]
		public string? CompressorType { get; set; }


 


		[DisplayName("Power Transmission")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public PartExtraInfoPowerTransmissionEnum? PowerTransmission { get; set; }


		[DisplayName("Min & Max. Ambient Working Temprature")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(200)]
		public string? MinMaxAmbientWorkingTemperature { get; set; }





		[DisplayName("Installation Location")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public PartExtraInfoInstallationLocationEnum? InstallationLocation { get; set; }




		[DisplayName("Hazardous Area Clasification")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public PartExtraInfoHazardousAreaClassificationEnum? HazardousAreaClassification { get; set; }


		[DisplayName("LoaderAndPlatform")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public PartExtraInfoLoaderAndPlatformEnum? LoaderAndPlatform { get; set; }


		[DisplayName("Noise Level")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? NoiseLevel { get; set; }


		[DisplayName("Dimension L")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? L { get; set; }


		[DisplayName("W")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? W { get; set; }


		[DisplayName("H")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? H { get; set; }


		[DisplayName("Weight")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? Weight { get; set; }


		[DisplayName("Starting Method")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(200)]
		public string? StartingMethod { get; set; }

		[DisplayName("RalAndTypePaint")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(128)]
		public string? RalAndTypePaint { get; set; }


		[DisplayName("Control Panel	")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(200)]
		public string? ControlPanel { get; set; }


		[DisplayName("Cooling Method")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public PartExtraInfoCoolingMethodEnum? CoolingMethod { get; set; }


		[DisplayName("Connection Size")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(200)]
		public string? ConnectionSize { get; set; }


		[DisplayName("Starter Panel")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(200)]
		public string? StarterPanel { get; set; }


		 


		[DisplayName("OilCapacityBrand")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(200)]
		public string? OilCapacityBrand { get; set; }


		[DisplayName("AirOilFilterSepratorBrand")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(200)]
		public string? AirOilFilterSeparatorBrand { get; set; }


		[DisplayName("Barometric Pressure")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? BarometricPressure { get; set; }


		[DisplayName("Maximum Temperature")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? MaximumTemperature { get; set; }


		[DisplayName("Relative Humidity")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? RelativeHumidity { get; set; }



		[DisplayName("Comment")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? Comment { get; set; }


		[DisplayName("Oil Heater")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool OilHeater { get; set; } = false;


		[DisplayName("Motor Heater")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool MotorHeater { get; set; } = false;


		[DisplayName("SpaceHeater")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool SpaceHeater { get; set; } = false;


		[DisplayName("Motor PTC")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool MotorPTC { get; set; } = false;


		[DisplayName("Anti Condence Panel")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool AntiCondensationPanel { get; set; } = false;


		[DisplayName("Cooler Panel")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool CoolerPanel { get; set; } = false;


		[DisplayName("Heavy Duty Intake Filter")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool HeavyDutyIntakeFilter { get; set; } = false;


		[DisplayName("Dpt / Dps Intake Filter")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool DepartmentIntakeFilter { get; set; } = false;

		[DisplayName("AirendQty")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]
 
		public decimal? AirendQty { get; set; }

		[DisplayName("Comment")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? AirendBrand { get; set; }


		[DisplayName("ElectroMotorQty")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]

		public decimal? ElectroMotorQty { get; set; }


		[DisplayName("ElectroMotorBrand")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? ElectroMotorBrand { get; set; }


		[DisplayName("CoolingFanQty")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]

		public decimal? CoolingFanQty { get; set; }


		[DisplayName("CoolingFanBrand")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? CoolingFanBrand { get; set; }


		[DisplayName("UnloaderValveQty")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]

		public decimal? UnloaderValveQty { get; set; }


		[DisplayName("UnloaderValveBrand")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? UnloaderValveBrand { get; set; }




		[DisplayName("MinimumPressureValveQty")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]

		public decimal? MinimumPressureValveQty { get; set; }

		[DisplayName("MinimumPressureValveBrand")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? MinimumPressureValveBrand { get; set; }


		[DisplayName("OilTermostaticValveQty")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]

		public decimal? OilTermostaticValveQty { get; set; }


		[DisplayName("OilTermostaticValveBrand")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? OilTermostaticValveBrand { get; set; }


		[DisplayName("AirOilFilterSepratorQty")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]

		public decimal? AirOilFilterSepratorQty { get; set; }


		[DisplayName("AirOilFilterSepratorBrand")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? AirOilFilterSepratorBrand { get; set; }


		[DisplayName("AirIntakeFilterQty")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]

		public decimal? AirIntakeFilterQty { get; set; }


		[DisplayName("AirIntakeFilterBrand")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? AirIntakeFilterBrand { get; set; }

		[DisplayName("TemperatureSensorQty")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]

		public decimal? TemperatureSensorQty { get; set; }


		[DisplayName("TemperatureSensorBrand")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? TemperatureSensorBrand { get; set; }

		[DisplayName("PressureSensorQty")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]

		public decimal? PressureSensorQty { get; set; }


		[DisplayName("PressureSensorBrand")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? PressureSensorBrand { get; set; }


		[DisplayName("PressureIndicatorQty")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]

		public decimal? PressureIndicatorQty { get; set; }


		[DisplayName("PressureIndicatorBrand")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? PressureIndicatorBrand { get; set; }


		[DisplayName("SafetyValveQty")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]

		public decimal? SafetyValveQty { get; set; }


		[DisplayName("SafetyValveBrand")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? SafetyValveBrand { get; set; }


		[DisplayName("CouplingPulleyQty")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]

		public decimal? CouplingPulleyQty { get; set; }


		[DisplayName("CouplingPulleyBrand")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? CouplingPulleyBrand { get; set; }


		[DisplayName("CouplingPulley2Qty")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]

		public decimal? CouplingPulley2Qty { get; set; }


		[DisplayName("CouplingPulley2Brand")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? CouplingPulley2Brand { get; set; }


		[DisplayName("CoolerModelQty")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]

		public decimal? CoolerModelQty { get; set; }


		[DisplayName("CoolerModelBrand")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? CoolerModelBrand { get; set; }


		[DisplayName("OilFilterQty")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]

		public decimal? OilFilterQty { get; set; }


		[DisplayName("OilFilterBrand")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? OilFilterBrand { get; set; }



		[DisplayName("BeltSizeQty")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]

		public decimal? BeltSizeQty { get; set; }


		[DisplayName("BeltSizeBrand")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(256)]
		public string? BeltSizeBrand { get; set; }


		[DisplayName("OilCapacityQty")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]

		public decimal? OilCapacityQty { get; set; }

		[DisplayName("FanElectroMotor")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[StringLength(256)]
		public string? FanElectroMotor { get; set; }


		[DisplayName("PowerConsumption")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? PowerConsumption { get; set; }



		[DisplayName("PressureSafetyValve")]
		[DisplayInfo(null, false, type: SystemType.Decimal)]
		public decimal? PressureSafetyValve { get; set; }


		[DisplayName("DryerExtraInfo")]
		[DisplayInfo(null, false, type: SystemType.String)]
 
		public string? DryerExtraInfo { get; set; }


		[DisplayName("Psa Type")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public PartExtraInfoPsaTypeEnum? PsaType { get; set; }


		[DisplayName("Operational Design")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public PartExtraInfoOperationalDesignEnum? OperationalDesign { get; set; }


		[DisplayName("Opening Type")]
		[DisplayInfo(null, false, type: SystemType.Select)]
		public PartExtraInfoOpeningTypeEnum? OpeningType { get; set; }


		[DisplayName("Inlet Air Pressure (Barg)")]
		[DisplayInfo(null, false, type: SystemType.String)]

		public string? InletAirPressureBarg { get; set; }


		[DisplayName("Inlet Air Temperature(‘C)")]
		[DisplayInfo(null, false, type: SystemType.String)]

		public string? InletAirTemperature { get; set; }


		[DisplayName("N2 Flow Rate(Nm3/hr)")]
		[DisplayInfo(null, false, type: SystemType.String)]

		public string? N2FlowRate { get; set; }

		[DisplayName("N2 Purity(%)")]
		[DisplayInfo(null, false, type: SystemType.String)]

		public string? N2Purity { get; set; }


		[DisplayName("DryAirConsumption(Nm3/hr)")]
		[DisplayInfo(null, false, type: SystemType.String)]

		public string? DryAirConsumption { get; set; }

		
		 
	}
}
