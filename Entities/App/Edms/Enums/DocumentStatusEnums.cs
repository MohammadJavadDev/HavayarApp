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

		[Display(Name = "NotIssue صادر نشده")]
		NotIssue = 1,

		[Display(Name = "Issue صادر شده")]
		Issue = 2,

		[Display(Name = "RejectByReviewer  رد شده بررسی کننده")]
		RejectByReviewer = 3,

		[Display(Name = "ApproveByReviewer  تایید شده بررسی کننده")]
		ApproveByReviewer = 4,
		 
		[Display(Name = "CommentedByReviewer  نظر داده شده بررسی کننده")]
		CommentedByReviewer = 5,


		[Display(Name = "RejectByApprover  رد شده تایید کننده")]
		RejectByApprover = 6,

		[Display(Name = "ApproveByApprover  تایید شده تایید کننده")]
		ApproveByApprover = 7,

		[Display(Name = "CommentedByApprover  نظر داده شده تایید کننده")]
		CommentedByApprover = 8 ,



		[Display(Name = "ApprovedByDcc تایید DCC")]
		ApprovedByDcc = 9,

		[Display(Name = "RejectByDcc رد شده DCC")]
		RejectByDcc = 10,

		[Display(Name = "CommentedByDcc  نظر داده شده DCC")]
		CommentedByDcc = 11,


		[Display(Name = "RejectByClient رد توسط کارفرما")]
		RejectByClient = 12,

		[Display(Name = "ApproveByClient تایید توسط کارفرما")]
		ApproveByClient = 13,

		[Display(Name = "ApprovedAsNoteClient تایید با یادداشت توسط کارفرما")]
		ApprovedAsNoteByClient = 14,

		[Display(Name = "CommentedByClient نظر داده شده توسط کارفرما")]
		CommentedByClient = 15,

		[Display(Name = "Hold معلق")]
		Hold = 16,

		[Display(Name = "UnHold خارج از انتظار")]
		UnHold = 17,

		[Display(Name = "NotReview بررسی نشده")]
		NotReview = 18,

	 

		[Display(Name = "SendEmailToClient ارسال ایمیل به مشتری")]
		SendEmailToClient = 20,

		[Display(Name = "ReIssued صادر مجدد")]
		ReIssued = 21,

		[Display(Name = "Commited ثبت شده")]
		Commited = 22,

		[Display(Name = "UnCommit لغو ثبت")]
		UnCommit = 23,


		[Display(Name = "ApprovedBySale تایید توسط فروش")]
		ApprovedBySale = 24,

		[Display(Name = "CommentedBySale نظر داده شده توسط فروش")]
		CommentedBySale = 25,
		[Display(Name = "RejectedBySale رد توسط فروش")]
		RejectedBySale = 26,


		[Display(Name = "AsBuild طبق ساخت")]
		AsBuild = 27,

		[Display(Name = "ReplaySheet برگه پاسخ")]
		ReplaySheet = 28,

		[Display(Name = "ReplaySheetFromClient برگه پاسخ از مشتری")]
		ReplaySheetFromClient = 29,

		[Display(Name = "IssueForClient صادر برای مشتری")]
		IssueForClient = 30,



		[Display(Name = "Archived بایگانی شده")]
		Archived = 31,

		[Display(Name = "HoldByProject در انتظار توسط پروژه")]
		HoldByProject = 32,

		[Display(Name = "ConvertToGeneralPackage تبدیل به پکیج عمومی")]
		ConvertToGeneralPackage = 33,

		[Display(Name = "CommentedInternal  نظر داده شده داخلی")]
		CommentedInternal = 34,
		 
	 

		[Display(Name = "ApprovalInternal  تایید داخلی")]
		ApprovalInternal = 37,

		[Display(Name = "ReviewIssuer  بررسی شده توسط صادر کننده")]
		ReviewIssuer = 38,

		 




	}
}
