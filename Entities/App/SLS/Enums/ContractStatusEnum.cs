using System.ComponentModel.DataAnnotations;

namespace Entities.App.SLS.Enums
{
	public enum ContractStatusEnum
	{
		[Display(Name = "دریافت شده")]
		Received = 5,
		[Display(Name = "در حال دریافت")]
		Receiving = 4,
		[Display(Name = "در حال تسویه")]
		Settling = 6,
		[Display(Name = "ثبت اولیه")]
		InitialRegistration = 0,
		[Display(Name = "تسویه شده")]
		Settled = 7,
		[Display(Name = "تایید شده")]
		Approved = 1,
		[Display(Name = "بسته شده")]
		Closed = 8,
		[Display(Name = "اختتام یافته")]
		Completed = 3,
		[Display(Name = "ابطال شده")]
		Cancelled = 2,
	}
}
