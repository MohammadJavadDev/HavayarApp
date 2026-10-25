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
	[ControllerInfo("ورود/خروج میهمان", typeof(GuardGuestIo))]
	public class GuardGuestIoController(IUnitOfWork unitOfWork, IFileService fileService, IWebHostEnvironment env) : BaseController
	{
		#region CRUD

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(GuardGuestIo model, CancellationToken cn)
		{
			var error = Validate(model);
			if (error != null) return BadRequest(error);

			await ApplyVisitorAsync(model, cn);
			model.Id = null;
			model.HtsId = 0;
			model.ExitFileId = null;

			var saved = await unitOfWork.Repository<GuardGuestIo>().SaveAsync(model, cn, true);
			return Ok(await LoadForEditAsync(saved.Id!.Value, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(GuardGuestIo model, CancellationToken cn)
		{
			var error = Validate(model);
			if (error != null) return BadRequest(error);

			var old = await unitOfWork.Repository<GuardGuestIo>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (old == null) return BadRequest("رکورد یافت نشد");

			await ApplyVisitorAsync(model, cn);
			model.HtsId = old.HtsId;
			await ReplaceFileAsync(old.EntryFileId, model.EntryFileId, cn);
			await ReplaceFileAsync(old.ExitFileId, model.ExitFileId, cn);

			var saved = await unitOfWork.Repository<GuardGuestIo>().UpdateAsync(model, cn, true);
			return Ok(await LoadForEditAsync(saved.Id!.Value, cn));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<GuardGuestIo>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return Ok();

			await unitOfWork.Repository<GuardGuestIo>().DeleteAsync(entity, cn, true);
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
			return View(@"\Views\Panel\Hrm\GuardGuestIo\Edit.cshtml", entity ?? new GuardGuestIo());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var now = DateTime.Now;
			return View(@"\Views\Panel\Hrm\GuardGuestIo\Edit.cshtml", new GuardGuestIo
			{
				EntryMiladiDate = now.Date,
				EntryShamsiDate = now.ToShamsiDate(),
				EntryTime = now.ToString("HH:mm")
			});
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Hrm\GuardGuestIo\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<GuardGuestIo>().FetchDataAsync(request, cn));

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + @"\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<GuardGuestIo>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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

		private static string? Validate(GuardGuestIo model)
		{
			if (!Enum.IsDefined(model.GuestType))
				return "نوع میهمان نمی‌تواند خالی باشد";
			if (string.IsNullOrWhiteSpace(model.Name))
				return "نام نمی‌تواند خالی باشد";
			if (string.IsNullOrWhiteSpace(model.Family))
				return "نام خانوادگی نمی‌تواند خالی باشد";
			if (model.PersonId is null or 0)
				return "شخص ملاقات شونده نمی‌تواند خالی باشد";

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

		private async Task ApplyVisitorAsync(GuardGuestIo model, CancellationToken cn)
		{
			if (model.PersonId is 0) model.PersonId = null;
			if (model.EntryFileId is 0) model.EntryFileId = null;
			if (model.ExitFileId is 0) model.ExitFileId = null;

			model.Person = null;
			model.Unit = null;
			model.EntryFile = null;
			model.ExitFile = null;
			model.Name = model.Name.Trim();
			model.Family = model.Family.Trim();

			model.UnitId = model.PersonId is > 0
				? await unitOfWork.Repository<Personel>().TableNoTracking
					.Where(p => p.Id == model.PersonId)
					.Select(p => p.OrgUnitId)
					.FirstOrDefaultAsync(cn)
				: null;
		}

		private async Task ReplaceFileAsync(long? oldId, long? newId, CancellationToken cn)
		{
			if (oldId is > 0 && oldId != newId)
				await fileService.DeleteAsync(oldId.Value, cn);
		}

		private async Task<GuardGuestIo?> LoadForEditAsync(long id, CancellationToken cn)
			=> await unitOfWork.Repository<GuardGuestIo>().TableNoTracking
				.Include(x => x.EntryFile)
				.Include(x => x.ExitFile)
				.FirstOrDefaultAsync(c => c.Id == id, cn);

		#endregion
	}
}
