using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Gnr;
using Entities.App.Hrm;
using Entities.App.Hrm.Enums;
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
	[ControllerInfo("ورود و خروج کالا (حراست/دژبان)", typeof(GuardPartIo))]
	public class GuardPartIoController(IUnitOfWork unitOfWork, IFileService fileService, IUserService userService) : BaseController
	{
		#region CRUD

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(GuardPartIo model, CancellationToken cn)
		{
			var details = model.Details ?? [];
			var error = Validate(model, details);
			if (error != null) return BadRequest(error);

			ClearNav(model);
			model.Id = null;
			model.HtsId = 0;
			model.IsConfirmed = false;
			model.PartEntryNotifyId = null;
			model.Details = null;

			var saved = await unitOfWork.Repository<GuardPartIo>().SaveAsync(model, cn, true);
			await SyncDetailsAsync(saved.Id!.Value, details, cn);
			await NotifyUnknownPartAsync(saved.Id.Value, details, cn);

			return Ok(await LoadForEditAsync(saved.Id.Value, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(GuardPartIo model, CancellationToken cn)
		{
			var details = model.Details ?? [];
			var error = Validate(model, details);
			if (error != null) return BadRequest(error);

			var old = await unitOfWork.Repository<GuardPartIo>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (old == null) return BadRequest("رکورد یافت نشد");

			ClearNav(model);
			model.HtsId = old.HtsId;
			model.IsConfirmed = old.IsConfirmed;
			model.PartEntryNotifyId = old.PartEntryNotifyId;
			model.Details = null;

			var saved = await unitOfWork.Repository<GuardPartIo>().UpdateAsync(model, cn, true);
			await SyncDetailsAsync(saved.Id!.Value, details, cn);
			await NotifyUnknownPartAsync(saved.Id.Value, details, cn);

			return Ok(await LoadForEditAsync(saved.Id.Value, cn));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<GuardPartIo>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return Ok();

			var details = await unitOfWork.Repository<GuardPartIoDetail>().TableNoTracking
				.Where(d => d.PartIoId == id).ToListAsync(cn);
			foreach (var d in details)
				await unitOfWork.Repository<GuardPartIoDetail>().DeleteAsync(d, cn, true);
			foreach (var fileId in details.Where(d => d.FileId != null).Select(d => d.FileId!.Value).Distinct())
				await fileService.DeleteAsync(fileId, cn);

			await unitOfWork.Repository<GuardPartIo>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		/// <summary>معادل HTS DoConfirmOperation: تایید، ایمیل اعلان و ثبت تاریخ ورود روی اعلام ورود.</summary>
		[HttpPost("[action]")]
		[ActionDisplayName("تایید و ارسال اعلان", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Confirm(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<GuardPartIo>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return BadRequest("رکورد یافت نشد");
			if (entity.PartEntryNotifyId == null || entity.PartEntryNotifyId == 0)
				return BadRequest("این رکورد به اعلان ورود کالا متصل نیست");
			if (entity.IsConfirmed) return BadRequest("این رکورد قبلاً تایید شده است");

			entity.IsConfirmed = true;
			await unitOfWork.Repository<GuardPartIo>().UpdateAsync(entity, cn, true);

			await GuardPartIoEmail.QueueAsync(unitOfWork, userService, id, CurrentUserId, cn);

			var now = DateTime.Now;
			await unitOfWork.Repository<PartEntryNotify>().UpdateFieldsAsync(entity.PartEntryNotifyId.Value, new Dictionary<string, object>
			{
				[nameof(PartEntryNotify.EntryMiladiDate)] = now.Date,
				[nameof(PartEntryNotify.EntryShamsiDate)] = now.ToShamsiDate()
			}, cn);

			return Ok(await LoadForEditAsync(id, cn));
		}

		#endregion

		#region Pages

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			var entity = (id != null && id != 0) ? await LoadForEditAsync(id.Value, cn) : null;
			return View(@"\Views\Panel\Hrm\GuardPartIo\Edit.cshtml", entity ?? new GuardPartIo { Details = [] });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
			=> View(@"\Views\Panel\Hrm\GuardPartIo\Edit.cshtml", new GuardPartIo { IoDoor = GuardIoDoorEnum.Upper, Details = [] });

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Hrm\GuardPartIo\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<GuardPartIo>().FetchDataAsync(request, cn));

		#endregion

		#region Helpers (no ActionDisplayName)

		/// <summary>اقلام فرم برای گرید.</summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> GetDetails(long partIoId, CancellationToken cn)
		{
			var rows = await unitOfWork.Repository<GuardPartIoDetail>().TableNoTracking
				.Include(d => d.Part)
				.Include(d => d.RelatedUnit)
				.Include(d => d.File)
				.Where(d => d.PartIoId == partIoId)
				.OrderBy(d => d.Id)
				.ToListAsync(cn);

			return Ok(rows.Select(d => new
			{
				d.Id,
				d.HtsId,
				d.PartId,
				PartTitle = d.Part?.Title,
				d.Mount,
				IoReason = (int)d.IoReason,
				IoReasonTitle = d.IoReason.ToDisplay(),
				d.PaperNumber,
				d.RelatedUnitId,
				RelatedUnitTitle = d.RelatedUnit?.Title,
				d.FileId,
				FileName = d.File?.OriginalName,
				FileSize = d.File?.Size,
				d.Comment
			}));
		}

		/// <summary>نام و حجم فایل پیوست آپلودشده (برای نمایش در گرید اقلام).</summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> GetFileInfo(long id, CancellationToken cn)
		{
			var file = await unitOfWork.Repository<Entities.Base.FileEntity>().TableNoTracking
				.Where(f => f.Id == id)
				.Select(f => new { f.Id, FileName = f.OriginalName, FileSize = f.Size })
				.FirstOrDefaultAsync(cn);
			return Ok(file);
		}

		/// <summary>فرم مودال قلم.</summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> ItemForm(long? id, long? partId, decimal? mount, int? ioReason,
			string? paperNumber, long? relatedUnitId, long? fileId, CancellationToken cn)
		{
			var item = new GuardPartIoDetail
			{
				Id = id,
				PartId = partId ?? 0,
				Mount = mount ?? 0,
				IoReason = ioReason != null && Enum.IsDefined(typeof(GuardIoReasonEnum), ioReason.Value)
					? (GuardIoReasonEnum)ioReason.Value : 0,
				PaperNumber = paperNumber,
				RelatedUnitId = relatedUnitId,
				FileId = fileId
			};
			if (fileId is > 0)
				item.File = await unitOfWork.Repository<Entities.Base.FileEntity>().TableNoTracking
					.FirstOrDefaultAsync(f => f.Id == fileId, cn);
			// HTS OrganizationUnitService.GetFinalAndValidItems: واحدهایی که پرسنل فعال با کد حداکثر ۵ رقم دارند؛ بدون واحدهای «*».
			ViewBag.UnitIds = await unitOfWork.Repository<Entities.App.Hcm.Personel>().TableNoTracking
				.Where(p => p.IsActive == Entities.Base.IsActiveEnum.Active && p.OrgUnitId != null
					&& p.Code != null && p.Code.Length <= 5)
				.Select(p => p.OrgUnitId!.Value)
				.Distinct()
				.ToListAsync(cn);
			ViewBag.IsNew = id == null || id == 0;
			return View(@"\Views\Panel\Hrm\GuardPartIo\_ItemForm.cshtml", item);
		}

		/// <summary>فرم مودال افزودن سریع پیمانکار/راننده/مکان/کالا (فرم اصلی HTS).</summary>
		[HttpGet("[action]")]
		public IActionResult QuickForm(string type)
		{
			var view = type?.ToLowerInvariant() switch
			{
				"contractor" => "_QuickContractor",
				"driver" => "_QuickDriver",
				"location" => "_QuickLocation",
				"part" => "_QuickPart",
				_ => null
			};
			if (view == null) return BadRequest("نوع نامعتبر است");
			return View($@"\Views\Panel\Hrm\GuardPartIo\{view}.cshtml");
		}

		#endregion

		#region Private

		private static void ClearNav(GuardPartIo model)
		{
			model.Person = null;
			model.Contractor = null;
			model.Driver = null;
			model.IoSource = null;
			model.IoDestination = null;
			model.PartEntryNotify = null;
		}

		private static string? Validate(GuardPartIo model, List<GuardPartIoDetail> details)
		{
			if (!Enum.IsDefined(model.IoType))
				return "نوع ورود/خروج نمی‌تواند خالی باشد";
			if (model.IoType == GuardIoTypeEnum.EmergencyExitReturn)
				return "نوع ورود/خروج انتخاب‌شده معتبر نیست";
			if (!Enum.IsDefined(model.OperatorType))
				return "نوع اپراتور نمی‌تواند خالی باشد";
			if (!Enum.IsDefined(model.IoDoor))
				return "درب نمی‌تواند خالی باشد";

			switch (model.OperatorType)
			{
				case GuardOperatorTypeEnum.Personnel when model.PersonId is null or 0:
					return "پرسنل نمی‌تواند خالی باشد";
				case GuardOperatorTypeEnum.Contractor when model.ContractorId is null or 0:
					return "پیمانکار نمی‌تواند خالی باشد";
				case GuardOperatorTypeEnum.Miscellaneous when string.IsNullOrWhiteSpace(model.OperatorTitle):
					return "شخص متفرقه نمی‌تواند خالی باشد";
			}

			if (details.Count < 1)
				return "کالاها نمی‌تواند خالی باشد";

			foreach (var d in details)
			{
				if (d.PartId <= 0)
					return "کالا نمی‌تواند خالی باشد";
				if (d.Mount < 0)
					return "مقدار نامعتبر است";
				if (!Enum.IsDefined(d.IoReason))
					return "علت نمی‌تواند خالی باشد";
			}

			if (details.GroupBy(d => d.PartId).Any(g => g.Count() > 1))
				return "امکان ثبت داده تکراری وجود دارد";

			return null;
		}

		/// <summary>HTS: Id&lt;1 insert، موجود update، غایب delete (فایل و HtsId ردیف‌های موجود حفظ می‌شود).</summary>
		private async Task SyncDetailsAsync(long partIoId, List<GuardPartIoDetail> details, CancellationToken cn)
		{
			var repo = unitOfWork.Repository<GuardPartIoDetail>();
			var existing = await repo.TableNoTracking.Where(d => d.PartIoId == partIoId).ToListAsync(cn);

			var keepIds = details.Where(d => d.Id is > 0).Select(d => d.Id!.Value).ToHashSet();
			foreach (var old in existing.Where(e => e.Id == null || !keepIds.Contains(e.Id.Value)))
				await repo.DeleteAsync(old, cn, true);

			foreach (var item in details)
			{
				item.PartIoId = partIoId;
				item.PartIo = null;
				item.Part = null;
				item.RelatedUnit = null;
				item.File = null;

				if (item.Id is null or 0 || item.Id < 1)
				{
					item.Id = null;
					item.HtsId = 0;
					item.HtsAttachmentPath = null;
					await repo.SaveAsync(item, cn, true);
				}
				else
				{
					var old = existing.FirstOrDefault(e => e.Id == item.Id);
					if (old == null) continue;
					item.HtsId = old.HtsId;
					item.HtsAttachmentPath = old.HtsAttachmentPath;
					item.Comment = old.Comment;
					await repo.UpdateAsync(item, cn, true);
				}
			}
		}

		/// <summary>HTS: برای هر ردیف با کالای «نامشخص» همان ایمیل اعلان ارسال می‌شد.</summary>
		private async Task NotifyUnknownPartAsync(long partIoId, List<GuardPartIoDetail> details, CancellationToken cn)
		{
			if (details.Count == 0) return;
			var unknownPartId = await unitOfWork.Repository<GuardPart>().TableNoTracking
				.Where(p => p.HtsId == GuardPartIoEmail.HtsUnknownPartId)
				.Select(p => p.Id)
				.FirstOrDefaultAsync(cn);
			if (unknownPartId == null || unknownPartId == 0) return;

			var count = details.Count(d => d.PartId == unknownPartId);
			for (var i = 0; i < count; i++)
				await GuardPartIoEmail.QueueAsync(unitOfWork, userService, partIoId, CurrentUserId, cn);
		}

		private async Task<GuardPartIo?> LoadForEditAsync(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<GuardPartIo>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return null;

			// فقط خود ردیف‌ها؛ نمایش اقلام با GetDetails (بدون navigation) انجام می‌شود.
			entity.Details = await unitOfWork.Repository<GuardPartIoDetail>().TableNoTracking
				.Where(d => d.PartIoId == id)
				.OrderBy(d => d.Id)
				.ToListAsync(cn);
			return entity;
		}

		#endregion
	}
}
