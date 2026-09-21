using System.ComponentModel.DataAnnotations;

namespace Entities.App.Trn.Enums
{
	/// <summary>
	/// جنسیت — معادل legacy Gnr_Lookup با LookupType_FK = 121 («جنسیت»).
	/// فقط دو ردیف در جدول legacy موجود است؛ شناسه عددی هر عضو برابر Lookup_ID
	/// و برچسب فارسی برابر Lookup_Title_Fa است.
	/// </summary>
	public enum GenderEnum
	{
		[Display(Name = "آقا")]
		Male = 695,

		[Display(Name = "خانم")]
		Female = 696
	}
}
