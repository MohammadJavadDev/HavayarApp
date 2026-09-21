using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Sup;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	/// <summary>
	/// پیشینه ارسال به پیمانکاران — معادل صفحه 545 HTS.
	/// دسترسی مستقل از «پیشینه درخواست‌ها» (D26). فقط‌خواندنی.
	/// </summary>
	[Route("Panel/Sup/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("پیشینه ارسال به پیمانکاران", typeof(OpenOrderRequestEmailToSupplier))]
	public class OpenOrderRequestEmailToSupplierController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		public const string ReadOnlyMessage = "پیشینه ارسال به پیمانکار فقط‌خواندنی است و از تنظیمات درخواست باز ثبت می‌شود";

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public IActionResult Save(OpenOrderRequestEmailToSupplier _) => BadRequest(ReadOnlyMessage);

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public IActionResult Add(OpenOrderRequestEmailToSupplier _) => BadRequest(ReadOnlyMessage);

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public IActionResult Update(OpenOrderRequestEmailToSupplier _) => BadRequest(ReadOnlyMessage);

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public IActionResult Delete(long id) => BadRequest(ReadOnlyMessage);

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New() => BadRequest(ReadOnlyMessage);

		[HttpGet("[action]")]
		[ActionDisplayName("مشاهده اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id == null || id == 0)
				throw new Exception("شناسه ارسال مشخص نشده است.");

			var entity = unitOfWork.Repository<OpenOrderRequestEmailToSupplier>().TableNoTracking
				.Include(e => e.OpenOrderRequest)
					.ThenInclude(o => o.Part)
				.Include(e => e.Supplier)
					.ThenInclude(s => s.Party)
				.Include(e => e.SenderUser)
				.FirstOrDefault(e => e.Id == id);

			if (entity == null)
				throw new Exception("رکورد ارسال به پیمانکار یافت نشد.");

			return View(@"\Views\Panel\Sup\OpenOrderRequestEmailToSupplier\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Sup\OpenOrderRequestEmailToSupplier\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<OpenOrderRequestEmailToSupplier>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
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
			return Ok(await unitOfWork.Repository<OpenOrderRequestEmailToSupplier>().FetchDataAsync(request, cn));
		}
	}
}
