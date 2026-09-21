using System.ComponentModel.DataAnnotations;

namespace Entities.App.Trn.Enums
{
	/// <summary>
	/// نوع دوره (CNG و ...) — معادل legacy Gnr_Lookup با LookupType_FK = 123 (نوع دوره سی ان جی).
	/// مقادیر از دیتابیس legacy HTS تأیید شده است (2026-09-09).
	/// </summary>
	public enum CourseTypeEnum
	{
		[Display(Name = "عمومی")]
		General = 0,

		[Display(Name = "آموزش و صدور")]
		IssueTraining = 701,

		[Display(Name = "بازآموزی")]
		Retraining = 702,

		[Display(Name = "گارانتی")]
		Warranty = 703,

		[Display(Name = "آنلاین")]
		Online = 1184,

		[Display(Name = "سمینار آموزشی")]
		TrainingSeminar = 2411,

		[Display(Name = "هوایار سوخت")]
		HavayarFuel = 2412,

		[Display(Name = "سمینار مشتریان حقوقی")]
		LegalCustomersSeminar = 2787,

		[Display(Name = "سمینار مشتریان حقیقی")]
		RealCustomersSeminar = 2788
	}
}
