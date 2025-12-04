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
		 
		[Display(Name = "رد شده")]
		Reject = 1,

		[Display(Name = "تایید شده")]
		Approve = 2,

		[Display(Name = "تایید با یادداشت")]
		ApprovedAsNote = 3,

		[Display(Name = "نظر داده شده")]
		Commented = 4,

		[Display(Name = "صادر شده")]
		Issue = 5,

		[Display(Name = "تایید توسط DCC")]
		ApprovedByDcc = 6,

		[Display(Name = "رد توسط کارفرما")]
		RejectByEmployer = 7,

		[Display(Name = "تایید توسط کارفرما")]
		ApproveByEmployer = 8,

		[Display(Name = "تایید با یادداشت توسط کارفرما")]
		ApprovedAsNoteByEmployer = 9,

		[Display(Name = "نظر داده شده توسط کارفرما")]
		CommentedByEmployer = 10,

		[Display(Name = "معلق")]
		Hold = 11,

		[Display(Name = "خارج از انتظار")]
		UnHold = 12,

		[Display(Name = "بررسی نشده")]
		NotReview = 13,

		[Display(Name = "خارج از انتظار توسط کارفرما")]
		UnHoldByEmployer = 14,

		[Display(Name = "ارسال ایمیل به مشتری")]
		SendEmailToClient = 15,

		[Display(Name = "صادر مجدد")]
		ReIssued = 16,

		[Display(Name = "ثبت شده")]
		Commited = 17,

		[Display(Name = "لغو ثبت")]
		UnCommit = 18,

		[Display(Name = "تایید توسط فروش")]
		ApprovedBySale = 19,

		[Display(Name = "نظر داده شده توسط فروش")]
		CommentedBySale = 20,

		[Display(Name = "طبق ساخت")]
		AsBuild = 21,

		[Display(Name = "برگه پاسخ")]
		ReplaySheet = 22,

		[Display(Name = "برگه پاسخ از مشتری")]
		ReplaySheetFromClient = 23,

		[Display(Name = "صادر برای مشتری")]
		IssueForClient = 24,

		[Display(Name = "رد توسط فروش")]
		RejectedBySale = 25,

		[Display(Name = "بایگانی شده")]
		Archived = 26,

		[Display(Name = "در انتظار توسط پروژه")]
		HoldByProject = 27,

		[Display(Name = "تبدیل به پکیج عمومی")]
		ConvertToGeneralPackage = 29
 
	}
}
