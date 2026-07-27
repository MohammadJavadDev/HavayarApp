using Common.Attributes;
using Entities.App.Inv;
using Entities.App.Sale;
using Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.App.Pln
{
	[Display(Name = "کسری اقلام سفارش ساخت")]
	[Table("ProductionOrderItemDeficit", Schema = "Pln")]
	public class ProductionOrderItemDeficit : BaseEntity
	{


		[DisplayName("سفارش ساخت")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public ProductionOrder? ProductionOrder { get; set; }
		public long? ProductionOrderId { get; set; }

		[DisplayName("قلم سفارش ساخت")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public ProductionOrderItem? ProductionOrderItem { get; set; }

		public long? ProductionOrderItemId { get; set; }

		[DisplayName("کالا")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public Part? Part { get; set; }

		public long? PartId { get; set; }

 
		[MaxLength(16)]
		[DisplayName("سریال")]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string Serial { get; set; }

		[DisplayName("تعداد کسری")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]
		public decimal? DeficitCount { get; set; }

		[DisplayName("درصد پیشرفت دستگاه")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? DeviceProgressPercentage { get; set; }

		[DisplayName("اولویت دستگاه")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public int? DevicePriority { get; set; }

		[DisplayName("تاریخ میلادی تحویل استاندارد")]
		[DisplayInfo(null, false, SystemType.DateTime)]
		public DateTime? StandardDeliveryMiladiDate { get; set; }

		[DisplayName("تاریخ شمسی تحویل استاندارد")]
		[DisplayInfo("StandardDeliveryMiladiDate", true, SystemType.DateShamsi)]
		[MaxLength(30)]
		public string? StandardDeliveryShamsiDateTime { get; set; }

		[DisplayInfo(null, true, type: SystemType.String)]
		[DisplayName("واحد اجرایی")]
		[StringLength(64)]
		public string? ExecutiveUnit { get; set; }

		[DisplayName("تاریخ میلادی صدور درخواست خرید")]
		[DisplayInfo(null, false, SystemType.DateTime)]
		public DateTime? PurchaseRequestIssuanceMiladiDate { get; set; }

		[DisplayName("تاریخ شمسی صدور درخواست خرید")]
		[DisplayInfo("SPurchaseRequestIssuanceMiladiDate", true, SystemType.DateShamsi)]
		[MaxLength(30)]
		public string? PurchaseRequestIssuanceShamsiDateTime { get; set; }



		[DisplayName("تاریخ میلادی تایید درخواست خرید")]
		[DisplayInfo(null, false, SystemType.DateTime)]
		public DateTime? PurchaseRequestConfirmationMiladiDate { get; set; }


		[DisplayName("تاریخ شمسی تایید درخواست خرید")]
		[DisplayInfo("PurchaseRequestConfirmationMiladiDate", true, SystemType.DateShamsi)]
		[MaxLength(30)]
		public string? PurchaseRequestConfirmationShamsiDateTime { get; set; }


		[DisplayName("تاریخ میلادی تایید درخواست خرید")]
		[DisplayInfo(null, false, SystemType.DateTime)]
		public DateTime? PurchaseRequestNeedMiladiDate { get; set; }


		[DisplayName("تاریخ شمسی تایید درخواست خرید")]
		[DisplayInfo("PurchaseRequestNeedMiladiDate", true, SystemType.DateShamsi)]
		[MaxLength(30)]
		public string? PurchaseRequestNeedShamsiDateTime { get; set; }




		[DisplayName("تعداد خرید در راه")]
		[DisplayInfo(null, true, type: SystemType.Decimal)]

		public decimal? OnWayPurchasedCount { get; set; }


		[DisplayName("تاریخ میلادی پیش بینی تامین واحد تامین کننده-گزارش قبل")]
		[DisplayInfo(null, false, SystemType.DateTime)]
		public DateTime? PreReportSupplierUnitForecastMiladiDate { get; set; }


		[DisplayName("تاریخ شمسی پیش بینی تامین واحد تامین کننده-گزارش قبل")]
		[DisplayInfo("PreReportSupplierUnitForecastMiladiDate", true, SystemType.DateShamsi)]
		[MaxLength(30)]
		public string? PreReportSupplierUnitForecastShamsiDateTime { get; set; }



		[DisplayName("تاریخ میلادی  پیش بینی تامین واحد تامین کننده-گزارش دو هفته قبل")]
		[DisplayInfo(null, false, SystemType.DateTime)]
		public DateTime? LastTwoWeeksForecastSupplierUnitMiladiDate { get; set; }


		[DisplayName("تاریخ شمسی  پیش بینی تامین واحد تامین کننده-گزارش دو هفته قبل")]
		[DisplayInfo("LastTwoWeeksForecastSupplierUnitMiladiDate", true, SystemType.DateShamsi)]
		[MaxLength(30)]
		public string? LastTwoWeeksForecastSupplierShamsiDateTime { get; set; }

		[DisplayName("تاریخ میلادی پیش بینی تامین واحد تامین کننده")]
		[DisplayInfo(null, false, SystemType.DateTime)]
		public DateTime? SupplierUnitForecastDateUnitMiladiDate { get; set; }


		[DisplayName("تاریخ شمسی پیش بینی تامین واحد تامین کننده")]
		[DisplayInfo("SupplierUnitForecastDateUnitMiladiDate", true, SystemType.DateShamsi)]
		[MaxLength(30)]
		public string? SupplierUnitForecastDateShamsiDateTime { get; set; }

		  
 
		[DisplayName("توضیح تامین کننده")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1024)]
		public string? SupplierDescription { get; set; }


		[DisplayName("متولی خرید")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(64)]
		public string? PurchaseExpert { get; set; }



		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2048)]
		public string? Description { get; set; }


	}
}
