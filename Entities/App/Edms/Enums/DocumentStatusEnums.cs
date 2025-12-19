using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.App.Edms.Enums
{
	public enum DocumentStatusEnums
	{
		 
		[Display(Name = "Reject رد شده")]
		Reject = 1,

		[Display(Name = "Approve تایید شده")]
		Approve = 2,

		[Display(Name = "ApprovedAsNote تایید با یادداشت")]
		ApprovedAsNote = 3,

		[Display(Name = "Commented نظر داده شده")]
		Commented = 4,

		[Display(Name = "Issue صادر شده")]
		Issue = 5,

		[Display(Name = "ApprovedByDcc تایید توسط DCC")]
		ApprovedByDcc = 6,

		[Display(Name = "RejectByEmployer رد توسط کارفرما")]
		RejectByEmployer = 7,

		[Display(Name = "ApproveByEmployer تایید توسط کارفرما")]
		ApproveByEmployer = 8,

		[Display(Name = "ApprovedAsNoteByEmployer تایید با یادداشت توسط کارفرما")]
		ApprovedAsNoteByEmployer = 9,

		[Display(Name = "CommentedByEmployer نظر داده شده توسط کارفرما")]
		CommentedByEmployer = 10,

		[Display(Name = "Hold معلق")]
		Hold = 11,

		[Display(Name = "UnHold خارج از انتظار")]
		UnHold = 12,

		[Display(Name = "NotReview بررسی نشده")]
		NotReview = 13,

		[Display(Name = "UnHoldByEmployer خارج از انتظار توسط کارفرما")]
		UnHoldByEmployer = 14,

		[Display(Name = "SendEmailToClient ارسال ایمیل به مشتری")]
		SendEmailToClient = 15,

		[Display(Name = "ReIssued صادر مجدد")]
		ReIssued = 16,

		[Display(Name = "Commited ثبت شده")]
		Commited = 17,

		[Display(Name = "UnCommit لغو ثبت")]
		UnCommit = 18,

		[Display(Name = "ApprovedBySale تایید توسط فروش")]
		ApprovedBySale = 19,

		[Display(Name = "CommentedBySale نظر داده شده توسط فروش")]
		CommentedBySale = 20,

		[Display(Name = "AsBuild طبق ساخت")]
		AsBuild = 21,

		[Display(Name = "ReplaySheet برگه پاسخ")]
		ReplaySheet = 22,

		[Display(Name = "ReplaySheetFromClient برگه پاسخ از مشتری")]
		ReplaySheetFromClient = 23,

		[Display(Name = "IssueForClient صادر برای مشتری")]
		IssueForClient = 24,

		[Display(Name = "RejectedBySale رد توسط فروش")]
		RejectedBySale = 25,

		[Display(Name = "Archived بایگانی شده")]
		Archived = 26,

		[Display(Name = "HoldByProject در انتظار توسط پروژه")]
		HoldByProject = 27,

		[Display(Name = "ConvertToGeneralPackage تبدیل به پکیج عمومی")]
		ConvertToGeneralPackage = 29
 
	}
}
