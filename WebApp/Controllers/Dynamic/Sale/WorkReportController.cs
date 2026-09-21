using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
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
	[ControllerInfo("گزارش کار ماموریت", typeof(WorkReport))]
	public class WorkReportController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(WorkReport model, CancellationToken cn)
		{
			ClearNav(model);
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<WorkReport>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(WorkReport model, CancellationToken cn)
		{
			ClearNav(model);
			try
			{
				return Ok(await unitOfWork.Repository<WorkReport>().SaveAsync(model, cn, true));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(WorkReport model, CancellationToken cn)
		{
			ClearNav(model);
			try
			{
				return Ok(await unitOfWork.Repository<WorkReport>().UpdateAsync(model, cn, true));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<WorkReport>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<WorkReport>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? serviceRequestId = null, long? missionId = null)
		{
			ViewBag.ServiceRequestId = serviceRequestId ?? 0;
			ViewBag.ServiceRequestDetails = GetServiceRequestDetails(serviceRequestId);
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<WorkReport>().TableNoTracking.FirstOrDefault(c => c.Id == id);
				return View(@"\Views\Panel\Sale\WorkReport\Edit.cshtml", entity ?? new WorkReport { MissionId = missionId });
			}
			return View(@"\Views\Panel\Sale\WorkReport\Edit.cshtml", new WorkReport { MissionId = missionId });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? serviceRequestId = null, long? missionId = null)
		{
			ViewBag.ServiceRequestId = serviceRequestId ?? 0;
			ViewBag.ServiceRequestDetails = GetServiceRequestDetails(serviceRequestId);
			return View(@"\Views\Panel\Sale\WorkReport\Edit.cshtml", new WorkReport { MissionId = missionId });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sale\WorkReport\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long serviceRequestId)
		{
			ViewBag.ParentId = serviceRequestId;
			return View(@"\Views\Panel\Sale\WorkReport\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? serviceRequestId, CancellationToken cn)
		{
			if (serviceRequestId == null || serviceRequestId == 0)
				return BadRequest("شناسه والد خالی است");
			var items = await unitOfWork.Repository<WorkReport>().TableNoTracking
				.Where(c =>
					(c.Mission != null && c.Mission.ServiceRequestId == serviceRequestId)
					|| (c.ServiceRequestDetail != null && c.ServiceRequestDetail.ServiceRequestId == serviceRequestId))
				.Select(c => new
				{
					c.Id,
					c.ReportNumber,
					HokmNumber = c.Mission != null ? c.Mission.HokmNumber : (int?)null,
					ExpertName = c.Mission != null && c.Mission.Personel != null ? c.Mission.Personel.Name +" " + c.Mission.Personel.Family  : null,

					PartCode = c.ServiceRequestDetail != null &&
					c.ServiceRequestDetail.OtherPart != null? c.ServiceRequestDetail.OtherPart.Code :
					c.ServiceRequestDetail.OrderDetailSerial != null ?
					c.ServiceRequestDetail.OrderDetailSerial.OrderDetail.Part.Code: null,

					PartName = c.ServiceRequestDetail != null && c.ServiceRequestDetail.OtherPart != null
						? c.ServiceRequestDetail.OtherPart.Name :
					 c.ServiceRequestDetail.OrderDetailSerial != null ?
					c.ServiceRequestDetail.OrderDetailSerial.OrderDetail.Part.Name : null,

					Serial = c.ServiceRequestDetail != null && c.ServiceRequestDetail.OtherPartSerial != null
					? c.ServiceRequestDetail.OtherPartSerial :
					 c.ServiceRequestDetail.OrderDetailSerial != null ?
					c.ServiceRequestDetail.OrderDetailSerial.Serial : null,

					c.FailureTypeInText,
					c.RepairsTypeInText,
					c.ServiceStartShamsiDate,
					c.ServiceStartTime,
					c.ServiceEndShamsiDate,
					c.ServiceEndTime,
					c.TotalRunTime,
					c.ExecutiveBarriersDescription,
					MissionResult = (int)c.MissionResult,
					c.IsSurveyLinkSended
				})
				.ToListAsync(cn);
			return Ok(items);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("گزارش‌های مشابه", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetSameReports(long serviceRequestId, CancellationToken cn)
		{
			var items = await unitOfWork.Repository<WorkReport>().TableNoTracking
				.Where(c =>
					(c.Mission != null && c.Mission.ServiceRequestId == serviceRequestId)
					|| (c.ServiceRequestDetail != null && c.ServiceRequestDetail.ServiceRequestId == serviceRequestId))
				.Select(c => new
				{
					id = c.Id,
					text = "ش گزارش : " + c.ReportNumber + " - " + (c.Mission != null && c.Mission.Expert != null
						? c.Mission.Expert.Name : "")
				})
				.ToListAsync(cn);
			return Ok(items);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("بارگذاری گزارش مشابه", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetSameReportInfo(long id, CancellationToken cn)
		{
			var item = await unitOfWork.Repository<WorkReport>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (item == null)
				return BadRequest("گزارشی یافت نشد");
			item.Id = 0;
			item.HtsId = 0;
			return Ok(item);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<WorkReport>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<WorkReport>().FetchDataAsync(request, cn));

		private static void ClearNav(WorkReport model)
		{
			model.Mission = null;
			model.ServiceRequestDetail = null;
		}

		private List<WorkReportServiceRequestDetailViewModel> GetServiceRequestDetails(long? serviceRequestId)
		{
			if (serviceRequestId == null || serviceRequestId == 0)
				return new List<WorkReportServiceRequestDetailViewModel>();

			return unitOfWork.Repository<ServiceRequestDetail>().TableNoTracking
				.Where(c => c.ServiceRequestId == serviceRequestId)
				.Select(c => new WorkReportServiceRequestDetailViewModel()
				{
					Id = c.Id,
					PartCode = c.OtherPart != null ? c.OtherPart.Code
						: c.OrderDetail != null && c.OrderDetail.Part != null ? c.OrderDetail.Part.Code :
						c.OrderDetailSerial != null && c.OrderDetailSerial.OrderDetail.Part != null ?
						c.OrderDetailSerial.OrderDetail.Part.Code : null,
					PartName = c.OtherPart != null ? c.OtherPart.Name
						: c.OrderDetail != null && c.OrderDetail.Part != null ? c.OrderDetail.Part.Name :
						c.OrderDetailSerial != null && c.OrderDetailSerial.OrderDetail.Part != null ?
						c.OrderDetailSerial.OrderDetail.Part.Name : null,
					Serial = c.OrderDetailSerial != null ? c.OrderDetailSerial.Serial : c.OtherPartSerial,
					NoticeShamsiDate = c.NoticeShamsiDate
				})
				.ToList();
		}

		public class WorkReportServiceRequestDetailViewModel
		{
			public long? Id { get; set; }
			public string? PartName { get; set; }
			public string? PartCode { get; set; }
			public string? Serial { get; set; }
			public string? NoticeShamsiDate { get; set; }
		}
	}
}
