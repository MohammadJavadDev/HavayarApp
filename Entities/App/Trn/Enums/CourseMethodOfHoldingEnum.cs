using System.ComponentModel.DataAnnotations;

namespace Entities.App.Trn.Enums
{
	/// <summary>
	/// نحوه برگزاری — معادل legacy Gnr_Lookup با LookupType_FK = 316 (روش برگزاری).
	/// مقادیر از دیتابیس legacy HTS تأیید شده است (2026-09-09).
	/// </summary>
	public enum CourseMethodOfHoldingEnum
	{
		[Display(Name = "اختصاصی")]
		Dedicated = 2379,

		[Display(Name = "آنلاین")]
		Online = 2380,

		[Display(Name = "در محل شرکت هوایار")]
		AtHavayarCompany = 2381
	}
}
