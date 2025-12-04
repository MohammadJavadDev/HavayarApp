using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.App.Epms.Enums
{
	public enum EquipmentPriceEstimateStatusEnum
	{
		[Display(Name = "ثبت اولیه")]
		Submit,
		[Display(Name = "ارسال به فروش")]
		SendToSell,
		[Display(Name = "تایید")]
		Approved,
		[Display(Name = "رد شده")]
		Rejected,
		[Display(Name = "بسته شده")]
		Closed = 50
	}
}
