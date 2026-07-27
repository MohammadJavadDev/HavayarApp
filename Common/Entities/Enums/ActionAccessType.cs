using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Auth.Enums
{

	public enum ActionAccessType
	{
		[Display(Name = "عملیات موجودیت")]
		EntityAction,
		[Display(Name = "صفحه")]
		View,
		[Display(Name = "Api")]
		Api,
		[Display(Name = "نمایه داده")]
		DataProfile,
		[Display(Name = "ورود اطلاعات")]
		ImportData,
		[Display(Name = "آیتم گزارش")]
		ReportItem,
		[Display(Name = "سایر")]
		Other = 1000
	}
}
