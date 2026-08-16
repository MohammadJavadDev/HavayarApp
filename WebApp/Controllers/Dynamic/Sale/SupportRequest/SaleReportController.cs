using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
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
	[ControllerInfo("گزارش کار", typeof(SaleReport))]
	public class SaleReportController(IUnitOfWork unitOfWork, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(SaleReport saleReport, CancellationToken cn)
		{
			if (saleReport.Id == null || saleReport.Id == 0)
			{
				return await Add(saleReport, cn);
			}
			var exist = await unitOfWork.Repository<SaleReport>().TableNoTracking.AnyAsync(c => c.Id == saleReport.Id);
			if (exist)
			{
				return await Update(saleReport, cn);
			}
			return await Add(saleReport, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(SaleReport saleReport, CancellationToken cn)
		{
			var serviceRequestId = saleReport.ServiceRequestId;
			var entity = await unitOfWork.Repository<SaleReport>().SaveAsync(saleReport, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(SaleReport saleReport, CancellationToken cn)
		{
			var serviceRequestId = saleReport.ServiceRequestId;
			var entity = await unitOfWork.Repository<SaleReport>().UpdateAsync(saleReport, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<SaleReport>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<SaleReport>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<SaleReport>().TableNoTracking
				    .FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Sale\ServiceRequest\SaleReport\Edit.cshtml", entity);
			}
			var newEntity = new SaleReport();
			return View(@"\Views\Panel\Sale\ServiceRequest\SaleReport\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? serviceRequestId)
		{
			var newEntity = new SaleReport
			{
				ServiceRequestId = serviceRequestId
			};
			return View(@"\Views\Panel\Sale\ServiceRequest\SaleReport\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Sale\ServiceRequest\SaleReport\List.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات با شناسه درخواست پشتیبانی", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult ListBy(long? serviceRequestId)
		{
			if (serviceRequestId == null || serviceRequestId == 0)
				throw new Exception("شناسه درخواست پشتیبانی نمیتواند خالی باشد.");

			var model = new SaleReportListByParentViewModel
			{
				ServiceRequestId = serviceRequestId.Value
			};

			return View(@"\Views\Panel\Sale\ServiceRequest\SaleReport\ListByParentId.cshtml", model);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست با شناسه درخواست پشتیبانی", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public IActionResult GetListByParentId(long? serviceRequestId)
		{
			if (serviceRequestId == null || serviceRequestId == 0)
				return BadRequest("شناسه درخواست پشتیبانی نمیتواند خالی باشد.");

			var items = unitOfWork
			    .Repository<SaleReport>()
			    .TableNoTracking
			    .Where(c => c.ServiceRequestId == serviceRequestId)
			    .Select(c => new SaleReportListItemViewModel
			    {
				    Id = c.Id,
				    ServiceRequestId = c.ServiceRequestId,
				    SaleMissionId = c.SaleMissionId,
				    ReportNumber = c.ReportNumber,
				    ServiceStartShamsiDate = c.ServiceStartShamsiDate,
				    ServiceStartTime = c.ServiceStartTime,
				    ServiceEndShamsiDate = c.ServiceEndShamsiDate,
				    ServiceEndTime = c.ServiceEndTime,
				    FailureTypeId = c.FailureTypeId,
				    FailureTypeTitle = c.FailureType.Title,
				    RepairsTypeId = c.RepairsTypeId,
				    RepairsTypeTitle = null,
				    ExecutiveBarriersDescription = c.ExecutiveBarriersDescription,
				    TotalRunTime = c.TotalRunTime,
				    WorkHoursUnderload = c.WorkHoursUnderload,
				    FlowIntensityUnderload = c.FlowIntensityUnderload,
				    FlowIntensityWithoutload = c.FlowIntensityWithoutload,
				    MaximumWorkingPressure = c.MaximumWorkingPressure,
				    MinimumWorkingPressure = c.MinimumWorkingPressure,
				    EnvironmentTemperature = c.EnvironmentTemperature,
				    DeviceTemperature = c.DeviceTemperature,
				    VoltagePowerGrid = c.VoltagePowerGrid,
				    AmountDryerDyupoint = c.AmountDryerDyupoint,
				    CompressorHouseStatus = c.CompressorHouseStatus,
				    ElectricalPanelStatus = c.ElectricalPanelStatus,
				    PipingStatus = c.PipingStatus,
				    MissionResult = c.MissionResult,
				    OldRunTime = c.OldRunTime,
				    ExpertGuaranteeDateShamsi = c.ExpertGuaranteeDateShamsi,
				    ExpertGuaranteeComment = c.ExpertGuaranteeComment,
				    OilLevelCheck = c.OilLevelCheck,
				    InspectionOfAllConnections = c.InspectionOfAllConnections,
				    OilSuctionInspection = c.OilSuctionInspection,
				    DustCollectorInspection = c.DustCollectorInspection,
				    InspectionOfTheseRpentinebeltTimingBelt = c.InspectionOfTheseRpentinebeltTimingBelt,
				    OilFilterReplacement = c.OilFilterReplacement,
				    SeparatFilterReplacement = c.SeparatFilterReplacement,
				    AirFilterReplacement = c.AirFilterReplacement,
				    ReplacingTheunLoaderkit = c.ReplacingTheunLoaderkit,
				    MinimumKitReplacement = c.MinimumKitReplacement,
				    Replacingthethermostatkit = c.Replacingthethermostatkit,
				    IrankitReplacement = c.IrankitReplacement,
				    ElectricMotorGreasing = c.ElectricMotorGreasing,
				    CleaningExteriorDevice = c.CleaningExteriorDevice,
				    StatusCompressor = c.StatusCompressor,
				    CoolingSystem = c.CoolingSystem,
				    CompressorRoomPowerSupplyPanelControl = c.CompressorRoomPowerSupplyPanelControl,
				    CompressorElectricalSystemElectricalPanel = c.CompressorElectricalSystemElectricalPanel,
				    StatusPeripheralEquipment = c.StatusPeripheralEquipment,
				    PipingCondition = c.PipingCondition,
				    ServiceMaintenanceManual = c.ServiceMaintenanceManual,
				    TheAboveItemsApprovedCustomer = c.TheAboveItemsApprovedCustomer,
				    TechnicalSupervisorsRemarks = c.TechnicalSupervisorsRemarks,
				    ExplanationByThereGionalOfficial = c.ExplanationByThereGionalOfficial,
				    OpinionofAfterSalesServiceSupervisor = c.OpinionofAfterSalesServiceSupervisor,
				    AfterSalesServiceManagersOpinion = c.AfterSalesServiceManagersOpinion
			    })
			    .ToList();

			return Ok(items);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<SaleReport>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<SaleReport>().FetchDataAsync(request, cn));
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
		public async Task<IActionResult> GetServiceRequest(long? serviceRequestId, CancellationToken cancellationToken)
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
	}

	public class SaleReportListByParentViewModel
	{
		public long ServiceRequestId { get; set; }
		public List<SaleReportListItemViewModel> Items { get; set; } = new();
	}

	public class SaleReportListItemViewModel
	{
		public long? Id { get; set; }
		public long? ServiceRequestId { get; set; }
		public long? SaleMissionId { get; set; }
		public int? ReportNumber { get; set; }
		public string? ServiceStartShamsiDate { get; set; }
		public string? ServiceStartTime { get; set; }
		public string? ServiceEndShamsiDate { get; set; }
		public string? ServiceEndTime { get; set; }
		public long? FailureTypeId { get; set; }
		public string? FailureTypeTitle { get; set; }
		public string? RepairsTypeId { get; set; }
		public string? RepairsTypeTitle { get; set; }
		public string? ExecutiveBarriersDescription { get; set; }
		public long? TotalRunTime { get; set; }
		public long? WorkHoursUnderload { get; set; }
		public decimal? FlowIntensityUnderload { get; set; }
		public decimal? FlowIntensityWithoutload { get; set; }
		public decimal? MaximumWorkingPressure { get; set; }
		public decimal? MinimumWorkingPressure { get; set; }
		public decimal? EnvironmentTemperature { get; set; }
		public decimal? DeviceTemperature { get; set; }
		public int? VoltagePowerGrid { get; set; }
		public decimal? AmountDryerDyupoint { get; set; }
		public bool CompressorHouseStatus { get; set; }
		public bool ElectricalPanelStatus { get; set; }
		public bool PipingStatus { get; set; }
		public MissionResultEnum MissionResult { get; set; }
		public int? OldRunTime { get; set; }
		public string? ExpertGuaranteeDateShamsi { get; set; }
		public string? ExpertGuaranteeComment { get; set; }
		public bool OilLevelCheck { get; set; }
		public bool InspectionOfAllConnections { get; set; }
		public bool OilSuctionInspection { get; set; }
		public bool DustCollectorInspection { get; set; }
		public bool InspectionOfTheseRpentinebeltTimingBelt { get; set; }
		public bool OilFilterReplacement { get; set; }
		public bool SeparatFilterReplacement { get; set; }
		public bool AirFilterReplacement { get; set; }
		public bool ReplacingTheunLoaderkit { get; set; }
		public bool MinimumKitReplacement { get; set; }
		public bool Replacingthethermostatkit { get; set; }
		public bool IrankitReplacement { get; set; }
		public bool ElectricMotorGreasing { get; set; }
		public bool CleaningExteriorDevice { get; set; }
		public bool StatusCompressor { get; set; }
		public bool CoolingSystem { get; set; }
		public bool CompressorRoomPowerSupplyPanelControl { get; set; }
		public bool CompressorElectricalSystemElectricalPanel { get; set; }
		public bool StatusPeripheralEquipment { get; set; }
		public bool PipingCondition { get; set; }
		public bool ServiceMaintenanceManual { get; set; }
		public bool TheAboveItemsApprovedCustomer { get; set; }
		public string? TechnicalSupervisorsRemarks { get; set; }
		public string? ExplanationByThereGionalOfficial { get; set; }
		public string? OpinionofAfterSalesServiceSupervisor { get; set; }
		public string? AfterSalesServiceManagersOpinion { get; set; }
	}
}
