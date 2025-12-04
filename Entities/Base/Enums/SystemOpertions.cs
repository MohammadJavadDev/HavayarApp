using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Base.Enums
{
	public enum SystemOpertions
	{
		[Display(Name = "ایجاد")]
		Create,
		[Display(Name = "ویرایش")]
		Edit,
		[Display(Name = "حذف")]
		Delete,
	}
}
