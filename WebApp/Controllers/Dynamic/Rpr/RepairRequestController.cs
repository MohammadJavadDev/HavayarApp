using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Rpr;
using Entities.App.Rpr.Enums;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Actions.Rpr;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Rpr/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("درخواست تعمیر", typeof(RepairRequest))]
	public class RepairRequestController(IUnitOfWork unitOfWork, IWebHostEnvironment env, RepairRequestAction repairRequestAction) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(RepairRequest model, CancellationToken cn)
		{
			ClearNavigations(model);
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<RepairRequest>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(RepairRequest model, CancellationToken cn)
		{
			ClearNavigations(model);
			if (!repairRequestAction.HasAfterSaleAccess() && !IsAdministrator)
				return BadRequest("برای ثبت درخواست تعمیر دسترسی خدمات پس از فروش لازم است");
			return Ok(await unitOfWork.Repository<RepairRequest>().SaveAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(RepairRequest model, CancellationToken cn)
		{
			ClearNavigations(model);
			if (!repairRequestAction.HasAfterSaleAccess() && !repairRequestAction.HasRepairsAccess() && !IsAdministrator)
				return BadRequest("دسترسی ویرایش درخواست تعمیر را ندارید");
			return Ok(await unitOfWork.Repository<RepairRequest>().UpdateAsync(model, cn, true));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<RepairRequest>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<RepairRequest>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			var entity = (id != null && id != 0)
				? unitOfWork.Repository<RepairRequest>().TableNoTracking.FirstOrDefault(c => c.Id == id)
				: new RepairRequest();
			FillAccessBags();
			return View(@"\Views\Panel\Rpr\RepairRequest\Edit.cshtml", entity ?? new RepairRequest());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			FillAccessBags();
			return View(@"\Views\Panel\Rpr\RepairRequest\Edit.cshtml", new RepairRequest());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Rpr\RepairRequest\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<RepairRequest>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<RepairRequest>().FetchDataAsync(request, cn));

		[HttpGet("[action]")]
		[ActionDisplayName("دسترسی خدمات پس از فروش", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult AfterSaleAccess()
			=> Ok(new { hasAccess = repairRequestAction.HasAfterSaleAccess() || IsAdministrator });

		[HttpGet("[action]")]
		[ActionDisplayName("دسترسی تعمیرات", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult RepairsAccess()
			=> Ok(new { hasAccess = repairRequestAction.HasRepairsAccess() || IsAdministrator });

		[HttpGet("[action]")]
		[ActionDisplayName("تصویربرداری تولید محتوا", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult ContentProductionImaging()
			=> Ok(new { hasAccess = repairRequestAction.HasImagingAccess() || IsAdministrator });

		[HttpGet("[action]")]
		[ActionDisplayName("گزارش تاخیر تعمیرات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult DelayList()
			=> View(@"\Views\Panel\Rpr\RepairRequest\DelayList.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("نمودار تاخیر تعمیرات", ActionAccessType.View, ActionAccessItemType.Dashbord)]
		public IActionResult DelayChart()
			=> View(@"\Views\Panel\Rpr\RepairRequest\DelayChart.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("داده نمودار تاخیر", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> DelayChartData(CancellationToken cn)
		{
			var rows = await unitOfWork.Repository<RepairRequest>().TableNoTracking
				.Where(x => x.HasDelay)
				.GroupBy(x => x.Status)
				.Select(g => new { status = g.Key, count = g.Count() })
				.ToListAsync(cn);
			return Ok(rows);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("تایید درخواست تعمیر", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Confirm(RepairRequestConfirmRequest request, CancellationToken cn)
		{
			if (!repairRequestAction.HasConfirmAccess() && !IsAdministrator)
				return BadRequest("دسترسی تایید را ندارید");
			if (request.Id == 0)
				return BadRequest("درخواست تعمیر یافت نشد");

			var entity = await unitOfWork.Repository<RepairRequest>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == request.Id, cn);
			if (entity == null)
				return BadRequest("درخواست تعمیر یافت نشد");

			var now = DateTime.Now;
			if (!string.IsNullOrWhiteSpace(request.DateShamsi) && request.DateMiladi == null)
			{
				try { request.DateMiladi = request.DateShamsi.ToMiladiDate(); }
				catch { request.DateMiladi = now; }
			}
			if (!string.IsNullOrWhiteSpace(request.AgreedShamsiDate) && request.AgreedMiladi == null)
			{
				try { request.AgreedMiladi = request.AgreedShamsiDate.ToMiladiDate(); }
				catch { request.AgreedMiladi = now; }
			}
			ApplyConfirm(entity, request, now);
			entity.IsConfirmOperation = true;
			ClearNavigations(entity);
			try
			{
				var saved = await unitOfWork.Repository<RepairRequest>().UpdateAsync(entity, cn, true);
				await repairRequestAction.SendConfirmEmailAsync(entity, request.ConfirmType, cn);
				return Ok(saved);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}
		}

		private void ApplyConfirm(RepairRequest entity, RepairRequestConfirmRequest request, DateTime now)
		{
			switch (request.ConfirmType)
			{
				case RepairRequestConfirmType.PreCheckedConfirm:
					entity.PreCheckShamsiDate = request.DateShamsi;
					entity.PreCheckMiladiDate = string.IsNullOrWhiteSpace(request.DateShamsi) ? null : request.DateMiladi ?? now;
					break;
				case RepairRequestConfirmType.PreCheckedConfirm2:
					entity.PreCheck2ShamsiDate = request.DateShamsi;
					entity.PreCheck2MiladiDate = string.IsNullOrWhiteSpace(request.DateShamsi) ? null : request.DateMiladi ?? now;
					entity.IsPreChecked2 = !string.IsNullOrWhiteSpace(request.DateShamsi);
					break;
				case RepairRequestConfirmType.FinancialProposalConfirm:
					entity.FinancialProposalShamsiDate = request.DateShamsi;
					entity.FinancialProposalMiladiDate = string.IsNullOrWhiteSpace(request.DateShamsi) ? null : request.DateMiladi ?? now;
					if (!string.IsNullOrWhiteSpace(request.AgreedShamsiDate))
					{
						entity.AgreedShamsiDate = request.AgreedShamsiDate;
						entity.AgreedMiladiDate = request.AgreedMiladi ?? now;
					}
					break;
				case RepairRequestConfirmType.FinancialProposalConfirm2:
					entity.FinancialProposal2ShamsiDate = request.DateShamsi;
					entity.FinancialProposal2MiladiDate = string.IsNullOrWhiteSpace(request.DateShamsi) ? null : request.DateMiladi ?? now;
					entity.IsApprovedFinancialProposal2 = !string.IsNullOrWhiteSpace(request.DateShamsi);
					break;
				case RepairRequestConfirmType.FullConfirm:
					if (entity.ConfirmerId != null)
					{
						entity.ConfirmerId = null;
						entity.ConfirmMiladiDate = null;
						entity.ConfirmShamsiDate = null;
						entity.ConfirmTime = null;
					}
					else
					{
						entity.ConfirmerId = CurrentUserId;
						entity.ConfirmShamsiDate = string.IsNullOrWhiteSpace(request.DateShamsi) ? now.ToString("yyyy/MM/dd") : request.DateShamsi;
						entity.ConfirmMiladiDate = request.DateMiladi ?? now;
						entity.ConfirmTime = now.ToString("HH:mm");
					}
					break;
				case RepairRequestConfirmType.FullConfirm2:
					entity.Confirm2ShamsiDate = request.DateShamsi;
					entity.Confirm2MiladiDate = string.IsNullOrWhiteSpace(request.DateShamsi) ? null : request.DateMiladi ?? now;
					entity.IsConfirmed2 = !string.IsNullOrWhiteSpace(request.DateShamsi);
					break;
				case RepairRequestConfirmType.SendFinancialProposal:
					entity.FinancialProposalSentShamsiDate = request.DateShamsi;
					entity.FinancialProposalSentMiladiDate = string.IsNullOrWhiteSpace(request.DateShamsi) ? null : request.DateMiladi ?? now;
					entity.IsSentFinancialProposal = !string.IsNullOrWhiteSpace(request.DateShamsi);
					break;
			}
		}

		private void FillAccessBags()
		{
			ViewBag.HasAfterSaleAccess = repairRequestAction.HasAfterSaleAccess() || IsAdministrator;
			ViewBag.HasRepairsAccess = repairRequestAction.HasRepairsAccess() || IsAdministrator;
			ViewBag.HasConfirmAccess = repairRequestAction.HasConfirmAccess() || IsAdministrator;
			ViewBag.HasImagingAccess = repairRequestAction.HasImagingAccess() || IsAdministrator;
		}

		private static void ClearNavigations(RepairRequest model)
		{
			model.ServiceRequest = null;
			model.Customer = null;
			model.CustomerAgency = null;
			model.CustomerAddress = null;
			model.Product = null;
			model.Zone = null;
			model.ZoneSupervisor = null;
			model.Dl = null;
			model.Confirmer = null;
			model.Responsible = null;
			model.CostCenter = null;
			model.InspectionPersonel = null;
			model.Station = null;
		}
	}

	public class RepairRequestConfirmRequest
	{
		public long Id { get; set; }
		public RepairRequestConfirmType ConfirmType { get; set; }
		public string? DateShamsi { get; set; }
		public DateTime? DateMiladi { get; set; }
		public string? AgreedShamsiDate { get; set; }
		public DateTime? AgreedMiladi { get; set; }
	}
}
