using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Cng;
using Entities.App.Inv;
using Entities.App.SLS;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	// Unique ControllerName; explicit route keeps /panel/cng/invoice/*
	[Route("Panel/Cng/Invoice")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("فاکتور", typeof(Invoice))]
	public class CngInvoiceController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		private const string RoleManage = "Cng.Invoice.Manage";
		private const string RoleFull = "Cng.Invoice.Full";
		private const string RoleShowAllMenus = "ShowAllMenus";

		private bool CanShowAllRows =>
			IsAdministrator || CurrentUserHasAnyRole(RoleFull, RoleShowAllMenus);

		#region CRUD

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(Invoice model, CancellationToken cn)
		{
			ClearNav(model);
			var err = ValidateHeader(model);
			if (err != null) return BadRequest(err);

			if (model.Id == null || model.Id == 0)
				return await Add(model, cn);
			if (await unitOfWork.Repository<Invoice>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(Invoice model, CancellationToken cn)
		{
			ClearNav(model);
			var err = ValidateHeader(model);
			if (err != null) return BadRequest(err);

			ApplyCreatedStamp(model);
			var items = model.Items ?? [];
			model.Items = null;

			var saved = await unitOfWork.Repository<Invoice>().SaveAsync(model, cn, true);
			await SyncItemsAsync(saved.Id!.Value, items, cn);

			return Ok(await LoadForEditAsync(saved.Id.Value, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(Invoice model, CancellationToken cn)
		{
			ClearNav(model);
			var old = await unitOfWork.Repository<Invoice>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (old == null)
				return BadRequest("رکورد یافت نشد");

			if (!CanShowAllRows && old.CreatedById != CurrentUserId)
				return BadRequest("دسترسی به این رکورد وجود ندارد");

			var err = ValidateHeader(model);
			if (err != null) return BadRequest(err);

			model.HtsId = old.HtsId;
			ApplyCreatedStamp(model);
			var items = model.Items ?? [];
			model.Items = null;

			var saved = await unitOfWork.Repository<Invoice>().UpdateAsync(model, cn, true);
			await SyncItemsAsync(saved.Id!.Value, items, cn);

			return Ok(await LoadForEditAsync(saved.Id.Value, cn));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<Invoice>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null)
				return Ok();

			if (!CanShowAllRows && entity.CreatedById != CurrentUserId)
				return BadRequest("دسترسی به این رکورد وجود ندارد");

			var items = await unitOfWork.Repository<InvoiceItem>().TableNoTracking
				.Where(c => c.InvoiceId == id).ToListAsync(cn);
			foreach (var item in items)
				await unitOfWork.Repository<InvoiceItem>().DeleteAsync(item, cn, true);

			await unitOfWork.Repository<Invoice>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			Invoice entity;
			if (id != null && id != 0)
			{
				entity = (await LoadForEditAsync(id.Value, cn)) ?? CreateNew();
				if (!CanShowAllRows && entity.CreatedById != CurrentUserId && entity.Id is > 0)
					return BadRequest("دسترسی به این رکورد وجود ندارد");
			}
			else
				entity = CreateNew();

			return View(@"\Views\Panel\Cng\Invoice\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
			=> View(@"\Views\Panel\Cng\Invoice\Edit.cshtml", CreateNew());

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Cng\Invoice\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<Invoice>().FetchDataAsync(request, cn));

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + @"\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<Invoice>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		#endregion

		#region Helpers (no ActionDisplayName)

		/// <summary>اقلام فاکتور برای گرید فرم — بدون ActionDisplayName.</summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> GetItems(long invoiceId, CancellationToken cn)
		{
			var items = await unitOfWork.Repository<InvoiceItem>().TableNoTracking
				.Include(c => c.Part)
				.Include(c => c.CreatedUser)
				.Where(c => c.InvoiceId == invoiceId)
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.HtsId,
					c.InvoiceId,
					c.PartId,
					PartCode = c.Part != null ? c.Part.Code : null,
					PartName = c.Part != null ? c.Part.Name : null,
					PartTitle = c.Part != null ? ((c.Part.Code ?? "") + " | " + (c.Part.Name ?? "")).Trim() : null,
					c.Quantity,
					c.Price,
					c.CreatedUserId,
					CreatedUser = c.CreatedUser != null
						? (c.CreatedUser.NameFa ?? c.CreatedUser.Name ?? c.CreatedUser.Username)
						: null,
					c.CreatedDate,
					c.CreatedDateInText
				})
				.ToListAsync(cn);
			return Ok(items);
		}

		/// <summary>فرم مودال قلم — بدون ActionDisplayName.</summary>
		[HttpGet("[action]")]
		public IActionResult ItemForm(long? partId, short? quantity, long? price, long? id, long? htsId)
		{
			var item = new InvoiceItem
			{
				Id = id,
				HtsId = htsId ?? 0,
				PartId = partId ?? 0,
				Quantity = quantity ?? 0,
				Price = price ?? 0
			};
			if (partId is > 0)
				item.Part = unitOfWork.Repository<Part>().TableNoTracking.FirstOrDefault(c => c.Id == partId);
			return View(@"\Views\Panel\Cng\Invoice\_ItemForm.cshtml", item);
		}

		#endregion

		#region Private

		private static Invoice CreateNew() => new()
		{
			InvoiceNumber = "",
			InvoiceDate = "",
			Items = []
		};

		private static void ClearNav(Invoice model)
		{
			model.Customer = null;
			model.CreatedUser = null;
			if (model.Items == null) return;
			foreach (var item in model.Items)
			{
				item.Part = null;
				item.Invoice = null;
				item.CreatedUser = null;
			}
		}

		private static string? ValidateHeader(Invoice model)
		{
			if (model.CustomerId <= 0)
				return "مشتری نمی‌تواند خالی باشد";
			if (string.IsNullOrWhiteSpace(model.InvoiceNumber))
				return "شماره فاکتور نمی‌تواند خالی باشد";
			if (string.IsNullOrWhiteSpace(model.InvoiceDate))
				return "تاریخ فاکتور نمی‌تواند خالی باشد";
			return null;
		}

		private void ApplyCreatedStamp(Invoice model)
		{
			var now = DateTime.Now;
			var userId = CurrentUserId ?? 0;
			var shamsi = now.ToShamsiDate();
			model.CreatedUserId = userId;
			model.CreatedDate = now.Date;
			model.CreatedDateInText = shamsi;
			// HTS بازنویسی Created روی هر ذخیره — BaseEntity Created* هم هم‌راستا می‌شود
			model.CreatedById = userId;
			model.CreatedByName = CurrentUserFullName;
			model.CreatedOnMiladiDateTime = now;
			model.CreatedOnShamsiDateTime = shamsi;

			if (model.Items == null) return;
			foreach (var item in model.Items)
			{
				item.CreatedUserId = userId;
				item.CreatedDate = now.Date;
				item.CreatedDateInText = shamsi;
				item.CreatedById = userId;
				item.CreatedByName = CurrentUserFullName;
				item.CreatedOnMiladiDateTime = now;
				item.CreatedOnShamsiDateTime = shamsi;
			}
		}

		/// <summary>HTS: Id&lt;1 insert، موجود update، غایب delete.</summary>
		private async Task SyncItemsAsync(long invoiceId, List<InvoiceItem> items, CancellationToken cn)
		{
			var existing = await unitOfWork.Repository<InvoiceItem>().Table
				.Where(c => c.InvoiceId == invoiceId)
				.ToListAsync(cn);

			var keepIds = items.Where(i => i.Id is > 0).Select(i => i.Id!.Value).ToHashSet();
			foreach (var old in existing.Where(e => e.Id == null || !keepIds.Contains(e.Id.Value)))
				await unitOfWork.Repository<InvoiceItem>().DeleteAsync(old, cn, true);

			var now = DateTime.Now;
			var userId = CurrentUserId ?? 0;
			var shamsi = now.ToShamsiDate();
			var userName = CurrentUserFullName;

			foreach (var item in items)
			{
				item.InvoiceId = invoiceId;
				item.Part = null;
				item.Invoice = null;
				item.CreatedUser = null;
				item.CreatedUserId = userId;
				item.CreatedDate = now.Date;
				item.CreatedDateInText = shamsi;
				item.CreatedById = userId;
				item.CreatedByName = userName;
				item.CreatedOnMiladiDateTime = now;
				item.CreatedOnShamsiDateTime = shamsi;

				if (item.Id is null or 0 || item.Id < 1)
				{
					item.Id = null;
					await unitOfWork.Repository<InvoiceItem>().SaveAsync(item, cn, true);
				}
				else
				{
					var old = existing.FirstOrDefault(e => e.Id == item.Id);
					if (old != null)
						item.HtsId = old.HtsId;
					await unitOfWork.Repository<InvoiceItem>().UpdateAsync(item, cn, true);
				}
			}
		}

		private async Task<Invoice?> LoadForEditAsync(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<Invoice>().TableNoTracking
				.Include(c => c.Customer)!.ThenInclude(c => c!.Party)
				.Include(c => c.CreatedUser)
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return null;

			entity.Items = await unitOfWork.Repository<InvoiceItem>().TableNoTracking
				.Include(c => c.Part)
				.Include(c => c.CreatedUser)
				.Where(c => c.InvoiceId == id)
				.OrderBy(c => c.Id)
				.ToListAsync(cn);
			return entity;
		}

		#endregion
	}
}
