using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Hcm;
using Entities.App.Sale;
using Entities.App.Sale.DTO;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Services.WebApp.Controllers.Dynamic;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sale/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("مدیریت ماموریت ها", typeof(SaleMission))]
	public class SaleMissionController : BaseController
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IWebHostEnvironment _webHostEnvironment;
		private readonly SaleMissionService _missionService;

		public SaleMissionController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
		{
			_unitOfWork = unitOfWork;
			_webHostEnvironment = webHostEnvironment;
			_missionService = new SaleMissionService(unitOfWork);
		}

		#region Mission CRUD

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(SaleMission saleMission, CancellationToken cn)
		{
			if (saleMission.Id == null || saleMission.Id == 0)
			{
				return await Add(saleMission, cn);
			}

			var exist = await _unitOfWork.Repository<SaleMission>()
			    .TableNoTracking
			    .AnyAsync(c => c.Id == saleMission.Id, cn);

			return exist ? await Update(saleMission, cn) : await Add(saleMission, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(SaleMission saleMission, CancellationToken cn)
		{
			_missionService.CalculateTotalCost(saleMission);
			var entity = await _unitOfWork.Repository<SaleMission>().SaveAsync(saleMission, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(SaleMission saleMission, CancellationToken cn)
		{
			_missionService.CalculateTotalCost(saleMission);
			var entity = await _unitOfWork.Repository<SaleMission>().UpdateAsync(saleMission, cn, true);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = await _unitOfWork.Repository<SaleMission>()
			    .TableNoTracking
			    .FirstOrDefaultAsync(c => c.Id == id, cn);

			if (model != null)
				await _unitOfWork.Repository<SaleMission>().DeleteAsync(model, cn, true);

			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? serviceRequestId, CancellationToken cancellationToken)
		{
			if (id != null && id != 0)
			{
				var entity = _unitOfWork.Repository<SaleMission>()
				    .TableNoTracking
				    .FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Sale\ServiceRequest\SaleMission\Edit.cshtml", entity);
			}

			return View(@"\Views\Panel\Sale\ServiceRequest\SaleMission\Edit.cshtml", new SaleMission());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? serviceRequestId)
		{
			var personList = _unitOfWork.Repository<SaleMission>()
			    .TableNoTracking
			    .FirstOrDefault(it => it.ServiceRequestId == serviceRequestId);

			var newEntity = new SaleMission
			{
				ServiceRequestId = serviceRequestId,
			};

			ViewBag.PersonList = personList;
			return View(@"\Views\Panel\Sale\ServiceRequest\SaleMission\Edit.cshtml", newEntity);
		}

		#endregion

		#region List & Data Views

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Sale\ServiceRequest\SaleMission\List.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات با شناسه درخواست پشتیبانی", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult ListBy(long? serviceRequestId)
		{
			if (serviceRequestId == null || serviceRequestId == 0)
				throw new Exception("شناسه درخواست پشتیبانی نمیتواند خالی باشد.");

			var model = new SaleMissionListByParentViewModel
			{
				ServiceRequestId = serviceRequestId.Value
			};

			return View(@"\Views\Panel\Sale\ServiceRequest\SaleMission\ListByParentId.cshtml", model);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست با شناسه درخواست پشتیبانی", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? serviceRequestId)
		{
			if (serviceRequestId == null || serviceRequestId == 0)
				return BadRequest("شناسه درخواست پشتیبانی نمیتواند خالی باشد.");

			var items = await _unitOfWork.Repository<SaleMission>()
			    .TableNoTracking
			    .Where(c => c.ServiceRequestId == serviceRequestId)
			    .GroupBy(c => new { c.PersonelId, c.Personel.Name, c.Personel.Family })
			    .Select(it => new SaleMissionListItemViewModel
			    {
				    Id = it.First().Id,
				    ServiceRequestId = it.First().ServiceRequestId,
				    StatementNumber = it.First().StatementNumber,
				    PersonelId = it.Key.PersonelId,
				    PersonelName = ((it.Key.Name ?? "") + " " + (it.Key.Family ?? "")).Trim(),
				    CostCenterId = it.First().CostCenterId,
				    CostCenterTitle = it.First().CostCenter.Title,
				    HaveGurantee = it.First().HaveGurantee,
				    DispatchShamsiDate = it.First().DispatchShamsiDate,
				    FromHour = it.First().FromHour,
				    ReturnShamsiDate = it.First().ReturnShamsiDate,
				    ToHour = it.First().ToHour,
				    WorkOvertimeFee = it.First().WorkOvertimeFee,
				    WorkOvertimeHour = it.First().WorkOvertimeHour,
				    Mission_Right_Factor = it.First().MissionRightFactor,
				    MissionRight = it.First().MissionRight,
				    TransportationPersonal = it.First().TransportationPersonal,
				    TransportationDriving = it.First().TransportationDriving,
				    MissionHolidayRight = it.First().MissionHolidayRight,
				    MissionHolidayRightFactor = it.First().MissionHolidayRightFactor,
				    DailySalary = it.First().DailySalary,
				    BreakfastPrice = it.First().BreakfastPrice,
				    BreakfastQuantity = it.First().BreakfastQuantity,
				    LunchPrice = it.First().LunchPrice,
				    LunchQuantity = it.First().LunchQuantity,
				    DinnerPrice = it.First().DinnerPrice,
				    DinnerQuantity = it.First().DinnerQuantity,
				    NightShiftAllowance = it.First().NightShiftAllowance,
				    NightShiftCoefficient = it.First().NightShiftCoefficient,
				    OtherCost = it.First().OtherCost,
				    TotalCost = it.First().TotalCost,
				    Comment = it.First().Comment
			    })
			    .ToListAsync();

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
				await _unitOfWork.Repository<SaleMission>()
				    .ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream,
				    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				    $"exportExcel.xlsx");
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
			return Ok(await _unitOfWork.Repository<SaleMission>().FetchDataAsync(request, cn));
		}

		#endregion

		#region Service Request Details

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
				var entity = await _unitOfWork.Repository<ServiceRequestDetail>()
				    .SaveAsync(serviceRequestDetail, cn, true);
				return Ok(new { isSuccess = true, message = "با موفقیت درج شد", data = entity });
			}
			else
			{
				var entity = await _unitOfWork.Repository<ServiceRequestDetail>()
				    .UpdateAsync(serviceRequestDetail, cn, true);
				return Ok(new { isSuccess = true, message = "با موفقیت ویرایش شد", data = entity });
			}
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت جزئیات درخواست پشتیبانی", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetServiceRequest(long serviceRequestId, CancellationToken cancellationToken)
		{
			if (serviceRequestId <= 0)
			{
				return BadRequest(new
				{
					success = false,
					message = "شناسه درخواست پشتیبانی معتبر نیست."
				});
			}

			var list = await _unitOfWork
			    .Repository<ServiceRequestDetail>()
			    .TableNoTracking
			    .Where(x => x.ServiceRequestId == serviceRequestId)
			    .ToListAsync(cancellationToken);

			return Ok(new
			{
				success = true,
				data = list
			});
		}

		[HttpDelete("[action]")]
		public async Task<IActionResult> DeleteServiceRequest([FromQuery] long? id, CancellationToken cancellationToken)
		{
			var model = await _unitOfWork.Repository<ServiceRequestDetail>()
			    .Table
			    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

			if (model == null)
				return NotFound(new { success = false, message = "رکورد یافت نشد" });

			await _unitOfWork.Repository<ServiceRequestDetail>()
			    .DeleteAsync(model, cancellationToken, true);

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
			var entity = await _unitOfWork.Repository<ServiceRequestDetail>()
			    .TableNoTracking
			    .FirstAsync(it => it.Id == id && it.ServiceRequestId == serviceRequestId, cancellationToken);

			if (entity == null)
			{
				return NotFound("رکورد مورد نظر یافت نشد");
			}

			ViewBag.serviceRequestId = entity.ServiceRequestId;

			return PartialView(@"\Views\Panel\Sale\ServiceRequest\ProductDetail\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		public IActionResult LoadServiceRequestForm()
		{
			return PartialView(@"\Views\Panel\Sale\ServiceRequest\MissionStatement\ServiceRequestPaperReportHtml.cshtml");
		}


		[HttpGet("[action]")]
		public async Task<IActionResult> GetMissionPersonelsWithServiceRequest(long serviceRequestId, CancellationToken cancellationToken)
		{
			if (serviceRequestId <= 0)
			{
				return BadRequest(new
				{
					success = false,
					message = "شناسه درخواست معتبر نیست."
				});
			}

			var rawExpertNameIds = await _unitOfWork.Repository<ServiceRequest>()
			    .TableNoTracking
			    .Where(sr => sr.Id == serviceRequestId)
			    .Select(sr => sr.ExpertNameId)
			    .FirstOrDefaultAsync(cancellationToken);

			if (string.IsNullOrWhiteSpace(rawExpertNameIds))
			{
				return Ok(new
				{
					success = true,
					data = new List<PersonelDto>()
				});
			}

			var ids = rawExpertNameIds
			    .Split(',', StringSplitOptions.RemoveEmptyEntries)
			    .Select(x => long.TryParse(x.Trim(), out var id) ? id : (long?)null)
			    .Where(x => x.HasValue)
			    .Select(x => x.Value)
			    .Distinct()
			    .ToList();

			if (ids.Count == 0)
			{
				return Ok(new
				{
					success = true,
					data = new List<PersonelDto>()
				});
			}
			var persons = await _unitOfWork.Repository<Personel>()
			    .TableNoTracking
			    .Where(p => p.Id.HasValue && ids.Contains(p.Id.Value))
			    .Select(p => new PersonelDto(
				   p.Id,
				   p.Name,
				   p.Family))
			    .ToListAsync(cancellationToken);

			return Ok(new
			{
				success = true,
				data = persons
			});
		}

		#endregion

		#region Mission Price Calculation

		[HttpGet("[action]")]
		public async Task<IActionResult> GetSaleMissionPrice(long? id, string fromDate, string toDate, string fromHour, string toHour)
		{
			if (id == null)
				return BadRequest(new { success = false, message = "شناسه پرسنل وارد نشده است" });

			if (string.IsNullOrEmpty(fromDate) || string.IsNullOrEmpty(toDate))
				return BadRequest(new { success = false, message = "تاریخ اعزام و برگشت باید وارد شوند" });

			var fromDateInEurope = fromDate.ToMiladiDateTimeWithSplitData();
			var toDateInEurope = toDate.ToMiladiDateTimeWithSplitData();

			if (!string.IsNullOrEmpty(fromHour))
			{
				var timeParts = fromHour.Split(':');
				if (timeParts.Length == 2)
				{
					var hour = int.Parse(timeParts[0]);
					var minute = int.Parse(timeParts[1]);
					fromDateInEurope = fromDateInEurope.AddHours(hour).AddMinutes(minute);
				}
			}

			if (!string.IsNullOrEmpty(toHour))
			{
				var timeParts = toHour.Split(':');
				if (timeParts.Length == 2)
				{
					var hour = int.Parse(timeParts[0]);
					var minute = int.Parse(timeParts[1]);
					toDateInEurope = toDateInEurope.AddHours(hour).AddMinutes(minute);
				}
			}

			var missionSalary = await _unitOfWork.Repository<MissionSalaryPersonnel>()
			    .TableNoTracking
			    .Where(it => it.PersonelId == id)
			    .FirstOrDefaultAsync();

			if (missionSalary == null)
				return BadRequest(new { success = false, message = "اطلاعات حق ماموریت برای این پرسنل یافت نشد" });

			var subtractResult = toDateInEurope.Subtract(fromDateInEurope);
			var missionDays = (int)Math.Ceiling(subtractResult.TotalDays);
			var durationInMinutes = (int)DateTimeHelper.DurationInMinutes(fromDateInEurope, toDateInEurope);
			var (breakfastCount, lunchCount, dinnerCount, wentExtraWorkHours, finalMissionDays, nightShiftCoefficient, workOvertimeHour, missionRightFactor) =
			    _missionService.CalculateMissionMeals(fromDateInEurope, toDateInEurope, missionDays);

			var result = new MissionSalaryPersonnelDTO
			{
				PersonelId = id,
				BreakfastFee = missionSalary.BreakfastFee,
				LunchFee = missionSalary.LunchFee,
				DinnerFee = missionSalary.DinnerFee,
				NightRight = missionSalary.NightRight,
				MissionRight = missionSalary.MissionRight,
				WorkOvertimeFee = missionSalary.WorkOvertimeFee,
				MissionHolidayRight = missionSalary.MissionHolidayRight,
				DailySalary = missionSalary.DailySalary,
				BreakfastQuantity = breakfastCount,
				LunchQuantity = lunchCount,
				DinnerQuantity = dinnerCount,
				MissionDays = finalMissionDays,
				DurationInMinutes = durationInMinutes,
				WentExtraWorkHours = wentExtraWorkHours,
				MissionRightFactor = missionRightFactor,
				NightShiftCoefficient = nightShiftCoefficient,
				WorkOvertimeHour = workOvertimeHour
			};

			return Ok(new { success = true, result });
		}



		[HttpGet("[action]")]
		public async Task<IActionResult> GetSaleReportById(long id, CancellationToken cancellationToken)
		{
			try
			{
				var report = await _unitOfWork.Repository<SaleReport>()
				    .TableNoTracking
				    .Where(r => r.Id == id)
				    .FirstOrDefaultAsync(cancellationToken);

				if (report == null)
				{
					return Ok(new
					{
						isSuccess = false,
						data = (object)null,
						message = "گزارش مورد نظر یافت نشد"
					});
				}

				return Ok(new
				{
					isSuccess = true,
					data = report,
					message = "عملیات با موفقیت انجام شد"
				});
			}
			catch (Exception ex)
			{
				return Ok(new
				{
					isSuccess = false,
					data = (object)null,
					message = "خطا در دریافت اطلاعات: " + ex.Message
				});
			}
		}
		#endregion

		#region ViewModels

		public class SaleMissionListByParentViewModel
		{
			public long ServiceRequestId { get; set; }
			public List<SaleMissionListItemViewModel> Items { get; set; } = new();
		}

		public class SaleMissionListItemViewModel
		{
			public long? Id { get; set; }
			public long? ServiceRequestId { get; set; }
			public int StatementNumber { get; set; }
			public long? PersonelId { get; set; }
			public string? PersonelName { get; set; }
			public long? CostCenterId { get; set; }
			public string? CostCenterTitle { get; set; }
			public bool HaveGurantee { get; set; }
			public string? DispatchShamsiDate { get; set; }
			public string? FromHour { get; set; }
			public string? ReturnShamsiDate { get; set; }
			public string? ToHour { get; set; }
			public long? WorkOvertimeFee { get; set; }
			public string? WorkOvertimeHour { get; set; }
			public long? Mission_Right_Factor { get; set; }
			public long? MissionRight { get; set; }
			public long? TransportationPersonal { get; set; }
			public long? TransportationDriving { get; set; }
			public long? MissionHolidayRight { get; set; }
			public long? MissionHolidayRightFactor { get; set; }
			public long? DailySalary { get; set; }
			public decimal? BreakfastPrice { get; set; }
			public int? BreakfastQuantity { get; set; }
			public decimal? LunchPrice { get; set; }
			public int? LunchQuantity { get; set; }
			public decimal? DinnerPrice { get; set; }
			public int? DinnerQuantity { get; set; }
			public decimal? NightShiftAllowance { get; set; }
			public decimal? NightShiftCoefficient { get; set; }
			public long? OtherCost { get; set; }
			public long? TotalCost { get; set; }
			public string? Comment { get; set; }
		}

		#endregion
	}
}
public record PersonelDto(long? Id, string Name, string Family);