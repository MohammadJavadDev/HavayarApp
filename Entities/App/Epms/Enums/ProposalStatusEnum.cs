using System.ComponentModel.DataAnnotations;

namespace Entities.App.Epms.Enums
{
	public enum ProposalStatusEnum
	{
		[Display(Name = "شروع نشده")]
		NotStarted = 3,
		[Display(Name = "در جریان")]
		InProgress =4,
		[Display(Name = "خاتمه یافته")]
		Terminated =5,
		[Display(Name = "لغو شده")]
		Cancelled =6,
		[Display(Name = "متوقف شده")]
		Stopped =7,
		[Display(Name = "باخت")]
		Loss =2748,
		[Display(Name = "برد")]
		Win =2773,
		[Display(Name = "منتظر برسی توسط متره")]
		SendToMetre = 2774,
		[Display(Name = "منتظر برسی مدارک متره توسط فروش")]
		WaitingSellerCheckMeterDocuments = 2775

	}
}