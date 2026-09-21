using System.ComponentModel.DataAnnotations;

namespace Entities.App.Trn.Enums
{
	/// <summary>
	/// شیوه برگزاری (فراخوان دوره) — معادل legacy Gnr_Lookup با LookupType_FK = 113.
	/// در legacy به‌صورت متن آزاد ذخیره می‌شد («فراخوان عمومی» / «اختصاصی»)؛
	/// در سیستم جدید enum واقعی است (شناسه عددی برابر Lookup_ID).
	/// </summary>
	public enum CourseExecutingMethodEnum
	{
		[Display(Name = "فراخوان عمومی")]
		PublicCall = 635,

		[Display(Name = "اختصاصی")]
		Private = 636
	}
}
