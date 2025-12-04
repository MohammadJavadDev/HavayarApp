using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Auth.Enums
{
	public enum ActionAccessItemType
	{
		[Display(Name = "نمایش")]
		Show,
		[Display(Name = "لیست")]
		List,
		[Display(Name = "دریافت اطلاعات")]
		FetchData,
		[Display(Name = "ذخیره")]
		Save,
		[Display(Name = "ایجاد")]
		Create,
		[Display(Name = "ویرایش")]
		Update,
		[Display(Name = "حذف")]
		Delete,
		[Display(Name = " نمایه داده")]
		DataProfile,
		[Display(Name = "ایجاد نمایه داده")]
		DataProfileCreate,
		[Display(Name = "ویرایش نمایه داده")]
		DataProfileEdit,
		[Display(Name = "حذف نمایه داده")]
		DataProfileDelete,
		[Display(Name = "سایر")]
		Custom = 1000
	}
}
