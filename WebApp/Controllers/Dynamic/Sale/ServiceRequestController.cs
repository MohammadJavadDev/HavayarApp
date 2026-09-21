using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Entities.App.Crm;
using Entities.App.Gnr;
using Entities.App.Gnr.Enums;
using Entities.App.Hcm;
using Entities.App.Rpr;
using Entities.App.Sale;
using Entities.App.SLS;
using Entities.Base.DataTable;
using Entities.Base.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using WebApp.Actions.Sale;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sale/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("درخواست پشتیبانی", typeof(ServiceRequest))]
	public class ServiceRequestController(IUnitOfWork unitOfWork, IWebHostEnvironment env, IUserService userService) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ServiceRequest model, CancellationToken cn)
		{
			var saved = await unitOfWork.Repository<ServiceRequest>().SaveAsync(model, cn, true);
			await FillSelectorIds(saved, cn);
			return Ok(saved);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(ServiceRequest model, CancellationToken cn)
		{
			var saved = await unitOfWork.Repository<ServiceRequest>().SaveAsync(model, cn, true);
			await FillSelectorIds(saved, cn);
			return Ok(saved);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ServiceRequest model, CancellationToken cn)
		{
			var saved = await unitOfWork.Repository<ServiceRequest>().SaveAsync(model, cn, true);
			await FillSelectorIds(saved, cn);
			return Ok(saved);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<ServiceRequest>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<ServiceRequest>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			var entity = (id != null && id != 0)
				? await unitOfWork.Repository<ServiceRequest>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn)
				: new ServiceRequest();
			entity ??= new ServiceRequest();
			await FillSelectorIds(entity, cn);
			ViewBag.IsCompanyUser = await IsCompanyUser(cn);
			return View(@"\Views\Panel\Sale\ServiceRequest\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public async Task<IActionResult> New(CancellationToken cn)
		{
			ViewBag.IsCompanyUser = await IsCompanyUser(cn);
			return View(@"\Views\Panel\Sale\ServiceRequest\Edit.cshtml", new ServiceRequest());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Sale\ServiceRequest\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("اعزام کارشناس", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult Dispatch() => View(@"\Views\Panel\Sale\ServiceRequest\Dispatch.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("گزارش کار", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult WorkReport() => Ok();

		[HttpGet("[action]")]
		[ActionDisplayName("ماموریت", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult Mission() => Ok();

		[HttpGet("[action]")]
		[ActionDisplayName("درخواست کالا", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult RequestPart() => Ok();

		[HttpGet("[action]")]
		[ActionDisplayName("ارسال پیوست", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult SendAttachment() => Ok();

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ServiceRequest>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<ServiceRequest>().FetchDataAsync(request, cn));

		[HttpGet("[action]")]
		[ActionDisplayName("اطلاعات مشتری", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> GetCustomerInfo(long customerId, CancellationToken cn)
		{
			var party = await unitOfWork.Repository<Customer>().TableNoTracking
				.Where(c => c.Id == customerId)
				.Select(c => c.Party)
				.FirstOrDefaultAsync(cn);
			var phones = party == null ? null : string.Join(" | ", new[] { party.Phone, party.Mobile }.Where(s => !string.IsNullOrWhiteSpace(s)));
			return Ok(new { phone = phones });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("اطلاعات سایت مشتری", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> GetCustomerAddressInfo(long customerAddressId, CancellationToken cn)
		{
			var address = await unitOfWork.Repository<CustomerAddress>().TableNoTracking
				.Where(c => c.Id == customerAddressId)
				.Select(c => new { c.ZoneId, ZoneTitle = c.Zone != null ? c.Zone.Title : null })
				.FirstOrDefaultAsync(cn);
			return Ok(new { zoneId = address?.ZoneId, zoneTitle = address?.ZoneTitle });
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ثبت اعزام کارشناس", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SendDispatch(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ServiceRequest>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return BadRequest("درخواست یافت نشد");
			if (entity.DispatchMiladiDate == null && string.IsNullOrWhiteSpace(entity.DispatchShamsiDate))
			{
				entity.DispatchMiladiDate = DateTime.Now;
				entity.DispatchShamsiDate = DateTime.Now.ToShamsiDate();
			}
			entity.IsWaitingToSend = true;
			if (entity.DispatchMiladiDate == null && !string.IsNullOrWhiteSpace(entity.DispatchShamsiDate))
				entity.DispatchMiladiDate = entity.DispatchShamsiDate.ToMiladiDate();
			if (string.IsNullOrWhiteSpace(entity.DispatchShamsiDate) && entity.DispatchMiladiDate != null)
				entity.DispatchShamsiDate = entity.DispatchMiladiDate.Value.ToShamsiDate();

			ServiceRequest saved;
			try
			{
				saved = await unitOfWork.Repository<ServiceRequest>().UpdateAsync(entity, cn, true);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.InnerException?.Message ?? ex.Message);
			}

			await ServiceRequestResponsibleNotify.NotifyAsync(
				unitOfWork,
				userService,
				saved,
				"<div style='direction:rtl;text-align:right'>درخواست اعزام کارشناس ثبت شد.<br/>شماره: "
					+ entity.Id + "<br/>تاریخ اعزام: " + entity.DispatchShamsiDate + "</div>",
				cn);
			return Ok(saved);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ابلاغ و ارسال گزارش کارهای قبلی", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SendWorkReportAttachment(long serviceRequestId, CancellationToken cn)
		{
			var request = await unitOfWork.Repository<ServiceRequest>().TableNoTracking
				.Include(c => c.Customer)
				.ThenInclude(c => c!.Party)
				.Include(c => c.CustomerAddress)
				.FirstOrDefaultAsync(c => c.Id == serviceRequestId, cn);
			if (request == null)
				return BadRequest("درخواست یافت نشد");

			var expertEmails = await unitOfWork.Repository<ServiceRequestExpertMission>().TableNoTracking
				.Where(c => c.ServiceRequestId == serviceRequestId)
				.Select(c => new
				{
					PersonelEmail = c.Personel != null ? c.Personel.Email : null,
					ExpertEmail = c.Expert != null ? c.Expert.Email : null
				})
				.ToListAsync(cn);
			var emails = expertEmails
				.SelectMany(c => new[] { c.PersonelEmail, c.ExpertEmail })
				.Where(e => !string.IsNullOrWhiteSpace(e))
				.Select(e => e!.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
			if (emails.Count == 0)
				return BadRequest("هیچ آدرس ایمیلی ثبت نشده است");

			var fileIds = await unitOfWork.Repository<WorkReportAttachment>().TableNoTracking
				.Where(a => a.FileId != null && a.FileId != 0
					&& a.WorkReport != null
					&& a.WorkReport.ServiceRequestDetail != null
					&& unitOfWork.Repository<ServiceRequestDetail>().TableNoTracking.Any(cur =>
						cur.ServiceRequestId == serviceRequestId
						&& cur.OrderDetailId == a.WorkReport.ServiceRequestDetail.OrderDetailId
						&& cur.OrderDetailSerialId == a.WorkReport.ServiceRequestDetail.OrderDetailSerialId
						&& cur.OtherPartId == a.WorkReport.ServiceRequestDetail.OtherPartId))
				.Select(a => a.FileId!.Value)
				.Distinct()
				.ToListAsync(cn);
			if (fileIds.Count == 0)
				return BadRequest("هیچ گزارشی از قبل برای ارسال وجود ندارد");

			var customerTitle = request.Customer?.Party?.FullName ?? "";
			var siteTitle = request.CustomerAddress?.Title;
			var siteAddress = request.CustomerAddress?.Address;
			var body = new System.Text.StringBuilder();
			body.Append("<div style='text-align:center;direction:rtl'>");
			body.Append("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right;direction:rtl;width:100%'>");
			body.Append("<tr style='background:#000aa0'><td colspan='2'><div style='font-size:14pt;color:#fff;text-align:center'>Havayar Industrial Group</div></td></tr>");
			body.Append("<tr><td colspan='2'><div style='font-size:14pt;color:#1F497D;text-align:right'>با سلام <br />");
			body.Append(" درخواست پشتیبانی به شماره ").Append(request.Id).Append(" جهت مشتری ").Append(customerTitle);
			body.Append(" به شرح : ").Append(request.Comment);
			if (!string.IsNullOrWhiteSpace(siteTitle))
				body.Append(" :در سایت ").Append(siteTitle).Append(" به آدرس : ").Append(siteAddress);
			else
				body.Append(" به آدرس : ").Append(siteAddress);
			body.Append(" به شما ابلاغ گردید.<br /> همچنین سوابق گزارش کارهای قبلی طی پیوست ارسال گردید");
			body.Append("</div></td></tr></table></div>");

			await unitOfWork.Repository<Entities.Base.Notification.Notification>().AddAsync(new Entities.Base.Notification.Notification
			{
				Type = NotificationType.Email,
				Title = "ابلاغیه ماموریت",
				Body = body.ToString(),
				EntityId = request.Id,
				OwnerId = CurrentUserId ?? 1,
				ViewPath = "/Panel/Sale/ServiceRequest/Edit?id=" + request.Id,
				IsRead = false,
				IsSend = false,
				ToEmails = emails,
				AttachmentFileIds = fileIds
			}, cn);
			return Ok(new { sent = emails.Count, files = fileIds.Count });
		}

		private async Task<bool> IsCompanyUser(CancellationToken cn)
		{
			if (IsAdministrator || CurrentUserId is null or 0)
				return false;
			var partyId = await userService.TableNoTracking
				.Where(u => u.Id == CurrentUserId)
				.Select(u => u.PartyId)
				.FirstOrDefaultAsync(cn);
			if (partyId is null or 0)
				return false;
			var type = await unitOfWork.Repository<Party>().TableNoTracking
				.Where(p => p.Id == partyId)
				.Select(p => (PartyTypeEnum?)p.Type)
				.FirstOrDefaultAsync(cn);
			return type == PartyTypeEnum.Legal;
		}

		private async Task FillSelectorIds(ServiceRequest entity, CancellationToken cn)
		{
			if (entity.Id == null || entity.Id == 0)
				return;

			var missions = await unitOfWork.Repository<ServiceRequestExpertMission>().TableNoTracking
				.Where(c => c.ServiceRequestId == entity.Id)
				.Select(c => new { c.PersonelId, c.ExpertId })
				.ToListAsync(cn);
			var expertIds = missions
				.Where(c => c.PersonelId != null && c.PersonelId != 0)
				.Select(c => c.PersonelId!.Value)
				.ToHashSet();
			var missingUserIds = missions
				.Where(c => (c.PersonelId == null || c.PersonelId == 0) && c.ExpertId != null && c.ExpertId != 0)
				.Select(c => c.ExpertId!.Value)
				.Distinct()
				.ToList();
			if (missingUserIds.Count > 0)
			{
				var partyIds = await userService.TableNoTracking
					.Where(u => u.Id != null && missingUserIds.Contains(u.Id.Value) && u.PartyId != null)
					.Select(u => u.PartyId!.Value)
					.ToListAsync(cn);
				if (partyIds.Count > 0)
				{
					var extra = await unitOfWork.Repository<Personel>().TableNoTracking
						.Where(p => p.Id != null && p.PartyId != null && partyIds.Contains(p.PartyId.Value))
						.Select(p => p.Id!.Value)
						.ToListAsync(cn);
					foreach (var id in extra)
						expertIds.Add(id);
				}
			}
			entity.ExpertPersonelIds = expertIds.Count > 0 ? string.Join(",", expertIds) : "";

			var repairIds = await unitOfWork.Repository<RepairRequest>().TableNoTracking
				.Where(c => c.ServiceRequestId == entity.Id)
				.Select(c => c.Id!.Value)
				.ToListAsync(cn);
			if (repairIds.Count == 0 && !string.IsNullOrWhiteSpace(entity.LegacyRepairRequestIds))
			{
				var legacy = ServiceRequestAction.ParseIds(entity.LegacyRepairRequestIds);
				repairIds = await unitOfWork.Repository<RepairRequest>().TableNoTracking
					.Where(c => c.Id != null && legacy.Contains(c.Id.Value))
					.Select(c => c.Id!.Value)
					.ToListAsync(cn);
			}
			entity.LegacyRepairRequestIds = repairIds.Count > 0 ? string.Join(",", repairIds) : "";
		}
	}
}
