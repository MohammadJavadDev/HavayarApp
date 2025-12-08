using System.ComponentModel.DataAnnotations;

namespace Entities.App.Bom.Enums
{
	public enum FormulChangeRequestStatusEnum
	{
		[Display(Name = "ثبت اولیه")]
		InitialRegistration = 0,
		[Display(Name = "تایید برنامه ریزی")]
		PlanningApproval = 1,
		[Display(Name = "عدم تایید برنامه ریزی")]
		PlanningReject = 2,
		[Display(Name = "تایید کنترل کیفیت")]
		QcApproval = 3,
		[Display(Name = "عدم تایید کنترل کیفیت")]
		QcReject = 4,
		[Display(Name = "تایید مهندسی")]
		EngineeringApproval = 5,
		[Display(Name = "عدم تایید مهندسی")]
		EngineeringReject = 6,
		[Display(Name = "پایان عملیات")]
		EndOperation = 50,
	}
}
