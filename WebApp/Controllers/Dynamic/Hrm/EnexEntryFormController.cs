using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Hrm;
using Entities.App.Hrm.Enums;
using Entities.Auth;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using Services.FileServices;
using WebApp.Actions.Hrm;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Hrm/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("ورود اضطراری", typeof(EnexEntryForm))]
	public class EnexEntryFormController(IUnitOfWork unitOfWork, IFileService fileService, IUserService userService) : BaseController
	{
		public const string SecurityRoleName = "کارشناسان ورود اضطراری";

		private bool CanSecurity => IsAdministrator || CurrentUserHasRole(SecurityRoleName);

		private static bool IsOpenStatus(EnexEntryFormStatusEnum status)
			=> status is EnexEntryFormStatusEnum.PreRegister or EnexEntryFormStatusEnum.Edited;

		#region CRUD

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(EnexEntryForm model, CancellationToken cn)
		{
			if (!IsAdministrator) return BadRequest("فقط مدیر سیستم می‌تواند فرم جدید ثبت کند");
			if (model.SourceEntryId is null or 0) return BadRequest("یک ردیف را انتخاب کنید");
			var parts = model.Parts ?? [];
			var error = Validate(model, parts);
			if (error != null) return BadRequest(error);

			var now = DateTime.Now;
			var maxNumber = await unitOfWork.Repository<EnexEntryForm>().TableNoTracking
				.Select(x => (int?)x.FormNumber).MaxAsync(cn) ?? 0;

			ClearNav(model);
			model.Id = null;
			model.HtsId = 0;
			model.Parts = null;
			model.FormNumber = maxNumber + 1;
			model.IsFromExitForm = true;
			model.LastStatusId = EnexEntryFormStatusEnum.PreRegister;
			model.OrganizationUnitId = CurrentOrganizationUnitId;
			if (model.OrganizationUnitId is null or 0)
				return BadRequest("واحد سازمانی کاربر جاری مشخص نیست");

			var saved = await unitOfWork.Repository<EnexEntryForm>().SaveAsync(model, cn, true);
			await StampCreatorAsync(saved.Id!.Value, now, cn);
			await SyncPartsAsync(saved.Id.Value, parts, cn);
			await AddLogAsync(saved.Id.Value, EnexEntryFormStatusEnum.PreRegister, null, cn);
			await EnexEntryFormEmail.QueueCreatedAsync(unitOfWork, userService, saved.Id.Value, CurrentUserId, cn);
			return Ok(await LoadForEditAsync(saved.Id.Value, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(EnexEntryForm model, CancellationToken cn)
		{
			var parts = model.Parts ?? [];
			var old = await unitOfWork.Repository<EnexEntryForm>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (old == null) return BadRequest("رکورد یافت نشد");
			if (!IsAdministrator && !IsOpenStatus(old.LastStatusId))
				return BadRequest("این فرم قابل ویرایش نیست");

			var error = Validate(model, parts);
			if (error != null) return BadRequest(error);
			if (CurrentOrganizationUnitId is null or 0)
				return BadRequest("واحد سازمانی کاربر جاری مشخص نیست");

			var now = DateTime.Now;
			ClearNav(model);
			model.HtsId = old.HtsId;
			model.FormNumber = old.FormNumber;
			model.IsFromExitForm = old.IsFromExitForm;
			model.ExitFormId = old.ExitFormId;
			model.Parts = null;
			model.LastStatusId = EnexEntryFormStatusEnum.Edited;
			model.OrganizationUnitId = CurrentOrganizationUnitId;

			var saved = await unitOfWork.Repository<EnexEntryForm>().UpdateAsync(model, cn, true);
			await StampCreatorAsync(saved.Id!.Value, now, cn);
			await SyncPartsAsync(saved.Id.Value, parts, cn);
			await AddLogAsync(saved.Id.Value, EnexEntryFormStatusEnum.Edited, null, cn);
			return Ok(await LoadForEditAsync(saved.Id.Value, cn));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			if (!IsAdministrator) return BadRequest("فقط مدیر سیستم می‌تواند فرم را حذف کند");
			var entity = await unitOfWork.Repository<EnexEntryForm>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return Ok();

			var parts = await unitOfWork.Repository<EnexFormPart>().TableNoTracking
				.Where(p => p.EntryFormId == id).ToListAsync(cn);
			foreach (var part in parts)
				await unitOfWork.Repository<EnexFormPart>().DeleteAsync(part, cn, true);
			foreach (var fileId in parts.Where(p => p.FileId != null).Select(p => p.FileId!.Value).Distinct())
				await fileService.DeleteAsync(fileId, cn);

			var logs = await unitOfWork.Repository<EnexFormLog>().TableNoTracking
				.Where(l => l.EntryFormId == id).ToListAsync(cn);
			foreach (var log in logs)
				await unitOfWork.Repository<EnexFormLog>().DeleteAsync(log, cn, true);

			await unitOfWork.Repository<EnexEntryForm>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		/// <summary>تایید نهایی و بایگانی — وضعیت 861، فرم خروج بازگشت‌شده، ایمیل به سازنده فرم ورود.</summary>
		[HttpPost("[action]")]
		[ActionDisplayName("تایید نهایی و بایگانی", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> FinalConfirm(long id, CancellationToken cn)
		{
			if (!CanSecurity) return BadRequest("دسترسی تایید حراست ندارید");
			var entity = await unitOfWork.Repository<EnexEntryForm>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return BadRequest("رکورد یافت نشد");
			if (!IsAdministrator && entity.LastStatusId != EnexEntryFormStatusEnum.Edited)
				return BadRequest("تایید نهایی فقط برای فرم ویرایش‌شده ممکن است");

			var now = DateTime.Now;
			entity.LastStatusId = EnexEntryFormStatusEnum.SecurityFinalApprove;
			await unitOfWork.Repository<EnexEntryForm>().UpdateAsync(entity, cn, true);
			await AddLogAsync(id, EnexEntryFormStatusEnum.SecurityFinalApprove, null, cn);

			if (entity.ExitFormId is > 0)
			{
				await unitOfWork.Repository<EnexExitForm>().UpdateFieldsAsync(entity.ExitFormId.Value, new Dictionary<string, object>
				{
					[nameof(EnexExitForm.IsReturned)] = true,
					[nameof(EnexExitForm.ReturnMiladiDate)] = now.Date,
					[nameof(EnexExitForm.ReturnShamsiDate)] = now.ToShamsiDate()
				}, cn);
			}

			await EnexEntryFormEmail.QueueFinalAsync(unitOfWork, userService, id, CurrentUserId, cn);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دانلود پیوست", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> DownloadAttachment(long id, CancellationToken cn)
		{
			if (!CanSecurity) return BadRequest("دسترسی دانلود پیوست ندارید");
			var fileId = await unitOfWork.Repository<EnexEntryForm>().TableNoTracking
				.Where(x => x.Id == id)
				.Select(x => x.ExitForm != null ? x.ExitForm.FileId : null)
				.FirstOrDefaultAsync(cn);
			if (fileId is null or 0) return BadRequest("فایل یافت نشد");

			var (stream, contentType, fileName) = await fileService.DownloadAsync(fileId.Value, cn);
			return File(stream, contentType, fileName);
		}

		#endregion

		#region Pages

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			if (id is null or 0) return BadRequest("رکورد یافت نشد");
			var entity = await LoadForEditAsync(id.Value, cn);
			if (entity == null) return BadRequest("رکورد یافت نشد");
			if (!IsAdministrator && !IsOpenStatus(entity.LastStatusId))
				return BadRequest("این فرم قابل ویرایش نیست");
			return View(@"\Views\Panel\Hrm\EnexEntryForm\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public async Task<IActionResult> New(long? fromId, CancellationToken cn)
		{
			if (!IsAdministrator) return BadRequest("فقط مدیر سیستم می‌تواند فرم جدید ثبت کند");
			if (fromId is null or 0) return BadRequest("یک ردیف را انتخاب کنید");
			var source = await unitOfWork.Repository<EnexEntryForm>().TableNoTracking
				.FirstOrDefaultAsync(x => x.Id == fromId, cn);
			if (source == null) return BadRequest("رکورد یافت نشد");

			return View(@"\Views\Panel\Hrm\EnexEntryForm\Edit.cshtml", new EnexEntryForm
			{
				IsFromExitForm = true,
				ExitFormId = source.ExitFormId,
				SourceEntryId = source.Id,
				LastStatusId = EnexEntryFormStatusEnum.PreRegister,
				Parts = []
			});
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Hrm\EnexEntryForm\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("گزارش ورود اضطراری", ActionAccessType.View, ActionAccessItemType.ReportItem)]
		public IActionResult Report() => View(@"\Views\Panel\Hrm\EnexEntryForm\Report.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<EnexEntryForm>().FetchDataAsync(request, cn));

		#endregion

		#region Helpers

		[HttpGet("[action]")]
		public async Task<IActionResult> GetParts(long entryFormId, CancellationToken cn)
		{
			var rows = await unitOfWork.Repository<EnexFormPart>().TableNoTracking
				.Include(p => p.EnexPart)
				.Include(p => p.Part)
				.Include(p => p.File)
				.Where(p => p.EntryFormId == entryFormId)
				.OrderBy(p => p.Id)
				.ToListAsync(cn);

			return Ok(rows.Select(p => new
			{
				p.Id,
				p.HtsId,
				p.EnexPartId,
				p.PartId,
				PartTitle = PartTitle(p),
				p.Mount,
				p.EnterAmount,
				p.IsSecurityDisApproved,
				p.ReturnShamsiDate,
				p.ReturnMiladiDate,
				p.Comment,
				p.FileId,
				FileName = p.File?.OriginalName,
				FileSize = p.File?.Size
			}));
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetLogs(long entryFormId, CancellationToken cn)
		{
			var rows = await unitOfWork.Repository<EnexFormLog>().TableNoTracking
				.Where(l => l.EntryFormId == entryFormId)
				.OrderByDescending(l => l.Id)
				.ToListAsync(cn);
			return Ok(rows.Select(l => new
			{
				l.Id,
				StatusTitle = l.StatusId.ToDisplay(),
				l.CreatedByName,
				l.CreatedOnShamsiDateTime,
				l.Comment
			}));
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetFileInfo(long id, CancellationToken cn)
		{
			var file = await unitOfWork.Repository<FileEntity>().TableNoTracking
				.Where(f => f.Id == id)
				.Select(f => new { f.Id, FileName = f.OriginalName, FileSize = f.Size })
				.FirstOrDefaultAsync(cn);
			return Ok(file);
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> ItemForm(long? id, long? enexPartId, long? partId, decimal? mount,
			decimal? enterAmount, bool? isSecurityDisApproved, string? returnShamsiDate, string? returnMiladiDate,
			string? comment, long? fileId, CancellationToken cn)
		{
			var item = new EnexFormPart
			{
				Id = id,
				EnexPartId = enexPartId,
				PartId = partId,
				Mount = mount ?? 0,
				EnterAmount = enterAmount,
				IsSecurityDisApproved = isSecurityDisApproved,
				ReturnShamsiDate = returnShamsiDate,
				Comment = comment,
				FileId = fileId
			};
			if (DateTime.TryParse(returnMiladiDate, out var miladi))
				item.ReturnMiladiDate = miladi.Date;
			if (fileId is > 0)
				item.File = await unitOfWork.Repository<FileEntity>().TableNoTracking
					.FirstOrDefaultAsync(f => f.Id == fileId, cn);
			if (partId is > 0 && enexPartId is null or 0)
			{
				ViewBag.InvPartTitle = await unitOfWork.Repository<Entities.App.Inv.Part>().TableNoTracking
					.Where(p => p.Id == partId)
					.Select(p => p.Code + " | " + p.Name)
					.FirstOrDefaultAsync(cn);
			}
			return View(@"\Views\Panel\Hrm\EnexEntryForm\_ItemForm.cshtml", item);
		}

		#endregion

		#region Private

		private static void ClearNav(EnexEntryForm model)
		{
			model.ExitForm = null;
			model.OrganizationUnit = null;
			model.Parts = null;
		}

		private static string? Validate(EnexEntryForm model, List<EnexFormPart> parts)
		{
			if (string.IsNullOrWhiteSpace(model.ImporterTitle))
				return "عنوان واردکننده نمی‌تواند خالی باشد";
			if (parts.Count < 1)
				return "اقلام نمی‌تواند خالی باشد";

			foreach (var part in parts)
			{
				if (part.EnexPartId is null or 0 && part.PartId is null or 0)
					return "قطعه نمی‌تواند خالی باشد";
				if (part.EnterAmount == null)
					return "مقدار ورود نمی‌تواند خالی باشد";
				if (part.Mount > 0 && part.EnterAmount > part.Mount)
					return "مقدار ورود نمی‌تواند از مقدار خروج بیشتر باشد";
				if (part.IsSecurityDisApproved != true && string.IsNullOrWhiteSpace(part.ReturnShamsiDate))
					return "تاریخ بازگشت نمی‌تواند خالی باشد";
			}

			return null;
		}

		private async Task StampCreatorAsync(long id, DateTime now, CancellationToken cn)
		{
			await unitOfWork.Repository<EnexEntryForm>().UpdateFieldsAsync(id, new Dictionary<string, object>
			{
				[nameof(EnexEntryForm.CreatedById)] = CurrentUserId!,
				[nameof(EnexEntryForm.CreatedByName)] = CurrentUserFullName ?? "",
				[nameof(EnexEntryForm.CreatedOnMiladiDateTime)] = now,
				[nameof(EnexEntryForm.CreatedOnShamsiDateTime)] = now.ToShamsiDateTime()
			}, cn);
		}

		private async Task AddLogAsync(long entryFormId, EnexEntryFormStatusEnum status, string? comment, CancellationToken cn)
		{
			await unitOfWork.Repository<EnexFormLog>().SaveAsync(new EnexFormLog
			{
				EntryFormId = entryFormId,
				StatusId = status,
				Comment = comment,
				HtsId = 0
			}, cn, true);
		}

		private async Task SyncPartsAsync(long entryFormId, List<EnexFormPart> parts, CancellationToken cn)
		{
			var repo = unitOfWork.Repository<EnexFormPart>();
			var existing = await repo.TableNoTracking.Where(p => p.EntryFormId == entryFormId).ToListAsync(cn);
			var keepIds = parts.Where(p => p.Id is > 0).Select(p => p.Id!.Value).ToHashSet();
			foreach (var old in existing.Where(e => e.Id == null || !keepIds.Contains(e.Id.Value)))
			{
				await repo.DeleteAsync(old, cn, true);
				if (old.FileId != null)
					await fileService.DeleteAsync(old.FileId.Value, cn);
			}

			foreach (var item in parts)
			{
				item.EntryFormId = entryFormId;
				item.EntryForm = null;
				item.ExitForm = null;
				item.EnexPart = null;
				item.Part = null;
				item.File = null;
				if (item.EnexPartId is 0) item.EnexPartId = null;
				if (item.PartId is 0) item.PartId = null;

				if (item.Id is null or 0 || item.Id < 1)
				{
					item.Id = null;
					item.HtsId = 0;
					item.HtsAttachmentFileName = null;
					await repo.SaveAsync(item, cn, true);
				}
				else
				{
					var old = existing.FirstOrDefault(e => e.Id == item.Id);
					if (old == null) continue;
					item.HtsId = old.HtsId;
					item.HtsAttachmentFileName = old.HtsAttachmentFileName;
					item.ExitFormId = old.ExitFormId;
					if (item.FileId == null) item.FileId = old.FileId;
					await repo.UpdateAsync(item, cn, true);
				}
			}
		}

		private static string PartTitle(EnexFormPart p)
		{
			if (p.EnexPart != null)
				return string.IsNullOrWhiteSpace(p.EnexPart.Code) ? p.EnexPart.Title : p.EnexPart.Code + " - " + p.EnexPart.Title;
			if (p.Part != null)
				return (p.Part.Code ?? "") + " | " + p.Part.Name;
			return "";
		}

		private async Task<EnexEntryForm?> LoadForEditAsync(long id, CancellationToken cn)
		{
			return await unitOfWork.Repository<EnexEntryForm>().TableNoTracking
				.Include(x => x.ExitForm)
				.Include(x => x.OrganizationUnit)
				.FirstOrDefaultAsync(c => c.Id == id, cn);
		}

		#endregion
	}
}
