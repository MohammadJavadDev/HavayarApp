using System.ComponentModel.DataAnnotations;

namespace Entities.App.Edms.Enums
{
	public enum DocumentLegalHolderEnum
	{
		[Display(Name = "مهندسی")]
		Engineering = 0,
		[Display(Name = "پروژه")]
		ProjectName = 1,
		[Display(Name = "کارفرما")]
		EmployerName = 2,
		[Display(Name = "تدارکات")]
		Supplies = 3,
		[Display(Name = "فروش")]
		Sales = 4,
		[Display(Name = "مدیریت")]
		Management = 5,
		[Display(Name = "تولید")]
		Production = 6,
		[Display(Name = "کنترل کیفیت")]
		QualityControl = 7,
		[Display(Name = "بازرگانی خارجی")]
		ForeignTrader = 8,
		[Display(Name = "خدمات")]
		Services = 9,
		[Display(Name = "پیمانکار")]
		Contractor = 10,
	}
}
