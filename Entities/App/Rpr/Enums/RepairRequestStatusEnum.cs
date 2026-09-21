using System.ComponentModel.DataAnnotations;

namespace Entities.App.Rpr.Enums
{
	public enum RepairRequestStatusEnum
	{
		[Display(Name = "1-در حال انجام")]
		InProgress = 301,
		[Display(Name = "2-توقف")]
		Stopped = 302,
		[Display(Name = "3-غیرقابل تعمیر")]
		Unrepairable = 304,
		[Display(Name = "4-تکمیل شده")]
		Completed = 305,
		[Display(Name = "5-ارسال به پیمانکار")]
		SentToContractor = 1535,
		[Display(Name = "6-در انتظار تایید مشتری")]
		AwaitingCustomerConfirm = 1536,
		[Display(Name = "7-در انتظار خرید قطعه")]
		AwaitingPartPurchase = 1537,
		[Display(Name = "8-در حال تهیه PreCheck")]
		PreCheck = 1774,
		[Display(Name = "9-در انتظار تست")]
		AwaitingTest = 1871,
		[Display(Name = "10-عدم تایید مشتری")]
		CustomerRejected = 2092,
		[Display(Name = "11-تحویل به انبار")]
		DeliveredToWarehouse = 2093,
		[Display(Name = "12-آیتم صوری")]
		UnrealItem = 2236,
		[Display(Name = "13-در انتظار تعمیر")]
		AwaitingRepair = 2413,
		[Display(Name = "14-در انتظار بازدید مشتری")]
		AwaitingCustomerVisit = 3169,
		[Display(Name = "15-توقف-تعیین تکلیف مشتری")]
		StoppedCustomerDecision = 3175,
		[Display(Name = "16-توقف-خرید قطعه")]
		StoppedPartPurchase = 3176
	}
}
