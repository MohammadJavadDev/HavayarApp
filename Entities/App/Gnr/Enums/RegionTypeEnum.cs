using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entities.App.Gnr.Enums
{
    public enum RegionTypeEnum
    {
		[Display(Name = "کشور")]
		Cuntry = 1,
			[Display(Name = "استان")]
		Province = 2,
			[Display(Name = "شهر")]
		City = 3,

		[Display(Name = "شهرستان")]
		County = 3,
		[Display(Name = "منطقه")]
		area = 5,
		

	}
}
