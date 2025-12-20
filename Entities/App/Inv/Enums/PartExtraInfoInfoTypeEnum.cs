using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.App.Inv.Enums
{
	public enum PartExtraInfoInfoTypeEnum
	{
		[Display(Name = "کمپرسور اسکرو اویل اینجکت")]
		OilInjectedScrewCompressor = 1,
		[Display(Name = " کمپرسور اسکرو اویل فری")]
		OilFreeScrewCompressor = 2,
		[Display(Name = "درایر")]
		Dryer = 3,
		[Display(Name = "PSA")]
		PSA = 4,
		[Display(Name = "ثابت")]
		Proven = 5,
	 
	}
}
