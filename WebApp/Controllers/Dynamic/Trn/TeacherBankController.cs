using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Trn;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Trn/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("بانک مدرسین", typeof(TeacherBank))]
	public class TeacherBankController(IUnitOfWork unitOfWork, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(TeacherBank teacherBank, CancellationToken cn)
		{
			if (teacherBank.Id == null || teacherBank.Id == 0)
			{
				return await Add(teacherBank, cn);
			}
			var exist = await unitOfWork.Repository<TeacherBank>().TableNoTracking.AnyAsync(c => c.Id == teacherBank.Id);
			if (exist)
			{
				return await Update(teacherBank, cn);
			}
			return await Add(teacherBank, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(TeacherBank teacherBank, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<TeacherBank>().SaveAsync(teacherBank, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(TeacherBank teacherBank, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<TeacherBank>().UpdateAsync(teacherBank, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<TeacherBank>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<TeacherBank>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<TeacherBank>().TableNoTracking
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Trn\TeacherBank\Edit.cshtml", entity);
			}
			var newEntity = new TeacherBank();
			return View(@"\Views\Panel\Trn\TeacherBank\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new TeacherBank();
			return View(@"\Views\Panel\Trn\TeacherBank\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Trn\TeacherBank\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<TeacherBank>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		[HttpPost("[action]")]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
		{
			return Ok(await unitOfWork.Repository<TeacherBank>().FetchDataAsync(request, cn));
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetAttachments(long? teacherBankId, CancellationToken cn)
		{
			if (teacherBankId == null || teacherBankId == 0)
				return BadRequest("شناسه مدرس نمیتواند خالی باشد.");

			var items = await unitOfWork.Repository<TeacherBankAttachment>()
				.TableNoTracking
				.Where(c => c.TeacherBankId == teacherBankId)
				.Select(c => new TeacherBankAttachmentListItemViewModel
				{
					Id = c.Id,
					TeacherBankId = c.TeacherBankId,
					Title = c.Title,
					FileId = c.FileId,
					FileName = c.File != null ? c.File.OriginalName : null
				})
				.ToListAsync(cn);

			return Ok(items);
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> SaveAttachment(TeacherBankAttachment attachment, CancellationToken cn)
		{
			if (attachment.TeacherBankId <= 0)
				return BadRequest("شناسه مدرس نامعتبر است");

			if (attachment.Id == null || attachment.Id == 0)
			{
				var entity = await unitOfWork.Repository<TeacherBankAttachment>().SaveAsync(attachment, cn, true);
				return Ok(entity);
			}

			var updated = await unitOfWork.Repository<TeacherBankAttachment>().UpdateAsync(attachment, cn, true);
			return Ok(updated);
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> DeleteAttachment(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<TeacherBankAttachment>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<TeacherBankAttachment>().DeleteAsync(model, cn, true);
			return Ok();
		}
	}
}
