using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entities.App.Edms.Enums
{
    public enum ProjectAttachmentTypeEnum
    {
		[Display(Name = "پروپزال")]
		Proposal = 257,

		[Display(Name = "متره")]
		Metre = 258,

		[Display(Name = "Spec")]
		Spec = 259,

		[Display(Name = "MOM")]
		MOM = 260,

		[Display(Name = "سفارش ساخت")]
		ProductionOrder = 261,

		[Display(Name = "قرارداد")]
		Contract = 262,

		[Display(Name = "داکیومنت")]
		Document = 262,


		[Display(Name = "فرمت مدارک")]
		DocumentsFormat = 471,


	}
}
