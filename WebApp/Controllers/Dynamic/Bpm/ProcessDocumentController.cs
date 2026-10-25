using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App.Bpm;
using Entities.App.Bpm.Enums;
using Entities.Auth;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using Services.FileServices;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Bpm/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("سند فرآیندی", typeof(ProcessDocument))]
	public partial class ProcessDocumentController(
		IUnitOfWork unitOfWork,
		IWebHostEnvironment _webHostEnvironment,
		IFileService fileService,
		IUserService userService,
		IOnlineUserService onlineUserService) : BaseController
	{
		//Seed Role => Bpm.ProcessDocument.ShowAll => اسناد فرآیندی - نمایش همه درخواست‌ها
		private const string RoleShowAll = "Bpm.ProcessDocument.ShowAll";
		//Seed Role => Bpm.ProcessDocument.Supervisor => اسناد فرآیندی - سرپرست سیستم‌ها و روش‌ها (معادل گروه ۳۵۳)
		private const string RoleSupervisor = "Bpm.ProcessDocument.Supervisor";
		//Seed Role => Bpm.ProcessDocument.Expert => اسناد فرآیندی - کارشناس سیستم‌ها و روش‌ها (معادل گروه ۳۵۴)
		private const string RoleExpert = "Bpm.ProcessDocument.Expert";
		//Seed Role => Bpm.ProcessDocument.ViewManage => مشاهده مدیریت اسناد (معادل گروه ۴۱۸)
		private const string RoleViewManage = "Bpm.ProcessDocument.ViewManage";
		//Seed Role => Bpm.ProcessDocument.ViewAllAttachments => مشاهده همه پیوست‌ها (معادل ۱۴۳)
		private const string RoleViewAllAttachments = "Bpm.ProcessDocument.ViewAllAttachments";

		private const long MaxFileSizeBytes = 20L * 1024 * 1024;

		private const string MsgNoFileSelected = "فایلی انتخاب نشده است";
		private const string MsgFileTooLarge = "حجم فایل ارسالی از حداکثر حجم مجاز بیشتر است";
		private static string MsgEmpty(string caption) => $"({caption}) نمیتواند خالی باشد";
		private static string MsgInvalid(string caption) => $"مقدار فیلد ({caption}) نامعتبر است.";

		private static readonly ProcessDocumentStatusEnum[] RequesterEditableStatuses =
		[
			ProcessDocumentStatusEnum.Issue,
			ProcessDocumentStatusEnum.Updated,
			ProcessDocumentStatusEnum.Reject
		];

		private static readonly ProcessDocumentTypeEnum[] HiddenTypesOnCreate =
		[
			ProcessDocumentTypeEnum.BPM,
			ProcessDocumentTypeEnum.QM,
			ProcessDocumentTypeEnum.QP
		];

		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ProcessDocument processDocument, CancellationToken cn)
		{
			if (processDocument.Id == null || processDocument.Id == 0)
				return await Add(processDocument, cn);

			var exist = await unitOfWork.Repository<ProcessDocument>().TableNoTracking.AnyAsync(c => c.Id == processDocument.Id, cn);
			return exist ? await Update(processDocument, cn) : await Add(processDocument, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(ProcessDocument processDocument, CancellationToken cn)
		{
			var entity = CreateNew();
			CopyRequesterFields(entity, processDocument);
			ClearManageFields(entity);
			entity.Id = null;
			entity.HtsId = 0;
			entity.LastStatus = null;
			entity.LastStatusUserId = null;
			if (CurrentOrganizationUnitId is > 0)
				entity.CreatedOrganizationUnitId = CurrentOrganizationUnitId.Value;

			var validation = await ValidateRequesterAsync(entity, isNew: true, cn);
			if (validation != null)
				return BadRequest(validation);

			var saved = await unitOfWork.Repository<ProcessDocument>().SaveAsync(entity, cn, true);
			return Ok(await LoadForEditAsync(saved.Id!.Value, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ProcessDocument processDocument, CancellationToken cn)
		{
			if (processDocument.Id is null or 0)
				return BadRequest("درخواست یافت نشد");

			var existing = await unitOfWork.Repository<ProcessDocument>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == processDocument.Id, cn);
			if (existing == null)
				return BadRequest("درخواست یافت نشد");

			if (!CanViewRequester(existing))
				return BadRequest("دسترسی به این درخواست را ندارید");
			if (!CanEditRequester(existing))
				return BadRequest("ویرایش این درخواست در وضعیت فعلی مجاز نیست");

			CopyRequesterFields(existing, processDocument);

			var validation = await ValidateRequesterAsync(existing, isNew: false, cn);
			if (validation != null)
				return BadRequest(validation);

			await unitOfWork.Repository<ProcessDocument>().UpdateAsync(existing, cn, true);
			await AddStatusLogAsync(existing.Id!.Value, ProcessDocumentStatusEnum.Updated, cn);
			return Ok(await LoadForEditAsync(existing.Id.Value, cn));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			return await CancelRequestCore(id, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("لغو درخواست", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> CancelRequest(long id, CancellationToken cn)
		{
			return await CancelRequestCore(id, cn);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			if (id is null or 0)
			{
				var created = CreateNew();
				FillViewBag(created, isNew: true);
				return View(@"\Views\Panel\Bpm\ProcessDocument\Edit.cshtml", created);
			}

			var entity = await LoadForEditAsync(id.Value, cn);
			if (entity.Id is null or 0)
				throw new Exception("درخواست یافت نشد");
			if (!CanViewRequester(entity))
				throw new Exception("دسترسی به این درخواست را ندارید");

			FillViewBag(entity, isNew: false);
			return View(@"\Views\Panel\Bpm\ProcessDocument\Edit.cshtml", entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New()
		{
			var created = CreateNew();
			FillViewBag(created, isNew: true);
			return View(@"\Views\Panel\Bpm\ProcessDocument\Edit.cshtml", created);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Bpm\ProcessDocument\List.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دانلود فایل اصلی", ActionAccessType.View)]
		public async Task<IActionResult> DownloadMain(long id, CancellationToken cn)
		{
			return await DownloadFileAsync(id, ProcessDocumentFileSlot.Main, cn);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دانلود فایل متفرقه", ActionAccessType.View)]
		public async Task<IActionResult> DownloadTemp(long id, CancellationToken cn)
		{
			return await DownloadFileAsync(id, ProcessDocumentFileSlot.Temp, cn);
		}

		[HttpGet("[action]")]
		public async Task<IActionResult> GetParent(long id, CancellationToken cn)
		{
			var parent = await unitOfWork.Repository<ProcessDocument>().TableNoTracking
				.Where(c => c.Id == id && !c.IsDeprecate && c.LastStatus == ProcessDocumentStatusEnum.Notified)
				.Select(c => new
				{
					c.Id,
					c.DocumentType,
					c.DocumentTitle,
					c.AccessLevel,
					c.DocumentNumber,
					c.Revision
				})
				.FirstOrDefaultAsync(cn);

			if (parent == null)
				return NotFound("سند والد یافت نشد");

			return Ok(parent);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ProcessDocument>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
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
			var query = ApplyRequesterCartableFilter(unitOfWork.Repository<ProcessDocument>().TableNoTracking);

			var total = await query.CountAsync(cn);
			var take = request.length > 0 ? request.length : 25;
			var items = await query
				.OrderByDescending(c => c.Id)
				.Skip(request.start)
				.Take(take)
				.Select(c => new
				{
					c.Id,
					c.LastStatus,
					c.DocumentTitle,
					c.DocumentNumber,
					c.Revision,
					c.RequestType,
					c.DocumentType,
					c.AccessLevel,
					c.Source,
					c.Priority,
					RequesterUserName = c.RequesterUser != null ? c.RequesterUser.Name : null,
					c.CreatedByName,
					c.CreatedOnShamsiDateTime,
					c.CreatedById,
					c.ModifiedById,
					c.MainFileId,
					c.TempFileId
				})
				.ToListAsync(cn);

			return Ok(new DataTableResponse
			{
				Draw = request.draw,
				RecordsTotal = total,
				RecordsFiltered = total,
				Data = items
			});
		}

		private async Task<IActionResult> CancelRequestCore(long id, CancellationToken cn)
		{
			var existing = await unitOfWork.Repository<ProcessDocument>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (existing == null)
				return BadRequest("درخواست یافت نشد");
			if (!CanViewRequester(existing))
				return BadRequest("دسترسی به این درخواست را ندارید");
			if (!CanCancelRequester(existing))
				return BadRequest("لغو این درخواست در وضعیت فعلی مجاز نیست");

			await AddStatusLogAsync(id, ProcessDocumentStatusEnum.CancelRequest, cn);
			return Ok();
		}

		private async Task<IActionResult> DownloadFileAsync(long id, ProcessDocumentFileSlot slot, CancellationToken cn)
		{
			var document = await unitOfWork.Repository<ProcessDocument>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (document == null)
				return NotFound("درخواست یافت نشد");
			if (!CanViewRequester(document) && !CanViewManage(document))
				return BadRequest("دسترسی به این درخواست را ندارید");

			if (slot == ProcessDocumentFileSlot.Temp && !CanDownloadTempFile(document))
				return BadRequest("دسترسی دانلود فایل متفرقه را ندارید");
			if (slot == ProcessDocumentFileSlot.Comment && !CanDownloadCommentFile(document))
				return BadRequest("دسترسی دانلود فایل کامنت را ندارید");

			var fileId = slot switch
			{
				ProcessDocumentFileSlot.Main => document.MainFileId,
				ProcessDocumentFileSlot.Temp => document.TempFileId,
				ProcessDocumentFileSlot.Final => document.FinalFileId,
				ProcessDocumentFileSlot.FinalPdf => document.FinalPdfFileId,
				ProcessDocumentFileSlot.Comment => document.CommentFileId,
				_ => null
			};
			if (fileId is null or 0)
				return NotFound("فایلی برای این درخواست ثبت نشده است");

			var (stream, contentType, fileName) = await fileService.DownloadAsync(fileId.Value, cn);
			return File(stream, contentType, fileName);
		}

		private IQueryable<ProcessDocument> ApplyRequesterCartableFilter(IQueryable<ProcessDocument> query)
		{
			if (IsAdministrator || CurrentUserHasAnyRole(RoleShowAll))
				return query;

			if (CurrentUserHasAnyRole(RoleSupervisor, RoleExpert))
				return query.Where(c => c.LastStatus != ProcessDocumentStatusEnum.Notified);

			var userId = CurrentUserId;
			return query.Where(c =>
				(c.CreatedById == userId || c.ModifiedById == userId)
				&& c.LastStatus != ProcessDocumentStatusEnum.Notified);
		}

		private bool CanViewRequester(ProcessDocument entity)
		{
			if (IsAdministrator || CurrentUserHasAnyRole(RoleShowAll))
				return true;
			if (entity.LastStatus == ProcessDocumentStatusEnum.Notified)
				return false;
			if (CurrentUserHasAnyRole(RoleSupervisor, RoleExpert))
				return true;
			return entity.CreatedById == CurrentUserId || entity.ModifiedById == CurrentUserId;
		}

		// ادمین در هر وضعیتی می‌تواند فیلدها و فایل‌ها را اصلاح کند
		private bool CanEditRequester(ProcessDocument entity)
		{
			return IsAdministrator || CanCancelRequester(entity);
		}

		private bool CanCancelRequester(ProcessDocument entity)
		{
			if (entity.Id is null or 0)
				return true;
			if (!RequesterEditableStatuses.Contains(entity.LastStatus ?? default))
				return false;
			return IsAdministrator || entity.CreatedById == CurrentUserId;
		}

		private ProcessDocument CreateNew()
		{
			return new ProcessDocument
			{
				RequestType = ProcessDocumentRequestTypeEnum.Create,
				AccessLevel = ProcessDocumentAccessLevelEnum.Public,
				Source = ProcessDocumentSourceEnum.Other,
				Priority = ProcessDocumentPriorityEnum.Normal,
				RequesterUserId = CurrentUserId ?? 0,
				CreatedOrganizationUnitId = CurrentOrganizationUnitId ?? 0,
				Revision = 0
			};
		}

		private async Task<ProcessDocument> LoadForEditAsync(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<ProcessDocument>().TableNoTracking
				.Include(c => c.RequesterUser)
				.Include(c => c.MainFile)
				.Include(c => c.TempFile)
				.Include(c => c.FinalFile)
				.Include(c => c.FinalPdfFile)
				.Include(c => c.CommentFile)
				.Include(c => c.StatusLogs)
				.Include(c => c.EditingUsers).ThenInclude(x => x.User)
				.Include(c => c.ExecuterOrgUnits).ThenInclude(x => x.OrgUnit)
				.Include(c => c.ProcessExecuterOrgUnits).ThenInclude(x => x.OrgUnit)
				.Include(c => c.BeneficiaryOrgUnits).ThenInclude(x => x.OrgUnit)
				.Include(c => c.NotificationRecipients).ThenInclude(x => x.User)
				.Include(c => c.RelatedOrgUnits).ThenInclude(x => x.OrgUnit)
				.FirstOrDefaultAsync(c => c.Id == id, cn);

			if (entity == null)
				return new ProcessDocument();

			if (entity.StatusLogs != null)
			{
				entity.StatusLogs = entity.StatusLogs.OrderBy(x => x.Id).ToList();

				// HTS «کاربر ایجادکننده» = Gnr_User.FullName (نام فارسی)، نه نام ورود.
				var logUserIds = entity.StatusLogs.Where(x => x.CreatedById != null).Select(x => x.CreatedById!.Value).Distinct().ToList();
				if (logUserIds.Count > 0)
				{
					var names = await userService.TableNoTracking
						.Where(u => logUserIds.Contains(u.Id!.Value))
						.Select(u => new { u.Id, u.NameFa })
						.ToListAsync(cn);
					foreach (var log in entity.StatusLogs)
					{
						var fa = names.FirstOrDefault(n => n.Id == log.CreatedById)?.NameFa;
						if (!string.IsNullOrWhiteSpace(fa))
							log.CreatedByName = fa;
					}
				}
			}

			FillJunctionBindFields(entity);
			entity.EditingUsers = [];
			entity.ExecuterOrgUnits = [];
			entity.ProcessExecuterOrgUnits = [];
			entity.BeneficiaryOrgUnits = [];
			entity.NotificationRecipients = [];
			entity.RelatedOrgUnits = [];
			entity.Parent = null;
			entity.ProcessOwner = null;
			entity.Approver = null;
			entity.FinalApprover = null;
			entity.LastStatusUser = null;
			entity.CreatedOrganizationUnit = null;
			entity.RequesterUser = null;
			return entity;
		}

		private void FillViewBag(ProcessDocument entity, bool isNew)
		{
			var canEdit = isNew || CanEditRequester(entity);
			ViewBag.IsManage = false;
			ViewBag.CanEdit = canEdit;
			ViewBag.CanCancel = !isNew && CanCancelRequester(entity);
			ViewBag.CanSaveManage = false;
			ViewBag.CanChangeStatus = false;
			ViewBag.CanEditProcess = false;
			ViewBag.IsSupervisor = CurrentUserHasAnyRole(RoleSupervisor);
			ViewBag.IsExpert = CurrentUserHasAnyRole(RoleExpert);
			ViewBag.CanDownloadMain = entity.MainFileId is > 0 && (canEdit || CanViewRequester(entity));
			ViewBag.CanDownloadTemp = CanDownloadTempFile(entity) && CanViewRequester(entity);
			ViewBag.CanDownloadFinal = false;
			ViewBag.CanDownloadFinalPdf = false;
			ViewBag.CanDownloadComment = false;
			ViewBag.ShowForceNotify = false;
			ViewBag.ShowMainFileSlot = false;
			ViewBag.ShowFinalFileSlot = false;
			ViewBag.ShowFinalPdfFileSlot = false;
			ViewBag.ShowCommentFileSlot = false;
			ViewBag.ShowSendReceive = false;
			ViewBag.AllowedNextStatuses = new List<ProcessDocumentStatusOption>();
		}

		private static void CopyRequesterFields(ProcessDocument target, ProcessDocument posted)
		{
			target.RequestType = posted.RequestType;
			target.ParentId = posted.ParentId is > 0 ? posted.ParentId : null;
			target.DocumentType = posted.DocumentType;
			target.DocumentTitle = posted.DocumentTitle?.Trim() ?? "";
			target.AccessLevel = posted.AccessLevel;
			target.Source = posted.Source;
			target.Priority = posted.Priority;
			target.RequesterUserId = posted.RequesterUserId;
			target.Comment = posted.Comment;
			target.MainFileId = posted.MainFileId is > 0 ? posted.MainFileId : null;
			target.TempFileId = posted.TempFileId is > 0 ? posted.TempFileId : null;
		}

		private static void ClearManageFields(ProcessDocument entity)
		{
			entity.ProcessSet = null;
			entity.SubProcessTitle = null;
			entity.ProcessOwnerId = null;
			entity.ApproverId = null;
			entity.FinalApproverId = null;
			entity.CompletionForecastMiladiDate = null;
			entity.CompletionForecastShamsiDate = null;
			entity.NotificationMiladiDate = null;
			entity.NotificationShamsiDate = null;
			entity.FinalFileId = null;
			entity.FinalPdfFileId = null;
			entity.CommentFileId = null;
			entity.RelatedDocumentsText = null;
			entity.IsDeprecate = false;
			entity.ProcessOwner = null;
			entity.Approver = null;
			entity.FinalApprover = null;
			entity.LastStatusUser = null;
			entity.Parent = null;
			entity.RequesterUser = null;
			entity.CreatedOrganizationUnit = null;
			entity.MainFile = null;
			entity.TempFile = null;
			entity.FinalFile = null;
			entity.FinalPdfFile = null;
			entity.CommentFile = null;
			entity.StatusLogs = new List<ProcessDocumentStatusLog>();
			entity.EditingUsers = new List<ProcessDocumentEditingUser>();
			entity.ExecuterOrgUnits = new List<ProcessDocumentExecuterOrgUnit>();
			entity.ProcessExecuterOrgUnits = new List<ProcessDocumentProcessExecuterOrgUnit>();
			entity.BeneficiaryOrgUnits = new List<ProcessDocumentBeneficiaryOrgUnit>();
			entity.NotificationRecipients = new List<ProcessDocumentNotificationRecipient>();
			entity.RelatedOrgUnits = new List<ProcessDocumentRelatedOrgUnit>();
		}

		private async Task<string?> ValidateRequesterAsync(ProcessDocument entity, bool isNew, CancellationToken cn)
		{
			// متن پیام‌ها عیناً از MessagesLibrary.fa-IR / CaptionsLibrary.fa-IR در HTS
			if (!Enum.IsDefined(entity.RequestType))
				return MsgEmpty("نوع درخواست");
			if (!Enum.IsDefined(entity.AccessLevel))
				return MsgEmpty("سطح محرمانگی");
			if (!Enum.IsDefined(entity.Source))
				return MsgEmpty("ورودی درخواست");
			if (entity.RequesterUserId <= 0)
				return MsgEmpty("درخواست کننده");
			if (!Enum.IsDefined(entity.Priority))
				return MsgInvalid("فوریت رسیدگی");
			if (string.IsNullOrWhiteSpace(entity.Comment))
				return MsgInvalid("شرح درخواست");
			if (entity.CreatedOrganizationUnitId <= 0)
				return "واحد سازمانی کاربر جاری مشخص نیست";

			var needsParent = NeedsParent(entity.RequestType);
			if (needsParent)
			{
				if (entity.ParentId is null or 0)
					return MsgEmpty("سند");

				var parent = await unitOfWork.Repository<ProcessDocument>().TableNoTracking
					.FirstOrDefaultAsync(c => c.Id == entity.ParentId, cn);
				if (parent == null)
					return "سند والد یافت نشد";
				if (parent.LastStatus != ProcessDocumentStatusEnum.Notified || parent.IsDeprecate)
					return "سند والد باید ابلاغ‌شده و غیرمنسوخ باشد";

				entity.DocumentType = parent.DocumentType;
				entity.DocumentTitle = parent.DocumentTitle;
				entity.AccessLevel = parent.AccessLevel;
				entity.DocumentNumber = parent.DocumentNumber;
				if (isNew && entity.RequestType == ProcessDocumentRequestTypeEnum.Review)
					entity.Revision = (short)(parent.Revision + 1);
			}
			else if (isNew)
			{
				entity.ParentId = null;
				if (entity.RequestType is ProcessDocumentRequestTypeEnum.ProcessAutomation
					or ProcessDocumentRequestTypeEnum.MechanizedProcessChange)
				{
					entity.DocumentNumber = null;
				}
			}

			// HTS bpmDocument_validatePropertyGrid: نوع سند همیشه الزامی است و عنوان خالی
			// (برای هر نوع درخواست) با متن نوع درخواست پر می‌شود. در دادهٔ HTS نیز هیچ سندی بدون نوع سند نیست.
			if (entity.DocumentType == null || !Enum.IsDefined(entity.DocumentType.Value))
				return MsgEmpty("نوع سند");

			if (string.IsNullOrWhiteSpace(entity.DocumentTitle))
				entity.DocumentTitle = entity.RequestType.ToDisplay();

			if (entity.DocumentTitle.Length > 128)
				return "عنوان سند حداکثر ۱۲۸ کاراکتر است";

			if (entity.RequestType == ProcessDocumentRequestTypeEnum.Create
				&& entity.DocumentType != null
				&& HiddenTypesOnCreate.Contains(entity.DocumentType.Value))
			{
				return "نوع سند انتخاب‌شده برای درخواست ایجاد مجاز نیست";
			}

			if (entity.MainFileId is > 0)
			{
				var mainFile = await unitOfWork.Repository<FileEntity>().TableNoTracking
					.FirstOrDefaultAsync(f => f.Id == entity.MainFileId, cn);
				if (mainFile == null)
					return "فایل اصلی یافت نشد";
				if (mainFile.Size > MaxFileSizeBytes)
					return MsgFileTooLarge;
			}

			if (entity.TempFileId is > 0)
			{
				var tempFile = await unitOfWork.Repository<FileEntity>().TableNoTracking
					.FirstOrDefaultAsync(f => f.Id == entity.TempFileId, cn);
				if (tempFile == null)
					return "فایل متفرقه یافت نشد";
				if (tempFile.Size > MaxFileSizeBytes)
					return MsgFileTooLarge;
			}

			return null;
		}

		private static bool NeedsParent(ProcessDocumentRequestTypeEnum requestType)
			=> requestType is ProcessDocumentRequestTypeEnum.Delete or ProcessDocumentRequestTypeEnum.Review;

		private async Task AddStatusLogAsync(
			long processDocumentId,
			ProcessDocumentStatusEnum status,
			CancellationToken cn,
			string? comment = null,
			DateTime? sendMiladi = null,
			string? sendShamsi = null,
			DateTime? receiveMiladi = null,
			string? receiveShamsi = null)
		{
			await unitOfWork.Repository<ProcessDocumentStatusLog>().AddAsync(new ProcessDocumentStatusLog
			{
				ProcessDocumentId = processDocumentId,
				Status = status,
				Comment = comment,
				SendMiladiDate = sendMiladi,
				SendShamsiDate = sendShamsi,
				ReceiveMiladiDate = receiveMiladi,
				ReceiveShamsiDate = receiveShamsi
			}, cn, true);
		}

		private bool CanViewManage(ProcessDocument entity)
		{
			return ProcessDocumentWorkflow.MatchesManageCartable(
				entity,
				IsAdministrator || CurrentUserHasAnyRole(RoleShowAll),
				CurrentUserHasAnyRole(RoleSupervisor),
				CurrentUserHasAnyRole(RoleExpert),
				CurrentUserId);
		}

		private bool CanDownloadTempFile(ProcessDocument entity)
		{
			return ProcessDocumentWorkflow.CanDownloadTemp(
				entity,
				IsAdministrator || CurrentUserHasAnyRole(RoleSupervisor),
				IsAdministrator || CurrentUserHasAnyRole(RoleExpert),
				IsAdministrator || CurrentUserHasAnyRole(RoleViewAllAttachments),
				CurrentUserId);
		}

		private bool CanDownloadCommentFile(ProcessDocument entity)
		{
			return ProcessDocumentWorkflow.CanDownloadComment(
				entity,
				IsAdministrator || CurrentUserHasAnyRole(RoleSupervisor),
				IsAdministrator || CurrentUserHasAnyRole(RoleExpert),
				IsAdministrator || CurrentUserHasAnyRole(RoleViewAllAttachments));
		}

		private static void FillJunctionBindFields(ProcessDocument entity)
		{
			entity.EditingUserIds = JoinIds(entity.EditingUsers?.Select(x => x.UserId));
			entity.EditingUserNames = JoinNames(entity.EditingUsers?.Select(x => x.User?.NameFa ?? x.User?.Name));
			entity.ExecuterOrgUnitIds = JoinIds(entity.ExecuterOrgUnits?.Select(x => x.OrgUnitId));
			entity.ExecuterOrgUnitNames = JoinNames(entity.ExecuterOrgUnits?.Select(x => x.OrgUnit?.Title));
			entity.ProcessExecuterOrgUnitIds = JoinIds(entity.ProcessExecuterOrgUnits?.Select(x => x.OrgUnitId));
			entity.ProcessExecuterOrgUnitNames = JoinNames(entity.ProcessExecuterOrgUnits?.Select(x => x.OrgUnit?.Title));
			entity.BeneficiaryOrgUnitIds = JoinIds(entity.BeneficiaryOrgUnits?.Select(x => x.OrgUnitId));
			entity.BeneficiaryOrgUnitNames = JoinNames(entity.BeneficiaryOrgUnits?.Select(x => x.OrgUnit?.Title));
			entity.NotificationRecipientIds = JoinIds(entity.NotificationRecipients?.Select(x => x.UserId));
			entity.NotificationRecipientNames = JoinNames(entity.NotificationRecipients?.Select(x => x.User?.NameFa ?? x.User?.Name));
			entity.RelatedOrgUnitIds = JoinIds(entity.RelatedOrgUnits?.Select(x => x.OrgUnitId));
			entity.RelatedOrgUnitNames = JoinNames(entity.RelatedOrgUnits?.Select(x => x.OrgUnit?.Title));
		}

		private static string? JoinIds(IEnumerable<long>? ids)
		{
			var list = ids?.Where(id => id > 0).Distinct().Select(id => id.ToString()).ToList() ?? [];
			return list.Count == 0 ? null : string.Join(",", list);
		}

		private static string? JoinNames(IEnumerable<string?>? names)
		{
			var list = names?.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!.Trim()).ToList() ?? [];
			return list.Count == 0 ? null : string.Join(" , ", list);
		}

		private static List<long> ParseIds(string? csv)
		{
			if (string.IsNullOrWhiteSpace(csv))
				return [];

			return csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
				.Select(s => long.TryParse(s.Trim(), out var id) ? id : 0)
				.Where(id => id > 0)
				.Distinct()
				.ToList();
		}

		private enum ProcessDocumentFileSlot
		{
			Main,
			Temp,
			Final,
			FinalPdf,
			Comment
		}
	}

	internal sealed class ProcessDocumentStatusOption
	{
		public int Value { get; set; }
		public string Text { get; set; } = "";
	}
}
