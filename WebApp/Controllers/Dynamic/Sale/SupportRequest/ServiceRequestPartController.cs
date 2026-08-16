using Common.Attributes;
using Common.Auth.Enums;
using Data;
using Data.Contracts;
using Entities.App.Sale;
using Entities.App.Sale.DTO;
using Entities.App.Sale.Enums;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sale/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("درخواست کالا", typeof(ServiceRequestPart))]
	public class ServiceRequestPartController(IUnitOfWork unitOfWork, IWebHostEnvironment _webHostEnvironment, HtsDbContext htsDbContext) : BaseController
	{
		private readonly HtsDbContext _htsDbContext = htsDbContext;

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ServiceRequestPart serviceRequestPart, CancellationToken cn)
		{
			if (serviceRequestPart.Id == null || serviceRequestPart.Id == 0)
			{
				return await Add(serviceRequestPart, cn);

			}
			var exist = await unitOfWork.Repository<ServiceRequestPart>().TableNoTracking.AnyAsync(c => c.Id == serviceRequestPart.Id);
			if (exist)
			{
				return await Update(serviceRequestPart, cn);
			}
			return await Add(serviceRequestPart, cn);
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> Add(ServiceRequestPart serviceRequestPart, CancellationToken cn)
		{
			try
			{
				var entity = await unitOfWork.Repository<ServiceRequestPart>().SaveAsync(serviceRequestPart, cn);
				return Ok(entity);
			}
			catch (DbUpdateException ex)
			{
				var message = ex.InnerException?.Message ?? ex.Message;
				return BadRequest(new { Error = "خطا در ذخیره‌سازی", Details = message });
			}
		}


		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ServiceRequestPart serviceRequestPart, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ServiceRequestPart>().UpdateAsync(serviceRequestPart, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<ServiceRequestPart>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<ServiceRequestPart>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<ServiceRequestPart>().TableNoTracking
				    .FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Sale\ServiceRequest\RequestPart\Edit.cshtml", entity);
			}
			var newEntity = new ServiceRequest();
			return View(@"\Views\Panel\Sale\ServiceRequest\RequestPart\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? serviceRequestId)
		{
			var newEntity = new ServiceRequestPart();
			newEntity.ServiceRequestId = serviceRequestId;
			return View(@"\Views\Panel\Sale\ServiceRequest\RequestPart\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Sale\ServiceRequest\RequestPart\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ServiceRequestPart>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<ServiceRequestPart>().FetchDataAsync(request, cn));
		}

		[HttpGet("[action]")]
		public IActionResult ShowProductDetailForm(long? serviceRequestId, CancellationToken cancellationToken)
		{
			ViewBag.ServiceRequestId = serviceRequestId;
			var model = new ServiceRequestDetail
			{
				ServiceRequestId = serviceRequestId
			};
			return PartialView(@"\Views\Panel\Sale\ServiceRequest\ProductDetail\Edit.cshtml", model);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره جزئیات", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> AddRepairProductDetail(ServiceRequestDetail serviceRequestDetail, CancellationToken cn)
		{
			if (serviceRequestDetail.Id == 0 || serviceRequestDetail.Id == null)
			{
				var entity = await unitOfWork.Repository<ServiceRequestDetail>().SaveAsync(serviceRequestDetail, cn, true);
				return Ok(new { isSuccess = true, message = "با موفقیت درج شد", data = entity });
			}
			else
			{
				var entity = await unitOfWork.Repository<ServiceRequestDetail>().UpdateAsync(serviceRequestDetail, cn, true);
				return Ok(new { isSuccess = true, message = "با موفقیت ویرایش شد", data = entity });
			}
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetServiceRequest(long serviceRequestId)
		{
			try
			{
				var list = await unitOfWork.Repository<ServiceRequestDetail>()
				    .TableNoTracking
				    .Where(it => it.ServiceRequestId == serviceRequestId)
				    .ToListAsync();

				return Ok(list);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
				Console.WriteLine($"Stack Trace: {ex.StackTrace}");

				if (ex.InnerException != null)
				{
					Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
				}

				return BadRequest(new
				{
					success = false,
					message = ex.Message,
					innerMessage = ex.InnerException?.Message
				});
			}
		}

		[HttpDelete("[action]")]
		public async Task<IActionResult> DeleteServiceRequest([FromQuery] long? id, CancellationToken cancellationToken)
		{
			var model = await unitOfWork.Repository<ServiceRequestDetail>().Table
			    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

			if (model == null)
				return NotFound(new { success = false, message = "رکورد یافت نشد" });

			await unitOfWork.Repository<ServiceRequestDetail>().DeleteAsync(model, cancellationToken, true);

			return Ok(new
			{
				data = new
				{
					success = true,
					message = "حذف با موفقیت انجام شد"
				}
			});

		}

		[HttpGet("[action]")]
		public async Task<IActionResult> EditServiceRequest(long? id, long? serviceRequestId, CancellationToken cancellationToken)
		{
			var entity = await unitOfWork.Repository<ServiceRequestDetail>().TableNoTracking
			   .FirstAsync(it => it.Id == id && it.ServiceRequestId == serviceRequestId);

			if (entity == null)
			{
				return NotFound("رکورد مورد نظر یافت نشد");
			}

			ViewBag.serviceRequestId = entity.ServiceRequestId;

			return PartialView(@"\Views\Panel\Sale\ServiceRequest\ProductDetail\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetRelatedProduct(long? serviceRequestId, CancellationToken cancellationToken)
		{
			if (serviceRequestId is null)
				return BadRequest("شناسه درخواست سرویس ارسال نشده است.");

			try
			{
				var products = await unitOfWork.Repository<ServiceRequestDetail>()
				    .TableNoTracking
				    .Where(srd => srd.ServiceRequestId == serviceRequestId.Value)
				    .Select(srd => new
				    {
					    Id = srd.Id,
					    PartCode = srd.Part != null ? srd.Part.Code : string.Empty,
					    PartName = srd.Part != null ? srd.Part.Name : string.Empty,
					    SendDate = srd.OrderDetail != null && srd.OrderDetail.Sale_Order != null
						? srd.OrderDetail.Sale_Order.VchDateShamsiDate
						: null,
					    Serial = srd.OrderDetailSerial != null ? srd.OrderDetailSerial.Serial : string.Empty
				    })
				    .ToListAsync(cancellationToken);

				var result = products.Select(x => new
				{
					id = x.Id,
					text = $"کد: {x.PartCode ?? "-"} | نام: {x.PartName ?? "-"} | تاریخ ارسال: {x.SendDate ?? "-"} | سریال: {x.Serial ?? "-"}"
				}).ToList();

				return Ok(result);
			}
			catch (OperationCanceledException)
			{
				return StatusCode(500, "درخواست لغو شد.");
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("GetPiecesDataAsync")]
		[ActionDisplayName("دریافت قطعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetPiecesDataAsync(int projectVchNum, long? SalesOfficeId, int projectVchYear, CancellationToken cancellationToken = default)
		{
			try
			{
				List<PieceDto> pieces;

				short vchNum = (short)projectVchNum;
				short vchYear = (short)projectVchYear;
				//SalesOfficeId = 19;
				if (SalesOfficeId.HasValue)
				{
					var data = await GetSalePrefactorPartAsync(
					    vchNum,
					    vchYear,
					    SalesOfficeId.Value.ToString(),
					    cancellationToken);

					pieces = data.Select(x => new PieceDto
					{
						Id = x.OrderDetailId,
						Date = x.VchDate ?? string.Empty,
						PartName = x.PartName,
						Qty = x.Qty,
						PartRef = x.PartRef,
						MunitRef = null,
						LastBuyPrice = x.LastBuyPrice
					}).ToList();
				}
				else
				{
					var voucherItems = await GetInventoryVoucherItemsAsync(
					    vchNum,
					    vchYear,
					    cancellationToken);

					pieces = voucherItems.Select(x => new PieceDto
					{
						Id = x.VchItmID,
						Date = x.VchDate ?? string.Empty,
						PartName = x.PartName,
						Qty = x.Qty,
						PartRef = x.PartRef,
						MunitRef = x.MunitRef,
						LastBuyPrice = x.LastBuyPrice
					}).ToList();
				}

				return Ok(new { pieces });
			}
			catch (Exception ex)
			{
				return BadRequest(new { isSuccess = false, message = ex.Message });
			}
		}

		public async Task<List<SalePrefactorPartDto>> GetSalePrefactorPartAsync(int projectVchNum, int projectVchYear, string branchIdOrOrderDetailId, CancellationToken cancellationToken = default)
		{
			const string Tsql = """
                                SELECT DISTINCT
                                    Sale_OrderDetail.OrderDetail_ID,
                                    VchDate_Shamsi AS VchDate,
                                    Part.Part_ID AS PartRef,
                                    Part.Part_Code + ' | ' + Part.Part_Name AS PartName,
                                    Qty,
                                    Branch_FK,
                                    LastPrice.UnitPrice AS LastBuyPrice
                                FROM Sale_OrderDetail
                                INNER JOIN Inv_Part AS Part
                                    ON Sale_OrderDetail.Part_FK = Part.Part_ID
                                INNER JOIN Sale_Order
                                    ON Sale_Order.Order_ID = Sale_OrderDetail.Order_FK
                                LEFT JOIN dbo.Vw_Bom_PartLastPriceSimple AS LastPrice
                                    ON LastPrice.Hamkaran_Part_FK = Part.Hamkaran_Part_FK
                                WHERE VchNo = @VchNo
                                  AND Year = @FiscalYear
                                  AND (Branch_FK = @BranchId OR OrderDetail_ID = @BranchId)
                                  AND Hamkaran_Vchitm_FK IN
                                  (
                                        SELECT Item.OrderItemID
                                        FROM ERPS.Erps.SLS3.OrderItem AS Item
                                        JOIN Erps.Erps.SLS3.[Order] AS SaleOrder
                                        ON SaleOrder.OrderID = Item.OrderRef
                                        JOIN ERPS.Erps.SLS3.SalesOffice AS SalesOffice
                                        ON SalesOffice.SalesOfficeID = SaleOrder.SalesOfficeRef
                                        WHERE Item.OrderNumber = @VchNo
                                        AND SaleOrder.FiscalYearRef = @FiscalYear
                                        AND SalesOffice.Code = @BranchId
                                  )
                                """;

			var result = new List<SalePrefactorPartDto>();

			var connection = _htsDbContext.Database.GetDbConnection();
			var dbState = connection.State;

			try
			{
				if (dbState != ConnectionState.Open)
					await connection.OpenAsync(cancellationToken);

				using var command = connection.CreateCommand();

				command.CommandText = Tsql;
				command.Transaction = _htsDbContext.Database.CurrentTransaction?.GetDbTransaction();

				AddParameter(command, "@VchNo", projectVchNum);
				AddParameter(command, "@FiscalYear", projectVchYear);
				AddParameter(command, "@BranchId", branchIdOrOrderDetailId);

				using var reader = await command.ExecuteReaderAsync(cancellationToken);

				while (await reader.ReadAsync(cancellationToken))
				{

					result.Add(new SalePrefactorPartDto
					{
						OrderDetailId = reader.GetInt32(reader.GetOrdinal("OrderDetail_ID")),
						VchDate = reader.GetString(reader.GetOrdinal("VchDate")),
						PartRef = reader.GetInt64(reader.GetOrdinal("PartRef")),
						PartName = reader.GetString(reader.GetOrdinal("PartName")),
						Qty = reader.GetDecimal(reader.GetOrdinal("Qty")),
						BranchFk = (int)reader.GetByte(reader.GetOrdinal("Branch_FK")),
						LastBuyPrice = reader.IsDBNull(reader.GetOrdinal("LastBuyPrice"))
						  ? 0
						  : reader.GetDecimal(reader.GetOrdinal("LastBuyPrice"))
					});
				}
			}
			finally
			{
				if (dbState != ConnectionState.Open &&
				    connection.State == ConnectionState.Open)
					await connection.CloseAsync();
			}

			return result;
		}
		public async Task<List<InventoryVoucherItemDto>> GetInventoryVoucherItemsAsync(int projectVchNum, int projectVchYear, CancellationToken cancellationToken = default)
		{
			const string tSql = """
                                SELECT  DISTINCT
                                Top(10)
                                       CAST(0 AS bit) AS RowSelector,
                                       i.InventoryVoucherItemID AS VchItmID,
                                       dbo.GregorianToPersian(h.[Date]) AS VchDate,
                                       i.PartRef,
                                       Part.Code + ' | ' + Part.Name AS PartName,
                                       i.ReferenceType AS MunitRef,
                                       i.Quantity - ISNULL(i2.SumQuantity, 0) AS Qty,
                                       LastPrice.UnitPrice AS LastBuyPrice
                                FROM Erps.Erps.LGS3.InventoryVoucherItem i
                                INNER JOIN Erps.Erps.LGS3.InventoryVoucher h
                                    ON i.InventoryVoucherRef = h.InventoryVoucherID
                                INNER JOIN Erps.Erps.LGS3.Part Part
                                    ON Part.PartID = i.PartRef
                                LEFT JOIN dbo.Vw_Bom_PartLastPriceSimple LastPrice
                                    ON LastPrice.Hamkaran_Part_FK = Part.PartID
                                LEFT JOIN
                                (
                                    SELECT
                                    InventoryVoucherItem.ReferenceRef,
                                    SUM(InventoryVoucherItem.Quantity) AS SumQuantity
                                    FROM Erps.Erps.LGS3.InventoryVoucherItem InventoryVoucherItem
                                    INNER JOIN Erps.Erps.LGS3.InventoryVoucherItem BaseVchItem
                                    ON BaseVchItem.InventoryVoucherItemID = InventoryVoucherItem.ReferenceRef
                                    WHERE InventoryVoucherItem.InventoryVoucherSpecificationRef = 7
                                    AND BaseVchItem.InventoryVoucherSpecificationRef = 53
                                    GROUP BY InventoryVoucherItem.ReferenceRef
                                ) i2
                                    ON i2.ReferenceRef = i.InventoryVoucherItemID
                                WHERE h.InventoryVoucherSpecificationRef = 53
                                  AND h.FiscalYearRef = @FiscalYear
                                  AND h.Number = @VchNo
                                  AND i.Quantity - ISNULL(i2.SumQuantity, 0) > 0;
                                ;
                                """;

			var result = new List<InventoryVoucherItemDto>();

			var connection = _htsDbContext.Database.GetDbConnection();
			var state = connection.State;

			_htsDbContext.Database.SqlQueryRaw<InventoryVoucherItemDto>("", new { });

			try
			{
				if (state != ConnectionState.Open)
					await connection.OpenAsync(cancellationToken);

				using var command = connection.CreateCommand();

				command.CommandText = tSql;
				command.Transaction = _htsDbContext.Database.CurrentTransaction?.GetDbTransaction();

				AddParameter(command, "@FiscalYear", projectVchYear);
				AddParameter(command, "@VchNo", projectVchNum);

				using var reader = await command.ExecuteReaderAsync(cancellationToken);

				while (await reader.ReadAsync(cancellationToken))
				{

					for (int i = 0; i < reader.FieldCount; i++)
					{
						Console.WriteLine($"{reader.GetName(i)} => {reader.GetFieldType(i)}");
					}

					result.Add(new InventoryVoucherItemDto
					{
						RowSelector = reader.GetBoolean(reader.GetOrdinal("RowSelector")),
						VchItmID = reader.GetInt64(reader.GetOrdinal("VchItmID")),
						VchDate = reader.GetString(reader.GetOrdinal("VchDate")),
						PartRef = reader.GetInt64(reader.GetOrdinal("PartRef")),
						PartName = reader.GetString(reader.GetOrdinal("PartName")),
						MunitRef = reader.IsDBNull(reader.GetOrdinal("MunitRef"))
							? null
							: Convert.ToInt64(reader["MunitRef"]),
						Qty = reader.GetDecimal(reader.GetOrdinal("Qty")),
						LastBuyPrice = reader.IsDBNull(reader.GetOrdinal("LastBuyPrice"))
						  ? null
						  : reader.GetDecimal(reader.GetOrdinal("LastBuyPrice"))
					});
				}
			}
			finally
			{
				if (state != ConnectionState.Open &&
				    connection.State == ConnectionState.Open)
					await connection.CloseAsync();
			}

			return result;
		}
		private static void AddParameter(DbCommand command, string name, object value)
		{
			var parameter = command.CreateParameter();
			parameter.ParameterName = name;
			parameter.Value = value;
			command.Parameters.Add(parameter);
		}


		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات با شناسه درخواست پشتیبانی", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult ListBy(long? serviceRequestId)
		{
			if (serviceRequestId == null || serviceRequestId == 0)
				throw new Exception("شناسه درخواست پشتیبانی نمیتواند خالی باشد.");

			var model = new ServiceRequestPartListByParentViewModel
			{
				ServiceRequestId = serviceRequestId.Value
			};

			return View(@"\Views\Panel\Sale\ServiceRequest\RequestPart\ListByParentId.cshtml", model);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست با شناسه درخواست پشتیبانی", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public IActionResult GetListByParentId(long? serviceRequestId)
		{
			if (serviceRequestId == null || serviceRequestId == 0)
				return BadRequest("شناسه درخواست پشتیبانی نمیتواند خالی باشد.");

			var items = unitOfWork
			    .Repository<ServiceRequestPart>()
			    .TableNoTracking
			    .Where(c => c.ServiceRequestId == serviceRequestId)
			    .Select(c => new ServiceRequestPartListItemViewModel
			    {
				    Id = c.Id,
				    ServiceRequestId = c.ServiceRequestId,
				    PartCode = c.Part.Code,
				    PartName = c.Part.Name,
				    PartId = c.PartId,
				    ProjectVchNum = c.ProjectVchNum,
				    SalesOfficeName = c.SalesOffice.Name,
				    ProjectPartUnitTitle = c.ProjectPartUnit.Title ?? "",
				    ProjectVchYear = c.ProjectVchYear,
				    ProjectVoucherShamsiDate = c.ProjectVoucherShamsiDate,
				    ProjectVchPartCode = c.ProjectVchPart.Code,
				    ProjectVchPartName = c.ProjectVchPart.Name,
				    ProjectVchPartId = c.ProjectVchPartId,
				    ReplacePartCode = c.ReplacePart.Code,
				    ReplacePartName = c.ReplacePart.Name,
				    CostCenterTitle = c.CostCenter.Title,
				    CostCenterId = c.CostCenterId,
				    SupplierName = c.Supplier.Party.FullName,
				    SupplierId = c.SupplierId,
				    Mount = c.Mount,
				    UnitPrice = c.UnitPrice,
				    ReturnLicence = c.ReturnLicence,
				    HasReturn_DamagedPart = c.HasReturn_DamagedPart,
				    DamagedPartId = c.DamagedPartId,
				    DamagedPartCode = c.DamagedPart.Code ?? "",
				    DamagedPartName = c.DamagedPart.Name ?? "",
				    DamagedPartType = c.DamagedPartType,
				    DamagedPartDescription = c.DamagedPartDescription,
				    HasFailure = c.HasFailure,
				    FailurePartId = c.FailurePartId,
				    FailurePartCode = c.FailurePart.Code,
				    FailurePartName = c.FailurePart.Name,
				    FailureDescription = c.FailureDescription,
				    Comment = c.Comment
			    })
			    .ToList();

			return Ok(items);
		}
	}

	public class ServiceRequestPartListByParentViewModel
	{
		public long ServiceRequestId { get; set; }
		public List<ServiceRequestPartListItemViewModel> Items { get; set; } = new();
	}

	public class ServiceRequestPartListItemViewModel
	{
		public long? Id { get; set; }
		public long? ServiceRequestId { get; set; }
		public string? PartCode { get; set; }
		public string? PartName { get; set; }
		public long? PartId { get; set; }
		public short? ProjectVchNum { get; set; }
		public string? SalesOfficeName { get; set; }
		public string? ProjectPartUnitTitle { get; set; }
		public short? ProjectVchYear { get; set; }
		public string? ProjectVoucherShamsiDate { get; set; }
		public string? ProjectVchPartCode { get; set; }
		public string? ProjectVchPartName { get; set; }
		public long? ProjectVchPartId { get; set; }
		public string? ReplacePartCode { get; set; }
		public string? ReplacePartName { get; set; }
		public string? CostCenterTitle { get; set; }
		public long? CostCenterId { get; set; }
		public string? SupplierName { get; set; }
		public long? SupplierId { get; set; }
		public decimal? Mount { get; set; }
		public long? UnitPrice { get; set; }
		public bool ReturnLicence { get; set; }
		public bool HasReturn_DamagedPart { get; set; }
		public string? DamagedPartId { get; set; }
		public string? DamagedPartCode { get; set; }
		public string? DamagedPartName { get; set; }
		public DamagedPartTypeEnum DamagedPartType { get; set; }
		public string? DamagedPartDescription { get; set; }
		public bool HasFailure { get; set; }
		public string? FailurePartId { get; set; }
		public string? FailurePartCode { get; set; }
		public string? FailurePartName { get; set; }
		public string? FailureDescription { get; set; }
		public string? Comment { get; set; }
	}
}
