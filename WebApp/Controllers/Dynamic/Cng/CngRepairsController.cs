using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Cng;
using Entities.App.Gnr;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	// Unique ControllerName; explicit route keeps /panel/cng/repairs/*
	[Route("Panel/Cng/Repairs")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("تعمیرات CNG", typeof(Repairs))]
	public class CngRepairsController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(Repairs model, CancellationToken cn)
		{
			if (model.ProductId == 0)
				return BadRequest("تجهیز نمی‌تواند خالی باشد");
			if (model.AgencyId == 0)
				return BadRequest("نمایندگی نمی‌تواند خالی باشد");
			if (model.DestinationAfterRepair == null)
				return BadRequest("مقصد پس از تعمیر نمی‌تواند خالی باشد");
			if (model.Status == null)
				return BadRequest("وضعیت نمی‌تواند خالی باشد");
			if (string.IsNullOrWhiteSpace(model.EntryShamsiDate) && model.EntryMiladiDate == default)
				return BadRequest("تاریخ ورود نمی‌تواند خالی باشد");

			ClearNav(model);
			ApplyDateFields(model);

			if (model.Id == null || model.Id == 0)
				return await Add(model, cn);
			if (await unitOfWork.Repository<Repairs>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(Repairs model, CancellationToken cn)
		{
			ClearNav(model);
			ApplyDateFields(model);
			var saved = await unitOfWork.Repository<Repairs>().SaveAsync(model, cn, true);
			await TryImportUsedPartsAsync(cn);
			return Ok(saved);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(Repairs model, CancellationToken cn)
		{
			ClearNav(model);
			ApplyDateFields(model);
			var saved = await unitOfWork.Repository<Repairs>().UpdateAsync(model, cn, true);
			await TryImportUsedPartsAsync(cn);
			return Ok(saved);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<Repairs>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null)
				await unitOfWork.Repository<Repairs>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			var entity = (id != null && id != 0)
				? unitOfWork.Repository<Repairs>().TableNoTracking
					.Include(c => c.Product)
					.Include(c => c.Agency!).ThenInclude(a => a!.Party)
					.Include(c => c.Customer!).ThenInclude(c => c!.Party)
					.Include(c => c.Dl)
					.Include(c => c.Confirmer!).ThenInclude(u => u!.Party)
					.FirstOrDefault(c => c.Id == id)
				: CreateNew();
			return View(@"\Views\Panel\Cng\Repairs\Edit.cshtml", entity ?? CreateNew());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
			=> View(@"\Views\Panel\Cng\Repairs\Edit.cshtml", CreateNew());

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Cng\Repairs\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + @"\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<Repairs>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			=> Ok(await unitOfWork.Repository<Repairs>().FetchDataAsync(request, cn));

		/// <summary>
		/// تایید / لغو تایید — معادل HTS DoConfirmOperation (PermissionType.Accept).
		/// از UpdateFields استفاده می‌کند تا EntityAction فیلدهای تایید را برنگرداند.
		/// </summary>
		[HttpPost("[action]")]
		[ActionDisplayName("تایید تعمیرات", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Confirm(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<Repairs>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null)
				return BadRequest("رکورد یافت نشد");

			var isConfirmed = entity.ConfirmerId.HasValue;
			var now = DateTime.Now;

			var fields = new Dictionary<string, object>
			{
				[nameof(Repairs.ConfirmerId)] = isConfirmed ? null! : (object)(CurrentUserId ?? 0),
				[nameof(Repairs.ConfirmMiladiDate)] = isConfirmed ? null! : now,
				[nameof(Repairs.ConfirmShamsiDate)] = isConfirmed ? null! : now.ToShamsiDate(),
				[nameof(Repairs.ConfirmTime)] = isConfirmed ? null! : $"{now:HH:mm}"
			};

			if (!isConfirmed && (CurrentUserId == null || CurrentUserId == 0))
				return BadRequest("کاربر جاری مشخص نیست");

			await unitOfWork.Repository<Repairs>().UpdateFieldsAsync(id, fields, cn);

			var updated = await unitOfWork.Repository<Repairs>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			return Ok(updated);
		}

		/// <summary>معادل HTS AddPartEntryNotifyOperation — مسیر RoleAccess/دکمه: SendAsPartEntryNotify.</summary>
		[HttpPost("[action]")]
		[ActionDisplayName("اعلام ورود قطعه", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SendAsPartEntryNotify(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<Repairs>().TableNoTracking
				.Include(r => r.Customer!).ThenInclude(c => c!.Party)
				.Include(r => r.Dl)
				.FirstOrDefaultAsync(r => r.Id == id, cn);

			if (entity == null)
				return BadRequest("رکورد یافت نشد");
			if (entity.PartEntryNotifyId != null && entity.PartEntryNotifyId != 0)
				return BadRequest("اعلام ورود قطعه قبلاً ثبت شده است");

			var dlTitle = string.IsNullOrEmpty(entity.Dl?.Title)
				? string.Empty
				: $" - تفضیل ({entity.Dl!.Title})";
			var customerTitle = entity.Customer?.Party?.FullName ?? "";

			var notify = new PartEntryNotify
			{
				PartId = entity.ProductId,
				Mount = entity.Count,
				ApproximateEntryMiladiDate = entity.EntryMiladiDate,
				ApproximateEntryShamsiDate = entity.EntryShamsiDate ?? entity.EntryMiladiDate.ToShamsiDate(),
				PartCategoryId = null,
				PartGroupId = 1481,
				SupplierTitle = Truncate((customerTitle + dlTitle).Trim(), 128),
				PartOwnerUnitId = 146,
				Comment = !string.IsNullOrWhiteSpace(entity.Comment)
					? Truncate(entity.Comment + $" - شماره شناسنامه {entity.IdNumber}", 2048)
					: $"ایجاد شده از طریق ماژول تعمیرات سی.ان.جی- ش شناسنامه {entity.IdNumber}"
			};

			var saved = await unitOfWork.Repository<PartEntryNotify>().SaveAsync(notify, cn, true);
			await unitOfWork.Repository<Repairs>().UpdateFieldsAsync(id, new Dictionary<string, object>
			{
				[nameof(Repairs.PartEntryNotifyId)] = saved.Id!
			}, cn);

			var updated = await unitOfWork.Repository<Repairs>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			return Ok(updated);
		}

		private async Task TryImportUsedPartsAsync(CancellationToken cn)
		{
			// پیاده‌سازی توسط ایجنت Jobs/Cng؛ تا ثبت DI فقط skip
			if (HttpContext.RequestServices.GetService(typeof(Data.Contracts.ICngRepairsUsedPartImporter))
			    is not Data.Contracts.ICngRepairsUsedPartImporter importer)
				return;
			await importer.ImportAsync(cn);
		}

		/// <summary>
		/// اصلاح باگ HTS: Entry از EntryShamsi (نه WarehouseExit)، و هر تاریخ از فیلد shamsi خودش.
		/// </summary>
		private static void ApplyDateFields(Repairs model)
		{
			if (!string.IsNullOrWhiteSpace(model.EntryShamsiDate))
				model.EntryMiladiDate = model.EntryShamsiDate.ToMiladiDate();

			if (!string.IsNullOrWhiteSpace(model.WarehouseExitShamsiDate))
				model.WarehouseExitMiladiDate = model.WarehouseExitShamsiDate.ToMiladiDate();
			else
				model.WarehouseExitMiladiDate = null;

			if (!string.IsNullOrWhiteSpace(model.WarehouseDeliveryShamsiDate))
				model.WarehouseDeliveryMiladiDate = model.WarehouseDeliveryShamsiDate.ToMiladiDate();
			else
				model.WarehouseDeliveryMiladiDate = null;
		}

		private static void ClearNav(Repairs model)
		{
			model.Product = null;
			model.Agency = null;
			model.Customer = null;
			model.Dl = null;
			model.Confirmer = null;
			model.ResponsiblePersonnel = null;
			model.PartEntryNotify = null;
		}

		private static Repairs CreateNew()
			=> new()
			{
				Count = 1,
				EntryMiladiDate = DateTime.Today,
				EntryShamsiDate = DateTime.Today.ToShamsiDate(),
				Status = Entities.App.Cng.Enums.RepairsStatusEnum.Running
			};

		private static string Truncate(string value, int max)
			=> value.Length <= max ? value : value[..max];
	}
}
