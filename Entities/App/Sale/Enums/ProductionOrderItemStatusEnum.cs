using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entities.App.Sale.Enums
{
    public enum ProductionOrderItemStatusEnum
    {

		[Display(Name = "0")]
		NotCompleted = 579,

		[Display(Name = "1")]
		Completed = 580,

		[Display(Name = "باطل")]
		Invalid = 581,
	}
}
