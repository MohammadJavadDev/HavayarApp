using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Qc;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Qc/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("پارامتر اندازه گیری", typeof(MeasurementParameterItem))]
	public class MeasurementParameterItemController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(MeasurementParameterItem measurementParameterItem, CancellationToken cn)
		{
			var postedSubItems = measurementParameterItem.SubItems?.ToList() ?? new List<MeasurementParameterSubItem>();
			measurementParameterItem.SubItems = null!;
			measurementParameterItem.MeasurementParameter = null;

			var error = await Validate(measurementParameterItem, postedSubItems, cn);
			if (error != null)
				return BadRequest(error);

			var isNew = measurementParameterItem.Id == null || measurementParameterItem.Id == 0
				|| !await unitOfWork.Repository<MeasurementParameterItem>().TableNoTracking.AnyAsync(c => c.Id == measurementParameterItem.Id, cn);

			if (!isNew)
			{
				var foreignId = await ForeignSubItemId(measurementParameterItem.Id!.Value, postedSubItems, cn);
				if (foreignId)
					return BadRequest("ریزقلم متعلق به این پارامتر نیست.");
			}

			MeasurementParameterItem saved = null!;
			await unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var repo = unitOfWork.Repository<MeasurementParameterItem>();
				saved = isNew
					? await repo.AddAsync(measurementParameterItem, cn, true, true)
					: await repo.UpdateAsync(measurementParameterItem, cn, true, true);

				await SyncSubItems(saved.Id!.Value, postedSubItems, isNew, cn);
				return saved;
			}, cn);

			return Ok(await Load(saved.Id!.Value, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public Task<IActionResult> Add(MeasurementParameterItem measurementParameterItem, CancellationToken cn)
		{
			measurementParameterItem.Id = 0;
			return Save(measurementParameterItem, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public Task<IActionResult> Update(MeasurementParameterItem measurementParameterItem, CancellationToken cn)
		{
			return Save(measurementParameterItem, cn);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<MeasurementParameterItem>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<MeasurementParameterItem>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			if (id != null && id != 0)
			{
				var entity = await Load(id.Value, cn);
				if (entity == null)
					return NotFound();
				return View(@"\Views\Panel\Qc\MeasurementParameterItem\Edit.cshtml", entity);
			}
			return View(@"\Views\Panel\Qc\MeasurementParameterItem\Edit.cshtml", new MeasurementParameterItem());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			return View(@"\Views\Panel\Qc\MeasurementParameterItem\Edit.cshtml", new MeasurementParameterItem());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Qc\MeasurementParameterItem\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<MeasurementParameterItem>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<MeasurementParameterItem>().FetchDataAsync(request, cn));
		}

		private async Task<string?> Validate(MeasurementParameterItem item, List<MeasurementParameterSubItem> subItems, CancellationToken cn)
		{
			if (string.IsNullOrWhiteSpace(item.Title))
				return "عنوان نمی‌تواند خالی باشد.";

			if (item.Title.Length > 2048)
				return "عنوان حداکثر ۲۰۴۸ کاراکتر است.";

			if (item.CommonSymbol != null && item.CommonSymbol.Length > 10)
				return "نماد رایج حداکثر ۱۰ کاراکتر است.";

			if (item.MeasurementParameterId <= 0
				|| !await unitOfWork.Repository<MeasurementParameter>().TableNoTracking.AnyAsync(c => c.Id == item.MeasurementParameterId, cn))
				return "گروه انتخاب نشده است.";

			if (subItems.Any(s => s.Title != null && s.Title.Length > 2048))
				return "عنوان ریزقلم حداکثر ۲۰۴۸ کاراکتر است.";

			return null;
		}

		private async Task<bool> ForeignSubItemId(long itemId, List<MeasurementParameterSubItem> posted, CancellationToken cn)
		{
			var keepIds = posted.Where(s => s.Id is > 0).Select(s => s.Id!.Value).Distinct().ToList();
			if (keepIds.Count == 0)
				return false;

			var existingIds = await unitOfWork.Repository<MeasurementParameterSubItem>().TableNoTracking
				.Where(s => s.MeasurementParameterItemId == itemId && s.Id != null && keepIds.Contains(s.Id.Value))
				.Select(s => s.Id!.Value)
				.ToListAsync(cn);

			return keepIds.Count != existingIds.Count;
		}

		private async Task SyncSubItems(long itemId, List<MeasurementParameterSubItem> posted, bool isNew, CancellationToken cn)
		{
			var repo = unitOfWork.Repository<MeasurementParameterSubItem>();

			if (!isNew)
			{
				var keepIds = posted.Where(s => s.Id is > 0).Select(s => s.Id!.Value).ToHashSet();
				var existing = await repo.Table.Where(s => s.MeasurementParameterItemId == itemId).ToListAsync(cn);
				foreach (var row in existing.Where(s => s.Id is > 0 && !keepIds.Contains(s.Id.Value)))
					await repo.DeleteAsync(row, cn, true);
			}

			foreach (var row in posted.Where(s => isNew || s.Id is null or 0))
			{
				await repo.AddAsync(new MeasurementParameterSubItem
				{
					MeasurementParameterItemId = itemId,
					Title = row.Title
				}, cn, true, true);
			}
		}

		private async Task<MeasurementParameterItem?> Load(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<MeasurementParameterItem>().TableNoTracking
				.Include(x => x.MeasurementParameter)
				.Include(x => x.SubItems)
				.FirstOrDefaultAsync(x => x.Id == id, cn);

			if (entity?.SubItems != null)
				entity.SubItems = entity.SubItems.OrderBy(s => s.Id).ToList();

			return entity;
		}
	}
}
