using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sup.Enums
{
	/// <summary>
	/// نوع توقف درخواست باز
	/// این Enum معادل Lookup با LookupType_FK = 346 در سیستم قدیم است
	/// </summary>
	public enum OpenOrderRequestStopTypeEnum
	{

		[Display(Name = "توقف مربوط به واحد مهندسی")]
		EngineeringUnitStop = 2796,

		[Display(Name = "توقف مربوط به واحد صنایع")]
		IndustrialUnitStop = 2797,
		 
		[Display(Name = "ساير موارد")]
		OtherStop = 2798,
 
	}
}
