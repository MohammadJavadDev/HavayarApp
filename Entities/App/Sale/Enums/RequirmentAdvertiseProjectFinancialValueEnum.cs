using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entities.App.Sale.Enums
{
	public enum RequirmentAdvertiseProjectFinancialValueEnum
    {

		[Display(Name = "کوچک")]
		Small = 1,

		[Display(Name = "متوسط")]
		Medium = 2,

		[Display(Name = "بزرگ")]
		Larg = 3,
	}
}
