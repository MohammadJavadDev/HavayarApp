using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Hcm;
using Entities.App.Hrm;
using Entities.App.Hrm.Enums;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.FileServices;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Hrm/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("ورود/خروج پرسنل/پیمانکار", typeof(GuardPersonIo))]
	public class GuardPersonIoController(IUnitOfWork unitOfWork, IFileService fileService, IWebHostEnvironment env) : BaseController
	{
		#region CRUD

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(GuardPersonIo model, CancellationToken cn)
		{
			var error = Validate(model);
			if (error != null) return BadRequest(error);

			await ApplyOperatorAsync(model, cn);
			model.Id = null;
			model.HtsId = 0;
			model.HtsEntryFilePath = null;
			model.ExitFileId = null;

			var saved = await unitOfWork.Repository<GuardPersonIo>().SaveAsync(model, cn, true);
			return Ok(await LoadForEditAsync(saved.Id!.Value, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(GuardPersonIo model, CancellationToken cn)
		{
			var error = Validate(model);
			if (error != null) return BadRequest(error);

			var old = await unitOfWork.Repository<GuardPersonIo>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (old == null) return BadRequest("رکورد یافت نشد");

			await ApplyOperatorAsync(model, cn);
			model.HtsId = old.HtsId;
			model.HtsEntryFilePath = old.HtsEntryFilePath;
			await ReplaceFileAsync(old.EntryFileId, model.EntryFileId, cn);
			await ReplaceFileAsync(old.ExitFileId, model.ExitFileId, cn);

			var saved = await unitOfWork.Repository<GuardPersonIo>().UpdateAsync(model, cn, true);
			return Ok(await LoadForEditAsync(saved.Id!.Value, cn));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<GuardPersonIo>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return Ok();

			await unitOfWork.Repository<GuardPersonIo>().DeleteAsync(entity, cn, true);
			if (entity.EntryFileId is > 0)
				await fileService.DeleteAsync(entity.EntryFileId.Value, cn);
			if (entity.ExitFileId is > 0)
				await fileService.DeleteAsync(entity.ExitFileId.Value, cn);
			return Ok();
		}

		#endregion

		#region Pages

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			var entity = (id != null && id != 0) ? await LoadForEditAsync(id.Value, cn) : null;
			return View(@"\Views\Panel\Hrm\GuardPersonIo\Edit.cshtml", entity ?? new GuardPersonIo());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var now = DateTime.Now;
			return View(@"\Views\Panel\Hrm\GuardPersonIo\Edit.cshtml", new GuardPersonIo
			{
				EntryMiladiDate = now.Date,
				EntryShamsiDate = now.ToShamsiDate(),
				EntryTime = now.ToString("HH:mm")
			});
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Hrm\GuardPersonIo\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<GuardPersonIo>().FetchDataAsync(request, cn));

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + @"\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<GuardPersonIo>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		#endregion

		#region Private

		private static string? Validate(GuardPersonIo model)
		{
			if (!Enum.IsDefined(model.OperatorType) || model.OperatorType == GuardOperatorTypeEnum.Miscellaneous)
				return "نوع نمی‌تواند خالی باشد";

			if (model.OperatorType == GuardOperatorTypeEnum.Personnel && model.PersonId is null or 0)
				return "پرسنل نمی‌تواند خالی باشد";
			if (model.OperatorType == GuardOperatorTypeEnum.Contractor && model.ContractorId is null or 0)
				return "پیمانکار نمی‌تواند خالی باشد";

			if (HasDate(model.EntryShamsiDate, model.EntryMiladiDate) && !HasTime(model.EntryTime))
				return "ساعت ورود نمی‌تواند خالی باشد";
			if (HasDate(model.ExitShamsiDate, model.ExitMiladiDate) && !HasTime(model.ExitTime))
				return "ساعت خروج نمی‌تواند خالی باشد";

			return null;
		}

		private static bool HasDate(string? shamsi, DateTime? miladi)
			=> !string.IsNullOrWhiteSpace(shamsi) || miladi.HasValue;

		private static bool HasTime(string? time)
		{
			if (string.IsNullOrWhiteSpace(time)) return false;
			return time.Replace("_", "", StringComparison.Ordinal).Replace(":", "", StringComparison.Ordinal).Trim().Length > 0;
		}

		private async Task ApplyOperatorAsync(GuardPersonIo model, CancellationToken cn)
		{
			if (model.PersonId is 0) model.PersonId = null;
			if (model.ContractorId is 0) model.ContractorId = null;
			if (model.EntryFileId is 0) model.EntryFileId = null;
			if (model.ExitFileId is 0) model.ExitFileId = null;

			model.Person = null;
			model.Unit = null;
			model.Contractor = null;
			model.EntryFile = null;
			model.ExitFile = null;

			if (model.OperatorType == GuardOperatorTypeEnum.Personnel)
			{
				model.ContractorId = null;
				model.UnitId = model.PersonId is > 0
					? await unitOfWork.Repository<Personel>().TableNoTracking
						.Where(p => p.Id == model.PersonId)
						.Select(p => p.OrgUnitId)
						.FirstOrDefaultAsync(cn)
					: null;
			}
			else if (model.OperatorType == GuardOperatorTypeEnum.Contractor)
			{
				model.PersonId = null;
				model.UnitId = null;
			}
		}

		private async Task ReplaceFileAsync(long? oldId, long? newId, CancellationToken cn)
		{
			if (oldId is > 0 && oldId != newId)
				await fileService.DeleteAsync(oldId.Value, cn);
		}

		private async Task<GuardPersonIo?> LoadForEditAsync(long id, CancellationToken cn)
			=> await unitOfWork.Repository<GuardPersonIo>().TableNoTracking
				.Include(x => x.EntryFile)
				.Include(x => x.ExitFile)
				.FirstOrDefaultAsync(c => c.Id == id, cn);

		#endregion
	}
}
