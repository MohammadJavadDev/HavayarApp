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
	[ControllerInfo("پیوست سریال حواله فروش", typeof(OrderDetailSerialAttachment))]
	public class OrderDetailSerialAttachmentController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(OrderDetailSerialAttachment model, CancellationToken cn)
		{
			model.OrderDetailSerial = null;
			model.File = null;
			if (model.Id == null || model.Id == 0)
				return await Add(model, cn);
			if (await unitOfWork.Repository<OrderDetailSerialAttachment>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(OrderDetailSerialAttachment model, CancellationToken cn)
		{
			model.OrderDetailSerial = null;
			model.File = null;
			return Ok(await unitOfWork.Repository<OrderDetailSerialAttachment>().SaveAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(OrderDetailSerialAttachment model, CancellationToken cn)
		{
			model.OrderDetailSerial = null;
			model.File = null;
			return Ok(await unitOfWork.Repository<OrderDetailSerialAttachment>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<OrderDetailSerialAttachment>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null)
				await unitOfWork.Repository<OrderDetailSerialAttachment>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? orderDetailSerialId = null)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<OrderDetailSerialAttachment>().TableNoTracking.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Sale\OrderDetailSerialAttachment\Edit.cshtml", entity ?? new OrderDetailSerialAttachment { OrderDetailSerialId = orderDetailSerialId });
			}
			return View(@"\Views\Panel\Sale\OrderDetailSerialAttachment\Edit.cshtml", new OrderDetailSerialAttachment { OrderDetailSerialId = orderDetailSerialId });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? orderDetailSerialId = null)
			=> View(@"\Views\Panel\Sale\OrderDetailSerialAttachment\Edit.cshtml", new OrderDetailSerialAttachment { OrderDetailSerialId = orderDetailSerialId });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sale\OrderDetailSerialAttachment\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("لیست پیوست‌های سریال", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long orderDetailSerialId)
		{
			ViewBag.OrderDetailSerialId = orderDetailSerialId;
			return View(@"\Views\Panel\Sale\OrderDetailSerialAttachment\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت پیوست‌های سریال", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? orderDetailSerialId, CancellationToken cn)
		{
			if (orderDetailSerialId == null || orderDetailSerialId == 0)
				return BadRequest("شناسه سریال خالی است");

			var items = await unitOfWork.Repository<OrderDetailSerialAttachment>().TableNoTracking
				.Where(c => c.OrderDetailSerialId == orderDetailSerialId)
				.Select(c => new
				{
					c.Id,
					c.OrderDetailSerialId,
					c.Title,
					c.FileId,
					FileName = c.File != null ? c.File.OriginalName : null
				})
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
				await unitOfWork.Repository<OrderDetailSerialAttachment>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<OrderDetailSerialAttachment>().FetchDataAsync(request, cn));
	}
}
