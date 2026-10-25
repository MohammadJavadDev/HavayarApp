using Aspose.Cells;
using Common;
using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Data.Services.Sup;
using Data.SystemAuth;
using Entities.App.Edms;
using Entities.App.Edms.Enums;
using Entities.App.Gnr;
using Entities.App.Hrm;
using Entities.App.Inv;
using Entities.App.Inv.Enums;
using Entities.App.Pln;
using Entities.App.Sup;
using Entities.App.Sup.Enums;
using Entities.Auth;
using Entities.Base.DataTable;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using Services.NotificationGroupServices;
using Services.NotificationServices;
using Services.ProductionOrderServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebApp.ViewModels.Sup;
using WebFramework.Api;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Sup/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("درخواست های باز", typeof(OpenOrderRequest))]
	public class OpenOrderRequestController(
		IUnitOfWork unitOfWork,
		IWebHostEnvironment _webHostEnvironment,
		IUserService userService,
		INotificationGroupService _notificationGroupService,
		INotificationService notificationService,
		IOnlineUserService onlineUserService
		) : BaseController
	{
		#region Roles / Permission helpers (HTS page 74 «درخواست های باز» + page 71 «پیوست»)

		/// <summary>
		/// نقش‌های seed (Entities/Auth/Role.cs 100000–100012، 100017 در DB). نگاشت PageAction های HTS:
		///  FullAccess(2)→SupplyAndPurchase · Accept_Engineering(12)→EngineeringAccept · SalesOrProjectPermission(187)→SalesOrProjectAccept
		///  Stop(22)→Stop · Start(23)→Start · Sending(24)→Sending · Query(25)→Query · Terminate(101)→Terminate
		///  HasEngineeringPermission(82)→HasEngineering · ShowAll(7)→ShowAll · صفحه 76→ConfigManage · Read(3)→View · صفحه 77→HistoryView
		/// </summary>
		private static class Roles
		{
			public const string SupplyAndPurchase = "SupplyAndPurchase";                       // HTS گروه 17 «مدیر سیستم تدارکات» (FullAccess)
			public const string EngineeringAccept = "Sup.OpenOrderRequest.EngineeringAccept";
			public const string SalesOrProjectAccept = "Sup.OpenOrderRequest.SalesOrProjectAccept";
			public const string Stop = "Sup.OpenOrderRequest.Stop";
			public const string Start = "Sup.OpenOrderRequest.Start";
			public const string Sending = "Sup.OpenOrderRequest.Sending";
			public const string Query = "Sup.OpenOrderRequest.Query";
			public const string Terminate = "Sup.OpenOrderRequest.Terminate";
			public const string HasEngineering = "Sup.OpenOrderRequest.HasEngineering";
			public const string Industrial = "Sup.OpenOrderRequest.Industrial";
			public const string IndustriesLegacy = "Industries";                                 // نقش قدیمی 3 «صنایع» — همان RoleAccess نقش Industrial
			public const string Supply = "Sup.OpenOrderRequest.Supply";
			public const string ConfigManage = "Sup.OpenOrderRequest.ConfigManage";
			public const string View = "Sup.OpenOrderRequest.View";
		}

		/// <summary>
		/// همان الگوی EngineeringAccept/SalesOrProjectAccept (CurrentUserHasAnyRole) + میان‌بر ادمین
		/// مثل Edit.cshtml (isAdmin || sdk.HasRole) و AppSdk.hasRole سمت کلاینت.
		/// </summary>
		private bool HasOpenOrderRequestPermission(params string[] roleNames)
			=> IsAdministrator || CurrentUserHasAnyRole(roleNames);

		/// <summary>پاسخ استاندارد عدم دسترسی: { isSuccess:false, statusCode:UnAuthorized, message } با HTTP 403.</summary>
		private IActionResult AccessDenied(string message)
			=> new JsonResult(new ApiResult(false, ApiResultStatusCode.UnAuthorized, message))
			{
				StatusCode = StatusCodes.Status403Forbidden
			};

		// HTS 74·Edit(5): ثبت کامنت (DoOperation با OperationType=Update) — گروه‌های 17 (Full)، 27 (تدارکات)، 70 (تایید مهندسی)
		private bool CanSaveComment => HasOpenOrderRequestPermission(Roles.SupplyAndPurchase, Roles.Supply, Roles.EngineeringAccept, Roles.Industrial, Roles.IndustriesLegacy);

		// HTS صفحه 71 (پیوست): New/Edit/Delete فقط با FullAccess صفحه 71 — گروه‌های 17، 27، 70 (و 33 QC بدون معادل)
		private bool CanManageAttachments => HasOpenOrderRequestPermission(Roles.SupplyAndPurchase, Roles.Supply, Roles.EngineeringAccept);

		// HTS 74·Terminate(101) با PermissionAuthorize (FullAccess هم شامل می‌شود)
		private bool CanTerminate => HasOpenOrderRequestPermission(Roles.Terminate, Roles.SupplyAndPurchase);

		// HTS صفحه 76 · SendEmail(27) — تصمیم WP1: نقش ConfigManage (+ FullAccess)
		private bool CanSendToSupplier => HasOpenOrderRequestPermission(Roles.ConfigManage, Roles.SupplyAndPurchase);

		// HTS AddComponyMansToRequests (صفحه 76) + واحد تدارکات (گروه 27 / Sup.OpenOrderRequest.Supply)
		private bool CanSetSelectedSupplier => HasOpenOrderRequestPermission(Roles.ConfigManage, Roles.Industrial, Roles.IndustriesLegacy, Roles.SupplyAndPurchase, Roles.Supply);

		#endregion

		/// <summary>
		/// D35: درخواست باز فقط از همگام‌سازی راهکاران ایجاد/ویرایش می‌شود (HTS روی صفحه 74 New/Edit فیلدی نداشت).
		/// ثبت/ویرایش دستی فقط برای ادمین باقی می‌ماند تا فراخوان‌های احتمالی نشکنند.
		/// </summary>
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(OpenOrderRequest openOrderRequest, CancellationToken cn)
		{
			if (!IsAdministrator)
				return AccessDenied("ثبت/ویرایش دستی درخواست باز مجاز نیست؛ درخواست‌ها فقط از راهکاران همگام می‌شوند");

			if (openOrderRequest.Id == null || openOrderRequest.Id == 0)
			{
				return await Add(openOrderRequest, cn);
			}
			var exist = await unitOfWork.Repository<OpenOrderRequest>().TableNoTracking.AnyAsync(c => c.Id == openOrderRequest.Id);
			if (exist)
			{
				return await Update(openOrderRequest, cn);
			}
			return await Add(openOrderRequest, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(OpenOrderRequest openOrderRequest, CancellationToken cn)
		{
			if (!IsAdministrator)
				return AccessDenied("ایجاد دستی درخواست باز مجاز نیست؛ درخواست‌ها فقط از راهکاران همگام می‌شوند");

			var entity = await unitOfWork.Repository<OpenOrderRequest>().SaveAsync(openOrderRequest, cn, true);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(OpenOrderRequest openOrderRequest, CancellationToken cn)
		{
			if (!IsAdministrator)
				return AccessDenied("ویرایش فیلدهای درخواست باز مجاز نیست؛ اطلاعات از راهکاران همگام می‌شود");

			var entity = await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, true);
			return Ok(entity);
		}

		/// <summary>
		/// D35: HTS حذف فیزیکی نداشت (فقط Terminate نرم). دکمه «حذف» پروفایل‌های داده به همین اکشن می‌آید،
		/// بنابراین به مسیر خاتمه نرم (IsDeleted + IsForceDeletedByUser + کامنت) هدایت می‌شود و همان نقش Terminate را می‌خواهد.
		/// </summary>
		[HttpGet("[action]")]
		[ActionDisplayName("حذف (خاتمه نرم)", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			if (!CanTerminate)
				return AccessDenied("شما دسترسی خاتمه/حذف درخواست باز را ندارید");

			var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
				.Table.FirstOrDefaultAsync(x => x.Id == id, cn);

			if (openOrderRequest == null)
				return BadRequest("درخواست باز یافت نشد");

			if (openOrderRequest.IsDeleted)
				return BadRequest("این درخواست قبلاً خاتمه یافته است");

			await SoftTerminateAsync(openOrderRequest, "حذف (خاتمه نرم)", cn);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			if (id != null && id != 0)
			{
				var entity = await unitOfWork.Repository<OpenOrderRequest>().TableNoTracking
					.Include(c => c.Dl)
					.Include(c => c.Part)
					.Include(c => c.EngineeringAcceptUser)
					.Include(c => c.SalesUnitConfirmationUser)
					.Include(c => c.ProductionOrder)
					.Include(c => c.SalesUnitSalesExpert)
					.Include(c => c.SalesUnitSalesManager)
					.Include(c => c.SalesUnitProjectManager)
					.Include(c => c.RelatedPart)
					.Include(c => c.ManCompany)
					.Include(c => c.Attachments!).ThenInclude(a => a.Attachment)
					.Include(c => c.Comments)
					.FirstOrDefaultAsync(c => c.Id == id, cn);

				ViewBag.AttachmentListItems = await BuildAttachmentListItemsAsync(id.Value, cn);
				return View(@"\Views\Panel\Sup\OpenOrderRequest\Edit.cshtml", entity);
			}
			ViewBag.AttachmentListItems = new List<OpenOrderRequestAttachmentListItemVm>();
			var newEntity = new OpenOrderRequest();
			return View(@"\Views\Panel\Sup\OpenOrderRequest\Edit.cshtml", newEntity);
		}

		/// <summary>D35: ایجاد دستی فقط برای ادمین؛ سایر کاربران به لیست هدایت می‌شوند (HTS روی صفحه 74 New نداشت).</summary>
		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			if (!IsAdministrator)
				return RedirectToAction(nameof(List));

			ViewBag.AttachmentListItems = new List<OpenOrderRequestAttachmentListItemVm>();
			var newEntity = new OpenOrderRequest();
			return View(@"\Views\Panel\Sup\OpenOrderRequest\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Sup\OpenOrderRequest\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<OpenOrderRequest>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
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
			return Ok(await unitOfWork.Repository<OpenOrderRequest>().FetchDataAsync(request, cn));
		}

		[HttpGet("[action]")]
		public IActionResult OpenOrderRequestAttachmentPartial()
		{
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_OpenOrderRequestAttachmentPartial.cshtml");
		}

		[HttpGet("[action]")]
		public IActionResult OpenOrderRequestCommentPartial()
		{
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_OpenOrderRequestCommentPartial.cshtml");
		}

		#region Comment Operations

		[HttpGet("[action]")]
		public IActionResult AddCommentPartial(long openOrderRequestId)
		{
			ViewBag.OpenOrderRequestId = openOrderRequestId;
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_AddCommentPartial.cshtml");
		}

		/// <summary>HTS: ویرایش (Edit 5) صفحه 74 → DoOperation(OperationType=Update) = ثبت کامنت.</summary>
		[HttpPost("[action]")]
		[ActionDisplayName("ثبت کامنت", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SaveComment(SaveCommentRequest request, CancellationToken cn)
		{
			try
			{
				if (!CanSaveComment)
					return AccessDenied("شما دسترسی ثبت کامنت روی درخواست باز را ندارید");

				var openOrderRequest = await unitOfWork
					.Repository<OpenOrderRequest>()
					.TableNoTracking
					.FirstOrDefaultAsync(x => x.Id == request.OpenOrderRequestId, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				// HTS btnSaveItem_click: فقط خالی بودن Requested_Personel مانع ثبت است
				var hasRequestedPersonel =
					(openOrderRequest.RequestedPersonelIds?.Any() ?? false) ||
					openOrderRequest.RequestedPersonel.HasValue();

				if (!hasRequestedPersonel)
					return BadRequest("امکان ثبت توضیحات میسر نیست لطفا درخواست کننده را وارد نمایید");

				var now = DateTime.Now;
				var comment = new OpenOrderRequestComment
				{
					OpenOrderRequestId = request.OpenOrderRequestId,
					ShamsiDate = request.CommentShamsiDate,
					MiladiDate = string.IsNullOrEmpty(request.CommentShamsiDate) ? null : request.CommentShamsiDate.ToMiladiDate(),
					CommentValue = request.CommentValue,
					CreatedOrgUnitId = CurrentOrganizationUnitId
				};

				await unitOfWork.Repository<OpenOrderRequestComment>().AddAsync(comment, cn);
				await unitOfWork.SaveChangesAsync(cn);

				// Create notification for requested personnel
				await CreateCommentNotification(openOrderRequest, comment, cn);

				return Ok(openOrderRequest);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در ذخیره کامنت: " + ex.Message);
			}
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> CommentListPartial(long openOrderRequestId, CancellationToken cn)
		{
			var comments = await unitOfWork.Repository<OpenOrderRequestComment>()
				.TableNoTracking
				.Include(c => c.StopOperator)
				.Where(c => c.OpenOrderRequestId == openOrderRequestId)
				.OrderByDescending(c => c.CreatedOnMiladiDateTime)
				.ToListAsync(cn);

			ViewBag.OpenOrderRequestId = openOrderRequestId;
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_CommentListPartial.cshtml", comments);
		}

		#endregion

		#region Engineering Accept

		/// <summary>HTS 74·Accept_Engineering(12) — HasAccessWithoutCheckFullAccess (FullAccess شامل نمی‌شود).</summary>
		[HttpPost("[action]/{id}")]
		[ActionDisplayName("تایید مهندسی", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> EngineeringAccept(long id, CancellationToken cn)
		{
			try
			{
				if (!HasOpenOrderRequestPermission(Roles.EngineeringAccept))
					return AccessDenied("شما دسترسی تایید مهندسی را ندارید");

				var (ok, error, entity) = await TryApplyEngineeringAcceptAsync(id, cn);
				if (!ok)
					return BadRequest(error);

				return Ok(entity);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در تایید مهندسی: " + ex.Message);
			}
		}

		/// <summary>
		/// تایید چند درخواست از نمایه «منتظر تایید مهندسی».
		/// همان نقش و همان قواعد EngineeringAccept؛ هر ردیف جدا ذخیره و اعلام می‌شود.
		/// </summary>
		[HttpPost("[action]")]
		[ActionDisplayName("تایید مهندسی گروهی", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> EngineeringAcceptBulk(EngineeringAcceptBulkRequest request, CancellationToken cn)
		{
			if (!HasOpenOrderRequestPermission(Roles.EngineeringAccept))
				return AccessDenied("شما دسترسی تایید مهندسی را ندارید");

			var ids = request?.Ids?
				.Where(id => id > 0)
				.Distinct()
				.Take(200)
				.ToList() ?? [];

			if (ids.Count == 0)
				return BadRequest("حداقل یک درخواست را انتخاب کنید");

			var result = new EngineeringAcceptBulkResult();
			foreach (var id in ids)
			{
				try
				{
					var (ok, error, _) = await TryApplyEngineeringAcceptAsync(id, cn);
					if (ok)
						result.AcceptedIds.Add(id);
					else
						result.Failed.Add(new EngineeringAcceptBulkFailure { Id = id, Message = error ?? "تایید انجام نشد" });
				}
				catch (Exception ex)
				{
					result.Failed.Add(new EngineeringAcceptBulkFailure { Id = id, Message = ex.Message });
				}
			}

			result.AcceptedCount = result.AcceptedIds.Count;
			return Ok(result);
		}

		/// <summary>Seed Role => Sup.OpenOrderRequest.EngineeringAccept => تامین و خرید - درخواست های باز - تایید مهندسی</summary>
		private async Task<(bool Ok, string? Error, OpenOrderRequest? Entity)> TryApplyEngineeringAcceptAsync(long id, CancellationToken cn)
		{
			var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
				.Table
				.Include(o => o.Part)
				.FirstOrDefaultAsync(x => x.Id == id, cn);

			if (openOrderRequest == null)
				return (false, "درخواست باز یافت نشد", null);

			if (openOrderRequest.EngineeringAccept)
				return (false, "این درخواست قبلاً تایید مهندسی شده است", null);

			if (openOrderRequest.IsStop)
				return (false, "درخواست متوقف شده قابل تایید نیست", null);

			var now = DateTime.Now;
			var currentUserId = CurrentUserId;

			openOrderRequest.EngineeringAccept = true;
			openOrderRequest.EngineeringAcceptUserId = currentUserId;
			openOrderRequest.EngineeringAcceptShamsiDateTime = now.ToShamsiDateTime();
			openOrderRequest.EngineeringConfirmationMiladiDateTime = now;

			var leadTime = await unitOfWork.Repository<LeadTime>().TableNoTracking
				.OrderByDescending(c => c.CreatedOnMiladiDateTime)
				.Select(c => new { c.LeadTimeDay, c.PartId, c.Id })
				.FirstOrDefaultAsync(c => c.PartId == openOrderRequest.PartId);

			if (leadTime != null)
			{
				openOrderRequest.SupplyMiladiDate = now.AddDays(leadTime.LeadTimeDay);
				openOrderRequest.SupplyShamsiDate = openOrderRequest.SupplyMiladiDate.Value.ToShamsiDate();
			}

			await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, true);
			await CreateEngineeringAcceptNotification(openOrderRequest.Id, false, cn);
			return (true, null, openOrderRequest);
		}

		#endregion

		#region Sales/Project Accept

		[HttpGet("[action]")]
		public IActionResult SalesOrProjectAcceptPartial(long openOrderRequestId)
		{
			ViewBag.OpenOrderRequestId = openOrderRequestId;
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_SalesOrProjectAcceptPartial.cshtml");
		}

		/// <summary>HTS 74·SalesOrProjectPermission(187) — HasAccessWithoutCheckFullAccess + تطبیق هویت (D20).</summary>
		[HttpPost("[action]")]
		[ActionDisplayName("تایید فروش/پروژه", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SalesOrProjectAccept(SalesOrProjectAcceptRequest request, CancellationToken cn)
		{
			try
			{
				//Seed Role => Sup.OpenOrderRequest.SalesOrProjectAccept => تامین و خرید -درخواست های باز - تایید فروش/پروژه 
				//برسی داشتن دسترسی برای تایید فروش/پروژه 
				if (!HasOpenOrderRequestPermission(Roles.SalesOrProjectAccept))
					return AccessDenied("شما دسترسی تایید فروش/پروژه را ندارید");

				var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
					.Table.FirstOrDefaultAsync(x => x.Id == request.OpenOrderRequestId, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				// D20 (هویت) — HTS: validSalesOrProjectUserIds.any(p => p === userId) || userIsAdmin
				if (!IsAdministrator)
				{
					var validSalesOrProjectUserIds = new[]
					{
						openOrderRequest.SalesUnitSalesExpertId,
						openOrderRequest.SalesUnitSalesManagerId,
						openOrderRequest.SalesUnitProjectManagerId
					};

					if (!validSalesOrProjectUserIds.Any(u => u.HasValue && u == CurrentUserId))
						return AccessDenied("فقط کارشناس فروش، مدیر فروش یا مدیر پروژه همین درخواست می‌تواند تایید فروش/پروژه را ثبت کند");
				}

				if (!openOrderRequest.EngineeringAccept)
					return BadRequest("ابتدا باید تایید مهندسی انجام شود");

				if (openOrderRequest.IsAcceptedAutomaticallyByEngineering)
					return BadRequest("این درخواست به صورت خودکار تایید مهندسی شده و نیاز به تایید واحد فروش/پروژه ندارد");

				if (openOrderRequest.HasSalesUnitConfirmation)
					return BadRequest("این درخواست قبلاً تایید شده است");

				// D20 — HTS: تایید فروش/پروژه فقط پس از لینک VPIS واجد شرط نمایش/افزودن
				var latestVpisLinks = await unitOfWork.Repository<OpenOrderRequestVpis>()
					.TableNoTracking
					.Include(v => v.ProjectVpis)
						.ThenInclude(pv => pv.ProjectName)
					.Include(v => v.Project)
					.Include(v => v.Document)
						.ThenInclude(d => d!.Comments)
					.Where(v => v.OpenOrderRequestId == request.OpenOrderRequestId && v.IsLatest)
					.ToListAsync(cn);
				var hasVpisLink = latestVpisLinks.Any(v =>
					OpenOrderRequestVpisRules.IsEligibleForOpenOrderLink(v.Document, v.ProjectVpis, v.Project));
				if (!hasVpisLink)
					return BadRequest("ابتدا مدارک پروژه (VPIS) واجد شرط را لینک کنید");

				var now = DateTime.Now;
				var currentUserId = CurrentUserId;

				openOrderRequest.HasSalesUnitConfirmation = true;
				openOrderRequest.SalesUnitConfirmationUserId = currentUserId;
				openOrderRequest.SalesUnitConfirmationMiladiDateTime = now;
				openOrderRequest.SalesUnitConfirmationShamsiDateTime = now.ToShamsiDateTime();
				openOrderRequest.SalesUnitConfirmationComment = request.SalesUnitConfirmationComment;

				await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, true);

				// HTS: Comment_Value = توضیح کاربر + عنوان و رویژن مدارک VPIS تاییدشده
				var approvedDocuments = string.Join(" | ", latestVpisLinks.Select(v =>
				{
					var title = v.ProjectVpis?.Title?.Trim();
					var revision = v.RevisionNumber?.ToString();
					if (!title.HasValue())
						return revision.HasValue() ? $"Rev({revision})" : null;
					return revision.HasValue() ? $"{title} Rev({revision})" : title;
				}).Where(s => s.HasValue()));

				var salesCommentValue = request.SalesUnitConfirmationComment;
				if (approvedDocuments.HasValue())
					salesCommentValue = salesCommentValue.HasValue()
						? salesCommentValue + " | " + approvedDocuments
						: approvedDocuments;

				var comment = new OpenOrderRequestComment
				{
					OpenOrderRequestId = request.OpenOrderRequestId,
					ShamsiDate = now.ToShamsiDate(),
					MiladiDate = now,
					HasSalesUnitConfirmation = true,
					SalesUnitConfirmationComment = "تایید درخواست باز توسط واحد پروژه/فروش",
					CommentValue = salesCommentValue,
					CreatedOrgUnitId = CurrentOrganizationUnitId
				};

				await unitOfWork.Repository<OpenOrderRequestComment>().AddAsync(comment, cn);
				await unitOfWork.SaveChangesAsync(cn);

				await CreateEngineeringAcceptNotification(openOrderRequest.Id, true, cn);

				return Ok(openOrderRequest);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در تایید: " + ex.Message);
			}
		}

		#endregion

		#region Stop Operations

		[HttpGet("[action]")]
		public async Task<IActionResult> StopRequestPartial(long openOrderRequestId, CancellationToken cn)
		{
			var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
				.TableNoTracking
				.FirstOrDefaultAsync(x => x.Id == openOrderRequestId, cn);

			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_StopRequestPartial.cshtml", openOrderRequest);
		}

		/// <summary>
		/// HTS: اگر IsStop نباشد دکمه توقف روشن است (JS صفحه 74). ثبت اولیه (0/2815) با Stop(22)؛
		/// مراحل بعدی در UI فقط عامل توقف (2807/2813/2816) یا ثبت‌کنندهٔ آخرین کامنت توقف (2808/2810/2811/2812/2821) — ادمین همه.
		/// </summary>
		[HttpPost("[action]")]
		[ActionDisplayName("ثبت / بررسی توقف", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> StopOperation(StopOperationRequest request, CancellationToken cn)
		{
			try
			{
				var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
					.Table
					.Include(o => o.Part)
					.Include(o => o.Comments)
					.FirstOrDefaultAsync(x => x.Id == request.OpenOrderRequestId, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				var stopAccessError = CheckStopOperationAccess(openOrderRequest);
				if (stopAccessError != null)
					return AccessDenied(stopAccessError);

				// Validation
				var validationError = ValidateStopRequest(request, (int?)openOrderRequest.StopStatus ?? 0);
				if (!string.IsNullOrEmpty(validationError))
					return BadRequest(validationError);

				var now = DateTime.Now;
				var currentUserId = CurrentUserId;
				var currentStopStatus = (int?)openOrderRequest.StopStatus ?? 0;

				OpenOrderRequestComment comment;
				OpenOrderRequestStopStatusEnum nextStopStatus;

				// تعیین مرحله بعدی بر اساس وضعیت فعلی
				switch (currentStopStatus)
				{
					case 0: // Initial Stop
					case 2815: // Restart
						nextStopStatus = OpenOrderRequestStopStatusEnum.InitialSubmissionAndSendToStopBoard;
						
						comment = new OpenOrderRequestComment
						{
							OpenOrderRequestId = request.OpenOrderRequestId,
							ShamsiDate = now.ToShamsiDate(),
							MiladiDate = now,
							CommentValue = request.CommentValue,
							IsStop = true,
							StopType = request.StopTypeEnum.HasValue ? (OpenOrderRequestStopTypeEnum)request.StopTypeEnum.Value : null,
							StopOperatorId = request.StopOperatorId,
							BeneficiariesIds = request.BeneficiariesIds,
							AttachmentId = request.AttachmentId,
							ResponseDeadlineMiladiDate = now.AddDays(openOrderRequest.IsRoutineRequest ? 2 : 3),
							CreatedOrgUnitId = CurrentOrganizationUnitId,
						};
						comment.ResponseDeadlineShamsiDate = comment.ResponseDeadlineMiladiDate.Value.ToShamsiDate();

						openOrderRequest.IsStop = true;
						openOrderRequest.StopStatus = nextStopStatus;
						openOrderRequest.StopShamsiDate = now.ToShamsiDate();
						openOrderRequest.StopMiladiDate = now;

						await unitOfWork.Repository<OpenOrderRequestComment>().AddAsync(comment, cn);

						await CreateStopInitNotification(openOrderRequest, comment, cn);


						break;

					case 2807: // Step1_InitialAndSendToResponsible
					case 2813: // Step6_WaitingForProjectManager
					case 2816: // Step8_RegisterStopAgain
						// Operator Review
						nextStopStatus = DetermineNextStopStatusFromCheckingResult(request.StopCheckingResult);

						var lastComment = openOrderRequest.Comments
							.Where(c => c.IsStop)
							.OrderByDescending(c => c.Id)
							.FirstOrDefault();

						if (lastComment != null)
						{
							if (currentStopStatus == 2813) // If waiting for project manager, create new comment
							{
								comment = new OpenOrderRequestComment
								{
									OpenOrderRequestId = request.OpenOrderRequestId,
									ShamsiDate = now.ToShamsiDate(),
									MiladiDate = now,
									CommentValue = "ادامه روند توقف پس از اعمال نظر مدیر پروژه / کارفرما",
									IsStop = true,
									StopType = lastComment.StopType,
									StopOperatorId = lastComment.StopOperatorId,
									BeneficiariesIds = lastComment.BeneficiariesIds,
									StopCheckingResult = request.StopCheckingResult,
									StopCheckingMiladiDateTime = now,
									StopCheckingShamsiDateTime = now.ToShamsiDateTime(),
									StopCheckingComment = request.StopCheckingComment,
									StopCheckingDelayReasonText = request.StopCheckingDelayReasonText,
									IsNeedToUpdateBom = request.IsNeedToUpdateBom,
									IsNeedToDeleteBom = request.IsNeedToDeleteBom,
									ProjectManagerApproximateCommentShamsiDate = request.ProjectManagerApproximateCommentShamsiDate,
									ProjectManagerApproximateCommentMiladiDate = string.IsNullOrEmpty(request.ProjectManagerApproximateCommentShamsiDate) 
										? null : request.ProjectManagerApproximateCommentShamsiDate.ToMiladiDate(),
									AlternativeAttachmentId = request.AlternativeAttachmentId,
									CreatedOrgUnitId = CurrentOrganizationUnitId
								};

								await unitOfWork.Repository<OpenOrderRequestComment>().AddAsync(comment, cn);
							}
							else
							{
								lastComment.StopCheckingResult = request.StopCheckingResult;
								lastComment.StopCheckingMiladiDateTime = now;
								lastComment.StopCheckingShamsiDateTime = now.ToShamsiDateTime();
								lastComment.StopCheckingComment = request.StopCheckingComment;
								lastComment.StopCheckingDelayReasonText = request.StopCheckingDelayReasonText;
								lastComment.IsNeedToUpdateBom = request.IsNeedToUpdateBom;
								lastComment.IsNeedToDeleteBom = request.IsNeedToDeleteBom;
								lastComment.ProjectManagerApproximateCommentShamsiDate = request.ProjectManagerApproximateCommentShamsiDate;
								lastComment.ProjectManagerApproximateCommentMiladiDate = string.IsNullOrEmpty(request.ProjectManagerApproximateCommentShamsiDate)
									? null : request.ProjectManagerApproximateCommentShamsiDate.ToMiladiDate();
								lastComment.AlternativeAttachmentId = request.AlternativeAttachmentId;

								await unitOfWork.Repository<OpenOrderRequestComment>().UpdateAsync(lastComment, cn, false);
								comment = lastComment;
							}
						}
						else
						{
							return BadRequest("کامنت توقف یافت نشد");
						}

						// HTS DoStopOperation روی ادامهٔ گردش توقف IsStop را true می‌کند
						// (راه‌اندازی جداگانه فقط IsStop را خاموش می‌کند و StopStatus را عوض نمی‌کند).
						openOrderRequest.IsStop = true;
						openOrderRequest.StopStatus = nextStopStatus;
						await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, true);

						// Send notification
						await CreateStopOperatorReviewNotification(openOrderRequest, comment, cn);

						break;

					case 2808: // Step2_NeedToRestartRequest
					case 2810: // Step3_NeedToEdit
					case 2811: // Step4_NeedToEditDocuments
					case 2812: // Step5_DeleteIndustrialRequest
					case 2821: // Step10_SendToSupplyCartable
						// Requester Decision
						nextStopStatus = DetermineNextStopStatusFromCheckingStatus(request.StopCheckingStatus);

						var currentComment = openOrderRequest.Comments
							.Where(c => c.IsStop)
							.OrderByDescending(c => c.Id)
							.FirstOrDefault();

						if (currentComment == null)
							return BadRequest("کامنت توقف یافت نشد");

						if (nextStopStatus == OpenOrderRequestStopStatusEnum.CheckStopFactorReRegisterStop)
						{
							// Create new stop comment
							comment = new OpenOrderRequestComment
							{
								OpenOrderRequestId = request.OpenOrderRequestId,
								ShamsiDate = now.ToShamsiDate(),
								MiladiDate = now,
								CommentValue = AppendReStopSuffix(request.ReStopCommentValue ?? request.CommentValue),
								IsStop = true,
								StopType = request.ReStopTypeEnum.HasValue ? (OpenOrderRequestStopTypeEnum)request.ReStopTypeEnum.Value : null,
								StopOperatorId = request.ReStopOperatorId ?? request.StopOperatorId,
								BeneficiariesIds = currentComment.BeneficiariesIds,
								StopCheckingStatus = request.StopCheckingStatus,
								StopCheckingStatusComment = request.StopCheckingStatusComment,
								AttachmentId = request.AttachmentId,
								CreatedOrgUnitId = CurrentOrganizationUnitId
							};

							await unitOfWork.Repository<OpenOrderRequestComment>().AddAsync(comment, cn);
						}
						else
						{
							currentComment.StopCheckingStatus = request.StopCheckingStatus;
							currentComment.StopCheckingStatusComment = request.StopCheckingStatusComment;
							await unitOfWork.Repository<OpenOrderRequestComment>().UpdateAsync(currentComment, cn, false);
							comment = currentComment;
						}

						if (nextStopStatus == OpenOrderRequestStopStatusEnum.RestartStopCauseCheck)
						{
							// Restart request
							openOrderRequest.IsStop = false;
							
							var restartComment = new OpenOrderRequestComment
							{
								OpenOrderRequestId = request.OpenOrderRequestId,
								ShamsiDate = now.ToShamsiDate(),
								MiladiDate = now,
								CommentValue = $"درخواست توسط {CurrentUserFullNameFn ?? CurrentUserFullName ?? "کاربر"} راه اندازی شد",
								IsStop = false,
								IsLaunched = true,
								CreatedOrgUnitId = CurrentOrganizationUnitId
							};
							await unitOfWork.Repository<OpenOrderRequestComment>().AddAsync(restartComment, cn);
						}
						else
						{
							// HTS ادامهٔ گردش توقف را دوباره متوقف می‌کند
							openOrderRequest.IsStop = true;
						}

						openOrderRequest.StopStatus = nextStopStatus;
						await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, true);

						await CreateStopRequesterDecisionNotification(openOrderRequest, comment, cn);

						break;

					default:
						return BadRequest("وضعیت توقف نامعتبر است");
				}

				await unitOfWork.SaveChangesAsync(cn);

				if (comment.StopOperatorId is > 0
					&& openOrderRequest.StopStatus is OpenOrderRequestStopStatusEnum.InitialSubmissionAndSendToStopBoard
						or OpenOrderRequestStopStatusEnum.CheckStopFactorReRegisterStop)
					await GrantOpenOrderRequestViewRoleAsync(comment.StopOperatorId.Value, cn);

				return Ok(openOrderRequest);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در ثبت توقف: " + ex.Message);
			}
		}

		/// <summary>HTS UpdatePermissions(OpenOrderRequestView=442): عامل توقف عضو گروه مشاهده می‌شود.</summary>
		private async Task GrantOpenOrderRequestViewRoleAsync(long userId, CancellationToken cn)
		{
			try
			{
				var role = await userService.Context.Set<Role>().AsNoTracking()
					.FirstOrDefaultAsync(r => r.Name == Roles.View, cn);
				if (role?.Id is null or 0)
					return;

				var user = await userService.GetById(userId);
				if (user == null)
					return;

				user.RoleIds ??= [];
				user.Roles ??= [];
				if (user.RoleIds.Contains(role.Id.Value) || user.Roles.Contains(Roles.View, StringComparer.OrdinalIgnoreCase))
					return;

				user.RoleIds.Add(role.Id.Value);
				user.Roles.Add(Roles.View);
				await userService.AddAndUpdateUserAsync(user, cn);

				try { await onlineUserService.RefreshUserRoleAccessesAsync(userId, cn); }
				catch { /* نقش ذخیره شده؛ کش با ورود بعدی تازه می‌شود */ }
			}
			catch
			{
				// شکست انتساب نقش، ثبت توقف را برنمی‌گرداند
			}
		}

		/// <summary>
		/// قاعده دسترسی مراحل توقف (معادل JS صفحه 74 HTS).
		/// اگر درخواست متوقف نباشد دکمه برای کاربر صفحه روشن است و سرور هم رد نمی‌کند.
		/// null = مجاز؛ در غیر این صورت پیام خطا.
		/// </summary>
		private string? CheckStopOperationAccess(OpenOrderRequest openOrderRequest)
		{
			if (IsAdministrator)
				return null;

			// HTS: else if (selectedItem.IsStop === false) btnStop.enable(true)
			if (!openOrderRequest.IsStop)
				return null;

			var hasStopRole = CurrentUserHasAnyRole(Roles.Stop, Roles.SupplyAndPurchase);
			var currentStopStatus = (int?)openOrderRequest.StopStatus ?? 0;
			var lastStopComment = openOrderRequest.Comments?
				.Where(c => c.IsStop)
				.OrderByDescending(c => c.Id)
				.FirstOrDefault();

			switch (currentStopStatus)
			{
				case 0:     // ثبت اولیه
				case 2815:  // راه‌اندازی مجدد → ثبت توقف جدید
					return hasStopRole ? null : "شما دسترسی ثبت توقف را ندارید";

				case 2807:  // در کارتابل عامل توقف
				case 2813:  // در انتظار مدیر پروژه / کارفرما
				case 2816:  // ثبت توقف مجدد
					if (lastStopComment?.StopOperatorId.HasValue == true && lastStopComment.StopOperatorId == CurrentUserId)
						return null;
					return "فقط عامل توقف این درخواست می‌تواند نتیجه بررسی را ثبت کند";

				case 2808:  // نیاز به راه‌اندازی مجدد
				case 2810:  // نیاز به اصلاح BOM
				case 2811:  // نیاز به اصلاح مدارک / شرح کالا
				case 2812:  // حذف درخواست مرتبط با صنایع
				case 2821:  // ارسال خودکار به کارتابل تدارکات
					if (lastStopComment?.CreatedById.HasValue == true && lastStopComment.CreatedById == CurrentUserId)
						return null;
					return "فقط ثبت‌کننده توقف (آخرین کامنت توقف) می‌تواند تصمیم نهایی را ثبت کند";

				default:
					return hasStopRole ? null : "شما دسترسی عملیات توقف را ندارید";
			}
		}

		#endregion

		#region Triggering (Start)

		/// <summary>HTS 74·Start(23) — HasStopStartPermissions(isStop:false).</summary>
		[HttpPost("[action]/{id}")]
		[ActionDisplayName("راه‌اندازی", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Triggering(long id, CancellationToken cn)
		{
			try
			{
				if (!HasOpenOrderRequestPermission(Roles.Start, Roles.SupplyAndPurchase))
					return AccessDenied("شما دسترسی راه‌اندازی درخواست را ندارید");

				var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
					.Table.FirstOrDefaultAsync(x => x.Id == id, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				var now = DateTime.Now;
				
				openOrderRequest.IsStop = false;
				openOrderRequest.Status = "راه اندازی شده";

				await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, false);

				// ثبت کامنت
				var comment = new OpenOrderRequestComment
				{
					OpenOrderRequestId = id,
					ShamsiDate = now.ToShamsiDate(),
					MiladiDate = now,
					CommentValue = "درخواست راه اندازی شد",
					IsStop = false,
					IsLaunched = true,
					CreatedOrgUnitId = CurrentOrganizationUnitId
				};

				await unitOfWork.Repository<OpenOrderRequestComment>().AddAsync(comment, cn);
				await unitOfWork.SaveChangesAsync(cn);

				await CreateTriggeringNotification(openOrderRequest, cn);

				return Ok(openOrderRequest);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در راه‌اندازی: " + ex.Message);
			}
		}

		#endregion

		#region InWay and StatusInquiry

		/// <summary>HTS 74·Sending(24) — HasStatusPermissions(isSending:true)؛ در HTS عملاً فقط FullAccess (گروه 17).</summary>
		[HttpPost("[action]/{id}")]
		[ActionDisplayName("در راه", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SetInWayStatus(long id, CancellationToken cn)
		{
			try
			{
				if (!HasOpenOrderRequestPermission(Roles.Sending, Roles.SupplyAndPurchase))
					return AccessDenied("شما دسترسی تغییر وضعیت به «در راه» را ندارید");

				var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
					.Table.FirstOrDefaultAsync(x => x.Id == id, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				openOrderRequest.Status = "در راه";

				await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, true);

				return Ok(openOrderRequest);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در تغییر وضعیت: " + ex.Message);
			}
		}

		/// <summary>HTS 74·Query(25) — HasStatusPermissions(isSending:false)؛ در HTS عملاً فقط FullAccess (گروه 17).</summary>
		[HttpPost("[action]/{id}")]
		[ActionDisplayName("درحال استعلام", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> StatusInquiry(long id, CancellationToken cn)
		{
			try
			{
				if (!HasOpenOrderRequestPermission(Roles.Query, Roles.SupplyAndPurchase))
					return AccessDenied("شما دسترسی تغییر وضعیت به «در حال استعلام» را ندارید");

				var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
					.Table.FirstOrDefaultAsync(x => x.Id == id, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				openOrderRequest.Status = "در حال استعلام";

				await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, true);

				return Ok(openOrderRequest);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در استعلام: " + ex.Message);
			}
		}

		#endregion

		#region Terminate

		/// <summary>HTS 74·Terminate(101) — PermissionAuthorize(Supp, 74, Terminate) (FullAccess هم مجاز).</summary>
		[HttpPost("[action]/{id}")]
		[ActionDisplayName("خاتمه دادن", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Terminate(long id, CancellationToken cn)
		{
			try
			{
				if (!CanTerminate)
					return AccessDenied("شما دسترسی خاتمه درخواست را ندارید");

				var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
					.Table.FirstOrDefaultAsync(x => x.Id == id, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				if (openOrderRequest.IsDeleted)
					return BadRequest("این درخواست قبلاً خاتمه یافته است");

				await SoftTerminateAsync(openOrderRequest, "خاتمه توسط کاربر", cn);

				return Ok(openOrderRequest);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در خاتمه: " + ex.Message);
			}
		}

		/// <summary>
		/// خاتمه نرم (معادل DoSetTerminateOperation در HTS): IsDeleted + IsForceDeletedByUser + کامنت اختتام.
		/// مشترک بین Terminate و Delete (D35 — حذف فیزیکی وجود ندارد).
		/// </summary>
		private async Task SoftTerminateAsync(OpenOrderRequest openOrderRequest, string reason, CancellationToken cn)
		{
			var now = DateTime.Now;

			openOrderRequest.IsForceDeletedByUser = true;
			openOrderRequest.IsDeleted = true;
			openOrderRequest.IsDeletedMiladiDate = now;
			openOrderRequest.IsDeletedShamsiDate = now.ToShamsiDateTime();
			openOrderRequest.Comment += " || " + reason;

			await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, false);

			// ثبت کامنت خاتمه
			var comment = new OpenOrderRequestComment
			{
				OpenOrderRequestId = openOrderRequest.Id!.Value,
				ShamsiDate = now.ToShamsiDate(),
				MiladiDate = now,
				CommentValue = $"اختتام یافته در Portal توسط {CurrentUserFullName ?? CurrentUserName ?? CurrentUserId?.ToString()}",
				CreatedOrgUnitId = CurrentOrganizationUnitId
			};

			await unitOfWork.Repository<OpenOrderRequestComment>().AddAsync(comment, cn);
			await unitOfWork.SaveChangesAsync(cn);
		}

		#endregion

		#region Attachment Operations

		[HttpGet("[action]")]
		public async Task<IActionResult> AttachmentPartial(long openOrderRequestId, long? id, CancellationToken cn)
		{
			OpenOrderRequestAttachment? model = null;
			
			if (id.HasValue && id.Value > 0)
			{
				model = await unitOfWork.Repository<OpenOrderRequestAttachment>()
					.TableNoTracking
					.Include(a => a.Attachment)
					.FirstOrDefaultAsync(x => x.Id == id, cn);
			}

			ViewBag.OpenOrderRequestId = openOrderRequestId;
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_AttachmentPartial.cshtml", model);
		}

		/// <summary>HTS صفحه 71 «پیوست درخواست های باز» — DoAttachmentOperation (New/Edit فقط با FullAccess صفحه 71).</summary>
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره پیوست", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SaveAttachment(SaveAttachmentRequest request, CancellationToken cn)
		{
			try
			{
				if (!CanManageAttachments)
					return AccessDenied("شما دسترسی ثبت/ویرایش پیوست درخواست باز را ندارید");

				var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
					.Table.FirstOrDefaultAsync(x => x.Id == request.OpenOrderRequestId, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				OpenOrderRequestAttachment attachment;
				var isNewUpload = !(request.Id is > 0);

				if (request.Id.HasValue && request.Id.Value > 0)
				{
					attachment = await unitOfWork.Repository<OpenOrderRequestAttachment>()
						.Table.FirstOrDefaultAsync(x => x.Id == request.Id, cn);
					
					if (attachment == null)
						return BadRequest("پیوست یافت نشد");

					attachment.FileType = request.FileType;
					attachment.Comment = request.Comment;
					attachment.AttachmentId = request.AttachmentId;

					await unitOfWork.Repository<OpenOrderRequestAttachment>().UpdateAsync(attachment, cn, true);
				}
				else
				{
					attachment = new OpenOrderRequestAttachment
					{
						OpenOrderRequestId = request.OpenOrderRequestId,
						FileType = request.FileType,
						Comment = request.Comment,
						AttachmentId = request.AttachmentId
					};

					await unitOfWork.Repository<OpenOrderRequestAttachment>().AddAsync(attachment, cn);
					await unitOfWork.SaveChangesAsync(cn);
				}

				openOrderRequest.Changed = true;
				await unitOfWork.Repository<OpenOrderRequest>().UpdateAsync(openOrderRequest, cn, true);

				if (isNewUpload)
					await SendUploadedAttachmentEmailAsync(openOrderRequest, cn);

				// HTS: DocumentTypeId در {264 Data Sheet, 271 Wiring Diagram, 307 Nameplate}
				var productionDocumentTypes = new[]
				{
					OpenOrderRequestAttachmentFileTypeEnum.DataSheet,
					OpenOrderRequestAttachmentFileTypeEnum.WiringDiagram,
					OpenOrderRequestAttachmentFileTypeEnum.Nameplate
				};

				if (request.FileType.HasValue && productionDocumentTypes.Contains(request.FileType.Value))
					await CreateProductionAttachmentEmailAsync(openOrderRequest, cn);

				return Ok(attachment);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در ذخیره پیوست: " + ex.Message);
			}
		}

		/// <summary>HTS صفحه 71 Read/ViewAttachment — همه نقش‌های دارای دید صفحه 74/77.</summary>
		[HttpGet("[action]")]
		[ActionDisplayName("لیست پیوست‌ها", ActionAccessType.View, ActionAccessItemType.Custom)]
		public async Task<IActionResult> AttachmentListPartial(long openOrderRequestId, CancellationToken cn)
		{
			var items = await BuildAttachmentListItemsAsync(openOrderRequestId, cn);
			ViewBag.OpenOrderRequestId = openOrderRequestId;
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_AttachmentListPartial.cshtml", items);
		}

		private async Task<List<OpenOrderRequestAttachmentListItemVm>> BuildAttachmentListItemsAsync(long openOrderRequestId, CancellationToken cn)
		{
			var items = new List<OpenOrderRequestAttachmentListItemVm>();
			var canDeleteAttachment = CanManageAttachments;

			var attachments = await unitOfWork.Repository<OpenOrderRequestAttachment>()
				.TableNoTracking
				.Include(a => a.Attachment)
				.Where(a => a.OpenOrderRequestId == openOrderRequestId)
				.ToListAsync(cn);

			foreach (var attachment in attachments)
			{
				items.Add(new OpenOrderRequestAttachmentListItemVm
				{
					Id = attachment.Id!.Value,
					IsFromVpis = false,
					FileTypeDisplay = attachment.FileType?.ToDisplay(),
					FileName = attachment.Attachment?.OriginalName,
					DownloadFileId = attachment.AttachmentId,
					SizeDisplay = attachment.Attachment != null
						? (attachment.Attachment.Size / 1024.0).ToString("N2") + " KB"
						: "-",
					Comment = attachment.Comment,
					CreatedByName = attachment.CreatedByName,
					CreatedOnShamsiDateTime = attachment.CreatedOnShamsiDateTime,
					CreatedOnMiladiDateTime = attachment.CreatedOnMiladiDateTime,
					CanDelete = canDeleteAttachment
				});
			}

			var partId = await unitOfWork.Repository<OpenOrderRequest>().TableNoTracking
				.Where(o => o.Id == openOrderRequestId)
				.Select(o => o.PartId)
				.FirstOrDefaultAsync(cn);

			if (partId > 0)
			{
				var partDocTypes = new[]
				{
					PartDocumentTypeEnum.Data_Sheet,
					PartDocumentTypeEnum.Detail_Drawing,
					PartDocumentTypeEnum.Wiring_Diagram,
					PartDocumentTypeEnum.Technical_Documents
				};
				var partDocs = await unitOfWork.Repository<PartDocument>().TableNoTracking
					.Include(d => d.Attachment)
					.Where(d => d.PartId == partId
						&& d.Main == true
						&& d.AttachmentId > 0
						&& d.Type != null
						&& partDocTypes.Contains(d.Type.Value))
					.ToListAsync(cn);

				foreach (var doc in partDocs
					.GroupBy(d => d.Type)
					.Select(g => g.OrderByDescending(x => x.Id).First()))
				{
					items.Add(new OpenOrderRequestAttachmentListItemVm
					{
						Id = doc.Id!.Value,
						IsFromVpis = false,
						FileTypeDisplay = doc.Type?.ToDisplay() ?? "پیوست کالا",
						FileName = doc.Attachment?.OriginalName,
						DownloadFileId = doc.AttachmentId,
						SizeDisplay = doc.Attachment != null
							? (doc.Attachment.Size / 1024.0).ToString("N2") + " KB"
							: "-",
						Comment = doc.Comment,
						CreatedByName = doc.CreatedByName,
						CreatedOnShamsiDateTime = doc.CreatedOnShamsiDateTime,
						CreatedOnMiladiDateTime = doc.CreatedOnMiladiDateTime,
						CanDelete = false
					});
				}
			}

			var vpisLinks = await unitOfWork.Repository<OpenOrderRequestVpis>()
				.TableNoTracking
				.Include(v => v.ProjectVpis)
					.ThenInclude(pv => pv.ProjectName)
				.Include(v => v.Project)
				.Include(v => v.Document)
					.ThenInclude(d => d!.MainFile)
				.Include(v => v.Document)
					.ThenInclude(d => d!.Comments)
				.Where(v => v.OpenOrderRequestId == openOrderRequestId && v.IsLatest)
				.ToListAsync(cn);

			foreach (var link in vpisLinks)
			{
				var vpis = link.ProjectVpis;
				var document = link.Document;
				if (!OpenOrderRequestVpisRules.IsEligibleForOpenOrderLink(document, vpis, link.Project))
					continue;
				var fullTitle = $"{vpis?.ProjectName?.ProjectName} | {vpis?.Title} | {vpis?.Code} | Ver({document?.Revision ?? link.RevisionNumber})";
				var mainFile = document?.MainFile;

				items.Add(new OpenOrderRequestAttachmentListItemVm
				{
					Id = link.Id!.Value,
					IsFromVpis = true,
					FileTypeDisplay = "مدارک پروژه (VPIS)",
					FileName = fullTitle,
					DownloadFileId = document?.MainFileId,
					SizeDisplay = mainFile != null
						? (mainFile.Size / 1024.0).ToString("N2") + " KB"
						: "-",
					Comment = link.Comment,
					CreatedByName = link.CreatedByName,
					CreatedOnShamsiDateTime = link.CreatedOnShamsiDateTime,
					CreatedOnMiladiDateTime = link.CreatedOnMiladiDateTime,
					CanDelete = false
				});
			}

			return items
				.OrderByDescending(i => i.CreatedOnMiladiDateTime)
				.ToList();
		}

		/// <summary>HTS صفحه 71 ViewAttachment — ViewAttachedFile.</summary>
		[HttpGet("[action]")]
		[ActionDisplayName("دانلود پیوست", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> DownloadAttachment(long id, CancellationToken cn)
		{
			var attachment = await unitOfWork.Repository<OpenOrderRequestAttachment>()
				.TableNoTracking
				.Include(a => a.Attachment)
				.FirstOrDefaultAsync(x => x.Id == id, cn);

			if (attachment?.Attachment == null)
				return NotFound("فایل یافت نشد");

			var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", attachment.Attachment.PhysicalPath ?? "");
			
			if (!System.IO.File.Exists(filePath))
				return NotFound("فایل در مسیر ذخیره‌سازی یافت نشد");

			var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath, cn);
			return File(fileBytes, "application/octet-stream", attachment.Attachment.OriginalName);
		}

		/// <summary>HTS صفحه 71 Delete — DoAttachmentOperation(Delete) فقط با FullAccess صفحه 71.</summary>
		[HttpPost("[action]")]
		[ActionDisplayName("حذف پیوست", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> DeleteAttachment(long id, CancellationToken cn)
		{
			try
			{
				if (!CanManageAttachments)
					return AccessDenied("شما دسترسی حذف پیوست درخواست باز را ندارید");

				var attachment = await unitOfWork.Repository<OpenOrderRequestAttachment>()
					.Table
					.Include(a => a.Attachment)
					.FirstOrDefaultAsync(x => x.Id == id, cn);

				if (attachment == null)
					return BadRequest("پیوست یافت نشد");

				await unitOfWork.Repository<OpenOrderRequestAttachment>().DeleteAsync(attachment, cn, true);

				// TODO: Delete physical file if needed
				
				return Ok();
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در حذف پیوست: " + ex.Message);
			}
		}

		#endregion

		#region Helper Methods

		[HttpGet("[action]")]
		public async Task<IActionResult> GetBeneficiaries(CancellationToken cn)
		{
			var users = await userService
				.TableNoTracking
				.Where(u => u.IsActive == Entities.Base.IsActiveEnum.Active)
				.Select(u => new { id = u.Id, name = u.Name })
				.ToListAsync(cn);

			return Ok(users);
		}

		private OpenOrderRequestStopStatusEnum DetermineNextStopStatusFromCheckingResult(OpenOrderRequestCommentStopCheckingResultEnum? result)
		{
			return result switch
			{
				OpenOrderRequestCommentStopCheckingResultEnum.NeedsRestart => OpenOrderRequestStopStatusEnum.StopCauseRequiresRestart,
				OpenOrderRequestCommentStopCheckingResultEnum.NeedsBOMCorrection => OpenOrderRequestStopStatusEnum.CheckStopFactorNeedToFixBom,
				OpenOrderRequestCommentStopCheckingResultEnum.NeedsDocumentOrItemDescriptionCorrection => OpenOrderRequestStopStatusEnum.NeedsDocumentRevision,
				OpenOrderRequestCommentStopCheckingResultEnum.DeleteIndustrialUnitRelatedRequest => OpenOrderRequestStopStatusEnum.IndustrialUnitStopCauseDeletionRequestCheck,
				OpenOrderRequestCommentStopCheckingResultEnum.AwaitingProjectManagerOrEmployer => OpenOrderRequestStopStatusEnum.ProjectManagerPendingStopReason,
				_ => OpenOrderRequestStopStatusEnum.InitialSubmissionAndSendToStopBoard
			};
		}

		private OpenOrderRequestStopStatusEnum DetermineNextStopStatusFromCheckingStatus(OpenOrderRequestStopCheckingStatusEnum? status)
		{
			return status switch
			{
				OpenOrderRequestStopCheckingStatusEnum.RetryRequest => OpenOrderRequestStopStatusEnum.RestartStopCauseCheck,
				OpenOrderRequestStopCheckingStatusEnum.ReRegisterStop => OpenOrderRequestStopStatusEnum.CheckStopFactorReRegisterStop,
				OpenOrderRequestStopCheckingStatusEnum.DeleteRequest => OpenOrderRequestStopStatusEnum.StoppingFactorForDeletionRequest,
				_ => OpenOrderRequestStopStatusEnum.InitialSubmissionAndSendToStopBoard
			};
		}

		#endregion

		#region Notification Methods

		/// <summary>
		/// HTS DoOperation ثبت کامنت: یک ایمیل.
		/// گیرنده: درخواست‌کننده + ذینفع مهندسی + (اگر نویسنده تدارکات نیست و تایید مهندسی دارد) متولی خرید
		/// + کاربر تایید مهندسی + گروه ثابت + اگر bagheri در گیرنده‌هاست اعضای گروه اضافه.
		/// رونوشت: گروه صنایع. اگر گیرنده‌ای نباشد، صنایع گیرنده می‌شود و رونوشت خالی است.
		/// </summary>
		private async Task CreateCommentNotification(OpenOrderRequest openOrderRequest, OpenOrderRequestComment comment, CancellationToken cn)
		{
			var row = await unitOfWork.Repository<OpenOrderRequest>().TableNoTracking
				.Where(o => o.Id == openOrderRequest.Id)
				.Select(o => new
				{
					o.Id,
					o.PurchaseRequestNumber,
					o.OrderNo,
					o.RequiredQty,
					o.OrderQty,
					o.SumSendQty,
					o.FactoredCount,
					o.SupplyShamsiDate,
					o.NeedDateShamsiDate,
					o.ProductionOrderNumber,
					o.OrderItemComment,
					o.EngineeringAccept,
					o.EngineeringAcceptUserId,
					o.PartId,
					o.RequestedPersonelEmail,
					o.RequestedEngineeringPersonelEmail,
					PartCode = o.Part.Code,
					PartName = o.Part.Name,
					CompanyName = o.ManCompany != null && o.ManCompany.Party != null ? o.ManCompany.Party.FullName : null,
					BuyerName = o.Part.BuyCategory != null && o.Part.BuyCategory.PurchaseResponsible != null
						? o.Part.BuyCategory.PurchaseResponsible.NameFa
						: null,
					BuyerEmail = o.Part.BuyCategory != null && o.Part.BuyCategory.PurchaseResponsible != null
						? o.Part.BuyCategory.PurchaseResponsible.Email
						: null,
					BuyerUsername = o.Part.BuyCategory != null && o.Part.BuyCategory.PurchaseResponsible != null
						? o.Part.BuyCategory.PurchaseResponsible.Username
						: null
				})
				.FirstOrDefaultAsync(cn);

			if (row?.Id == null)
				return;

			var to = new List<string>();
			foreach (var email in SplitEmailList(row.RequestedPersonelEmail))
				AddEmail(to, email);
			foreach (var email in SplitEmailList(row.RequestedEngineeringPersonelEmail))
				AddEmail(to, email);

			var isSupplyOrg = await unitOfWork.Repository<OrgUnit>().TableNoTracking
				.AnyAsync(c => c.Id == CurrentOrganizationUnitId && c.HamkaranUnitId == 121000108, cn);

			if (row.EngineeringAccept && !isSupplyOrg)
				AddEmail(to, row.BuyerEmail.HasValue() ? row.BuyerEmail : row.BuyerUsername);

			if (row.EngineeringAccept && row.EngineeringAcceptUserId is > 0)
			{
				var engineer = await userService.TableNoTracking
					.Where(u => u.Id == row.EngineeringAcceptUserId)
					.Select(u => new { u.Email, u.Username })
					.FirstOrDefaultAsync(cn);
				if (engineer != null)
					AddEmail(to, engineer.Email.HasValue() ? engineer.Email : engineer.Username);
			}

			if (to.Any(e => e.Contains("bagheri.h", StringComparison.OrdinalIgnoreCase)))
			{
				var extras = await _notificationGroupService.GetGroupMembersAsync("Sup.OpenOrderRequest.CommentExtraWhenBagheri", cn);
				foreach (var member in extras)
					AddEmail(to, member.Email);
			}

			var fixedTo = await _notificationGroupService.GetGroupMembersAsync("Sup.OpenOrderRequest.CommentFixedTo", cn);
			foreach (var member in fixedTo)
				AddEmail(to, member.Email);

			var industrial = await _notificationGroupService.GetGroupMembersAsync("Sup.OpenOrderRequest.Industrial", cn);
			var cc = new List<string>();
			if (to.Count == 0)
			{
				foreach (var member in industrial)
					AddEmail(to, member.Email);
			}
			else
			{
				foreach (var member in industrial)
					AddEmail(cc, member.Email);
				cc.RemoveAll(c => to.Any(t => string.Equals(t, c, StringComparison.OrdinalIgnoreCase)));
			}

			if (to.Count == 0)
				return;

			var supplyDate = isSupplyOrg ? comment.ShamsiDate : row.SupplyShamsiDate;
			var body = BuildCommentNotificationBody(
				CurrentUserFullName, row.PartCode, row.PartName, row.RequiredQty, row.OrderQty, row.SumSendQty,
				row.FactoredCount, supplyDate, row.NeedDateShamsiDate, row.OrderNo, row.CompanyName,
				row.ProductionOrderNumber, row.BuyerName, comment.CommentValue, row.OrderItemComment);

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = "کامنت درخواست خرید",
				Body = body,
				EntityId = row.Id,
				OwnerId = CurrentUserId ?? 1,
				ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={row.Id}",
				IsRead = false,
				IsSend = false,
				ToEmails = to,
				CcEmails = cc.Count == 0 ? null : cc
			}, cn);
			await unitOfWork.SaveChangesAsync(cn);
		}

		private static string BuildCommentNotificationBody(
			string? author, string? partCode, string? partName, decimal requiredQty, decimal? orderQty, decimal? sumSendQty,
			decimal factoredCount, string? supplyDate, string? needDate, long? orderNo, string? companyName,
			int? productionOrderNumber, string? buyerName, string? commentValue, string? orderItemComment)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"کاربر گرامی کامنت ذیل برای درخواست خرید شما توسط <strong>{author}</strong> صادر شد");
			sb.AppendLine("<br />");
			sb.AppendLine("<ul>");
			sb.AppendLine(ProductionOrderItemNotificationHelper.Li("کد کالا", partCode));
			sb.AppendLine(ProductionOrderItemNotificationHelper.Li("عنوان کالا", partName));
			sb.AppendLine(ProductionOrderItemNotificationHelper.Li("تعداد درخواست", requiredQty));
			sb.AppendLine(ProductionOrderItemNotificationHelper.Li("تعداد سفارش", orderQty));
			sb.AppendLine(ProductionOrderItemNotificationHelper.Li("تعداد برگ ارسال", sumSendQty));
			sb.AppendLine(ProductionOrderItemNotificationHelper.Li("تعداد خرید", factoredCount));
			sb.AppendLine(ProductionOrderItemNotificationHelper.Li("تاریخ تامین", supplyDate));
			sb.AppendLine(ProductionOrderItemNotificationHelper.Li("تاریخ نیاز", needDate));
			sb.AppendLine(ProductionOrderItemNotificationHelper.Li("شماره درخواست خرید", orderNo));
			sb.AppendLine(ProductionOrderItemNotificationHelper.Li("عنوان شرکت", companyName));
			sb.AppendLine(ProductionOrderItemNotificationHelper.Li("شماره سفارش ساخت", productionOrderNumber));
			sb.AppendLine(ProductionOrderItemNotificationHelper.Li("مسئول خرید", buyerName));
			sb.AppendLine(ProductionOrderItemNotificationHelper.Li("توضیحات", commentValue));
			sb.AppendLine(ProductionOrderItemNotificationHelper.Li("توضیحات آیتم", orderItemComment));
			sb.AppendLine("</ul>");
			return ProductionOrderItemNotificationHelper.BuildEmailShell(sb.ToString());
		}

		/// <summary>
		/// معادل HTS SendConfirmedItemsNotification برای تایید مهندسی:
		/// یک ایمیل با موضوع «درخواست های باز تایید شده» از صندوق IT Support.
		/// </summary>
		/// <param name="isSalesOrProject">false = تایید مهندسی؛ true = تایید فروش/پروژه (شاخه گیرندگان HTS فرق دارد).</param>
		private async Task CreateEngineeringAcceptNotification(long? openOrderRequestId, bool isSalesOrProject, CancellationToken cn)
		{
			var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
				.TableNoTracking
				.Select(c => new
				{
					c.Id,
					c.PurchaseRequestNumber,
					c.OrderNo,
					c.RequiredQty,
					c.OrderItemComment,
					PartCode = c.Part.Code,
					PartName = c.Part.Name,
					c.EngineeringAcceptUserId,
					c.SalesUnitConfirmationUserId,
					c.SalesUnitSalesExpertId,
					c.SalesUnitSalesManagerId,
					c.SalesUnitProjectManagerId,
					PurchaseResponsibleEmail = c.Part.BuyCategory != null && c.Part.BuyCategory.PurchaseResponsible != null
						? c.Part.BuyCategory.PurchaseResponsible.Email
						: null,
					PurchaseResponsibleUsername = c.Part.BuyCategory != null && c.Part.BuyCategory.PurchaseResponsible != null
						? c.Part.BuyCategory.PurchaseResponsible.Username
						: null
				}).FirstOrDefaultAsync(c => c.Id == openOrderRequestId, cn);

			if (openOrderRequest?.Id == null)
				return;

			var relatedUserIds = new[]
			{
				openOrderRequest.EngineeringAcceptUserId,
				openOrderRequest.SalesUnitConfirmationUserId,
				openOrderRequest.SalesUnitSalesExpertId,
				openOrderRequest.SalesUnitSalesManagerId,
				openOrderRequest.SalesUnitProjectManagerId
			}.Where(id => id is > 0).Select(id => id!.Value).Distinct().ToArray();

			var relatedUsers = relatedUserIds.Length == 0
				? []
				: await userService.TableNoTracking
					.Where(u => u.Id != null && relatedUserIds.Contains(u.Id.Value))
					.Select(u => new { u.Id, u.Username, u.Email })
					.ToListAsync(cn);

			string EmailOf(long? userId)
			{
				if (userId is not > 0)
					return "";
				var user = relatedUsers.FirstOrDefault(u => u.Id == userId);
				if (user == null)
					return "";
				return HavayarEmail(user.Email.HasValue() ? user.Email : user.Username);
			}

			var suppliesUnitReceivers = new[]
			{
				"Dordab.y", "Bagheri.h", "Yaltaghian.f", "Shahpordeli.s",
				"Sharifi.b", "sohrabi.za", "Ashrafi.m", "Abdollahi.a",
				"zare.m", "Rajablou.a", "sarmadi.p"
			}.Select(HavayarEmail).Where(e => e.Length > 0).ToList();

			var receivers = new List<string>();
			var ccReceivers = new List<string>();
			AddEmail(ccReceivers, EmailOf(openOrderRequest.EngineeringAcceptUserId));

			var responsibleEmail = HavayarEmail(
				openOrderRequest.PurchaseResponsibleEmail.HasValue()
					? openOrderRequest.PurchaseResponsibleEmail
					: openOrderRequest.PurchaseResponsibleUsername);

			if (openOrderRequest.SalesUnitSalesExpertId.HasValue)
			{
				if (isSalesOrProject)
				{
					foreach (var email in suppliesUnitReceivers)
						AddEmail(receivers, email);

					AddEmail(ccReceivers, EmailOf(openOrderRequest.SalesUnitConfirmationUserId));
					AddEmail(ccReceivers, EmailOf(openOrderRequest.SalesUnitSalesExpertId));
					AddEmail(ccReceivers, EmailOf(openOrderRequest.SalesUnitSalesManagerId));
					AddEmail(ccReceivers, EmailOf(openOrderRequest.SalesUnitProjectManagerId));
				}
				else
				{
					AddEmail(receivers, EmailOf(openOrderRequest.SalesUnitConfirmationUserId));
					AddEmail(receivers, EmailOf(openOrderRequest.SalesUnitSalesExpertId));
					AddEmail(receivers, EmailOf(openOrderRequest.SalesUnitSalesManagerId));
					AddEmail(receivers, EmailOf(openOrderRequest.SalesUnitProjectManagerId));

					if (responsibleEmail.Length > 0)
						AddEmail(receivers, responsibleEmail);
					else
					{
						foreach (var email in suppliesUnitReceivers)
							AddEmail(receivers, email);
						AddEmail(receivers, "Razaghmanesh.z@havayar.com");
					}
				}
			}
			else if (responsibleEmail.Length > 0)
			{
				AddEmail(receivers, responsibleEmail);
			}
			else
			{
				foreach (var email in suppliesUnitReceivers)
					AddEmail(receivers, email);
				AddEmail(receivers, "Razaghmanesh.z@havayar.com");
			}

			if (receivers.Any(p => ContainsUser(p, "sohrabi.za") || ContainsUser(p, "razaghmanesh.z") || ContainsUser(p, "sarmadi.p")))
				AddEmail(ccReceivers, "bagheri.h@havayar.com");
			if (receivers.Any(p => ContainsUser(p, "shahpordeli.s") || ContainsUser(p, "rajablou.a")))
				AddEmail(ccReceivers, "yaltaghian.f@havayar.com");
			if (receivers.Any(p => ContainsUser(p, "ashrafi.m") || ContainsUser(p, "sharifi.b") || ContainsUser(p, "zabihian.s")))
				AddEmail(ccReceivers, "parhizkari.s@havayar.com");

			ccReceivers.RemoveAll(cc => receivers.Contains(cc, StringComparer.OrdinalIgnoreCase));

			if (receivers.Count == 0)
				return;

			var requestId = openOrderRequest.Id.Value;
			var pageUrl = BuildOpenOrderRequestAbsoluteUrl(requestId);
			var confirmer = CurrentUserFullName.HasValue() ? CurrentUserFullName : CurrentUserName;
			var unitTitle = isSalesOrProject ? "واحد پروژه / فروش" : "واحد مهندسی";

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = "درخواست های باز تایید شده",
				Body = BuildEngineeringAcceptEmailBody(openOrderRequest.PurchaseRequestNumber, openOrderRequest.OrderNo, openOrderRequest.PartCode, openOrderRequest.PartName, openOrderRequest.RequiredQty, openOrderRequest.OrderItemComment, confirmer, unitTitle, pageUrl),
				EntityId = requestId,
				OwnerId = CurrentUserId ?? openOrderRequest.EngineeringAcceptUserId ?? 1,
				ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={requestId}",
				IsRead = false,
				IsSend = false,
				ToEmails = receivers,
				CcEmails = ccReceivers.Count == 0 ? null : ccReceivers
			}, cn);

			await unitOfWork.SaveChangesAsync(cn);
		}

		private static string BuildEngineeringAcceptEmailBody(
			long purchaseRequestNumber,
			long? orderNo,
			string? partCode,
			string? partName,
			decimal requiredQty,
			string? orderItemComment,
			string? confirmer,
			string unitTitle,
			string pageUrl)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<div style='width: 100%;text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl' width='100%'>");
			sb.AppendLine("<tr style='background: #000aa0'>");
			sb.AppendLine("<td>");
			sb.AppendLine("<div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center;direction:rtl'>");
			sb.AppendLine("گروه صنعتی هوایار");
			sb.AppendLine("</div>");
			sb.AppendLine("</td>");
			sb.AppendLine("</tr>");
			sb.AppendLine("<tr>");
			sb.AppendLine("<td>");
			sb.AppendLine("با سلام و احترام");
			sb.AppendLine("<br />");
			sb.AppendLine($" بدینوسیله به استحضار می رساند، درخواست های باز ذیل توسط  <strong>{System.Net.WebUtility.HtmlEncode(confirmer)}</strong> به تایید {System.Net.WebUtility.HtmlEncode(unitTitle)} رسید");
			sb.AppendLine("<br />");
			sb.AppendLine("خواهشمند است بررسی و در صورت نیاز، اعمال لازم در جهت طی شدن ادامه مسیر درخواست اقدام فرمایید");
			sb.Append("<ul>");
			sb.Append("<li>");
			sb.Append("شماره درخواست : &nbsp; <strong>" + purchaseRequestNumber + "</strong>");
			sb.Append("</li>");
			sb.Append("<li>");
			sb.Append("شماره سفارش : &nbsp; <strong>" + orderNo + "</strong>");
			sb.Append("</li>");
			sb.Append("<li>");
			sb.Append(" کد کالا : &nbsp;" + System.Net.WebUtility.HtmlEncode(partCode));
			sb.Append("</li>");
			sb.Append("<li>");
			sb.Append(" نام کالا : &nbsp;" + System.Net.WebUtility.HtmlEncode(partName));
			sb.Append("</li>");
			sb.Append("<li>");
			sb.Append(" تعداد درخواست : &nbsp;" + requiredQty);
			sb.Append("</li>");
			sb.Append("<li>");
			sb.Append(" توضیحات : &nbsp;" + System.Net.WebUtility.HtmlEncode(orderItemComment));
			sb.Append("</li>");
			sb.Append("<br/><br/>");
			sb.Append("</ul>");
			sb.Append("<br />");
			sb.Append("<a href='" + System.Net.WebUtility.HtmlEncode(pageUrl) + "'>مشاهده درخواست باز</a>");
			sb.AppendLine("</td>");
			sb.AppendLine("</tr>");
			sb.AppendLine("</table>");
			sb.AppendLine("</div>");
			return sb.ToString();
		}

		private string BuildOpenOrderRequestAbsoluteUrl(long id)
		{
			var config = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
			var baseUrl = config["WebAppBaseUrl"];
			if (!baseUrl.HasValue())
				baseUrl = "https://portal.havayar.com";
			return $"{baseUrl!.TrimEnd('/')}/Panel/Sup/OpenOrderRequest/Edit?id={id}";
		}

		private static string HavayarEmail(string? value)
		{
			if (!value.HasValue())
				return "";
			var email = value!.Trim();
			return email.Contains('@') ? email : email + "@havayar.com";
		}

		private static string? UserNotifyEmail(User? user)
		{
			if (user == null)
				return null;
			if (user.Email.HasValue())
				return user.Email;
			return user.Username.HasValue() ? HavayarEmail(user.Username) : null;
		}

		private static IEnumerable<string> SplitEmailList(string? raw)
		{
			if (!raw.HasValue())
				yield break;

			foreach (var part in raw!.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				if (part.HasValue())
					yield return part.Trim();
			}
		}

		/// <summary>HTS روی هر ایمیل توقف این دو نشانی را CC می‌کند.</summary>
		private static void AddStopFixedCc(List<string> cc)
		{
			AddEmail(cc, "Saemian.r@havayar.com");
			AddEmail(cc, "mosayebi.m@havayar.com");
		}

		private static string? AppendReStopSuffix(string? comment)
		{
			if (!comment.HasValue())
				return comment;
			return comment!.Contains("ثبت توقف مجدد") ? comment : comment + " (ثبت توقف مجدد)";
		}

		private static void AddEmail(List<string> target, string? email)
		{
			foreach (var part in SplitEmailList(email))
			{
				var normalized = HavayarEmail(part);
				if (normalized.Length == 0)
					continue;
				if (target.Any(e => string.Equals(e, normalized, StringComparison.OrdinalIgnoreCase)))
					continue;
				target.Add(normalized);
			}
		}

		private static bool ContainsUser(string email, string username)
			=> email.Contains(username, StringComparison.OrdinalIgnoreCase);

		private async Task CreateStopInitNotification(OpenOrderRequest openOrderRequest, OpenOrderRequestComment comment, CancellationToken cn)
		{
			var recipientEmails = new List<string>();
			var ccRecipientEmails = new List<string>();
			var userData =  await userService.GetAll();
			 
			comment.StopOperator = userData.FirstOrDefault(u => u.Id == comment.StopOperatorId);

			var title = "توقف درخواست خرید";
			var stopInfo = await LoadStopMailInfoAsync(openOrderRequest.Id, cn);
			if (stopInfo == null)
				return;
			var body = BuildStopInitNotificationBody(openOrderRequest, comment, stopInfo);

			var operatorEmail = UserNotifyEmail(comment.StopOperator);
			if (operatorEmail.HasValue())
				recipientEmails.Add(operatorEmail);

			if (CurrentUserEmail.HasValue())
				ccRecipientEmails.Add(CurrentUserEmail);

			var buyCategoryPurchaseResponsibleEmail =  unitOfWork.Repository<BuyCategoryItem>()
				.TableNoTracking
				.Include(c => c.BuyCategory)
				.ThenInclude(bc => bc.PurchaseResponsible)
				.Where(c => c.PartId == openOrderRequest.PartId && c.BuyCategory.PurchaseResponsible != null && c.BuyCategory.PurchaseResponsible.Email != null)
				.Select(c => c.BuyCategory.PurchaseResponsible.Email)
				.FirstOrDefault();

			if(buyCategoryPurchaseResponsibleEmail.HasValue())
				ccRecipientEmails.Add(buyCategoryPurchaseResponsibleEmail);

			if (comment.BeneficiariesIds.HasValue())
			{
				var beneficiariesIds = comment.BeneficiariesIds.Split(',').Where(p => p.HasValue()).Select(long.Parse).ToList();

				var beneficiariesEmails = userData.Where(p => beneficiariesIds.Contains((long)p.Id)).Select(p => p.Email).ToArray();
				if (beneficiariesEmails.Any())
					ccRecipientEmails.AddRange(beneficiariesEmails);
			}

			if (!string.IsNullOrEmpty(openOrderRequest.RequestedPersonelEmail))
				ccRecipientEmails.AddRange(SplitEmailList(openOrderRequest.RequestedPersonelEmail));

			if (!string.IsNullOrEmpty(openOrderRequest.RequestedEngineeringPersonelEmail))
				ccRecipientEmails.AddRange(SplitEmailList(openOrderRequest.RequestedEngineeringPersonelEmail));

			if (openOrderRequest.EngineeringAcceptUserId.HasValue)
			{
				var engineeringAcceptUser = userData.FirstOrDefault(p => p.Id == openOrderRequest.EngineeringAcceptUserId);
				if (engineeringAcceptUser != null && engineeringAcceptUser.Email.HasValue())
					ccRecipientEmails.Add(engineeringAcceptUser.Email);
			}

			if (openOrderRequest.SalesUnitProjectManagerId.HasValue)
			{
				var salesUnitProjectManager = userData.FirstOrDefault(p => p.Id == openOrderRequest.SalesUnitProjectManagerId);
				if (salesUnitProjectManager != null && salesUnitProjectManager.Email.HasValue())
					ccRecipientEmails.Add(salesUnitProjectManager.Email);
			}

			AddStopFixedCc(ccRecipientEmails);

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = title,
				Body = body,
				EntityId = openOrderRequest.Id,
				OwnerId = (long)CurrentUserId,
				ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={openOrderRequest.Id}",
				IsRead = false,
				IsSend = false,
				CcEmails = ccRecipientEmails,
				ToEmails = recipientEmails

			}, cn);

		  
		}

		private async Task CreateStopOperatorReviewNotification(OpenOrderRequest openOrderRequest, OpenOrderRequestComment comment, CancellationToken cn)
		{
			var recipientEmails = new List<string>();
			var ccRecipientEmails = new List<string>();
			var userData = await userService.GetAll();

			comment.StopOperator = userData.FirstOrDefault(u => u.Id == comment.StopOperatorId);
			var operatorEmail = UserNotifyEmail(comment.StopOperator);
			if (operatorEmail.HasValue())
				recipientEmails.Add(operatorEmail);

			if (comment.CreatedById is > 0)
			{
				var creator = userData.FirstOrDefault(u => u.Id == comment.CreatedById);
				var creatorEmail = UserNotifyEmail(creator);
				if (creatorEmail.HasValue())
					recipientEmails.Add(creatorEmail);
			}

			var stopStatus = (int?)openOrderRequest.StopStatus ?? 0;
			var title = stopStatus == (int)OpenOrderRequestStopStatusEnum.ProjectManagerPendingStopReason
				? "اطلاع رسانی 'در انتظار تایید' توقف درخواست خرید"
				: "تعیین وضعیت عامل توقف درخواست خرید";
			var stopInfo = await LoadStopMailInfoAsync(openOrderRequest.Id, cn);
			if (stopInfo == null)
				return;
			var reviewer = CurrentUserFullNameFn ?? CurrentUserFullName;
			var reviewMessage = stopStatus == (int)OpenOrderRequestStopStatusEnum.ProjectManagerPendingStopReason
				? $"بدینوسیله اعلام می گردد توقفی با مشخصات ذیل در ماژول درخواست های باز توسط {reviewer} بررسی گردید، خواهشمند است تا اعمال نظر از سوی مدیر پروژه / کارفرما تا تاریخ تقریبی اعلام شده منتظر بمانید."
				: $"بدینوسیله اعلام می گردد توقفی با مشخصات ذیل در ماژول درخواست های باز توسط {reviewer} بررسی گردید، خواهشمند است نسبت به ادامه روند آن اقدام فرمایید.";
			var body = BuildStopReviewNotificationBody(openOrderRequest, comment, stopInfo, reviewMessage);

			if (CurrentUserEmail.HasValue())
				ccRecipientEmails.Add(CurrentUserEmail);

			var buyCategoryPurchaseResponsibleEmail = unitOfWork.Repository<BuyCategoryItem>()
				.TableNoTracking
				.Include(c => c.BuyCategory)
				.ThenInclude(bc => bc.PurchaseResponsible)
				.Where(c => c.PartId == openOrderRequest.PartId && c.BuyCategory.PurchaseResponsible != null && c.BuyCategory.PurchaseResponsible.Email != null)
				.Select(c => c.BuyCategory.PurchaseResponsible.Email)
				.FirstOrDefault();

			if (buyCategoryPurchaseResponsibleEmail.HasValue())
				ccRecipientEmails.Add(buyCategoryPurchaseResponsibleEmail);

			if (comment.BeneficiariesIds.HasValue())
			{
				var beneficiariesIds = comment.BeneficiariesIds.Split(',').Where(p => p.HasValue()).Select(long.Parse).ToList();

				var beneficiariesEmails = userData.Where(p => beneficiariesIds.Contains((long)p.Id)).Select(p => p.Email).ToArray();
				if (beneficiariesEmails.Any())
					ccRecipientEmails.AddRange(beneficiariesEmails);
			}

			if (!string.IsNullOrEmpty(openOrderRequest.RequestedPersonelEmail))
				ccRecipientEmails.AddRange(SplitEmailList(openOrderRequest.RequestedPersonelEmail));

			if (!string.IsNullOrEmpty(openOrderRequest.RequestedEngineeringPersonelEmail))
				ccRecipientEmails.AddRange(SplitEmailList(openOrderRequest.RequestedEngineeringPersonelEmail));

			if (openOrderRequest.EngineeringAcceptUserId.HasValue)
			{
				var engineeringAcceptUser = userData.FirstOrDefault(p => p.Id == openOrderRequest.EngineeringAcceptUserId);
				if (engineeringAcceptUser != null && engineeringAcceptUser.Email.HasValue())
					ccRecipientEmails.Add(engineeringAcceptUser.Email);
			}

			if (openOrderRequest.SalesUnitProjectManagerId.HasValue)
			{
				var salesUnitProjectManager = userData.FirstOrDefault(p => p.Id == openOrderRequest.SalesUnitProjectManagerId);
				if (salesUnitProjectManager != null && salesUnitProjectManager.Email.HasValue())
					ccRecipientEmails.Add(salesUnitProjectManager.Email);
			}

			AddStopFixedCc(ccRecipientEmails);

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = title,
				Body = body,
				EntityId = openOrderRequest.Id,
				OwnerId = (long)CurrentUserId,
				ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={openOrderRequest.Id}",
				IsRead = false,
				IsSend = false,
				CcEmails = ccRecipientEmails,
				ToEmails = recipientEmails

			}, cn);
		}

		private sealed record StopMailInfo(
			long PurchaseRequestNumber, long? OrderNo, string? PartCode, string? PartName, decimal RequiredQty,
			string? OrderItemComment, string? BuyerName, int? ProductionOrderNumber, string? DlTitle, bool HasSalesExpert);

		/// <summary>اطلاعات کامل کالا/درخواست برای ایمیل‌های توقف (معادل SendStopRequestNotifications در HTS).</summary>
		private async Task<StopMailInfo?> LoadStopMailInfoAsync(long? openOrderRequestId, CancellationToken cn)
		{
			return await unitOfWork.Repository<OpenOrderRequest>().TableNoTracking
				.Where(o => o.Id == openOrderRequestId)
				.Select(o => new StopMailInfo(
					o.PurchaseRequestNumber,
					o.OrderNo,
					o.Part.Code,
					o.Part.Name,
					o.RequiredQty,
					o.OrderItemComment,
					o.Part.BuyCategory != null && o.Part.BuyCategory.PurchaseResponsible != null
						? o.Part.BuyCategory.PurchaseResponsible.NameFa
						: null,
					o.ProductionOrderNumber,
					o.Dl != null ? o.Dl.Title : null,
					o.SalesUnitSalesExpertId != null))
				.FirstOrDefaultAsync(cn);
		}

		private static string BuildStopRequestItems(StopMailInfo info, string? stopReason)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"<li>شماره درخواست خرید : <strong>{info.PurchaseRequestNumber}</strong></li>");
			sb.AppendLine($"<li>شماره سفارش : <strong>{info.OrderNo}</strong></li>");
			sb.AppendLine($"<li>کد کالا : <strong>{info.PartCode}</strong></li>");
			sb.AppendLine($"<li>عنوان کالا : <strong>{info.PartName}</strong></li>");
			sb.AppendLine($"<li>مقدار درخواست : <strong>{info.RequiredQty}</strong></li>");
			sb.AppendLine($"<li>توضیحات درخواست : <strong>{info.OrderItemComment}</strong></li>");
			sb.AppendLine($"<li>مسئول خرید : <strong>{info.BuyerName}</strong></li>");
			sb.AppendLine($"<li>علت توقف : <strong style='color:red'>{stopReason}</strong></li>");
			if (info.HasSalesExpert)
			{
				sb.AppendLine($"<li>شماره سفارش ساخت : <strong>{info.ProductionOrderNumber}</strong></li>");
				sb.AppendLine($"<li>تفصیل : <strong>{info.DlTitle}</strong></li>");
			}
			return sb.ToString();
		}

		private static string BuildStopTypeOperatorItems(OpenOrderRequestComment comment)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"<li>نوع توقف : <strong style='color:red'>{(comment.StopType.HasValue ? comment.StopType.Value.ToDisplay() : "نامشخص")}</strong></li>");
			sb.AppendLine($"<li>عامل توقف : <strong style='color:red'>{comment.StopOperator?.NameFa ?? "-"}</strong></li>");
			return sb.ToString();
		}

		private string BuildStopReviewNotificationBody(OpenOrderRequest openOrderRequest, OpenOrderRequestComment comment, StopMailInfo info, string message)
		{
			var sb = new StringBuilder();
			sb.AppendLine(message);
			sb.AppendLine("<br /><br />");
			sb.AppendLine("<ul>");
			sb.Append(BuildStopTypeOperatorItems(comment));
			sb.AppendLine($"<li>نتیجه بررسی علت توقف : <strong style='color:red'>{comment.StopCheckingResult?.ToDisplay()}</strong></li>");
			if (comment.StopCheckingComment.HasValue())
				sb.AppendLine($"<li>توضیحات بررسی : <strong>{comment.StopCheckingComment}</strong></li>");

			if (!string.IsNullOrEmpty(comment.ProjectManagerApproximateCommentShamsiDate))
				sb.AppendLine($"<li>تاریخ تقریبی اعمال نظر مدیر پروژه/ کارفرما : <strong style='color:red'>{comment.ProjectManagerApproximateCommentShamsiDate}</strong></li>");

			if (comment.IsNeedToUpdateBom)
				sb.AppendLine("<li>نیاز به اصلاح BOM : <strong style='color:red'>بلی</strong></li>");

			if (comment.IsNeedToDeleteBom)
				sb.AppendLine("<li>نیاز به حذف BOM : <strong style='color:red'>بلی</strong></li>");

			sb.Append(BuildStopRequestItems(info, comment.CommentValue));
			sb.AppendLine("</ul>");
			return ProductionOrderItemNotificationHelper.BuildEmailShell(sb.ToString());
		}

		private string BuildStopInitNotificationBody(OpenOrderRequest openOrderRequest, OpenOrderRequestComment comment, StopMailInfo info)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"بدینوسیله اعلام می گردد توقف جدیدی توسط <strong style='color:red'>{CurrentUserFullNameFn}</strong> در ماژول درخواست های باز و با مشخصات ذیل ثبت گردید، خواهشمند است نسبت به بررسی و ادامه روند آن اقدام فرمایید");
			sb.AppendLine("<br /><br />");
			sb.AppendLine("<ul>");
			sb.Append(BuildStopTypeOperatorItems(comment));
			sb.Append(BuildStopRequestItems(info, comment.CommentValue));
			sb.AppendLine("</ul>");
			return ProductionOrderItemNotificationHelper.BuildEmailShell(sb.ToString());
		}

		private async Task CreateStopRequesterDecisionNotification(OpenOrderRequest openOrderRequest, OpenOrderRequestComment comment, CancellationToken cn)
		{
			var recipientEmails = new List<string>();
			var ccRecipientEmails = new List<string>();
			var userData = await userService.GetAll();

			if (comment.StopOperatorId.HasValue)
			{
				var stopOperator = userData.FirstOrDefault(u => u.Id == comment.StopOperatorId);
				AddEmail(recipientEmails, UserNotifyEmail(stopOperator));
			}

			if (CurrentUserEmail.HasValue())
				ccRecipientEmails.Add(CurrentUserEmail);

			var buyCategoryPurchaseResponsibleEmail = unitOfWork.Repository<BuyCategoryItem>()
				.TableNoTracking
				.Include(c => c.BuyCategory)
				.ThenInclude(bc => bc.PurchaseResponsible)
				.Where(c => c.PartId == openOrderRequest.PartId && c.BuyCategory.PurchaseResponsible != null && c.BuyCategory.PurchaseResponsible.Email != null)
				.Select(c => c.BuyCategory.PurchaseResponsible.Email)
				.FirstOrDefault();

			if (buyCategoryPurchaseResponsibleEmail.HasValue())
				ccRecipientEmails.Add(buyCategoryPurchaseResponsibleEmail);

			if (comment.BeneficiariesIds.HasValue())
			{
				var beneficiariesIds = comment.BeneficiariesIds.Split(',').Where(p => p.HasValue()).Select(long.Parse).ToList();

				var beneficiariesEmails = userData.Where(p => beneficiariesIds.Contains((long)p.Id)).Select(p => p.Email).ToArray();
				if (beneficiariesEmails.Any())
					ccRecipientEmails.AddRange(beneficiariesEmails);
			}

			if (!string.IsNullOrEmpty(openOrderRequest.RequestedPersonelEmail))
				ccRecipientEmails.AddRange(SplitEmailList(openOrderRequest.RequestedPersonelEmail));

			if (!string.IsNullOrEmpty(openOrderRequest.RequestedEngineeringPersonelEmail))
				ccRecipientEmails.AddRange(SplitEmailList(openOrderRequest.RequestedEngineeringPersonelEmail));

			if (openOrderRequest.EngineeringAcceptUserId.HasValue)
			{
				var engineeringAcceptUser = userData.FirstOrDefault(p => p.Id == openOrderRequest.EngineeringAcceptUserId);
				if (engineeringAcceptUser != null && engineeringAcceptUser.Email.HasValue())
					ccRecipientEmails.Add(engineeringAcceptUser.Email);
			}

			if (openOrderRequest.SalesUnitProjectManagerId.HasValue)
			{
				var salesUnitProjectManager = userData.FirstOrDefault(p => p.Id == openOrderRequest.SalesUnitProjectManagerId);
				if (salesUnitProjectManager != null && salesUnitProjectManager.Email.HasValue())
					ccRecipientEmails.Add(salesUnitProjectManager.Email);
			}

			var actor = CurrentUserFullNameFn ?? CurrentUserFullName;
			var decisionStatus = (int?)openOrderRequest.StopStatus ?? 0;
			string title;
			string message;
			switch (decisionStatus)
			{
				case (int)OpenOrderRequestStopStatusEnum.RestartStopCauseCheck:
					title = "تعیین وضعیت درخواست کننده توقف درخواست خرید";
					message = $"بدینوسیله اعلام می گردد توقفی با مشخصات ذیل در ماژول درخواست های باز توسط {actor} بررسی و <strong style='color:blue'>راه اندازی مجدد</strong> گردید، مراتب جهت هرگونه اقدام مقتضی به عرض می رسد.";
					AddEmail(recipientEmails, buyCategoryPurchaseResponsibleEmail);
					break;
				case (int)OpenOrderRequestStopStatusEnum.CheckStopFactorReRegisterStop:
					title = "تعیین وضعیت درخواست کننده توقف درخواست خرید";
					message = $"بدینوسیله اعلام می گردد توقفی <strong style='color:red'>بصورت مجدد</strong> با مشخصات ذیل در ماژول درخواست های باز توسط {actor} بررسی گردید، خواهشمند است نسبت به ادامه روند آن اقدام فرمایید.";
					break;
				case (int)OpenOrderRequestStopStatusEnum.StoppingFactorForDeletionRequest:
					title = "تعیین وضعیت درخواست کننده توقف درخواست خرید";
					message = $"بدینوسیله اعلام می گردد توقفی <strong style='color:red'>بعلت عدم بررسی توسط عامل توقف</strong> با مشخصات ذیل توسط {actor} به کارتابل واحد صنایع منتسب گردید، لذا خواهشمند است در رابطه با 'حذف / ثبت توقف مجدد درخوایت' اقدامات لازم را مبذول فرمایید.";
					var industrialMembers = await _notificationGroupService.GetGroupMembersAsync("Sup.OpenOrderRequest.Industrial", cn);
					foreach (var member in industrialMembers)
						AddEmail(recipientEmails, member.Email);
					break;
				default:
					title = "تصمیم درخواست‌کننده توقف";
					message = "درخواست‌کننده توقف تصمیم خود را اعلام کرد";
					break;
			}

			var stopInfo = await LoadStopMailInfoAsync(openOrderRequest.Id, cn);
			if (stopInfo == null)
				return;
			comment.StopOperator ??= userData.FirstOrDefault(u => u.Id == comment.StopOperatorId);

			var bodySb = new StringBuilder();
			bodySb.AppendLine(message);
			bodySb.AppendLine("<br /><br />");
			bodySb.AppendLine("<ul>");
			bodySb.Append(BuildStopTypeOperatorItems(comment));
			if (decisionStatus != (int)OpenOrderRequestStopStatusEnum.CheckStopFactorReRegisterStop)
				bodySb.AppendLine($"<li>نتیجه بررسی علت توقف : <strong style='color:red'>{comment.StopCheckingResult?.ToDisplay()}</strong></li>");
			bodySb.AppendLine($"<li>وضعیت : <strong>{comment.StopCheckingStatus?.ToDisplay()}</strong></li>");
			if (comment.StopCheckingStatusComment.HasValue())
				bodySb.AppendLine($"<li>توضیحات : <strong>{comment.StopCheckingStatusComment}</strong></li>");
			bodySb.Append(BuildStopRequestItems(stopInfo, comment.CommentValue));
			bodySb.AppendLine("</ul>");
			var body = ProductionOrderItemNotificationHelper.BuildEmailShell(bodySb.ToString());

			AddStopFixedCc(ccRecipientEmails);

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = title,
				Body = body,
				EntityId = openOrderRequest.Id,
				OwnerId = (long)CurrentUserId,
				ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={openOrderRequest.Id}",
				IsRead = false,
				IsSend = false,
				CcEmails = ccRecipientEmails,
				ToEmails = recipientEmails

			}, cn);
		}

		/// <summary>
		/// HTS DoStartStopOperation وقتی IsStop=false: ایمیل فقط اگر ایمیل درخواست‌کننده پر باشد.
		/// گیرنده: درخواست‌کننده + متولی خرید + ذینفع مهندسی + کاربر تایید مهندسی.
		/// متن: همان فهرست کالای HTS، با قالب ایمیل کامنت درخواست خرید.
		/// </summary>
		private async Task CreateTriggeringNotification(OpenOrderRequest openOrderRequest, CancellationToken cn)
		{
			if (!openOrderRequest.RequestedPersonelEmail.HasValue())
				return;

			var row = await unitOfWork.Repository<OpenOrderRequest>().TableNoTracking
				.Where(o => o.Id == openOrderRequest.Id)
				.Select(o => new
				{
					o.Id,
					o.PurchaseRequestNumber,
					o.OrderNo,
					o.RequiredQty,
					o.OrderQty,
					o.SumSendQty,
					o.FactoredCount,
					o.SupplyShamsiDate,
					o.NeedDateShamsiDate,
					o.OrderItemComment,
					PartCode = o.Part.Code,
					PartName = o.Part.Name,
					BuyerName = o.Part.BuyCategory != null && o.Part.BuyCategory.PurchaseResponsible != null
						? o.Part.BuyCategory.PurchaseResponsible.NameFa
						: null
				})
				.FirstOrDefaultAsync(cn);

			if (row?.Id == null)
				return;

			var to = new List<string>();
			foreach (var email in SplitEmailList(openOrderRequest.RequestedPersonelEmail))
				AddEmail(to, email);
			foreach (var email in SplitEmailList(openOrderRequest.RequestedEngineeringPersonelEmail))
				AddEmail(to, email);

			var responsible = await unitOfWork.Repository<BuyCategoryItem>().TableNoTracking
				.Where(c => c.PartId == openOrderRequest.PartId && c.BuyCategory.PurchaseResponsible != null)
				.Select(c => new
				{
					c.BuyCategory.PurchaseResponsible!.Email,
					c.BuyCategory.PurchaseResponsible.Username
				})
				.FirstOrDefaultAsync(cn);
			if (responsible != null)
				AddEmail(to, responsible.Email.HasValue() ? responsible.Email : responsible.Username);

			if (openOrderRequest.EngineeringAcceptUserId is > 0)
			{
				var engineer = await userService.TableNoTracking
					.Where(u => u.Id == openOrderRequest.EngineeringAcceptUserId)
					.Select(u => new { u.Email, u.Username })
					.FirstOrDefaultAsync(cn);
				if (engineer != null)
					AddEmail(to, engineer.Email.HasValue() ? engineer.Email : engineer.Username);
			}

			if (to.Count == 0)
				return;

			var actor = CurrentUserFullNameFn ?? CurrentUserFullName ?? "";
			var body = BuildTriggeringNotificationBody(
				row.PartCode, row.PartName, row.RequiredQty, row.OrderQty, row.SumSendQty, row.FactoredCount,
				row.SupplyShamsiDate, row.NeedDateShamsiDate, row.PurchaseRequestNumber, row.OrderNo,
				row.BuyerName, row.OrderItemComment, actor);

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = "راه اندازی درخواست خرید",
				Body = body,
				EntityId = openOrderRequest.Id,
				OwnerId = CurrentUserId ?? 1,
				ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={openOrderRequest.Id}",
				IsRead = false,
				IsSend = false,
				ToEmails = to
			}, cn);

			await unitOfWork.SaveChangesAsync(cn);
		}

		private static string BuildTriggeringNotificationBody(
			string? partCode, string? partName, decimal requiredQty, decimal? orderQty, decimal? sumSendQty, decimal factoredCount,
			string? supplyDate, string? needDate, long purchaseRequestNumber, long? orderNo,
			string? buyerName, string? orderItemComment, string actor)
		{
			var sb = new StringBuilder();
			sb.AppendLine("کاربر گرامی درخواست کالای شما توسط واحد صنایع راه اندازی شده است");
			sb.AppendLine("<br /><br />");
			sb.AppendLine("<ul>");
			sb.AppendLine($"<li>کد کالا : <strong>{partCode}</strong></li>");
			sb.AppendLine($"<li>عنوان کالا : <strong>{partName}</strong></li>");
			sb.AppendLine($"<li>تعداد درخواست : <strong>{requiredQty}</strong></li>");
			sb.AppendLine($"<li>تعداد سفارش : <strong>{orderQty}</strong></li>");
			sb.AppendLine($"<li>تعداد برگ ارسال : <strong>{sumSendQty}</strong></li>");
			sb.AppendLine($"<li>تعداد خرید شده : <strong>{factoredCount}</strong></li>");
			sb.AppendLine($"<li>تاریخ تامین : <strong>{supplyDate}</strong></li>");
			sb.AppendLine($"<li>تاریخ نیاز : <strong>{needDate}</strong></li>");
			sb.AppendLine($"<li>شماره درخواست خرید : <strong>{purchaseRequestNumber}</strong></li>");
			sb.AppendLine($"<li>شماره سفارش خرید : <strong>{orderNo}</strong></li>");
			sb.AppendLine($"<li>مسئول خرید : <strong>{buyerName}</strong></li>");
			sb.AppendLine($"<li>توضیحات : <strong>{orderItemComment}</strong></li>");
			sb.AppendLine("</ul>");
			sb.AppendLine($"راه اندازی توسط : {actor}<br>");
			return ProductionOrderItemNotificationHelper.BuildEmailShell(sb.ToString());
		}

		/// <summary>
		/// HTS SendNewAttachmentsEmailNotification: با آپلود پیوست، یک ایمیل «اعلان ثبت پیوست درخواست باز».
		/// گیرنده متولی خرید؛ اگر عضو گروه همتا باشد بقیهٔ همان گروه هم گیرنده می‌شوند.
		/// رونوشت: صنایع + رونوشت ثابت. اگر متولی ایمیل نداشته باشد، رونوشت گیرنده می‌شود.
		/// </summary>
		private async Task SendUploadedAttachmentEmailAsync(OpenOrderRequest openOrderRequest, CancellationToken cn)
		{
			var row = await unitOfWork.Repository<OpenOrderRequest>().TableNoTracking
				.Where(o => o.Id == openOrderRequest.Id)
				.Select(o => new
				{
					o.Id,
					o.PurchaseRequestNumber,
					o.OrderNo,
					o.IsStop,
					PartCode = o.Part.Code,
					PartName = o.Part.Name,
					BuyerId = o.Part.BuyCategory != null ? o.Part.BuyCategory.PurchaseResponsibleId : null,
					BuyerEmail = o.Part.BuyCategory != null && o.Part.BuyCategory.PurchaseResponsible != null
						? o.Part.BuyCategory.PurchaseResponsible.Email
						: null,
					BuyerUsername = o.Part.BuyCategory != null && o.Part.BuyCategory.PurchaseResponsible != null
						? o.Part.BuyCategory.PurchaseResponsible.Username
						: null
				})
				.FirstOrDefaultAsync(cn);
			if (row?.Id == null)
				return;

			var to = new List<string>();
			AddEmail(to, row.BuyerEmail.HasValue() ? row.BuyerEmail : row.BuyerUsername);

			var peers = await _notificationGroupService.GetGroupMembersAsync("Sup.OpenOrderRequest.AutoAcceptPeerTo", cn);
			if (to.Count > 0 && peers.Any(p => p.Email.HasValue() && to.Any(t => t.Contains(p.Email.Split('@')[0], StringComparison.OrdinalIgnoreCase))))
			{
				foreach (var peer in peers)
					AddEmail(to, peer.Email);
			}

			var cc = new List<string>();
			foreach (var member in await _notificationGroupService.GetGroupMembersAsync("Sup.OpenOrderRequest.Industrial", cn))
				AddEmail(cc, member.Email);
			foreach (var member in await _notificationGroupService.GetGroupMembersAsync("Sup.OpenOrderRequest.AutoAcceptFixedCc", cn))
				AddEmail(cc, member.Email);

			if (to.Count == 0)
			{
				foreach (var email in cc)
					AddEmail(to, email);
				cc.Clear();
			}
			else
			{
				cc.RemoveAll(c => to.Any(t => string.Equals(t, c, StringComparison.OrdinalIgnoreCase)));
			}

			if (to.Count == 0)
				return;

			var sb = new StringBuilder();
			sb.AppendLine("بدینوسیله اعلام میگردد، مدرک/مدارک جدیدی جهت درخواست باز، با مشخصات ذیل آپلود گردید");
			sb.AppendLine("<br><br>شما می توانید با مراجعه به سیستم جامع / سیستم تدارکات / عملیات / درخواست های باز / پیوست مدارک از جزییات آن اطلاع یابید<br><br>");
			sb.AppendLine("<ul>");
			sb.AppendLine($"<li>شماره درخواست : <strong>{row.PurchaseRequestNumber}</strong></li>");
			sb.AppendLine($"<li>شماره سفارش : <strong>{row.OrderNo}</strong></li>");
			if (row.IsStop)
				sb.AppendLine("<li>وضعیت درخواست : <strong style='color:red'>متوقف شده</strong></li>");
			sb.AppendLine($"<li>کد کالا : <strong>{row.PartCode}</strong></li>");
			sb.AppendLine($"<li>عنوان کالا : <strong>{row.PartName}</strong></li>");
			sb.AppendLine("</ul>");

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = "اعلان ثبت پیوست درخواست باز",
				Body = ProductionOrderItemNotificationHelper.BuildEmailShell(sb.ToString()),
				EntityId = row.Id,
				OwnerId = row.BuyerId is > 0 ? row.BuyerId.Value : (CurrentUserId ?? 1),
				ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={row.Id}",
				IsRead = false,
				IsSend = false,
				ToEmails = to,
				CcEmails = cc.Count == 0 ? null : cc
			}, cn);
			await unitOfWork.SaveChangesAsync(cn);
		}

		/// <summary>
		/// HTS SendNewAttachmentNotificationEmailToProduction.
		/// گیرندگان گروه Sup.OpenOrderRequest.ProductionAttachment (قدیم: Sohrabi.z، golestaneh.a، Eftekhari.a، Estaki.a).
		/// </summary>
		private async Task CreateProductionAttachmentEmailAsync(OpenOrderRequest openOrderRequest, CancellationToken cn)
		{
			var members = await _notificationGroupService.GetGroupMembersAsync("Sup.OpenOrderRequest.ProductionAttachment", cn);
			var to = new List<string>();
			foreach (var member in members)
				AddEmail(to, member.Email);
			if (to.Count == 0)
				return;

			var row = await unitOfWork.Repository<OpenOrderRequest>().TableNoTracking
				.Where(o => o.Id == openOrderRequest.Id)
				.Select(o => new
				{
					o.PurchaseRequestNumber,
					o.OrderNo,
					PartCode = o.Part.Code,
					PartName = o.Part.Name
				})
				.FirstOrDefaultAsync(cn);

			var sb = new StringBuilder();
			sb.AppendLine("بدینوسیله اعلام میگردد، مدرک/مدارک جدیدی جهت درخواست باز، با مشخصات ذیل آپلود گردید");
			sb.AppendLine("<br><br>شما می توانید با مراجعه به سیستم جامع گروه صنعتی هوایار / سیستم تدارکات / عملیات / درخواست های باز / پیوست مدارک از جزییات آن اطلاع یابید<br><br>");
			sb.AppendLine("<ul>");
			sb.AppendLine($"<li>شماره درخواست : <strong>{row?.PurchaseRequestNumber}</strong></li>");
			sb.AppendLine($"<li>شماره سفارش : <strong>{row?.OrderNo}</strong></li>");
			sb.AppendLine($"<li>کد کالا : <strong>{row?.PartCode}</strong></li>");
			sb.AppendLine($"<li>عنوان کالا : <strong>{row?.PartName}</strong></li>");
			sb.AppendLine("</ul>");

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = "اعلام ایجاد پیوست درخواست باز",
				Body = ProductionOrderItemNotificationHelper.BuildEmailShell(sb.ToString()),
				EntityId = openOrderRequest.Id,
				OwnerId = CurrentUserId ?? 1,
				ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={openOrderRequest.Id}",
				IsRead = false,
				IsSend = false,
				ToEmails = to
			}, cn);
			await unitOfWork.SaveChangesAsync(cn);
		}

		private string? ValidateStopRequest(StopOperationRequest request, int currentStopStatus)
		{
			switch (currentStopStatus)
			{
				case 0: // Initial
				case 2815: // Restart
					if (!request.StopTypeEnum.HasValue)
						return "نوع توقف الزامی است";
					if (!request.StopOperatorId.HasValue)
						return "عامل توقف الزامی است";
					if (string.IsNullOrEmpty(request.BeneficiariesIds))
						return "انتخاب ذینفعان الزامی است";
					if (string.IsNullOrEmpty(request.CommentValue))
						return "علت توقف الزامی است";
					break;

				case 2807: // InitialAndSendToResponsible
				case 2813: // WaitingForProjectManager
				case 2816: // RegisterStopAgain
					if (!request.StopCheckingResult.HasValue)
						return "نتیجه بررسی الزامی است";
					if (string.IsNullOrEmpty(request.StopCheckingComment))
						return "توضیحات بررسی الزامی است";
					
					// اگر نیاز به اصلاح BOM است، حداقل یکی از گزینه‌ها باید انتخاب شود
					if (request.StopCheckingResult == OpenOrderRequestCommentStopCheckingResultEnum.NeedsBOMCorrection)
					{
						if (!request.IsNeedToUpdateBom && !request.IsNeedToDeleteBom)
							return "لطفاً حداقل یکی از گزینه‌های 'نیاز به بروزرسانی BOM' یا 'نیاز به حذف BOM' را انتخاب کنید";
					}
					
					// اگر در انتظار مدیر پروژه است، تاریخ تقریبی الزامی است
					if (request.StopCheckingResult == OpenOrderRequestCommentStopCheckingResultEnum.AwaitingProjectManagerOrEmployer)
					{
						if (string.IsNullOrEmpty(request.ProjectManagerApproximateCommentShamsiDate))
							return "تاریخ تقریبی اعمال نظر مدیر پروژه الزامی است";
					}
					break;

				case 2808: // NeedToRestartRequest
				case 2810: // NeedToEdit
				case 2811: // NeedToEditDocuments
				case 2812: // DeleteIndustrialRequest
				case 2821: // SendToSupplyCartable
					if (!request.StopCheckingStatus.HasValue)
						return "وضعیت بررسی الزامی است";
					if (string.IsNullOrEmpty(request.StopCheckingStatusComment))
						return "توضیحات الزامی است";
					
					// اگر ثبت توقف مجدد انتخاب شده، فیلدهای مربوطه الزامی است
					if (request.StopCheckingStatus == OpenOrderRequestStopCheckingStatusEnum.ReRegisterStop)
					{
						if (!request.ReStopTypeEnum.HasValue)
							return "نوع توقف مجدد الزامی است";
						if (!request.ReStopOperatorId.HasValue)
							return "عامل توقف مجدد الزامی است";
						if (string.IsNullOrEmpty(request.ReStopCommentValue))
							return "علت توقف مجدد الزامی است";
					}
					break;
			}

			return null;
		}

		#endregion

		#region VPIS Operations

		[HttpGet("[action]")]
		public async Task<IActionResult> LinkVpisPartial(long openOrderRequestId, CancellationToken cn)
		{
			ViewBag.OpenOrderRequestId = openOrderRequestId;
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_LinkVpisPartial.cshtml");
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetCurrentProjectForVpis(long openOrderRequestId, CancellationToken cn)
		{
			try
			{
				// دریافت پروژه فعلی از VPIS های لینک شده
				var projectId = await unitOfWork.Repository<OpenOrderRequestVpis>()
					.TableNoTracking
					.Where(v => v.OpenOrderRequestId == openOrderRequestId && v.IsLatest)
					.Select(v => v.ProjectId)
					.FirstOrDefaultAsync(cn);

				return Ok(projectId);
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در دریافت پروژه: " + ex.Message);
			}
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetVpisListByProject(long projectId, long openOrderRequestId, CancellationToken cn)
		{
			try
			{
				var projectVpisList = await unitOfWork.Repository<ProjectVpis>()
					.TableNoTracking
					.Include(pv => pv.ProjectName)
					.Where(pv => pv.ProjectNameId == projectId)
					.ToListAsync(cn);

				if (!projectVpisList.Any())
					return Ok(new { vpisList = new List<object>(), linkedDocumentIds = new List<long>() });

				var projectVpisById = projectVpisList
					.Where(pv => pv.Id.HasValue)
					.ToDictionary(pv => pv.Id!.Value);
				var projectVpisIds = projectVpisById.Keys.ToList();

				var allDocuments = await unitOfWork.Repository<Document>()
					.TableNoTracking
					.Include(d => d.Comments)
					.Where(d => d.DocumentVpisId != null && projectVpisIds.Contains(d.DocumentVpisId.Value))
					.ToListAsync(cn);

				var lastSendedDocuments = OpenOrderRequestVpisRules.SelectLatestEligiblePerVpis(
					allDocuments,
					vpisId => projectVpisById.TryGetValue(vpisId, out var vpis) ? vpis.Title : null,
					vpisId => projectVpisById.TryGetValue(vpisId, out var vpis)
						&& vpis.ProjectName?.ProjectIsVendoriType == true);

				if (!lastSendedDocuments.Any())
					return Ok(new { vpisList = new List<object>(), linkedDocumentIds = new List<long>() });

				var vpisList = lastSendedDocuments.Select(d =>
				{
					projectVpisById.TryGetValue(d.DocumentVpisId!.Value, out var vpis);
					return new
					{
						id = d.Id!.Value,
						projectVpisId = d.DocumentVpisId!.Value,
						documentNumber = vpis?.Code,
						documentTitle = vpis?.Title,
						projectName = vpis?.ProjectName?.ProjectName,
						revision = d.Revision,
						documentFullTitle = $"{vpis?.ProjectName?.ProjectName} | {vpis?.Title} | {vpis?.Code} | Ver({d.Revision})"
					};
				}).ToList();

				var linkedDocumentIds = await unitOfWork.Repository<OpenOrderRequestVpis>()
					.TableNoTracking
					.Where(v => v.OpenOrderRequestId == openOrderRequestId && v.IsLatest)
					.Select(v => v.DocumentId)
					.ToListAsync(cn);

				return Ok(new { vpisList, linkedDocumentIds });
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در دریافت لیست VPIS: " + ex.Message);
			}
		}

		/// <summary>HTS 74·HasEngineeringPermission(82) — DoLinkVpisOperation (PermissionAuthorize: FullAccess هم مجاز). D42: گروه 70 هر دو مجوز را داشت.</summary>
		[HttpPost("[action]")]
		[ActionDisplayName("اتصال مدارک پروژه (VPIS)", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> DoLinkVpis(LinkVpisRequest request, CancellationToken cn)
		{
			try
			{
				if (!HasOpenOrderRequestPermission(Roles.HasEngineering, Roles.EngineeringAccept, Roles.SupplyAndPurchase))
					return AccessDenied("شما دسترسی اتصال مدارک پروژه (VPIS) را ندارید");

				if (request.SelectedVpisIds == null || !request.SelectedVpisIds.Any())
					return BadRequest("لطفاً حداقل یک مدرک VPIS انتخاب کنید");

				if (!request.ProjectId.HasValue)
					return BadRequest("لطفاً پروژه را انتخاب کنید");

				var openOrderRequest = await unitOfWork.Repository<OpenOrderRequest>()
					.Table.FirstOrDefaultAsync(x => x.Id == request.OpenOrderRequestId, cn);

				if (openOrderRequest == null)
					return BadRequest("درخواست باز یافت نشد");

				var documents = await unitOfWork.Repository<Document>()
					.TableNoTracking
					.Include(d => d.Comments)
					.Include(d => d.DocumentVpis)
						.ThenInclude(pv => pv!.ProjectName)
					.Include(d => d.Project)
					.Where(d => request.SelectedVpisIds.Contains(d.Id!.Value))
					.ToListAsync(cn);

				if (!documents.Any())
					return BadRequest("مدارک انتخاب شده یافت نشد");

				var ineligible = documents
					.Where(d => !OpenOrderRequestVpisRules.IsEligibleForOpenOrderLink(d, d.DocumentVpis, d.Project ?? d.DocumentVpis?.ProjectName))
					.ToList();
				if (ineligible.Count > 0)
					return BadRequest("برخی مدارک انتخاب‌شده شرط لینک به درخواست باز را ندارند");

				var now = DateTime.Now;
				var currentUserId = CurrentUserId;

				// غیرفعال کردن لینک‌های قبلی
				var oldLinks = await unitOfWork.Repository<OpenOrderRequestVpis>()
					.Table
					.Where(v => v.OpenOrderRequestId == request.OpenOrderRequestId && v.IsLatest)
					.ToListAsync(cn);

				foreach (var oldLink in oldLinks)
				{
					oldLink.IsLatest = false;
				}

				await unitOfWork.SaveChangesAsync(cn);

				// ایجاد لینک‌های جدید
				var newLinks = new List<OpenOrderRequestVpis>();
				foreach (var document in documents)
				{
					newLinks.Add(new OpenOrderRequestVpis
					{
						OpenOrderRequestId = request.OpenOrderRequestId,
						ProjectId = request.ProjectId.Value,
						DocumentId = document.Id,
						ProjectVpisId = document.DocumentVpisId.Value,
						RevisionNumber = document.Revision,
						IsLatest = true,
						Comment = "لینک توسط کاربر"
					});
				}

				foreach (var link in newLinks)
				{
					await unitOfWork.Repository<OpenOrderRequestVpis>().AddAsync(link, cn);
				}

				await unitOfWork.SaveChangesAsync(cn);

				return Ok(new { message = "لینک با موفقیت ثبت شد" });
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در ثبت لینک VPIS: " + ex.Message);
			}
		}



		#endregion

		#region AddRequsted
		[HttpGet("[action]")]
		public async Task<IActionResult> AddRequestedPersonelPartial(long? openOrderRequestId, string? openOrderRequestIds, bool loadExisting, CancellationToken cn)
		{
			var ids = ParseOpenOrderRequestIds(openOrderRequestId, openOrderRequestIds);
			ViewBag.OpenOrderRequestId = ids.FirstOrDefault();

			var staticPersonels = await userService.GetUsersByRoleName("Sup.OpenOrderRequest.ConfigManageStaticPersonel");
			ViewBag.StaticPersonels = staticPersonels?.Select(c => c.Id) ?? [];

			var requestedPersonelIds = new List<long>();
			var requestedEngineeringPersonelIds = new List<long>();
			if (loadExisting && ids.Count > 0)
			{
				var rows = await unitOfWork.Repository<OpenOrderRequest>().TableNoTracking
					.Where(x => x.Id != null && ids.Contains(x.Id.Value))
					.Select(x => new { x.RequestedPersonelIds, x.RequestedEngineeringPersonelIds })
					.ToListAsync(cn);

				requestedPersonelIds = rows
					.SelectMany(r => r.RequestedPersonelIds ?? new List<long>())
					.Distinct()
					.ToList();
				requestedEngineeringPersonelIds = rows
					.SelectMany(r => r.RequestedEngineeringPersonelIds ?? new List<long>())
					.Distinct()
					.ToList();
			}

			ViewBag.RequestedPersonelIds = requestedPersonelIds.Count == 0 ? null : string.Join(",", requestedPersonelIds);
			ViewBag.RequestedEngineeringPersonelIds = requestedEngineeringPersonelIds.Count == 0 ? null : string.Join(",", requestedEngineeringPersonelIds);
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_AddRequestedPersonelPartial.cshtml");
		}

		private static List<long> ParseOpenOrderRequestIds(long? openOrderRequestId, string? openOrderRequestIds)
		{
			var ids = new List<long>();
			if (openOrderRequestId is > 0)
				ids.Add(openOrderRequestId.Value);

			if (string.IsNullOrWhiteSpace(openOrderRequestIds))
				return ids;

			foreach (var part in openOrderRequestIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				if (long.TryParse(part, out var id) && id > 0 && !ids.Contains(id))
					ids.Add(id);
			}

			return ids;
		}

		/// <summary>HTS صفحه 76 «تنظیمات درخواست های باز» — DoOperation پرسنل (FullAccess صفحه 76: گروه‌های 430 و 28).</summary>
		[HttpPost("[action]")]
		[ActionDisplayName("افزودن درخواست‌کنندگان و ذینفعان", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> AddRequestedPersonelToOpenRequest(
    [FromBody] AddRequestedPersonel request,
    CancellationToken cancellationToken)
		{
			try
			{
				if (!HasOpenOrderRequestPermission(Roles.ConfigManage, Roles.Industrial, Roles.IndustriesLegacy, Roles.SupplyAndPurchase))
					return AccessDenied("شما دسترسی افزودن درخواست‌کنندگان و ذینفعان را ندارید");

				// 1. اعتبارسنجی ورودی
				if (request.OpenOrderRequestIds == null || request.OpenOrderRequestIds.Length == 0)
					return BadRequest("هیچ شناسه‌ای برای درخواست باز ارسال نشده است.");

				// 2. بارگذاری تمام رکوردهای OpenOrderRequest مربوطه
				var openOrderRequests = await unitOfWork
				    .Repository<OpenOrderRequest>()
				    .Table
				    .Where(x => request.OpenOrderRequestIds.Contains(x.Id.Value))
				    .ToListAsync(cancellationToken);

				// 3. بررسی وجود تمام رکوردها
				if (openOrderRequests.Count != request.OpenOrderRequestIds.Length)
					return BadRequest("برخی از درخواست‌های باز یافت نشدند.");

				var personelIds = request.RequestedPersonelIds ?? new List<long>();
				var engineeringIds = request.RequestedEngineeringPersonelIds ?? new List<long>();
				var updatePersonel = request.ReplaceExisting || personelIds.Count > 0;
				var updateEngineering = request.ReplaceExisting || engineeringIds.Count > 0;

				// 4. بارگذاری کاربران مورد نیاز (با حذف تکراری‌ها)
				var allPersonelIds = personelIds
				    .Concat(engineeringIds)
				    .Distinct()
				    .ToArray();

				var users = allPersonelIds.Length == 0
					? new Dictionary<long?, User>()
					: await userService.TableNoTracking
						.Where(u => allPersonelIds.Contains(u.Id.Value))
						.ToDictionaryAsync(u => u.Id, cancellationToken);

				// 5. به‌روزرسانی هر رکورد — با ReplaceExisting لیست ذخیره‌شده جایگزین می‌شود (حذف اسم هم می‌ماند)
				foreach (var openOrderRequest in openOrderRequests)
				{
					if (updateEngineering)
						ApplyRequestedPersonel(openOrderRequest, engineeringIds, users, engineering: true);

					if (updatePersonel)
						ApplyRequestedPersonel(openOrderRequest, personelIds, users, engineering: false);

					if (personelIds.Count > 0 || engineeringIds.Count > 0)
						openOrderRequest.RequestedPersonelRegShamsiDate ??= DateTime.Now.ToShamsiDate();
				}

				// 6. ذخیره‌سازی یکپارچه
				await unitOfWork.SaveChangesAsync(cancellationToken);

				return Ok(new
				{
					message = "اطلاعات پرسنل با موفقیت ثبت شد.",
					updatedCount = openOrderRequests.Count
				});
			}
			catch (Exception ex)
			{
				// لاگ‌گیری مناسب (ترجیحاً با ILogger)
				return StatusCode(500, new
				{
					message = "خطا در ثبت اطلاعات پرسنل.",
					detail = ex.Message
				});
			}
		}

		#endregion

		#region SendToSupplier / SetSelectedSupplier (HTS صفحه 76)

		[HttpGet("[action]")]
		[ActionDisplayName("فرم ارسال به پیمانکار", ActionAccessType.View, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SendToSupplierPartial(long openOrderRequestId, string? ids, CancellationToken cn)
		{
			if (!CanSendToSupplier)
				return AccessDenied("شما دسترسی ارسال به پیمانکار را ندارید");

			// لیست درخواست‌هایی که واقعاً ارسال می‌شوند (جلوگیری از ارسال ناخواسته ردیف‌های انتخاب‌شدهٔ قبلی)
			var requestIds = (ids ?? string.Empty)
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(s => long.TryParse(s, out var v) ? v : 0)
				.Where(v => v > 0)
				.Distinct()
				.ToList();
			if (requestIds.Count == 0 && openOrderRequestId > 0)
				requestIds.Add(openOrderRequestId);

			var rows = await unitOfWork.Repository<OpenOrderRequest>().TableNoTracking
				.Where(o => requestIds.Contains(o.Id!.Value))
				.Select(o => new SendToSupplierPreviewRow
				{
					PurchaseRequestNumber = o.PurchaseRequestNumber,
					PartCode = o.Part.Code,
					PartName = o.Part.Name,
					Project = o.OrderItemComment,
					ProductionOrderNumber = o.ProductionOrderNumber
				})
				.ToListAsync(cn);

			ViewBag.PreviewRows = rows;
			ViewBag.OpenOrderRequestId = openOrderRequestId;
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_SendToSupplierPartial.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("فرم تامین‌کننده منتخب", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult SetSelectedSupplierPartial(long openOrderRequestId)
		{
			if (!CanSetSelectedSupplier)
				return AccessDenied("شما دسترسی تعیین تامین‌کننده منتخب را ندارید");

			ViewBag.OpenOrderRequestId = openOrderRequestId;
			return PartialView(@"\Views\Panel\Sup\OpenOrderRequest\_SetSelectedSupplierPartial.cshtml");
		}

		/// <summary>
		/// HTS صفحه 76 · SendEmailToContractor (Permission SendEmail=27).
		/// صف Notification Type=Email (Q10: بدون SMTP) + یک ردیف پیشینه به ازای هر درخواست + Changed=false و ManCompanyId.
		/// </summary>
		[HttpPost("[action]")]
		[ActionDisplayName("ارسال به پیمانکار", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SendToSupplier(SendToSupplierRequest request, CancellationToken cn)
		{
			try
			{
				if (!CanSendToSupplier)
					return AccessDenied("شما دسترسی ارسال به پیمانکار را ندارید");

				if (request.OpenOrderRequestIds == null || request.OpenOrderRequestIds.Length == 0)
					return BadRequest("هیچ درخواستی انتخاب نشده است");

				if (request.SupplierId <= 0)
					return BadRequest("تامین‌کننده را انتخاب کنید");

				if (request.SendingCount is not > 0)
					return BadRequest("تعداد سفارش را وارد کنید");

				var supplier = await unitOfWork.Repository<Supplier>().Table
					.Include(s => s.Party)
					.FirstOrDefaultAsync(s => s.Id == request.SupplierId, cn);
				if (supplier == null)
					return BadRequest("تامین‌کننده یافت نشد");

				var toEmail = FirstNonEmptyEmail(supplier.Email, supplier.Party?.Email);
				if (!toEmail.HasValue())
					return BadRequest("ایمیل پیمانکار خالی است");

				var ids = request.OpenOrderRequestIds.Distinct().ToArray();
				var openOrderRequests = await unitOfWork.Repository<OpenOrderRequest>().Table
					.Include(o => o.Part)
						.ThenInclude(p => p.BuyCategory)
							.ThenInclude(b => b!.PurchaseResponsible)
					.Where(o => ids.Contains(o.Id!.Value))
					.ToListAsync(cn);

				if (openOrderRequests.Count != ids.Length)
					return BadRequest("برخی از درخواست‌های باز یافت نشدند");

				var now = DateTime.Now;
				var senderId = CurrentUserId;
				var companyName = supplier.Party?.FullName ?? supplier.Prefix ?? toEmail;
				var subject = "سفارش ساخت - لطفا Replay نفرمایید";

				var priorSends = await unitOfWork.Repository<OpenOrderRequestEmailToSupplier>().TableNoTracking
					.Where(e => ids.Contains(e.OpenOrderRequestId))
					.Select(e => new { e.OpenOrderRequestId, e.SupplierId })
					.ToListAsync(cn);

				var bodyRows = new List<SendToSupplierBodyRow>();
				var notificationFileIds = new List<long>();
				var historyRows = new List<OpenOrderRequestEmailToSupplier>();

				foreach (var item in openOrderRequests)
				{
					var letterCount = item.ManCompanyId is long currentCompanyId
						? priorSends.Count(p => p.OpenOrderRequestId == item.Id && p.SupplierId == currentCompanyId)
						: 0;

					var packs = await CollectSendToSupplierAttachmentPacksAsync(item.Id!.Value, item.PartId, cn);
					foreach (var fileId in packs.SelectMany(p => p.FileIds))
					{
						if (!notificationFileIds.Contains(fileId))
							notificationFileIds.Add(fileId);
					}

					var buyer = item.Part?.BuyCategory?.PurchaseResponsible;
					var itemComment = item.Part?.Code?.StartsWith("14750") == true ? item.OrderItemComment : null;
					bodyRows.Add(new SendToSupplierBodyRow
					{
						PurchaseRequestNumber = item.PurchaseRequestNumber,
						OrderNo = item.OrderNo,
						ProductionOrderNumber = item.ProductionOrderNumber,
						PartCode = item.Part?.Code,
						PartName = item.Part?.Name,
						Project = item.OrderItemComment,
						NeedDate = item.NeedDateShamsiDate,
						BuyerName = buyer?.NameFa.HasValue() == true ? buyer.NameFa : buyer?.Name,
						LetterVersion = letterCount + 1,
						ItemComment = itemComment,
						AttachmentComments = string.Join(" - ", packs.Select(p => p.Comment).Where(c => c.HasValue()))
					});

					var lastPack = packs.Count == 0 ? null : packs[^1];
					historyRows.Add(new OpenOrderRequestEmailToSupplier
					{
						OpenOrderRequestId = item.Id!.Value,
						SupplierId = request.SupplierId,
						SendMiladiDateTime = now,
						SendShamsiDateTime = now.ToShamsiDateTime(),
						SenderUserId = senderId,
						ToEmail = toEmail,
						Subject = subject,
						Comment = lastPack == null ? "ارسال شده بدون فایل پیوست" : null,
						SendingCount = request.SendingCount,
						AttachmentId = lastPack?.OpenOrderAttachmentFileId,
						PartAttachmentId = lastPack?.PartAttachmentFileId,
						EdmsDocumentAttachmentId = lastPack?.EdmsDocumentFileId
					});

					item.Changed = false;
					item.ManCompanyId = request.SupplierId;
				}

				var body = BuildSendToSupplierBody(bodyRows, companyName, request.SendingCount);
				foreach (var history in historyRows)
					history.Body = body;

				var toEmails = new List<string>();
				AddEmail(toEmails, toEmail);
				foreach (var item in openOrderRequests)
				{
					var buyer = item.Part?.BuyCategory?.PurchaseResponsible;
					AddEmail(toEmails, buyer?.Email.HasValue() == true ? buyer.Email : buyer?.Username);
				}

				var senderIsBagheri = string.Equals(CurrentUserName, "bagheri.h", StringComparison.OrdinalIgnoreCase);
				if (senderIsBagheri || toEmails.Any(e => e.Contains("bagheri.h", StringComparison.OrdinalIgnoreCase)))
					AddEmail(toEmails, "bagheri.h@havayar.com");
				else
				{
					AddEmail(toEmails, "sharifi.b@havayar.com");
					AddEmail(toEmails, "Ashrafi.m@havayar.com");
					AddEmail(toEmails, "Abdollahi.a@havayar.com");
				}

				if (toEmails.Any(e => e.Contains("bagheri.h@", StringComparison.OrdinalIgnoreCase)))
				{
					AddEmail(toEmails, "Shahpordeli.s@havayar.com");
					AddEmail(toEmails, "Sohrabi.za@havayar.com");
				}
				else if (toEmails.Any(e => e.Contains("shahpordeli.s@", StringComparison.OrdinalIgnoreCase)))
				{
					AddEmail(toEmails, "bagheri.h@havayar.com");
					AddEmail(toEmails, "Sohrabi.za@havayar.com");
				}
				else if (toEmails.Any(e => e.Contains("sohrabi.za@", StringComparison.OrdinalIgnoreCase)))
				{
					AddEmail(toEmails, "bagheri.h@havayar.com");
					AddEmail(toEmails, "Shahpordeli.s@havayar.com");
				}

				var ccEmails = await CollectSendToSupplierCcEmails(openOrderRequests, cn);

				if (openOrderRequests.Any(o => o.Part?.BuyCategory?.Title?.Contains("تابلو برق") == true))
				{
					RemoveEmailLocalParts(toEmails, "sharifi.b", "mohammadzadeh.m", "maroufkhani.m", "masoumi.s", "abdollahi.a", "mehrafzoon");
					RemoveEmailLocalParts(ccEmails, "sharifi.b", "ashrafi.m", "maroufkhani.m", "masoumi.s", "abdollahi.a", "mehrafzoon");
				}

				foreach (var history in historyRows)
					await unitOfWork.Repository<OpenOrderRequestEmailToSupplier>().AddAsync(history, cn);

				await unitOfWork.Repository<Notification>().AddAsync(new Notification
				{
					Type = NotificationType.Email,
					Title = subject,
					Body = body,
					EntityId = openOrderRequests[0].Id,
					OwnerId = senderId ?? 1,
					ViewPath = "/Panel/Sup/OpenOrderRequest/List",
					ToEmails = toEmails,
					CcEmails = ccEmails.Count == 0 ? null : ccEmails,
					AttachmentFileIds = notificationFileIds.Count == 0 ? null : notificationFileIds,
					IsRead = false
				}, cn);

				await unitOfWork.SaveChangesAsync(cn);

				return Ok(new
				{
					message = "ارسال به پیمانکار در صف اعلان ثبت شد",
					sentCount = openOrderRequests.Count,
					supplierId = request.SupplierId,
					attachmentCount = notificationFileIds.Count
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در ارسال به پیمانکار: " + ex.Message);
			}
		}

		/// <summary>
		/// معادل HTS <c>OpenOrderRequestAttachmentService.GetAttachmentModel(List)</c> که ارسال به پیمانکار صدا می‌زند:
		/// پیوست درخواست باز (غیر از «Add automatically by system»)،
		/// آخرین نسخهٔ اصلی مدارک کالا از انواع Data Sheet / Detail Drawing / Wiring Diagram / Technical Documents،
		/// و فایل اصلی مدرک مهندسی آخرین VPIS، اگر همان شرط قابل‌قبول بودن پیوست مدارک درخواست باز را داشته باشد
		/// و پروژه خاتمه‌یافته یا لغوشده نباشد (HTS وضعیت ۵ و ۶).
		/// </summary>
		private async Task<List<SendToSupplierAttachmentPack>> CollectSendToSupplierAttachmentPacksAsync(
			long openOrderRequestId,
			long partId,
			CancellationToken cn)
		{
			var packs = new List<SendToSupplierAttachmentPack>();

			var oorAtts = await unitOfWork.Repository<OpenOrderRequestAttachment>().TableNoTracking
				.Where(a => a.OpenOrderRequestId == openOrderRequestId
					&& a.AttachmentId != null
					&& (a.Comment == null || a.Comment != "Add automatically by system"))
				.Select(a => new { a.AttachmentId, a.Comment })
				.ToListAsync(cn);

			foreach (var a in oorAtts)
			{
				packs.Add(new SendToSupplierAttachmentPack
				{
					OpenOrderAttachmentFileId = a.AttachmentId,
					Comment = a.Comment,
					FileIds = a.AttachmentId.HasValue ? [a.AttachmentId.Value] : []
				});
			}

			var partDocTypes = new[]
			{
				PartDocumentTypeEnum.Data_Sheet,
				PartDocumentTypeEnum.Detail_Drawing,
				PartDocumentTypeEnum.Wiring_Diagram,
				PartDocumentTypeEnum.Technical_Documents
			};

			var partDocsRaw = await unitOfWork.Repository<PartDocument>().TableNoTracking
				.Where(d => d.PartId == partId
					&& d.Main == true
					&& d.AttachmentId > 0
					&& d.Type != null
					&& partDocTypes.Contains(d.Type.Value))
				.ToListAsync(cn);

			var partDocs = partDocsRaw
				.GroupBy(d => d.Type)
				.Select(g => g.OrderByDescending(x => x.Id).First())
				.ToList();

			foreach (var doc in partDocs)
			{
				packs.Add(new SendToSupplierAttachmentPack
				{
					PartAttachmentFileId = doc.AttachmentId,
					Comment = doc.Comment,
					FileIds = [doc.AttachmentId]
				});
			}

			var vpisLinks = await unitOfWork.Repository<OpenOrderRequestVpis>().TableNoTracking
				.Include(v => v.ProjectVpis)
					.ThenInclude(pv => pv.ProjectName)
				.Include(v => v.Project)
				.Include(v => v.Document)
					.ThenInclude(d => d!.Comments)
				.Where(v => v.OpenOrderRequestId == openOrderRequestId && v.IsLatest && v.DocumentId != null)
				.ToListAsync(cn);

			foreach (var link in vpisLinks)
			{
				if (!IsSendToSupplierEngineeringDocument(link))
					continue;
				if (link.Document?.MainFileId is not long mainFileId || mainFileId <= 0)
					continue;

				packs.Add(new SendToSupplierAttachmentPack
				{
					EdmsDocumentFileId = mainFileId,
					Comment = link.Comment,
					FileIds = [mainFileId]
				});
			}

			return packs;
		}

		/// <summary>
		/// فایل اصلی مدرک آخرین VPIS فقط وقتی به ایمیل پیمانکار می‌رود که
		/// همان شرط قابل‌قبول بودن پیوست مدارک درخواست باز را داشته باشد
		/// و پروژه خاتمه‌یافته یا لغوشده نباشد.
		/// </summary>
		private static bool IsSendToSupplierEngineeringDocument(OpenOrderRequestVpis link)
		{
			if (!OpenOrderRequestVpisRules.IsEligibleForOpenOrderLink(link.Document, link.ProjectVpis, link.Project))
				return false;

			var project = link.ProjectVpis?.ProjectName ?? link.Project;
			if (project == null)
				return false;

			return project.Status != ProjectStatusEnum.Completed
				&& project.Status != ProjectStatusEnum.Canceled;
		}

		private sealed class SendToSupplierAttachmentPack
		{
			public long? OpenOrderAttachmentFileId { get; set; }
			public long? PartAttachmentFileId { get; set; }
			public long? EdmsDocumentFileId { get; set; }
			public string? Comment { get; set; }
			public List<long> FileIds { get; set; } = [];
		}

		/// <summary>HTS AddComponyMansToRequests — فقط ManCompanyId.</summary>
		[HttpPost("[action]")]
		[ActionDisplayName("تامین‌کننده منتخب", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SetSelectedSupplier(SetSelectedSupplierRequest request, CancellationToken cn)
		{
			try
			{
				if (!CanSetSelectedSupplier)
					return AccessDenied("شما دسترسی تعیین تامین‌کننده منتخب را ندارید");

				if (request.OpenOrderRequestIds == null || request.OpenOrderRequestIds.Length == 0)
					return BadRequest("هیچ درخواستی انتخاب نشده است");

				if (request.SupplierId <= 0)
					return BadRequest("تامین‌کننده را انتخاب کنید");

				var supplierExists = await unitOfWork.Repository<Supplier>().TableNoTracking
					.AnyAsync(s => s.Id == request.SupplierId, cn);
				if (!supplierExists)
					return BadRequest("تامین‌کننده یافت نشد");

				var ids = request.OpenOrderRequestIds.Distinct().ToArray();
				var openOrderRequests = await unitOfWork.Repository<OpenOrderRequest>().Table
					.Where(o => ids.Contains(o.Id!.Value))
					.ToListAsync(cn);

				if (openOrderRequests.Count != ids.Length)
					return BadRequest("برخی از درخواست‌های باز یافت نشدند");

				foreach (var item in openOrderRequests)
					item.ManCompanyId = request.SupplierId;

				await unitOfWork.SaveChangesAsync(cn);

				return Ok(new
				{
					message = "تامین‌کننده منتخب ثبت شد",
					updatedCount = openOrderRequests.Count
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در ثبت تامین‌کننده منتخب: " + ex.Message);
			}
		}

		private static string? FirstNonEmptyEmail(params string?[] values)
			=> values.FirstOrDefault(v => v.HasValue())?.Trim();

		private async Task<List<string>> CollectSendToSupplierCcEmails(List<OpenOrderRequest> requests, CancellationToken cn)
		{
			var cc = new List<string> { "Maroufkhani.m@havayar.com" };
			var userIds = requests
				.SelectMany(r => new[] { r.SalesUnitSalesExpertId, r.SalesUnitSalesManagerId, r.SalesUnitProjectManagerId })
				.Where(id => id.HasValue)
				.Select(id => id!.Value)
				.Distinct()
				.ToList();

			if (userIds.Count == 0)
				return cc;

			var users = await userService.TableNoTracking
				.Where(u => userIds.Contains(u.Id!.Value))
				.Select(u => new { u.Email, u.Username })
				.ToListAsync(cn);

			foreach (var user in users)
				AddEmail(cc, user.Email.HasValue() ? user.Email : user.Username);

			return cc;
		}

		private static void RemoveEmailLocalParts(List<string> emails, params string[] localParts)
		{
			emails.RemoveAll(email => localParts.Any(part =>
				email.Contains(part, StringComparison.OrdinalIgnoreCase)));
		}

		public sealed class SendToSupplierPreviewRow
		{
			public long PurchaseRequestNumber { get; set; }
			public string? PartCode { get; set; }
			public string? PartName { get; set; }
			public string? Project { get; set; }
			public int? ProductionOrderNumber { get; set; }
		}

		private sealed class SendToSupplierBodyRow
		{
			public string? Project { get; set; }
			public long PurchaseRequestNumber { get; set; }
			public long? OrderNo { get; set; }
			public int? ProductionOrderNumber { get; set; }
			public string? PartCode { get; set; }
			public string? PartName { get; set; }
			public string? NeedDate { get; set; }
			public string? BuyerName { get; set; }
			public int LetterVersion { get; set; }
			public string? ItemComment { get; set; }
			public string? AttachmentComments { get; set; }
		}

		private static string BuildSendToSupplierBody(List<SendToSupplierBodyRow> rows, string companyName, decimal? sendingCount)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<div style='margin: 0 auto; width: 100%;direction:rtl; font-family:tahoma; font-size:9pt'>");
			sb.AppendLine("<b>با سلام و احترام</b>");
			sb.AppendLine("<br><br><br>");
			sb.AppendLine("از : گروه صنعتی هوایار<br/><br/>");
			sb.AppendLine("به : " + companyName);
			sb.AppendLine("<br><br>");
			sb.AppendLine("<table border='1' title='سفارش ساخت' style='direction: rtl;width:100%;border-color: whitesmoke; border-width: 1px;font-family:tahoma; font-size:9pt'>");
			sb.AppendLine("<tr style='background-color: #e0e0e0'>");
			sb.AppendLine("<td style='width: 150px;'>ش درخواست</td>");
			sb.AppendLine("<td style='width: 150px;'>ش سفارش</td>");
			sb.AppendLine("<td style='width: 150px;'>ش سفارش ساخت</td>");
			sb.AppendLine("<td style='width: 100px;'>کد کالا</td>");
			sb.AppendLine("<td style='width: 400px;'>عنوان کالا</td>");
			sb.AppendLine("<td style='width: 250px;'>پروژه</td>");
			sb.AppendLine("<td style='width: 120px;'>تعداد درخواست</td>");
			sb.AppendLine("<td style='width: 100px;'>تاریخ نیاز</td>");
			sb.AppendLine("<td style='width: 100px;'>مسئول خرید</td>");
			sb.AppendLine("<td style='width: 75px;'>نسخه نامه</td>");
			sb.AppendLine("<td>توضیحات</td>");
			sb.AppendLine("</tr>");

			var qty = sendingCount?.ToString() ?? "";
			foreach (var item in rows)
			{
				sb.AppendLine("<tr>");
				sb.AppendLine("<td>" + item.PurchaseRequestNumber + "</td>");
				sb.AppendLine("<td>" + (item.OrderNo?.ToString() ?? "") + "</td>");
				sb.AppendLine("<td>" + (item.ProductionOrderNumber?.ToString() ?? "") + "</td>");
				sb.AppendLine("<td>" + (item.PartCode ?? "") + "</td>");
				sb.AppendLine("<td>" + (item.PartName ?? "") + "</td>");
				sb.AppendLine("<td>" + System.Net.WebUtility.HtmlEncode(item.Project ?? "") + "</td>");
				sb.AppendLine("<td>" + qty + "</td>");
				sb.AppendLine("<td style='direction:ltr'>" + (item.NeedDate ?? "") + "</td>");
				sb.AppendLine("<td>" + (item.BuyerName ?? "") + "</td>");
				sb.AppendLine("<td>" + item.LetterVersion + "</td>");
				sb.AppendLine("<td>" + (item.ItemComment ?? "") + "</td>");
				sb.AppendLine("</tr>");

				if (item.AttachmentComments.HasValue())
				{
					sb.AppendLine("<tr>");
					sb.AppendLine("<td colspan='11'><strong>علت تغییر/بارگذاری : </strong>" + item.AttachmentComments + "</td>");
					sb.AppendLine("</tr>");
				}
			}

			sb.AppendLine("</table>");
			sb.AppendLine("<br><br><br>");
			sb.AppendLine("پیمانکار متعهد می باشد تا در طول مدت گارانتی ، در صورت بروز برخی خرابی در اقلام مورد سفارش، نسبت به تعویض و یا تعمیر اقلام (در صورت تایید کارفرما) اقدام نماید <br> و در غیر اینصورت کلیه خسارت های وارده به کارفرما ناشی از عدم اجرای این بند توسط پیمانکار می باشد،<br> از ضمانت های پیمانکار که نزد کارفرما موجود است ، به میزان مصوب برداشت خواهد شد. ،");
			sb.AppendLine("ضمنا هزینه های دوره گارانتی بعهده پیمانکار می باشد.");
			sb.AppendLine("<br><br>");
			sb.AppendLine("لطفا در صورت عدم امکان تحویل سفارش در تاریخ نیاز اعلام شده، مراتب را حداکثر چهار ساعت پس از دریافت درخواست با مسئول خرید اعلام فرمایید <br> و عدم اعلام شما به منزله توافق با تاریخ نیاز اعلام شده می باشد.");
			sb.AppendLine("<br><br>");
			sb.AppendLine("**** لطفا این ایمیل را Replay نفرمایید ****");
			sb.AppendLine("</div>");
			return sb.ToString();
		}

		#endregion

		#region Request DTOs

		public class SendToSupplierRequest
		{
			public long[] OpenOrderRequestIds { get; set; } = [];
			public long SupplierId { get; set; }
			public string? Message { get; set; }
			public decimal? SendingCount { get; set; }
		}

		public class SetSelectedSupplierRequest
		{
			public long[] OpenOrderRequestIds { get; set; } = [];
			public long SupplierId { get; set; }
		}

		public class AddRequestedPersonel
		{
			public long[] OpenOrderRequestIds { get; set; }
			public List<long> RequestedPersonelIds { get; set; } = new();
			public List<long> RequestedEngineeringPersonelIds { get; set; } = new();
			/// <summary>وقتی مودال اسامی ذخیره‌شده را نشان می‌دهد، لیست خالی هم باید نام‌ها را پاک کند.</summary>
			public bool ReplaceExisting { get; set; }
		}

		private static void ApplyRequestedPersonel(OpenOrderRequest row, List<long> ids, Dictionary<long?, User> users, bool engineering)
		{
			var selected = ids
				.Where(id => users.ContainsKey(id))
				.Select(id => users[id])
				.ToList();
			var names = selected.Count == 0 ? null : string.Join("، ", selected.Select(u => u.NameFa));
			var emails = selected.Count == 0
				? null
				: string.Join("; ", selected.Select(u => u.Email).Where(e => !string.IsNullOrWhiteSpace(e)));

			if (engineering)
			{
				row.RequestedEngineeringPersonelIds = ids;
				row.RequestedEngineeringPersonel = names;
				row.RequestedEngineeringPersonelEmail = emails;
			}
			else
			{
				row.RequestedPersonelIds = ids;
				row.RequestedPersonel = names;
				row.RequestedPersonelEmail = emails;
			}
		}
		public class SaveCommentRequest
		{
			public long OpenOrderRequestId { get; set; }
			public string? CommentShamsiDate { get; set; }
			public string? CommentValue { get; set; }
		}

		public class SalesOrProjectAcceptRequest
		{
			public long OpenOrderRequestId { get; set; }
			public string? SalesUnitConfirmationComment { get; set; }
		}

		public class EngineeringAcceptBulkRequest
		{
			public List<long>? Ids { get; set; }
		}

		public class EngineeringAcceptBulkResult
		{
			public int AcceptedCount { get; set; }
			public List<long> AcceptedIds { get; set; } = [];
			public List<EngineeringAcceptBulkFailure> Failed { get; set; } = [];
		}

		public class EngineeringAcceptBulkFailure
		{
			public long Id { get; set; }
			public string? Message { get; set; }
		}

		public class StopOperationRequest
		{
			public long OpenOrderRequestId { get; set; }
			public int? StopTypeEnum { get; set; }
			public long? StopOperatorId { get; set; }
			public string? BeneficiariesIds { get; set; }
			public string? CommentValue { get; set; }
			public long? AttachmentId { get; set; }
			
			// Section 2 fields
			public OpenOrderRequestCommentStopCheckingResultEnum? StopCheckingResult { get; set; }
			public string? StopCheckingComment { get; set; }
			public string? StopCheckingDelayReasonText { get; set; }
			public bool IsNeedToUpdateBom { get; set; }
			public bool IsNeedToDeleteBom { get; set; }
			public string? ProjectManagerApproximateCommentShamsiDate { get; set; }
			public long? AlternativeAttachmentId { get; set; }
			
			// Section 3 fields
			public OpenOrderRequestStopCheckingStatusEnum? StopCheckingStatus { get; set; }
			public string? StopCheckingStatusComment { get; set; }
			
			// Re-stop fields
			public int? ReStopTypeEnum { get; set; }
			public long? ReStopOperatorId { get; set; }
			public string? ReStopCommentValue { get; set; }
		}

		public class SaveAttachmentRequest
		{
			public long? Id { get; set; }
			public long OpenOrderRequestId { get; set; }
			public OpenOrderRequestAttachmentFileTypeEnum? FileType { get; set; }
			public string? Comment { get; set; }
			public long? AttachmentId { get; set; }
		}

		public class LinkVpisRequest
		{
			public long OpenOrderRequestId { get; set; }
			public long? ProjectId { get; set; }
			public List<long>? SelectedVpisIds { get; set; }
		}

		#endregion
	}
}
