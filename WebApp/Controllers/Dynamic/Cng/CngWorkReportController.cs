using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Cng;
using Entities.App.Cng.Enums;
using Entities.App.Inv;
using Entities.App.Sale;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.FileServices;
using System.Net.Mime;
using WebFramework.Filtters;
using WebFramework.Page;
using CngWorkReport = Entities.App.Cng.WorkReport;
using CngWorkReportConsumedPart = Entities.App.Cng.WorkReportConsumedPart;
using CngWorkReportFailure = Entities.App.Cng.WorkReportFailure;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Cng/WorkReport")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("گزارش کار CNG", typeof(CngWorkReport))]
	public class CngWorkReportController(
		IUnitOfWork unitOfWork,
		IWebHostEnvironment env,
		IFileService fileService) : BaseController
	{
		private const string RoleAgency = "Cng.WorkReport.Agency";
		private const string RoleExpert = "Cng.WorkReport.Expert";
		private const string RoleShowAll = "Cng.WorkReport.ShowAll";
		private const string RoleShowAllMenus = "ShowAllMenus";

		private bool CanShowAllRows =>
			IsAdministrator || CurrentUserHasAnyRole(RoleExpert, RoleShowAll, RoleShowAllMenus);

		private bool CanDelete =>
			IsAdministrator || CurrentUserHasAnyRole(RoleExpert, RoleShowAllMenus);

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(CngWorkReport model, CancellationToken cn)
		{
			ClearNav(model);
			var err = ValidateModel(model);
			if (err != null) return BadRequest(err);
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<CngWorkReport>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(CngWorkReport model, CancellationToken cn)
		{
			ClearNav(model);
			var err = ValidateModel(model);
			if (err != null) return BadRequest(err);
			ApplyFileMetaFromFile(model);
			var consumed = model.ConsumedParts ?? [];
			var failures = BuildFailureRows(model);
			model.ConsumedParts = null;
			model.Failures = null;
			var saved = await unitOfWork.Repository<CngWorkReport>().SaveAsync(model, cn, true);
			await ReplaceChildrenAsync(saved.Id!.Value, consumed, failures, cn);
			await UpdateStationRepresentativeAsync(saved.StationInfoId, model, cn);
			return Ok(await LoadForEditAsync(saved.Id.Value, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(CngWorkReport model, CancellationToken cn)
		{
			ClearNav(model);
			var err = ValidateModel(model);
			if (err != null) return BadRequest(err);
			var old = await unitOfWork.Repository<CngWorkReport>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (old == null) return BadRequest("رکورد یافت نشد");
			if (!CanShowAllRows && old.CreatedById != CurrentUserId)
				return BadRequest("دسترسی به این رکورد وجود ندارد");
			ApplyFileMetaFromFile(model);
			var consumed = model.ConsumedParts ?? [];
			var failures = BuildFailureRows(model);
			model.ConsumedParts = null;
			model.Failures = null;
			var saved = await unitOfWork.Repository<CngWorkReport>().UpdateAsync(model, cn, true);
			await ReplaceChildrenAsync(saved.Id!.Value, consumed, failures, cn);
			await UpdateStationRepresentativeAsync(saved.StationInfoId, model, cn);
			return Ok(await LoadForEditAsync(saved.Id.Value, cn));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			if (!CanDelete) return BadRequest("دسترسی حذف وجود ندارد");
			var entity = await unitOfWork.Repository<CngWorkReport>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return Ok();
			if (!CanShowAllRows && entity.CreatedById != CurrentUserId)
				return BadRequest("دسترسی به این رکورد وجود ندارد");
			var failures = await unitOfWork.Repository<CngWorkReportFailure>().TableNoTracking.Where(c => c.WorkReportId == id).ToListAsync(cn);
			foreach (var f in failures)
				await unitOfWork.Repository<CngWorkReportFailure>().DeleteAsync(f, cn, true);
			var parts = await unitOfWork.Repository<CngWorkReportConsumedPart>().TableNoTracking.Where(c => c.WorkReportId == id).ToListAsync(cn);
			foreach (var p in parts)
				await unitOfWork.Repository<CngWorkReportConsumedPart>().DeleteAsync(p, cn, true);
			await unitOfWork.Repository<CngWorkReport>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			CngWorkReport entity;
			if (id != null && id != 0)
			{
				entity = (await LoadForEditAsync(id.Value, cn)) ?? CreateNew();
				if (!CanShowAllRows && entity.CreatedById != CurrentUserId && entity.Id is > 0)
					return BadRequest("دسترسی به این رکورد وجود ندارد");
			}
			else entity = CreateNew();
			FillViewBag(entity);
			return View(@"\Views\Panel\Cng\WorkReport\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var entity = CreateNew();
			FillViewBag(entity);
			return View(@"\Views\Panel\Cng\WorkReport\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Cng\WorkReport\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<CngWorkReport>().FetchDataAsync(request, cn));

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + @"\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<CngWorkReport>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دانلود فایل", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Download(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<CngWorkReport>().TableNoTracking.Include(c => c.File).FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return BadRequest("رکورد یافت نشد");

			// پس از جاب انتقال فایل، دانلود فقط از FileId / IFileService سرو می‌شود.
			if (entity.FileId is > 0)
			{
				try
				{
					var (stream, contentType, fileName) = await fileService.DownloadAsync(entity.FileId.Value, cn);
					var downloadName = !string.IsNullOrWhiteSpace(fileName)
						? fileName
						: ((entity.ReportNumber ?? "WorkReport") + (entity.WorkReportFileType ?? ""));
					return File(stream, string.IsNullOrWhiteSpace(contentType) ? MediaTypeNames.Application.Octet : contentType, downloadName);
				}
				catch (FileNotFoundException)
				{
					return BadRequest("فایل گزارش کار یافت نشد");
				}
			}

			// فقط وقتی FileId هنوز null است (قبل از جاب انتقال) از UNC قدیمی استفاده می‌شود.
			if (!string.IsNullOrWhiteSpace(entity.WorkReportFileAddress) && System.IO.File.Exists(entity.WorkReportFileAddress))
			{
				var name = (entity.ReportNumber ?? "WorkReport") + (entity.WorkReportFileType ?? Path.GetExtension(entity.WorkReportFileAddress));
				return PhysicalFile(entity.WorkReportFileAddress, MediaTypeNames.Application.Octet, name);
			}
			return BadRequest("فایل گزارش کار یافت نشد");
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetSaleOrders(long? customerId, CancellationToken cn)
		{
			if (customerId is null or 0) return Ok(Array.Empty<object>());
			var rows = await unitOfWork.Repository<Order>().TableNoTracking
				.Where(o => o.CustomerId == customerId && o.IsActive == IsActiveEnum.Active)
				.OrderByDescending(o => o.Id)
				.Select(o => new { id = o.Id, htsVchNo = o.HtsVchNo, orderNumber = o.OrderNumber, vchDate = o.VchDateShamsiDate })
				.Take(500).ToListAsync(cn);
			return Ok(rows.Select(o => new { o.id, text = ((o.htsVchNo ?? o.orderNumber)?.ToString() ?? "") + " | " + (o.vchDate ?? "") }));
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetPartsByOrder(long? orderId, CancellationToken cn)
		{
			if (orderId is null or 0) return Ok(Array.Empty<object>());
			var rows = await unitOfWork.Repository<OrderDetail>().TableNoTracking.Include(d => d.Part)
				.Where(d => d.SaleOrderId == orderId && d.PartId != null && d.PartId > 0)
				.Select(d => new { id = d.PartId, text = ((d.Part != null ? d.Part.Code : null) ?? "") + " | " + ((d.Part != null ? d.Part.Name : null) ?? ""), partId = d.PartId })
				.Take(500).ToListAsync(cn);
			return Ok(rows.GroupBy(x => x.id).Select(g => g.First()));
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetFailuresByGroup(int? groupId, CancellationToken cn)
		{
			if (groupId is null or 0) return Ok(Array.Empty<object>());
			var group = (FailureInfoGroupEnum)groupId.Value;
			var rows = await unitOfWork.Repository<FailureInfo>().TableNoTracking
				.Where(f => f.Group == group && f.IsActive == IsActiveEnum.Active)
				.OrderBy(f => f.Title)
				.Select(f => new { id = f.Id, text = f.Title, initialValue = f.InitialValue })
				.ToListAsync(cn);
			return Ok(rows);
		}

		[HttpGet("[action]")]
		public IActionResult ConsumedPartForm(long? saleOrderId, long? partId, decimal? mount, string? hotnessCondition, string? comment, long? id, long? htsId, long? customerId)
		{
			var item = new CngWorkReportConsumedPart { Id = id, HtsId = htsId ?? 0, SaleOrderId = saleOrderId, PartId = partId ?? 0, Mount = mount ?? 0, HotnessCondition = hotnessCondition, Comment = comment };
			if (partId is > 0) item.Part = unitOfWork.Repository<Part>().TableNoTracking.FirstOrDefault(c => c.Id == partId);
			if (saleOrderId is > 0) item.SaleOrder = unitOfWork.Repository<Order>().TableNoTracking.FirstOrDefault(c => c.Id == saleOrderId);
			ViewBag.CustomerId = customerId;
			return View(@"\Views\Panel\Cng\WorkReport\_ConsumedPartForm.cshtml", item);
		}

		[HttpGet("[action]")]
		public IActionResult FailureForm(long? failureInfoId, int? groupId, decimal? value, decimal? setPointValue, string? comment, long? id, long? htsId)
		{
			var item = new CngWorkReportFailure { Id = id, HtsId = htsId ?? 0, FailureInfoId = failureInfoId ?? 0, Value = value, SetPointValue = setPointValue, Comment = comment };
			if (failureInfoId is > 0) item.FailureInfo = unitOfWork.Repository<FailureInfo>().TableNoTracking.FirstOrDefault(c => c.Id == failureInfoId);
			ViewBag.GroupId = groupId ?? (item.FailureInfo?.Group != null ? (int)item.FailureInfo.Group : (int?)null);
			return View(@"\Views\Panel\Cng\WorkReport\_FailureForm.cshtml", item);
		}

		private void FillViewBag(CngWorkReport entity)
		{
			ViewBag.CanShowAll = CanShowAllRows;
			ViewBag.CanDelete = CanDelete;
			ViewBag.HasFile = entity.FileId is > 0 || !string.IsNullOrWhiteSpace(entity.WorkReportFileAddress);
			ViewBag.ErrorFailures = unitOfWork.Repository<FailureInfo>().TableNoTracking
				.Where(f => f.Group == null && f.IsActive == IsActiveEnum.Active).OrderBy(f => f.Title)
				.Select(f => new { f.Id, f.Title }).ToList();
		}

		private static CngWorkReport CreateNew() => new()
		{
			ReportMiladiDate = DateTime.Today, IsApprovedByOwner = false,
			ConsumedParts = [], Failures = []
		};

		private static void ClearNav(CngWorkReport model)
		{
			model.File = null; model.Station = null;
			if (model.ConsumedParts != null) foreach (var item in model.ConsumedParts) { item.Part = null; item.SaleOrder = null; item.WorkReport = null; }
			if (model.Failures != null) foreach (var item in model.Failures) { item.FailureInfo = null; item.WorkReport = null; }
		}

		private static string? ValidateModel(CngWorkReport model)
		{
			if (model.StationInfoId <= 0) return "جایگاه اجباری است";
			if ((int)model.RequestTypeId == 0) return "نوع درخواست اجباری است";
			if (string.IsNullOrWhiteSpace(model.ReportShamsiDate) && model.ReportMiladiDate == default) return "تاریخ گزارش اجباری است";
			if (string.IsNullOrWhiteSpace(model.RepresentativeName)) return "نام نماینده اجباری است";
			if (string.IsNullOrWhiteSpace(model.RepresentativeMobileNumber)) return "موبایل نماینده اجباری است";
			return null;
		}

		private static List<CngWorkReportFailure> BuildFailureRows(CngWorkReport model)
		{
			return model.Failures?.Where(f => f.FailureInfoId > 0).ToList() ?? [];
		}

		private async Task ReplaceChildrenAsync(long workReportId, List<CngWorkReportConsumedPart> consumed, List<CngWorkReportFailure> failures, CancellationToken cn)
		{
			var oldFailures = await unitOfWork.Repository<CngWorkReportFailure>().Table.Where(c => c.WorkReportId == workReportId).ToListAsync(cn);
			foreach (var old in oldFailures) await unitOfWork.Repository<CngWorkReportFailure>().DeleteAsync(old, cn, true);
			var oldParts = await unitOfWork.Repository<CngWorkReportConsumedPart>().Table.Where(c => c.WorkReportId == workReportId).ToListAsync(cn);
			foreach (var old in oldParts) await unitOfWork.Repository<CngWorkReportConsumedPart>().DeleteAsync(old, cn, true);
			foreach (var item in consumed.Where(c => c.PartId > 0))
			{
				item.Id = null; item.WorkReportId = workReportId; item.Part = null; item.SaleOrder = null; item.WorkReport = null;
				await unitOfWork.Repository<CngWorkReportConsumedPart>().SaveAsync(item, cn, true);
			}
			foreach (var item in failures.Where(f => f.FailureInfoId > 0))
			{
				item.Id = null; item.WorkReportId = workReportId; item.FailureInfo = null; item.WorkReport = null;
				await unitOfWork.Repository<CngWorkReportFailure>().SaveAsync(item, cn, true);
			}
		}

		private async Task UpdateStationRepresentativeAsync(long stationInfoId, CngWorkReport model, CancellationToken cn)
		{
			var station = await unitOfWork.Repository<StationInfo>().Table.FirstOrDefaultAsync(s => s.Id == stationInfoId, cn);
			if (station == null) return;
			station.RepresentativeName = Truncate(model.RepresentativeName, 128);
			station.RepresentativeMobileNumber = Truncate(model.RepresentativeMobileNumber, 50);
			station.RepresentativeEmailAddress = Truncate(model.RepresentativeEmailAddress, 128);
			await unitOfWork.Repository<StationInfo>().UpdateAsync(station, cn, true);
		}

		private async Task<CngWorkReport?> LoadForEditAsync(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<CngWorkReport>().TableNoTracking
				.Include(c => c.File)
				.Include(c => c.Station!).ThenInclude(s => s.Customer!).ThenInclude(c => c.Party)
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return null;
			if (entity.Station != null)
			{
				entity.RepresentativeName = entity.Station.RepresentativeName;
				entity.RepresentativeMobileNumber = entity.Station.RepresentativeMobileNumber;
				entity.RepresentativeEmailAddress = entity.Station.RepresentativeEmailAddress;
			}
			entity.ConsumedParts = await unitOfWork.Repository<CngWorkReportConsumedPart>().TableNoTracking
				.Include(c => c.Part).Include(c => c.SaleOrder).Where(c => c.WorkReportId == id).OrderBy(c => c.Id).ToListAsync(cn);
			var allFailures = await unitOfWork.Repository<CngWorkReportFailure>().TableNoTracking
				.Include(c => c.FailureInfo).Where(c => c.WorkReportId == id).OrderBy(c => c.Id).ToListAsync(cn);
			entity.Failures = allFailures.Where(f => f.FailureInfo?.Group != null).ToList();
			ViewBag.SelectedErrorFailureIds = allFailures.Where(f => f.FailureInfo?.Group == null).Select(f => f.FailureInfoId).Distinct().ToList();
			return entity;
		}

		private void ApplyFileMetaFromFile(CngWorkReport model)
		{
			if (model.FileId is null or 0) return;
			var file = unitOfWork.Repository<FileEntity>().TableNoTracking.FirstOrDefault(c => c.Id == model.FileId);
			if (file == null) return;
			model.WorkReportFileAddress = Truncate(file.PhysicalPath, 256);
			model.WorkReportFileType = Truncate(Path.GetExtension(file.OriginalName ?? file.PhysicalPath ?? ""), 10);
		}

		private static string? Truncate(string? value, int max)
			=> string.IsNullOrEmpty(value) ? value : (value.Length <= max ? value : value[..max]);
	}
}
