using System.ComponentModel;

namespace Entities.Base.System.Enums;

public enum PageKindEnum
{
	[Description("سفارشی")]
	Custom = 0,

	[Description("لیست")]
	List = 1,

	[Description("درج/ویرایش")]
	Edit = 2,

	[Description("Partial")]
	Partial = 3
}
