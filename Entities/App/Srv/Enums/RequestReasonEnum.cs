using System.ComponentModel.DataAnnotations;

namespace Entities.App.Srv.Enums
{
	/// <summary>علت درخواست بلیط و هتل — Gnr_Lookup LookupType_FK = 195.</summary>
	public enum RequestReasonEnum
	{
		[Display(Name = "بازدید")]
		Visit = 1295,

		[Display(Name = "ارائه خدمات")]
		ServiceDelivery = 1296,

		[Display(Name = "کنترل کیفیت")]
		QualityControl = 1297,

		[Display(Name = "آموزش")]
		Training = 1298,

		[Display(Name = "سایر")]
		Other = 1299
	}
}
