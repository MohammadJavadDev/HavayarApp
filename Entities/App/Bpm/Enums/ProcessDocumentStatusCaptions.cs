namespace Entities.App.Bpm.Enums
{
	/// <summary>
	/// TitleInView برای دکمه/کمبوی گذار. اگر خالی باشد TitleInText (= Display) استفاده می‌شود.
	/// </summary>
	public static class ProcessDocumentStatusCaptions
	{
		public static string GetActionCaption(ProcessDocumentStatusEnum status)
		{
			return status switch
			{
				ProcessDocumentStatusEnum.Reject => "عدم تایید",
				ProcessDocumentStatusEnum.InProgress => "در دست اقدام",
				ProcessDocumentStatusEnum.BpmApproved => "تایید",
				ProcessDocumentStatusEnum.ProcessOwnerCommented => "کامنت",
				ProcessDocumentStatusEnum.ProcessOwnerApproved => "تایید",
				ProcessDocumentStatusEnum.ApproverCommented => "کامنت",
				ProcessDocumentStatusEnum.ApproverApproved => "تایید",
				ProcessDocumentStatusEnum.FinalApproverApproved => "تایید",
				ProcessDocumentStatusEnum.FinalApproverCommented => "کامنت",
				ProcessDocumentStatusEnum.Notified => "صدور ابلاغیه",
				ProcessDocumentStatusEnum.BpmExpertApproved => "ارسال به سرپرست",
				ProcessDocumentStatusEnum.BpmReject => "عدم تایید",
				ProcessDocumentStatusEnum.CancelRequest => "لغو درخواست",
				ProcessDocumentStatusEnum.BpmCancelRequest => "رد درخواست",
				ProcessDocumentStatusEnum.BpmAcceptComments => "قبول کامنت ها و تایید",
				_ => status.ToDisplayFallback()
			};
		}

		private static string ToDisplayFallback(this ProcessDocumentStatusEnum status)
		{
			var member = typeof(ProcessDocumentStatusEnum).GetField(status.ToString());
			var display = member?
				.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false)
				.OfType<System.ComponentModel.DataAnnotations.DisplayAttribute>()
				.FirstOrDefault();
			return display?.Name ?? status.ToString();
		}
	}
}
