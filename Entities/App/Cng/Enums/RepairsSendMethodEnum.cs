using System.ComponentModel.DataAnnotations;

namespace Entities.App.Cng.Enums
{
	/// <summary>نحوه ارسال تعمیرات CNG — Gnr_Lookup LookupType_FK = 140.</summary>
	public enum RepairsSendMethodEnum
	{
		[Display(Name = "پیش کرایه")]
		PrepaidFreight = 868,

		[Display(Name = "پس کرایه")]
		PostpaidFreight = 869,

		[Display(Name = "تحویل کارشناس")]
		ExpertDelivery = 870,

		[Display(Name = "تحویل نماینده مشتری")]
		CustomerAgentDelivery = 871
	}
}
