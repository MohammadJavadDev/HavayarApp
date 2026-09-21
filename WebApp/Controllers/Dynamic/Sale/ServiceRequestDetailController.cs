using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Sale;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.FileServices;
using System.IO.Compression;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sale/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("جزئیات درخواست پشتیبانی", typeof(ServiceRequestDetail))]
	public class ServiceRequestDetailController(IUnitOfWork unitOfWork, IWebHostEnvironment env, IFileService fileService) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ServiceRequestDetail model, CancellationToken cn)
		{
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<ServiceRequestDetail>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(ServiceRequestDetail model, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<ServiceRequestDetail>().SaveAsync(model, cn, true));

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ServiceRequestDetail model, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<ServiceRequestDetail>().UpdateAsync(model, cn, true));

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<ServiceRequestDetail>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<ServiceRequestDetail>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id, long? serviceRequestId = null)
		{
			ServiceRequestDetail entity;
			if (id != null && id != 0)
			{
				entity = unitOfWork.Repository<ServiceRequestDetail>().TableNoTracking.FirstOrDefault(c => c.Id == id)
					?? new ServiceRequestDetail { ServiceRequestId = serviceRequestId };
			}
			else
				entity = new ServiceRequestDetail { ServiceRequestId = serviceRequestId };
			FillCustomerId(entity.ServiceRequestId ?? serviceRequestId);
			return View(@"\Views\Panel\Sale\ServiceRequestDetail\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New(long? serviceRequestId = null)
		{
			FillCustomerId(serviceRequestId);
			return View(@"\Views\Panel\Sale\ServiceRequestDetail\Edit.cshtml", new ServiceRequestDetail { ServiceRequestId = serviceRequestId });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sale\ServiceRequestDetail\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("کارتابل مسئول منطقه", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult Cartable() => View(@"\Views\Panel\Sale\ServiceRequestDetail\Cartable.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("لیست وابسته", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult ListByParentId(long serviceRequestId)
		{
			ViewBag.ParentId = serviceRequestId;
			FillCustomerId(serviceRequestId);
			return View(@"\Views\Panel\Sale\ServiceRequestDetail\ListByParentId.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دریافت لیست وابسته", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetListByParentId(long? serviceRequestId, CancellationToken cn)
		{
			if (serviceRequestId == null || serviceRequestId == 0)
				return BadRequest("شناسه والد خالی است");
			var items = await unitOfWork.Repository<ServiceRequestDetail>().TableNoTracking
				.Where(c => c.ServiceRequestId == serviceRequestId)
				.Select(c => new
				{
					c.Id,
					RequestType = (int)c.RequestType,

					PartCode = c.OtherPart != null ? c.OtherPart.Code
						: c.OrderDetail != null && c.OrderDetail.Part != null ? c.OrderDetail.Part.Code :
						c.OrderDetailSerial != null && c.OrderDetailSerial.OrderDetail.Part != null ?
						c.OrderDetailSerial.OrderDetail.Part.Code : null,

					PartName = c.OtherPart != null ? c.OtherPart.Name
						: c.OrderDetail != null && c.OrderDetail.Part != null ? c.OrderDetail.Part.Name :
						c.OrderDetailSerial != null && c.OrderDetailSerial.OrderDetail.Part != null ?
						c.OrderDetailSerial.OrderDetail.Part.Name : null,

					Serial = c.OrderDetailSerial != null ? c.OrderDetailSerial.Serial : c.OtherPartSerial,
					ProductionOrderNumber = c.OrderDetailSerial != null ? c.OrderDetailSerial.ProductionOrderNumber : null,
					c.NoticeShamsiDate,
					c.NoticeTime
				})
				.ToListAsync(cn);
			var mapped = items.Select(c => new
			{
				c.Id,
				c.RequestType,
				RequestTypeTitle = RequestTypeTitle(c.RequestType),
				c.PartCode,
				c.PartName,
				c.Serial,
				c.ProductionOrderNumber,
				c.NoticeShamsiDate,
				c.NoticeTime
			});
			return Ok(mapped);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ServiceRequestDetail>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<ServiceRequestDetail>().FetchDataAsync(request, cn));

		[HttpGet("[action]")]
		[ActionDisplayName("بررسی پیوست گزارش کار", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> HasWorkReportAttachments(long id, CancellationToken cn)
		{
			var fileIds = await GetWorkReportAttachmentFileIds(id, cn);
			if (fileIds.Count == 0)
				return BadRequest("هیچ پیوستی یافت نشد");
			return Ok(true);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دانلود پیوست گزارش کار", ActionAccessType.View, ActionAccessItemType.Custom)]
		public async Task<IActionResult> DownloadWorkReportAttachments(long id, CancellationToken cn)
		{
			var fileIds = await GetWorkReportAttachmentFileIds(id, cn);
			if (fileIds.Count == 0)
				return BadRequest("هیچ پیوستی یافت نشد");

			var memoryStream = new MemoryStream();
			using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
			{
				var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (var fileId in fileIds)
				{
					var (stream, _, fileName) = await fileService.DownloadAsync(fileId, cn);
					await using (stream)
					{
						var entry = zip.CreateEntry(UniqueZipEntryName(fileName, usedNames));
						await using var entryStream = entry.Open();
						await stream.CopyToAsync(entryStream, cn);
					}
				}
			}
			memoryStream.Position = 0;
			return File(memoryStream, "application/zip", "WorkReportAttachments_" + id + ".zip");
		}

		private async Task<List<long>> GetWorkReportAttachmentFileIds(long detailId, CancellationToken cn)
		{
			return await unitOfWork.Repository<WorkReportAttachment>().TableNoTracking
				.Where(a => a.WorkReport != null
					&& a.WorkReport.ServiceRequestDetailId == detailId
					&& a.FileId != null
					&& a.FileId != 0)
				.Select(a => a.FileId!.Value)
				.ToListAsync(cn);
		}

		private static string UniqueZipEntryName(string? fileName, HashSet<string> usedNames)
		{
			var baseName = string.IsNullOrWhiteSpace(fileName) ? "file" : Path.GetFileName(fileName);
			if (string.IsNullOrWhiteSpace(baseName))
				baseName = "file";
			var candidate = baseName;
			var i = 1;
			while (!usedNames.Add(candidate))
			{
				var ext = Path.GetExtension(baseName);
				var stem = Path.GetFileNameWithoutExtension(baseName);
				candidate = stem + "_" + i + ext;
				i++;
			}
			return candidate;
		}

		private void FillCustomerId(long? serviceRequestId)
		{
			ViewBag.CustomerId = serviceRequestId == null || serviceRequestId == 0
				? 0
				: unitOfWork.Repository<ServiceRequest>().TableNoTracking
					.Where(c => c.Id == serviceRequestId)
					.Select(c => c.CustomerId)
					.FirstOrDefault() ?? 0;
		}

		private static string RequestTypeTitle(int value) => value switch
		{
			1 => "آموزش",
			2 => "اورهال",
			3 => "بازدید و مشاوره",
			4 => "تعمیرات گارانتی",
			5 => "تعمیرات وارانتی",
			6 => "راه اندازی اولیه",
			7 => "پایپینگ",
			8 => "سرویس دوره ای",
			9 => "سایر موارد",
			10 => "OPI",
			11 => "راه اندازی تجهیز تعمیری",
			12 => "پیش راه اندازی",
			13 => "نصب",
			_ => value.ToString()
		};
	}
}
