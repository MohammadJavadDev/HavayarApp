using System.ComponentModel.DataAnnotations;

namespace Entities.App.Inv.Enums
{
	public enum PartExtraInfoPowerTransmissionEnum
	{
		[Display(Name = "Direct Coupling")]
		DirectCoupling = 656,
		[Display(Name = "Belt & Pulley")]
		BeltAndPulley = 657,
	}
}
