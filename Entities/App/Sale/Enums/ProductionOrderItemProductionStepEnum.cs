using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entities.App.Sale.Enums
{
    public enum ProductionOrderItemProductionStepEnum
    {
		[Display(Name = "برنامه ریزی")]
		Planning = 210,

		[Display(Name = "بازرگانی در راه")]
		CommerceInTransit = 211,

		[Display(Name = "در انتظار مهندسی")]
		AwaitingEngineering = 213,

		[Display(Name = "منتظر تولید")]
		AwaitingProduction = 214,

		[Display(Name = "در حال تولید")]
		InProduction = 215,

		[Display(Name = "تست")]
		Testing = 216,

		[Display(Name = "بسته بندی")]
		Packaging = 220,

		[Display(Name = "آماده ارسال")]
		ReadyForShipping = 221,

		[Display(Name = "تحویل فروش")]
		DeliveredToSales = 223,

		[Display(Name = "تحویل امانی")]
		ConsignmentDelivery = 225,

		[Display(Name = "باطل")]
		Canceled = 227,

		[Display(Name = "اختتام یافته")]
		Completed = 228,

		[Display(Name = "تدارکات در راه")]
		LogisticsInTransit = 246,

		[Display(Name = "اصلاحیه")]
		Amendment = 1373,

		[Display(Name = "انبار تدارکات")]
		LogisticsWarehouse = 1374,

		[Display(Name = "برگشت از فروش")]
		ReturnedFromSales = 1375,

		[Display(Name = "تحت بررسی")]
		UnderReview = 1376,

		[Display(Name = "تعمیرات")]
		Repair = 1377,

		[Display(Name = "غیر قابل ارسال")]
		Unshippable = 1378,

		[Display(Name = "متوقف")]
		Suspended = 1379,

		[Display(Name = "در انتظار مدیر پروژه")]
		AwaitingProjectManager = 1380,

		[Display(Name = "در انتظار صنایع")]
		AwaitingIndustry = 2744
	}
}
