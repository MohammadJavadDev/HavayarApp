using Common.Attributes;
using Common.Auth.Enums;
using Entities.App.Bpm;
using Entities.App.Bpm.Enums;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Controllers.Dynamic
{
	public partial class ProcessDocumentController
	{
		[HttpGet("[action]")]
		[ActionDisplayName("لیست اسناد فرآیندی", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult Published()
		{
			return View(@"\Views\Panel\Bpm\ProcessDocument\Published.cshtml");
		}

		/// <summary>
		/// ACL list source for the published catalog (HTS GetReportGridData three-tier).
		/// Live <c>datatableprofile</c> grids post to <c>/System/FetchDataProfile</c> (SavedQuery
		/// <c>Bpm_ProcessDocument_Published</c>); this action remains the C# equivalent and the optional
		/// <c>fetch-data-path</c> target. Admin (unfiltered) is intentional HTS behaviour, not a bug.
		/// </summary>
		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات لیست اسناد فرآیندی", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchPublishedData(DataTableRequest request, CancellationToken cn)
		{
			var query = ApplyPublishedListFilter(unitOfWork.Repository<ProcessDocument>().TableNoTracking);

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
					c.ProcessSet,
					ProcessOwnerName = c.ProcessOwner != null ? c.ProcessOwner.Name : null,
					c.NotificationShamsiDate,
					c.NotificationMiladiDate,
					c.IsDeprecate,
					c.CreatedById,
					c.FinalFileId,
					c.FinalPdfFileId,
					IsRevisedAndHasSupervisorApprove = c.RequestType == ProcessDocumentRequestTypeEnum.Review
						&& (c.StatusLogs.Any(s => s.Status == ProcessDocumentStatusEnum.BpmUpdated) || !c.StatusLogs.Any())
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

		[HttpGet("[action]")]
		[ActionDisplayName("اطلاعات دانلود سند ابلاغ‌شده", ActionAccessType.View, ActionAccessItemType.Custom)]
		public async Task<IActionResult> GetPublishedDownloadInfo(long id, CancellationToken cn)
		{
			var document = await ApplyPublishedListFilter(unitOfWork.Repository<ProcessDocument>().TableNoTracking)
				.Where(c => c.Id == id)
				.Select(c => new
				{
					c.Id,
					c.DocumentType,
					c.AccessLevel,
					c.CreatedById,
					c.FinalFileId,
					c.FinalPdfFileId
				})
				.FirstOrDefaultAsync(cn);

			if (document == null)
				return BadRequest("دسترسی به این سند را ندارید");

			var canRestricted = await CanDownloadPublishedRestrictedAsync(id, document.AccessLevel, document.CreatedById, cn);
			var canFinal = canRestricted && CanOfferPublishedFinal(document.DocumentType, document.FinalFileId, document.FinalPdfFileId);
			var canPdf = canRestricted && CanOfferPublishedFinalPdf(document.FinalPdfFileId);

			return Ok(new
			{
				document.Id,
				canDownloadFinal = canFinal,
				canDownloadFinalPdf = canPdf
			});
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دانلود فایل نهایی ابلاغ‌شده", ActionAccessType.View)]
		public async Task<IActionResult> DownloadPublishedFinal(long id, CancellationToken cn)
		{
			return await DownloadPublishedFileAsync(id, publishedFinal: true, cn);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("دانلود PDF نهایی ابلاغ‌شده", ActionAccessType.View)]
		public async Task<IActionResult> DownloadPublishedFinalPdf(long id, CancellationToken cn)
		{
			return await DownloadPublishedFileAsync(id, publishedFinal: false, cn);
		}

		private IQueryable<ProcessDocument> ApplyPublishedListFilter(IQueryable<ProcessDocument> query)
		{
			if (IsAdministrator)
				return query;

			query = query.Where(p => p.LastStatus == ProcessDocumentStatusEnum.Notified);

			if (CurrentUserHasAnyRole(RoleShowAll))
				return query;

			var userId = CurrentUserId;
			var orgUnitId = CurrentOrganizationUnitId ?? 0;
			return query.Where(p =>
				!p.IsDeprecate
				&& p.RequestType != ProcessDocumentRequestTypeEnum.Delete
				&& (
					(p.AccessLevel != ProcessDocumentAccessLevelEnum.Secret
						&& p.AccessLevel != ProcessDocumentAccessLevelEnum.Limit)
					|| (p.AccessLevel == ProcessDocumentAccessLevelEnum.Secret
						&& (p.CreatedById == userId || p.NotificationRecipients.Any(r => r.UserId == userId)))
					|| (p.AccessLevel == ProcessDocumentAccessLevelEnum.Limit
						&& (p.CreatedById == userId || p.ExecuterOrgUnits.Any(u => u.OrgUnitId == orgUnitId)))));
		}

		private async Task<IActionResult> DownloadPublishedFileAsync(long id, bool publishedFinal, CancellationToken cn)
		{
			var document = await ApplyPublishedListFilter(unitOfWork.Repository<ProcessDocument>().TableNoTracking)
				.Where(c => c.Id == id)
				.Select(c => new
				{
					c.Id,
					c.DocumentType,
					c.AccessLevel,
					c.CreatedById,
					c.FinalFileId,
					c.FinalPdfFileId
				})
				.FirstOrDefaultAsync(cn);

			if (document == null)
				return BadRequest("دسترسی به این سند را ندارید");

			if (!await CanDownloadPublishedRestrictedAsync(id, document.AccessLevel, document.CreatedById, cn))
				return BadRequest("دانلود فایل محرمانه/محدود برای شما مجاز نیست");

			long? fileId;
			if (publishedFinal)
			{
				if (!CanOfferPublishedFinal(document.DocumentType, document.FinalFileId, document.FinalPdfFileId))
					return BadRequest("دانلود فایل نهایی برای این سند مجاز نیست");
				fileId = document.FinalFileId;
			}
			else
			{
				if (!CanOfferPublishedFinalPdf(document.FinalPdfFileId))
					return BadRequest("دانلود PDF نهایی برای این سند مجاز نیست");
				fileId = document.FinalPdfFileId;
			}

			if (fileId is null or 0)
				return NotFound("فایلی برای این سند ثبت نشده است");

			var (stream, contentType, fileName) = await fileService.DownloadAsync(fileId.Value, cn);
			return File(stream, contentType, fileName);
		}

		private async Task<bool> CanDownloadPublishedRestrictedAsync(
			long documentId,
			ProcessDocumentAccessLevelEnum accessLevel,
			long? createdById,
			CancellationToken cn)
		{
			if (IsAdministrator || CurrentUserHasAnyRole(RoleViewAllAttachments))
				return true;
			if (accessLevel is not ProcessDocumentAccessLevelEnum.Secret and not ProcessDocumentAccessLevelEnum.Limit)
				return true;

			var userId = CurrentUserId;
			var orgUnitId = CurrentOrganizationUnitId ?? 0;
			if (createdById != null && createdById == userId)
				return true;

			if (accessLevel == ProcessDocumentAccessLevelEnum.Secret)
			{
				return userId != null && await unitOfWork.Repository<ProcessDocumentNotificationRecipient>()
					.TableNoTracking.AnyAsync(r => r.ProcessDocumentId == documentId && r.UserId == userId, cn);
			}

			return await unitOfWork.Repository<ProcessDocumentExecuterOrgUnit>()
				.TableNoTracking.AnyAsync(u => u.ProcessDocumentId == documentId && u.OrgUnitId == orgUnitId, cn);
		}

		private static bool CanOfferPublishedFinal(
			ProcessDocumentTypeEnum? documentType,
			long? finalFileId,
			long? finalPdfFileId)
		{
			if (finalFileId is null or 0)
				return false;
			if (documentType == ProcessDocumentTypeEnum.F)
				return true;
			return finalPdfFileId is null or 0;
		}

		private static bool CanOfferPublishedFinalPdf(long? finalPdfFileId)
			=> finalPdfFileId is > 0;
	}
}
