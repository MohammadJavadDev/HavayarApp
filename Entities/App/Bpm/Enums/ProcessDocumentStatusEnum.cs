using System.ComponentModel.DataAnnotations;

namespace Entities.App.Bpm.Enums
{
	/// <summary>
	/// وضعیت سند فرآیندی — منبع حقیقت جدول Bpm_DocumentStatus (نه کامنت‌های معکوس enum در HTS).
	/// Display = TitleInText. شناسه‌های ۳–۸ و ۲۵ وجود ندارند.
	/// ۱۶ = FinalApproverApproved / «تایید، تصویب کننده»؛ ۱۷ = FinalApproverCommented / «کامنت، تصویب کننده».
	/// </summary>
	public enum ProcessDocumentStatusEnum : byte
	{
		[Display(Name = "ایجاد شده")]
		Issue = 1,

		[Display(Name = "ویرایش شده")]
		Updated = 2,

		[Display(Name = "رد شده")]
		Reject = 9,

		[Display(Name = "در جریان")]
		InProgress = 10,

		[Display(Name = "در انتظار تایید مالک")]
		BpmApproved = 11,

		[Display(Name = "کامنت مالک فرآیند")]
		ProcessOwnerCommented = 12,

		[Display(Name = "در انتظار تایید")]
		ProcessOwnerApproved = 13,

		[Display(Name = "کامنت، تایید کننده")]
		ApproverCommented = 14,

		[Display(Name = "در انتظار تصویب")]
		ApproverApproved = 15,

		[Display(Name = "تایید، تصویب کننده")]
		FinalApproverApproved = 16,

		[Display(Name = "کامنت، تصویب کننده")]
		FinalApproverCommented = 17,

		[Display(Name = "ابلاغ شده")]
		Notified = 18,

		[Display(Name = "جمع بندی نظرات ذینفعان")]
		BpmExpertApproved = 19,

		[Display(Name = "ویراش سیستم ها و روش ها")]
		BpmUpdated = 20,

		[Display(Name = "عدم تایید سرپرست")]
		BpmReject = 21,

		[Display(Name = "لغو درخواست")]
		CancelRequest = 22,

		[Display(Name = "رد درخواست")]
		BpmCancelRequest = 23,

		[Display(Name = "قبول کامنت ها")]
		BpmAcceptComments = 24,

		[Display(Name = "عدم گردش فرآیند و ابلاغ")]
		IgnoreProcessAndForceToNotify = 26
	}
}
