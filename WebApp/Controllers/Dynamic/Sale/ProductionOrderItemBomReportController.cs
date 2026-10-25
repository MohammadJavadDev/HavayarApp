using Common.Attributes;
using Common.Auth.Enums;
using Entities.App.Sale;
using Microsoft.AspNetCore.Mvc;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	/// <summary>
	/// معادل HTS PlanningSystem/ProductionOrderItemBomReport
	/// (گزارش Bom اقلام سفارش ساخت). فیلتر تاریخ بالای صفحه عمداً نیست؛
	/// بازه روی ستون «زمان ویرایش» با فیلتر نمایه داده اعمال می‌شود.
	/// </summary>
	[Route("Panel/ProductionOrderItem")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("گزارش Bom اقلام سفارش ساخت", typeof(ProductionOrderItemBom))]
	public class ProductionOrderItemBomReportController : BaseController
	{
		[HttpGet("BomReport")]
		[ActionDisplayName("گزارش Bom اقلام سفارش ساخت", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult BomReport()
		{
			return View(@"\Views\Panel\Sale\ProductionOrderItem\BomReport.cshtml");
		}
	}
}
