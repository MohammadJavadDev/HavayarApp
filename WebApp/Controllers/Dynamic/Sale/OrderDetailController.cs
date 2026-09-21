using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App.Sale;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sale/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("حواله های فروش", typeof(OrderDetail))]
	public class OrderDetailController(
		IUnitOfWork unitOfWork,
		IPropertyIdentityService identityService,
		IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Sale\OrderDetail\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<OrderDetail>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
		{
			return Ok(await unitOfWork.Repository<OrderDetail>().FetchDataAsync(request, cn));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست سریال‌ها", ActionAccessType.Api)]
		public async Task<IActionResult> GetSerials(long orderDetailId, CancellationToken cn)
		{
			var serials = await unitOfWork.Repository<OrderDetailSerial>()
				.TableNoTracking
				.Include(s => s.PlaningProject)
				.Include(s => s.Customer)
					.ThenInclude(c => c.Party)
				.Where(s => s.OrderDetailId == orderDetailId)
				.OrderBy(s => s.Id)
				.Select(s => new
				{
					s.Id,
					s.OrderDetailId,
					s.Serial,
					s.Model,
					s.ProductionOrderNumber,
					s.PlaningProjectId,
					PlaningProjectName = s.PlaningProject != null ? s.PlaningProject.Name : null,
					s.CustomerId,
					CustomerName = s.Customer != null && s.Customer.Party != null
						? s.Customer.Party.FullName
						: null,
					s.HoursDaily,
					s.GuaranteeTime,
					s.GuaranteeSendDay,
					s.GuaranteeLaunchDay,
					s.IndustrialComment,
					s.HasInsurance,
					s.IndustrialConfirm,
					s.SendShamsiDate,
					s.LaunchShamsiDate,
					s.ExitFactoryShamsiDate
				})
				.ToListAsync(cn);

			return Ok(serials);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست سریال‌های قلم حواله فروش", ActionAccessType.View, ActionAccessItemType.List)]
		public async Task<IActionResult> ListByParentId(long orderDetailId, CancellationToken cn)
		{
			var detail = await unitOfWork.Repository<OrderDetail>()
				.TableNoTracking
				.Include(d => d.Part)
				.Include(d => d.Unit)
				.Include(d => d.Customer)
					.ThenInclude(c => c.Party)
				.Include(d => d.Sale_Order)
					.ThenInclude(o => o.Customer)
						.ThenInclude(c => c.Party)
				.Include(d => d.Sale_Order)
					.ThenInclude(o => o.Branch)
				.FirstOrDefaultAsync(d => d.Id == orderDetailId, cn);

			if (detail == null)
				return NotFound("قلم حواله فروش یافت نشد");

			var serials = await unitOfWork.Repository<OrderDetailSerial>()
				.TableNoTracking
				.Include(s => s.PlaningProject)
				.Include(s => s.Customer)
					.ThenInclude(c => c.Party)
				.Where(s => s.OrderDetailId == orderDetailId)
				.OrderBy(s => s.Id)
				.ToListAsync(cn);

			ViewBag.Serials = serials;
			ViewBag.CanIndustrialConfirm = HasIndustrialConfirmAccess();
			return View(@"\Views\Panel\Sale\OrderDetail\ListByParentId.cshtml", detail);
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetSerialListModal(long orderDetailId, CancellationToken cn)
		{
			var detail = await unitOfWork.Repository<OrderDetail>()
				.TableNoTracking
				.Include(d => d.Part)
				.Include(d => d.Customer)
					.ThenInclude(c => c.Party)
				.Include(d => d.Sale_Order)
					.ThenInclude(o => o.Customer)
						.ThenInclude(c => c.Party)
				.FirstOrDefaultAsync(d => d.Id == orderDetailId, cn);

			if (detail == null)
				return NotFound("قلم حواله فروش یافت نشد");

			return PartialView(@"\Views\Panel\Sale\OrderDetail\_SerialListModalPartial.cshtml", detail);
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetSerialEditModal(long? id, long orderDetailId, CancellationToken cn)
		{
			if (id != null && id > 0)
			{
				var entity = await unitOfWork.Repository<OrderDetailSerial>()
					.TableNoTracking
					.Include(s => s.PlaningProject)
					.Include(s => s.Customer)
						.ThenInclude(c => c.Party)
					.FirstOrDefaultAsync(s => s.Id == id, cn);

				if (entity != null)
					return PartialView(@"\Views\Panel\Sale\OrderDetail\_SerialEditPartial.cshtml", entity);
			}

			var newEntity = new OrderDetailSerial { OrderDetailId = orderDetailId };
			return PartialView(@"\Views\Panel\Sale\OrderDetail\_SerialEditPartial.cshtml", newEntity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره سریال", ActionAccessType.Api)]
		public async Task<IActionResult> SaveSerial(OrderDetailSerial serial, CancellationToken cn)
		{
			if (serial.PlaningProjectId == 0) serial.PlaningProjectId = null;
			if (serial.CustomerId == 0) serial.CustomerId = null;
			if (serial.CustomerAddressId == 0) serial.CustomerAddressId = null;

			serial.PlaningProject = null!;
			serial.Customer = null!;
			serial.CustomerAddress = null!;
			serial.OrderDetail = null!;

			OrderDetailSerial entity;
			if (serial.Id == null || serial.Id == 0)
			{
				entity = await unitOfWork.Repository<OrderDetailSerial>().SaveAsync(serial, cn, true);
			}
			else
			{
				entity = await unitOfWork.Repository<OrderDetailSerial>().UpdateAsync(serial, cn, true);
			}

			return Ok(new { isSuccess = true, message = "با موفقیت ذخیره شد", data = entity });
		}

		[HttpGet("[action]")]
		[HttpDelete("[action]")]
		[ActionDisplayName("حذف سریال", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> DeleteSerial(long id, CancellationToken cn)
		{
			var model = await unitOfWork.Repository<OrderDetailSerial>()
				.TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);

			if (model == null)
				return Ok(new { isSuccess = false, message = "سریال یافت نشد" });

			await unitOfWork.Repository<OrderDetailSerial>().DeleteAsync(model, cn, true);
			return Ok(new { isSuccess = true, message = "با موفقیت حذف شد" });
		}

		private bool HasIndustrialConfirmAccess()
		{
			if (IsAdministrator)
				return true;

			const string needle = "/panel/sale/orderdetailserial/industrialconfirm";
			return sdk.CurrentUser?.RoleAccess?.Any(c =>
				c.Path != null && c.Path.Equals(needle, StringComparison.OrdinalIgnoreCase)) == true;
		}
	}
}
