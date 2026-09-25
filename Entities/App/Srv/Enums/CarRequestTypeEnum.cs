using System.ComponentModel.DataAnnotations;

namespace Entities.App.Srv.Enums
{
	/// <summary>نوع درخواست خودرو — Gnr_Lookup (HTS Srv_CarRequest.TypeId).</summary>
	public enum CarRequestTypeEnum
	{
		[Display(Name = "داخلی و برگشت")]
		InternalRoundTrip = 1009,

		[Display(Name = "برون شهری (به غیر از تهران و کرج)")]
		OutOfTown = 1010,

		[Display(Name = "شهرستان")]
		County = 1011,

		[Display(Name = "در اختیار")]
		AtDisposal = 1012,

		[Display(Name = "داخلی (رفت)")]
		InternalOutbound = 1039
	}
}
