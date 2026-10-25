using Common.Attributes;
using Common.Auth.Enums;
using Microsoft.AspNetCore.Mvc;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic.Hrm
{
	/// <summary>
	/// گزارش سابقه حضور پرسنل — معادل HTS صفحه EmployeeStatute (346) / hrm-employeestatute.
	/// نام یکتا نمایه: Hrm_EmployeeStatuteReport
	/// </summary>
	[Route("Panel/Hrm/EmployeeStatuteReport")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("گزارش سابقه حضور پرسنل")]
	public class HrmEmployeeStatuteReportController : BaseController
	{
		public const string ProfileUnicode = "HrmEmployeeStatuteReport";
		public const string ProfileName = "Hrm_EmployeeStatuteReport";

		[HttpGet("List")]
		[ActionDisplayName("گزارش سابقه حضور پرسنل", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
			=> View(@"\Views\Panel\Hrm\EmployeeStatuteReport\List.cshtml");
	}
}
