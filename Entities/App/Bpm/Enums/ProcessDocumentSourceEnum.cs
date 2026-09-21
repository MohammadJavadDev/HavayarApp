using System.ComponentModel.DataAnnotations;

namespace Entities.App.Bpm.Enums
{
	/// <summary>
	/// ورودی سند — معادل Gnr_Lookup LookupType 232 (Bpm_RequestSource).
	/// </summary>
	public enum ProcessDocumentSourceEnum
	{
		[Display(Name = "شکایات مشتری")]
		CustomerComplaints = 1740,

		[Display(Name = "جلسات بهبود فرآیند")]
		ProcessImprovementMeetings = 1741,

		[Display(Name = "عدم انطباق ممیزی داخلی/ شخص ثالث")]
		AuditNonconformity = 1742,

		[Display(Name = "پیشنهاد بهبود")]
		ImprovementSuggestion = 1743,

		[Display(Name = "پروژه بهبود/ استراتژیک")]
		ImprovementStrategicProject = 1744,

		[Display(Name = "تجربه مصوب")]
		ApprovedExperience = 1745,

		[Display(Name = "سایر")]
		Other = 1746,

		[Display(Name = "عارضه / مشکل فرآیندی")]
		ProcessProblem = 1915,

		[Display(Name = "جلسات بازنگری مدیریت")]
		ManagementReviewMeetings = 2253
	}
}
