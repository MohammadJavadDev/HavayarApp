using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Inv;
using Entities.App.Inv.Enums;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Inv/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("مدارک کالا", typeof(PartDocument))]
	public class PartDocumentController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(PartDocument model, CancellationToken cn)
		{
			if (model.Id == null || model.Id == 0)
				return await Add(model, cn);
			if (await unitOfWork.Repository<PartDocument>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(PartDocument model, CancellationToken cn)
		{
			var error = await Prepare(model, cn, isNew: true);
			if (error != null)
				return BadRequest(error);
			return Ok(await unitOfWork.Repository<PartDocument>().SaveAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(PartDocument model, CancellationToken cn)
		{
			var error = await Prepare(model, cn, isNew: false);
			if (error != null)
				return BadRequest(error);
			return Ok(await unitOfWork.Repository<PartDocument>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<PartDocument>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null)
				await unitOfWork.Repository<PartDocument>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? partId = null)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<PartDocument>().TableNoTracking
					.Include(c => c.Attachment)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Inv\PartDocument\Edit.cshtml", entity ?? new PartDocument { PartId = partId ?? 0 });
			}
			return View(@"\Views\Panel\Inv\PartDocument\Edit.cshtml", new PartDocument { PartId = partId ?? 0 });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? partId = null)
			=> View(@"\Views\Panel\Inv\PartDocument\Edit.cshtml", new PartDocument { PartId = partId ?? 0 });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Inv\PartDocument\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long partId)
		{
			ViewBag.ParentId = partId;
			return View(@"\Views\Panel\Inv\PartDocument\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? partId, CancellationToken cn)
		{
			if (partId == null || partId == 0)
				return BadRequest("شناسه کالا خالی است");

			var rows = await unitOfWork.Repository<PartDocument>().TableNoTracking
				.Where(c => c.PartId == partId)
				.Select(c => new
				{
					c.Id,
					c.Type,
					c.Main,
					c.Comment,
					c.OrganizationUnits,
					c.AttachmentId,
					FileName = c.Attachment != null ? c.Attachment.OriginalName : null,
					FileSize = c.Attachment != null ? c.Attachment.Size : (long?)null,
					c.CreatedByName,
					c.CreatedOnShamsiDateTime
				})
				.ToListAsync(cn);

			var items = rows.Select(c => new
			{
				c.Id,
				c.Type,
				TypeTitle = TypeTitle(c.Type),
				MainTitle = c.Main == true ? "بلی" : "خیر",
				c.Comment,
				c.OrganizationUnits,
				c.AttachmentId,
				c.FileName,
				c.FileSize,
				c.CreatedByName,
				c.CreatedOnShamsiDateTime
			});
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
				await unitOfWork.Repository<PartDocument>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			=> Ok(await unitOfWork.Repository<PartDocument>().FetchDataAsync(request, cn));

		private async Task<string?> Prepare(PartDocument model, CancellationToken cn, bool isNew)
		{
			model.Part = null!;
			model.Attachment = null!;

			if (model.PartId == 0)
				return "شناسه کالا خالی است";
			if (model.Type == null)
				return "نوع مدرک را انتخاب کنید";

			if (!isNew && model.Id is > 0)
			{
				var existing = await unitOfWork.Repository<PartDocument>().TableNoTracking
					.Where(c => c.Id == model.Id)
					.Select(c => new { c.HtsId, c.AttachmentId, c.PartId })
					.FirstOrDefaultAsync(cn);
				if (existing == null)
					return "مدرک یافت نشد";
				model.HtsId = existing.HtsId;
				if (model.PartId == 0)
					model.PartId = existing.PartId;
				if (model.AttachmentId == 0)
					model.AttachmentId = existing.AttachmentId;
			}
			else
			{
				model.HtsId = 0;
			}

			if (model.AttachmentId == 0)
				return "پیوست الزامی است";

			return null;
		}

		private static string TypeTitle(PartDocumentTypeEnum? type)
		{
			if (type == null)
				return "-";
			var member = typeof(PartDocumentTypeEnum).GetMember(type.Value.ToString()).FirstOrDefault();
			var display = member?.GetCustomAttribute<DisplayAttribute>();
			return display?.Name ?? type.Value.ToString();
		}
	}
}
