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
	[ControllerInfo("مشتریان حقیقی (شرکت‌کنندگان)", typeof(Participant))]
	public class ParticipantController(IUnitOfWork unitOfWork, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(Participant participant, CancellationToken cn)
		{
			if (participant.Id == null || participant.Id == 0)
			{
				return await Add(participant, cn);
			}
			var exist = await unitOfWork.Repository<Participant>().TableNoTracking.AnyAsync(c => c.Id == participant.Id);
			if (exist)
			{
				return await Update(participant, cn);
			}
			return await Add(participant, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(Participant participant, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<Participant>().SaveAsync(participant, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(Participant participant, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<Participant>().UpdateAsync(participant, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<Participant>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<Participant>().DeleteAsync(model, cn, true);
			return Ok();
		}

		// Duplicate-name warning (legacy behavior — client-side warning only, non-blocking).
		// The Edit page calls this before saving a new row; the save always proceeds.
		[HttpGet("[action]")]
		[ActionDisplayName("بررسی تکراری بودن نام", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> CheckDuplicateName(string? firstName, string? lastName, long? excludeId, CancellationToken cn)
		{
			if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName))
				return Ok(new { isDuplicate = false });

			var duplicate = await unitOfWork.Repository<Participant>().TableNoTracking
				.AnyAsync(p => p.FirstName == firstName && p.LastName == lastName
					&& (excludeId == null || p.Id != excludeId), cn);
			return Ok(new { isDuplicate = duplicate });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<Participant>().TableNoTracking
					.Include(p => p.Company)
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Trn\Participant\Edit.cshtml", entity);
			}
			var newEntity = new Participant();
			return View(@"\Views\Panel\Trn\Participant\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new Participant();
			return View(@"\Views\Panel\Trn\Participant\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Trn\Participant\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<Participant>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<Participant>().FetchDataAsync(request, cn));
		}
	}
}
