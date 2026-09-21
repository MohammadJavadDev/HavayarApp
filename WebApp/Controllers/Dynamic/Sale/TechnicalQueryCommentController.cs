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
	[ControllerInfo("یادداشت پرسش فنی", typeof(TechnicalQueryComment))]
	public class TechnicalQueryCommentController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(TechnicalQueryComment model, CancellationToken cn)
		{
			if (model.UserId == null) model.UserId = CurrentUserId;
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<TechnicalQueryComment>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(TechnicalQueryComment model, CancellationToken cn)
		{
			if (model.UserId == null) model.UserId = CurrentUserId;
			return Ok(await unitOfWork.Repository<TechnicalQueryComment>().SaveAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(TechnicalQueryComment model, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<TechnicalQueryComment>().UpdateAsync(model, cn, true));

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<TechnicalQueryComment>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<TechnicalQueryComment>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? technicalQueryId = null)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<TechnicalQueryComment>().TableNoTracking.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Sale\TechnicalQueryComment\Edit.cshtml", entity ?? new TechnicalQueryComment { TechnicalQueryId = technicalQueryId, UserId = CurrentUserId });
			}
			return View(@"\Views\Panel\Sale\TechnicalQueryComment\Edit.cshtml", new TechnicalQueryComment { TechnicalQueryId = technicalQueryId, UserId = CurrentUserId });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? technicalQueryId = null)
			=> View(@"\Views\Panel\Sale\TechnicalQueryComment\Edit.cshtml", new TechnicalQueryComment { TechnicalQueryId = technicalQueryId, UserId = CurrentUserId });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sale\TechnicalQueryComment\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long technicalQueryId)
		{
			ViewBag.ParentId = technicalQueryId;
			return View(@"\Views\Panel\Sale\TechnicalQueryComment\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? technicalQueryId, CancellationToken cn)
		{
			if (technicalQueryId == null || technicalQueryId == 0)
				return BadRequest("شناسه والد خالی است");
			var items = await unitOfWork.Repository<TechnicalQueryComment>().TableNoTracking
				.Where(c => c.TechnicalQueryId == technicalQueryId)
				.Select(c => new
				{
					c.Id,
					c.Comment,
					UserName = c.User != null ? c.User.Name : c.CreatedByName,
					c.CreatedOnShamsiDateTime
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
				await unitOfWork.Repository<TechnicalQueryComment>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<TechnicalQueryComment>().FetchDataAsync(request, cn));
	}
}
