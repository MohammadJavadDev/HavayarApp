using Common.Attributes;
using Common.Utilities;
using Common.Auth.Enums;
using Entities.App.Inv;
using Microsoft.AspNetCore.Mvc;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	/// <summary>
	/// کاردکس کالا — معادل HTS صفحه Inv_Part_Cardex (PartCardexManagementController).
	/// فقط نمایش و خروجی اکسل از طریق نمایه داده؛ اکشن دیگری وجود ندارد.
	/// </summary>
	[Route("Panel/Inv/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("کاردکس کالا", typeof(PartCardex))]
	public class PartCardexController : BaseController
	{
		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Inv\PartCardex\List.cshtml");
		}

		/// <summary>
		/// کنترل موجودی (برنامه‌ریزی) — معادل HTS صفحه planning-inventoryControl (InventoryControlController).
		/// گرید با نمایه داده Pln_InventoryControl_Report / Pln_InventoryControl_OtherUnits_Report پر می‌شود.
		/// </summary>
		[HttpGet("[action]")]
		[ActionDisplayName("کنترل موجودی", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult InventoryControl()
		{
			var today = DateTime.Now.ToShamsiDate();
			ViewBag.ToDate = today;
			ViewBag.FromDate = (today.Length >= 4 ? today[..4] : today) + "/01/01";
			return View(@"\Views\Panel\Inv\PartCardex\InventoryControl.cshtml");
		}
	}
}
