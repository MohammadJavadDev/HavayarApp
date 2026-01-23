using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entities.App.Sale.Enums
{
    public enum ProductionOrderStateEnum
    {
		[Display(Name = "ثبت اولیه")]
		Submit = 1,
		[Display(Name = "بررسی واحد مالی")]
		FinancialUnitReview = 2,
		[Display(Name = "تایید مالی و آغاز فرآیند سفارش ساخت")]
		FinancialApproval = 3,
		[Display(Name = "منسوخ شده")]
		Obsolete = 4,
	}
}
