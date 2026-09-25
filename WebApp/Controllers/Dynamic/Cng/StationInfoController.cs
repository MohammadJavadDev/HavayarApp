using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Cng;
using Entities.App.Sale;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Cng/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("اطلاعات مشتری/جایگاه", typeof(StationInfo))]
	public class StationInfoController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		// HTS ShowAll (صفحه 434): فیلتر ردیف در نمایه Cng_StationInfo_List با نقش‌های
		// Cng.StationInfo.Manage / Cng.StationInfo.ShowAll / ShowAllMenus / admin انجام می‌شود.

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(StationInfo model, CancellationToken cn)
		{
			if (model.CustomerId == null || model.CustomerId == 0)
				return BadRequest("مشتری نمی‌تواند خالی باشد");
			if (string.IsNullOrWhiteSpace(model.StationTitle))
				return BadRequest("جایگاه نمی‌تواند خالی باشد");
			if (model.EquipmentType == null)
				return BadRequest("نوع تجهیز نمی‌تواند خالی باشد");
			if (string.IsNullOrWhiteSpace(model.RepresentativeName))
				return BadRequest("عنوان و سمت نماینده نمی‌تواند خالی باشد");
			if (string.IsNullOrWhiteSpace(model.RepresentativeEmailAddress)
				&& string.IsNullOrWhiteSpace(model.RepresentativeMobileNumber))
				return BadRequest("حداقل یکی از فیلدهای آدرس ایمیل نماینده یا شماره موبایل نماینده باید پر شود");

			model.Customer = null;

			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<StationInfo>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(StationInfo model, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<StationInfo>().SaveAsync(model, cn, true));

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(StationInfo model, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<StationInfo>().UpdateAsync(model, cn, true));

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<StationInfo>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<StationInfo>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			var entity = (id != null && id != 0)
				? unitOfWork.Repository<StationInfo>().TableNoTracking
					.Include(c => c.Customer!).ThenInclude(c => c.Party)
					.FirstOrDefault(c => c.Id == id)
				: new StationInfo();
			return View(@"\Views\Panel\Cng\StationInfo\Edit.cshtml", entity ?? new StationInfo());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
			=> View(@"\Views\Panel\Cng\StationInfo\Edit.cshtml", new StationInfo());

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Cng\StationInfo\List.cshtml");

		/// <summary>
		/// لیست سریال‌های سفارش فروش مشتری — معادل HTS GetSerials / GetSaleOrderDetailSerialView.
		/// فیلتر از مسیر Order.CustomerId است؛ روی Sale.OrderDetailSerial.CustomerId تکیه نکنید
		/// (در دادهٔ مهاجرت‌شده اغلب NULL است و combo خالی می‌ماند).
		/// customerId از query می‌آید (مثل GetCustomerInfo) — $$.post JSON بادی به پارامتر ساده bind نمی‌شود.
		/// </summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> GetSerials(long? customerId, CancellationToken cn)
		{
			var result = new List<object>();
			if (customerId != null && customerId != 0)
			{
				var serials = await unitOfWork.Repository<OrderDetailSerial>().TableNoTracking
					.Where(s =>
						s.Serial != null && s.Serial != ""
						&& s.OrderDetail != null
						&& s.OrderDetail.Sale_Order != null
						&& s.OrderDetail.Sale_Order.CustomerId == customerId)
					.OrderByDescending(s => s.Id)
					.Select(s => new { id = s.Id, text = s.Serial })
					.ToListAsync(cn);

				result.AddRange(serials);
			}

			result.Add(new { id = 1000L, text = "ورود دستی سریال" });
			return Ok(result);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + @"\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<StationInfo>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<StationInfo>().FetchDataAsync(request, cn));
	}
}
