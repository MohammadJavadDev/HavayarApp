using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sale.Enums
{
	/// <summary>نوع رخداد ثبت‌شده در تاریخچهٔ Bom اقلام سفارش ساخت.</summary>
	public enum ProductionOrderItemBomHistoryEventEnum
	{
		[Display(Name = "ایجاد")]
		Create = 0,

		[Display(Name = "ویرایش")]
		Update = 1,

		[Display(Name = "ملاحظات واحد پروژه/فروش")]
		Consideration = 2,
	}
}
