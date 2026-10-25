using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Pln;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Pln/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("کسری اقلام سفارش ساخت", typeof(ProductionOrderItemDeficit))]
	public class ProductionOrderItemDeficitController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		private const string ImportExcelRoleName = "Pln.ProductionOrerItemDeficit.ImportExcell";
		private const string SupplierEditRoleName = "Pln.ProductionOrerItemDeficit.SupplierEdit";
		private const string IndustriesEditRoleName = "Pln.ProductionOrerItemDeficit.IndustriesEdit";

		public class SupplierBatchItem
		{
			public long Id { get; set; }
			public string? SupplierUnitForecastDate { get; set; }
			public string? SupplierDescription { get; set; }
			public string? PurchaseExpert { get; set; }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ProductionOrderItemDeficit productionOrderItemDeficit, CancellationToken cn)
		{
			if (productionOrderItemDeficit.Id == null || productionOrderItemDeficit.Id == 0)
			{
				return await Add(productionOrderItemDeficit, cn);
			}
			var exist = await unitOfWork.Repository<ProductionOrderItemDeficit>().TableNoTracking.AnyAsync(c => c.Id == productionOrderItemDeficit.Id);
			if (exist)
			{
				return await Update(productionOrderItemDeficit, cn);
			}
			return await Add(productionOrderItemDeficit, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(ProductionOrderItemDeficit productionOrderItemDeficit, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ProductionOrderItemDeficit>().SaveAsync(productionOrderItemDeficit, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ProductionOrderItemDeficit productionOrderItemDeficit, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ProductionOrderItemDeficit>().UpdateAsync(productionOrderItemDeficit, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<ProductionOrderItemDeficit>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<ProductionOrderItemDeficit>().DeleteAsync(model, cn, true);
			return Ok();
		}

		/// <summary>
		/// حذف تمام ردیف‌های کسری اقلام سفارش ساخت. فقط گروه ورود با اکسل (و مدیر سیستم) مجاز است.
		/// </summary>
		[HttpPost("[action]")]
		[ActionDisplayName("حذف همه", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> DeleteAll(CancellationToken cn)
		{
			if (!IsAdministrator && !CurrentUserHasAnyRole(ImportExcelRoleName))
				return BadRequest("شما دسترسی حذف همه را ندارید");

			var deletedCount = await unitOfWork.Repository<ProductionOrderItemDeficit>().Table
				.ExecuteDeleteAsync(cn);

			return Ok(new { deletedCount });
		}

		/// <summary>
		/// ذخیره گروهی فیلدهای تامین‌کننده (تاریخ پیش‌بینی تامین، توضیح تامین‌کننده، متولی خرید) از روی نمایه داده.
		/// فقط رکوردهای ارسال‌شده و فقط فیلدهای ارسال‌شده (null = بدون تغییر، "" = پاک کردن) به‌روزرسانی می‌شوند؛
		/// پس ویرایش هم‌زمان رکوردهای دیگر توسط کاربران دیگر اثری ندارد.
		/// کنترل دسترسی با نقش‌های SupplierEdit / IndustriesEdit (مثل صفحه Edit) است.
		/// </summary>
		[HttpPost("[action]")]
		public async Task<IActionResult> SaveSupplierBatch([FromBody] List<SupplierBatchItem> items, CancellationToken cn)
		{
			if (!IsAdministrator && !CurrentUserHasAnyRole(SupplierEditRoleName, IndustriesEditRoleName))
				return BadRequest("شما دسترسی ویرایش اطلاعات تامین‌کننده را ندارید");

			if (items == null || items.Count == 0)
				return BadRequest("رکوردی برای ذخیره ارسال نشده است");

			// ابتدا همه ورودی‌ها اعتبارسنجی می‌شوند تا ذخیره نیمه‌کاره رخ ندهد
			var updates = new List<(long Id, Dictionary<string, object> Fields)>();
			foreach (var item in items.GroupBy(i => i.Id).Select(g => g.Last()))
			{
				var fields = new Dictionary<string, object>();

				if (item.SupplierUnitForecastDate != null)
				{
					var text = item.SupplierUnitForecastDate.Trim();
					DateTime? miladi = null;
					if (text.Length > 0)
					{
						try { miladi = text.ToMiladiDateTimeWithSplitData(); }
						catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
						{
							return BadRequest($"تاریخ پیش‌بینی تامین رکورد {item.Id} معتبر نیست: {text}");
						}
					}
					fields[nameof(ProductionOrderItemDeficit.SupplierUnitForecastDateMiladiDateTime)] = miladi!;
					fields[nameof(ProductionOrderItemDeficit.SupplierUnitForecastDateShamsiDateTime)] =
						miladi.HasValue ? ToShamsiText(miladi.Value) : null!;
				}

				if (item.SupplierDescription != null)
				{
					var text = item.SupplierDescription.Trim();
					if (text.Length > 1024)
						return BadRequest($"توضیح تامین‌کننده رکورد {item.Id} بیش از ۱۰۲۴ کاراکتر است");
					fields[nameof(ProductionOrderItemDeficit.SupplierDescription)] = text.Length == 0 ? null! : text;
				}

				if (item.PurchaseExpert != null)
				{
					var text = item.PurchaseExpert.Trim();
					if (text.Length > 64)
						return BadRequest($"متولی خرید رکورد {item.Id} بیش از ۶۴ کاراکتر است");
					fields[nameof(ProductionOrderItemDeficit.PurchaseExpert)] = text.Length == 0 ? null! : text;
				}

				if (fields.Count > 0)
					updates.Add((item.Id, fields));
			}

			if (updates.Count == 0)
				return BadRequest("تغییری برای ذخیره وجود ندارد");

			var repository = unitOfWork.Repository<ProductionOrderItemDeficit>();
			var notFound = new List<long>();
			foreach (var (id, fields) in updates)
			{
				if (!await repository.UpdateFieldsAsync(id, fields, cn))
					notFound.Add(id);
			}

			if (notFound.Count > 0)
				return BadRequest($"رکورد(های) {string.Join("، ", notFound)} پیدا نشد (ممکن است حذف شده باشد). بقیه رکوردها ذخیره شدند.");

			return Ok(new { updated = updates.Count });
		}

		private static string ToShamsiText(DateTime date)
		{
			var calendar = new System.Globalization.PersianCalendar();
			return $"{calendar.GetYear(date):0000}/{calendar.GetMonth(date):00}/{calendar.GetDayOfMonth(date):00}";
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<ProductionOrderItemDeficit>().TableNoTracking
					.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Pln\ProductionOrderItemDeficit\Edit.cshtml", entity);
			}
			var newEntity = new ProductionOrderItemDeficit();
			return View(@"\Views\Panel\Pln\ProductionOrderItemDeficit\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var newEntity = new ProductionOrderItemDeficit();
			return View(@"\Views\Panel\Pln\ProductionOrderItemDeficit\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Pln\ProductionOrderItemDeficit\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ProductionOrderItemDeficit>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<ProductionOrderItemDeficit>().FetchDataAsync(request, cn));
		}
	}
}
