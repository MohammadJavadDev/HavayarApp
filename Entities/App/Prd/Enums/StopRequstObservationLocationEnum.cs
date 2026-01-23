using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entities.App.Prd.Enums
{
    public enum StopRequstObservationLocationEnum
    {
		[Display(Name ="تولید")]
		Product=2572,
		[Display(Name = "تست تولید")]
		TestProduct = 2573,
		[Display(Name = "بازرسی نهایی")]
		FinalInspection = 2574,

	}
}
