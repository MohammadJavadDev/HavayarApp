using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Entities.App.Bpm;
using Entities.App.Bpm.Enums;
using Entities.Auth;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Actions.Bpm;

namespace WebApp.Controllers.Dynamic
{
	public partial class ProcessDocumentController
	{
		[HttpGet("[action]")]
		[ActionDisplayName("لیست مدیریت درخواست‌ها", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult Manage()
		{
			return View(@"\Views\Panel\Bpm\ProcessDocument\Manage.cshtml");
		}

		/// <summary>
		/// فرم مودال «بررسی / تایید» (معادل property grid در حالت AddDetail در HTS).
		/// دکمهٔ لیست آن را با page.getPartialView باز می‌کند؛ ثبت از طریق ChangeStatus است.
		/// </summary>
		[HttpGet("[action]")]
		public async Task<IActionResult> ManageStatusForm(long id, CancellationToken cn)
		{
			var entity = await LoadForEditAsync(id, cn);
			if (entity.Id is null or 0)
				throw new Exception("درخواست یافت نشد");
			if (!CanViewManage(entity))
				throw new Exception("دسترسی به این درخواست را ندارید");

			FillManageViewBag(entity);
			if (!(bool)ViewBag.CanChangeStatus)
				throw new Exception("در این وضعیت مجاز به تعیین وضعیت نیستید");

			return View(@"\Views\Panel\Bpm\ProcessDocument\_ManageStatusForm.cshtml", entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات مدیریت درخواست", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchManageData(DataTableRequest request, CancellationToken cn)
		{
			var query = ApplyManageCartableFilter(unitOfWork.Repository<ProcessDocument>().TableNoTracking);

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
					c.ProcessSet,
					ProcessOwnerName = c.ProcessOwner != null ? c.ProcessOwner.Name : null,
					ApproverName = c.Approver != null ? c.Approver.Name : null,
					FinalApproverName = c.FinalApprover != null ? c.FinalApprover.Name : null,
					RequesterUserName = c.RequesterUser != null ? c.RequesterUser.Name : null,
					c.CreatedByName,
					c.CreatedOnShamsiDateTime,
					c.ProcessOwnerId,
					c.ApproverId,
					c.FinalApproverId,
					c.MainFileId,
					c.TempFileId,
					c.FinalFileId,
					c.FinalPdfFileId,
					c.CommentFileId
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

		[HttpPost("[action]")]
		[ActionDisplayName("ثبت وضعیت سند فرآیندی", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> ChangeStatus(ProcessDocument posted, CancellationToken cn)
		{
			if (posted.Id is null or 0)
				return BadRequest("درخواست یافت نشد");

			var existing = await unitOfWork.Repository<ProcessDocument>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == posted.Id, cn);
			if (existing == null)
				return BadRequest("درخواست یافت نشد");
			if (!CanViewManage(existing))
				return BadRequest("دسترسی به این درخواست را ندارید");

			var isSupervisor = CurrentUserHasAnyRole(RoleSupervisor);
			var isExpert = CurrentUserHasAnyRole(RoleExpert);
			var isAdmin = IsAdministrator;
			if (!ProcessDocumentWorkflow.CanOpenTransitionUi(existing, isSupervisor, isExpert, isAdmin, CurrentUserId))
				return BadRequest("در این وضعیت مجاز به تعیین وضعیت نیستید");

			var forceNotify = posted.IgnoreProcessAndForceNotification;
			if (forceNotify && !isSupervisor && !isAdmin)
				return BadRequest("فقط سرپرست می‌تواند بدون گردش ابلاغ کند");

			if (isSupervisor || isAdmin)
				ApplySupervisorFields(existing, posted);
			else if (isExpert)
				ApplyExpertFiles(existing, posted);

			var fileError = await ValidateManageFilesAsync(existing, cn);
			if (fileError != null)
				return BadRequest(fileError);

			if (forceNotify)
			{
				if (string.IsNullOrWhiteSpace(existing.NotificationShamsiDate) && existing.NotificationMiladiDate == null)
					return BadRequest(MsgEmpty("تاریخ ابلاغ"));
				if (existing.RequestType != ProcessDocumentRequestTypeEnum.Delete)
				{
					if (existing.FinalFileId is null or 0)
						return BadRequest(MsgNoFileSelected);
					if (existing.DocumentType != ProcessDocumentTypeEnum.F && existing.FinalPdfFileId is null or 0)
						return BadRequest(MsgNoFileSelected);
				}
			}
			else
			{
				if (posted.NextStatus == null || !Enum.IsDefined(posted.NextStatus.Value))
					return BadRequest(MsgEmpty("وضعیت"));

				var allowed = ProcessDocumentWorkflow.GetAllowedNextStatuses(
					existing.LastStatus,
					CurrentUserId,
					existing.ProcessOwnerId,
					existing.ApproverId,
					existing.FinalApproverId);
				if (!allowed.Contains(posted.NextStatus.Value))
					return BadRequest("گذار وضعیت مجاز نیست");

				var transitionError = ValidateTransitionRequirements(existing, posted.NextStatus.Value, posted.StatusLogComment, isSupervisor || isAdmin);
				if (transitionError != null)
					return BadRequest(transitionError);
			}

			var nextStatus = forceNotify ? ProcessDocumentStatusEnum.Notified : posted.NextStatus!.Value;
			if (nextStatus == ProcessDocumentStatusEnum.Notified
				|| forceNotify)
			{
				if (await HasDuplicateNumberOnNotify(existing, cn))
					return BadRequest(ProcessDocumentWorkflow.DuplicateDocumentNumberMessage);
			}

			ClearGraph(existing);
			await unitOfWork.Repository<ProcessDocument>().UpdateAsync(existing, cn, true);
			if (isSupervisor || isAdmin)
			{
				await ReplaceManageJunctions(existing.Id!.Value, posted, cn);
				await GrantViewManageRoleAsync(
				[
					existing.ProcessOwnerId ?? 0,
					existing.ApproverId ?? 0,
					existing.FinalApproverId ?? 0
				], cn);
			}

			var saved = await unitOfWork.Repository<ProcessDocument>().TableNoTracking
				.FirstAsync(c => c.Id == existing.Id, cn);
			if (!forceNotify
				&& nextStatus is ProcessDocumentStatusEnum.InProgress or ProcessDocumentStatusEnum.BpmAcceptComments
				&& string.IsNullOrWhiteSpace(saved.DocumentNumber))
				return BadRequest(MsgEmpty("کد سند"));

			if (forceNotify)
			{
				await AddStatusLogAsync(existing.Id!.Value, ProcessDocumentStatusEnum.IgnoreProcessAndForceToNotify, cn, posted.StatusLogComment);
				await AddStatusLogAsync(
					existing.Id.Value,
					ProcessDocumentStatusEnum.Notified,
					cn,
					posted.StatusLogComment,
					posted.StatusLogSendMiladiDate,
					posted.StatusLogSendShamsiDate,
					posted.StatusLogReceiveMiladiDate,
					posted.StatusLogReceiveShamsiDate);
			}
			else
			{
				await AddStatusLogAsync(
					existing.Id!.Value,
					nextStatus,
					cn,
					posted.StatusLogComment,
					posted.StatusLogSendMiladiDate,
					posted.StatusLogSendShamsiDate,
					posted.StatusLogReceiveMiladiDate,
					posted.StatusLogReceiveShamsiDate);
			}

			return Ok(await LoadForEditAsync(existing.Id!.Value, cn));
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دانلود فایل نهایی", ActionAccessType.View)]
		public async Task<IActionResult> DownloadFinal(long id, CancellationToken cn)
		{
			return await DownloadFileAsync(id, ProcessDocumentFileSlot.Final, cn);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دانلود فایل PDF نهایی", ActionAccessType.View)]
		public async Task<IActionResult> DownloadFinalPdf(long id, CancellationToken cn)
		{
			return await DownloadFileAsync(id, ProcessDocumentFileSlot.FinalPdf, cn);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دانلود فایل کامنت", ActionAccessType.View)]
		public async Task<IActionResult> DownloadComment(long id, CancellationToken cn)
		{
			return await DownloadFileAsync(id, ProcessDocumentFileSlot.Comment, cn);
		}

		private IQueryable<ProcessDocument> ApplyManageCartableFilter(IQueryable<ProcessDocument> query)
		{
			if (IsAdministrator || CurrentUserHasAnyRole(RoleShowAll))
				return query;

			var userId = CurrentUserId;
			var supervisorIds = ProcessDocumentWorkflow.SupervisorStatusIds;

			if (CurrentUserHasAnyRole(RoleSupervisor))
			{
				return query.Where(p =>
					(p.LastStatus != null && supervisorIds.Contains(p.LastStatus.Value))
					|| p.LastStatus == ProcessDocumentStatusEnum.InProgress
					|| (p.LastStatus == ProcessDocumentStatusEnum.BpmApproved && p.ProcessOwnerId == userId)
					|| (p.LastStatus == ProcessDocumentStatusEnum.ProcessOwnerApproved && p.ApproverId == userId)
					|| (p.LastStatus == ProcessDocumentStatusEnum.ApproverApproved && p.FinalApproverId == userId));
			}

			if (CurrentUserHasAnyRole(RoleExpert))
			{
				return query.Where(p =>
					p.LastStatus == ProcessDocumentStatusEnum.InProgress
					|| p.LastStatus == ProcessDocumentStatusEnum.BpmReject
					|| p.LastStatus == ProcessDocumentStatusEnum.BpmAcceptComments
					|| (p.LastStatus != null && supervisorIds.Contains(p.LastStatus.Value)));
			}

			return query.Where(p =>
				(p.LastStatus == ProcessDocumentStatusEnum.BpmApproved && p.ProcessOwnerId == userId)
				|| (p.LastStatus == ProcessDocumentStatusEnum.ProcessOwnerApproved && p.ApproverId == userId)
				|| (p.LastStatus == ProcessDocumentStatusEnum.ApproverApproved && p.FinalApproverId == userId));
		}

		private void FillManageViewBag(ProcessDocument entity)
		{
			var isSupervisor = CurrentUserHasAnyRole(RoleSupervisor);
			var isExpert = CurrentUserHasAnyRole(RoleExpert);
			var isAdmin = IsAdministrator;
			var canChange = ProcessDocumentWorkflow.CanOpenTransitionUi(entity, isSupervisor, isExpert, isAdmin, CurrentUserId);
			var last = entity.LastStatus;
			var allowed = canChange
				? ProcessDocumentWorkflow.GetAllowedNextStatuses(
					last,
					CurrentUserId,
					entity.ProcessOwnerId,
					entity.ApproverId,
					entity.FinalApproverId)
				: [];

			ViewBag.IsManage = true;
			ViewBag.CanEdit = false;
			ViewBag.CanCancel = false;
			ViewBag.CanSaveManage = isSupervisor || isAdmin;
			ViewBag.CanChangeStatus = canChange;
			ViewBag.CanEditProcess = isSupervisor || isAdmin;
			ViewBag.IsSupervisor = isSupervisor || isAdmin;
			ViewBag.IsExpert = isExpert;
			ViewBag.CanDownloadMain = entity.MainFileId is > 0;
			ViewBag.CanDownloadTemp = CanDownloadTempFile(entity);
			ViewBag.CanDownloadFinal = entity.FinalFileId is > 0;
			ViewBag.CanDownloadFinalPdf = entity.FinalPdfFileId is > 0;
			ViewBag.CanDownloadComment = CanDownloadCommentFile(entity);
			ViewBag.ShowForceNotify = canChange && (isSupervisor || isAdmin);
			ViewBag.ShowMainFileSlot = canChange && (isSupervisor || isAdmin) && last == ProcessDocumentStatusEnum.BpmExpertApproved;
			ViewBag.ShowFinalFileSlot = canChange && (isSupervisor || isAdmin)
				&& last is ProcessDocumentStatusEnum.FinalApproverApproved;
			ViewBag.ShowFinalPdfFileSlot = canChange && (isSupervisor || isAdmin)
				&& last is ProcessDocumentStatusEnum.FinalApproverApproved;
			ViewBag.ShowCommentFileSlot = canChange && isExpert
				&& last is ProcessDocumentStatusEnum.InProgress
					or ProcessDocumentStatusEnum.BpmReject
					or ProcessDocumentStatusEnum.BpmAcceptComments;
			ViewBag.ShowSendReceive = (bool)ViewBag.ShowCommentFileSlot;
			ViewBag.AllowedNextStatuses = allowed
				.Select(s => new ProcessDocumentStatusOption
				{
					Value = (int)s,
					Text = ProcessDocumentStatusCaptions.GetActionCaption(s)
				})
				.ToList();
		}

		private static void ApplySupervisorFields(ProcessDocument target, ProcessDocument posted)
		{
			// HTS DoDocumentDetailOperation: عنوان، سطح محرمانگی، شماره و ... فقط وقتی مقدار ارسال شده باشد جایگزین می‌شوند.
			target.DocumentType = posted.DocumentType ?? target.DocumentType;
			if (!string.IsNullOrWhiteSpace(posted.DocumentNumber))
				target.DocumentNumber = posted.DocumentNumber.Trim();
			if (!string.IsNullOrWhiteSpace(posted.DocumentTitle))
				target.DocumentTitle = posted.DocumentTitle.Trim();
			if (Enum.IsDefined(posted.AccessLevel))
				target.AccessLevel = posted.AccessLevel;
			target.ProcessSet = posted.ProcessSet;
			target.ProcessOwnerId = posted.ProcessOwnerId is > 0 ? posted.ProcessOwnerId : null;
			target.ApproverId = posted.ApproverId is > 0 ? posted.ApproverId : null;
			target.FinalApproverId = posted.FinalApproverId is > 0 ? posted.FinalApproverId : null;
			target.CompletionForecastMiladiDate = posted.CompletionForecastMiladiDate;
			target.CompletionForecastShamsiDate = posted.CompletionForecastShamsiDate;
			target.NotificationMiladiDate = posted.NotificationMiladiDate;
			target.NotificationShamsiDate = posted.NotificationShamsiDate;
			target.RelatedDocumentsText = posted.RelatedDocumentsText;
			if (posted.MainFileId is > 0)
				target.MainFileId = posted.MainFileId;
			if (posted.TempFileId is > 0)
				target.TempFileId = posted.TempFileId;
			if (posted.FinalFileId is > 0)
				target.FinalFileId = posted.FinalFileId;
			if (posted.FinalPdfFileId is > 0)
				target.FinalPdfFileId = posted.FinalPdfFileId;
			if (posted.CommentFileId is > 0)
				target.CommentFileId = posted.CommentFileId;
		}

		private static void ApplyExpertFiles(ProcessDocument target, ProcessDocument posted)
		{
			if (posted.CommentFileId is > 0)
				target.CommentFileId = posted.CommentFileId;
			if (posted.MainFileId is > 0)
				target.MainFileId = posted.MainFileId;
		}

		private static string? ValidateTransitionRequirements(
			ProcessDocument entity,
			ProcessDocumentStatusEnum nextStatus,
			string? comment,
			bool isSupervisor)
		{
			// متن و ترتیب پیام‌ها مطابق bpmManageDocument_validatePropertyGrid در HTS
			if (entity.DocumentType == null)
				return MsgEmpty("نوع سند");

			if (ProcessDocumentWorkflow.CommentRequiredStatuses.Contains(nextStatus)
				&& string.IsNullOrWhiteSpace(comment))
				return MsgEmpty("توضیحات");

			if (nextStatus == ProcessDocumentStatusEnum.BpmApproved
				&& isSupervisor
				&& entity.LastStatus == ProcessDocumentStatusEnum.BpmExpertApproved
				&& entity.MainFileId is null or 0)
				return MsgNoFileSelected;

			if (nextStatus is ProcessDocumentStatusEnum.InProgress or ProcessDocumentStatusEnum.BpmAcceptComments)
			{
				if (entity.ProcessSet == null)
					return MsgEmpty("مجموعه فرآیندی");
				if (entity.ProcessOwnerId is null or 0)
					return MsgEmpty("مالک فرآیند");
				if (entity.ApproverId is null or 0)
					return MsgEmpty("تایید کننده");
				if (entity.FinalApproverId is null or 0)
					return MsgEmpty("تصویب کننده");
				if (entity.CompletionForecastMiladiDate == null && string.IsNullOrWhiteSpace(entity.CompletionForecastShamsiDate))
					return MsgEmpty("تاریخ پیش بینی انجام درخواست");
			}

			if (nextStatus == ProcessDocumentStatusEnum.Notified
				&& entity.RequestType != ProcessDocumentRequestTypeEnum.Delete)
			{
				if (entity.FinalFileId is null or 0)
					return MsgNoFileSelected;
				if (entity.DocumentType != ProcessDocumentTypeEnum.F && entity.FinalPdfFileId is null or 0)
					return MsgNoFileSelected;
			}

			return null;
		}

		private async Task<bool> HasDuplicateNumberOnNotify(ProcessDocument entity, CancellationToken cn)
		{
			if (entity.RequestType != ProcessDocumentRequestTypeEnum.Create)
				return false;
			if (string.IsNullOrWhiteSpace(entity.DocumentNumber) || string.IsNullOrWhiteSpace(entity.DocumentTitle))
				return false;

			var cancel = ProcessDocumentWorkflow.CancelStatuses;
			return await unitOfWork.Repository<ProcessDocument>().TableNoTracking.AnyAsync(p =>
				p.Id != entity.Id
				&& !p.IsDeprecate
				&& p.LastStatus != null
				&& !cancel.Contains(p.LastStatus.Value)
				&& p.DocumentNumber == entity.DocumentNumber
				&& p.DocumentTitle == entity.DocumentTitle, cn);
		}

		private async Task<string?> ValidateManageFilesAsync(ProcessDocument entity, CancellationToken cn)
		{
			async Task<string?> Check(long? fileId, string label)
			{
				if (fileId is null or 0)
					return null;
				var file = await unitOfWork.Repository<FileEntity>().TableNoTracking
					.FirstOrDefaultAsync(f => f.Id == fileId, cn);
				if (file == null)
					return $"{label} یافت نشد";
				if (file.Size > MaxFileSizeBytes)
					return $"حجم {label} بیشتر از ۲۰ مگابایت است";
				return null;
			}

			return await Check(entity.MainFileId, "فایل اصلی")
				?? await Check(entity.TempFileId, "فایل متفرقه")
				?? await Check(entity.FinalFileId, "فایل نهایی")
				?? await Check(entity.FinalPdfFileId, "فایل PDF نهایی")
				?? await Check(entity.CommentFileId, "فایل کامنت");
		}

		private async Task ReplaceManageJunctions(long documentId, ProcessDocument posted, CancellationToken cn)
		{
			// HTS: هر فهرست فقط وقتی مقدار ارسال شده باشد جایگزین می‌شود (خالی = بدون تغییر).
			// «تیم بازنگری/تدوین» = کاربران، «مجری فرآیند» = واحدهای سازمانی (مطابق دادهٔ HTS).
			var editingUserIds = ParseIds(posted.EditingUserIds);
			if (editingUserIds.Count > 0)
			{
				await unitOfWork.Repository<ProcessDocumentEditingUser>()
					.DeleteWhereAsync(c => c.ProcessDocumentId == documentId, cn);
				await unitOfWork.Repository<ProcessDocumentEditingUser>().AddRangeAsync(
					editingUserIds.Select(userId => new ProcessDocumentEditingUser
					{
						ProcessDocumentId = documentId,
						UserId = userId
					}).ToList(), cn, true);
			}

			if (ParseIds(posted.ExecuterOrgUnitIds).Count > 0)
				await ReplaceOrgUnitJunction<ProcessDocumentExecuterOrgUnit>(
					documentId, ParseIds(posted.ExecuterOrgUnitIds), cn);
			if (ParseIds(posted.ProcessExecuterOrgUnitIds).Count > 0)
				await ReplaceOrgUnitJunction<ProcessDocumentProcessExecuterOrgUnit>(
					documentId, ParseIds(posted.ProcessExecuterOrgUnitIds), cn);
			if (ParseIds(posted.BeneficiaryOrgUnitIds).Count > 0)
				await ReplaceOrgUnitJunction<ProcessDocumentBeneficiaryOrgUnit>(
					documentId, ParseIds(posted.BeneficiaryOrgUnitIds), cn);
			if (ParseIds(posted.RelatedOrgUnitIds).Count > 0)
				await ReplaceOrgUnitJunction<ProcessDocumentRelatedOrgUnit>(
					documentId, ParseIds(posted.RelatedOrgUnitIds), cn);

			var recipientIds = ParseIds(posted.NotificationRecipientIds);
			if (recipientIds.Count > 0)
			{
				await unitOfWork.Repository<ProcessDocumentNotificationRecipient>()
					.DeleteWhereAsync(c => c.ProcessDocumentId == documentId, cn);
				await unitOfWork.Repository<ProcessDocumentNotificationRecipient>().AddRangeAsync(
					recipientIds.Select(userId => new ProcessDocumentNotificationRecipient
					{
						ProcessDocumentId = documentId,
						UserId = userId
					}).ToList(), cn, true);
			}
		}

		private async Task ReplaceOrgUnitJunction<TRow>(long documentId, List<long> orgUnitIds, CancellationToken cn)
			where TRow : BaseEntity, new()
		{
			await unitOfWork.Repository<TRow>().DeleteWhereAsync(
				c => EF.Property<long>(c, nameof(ProcessDocumentEditingOrgUnit.ProcessDocumentId)) == documentId, cn);

			if (orgUnitIds.Count == 0)
				return;

			var rows = new List<TRow>();
			foreach (var orgUnitId in orgUnitIds)
			{
				var row = new TRow();
				typeof(TRow).GetProperty(nameof(ProcessDocumentEditingOrgUnit.ProcessDocumentId))!
					.SetValue(row, documentId);
				typeof(TRow).GetProperty(nameof(ProcessDocumentEditingOrgUnit.OrgUnitId))!
					.SetValue(row, orgUnitId);
				rows.Add(row);
			}

			await unitOfWork.Repository<TRow>().AddRangeAsync(rows, cn, true);
		}

		private Task GrantViewManageRoleAsync(IEnumerable<long> userIds, CancellationToken cn)
		{
			return ProcessDocumentRoleGrant.GrantAsync(
				userService,
				onlineUserService,
				RoleViewManage,
				userIds,
				cn);
		}

		private static void ClearGraph(ProcessDocument entity)
		{
			entity.Parent = null;
			entity.ProcessOwner = null;
			entity.Approver = null;
			entity.FinalApprover = null;
			entity.LastStatusUser = null;
			entity.RequesterUser = null;
			entity.CreatedOrganizationUnit = null;
			entity.MainFile = null;
			entity.TempFile = null;
			entity.FinalFile = null;
			entity.FinalPdfFile = null;
			entity.CommentFile = null;
			entity.StatusLogs = [];
			entity.EditingOrgUnits = [];
			entity.EditingUsers = [];
			entity.ExecuterOrgUnits = [];
			entity.ProcessExecuters = [];
			entity.ProcessExecuterOrgUnits = [];
			entity.BeneficiaryOrgUnits = [];
			entity.NotificationRecipients = [];
			entity.RelatedOrgUnits = [];
		}
	}
}
