using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App.Sale;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic.Sale
{
	[Route("Panel/Sale/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("سریال اقلام حواله فروش", typeof(OrderDetailSerial))]
	public class OrderDetailSerialController(
		IUnitOfWork unitOfWork,
		IPropertyIdentityService identityService,
		IWebHostEnvironment _webHostEnvironment,
		IConfiguration configuration) : BaseController
	{
		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, long? orderDetailId = null, CancellationToken cn = default)
		{
			if (id != null && id > 0)
			{
				var entity = await LoadSerialAsync(id.Value, cn);
				if (entity != null)
					return View(@"\Views\Panel\Sale\OrderDetailSerial\Edit.cshtml", entity);
			}

			return await New(orderDetailId, cn);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش مانیتورینگ فروش", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> MonitoringEdit(long? id, CancellationToken cn = default)
		{
			if (id == null || id <= 0)
				return BadRequest("سریال یافت نشد");

			var entity = await LoadSerialAsync(id.Value, cn);
			if (entity == null)
				return BadRequest("سریال یافت نشد");

			FillMonitoringAccessBags();
			return View(@"\Views\Panel\Sale\OrderDetailSerial\MonitoringEdit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public async Task<IActionResult> New(long? orderDetailId = null, CancellationToken cn = default)
		{
			var newEntity = new OrderDetailSerial { OrderDetailId = orderDetailId };

			if (orderDetailId != null && orderDetailId > 0)
			{
				newEntity.OrderDetail = await unitOfWork.Repository<OrderDetail>().TableNoTracking
					.Include(od => od.Part)
					.Include(od => od.Customer).ThenInclude(c => c.Party)
					.Include(od => od.Sale_Order).ThenInclude(o => o.Customer).ThenInclude(c => c.Party)
					.FirstOrDefaultAsync(od => od.Id == orderDetailId, cn);

				if (newEntity.OrderDetail != null)
				{
					newEntity.CustomerId = newEntity.OrderDetail.CustomerId ?? newEntity.OrderDetail.Sale_Order?.CustomerId;
				}
			}

			return View(@"\Views\Panel\Sale\OrderDetailSerial\Edit.cshtml", newEntity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(OrderDetailSerial serial, CancellationToken cn = default)
		{
			if (serial.Id == null || serial.Id == 0)
			{
				return await Add(serial, cn);
			}
			return await Update(serial, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره مانیتورینگ فروش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> MonitoringSave(OrderDetailSerial posted, CancellationToken cn = default)
		{
			if (posted.Id == null || posted.Id == 0)
				return BadRequest("مانیتورینگ فقط ویرایش عملیاتی است");

			var existing = await unitOfWork.Repository<OrderDetailSerial>().Table
				.FirstOrDefaultAsync(c => c.Id == posted.Id, cn);
			if (existing == null)
				return BadRequest("سریال یافت نشد");

			var canInventory = HasMonitoringAction("monitoringinventoryaccess");
			var canExit = HasMonitoringAction("monitoringexitaccess");
			var canIndustries = CanEditIndustriesFields(canInventory);

			if (!canInventory && !canExit && !canIndustries)
				return BadRequest("دسترسی ویرایش مانیتورینگ را ندارید");

			if (canIndustries)
			{
				if (string.IsNullOrWhiteSpace(posted.LaunchShamsiDate)
					|| posted.GuaranteeTime == null
					|| posted.GuaranteeSendDay == null
					|| posted.GuaranteeLaunchDay == null)
					return BadRequest("تاریخ راه‌اندازی و سه فیلد گارانتی الزامی است");

				existing.LaunchShamsiDate = posted.LaunchShamsiDate;
				existing.LaunchMiladiDate = posted.LaunchMiladiDate;
				existing.GuaranteeTime = posted.GuaranteeTime;
				existing.GuaranteeSendDay = posted.GuaranteeSendDay;
				existing.GuaranteeLaunchDay = posted.GuaranteeLaunchDay;
				existing.HasInsurance = posted.HasInsurance;
				existing.IndustrialComment = posted.IndustrialComment;
			}

			if (canInventory)
			{
				existing.SendShamsiDate = posted.SendShamsiDate;
				existing.SendMiladiDate = posted.SendMiladiDate;
				existing.SendTime = posted.SendTime;
				existing.ExitFactoryShamsiDate = posted.ExitFactoryShamsiDate;
				existing.ExitFactoryMiladiDate = posted.ExitFactoryMiladiDate;
				existing.ExitFactoryTime = posted.ExitFactoryTime;
				existing.InvComment = posted.InvComment;
			}

			if (canExit)
			{
				existing.FinalExitShamsiDate = posted.FinalExitShamsiDate;
				existing.FinalExitMiladiDate = posted.FinalExitMiladiDate;
				existing.ExitTimeFinal = posted.ExitTimeFinal;
			}

			var entity = await unitOfWork.Repository<OrderDetailSerial>().UpdateAsync(existing, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(OrderDetailSerial serial, CancellationToken cn = default)
		{
			ClearNavigations(serial);
			var entity = await unitOfWork.Repository<OrderDetailSerial>().SaveAsync(serial, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(OrderDetailSerial serial, CancellationToken cn = default)
		{
			ClearNavigations(serial);
			var entity = await unitOfWork.Repository<OrderDetailSerial>().UpdateAsync(serial, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[HttpDelete("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn = default)
		{
			var model = await unitOfWork.Repository<OrderDetailSerial>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);

			if (model == null)
				return BadRequest("سریال یافت نشد");

			await unitOfWork.Repository<OrderDetailSerial>().DeleteAsync(model, cn, true);
			return Ok(new { isSuccess = true, message = "با موفقیت حذف شد" });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
			=> View(@"\Views\Panel\Sale\OrderDetailSerial\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("مانیتورینگ فروش", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult Monitoring()
			=> View(@"\Views\Panel\Sale\OrderDetailSerial\Monitoring.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<OrderDetailSerial>().FetchDataAsync(request, cn));

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<OrderDetailSerial>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("تایید صنایع", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> IndustrialConfirm(long id, CancellationToken cn)
		{
			if (!HasMonitoringAction("industrialconfirm"))
				return BadRequest("دسترسی تایید صنایع را ندارید");

			var entity = await unitOfWork.Repository<OrderDetailSerial>().Table.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null)
				return BadRequest("سریال یافت نشد");
			entity.IndustrialConfirm = entity.IndustrialConfirm != true;
			await unitOfWork.Repository<OrderDetailSerial>().UpdateAsync(entity, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("اقلام پروژه", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> GetProjectParts(long id, CancellationToken cn)
		{
			var serial = await unitOfWork.Repository<OrderDetailSerial>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (serial == null)
				return BadRequest("سریال یافت نشد");

			// HTS default: Get_Sale_ProjectProduct_AllPart(parentId = serial identity). Change this when the filter decision arrives.
			var parentId = serial.HtsId > 0 ? serial.HtsId : serial.Id ?? 0;
			var queryResult = await QueryHamkaranProjectPartsAsync(parentId, cn);
			return Ok(new
			{
				items = queryResult.Items,
				connectionMissing = queryResult.ConnectionMissing,
				message = queryResult.Message,
				parentId
			});
		}

		[HttpGet("[action]")]
		[ActionDisplayName("مشاهده قیمت", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult ShowPrice() => Ok();

		[HttpGet("[action]")]
		[ActionDisplayName("ثبت ساعت خروج (مانیتورینگ)", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult MonitoringExitAccess()
			=> Ok(new { hasAccess = HasMonitoringAction("monitoringexitaccess") });

		[HttpGet("[action]")]
		[ActionDisplayName("دسترسی انبار (مانیتورینگ)", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult MonitoringInventoryAccess()
			=> Ok(new { hasAccess = HasMonitoringAction("monitoringinventoryaccess") });

		private async Task<OrderDetailSerial?> LoadSerialAsync(long id, CancellationToken cn)
		{
			return await unitOfWork.Repository<OrderDetailSerial>().TableNoTracking
				.Include(s => s.PlaningProject)
				.Include(s => s.Customer).ThenInclude(c => c.Party)
				.Include(s => s.CustomerAddress)
				.Include(s => s.OrderDetail).ThenInclude(od => od.Part)
				.Include(s => s.OrderDetail).ThenInclude(od => od.Customer).ThenInclude(c => c.Party)
				.Include(s => s.OrderDetail).ThenInclude(od => od.Sale_Order).ThenInclude(o => o.Customer).ThenInclude(c => c.Party)
				.FirstOrDefaultAsync(s => s.Id == id, cn);
		}

		private static void ClearNavigations(OrderDetailSerial serial)
		{
			serial.PlaningProject = null!;
			serial.Customer = null!;
			serial.CustomerAddress = null!;
			serial.OrderDetail = null!;
		}

		private void FillMonitoringAccessBags()
		{
			var canInventory = HasMonitoringAction("monitoringinventoryaccess");
			var canExit = HasMonitoringAction("monitoringexitaccess");
			ViewBag.CanEditInventory = canInventory;
			ViewBag.CanEditExit = canExit;
			ViewBag.CanEditIndustries = CanEditIndustriesFields(canInventory);
			ViewBag.CanIndustrialConfirm = HasMonitoringAction("industrialconfirm");
		}

		private bool CanEditIndustriesFields(bool canInventory)
		{
			if (IsAdministrator)
				return true;
			if (CurrentUserHasRole("Sale.AfterSales") || CurrentUserHasRole("ShowAllMenus"))
				return true;
			return !canInventory;
		}

		private bool HasMonitoringAction(string action)
		{
			if (IsAdministrator)
				return true;
			var needle = "/panel/sale/orderdetailserial/" + action;
			return sdk.CurrentUser?.RoleAccess?.Any(c =>
				c.Path != null && c.Path.Equals(needle, StringComparison.OrdinalIgnoreCase)) == true;
		}

		private async Task<(List<OrderDetailSerialProjectPartRow> Items, bool ConnectionMissing, string? Message)> QueryHamkaranProjectPartsAsync(long parentId, CancellationToken cn)
		{
			var htsCs = configuration.GetConnectionString("Hts");
			if (string.IsNullOrWhiteSpace(htsCs))
				return ([], true, "رشته اتصال Hts / راهکاران تنظیم نشده است");

			// HTS Get_Sale_ProjectProduct_AllPart: newhavayar strings are SQL_Latin1_General_CP1256_CI_AS,
			// ERPS/Rahkaran strings (and i.Number) are Persian_100_CI_AI. VchNum is int vs nvarchar.
			// Force every UNION / varchar-compare text column to the Rahkaran collation HTS already used.
			const string sql = @"
SELECT
	a.VchItmID,
	CONVERT(nvarchar(200), a.StockName) COLLATE Persian_100_CI_AI AS StockName,
	CONVERT(nvarchar(50), a.Vchdate) COLLATE Persian_100_CI_AI AS Vchdate,
	CONVERT(nvarchar(50), a.VchNum) COLLATE Persian_100_CI_AI AS VchNum,
	CONVERT(nvarchar(100), a.PartCode) COLLATE Persian_100_CI_AI AS PartCode,
	CONVERT(nvarchar(400), a.PartName) COLLATE Persian_100_CI_AI AS PartName,
	a.Qty - ISNULL(b.Qty, 0) AS Qty
FROM
(
	SELECT
		I.VchItmID,
		I.StockRef,
		S.StockName,
		dbo.GregorianToPersian(H.VchDate) AS Vchdate,
		H.VchNum,
		P.PartCode,
		P.PartName,
		I.Qty
	FROM erp.newhavayar.INV.InvVchItm AS I
	INNER JOIN erp.newhavayar.INV.InvVchHdr AS H ON H.VchHdrID = I.VchHdrRef
	INNER JOIN erp.newhavayar.INV.Part AS P ON P.Serial = I.PartRef
	INNER JOIN erp.newhavayar.INV.Stock AS S ON S.Serial = H.StockRef
	WHERE I.VchType = 53 AND I.DlRef = @parentId
) AS a
LEFT JOIN
(
	SELECT ii.RefNum, ii.StockRef, ii.Qty
	FROM erp.newhavayar.INV.InvVchItm AS ii
	WHERE ii.VchType = 07 AND ii.DlRef = @parentId
) AS b ON b.RefNum = a.VchItmID AND a.StockRef = b.StockRef
WHERE a.Qty - ISNULL(b.Qty, 0) > 0
UNION
SELECT
	a.VchItmID,
	CONVERT(nvarchar(200), a.StockName) COLLATE Persian_100_CI_AI AS StockName,
	CONVERT(nvarchar(50), a.Vchdate) COLLATE Persian_100_CI_AI AS Vchdate,
	CONVERT(nvarchar(50), a.VchNum) COLLATE Persian_100_CI_AI AS VchNum,
	CONVERT(nvarchar(100), a.PartCode) COLLATE Persian_100_CI_AI AS PartCode,
	CONVERT(nvarchar(400), a.PartName) COLLATE Persian_100_CI_AI AS PartName,
	a.Qty - ISNULL(b.Qty, 0) AS Qty
FROM
(
	SELECT
		iv.InventoryVoucherItemID AS VchItmID,
		iv.StoreRef AS StockRef,
		s.[Name] AS StockName,
		dbo.GregorianToPersian(i.[date]) AS Vchdate,
		i.Number AS VchNum,
		p.Code AS PartCode,
		p.[Name] AS PartName,
		iv.Quantity AS Qty
	FROM ERPS.ERPS.LGS3.InventoryVoucherItem AS iv
	INNER JOIN ERPS.ERPS.LGS3.InventoryVoucher AS i ON i.InventoryVoucherID = iv.InventoryVoucherRef
	INNER JOIN ERPS.ERPS.LGS3.Part AS p ON p.PartID = iv.PartRef
	INNER JOIN ERPS.ERPS.LGS3.Store AS s ON s.StoreID = i.StoreRef
	WHERE iv.InventoryVoucherSpecificationRef = 53
		AND iv.CounterpartDLCode COLLATE Persian_100_CI_AI = @parentIdText COLLATE Persian_100_CI_AI
) AS a
LEFT JOIN
(
	SELECT InventoryVoucherRef AS RefNum, StoreRef AS StockRef, Quantity AS Qty
	FROM ERPS.ERPS.LGS3.InventoryVoucherItem
	WHERE InventoryVoucherSpecificationRef = 07
		AND CounterpartDLCode COLLATE Persian_100_CI_AI = @parentIdText COLLATE Persian_100_CI_AI
) AS b ON b.RefNum = a.VchItmID AND a.StockRef = b.StockRef
WHERE a.Qty - ISNULL(b.Qty, 0) > 0";

			try
			{
				await using var conn = new SqlConnection(htsCs);
				await conn.OpenAsync(cn);
				await using var cmd = new SqlCommand(sql, conn);
				cmd.Parameters.Add(new SqlParameter("@parentId", parentId));
				cmd.Parameters.Add(new SqlParameter("@parentIdText", parentId.ToString()));
				await using var reader = await cmd.ExecuteReaderAsync(cn);
				var rows = new List<OrderDetailSerialProjectPartRow>();
				while (await reader.ReadAsync(cn))
				{
					rows.Add(new OrderDetailSerialProjectPartRow
					{
						VchItmId = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0)),
						StockName = reader.IsDBNull(1) ? null : reader.GetValue(1)?.ToString(),
						VchDate = reader.IsDBNull(2) ? null : reader.GetValue(2)?.ToString(),
						VchNum = reader.IsDBNull(3) ? null : reader.GetValue(3)?.ToString(),
						PartCode = reader.IsDBNull(4) ? null : reader.GetValue(4)?.ToString(),
						PartName = reader.IsDBNull(5) ? null : reader.GetValue(5)?.ToString(),
						Qty = reader.IsDBNull(6) ? 0 : Convert.ToDecimal(reader.GetValue(6))
					});
				}
				return (rows, false, null);
			}
			catch (Exception ex)
			{
				return ([], true, "خواندن اقلام پروژه از راهکاران ممکن نشد: " + ex.Message);
			}
		}

		private sealed class OrderDetailSerialProjectPartRow
		{
			public int VchItmId { get; set; }
			public string? StockName { get; set; }
			public string? VchDate { get; set; }
			public string? VchNum { get; set; }
			public string? PartCode { get; set; }
			public string? PartName { get; set; }
			public decimal Qty { get; set; }
		}
	}
}
