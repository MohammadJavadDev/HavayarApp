using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sale.Enums
{
	public enum ProductionOrderItemCheckStatusEnum
	{
		[Display(Name = "ثبت اولیه")]
		InitialRegistration = 1903,
		[Display(Name = "در کارتابل صنایع (داخلی)")]
		IndustrialDashboardInternal = 1904,
		[Display(Name = "در کارتابل صنایع (خارجی)")]
		ForeignIndustryCatalog = 1905,
		[Display(Name = "نیاز به استعلام")]
		InquiryNeeded = 1906,
		[Display(Name = "نیاز به بررسی وزارت صنایع")]
		IndustrialMinistryReviewRequired = 1908,
		[Display(Name = "ارسال به رئیس کمیته تامین")]
		SendToSupplyCommitteeChair = 1909,
		[Display(Name = "در انتظار تایید مهندسی (مکانیک)")]
		MechanicalEngineeringApprovalPending = 2195,
		[Display(Name = "تایید مهندسی (مکانیک)")]
		EngineeringConfirmationMechanical = 2197,
		[Display(Name = "عدم تایید مهندسی (مکانیک)")]
		MechanicalEngineeringNotApproved = 2198,
		[Display(Name = "عدم تایید مهندسی و  نیاز به بازنگری")]
		EngineeringReassessmentNeeded = 2201,
		[Display(Name = "در انتظار بررسی رئیس کمیته تامین")]
		CommitteeHeadReviewPending = 2202,
		[Display(Name = "منسوخ شده")]
		Deprecated = 2208,
		[Display(Name = "در انتظار تایید مدیر پروژه")]
		AwaitingProjectManagerApproval = 2258,
		[Display(Name = "تایید مدیر پروژه")]
		ProjectManagerApproval = 2259,
		[Display(Name = "عدم تایید مدیر پروژه")]
		ProjectManagerUnapproved = 2260,
		[Display(Name = "عدم تایید مدیر پروژه و  نیاز به بازنگری")]
		ProjectManagerApprovalPending = 2261,
	}
}
