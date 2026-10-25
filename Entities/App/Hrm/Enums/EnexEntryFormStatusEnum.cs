using System.ComponentModel.DataAnnotations;

namespace Entities.App.Hrm.Enums
{
	/// <summary>وضعیت فرم ورود اضطراری — همان شناسه‌های Lookup در HTS.</summary>
	public enum EnexEntryFormStatusEnum
	{
		[Display(Name = "ثبت اولیه")]
		PreRegister = 766,

		[Display(Name = "ویرایش شده")]
		Edited = 767,

		[Display(Name = "تایید نهایی حراست")]
		SecurityFinalApprove = 861
	}
}
