using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Sale;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sale/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("یادداشت نقطه سفارش", typeof(OrderPointComment))]
	public class OrderPointCommentController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(OrderPointComment model, CancellationToken cn)
		{
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<OrderPointComment>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(OrderPointComment model, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<OrderPointComment>().SaveAsync(model, cn, true));

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(OrderPointComment model, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<OrderPointComment>().UpdateAsync(model, cn, true));

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<OrderPointComment>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<OrderPointComment>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? orderPointId = null)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<OrderPointComment>().TableNoTracking.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Sale\OrderPointComment\Edit.cshtml", entity ?? new OrderPointComment { OrderPointId = orderPointId });
			}
			return View(@"\Views\Panel\Sale\OrderPointComment\Edit.cshtml", new OrderPointComment { OrderPointId = orderPointId });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? orderPointId = null)
			=> View(@"\Views\Panel\Sale\OrderPointComment\Edit.cshtml", new OrderPointComment { OrderPointId = orderPointId });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sale\OrderPointComment\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long orderPointId)
		{
			ViewBag.ParentId = orderPointId;
			return View(@"\Views\Panel\Sale\OrderPointComment\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? orderPointId, CancellationToken cn)
		{
			if (orderPointId == null || orderPointId == 0)
				return BadRequest("شناسه والد خالی است");
			var items = await unitOfWork.Repository<OrderPointComment>().TableNoTracking
				.Where(c => c.OrderPointId == orderPointId)
				.Select(c => new { c.Id, c.Comment, c.CreatedByName, c.CreatedOnShamsiDateTime })
				.ToListAsync(cn);
			return Ok(items);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<OrderPointComment>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<OrderPointComment>().FetchDataAsync(request, cn));
	}
}
