using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.App.Epms.Enums
{
	public enum ProposalActivityActivityTypeEnum
	{
		 
		[Display(Name = "ماموریت")]
		Mission = 373,

		[Display(Name = "جلسه")]
		Meeting = 374,

		[Display(Name = "خواندن اسپک")]
		ReadingSpec = 375,

		[Display(Name = "مشاوره مهندسی")]
		EngineeringConsultation = 376,

		[Display(Name = "تست")]
		Test = 377,

		[Display(Name = "بازدید")]
		Visit = 378,

		[Display(Name = "بررسی مدارک وندور")]
		VendorDocumentReview = 379,

		[Display(Name = "بررسی پیشنهاد فنی تامین کنندگان")]
		SupplierTechnicalProposalReview = 380,

		[Display(Name = "ارایه داده های خرید")]
		PurchaseDataSubmission = 381,

		[Display(Name = "خدمات محصولات روتین")]
		RoutineProductServices = 382,

		[Display(Name = "مطالعه")]
		Study = 383,

		[Display(Name = "شرکت در دوره آموزشی")]
		AttendingTrainingCourse = 384,

		[Display(Name = "ارسال مدرک به کارفرما")]
		SendingDocumentToEmployer = 385,

		[Display(Name = "سایر")]
		Other = 401,

		[Display(Name = "کدینگ")]
		Coding = 402,

		[Display(Name = "طراحی و نقشه کشی صنعتی")]
		IndustrialDesignAndDrafting = 403,

		[Display(Name = "تهیه پروپوزال فنی")]
		TechnicalProposalPreparation = 463,

		[Display(Name = "متره و برآورد مالی")]
		QuantitySurveyAndFinancialEstimation = 464,

		[Display(Name = "تهیه مدارک مهندسی")]
		EngineeringDocumentPreparation = 465,

		[Display(Name = "تهیه TCL")]
		TCLPreparation = 466,

		[Display(Name = "برنامه ریزی و کنترل")]
		PlanningAndControl = 467,

		[Display(Name = "DCC")]
		DCC = 468
 
}
}

