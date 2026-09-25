using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.App.Cng;
using Entities.App.Cng.Enums;
using Entities.App.Inv;
using Entities.Base;
using Entities.Base.DataTable;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	// Unique ControllerName; explicit route keeps /panel/cng/partrequest/*
	[Route("Panel/Cng/PartRequest")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("درخواست کالا CNG", typeof(PartRequest))]
	public class CngPartRequestController(
		IUnitOfWork unitOfWork,
		ApplicationDbContext db,
		IWebHostEnvironment env) : BaseController
	{
		private const string RoleManage = "Cng.PartRequest.Manage";
		private const string RoleStatus = "Cng.PartRequest.Status";
		private const string RoleShowAll = "Cng.PartRequest.ShowAll";
		private const string RoleShowAllMenus = "ShowAllMenus";

		private static readonly string[] EmailRecipients =
		[
			"Piri.m@havayar.com",
			"Kalhor.m@havayar.com",
			"Masoudi.l@havayar.com"
		];

		private bool CanChangeStatus =>
			IsAdministrator || CurrentUserHasAnyRole(RoleStatus, RoleShowAllMenus);

		private bool CanShowAllRows =>
			IsAdministrator || CurrentUserHasAnyRole(RoleStatus, RoleShowAll, RoleShowAllMenus);

		#region CRUD

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(PartRequest model, CancellationToken cn)
		{
			ClearNav(model);
			if (model.Id == null || model.Id == 0)
				return await Add(model, cn);
			if (await unitOfWork.Repository<PartRequest>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(PartRequest model, CancellationToken cn)
		{
			ClearNav(model);
			var err = ValidateItems(model.Items, model.ImmediateSending == true);
			if (err != null) return BadRequest(err);

			if (model.ImmediateSending == true)
			{
				if (string.IsNullOrWhiteSpace(model.Receiver))
					return BadRequest("گیرنده برای ارسال فوری اجباری است");
				if (string.IsNullOrWhiteSpace(model.SendingLocation))
					return BadRequest("محل ارسال برای ارسال فوری اجباری است");
				model.LastStatusId = PartRequestStatusEnum.SendItems;
				model.LastStatusComment = "وضعیت درخواست به حالت درخواست ارسال اقلام در آمده است.";
			}
			else
			{
				model.LastStatusId = PartRequestStatusEnum.Inquiry;
			}

			ApplyFileMetaFromFile(model);
			var items = model.Items ?? [];
			model.Items = null;

			var saved = await unitOfWork.Repository<PartRequest>().SaveAsync(model, cn, true);
			await ReplaceItemsAsync(saved.Id!.Value, items, cn);

			var withItems = await LoadForEditAsync(saved.Id.Value, cn);
			await SendAddEmailAsync(withItems!, items, cn);

			return Ok(withItems);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(PartRequest model, CancellationToken cn)
		{
			ClearNav(model);
			var old = await unitOfWork.Repository<PartRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (old == null)
				return BadRequest("رکورد یافت نشد");

			if (old.LastStatusId == PartRequestStatusEnum.Inquiry && !CanChangeStatus)
				return BadRequest("در وضعیت استعلام امکان ویرایش وجود ندارد");

			var err = ValidateItems(model.Items, model.ImmediateSending == true);
			if (err != null) return BadRequest(err);

			if (!CanChangeStatus)
				model.LastStatusId = old.LastStatusId;

			var statusChangedByCreator =
				old.LastStatusId == PartRequestStatusEnum.Edit
				&& old.CreatedById == CurrentUserId;
			if (statusChangedByCreator)
			{
				model.LastStatusId = PartRequestStatusEnum.Inquiry;
				model.LastStatusComment = "درخواست پس از ویرایش درخواست کننده به حالت استعلام در آمده است.";
			}

			var newFileUploaded = model.FileId is > 0 && model.FileId != old.FileId;
			if (newFileUploaded && CanChangeStatus)
			{
				model.LastStatusId = PartRequestStatusEnum.Prefactor;
				model.LastStatusComment = "وضعیت درخواست به حالت پیش فاکتور در آمده است.";
				ApplyFileMetaFromFile(model);
			}
			else if (model.FileId is null or 0)
			{
				model.FileId = old.FileId;
				model.AttachmentFileName = old.AttachmentFileName;
				model.AttachmentFileSize = old.AttachmentFileSize;
				model.AttachmentFilePath = old.AttachmentFilePath;
			}
			else
			{
				ApplyFileMetaFromFile(model);
			}

			model.Comment = old.Comment;
			model.HtsId = old.HtsId;

			var oldStatus = old.LastStatusId;
			var items = model.Items ?? [];
			model.Items = null;

			var saved = await unitOfWork.Repository<PartRequest>().UpdateAsync(model, cn, true);
			await ReplaceItemsAsync(saved.Id!.Value, items, cn);

			var withItems = await LoadForEditAsync(saved.Id.Value, cn);
			if (oldStatus != model.LastStatusId)
				await SendStatusChangeEmailAsync(withItems!, cn);

			return Ok(withItems);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var items = await unitOfWork.Repository<PartRequestItem>().TableNoTracking
				.Where(c => c.PartRequestId == id).ToListAsync(cn);
			foreach (var item in items)
				await unitOfWork.Repository<PartRequestItem>().DeleteAsync(item, cn, true);

			var entity = await unitOfWork.Repository<PartRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity != null)
				await unitOfWork.Repository<PartRequest>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			PartRequest entity;
			if (id != null && id != 0)
			{
				entity = (await LoadForEditAsync(id.Value, cn)) ?? CreateNew();
				if (!CanShowAllRows && entity.CreatedById != CurrentUserId && entity.Id is > 0)
					return BadRequest("دسترسی به این رکورد وجود ندارد");
			}
			else
				entity = CreateNew();

			FillViewBag(entity);
			return View(@"\Views\Panel\Cng\PartRequest\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var entity = CreateNew();
			FillViewBag(entity);
			return View(@"\Views\Panel\Cng\PartRequest\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Cng\PartRequest\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<PartRequest>().FetchDataAsync(request, cn));

		#endregion

		#region Actions

		[HttpPost("[action]")]
		[ActionDisplayName("ارسال پیش فاکتور", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SendPreFactor(long id, string? requestDescription, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<PartRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null)
				return BadRequest("رکورد یافت نشد");
			if (entity.LastStatusId != PartRequestStatusEnum.Prefactor)
				return BadRequest("ارسال پیش‌فاکتور فقط در وضعیت پیش فاکتور مجاز است");

			entity.RequestDescription = requestDescription;
			entity.LastStatusId = PartRequestStatusEnum.SendItems;
			entity.LastStatusComment = "وضعیت درخواست به حالت درخواست ارسال اقلام در آمده است.";
			ClearNav(entity);
			var saved = await unitOfWork.Repository<PartRequest>().UpdateAsync(entity, cn, true);

			await QueueEmailAsync(
				"ارسال اقلام درخواست کالا (CNG)",
				BuildSendItemsEmailBody(saved),
				saved.Id,
				cn);

			return Ok(await LoadForEditAsync(saved.Id!.Value, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("انجام شد", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Done(long id, CancellationToken cn)
		{
			if (!CanChangeStatus)
				return BadRequest("دسترسی انجام‌شد وجود ندارد");

			var entity = await unitOfWork.Repository<PartRequest>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null)
				return BadRequest("رکورد یافت نشد");

			var oldStatus = entity.LastStatusId;
			entity.LastStatusId = PartRequestStatusEnum.Closed;
			entity.LastStatusComment = "درخواست بسته شده است.";
			ClearNav(entity);
			var saved = await unitOfWork.Repository<PartRequest>().UpdateAsync(entity, cn, true);

			if (oldStatus != PartRequestStatusEnum.Closed)
				await SendStatusChangeEmailAsync(saved, cn);

			return Ok(await LoadForEditAsync(saved.Id!.Value, cn));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دانلود فایل", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Download(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<PartRequest>().TableNoTracking
				.Include(c => c.File)
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null)
				return BadRequest("رکورد یافت نشد");

			if (entity.FileId is > 0 && entity.File != null && !string.IsNullOrWhiteSpace(entity.File.PhysicalPath))
			{
				var physical = ResolvePhysicalPath(entity.File.PhysicalPath);
				if (System.IO.File.Exists(physical))
					return PhysicalFile(physical, MediaTypeNames.Application.Octet, entity.File.OriginalName ?? entity.AttachmentFileName ?? "file");
			}

			if (!string.IsNullOrWhiteSpace(entity.AttachmentFilePath) && System.IO.File.Exists(entity.AttachmentFilePath))
				return PhysicalFile(entity.AttachmentFilePath, MediaTypeNames.Application.Octet, entity.AttachmentFileName ?? "file");

			return BadRequest("فایل پیوست یافت نشد");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("اقلام", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetItems(long partRequestId, CancellationToken cn)
		{
			var items = await unitOfWork.Repository<PartRequestItem>().TableNoTracking
				.Include(c => c.Part)
				.Include(c => c.Station)
				.Where(c => c.PartRequestId == partRequestId)
				.OrderBy(c => c.Id)
				.Select(c => new
				{
					c.Id,
					c.HtsId,
					c.PartRequestId,
					c.UsagePlaceId,
					UsagePlace = c.UsagePlaceId == 0 ? "" : ((PartRequestUsagePlaceEnum)(int)c.UsagePlaceId).ToString(),
					UsagePlaceTitle = GetEnumDisplayName(c.UsagePlaceId),
					c.StationId,
					StationTitle = c.Station != null ? c.Station.StationTitle : null,
					c.PartId,
					PartCode = c.Part != null ? c.Part.Code : null,
					PartName = c.Part != null ? c.Part.Name : null,
					PartTitle = c.Part != null ? ((c.Part.Code ?? "") + " | " + (c.Part.Name ?? "")).Trim() : null,
					c.Mount,
					c.Comment
				})
				.ToListAsync(cn);
			return Ok(items);
		}

		/// <summary>فرم مودال قلم — بدون ActionDisplayName.</summary>
		[HttpGet("[action]")]
		public IActionResult ItemForm(long? partId, long? stationId, int? usagePlaceId, decimal? mount, string? comment, long? id, long? htsId)
		{
			var item = new PartRequestItem
			{
				Id = id,
				HtsId = htsId ?? 0,
				PartId = partId ?? 0,
				StationId = stationId,
				Mount = mount ?? 0,
				Comment = comment,
				UsagePlaceId = usagePlaceId.HasValue
					? (PartRequestUsagePlaceEnum)usagePlaceId.Value
					: default
			};
			if (partId is > 0)
			{
				item.Part = unitOfWork.Repository<Part>().TableNoTracking.FirstOrDefault(c => c.Id == partId);
			}
			if (stationId is > 0)
			{
				item.Station = unitOfWork.Repository<StationInfo>().TableNoTracking.FirstOrDefault(c => c.Id == stationId);
			}
			return View(@"\Views\Panel\Cng\PartRequest\_ItemForm.cshtml", item);
		}

		/// <summary>موجودی آزاد انبار خدمات CNG (۱۷) — بدون ActionDisplayName.</summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> GetStock(long partId, CancellationToken cn)
		{
			if (partId <= 0)
				return Ok(new { isSuccess = false, qty = "0" });

			var part = await unitOfWork.Repository<Part>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == partId, cn);
			if (part == null || part.HtsId == 0)
				return Ok(new { isSuccess = false, qty = "0" });

			var shamsiFull = DateTime.Now.ToShamsiDate();
			var shamsiYear = shamsiFull.Length >= 4 ? shamsiFull[..4] : shamsiFull;
			var hamkaranYear = shamsiYear.GetHamkaranYearNumber();
			if (!short.TryParse(hamkaranYear, out var year))
				return Ok(new { isSuccess = false, qty = "0" });

			const string sql = """
				SELECT CAST(ISNULL(SUM(VQtyRatio), 0) AS decimal(18,4)) AS Qty
				FROM [TMS].[TotalSystem].[dbo].[Inv_PartCardex]
				WHERE IsPermanentVoucher = 1
				  AND StockId = 17
				  AND [Year] = @year
				  AND PartId = @partId
				""";

			try
			{
				var qty = await db.Database
					.SqlQueryRaw<StockQtyRow>(sql,
						new SqlParameter("@year", year),
						new SqlParameter("@partId", part.HtsId))
					.FirstOrDefaultAsync(cn);
				var value = qty?.Qty ?? 0m;
				return Ok(new { isSuccess = true, qty = value.ToString(CultureInfo.InvariantCulture) });
			}
			catch
			{
				return Ok(new { isSuccess = false, qty = "0" });
			}
		}

		#endregion

		#region Helpers

		private void FillViewBag(PartRequest entity)
		{
			ViewBag.CanChangeStatus = CanChangeStatus;
			ViewBag.CanShowAll = CanShowAllRows;
			var isInquiryLocked = entity.Id is > 0
				&& entity.LastStatusId == PartRequestStatusEnum.Inquiry
				&& !CanChangeStatus;
			ViewBag.CanSave = !isInquiryLocked;
			ViewBag.HasFile = entity.FileId is > 0
				|| !string.IsNullOrWhiteSpace(entity.AttachmentFileName)
				|| !string.IsNullOrWhiteSpace(entity.AttachmentFilePath);
			ViewBag.IsPrefactor = entity.LastStatusId == PartRequestStatusEnum.Prefactor;
		}

		private static PartRequest CreateNew() => new()
		{
			LastStatusId = PartRequestStatusEnum.Inquiry,
			ImmediateSending = false,
			Items = []
		};

		private static void ClearNav(PartRequest model)
		{
			model.File = null;
			if (model.Items != null)
			{
				foreach (var item in model.Items)
				{
					item.Part = null;
					item.Station = null;
					item.PartRequest = null;
				}
			}
		}

		private static string? ValidateItems(List<PartRequestItem>? items, bool immediateSending)
		{
			if (items == null || items.Count == 0)
				return "حداقل یک قلم کالا الزامی است";

			foreach (var item in items)
			{
				if (item.PartId <= 0)
					return "کالا نمی‌تواند خالی باشد";
				if (item.Mount <= 0)
					return "تعداد باید بزرگ‌تر از صفر باشد";
				if (item.UsagePlaceId == PartRequestUsagePlaceEnum.Station && (item.StationId == null || item.StationId == 0))
					return "برای محل مصرف جایگاه، انتخاب جایگاه اجباری است";
			}
			return null;
		}

		private async Task ReplaceItemsAsync(long partRequestId, List<PartRequestItem> items, CancellationToken cn)
		{
			var existing = await unitOfWork.Repository<PartRequestItem>().Table
				.Where(c => c.PartRequestId == partRequestId)
				.ToListAsync(cn);
			foreach (var old in existing)
				await unitOfWork.Repository<PartRequestItem>().DeleteAsync(old, cn, true);

			foreach (var item in items)
			{
				item.Id = null;
				item.PartRequestId = partRequestId;
				item.Part = null;
				item.Station = null;
				item.PartRequest = null;
				if (item.UsagePlaceId != PartRequestUsagePlaceEnum.Station)
					item.StationId = null;
				await unitOfWork.Repository<PartRequestItem>().SaveAsync(item, cn, true);
			}
		}

		private async Task<PartRequest?> LoadForEditAsync(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<PartRequest>().TableNoTracking
				.Include(c => c.File)
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity == null) return null;

			entity.Items = await unitOfWork.Repository<PartRequestItem>().TableNoTracking
				.Include(c => c.Part)
				.Include(c => c.Station)
				.Where(c => c.PartRequestId == id)
				.OrderBy(c => c.Id)
				.ToListAsync(cn);
			return entity;
		}

		private void ApplyFileMetaFromFile(PartRequest model)
		{
			if (model.FileId is null or 0) return;
			var file = unitOfWork.Repository<FileEntity>().TableNoTracking
				.FirstOrDefault(c => c.Id == model.FileId);
			if (file == null) return;
			model.AttachmentFileName = Truncate(file.OriginalName, 128);
			model.AttachmentFileSize = file.Size > int.MaxValue ? int.MaxValue : (int)file.Size;
			model.AttachmentFilePath = Truncate(file.PhysicalPath, 255);
		}

		private string ResolvePhysicalPath(string path)
		{
			if (Path.IsPathRooted(path) || path.StartsWith(@"\\", StringComparison.Ordinal))
				return path;
			var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
			return Path.Combine(webRoot, path.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar));
		}

		private async Task SendAddEmailAsync(PartRequest entity, List<PartRequestItem> items, CancellationToken cn)
		{
			var immediate = entity.ImmediateSending == true;
			var title = immediate ? "درخواست ارسال کالا فوری" : "استعلام قیمت کالا (CNG)";
			var subject = title;

			var partIds = items.Select(i => i.PartId).Distinct().ToList();
			var parts = await unitOfWork.Repository<Part>().TableNoTracking
				.Where(p => partIds.Contains(p.Id!.Value))
				.ToListAsync(cn);
			var stationIds = items.Where(i => i.StationId.HasValue).Select(i => i.StationId!.Value).Distinct().ToList();
			var stations = stationIds.Count == 0
				? []
				: await unitOfWork.Repository<StationInfo>().TableNoTracking
					.Where(s => stationIds.Contains(s.Id!.Value)).ToListAsync(cn);

			var sb = new StringBuilder();
			sb.AppendLine("<div style='text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl'  width='672' >");
			sb.AppendLine("<tr style='width: 7.0in; background: #000aa0'><td colspan='2'>");
			sb.AppendLine($"<div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center;direction:rtl'>گروه صنعتی هوایار<br/>{title}</div>");
			sb.AppendLine("</td></tr><tr><td colspan='2'>");
			sb.AppendLine("<div style='font-size:14.0pt;font-family:Zar;color:#1F497D;text-align:right;direction:rtl'>");
			sb.AppendLine("با سلام و احترام <br />");
			sb.AppendLine($"درخواست کالای جدید با شماره {entity.Id} و مشخصات ذیل در سیستم ثبت شد:<br/>");
			sb.AppendLine("<table>");
			sb.AppendLine("<tr style=\"background-color:#4472c4;\"><td style=\"width:50px;\">ردیف</td><td style=\"width:100px;\">کد کالا</td><td style=\"width:250px;\">شرح کالا</td><td style=\"width:50px;\">تعداد</td><td style=\"width:50px;\">جایگاه</td><td style=\"width:350px;\">کامنت</td></tr>");
			var index = 1;
			foreach (var item in items)
			{
				var part = parts.FirstOrDefault(p => p.Id == item.PartId);
				var station = stations.FirstOrDefault(s => s.Id == item.StationId);
				sb.AppendLine("<tr style=\"background-color:#d9e2f3;\">");
				sb.AppendLine($"<td>{index}</td><td>{part?.Code}</td><td>{part?.Name}</td><td>{item.Mount}</td><td>{station?.StationTitle}</td><td>{item.Comment}</td>");
				sb.AppendLine("</tr>");
				index++;
			}
			sb.AppendLine("</table><ul>");
			sb.AppendLine($"<li>گیرنده: {entity.Receiver}</li>");
			sb.AppendLine($"<li>محل آدرس: {entity.SendingLocation}</li>");
			sb.AppendLine($"<li>درخواست کننده: {CurrentUserFullName}</li>");
			sb.AppendLine("</ul></div></td></tr></table></div>");

			await QueueEmailAsync(subject, sb.ToString(), entity.Id, cn);
		}

		private async Task SendStatusChangeEmailAsync(PartRequest entity, CancellationToken cn)
		{
			var lastStatus = GetEnumDisplayName(entity.LastStatusId);
			var sb = new StringBuilder();
			sb.AppendLine("<div style='text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl'  width='672' >");
			sb.AppendLine("<tr style='width: 7.0in; background: #000aa0'><td colspan='2'>");
			sb.AppendLine("<div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center;direction:rtl'>گروه صنعتی هوایار<br/>ویرایش درخواست کالا (CNG)</div>");
			sb.AppendLine("</td></tr><tr><td colspan='2'>");
			sb.AppendLine("<div style='font-size:14.0pt;font-family:Zar;color:#1F497D;text-align:right;direction:rtl'>");
			sb.AppendLine("با سلام و احترام <br />");
			sb.AppendLine($"وضعیت نهایی درخواست کالا به شماره شناسه {entity.Id} در سیستم به حالت {lastStatus} تغییر پیدا کرد.<br/>");
			sb.AppendLine($"کامنت وضعیت: {entity.LastStatusComment}");
			sb.AppendLine("</div></td></tr></table></div>");

			await QueueEmailAsync("ویرایش درخواست کالا (CNG)", sb.ToString(), entity.Id, cn);
		}

		private static string BuildSendItemsEmailBody(PartRequest entity)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<div style='text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl'  width='672' >");
			sb.AppendLine("<tr style='width: 7.0in; background: #000aa0'><td colspan='2'>");
			sb.AppendLine("<div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center;direction:rtl'>گروه صنعتی هوایار<br/>ارسال اقلام درخواست کالا (CNG)</div>");
			sb.AppendLine("</td></tr><tr><td colspan='2'>");
			sb.AppendLine("<div style='font-size:14.0pt;font-family:Zar;color:#1F497D;text-align:right;direction:rtl'>");
			sb.AppendLine("با سلام و احترام <br />");
			sb.AppendLine($"برای درخواست کالا با شماره پیش فاکتور {entity.InvoiceNumber} درخواست ارسال پیش فاکتور ثبت گردید.<br/>");
			sb.AppendLine($"شرح درخواست: {entity.RequestDescription}<br/>");
			sb.AppendLine("</div></td></tr></table></div>");
			return sb.ToString();
		}

		private async Task QueueEmailAsync(string subject, string body, long? entityId, CancellationToken cn)
		{
			if (CurrentUserId is null or 0) return;
			var notification = new Notification
			{
				Type = NotificationType.Email,
				Title = subject,
				Body = body,
				OwnerId = CurrentUserId.Value,
				EntityId = entityId,
				ViewPath = entityId.HasValue ? $"/Panel/Cng/PartRequest/Edit?id={entityId}" : "/Panel/Cng/PartRequest/List",
				IsRead = false,
				IsSend = false,
				ToEmails = EmailRecipients.ToList()
			};
			await unitOfWork.Repository<Notification>().AddAsync(notification, cn);
			await unitOfWork.SaveChangesAsync(cn);
		}

		private static string GetEnumDisplayName(Enum value)
		{
			var raw = Convert.ToInt32(value);
			if (raw == 0) return "";
			var name = Enum.GetName(value.GetType(), value);
			if (name == null) return raw.ToString();
			var member = value.GetType().GetMember(name).FirstOrDefault();
			var display = member?.GetCustomAttribute<DisplayAttribute>();
			return display?.Name ?? name;
		}

		private static string? Truncate(string? value, int max)
			=> string.IsNullOrEmpty(value) ? value : (value.Length <= max ? value : value[..max]);

		private sealed class StockQtyRow
		{
			public decimal Qty { get; set; }
		}

		#endregion
	}
}
