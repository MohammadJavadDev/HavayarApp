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

 


		[DisplayName("مدل")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		public string Model { get; set; }


	 


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
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? WorkPressure { get; set; }


	 


		[DisplayName("Mode Airend")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(200)]
		public string? AirEndMode { get; set; }


 


		[DisplayName("Class")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(200)]
		public string? ClassName { get; set; }


	 


		[DisplayName("Capacity(M3/Min)")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? CapacityM3PerMinute { get; set; }


	 

		[DisplayName("Gear Ratio")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? GearRatio { get; set; }


		[DisplayName("Inverter")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool Inverter { get; set; } = false;

		 


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


		[DisplayName("Control Panel	")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(200)]
		public string? ControlPanel { get; set; }


		[DisplayName("Cooling Method	")]
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


		[DisplayName("OilCapacityQty")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(200)]
		public string? OilCapacityQuantity { get; set; }


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
		[DisplayInfo(null, false, type: SystemType.Int)]
		public int? MaximumTemperature { get; set; }


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


	}
}
