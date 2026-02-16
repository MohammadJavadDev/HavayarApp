using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sup.Enums
{
	public enum OpenOrderRequestStopCheckingStatusEnum
	{
		[Display(Name = "راه اندازي مجدد درخواست")]
		RetryRequest = 2804,
		[Display(Name = "ثبت توقف مجدد")]
		ReRegisterStop = 2805,
		[Display(Name = "حذف درخواست")]
		DeleteRequest = 2806,
	}
}
