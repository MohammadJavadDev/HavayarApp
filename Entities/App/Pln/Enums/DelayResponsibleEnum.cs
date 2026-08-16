using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entities.App.Pln.Enums
{
    public enum DelayResponsibleEnum
    {
		[Display(Name = "توقفات صنایع")]
		IndustrialStops = 752,

		[Display(Name = "توقفات مهندسی")]
		EngineeringStops = 753,

		[Display(Name = "توقفات تدارکات")]
		ProcurementStops = 754,

		[Display(Name = "توقفات تولید")]
		ProductionStops = 755,

		[Display(Name = "توقفات تست")]
		TestingStops = 756,

		[Display(Name = "توقفات پیش بینی نشده معاونت اجرایی")]
		UnforeseenExecutiveStops = 757,

		[Display(Name = "توقفات شناسایی نشده معاونت اجرایی")]
		UnidentifiedExecutiveStops = 758,

		[Display(Name = "توقفات فروش (منسوخ شده، استفاده نگردد!)")]
		SalesStopsObsolete = 759,

		[Display(Name = "توقفات بازرگانی")]
		CommercialStops = 760,

		[Display(Name = "توقفات مالی")]
		FinancialStops = 761,

		[Display(Name = "توقفات تولید ناشی از تغذیه نامناسب خطوط")]
		ProductionStopsImproperFeeding = 2376,

		[Display(Name = "توقفات انبار")]
		WarehouseStops = 2422,

		[Display(Name = "توقفات کمپرسورهای مهندسی")]
		EngineeringCompressorStops = 2423,

		[Display(Name = "توقفات فروش صنعتی 1")]
		IndustrialSalesStops1 = 2424,

		[Display(Name = "توقفات فروش صنعتی 2")]
		IndustrialSalesStops2 = 2425,

		[Display(Name = "توقفات فروش صنعتی 3")]
		IndustrialSalesStops3 = 2426,

		[Display(Name = "توقفات فروش گازهای صنعتی")]
		IndustrialGasSalesStops = 2427,

		[Display(Name = "توقفات فروش تجهیزات پزشکی")]
		MedicalEquipmentSalesStops = 2428,

		[Display(Name = "توقفات فروش توربو ماشین")]
		TurboMachineSalesStops = 2429,

		[Display(Name = "توقفات فروش CNG")]
		CNGSalesStops = 2430,

		[Display(Name = "توقفات فروش هوای فشرده")]
		CompressedAirSalesStops = 2431,

		[Display(Name = "توقفات فروش صنعتی")]
		IndustrialSalesStops = 2540
	}
}
