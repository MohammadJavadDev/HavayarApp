using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Sup;

namespace WebApp.Controllers.Dynamic
{
	/// <summary>
	/// دسته های خرید (والد) — معادل صفحه 70 HTS «دسته های خرید».
	/// تصمیم Q2 = (a): راهکاران (USR3.Sup_BuyCategory / Sup_BuyCategoryItems) مرجع کامل است و جاب ساعتی
	/// SyncBuyCategoryJobFromRahkaran همگام می‌کند. در این سامانه دسته، اقلام، متولی خرید و lead time **فقط‌خواندنی** هستند.
	/// اکشن‌های نوشتن (Save/Add/Update/Delete/New) دفاعی باقی مانده‌اند و همیشه isSuccess=false برمی‌گردانند.
	/// </summary>
	[Route("Panel/Sup/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("دسته های خرید", typeof(BuyCategory))]
	public class BuyCategoryController(IUnitOfWork unitOfWork, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		/// <summary>
		/// حداکثر تعداد قلمی که به‌صورت جدول درون‌خطی در صفحه نمایش دسته رندر می‌شود.
		/// دسته‌های بزرگ (مثلاً Flange-Fitting با ~۲٬۵۰۰ قلم) به صفحه «اقلام دسته های خرید» ارجاع می‌شوند.
		/// </summary>
		public const int MaxInlineItems = 300;

		public const string ReadOnlyMessage = "دسته‌های خرید از راهکاران همگام می‌شوند و در این سامانه قابل ویرایش نیستند";

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public IActionResult Save(BuyCategory buyCategory) => BadRequest(ReadOnlyMessage);

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public IActionResult Add(BuyCategory buyCategory) => BadRequest(ReadOnlyMessage);

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public IActionResult Update(BuyCategory buyCategory) => BadRequest(ReadOnlyMessage);

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public IActionResult Delete(long id) => BadRequest(ReadOnlyMessage);

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New() => BadRequest(ReadOnlyMessage);

		/// <summary>صفحه نمایش (فقط‌خواندنی) یک دسته خرید با اقلام آن.</summary>
		[HttpGet("[action]")]
		[ActionDisplayName("مشاهده اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id == null || id == 0)
				throw new Exception("شناسه دسته خرید مشخص نشده است.");

			var entity = unitOfWork.Repository<BuyCategory>().TableNoTracking
				.Include(c => c.PurchaseResponsible)
				.Include(c => c.Items.OrderBy(i => i.Id))
					.ThenInclude(i => i.Part)
				.FirstOrDefault(c => c.Id == id);

			if (entity == null)
				throw new Exception("دسته خرید یافت نشد.");

			return View(@"\Views\Panel\Sup\BuyCategory\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Sup\BuyCategory\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<BuyCategory>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<BuyCategory>().FetchDataAsync(request, cn));
		}
	}

	/// <summary>
	/// اقلام دسته های خرید — معادل صفحه 72 HTS «اقلام دسته های خرید». داده از راهکاران (USR3.Sup_BuyCategoryItems).
	/// Q2 = (a): فقط‌خواندنی؛ اکشن‌های نوشتن دفاعی‌اند و همیشه isSuccess=false برمی‌گردانند.
	/// </summary>
	[Route("Panel/Sup/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("اقلام دسته های خرید", typeof(BuyCategoryItem))]
	public class BuyCategoryItemController(IUnitOfWork unitOfWork, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		public const string ReadOnlyMessage = "اقلام دسته‌های خرید از راهکاران همگام می‌شوند و در این سامانه قابل ویرایش نیستند";

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public IActionResult Save(BuyCategoryItem buyCategoryItem) => BadRequest(ReadOnlyMessage);

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public IActionResult Add(BuyCategoryItem buyCategoryItem) => BadRequest(ReadOnlyMessage);

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public IActionResult Update(BuyCategoryItem buyCategoryItem) => BadRequest(ReadOnlyMessage);

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public IActionResult Delete(long id) => BadRequest(ReadOnlyMessage);

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Sup\BuyCategoryItem\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<BuyCategoryItem>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<BuyCategoryItem>().FetchDataAsync(request, cn));
		}

		/// <summary>پارشیال قلم (فقط برای سازگاری با ابزارهای قدیمی؛ صفحه نمایش دسته از آن استفاده نمی‌کند).</summary>
		[HttpGet("[action]")]
		public IActionResult BuyCategoryItemPartial()
		{
			return PartialView(@"\Views\Panel\Sup\BuyCategory\_BuyCategoryItemPartial.cshtml");
		}
	}
}
