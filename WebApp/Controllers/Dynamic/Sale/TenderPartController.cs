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
	[ControllerInfo("اقلام مناقصه", typeof(TenderPart))]
	public class TenderPartController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(TenderPart model, CancellationToken cn)
		{
			ClearNav(model);
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<TenderPart>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(TenderPart model, CancellationToken cn)
		{
			ClearNav(model);
			return Ok(await unitOfWork.Repository<TenderPart>().SaveAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(TenderPart model, CancellationToken cn)
		{
			ClearNav(model);
			return Ok(await unitOfWork.Repository<TenderPart>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<TenderPart>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<TenderPart>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? tenderId = null)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<TenderPart>().TableNoTracking.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Sale\TenderPart\Edit.cshtml", entity ?? new TenderPart { TenderId = tenderId });
			}
			return View(@"\Views\Panel\Sale\TenderPart\Edit.cshtml", new TenderPart { TenderId = tenderId });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? tenderId = null)
			=> View(@"\Views\Panel\Sale\TenderPart\Edit.cshtml", new TenderPart { TenderId = tenderId });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sale\TenderPart\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long tenderId)
		{
			ViewBag.ParentId = tenderId;
			return View(@"\Views\Panel\Sale\TenderPart\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? tenderId, CancellationToken cn)
		{
			if (tenderId == null || tenderId == 0)
				return BadRequest("شناسه والد خالی است");
			var items = await unitOfWork.Repository<TenderPart>().TableNoTracking
				.Where(c => c.TenderId == tenderId)
				.Select(c => new
				{
					c.Id,
					c.PartId,
					PartCode = c.Part != null ? c.Part.Code : null,
					PartName = c.Part != null ? c.Part.Name : null,
					ProductGroupTitle = c.ProductGroup != null ? c.ProductGroup.Title : null,
					c.Number,
					c.NotExistProducts
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
				await unitOfWork.Repository<TenderPart>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<TenderPart>().FetchDataAsync(request, cn));

		private static void ClearNav(TenderPart model)
		{
			model.Tender = null;
			model.Part = null;
			model.ProductGroup = null;
		}
	}
}
