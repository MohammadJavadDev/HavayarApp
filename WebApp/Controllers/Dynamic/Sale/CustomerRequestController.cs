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
	[ControllerInfo("درخواست مشتریان", typeof(CustomerRequest))]
	public class CustomerRequestController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sale\CustomerRequest\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id == null || id == 0)
				return RedirectToAction(nameof(List));
			var entity = unitOfWork.Repository<CustomerRequest>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity == null)
				return RedirectToAction(nameof(List));
			return View(@"\Views\Panel\Sale\CustomerRequest\Edit.cshtml", entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<CustomerRequest>().FetchDataAsync(request, cn));

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<CustomerRequest>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[HttpGet("[action]")]
		[ActionDisplayName("پیوست", ActionAccessType.View, ActionAccessItemType.Show)]
		public async Task<IActionResult> DownloadAttachment(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<CustomerRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity?.AttachmentFile == null || entity.AttachmentFile.Length == 0)
				return NotFound();
			var name = string.IsNullOrWhiteSpace(entity.AttachmentFileName) ? "attachment.bin" : entity.AttachmentFileName;
			return File(entity.AttachmentFile, "application/octet-stream", name);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("نظرات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetComments(long parentId, CancellationToken cn)
		{
			var items = await unitOfWork.Repository<CustomerRequestComment>().TableNoTracking
				.Where(c => c.CustomerRequestId == parentId)
				.OrderByDescending(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.Comment,
					Status = c.StatusId,
					c.CreatedByName,
					c.CreatedOnShamsiDateTime
				})
				.ToListAsync(cn);
			return Ok(items);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ثبت نظر", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> AddComment(CustomerRequestComment model, CancellationToken cn)
		{
			model.CustomerRequest = null;
			if (model.CustomerRequestId == null || model.CustomerRequestId == 0)
				return BadRequest("شناسه درخواست خالی است.");
			if (string.IsNullOrWhiteSpace(model.Comment))
				return BadRequest("نظر نمی‌تواند خالی باشد.");

			var saved = await unitOfWork.Repository<CustomerRequestComment>().SaveAsync(model, cn, true);
			if (model.StatusId != null)
			{
				var parent = await unitOfWork.Repository<CustomerRequest>().Table
					.FirstOrDefaultAsync(c => c.Id == model.CustomerRequestId, cn);
				if (parent != null)
				{
					parent.StatusId = model.StatusId;
					await unitOfWork.Repository<CustomerRequest>().UpdateAsync(parent, cn, true);
				}
			}
			return Ok(saved);
		}
	}
}
