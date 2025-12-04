using System.ComponentModel.DataAnnotations;

namespace Entities.App.Edms.Enums
{
	public enum ProjectStatusEnum
	{
		[Display(Name = "شروع نشده")]
		StartNotStarted = 0,
		[Display(Name = "در جریان")]
		InProcess = 1,
		[Display(Name = "خاتمه یافته")]
		Completed = 2,
		[Display(Name = "لغو شده")]
		Canceled = 3,
		[Display(Name = "متوقف شده")]
		Stopped = 4,
		[Display(Name = "در انتظار final book")]
		PendingFinalBook = 5,
		[Display(Name = "باخت")]
		Loss = 6,
		[Display(Name = "برد")]
		Win = 7,
	}
}