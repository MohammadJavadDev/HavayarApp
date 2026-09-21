using System.ComponentModel.DataAnnotations;

namespace Entities.App.Trn.Enums
{
	/// <summary>
	/// نوع پرداخت — معادل legacy Gnr_Lookup با LookupType_FK = 321 (نوع پرداخت).
	/// مقادیر از دیتابیس legacy HTS تأیید شده است (2026-09-09).
	/// </summary>
	public enum CoursePaymentTypeEnum
	{
		[Display(Name = "رایگان")]
		Free = 2396,

		[Display(Name = "اخذ هزینه")]
		Paid = 2397
	}
}
