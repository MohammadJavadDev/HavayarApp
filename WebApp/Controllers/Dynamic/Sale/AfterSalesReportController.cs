using Common.Attributes;
using Common.Auth.Enums;
using Microsoft.AspNetCore.Mvc;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	/// <summary>
	/// کاتالوگ گزارش‌های خدمات پس از فروش.
	/// معادل منوی HTS «گزارشات» (صفحه Sale_AllReport، Gnr_Report با System_FK = 14).
	/// هر گزارش بعدی یک SavedQuery با EntityFullName برابر <see cref="ProfileEntityName"/> است
	/// و فقط روی همین صفحه دیده می‌شود.
	/// </summary>
	[Route("panel/sale/aftersalesreport")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("گزارشات")]
	public class AfterSalesReportController : BaseController
	{
		public const string ProfileEntityName = "AfterSalesReports";

		[HttpGet("list")]
		[ActionDisplayName("لیست گزارشات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
			=> View(@"\Views\Panel\Sale\AfterSalesReport\List.cshtml");
	}
}
