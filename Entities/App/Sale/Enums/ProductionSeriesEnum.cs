using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sale.Enum
{

	public enum ProductionSeriesEnum
	{
		[Display(Name = "اینورتر")]
		Inverter = 1,

		[Display(Name = "سافت استارتر")]
		SoftStarter = 2,

		[Display(Name = "سه فاز")]
		ThreePhase = 3,

		[Display(Name = "تک فاز")]
		SinglePhase = 4,
	}

}

