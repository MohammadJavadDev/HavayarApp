using Common.Utilities;
using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Bpm;
using Entities.App.Bpm.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Services.Auth;
using Services.NotificationServices;

namespace WebApp.Actions.Bpm
{
	/// <summary>
	/// معادل تریگر HTS UpdateBpmDocumentLastStatus روی Bpm_DocumentDetail:
	/// آخرین ردیف گردش (max Id) → LastStatus و LastStatusUserId.
	/// گذار خودکار مالک=تصویب‌کننده، منسوخ‌کردن اسناد هم‌شماره، ایمیل ابلاغ.
	/// فایل روی مسیر آپلود FileService می‌ماند.
	/// </summary>
	public class ProcessDocumentStatusLogAction(
		IUnitOfWork unitOfWork,
		IUserService userService,
		INotificationService notificationService,
		IConfiguration configuration)
	{
		[EntityAction(typeof(ProcessDocumentStatusLog), EntityActionTrigger.AfterAdd,
			"SyncProcessDocumentLastStatusAfterAdd", "همگام‌سازی وضعیت آخر سند فرآیندی پس از درج گردش", Priority = 1)]
		public Task SyncAfterAdd(ProcessDocumentStatusLog model, CancellationToken ct)
			=> SyncLastStatusAsync(model.ProcessDocumentId, ct);

		[EntityAction(typeof(ProcessDocumentStatusLog), EntityActionTrigger.AfterUpdate,
			"SyncProcessDocumentLastStatusAfterUpdate", "همگام‌سازی وضعیت آخر سند فرآیندی پس از ویرایش گردش", Priority = 1)]
		public Task SyncAfterUpdate(ProcessDocumentStatusLog model, CancellationToken ct)
			=> SyncLastStatusAsync(model.ProcessDocumentId, ct);

		[EntityAction(typeof(ProcessDocumentStatusLog), EntityActionTrigger.AfterDelete,
			"SyncProcessDocumentLastStatusAfterDelete", "همگام‌سازی وضعیت آخر سند فرآیندی پس از حذف گردش", Priority = 1)]
		public Task SyncAfterDelete(ProcessDocumentStatusLog model, CancellationToken ct)
			=> SyncLastStatusAsync(model.ProcessDocumentId, ct);

		[EntityAction(typeof(ProcessDocumentStatusLog), EntityActionTrigger.AfterAdd,
			"AutoProcessOwnerApprovedWhenOwnerIsFinal", "تایید خودکار مالک در صورت تطابق با تصویب‌کننده", Priority = 2)]
		public async Task AutoProcessOwnerApproved(ProcessDocumentStatusLog model, CancellationToken ct)
		{
			if (model.Status != ProcessDocumentStatusEnum.BpmApproved || model.ProcessDocumentId <= 0)
				return;

			var document = await unitOfWork.Repository<ProcessDocument>().TableNoTracking
				.Where(c => c.Id == model.ProcessDocumentId)
				.Select(c => new { c.ProcessOwnerId, c.FinalApproverId })
				.FirstOrDefaultAsync(ct);

			if (document?.ProcessOwnerId is null or 0)
				return;
			if (document.ProcessOwnerId != document.FinalApproverId)
				return;

			var owner = await userService.GetById(document.ProcessOwnerId.Value);
			var ownerName = owner?.NameFa ?? owner?.Name ?? owner?.Username ?? "-";

			await unitOfWork.Repository<ProcessDocumentStatusLog>().AddAsync(new ProcessDocumentStatusLog
			{
				ProcessDocumentId = model.ProcessDocumentId,
				Status = ProcessDocumentStatusEnum.ProcessOwnerApproved,
				Comment = "بصورت اتوماتیک بعلت تطابق مالک و تصویب کننده",
				CreatedById = document.ProcessOwnerId,
				CreatedByName = ownerName
			}, ct, true);
		}

		[EntityAction(typeof(ProcessDocumentStatusLog), EntityActionTrigger.AfterAdd,
			"DeprecateSameNumberOnNotify", "منسوخ‌کردن اسناد هم‌شماره و والد پس از ابلاغ حذف/بازنگری", Priority = 3)]
		public async Task DeprecateOnNotify(ProcessDocumentStatusLog model, CancellationToken ct)
		{
			if (model.Status != ProcessDocumentStatusEnum.Notified || model.ProcessDocumentId <= 0)
				return;

			var document = await unitOfWork.Repository<ProcessDocument>().TableNoTracking
				.Where(c => c.Id == model.ProcessDocumentId)
				.Select(c => new { c.Id, c.RequestType, c.ParentId, c.DocumentNumber })
				.FirstOrDefaultAsync(ct);

			if (document == null)
				return;
			if (document.RequestType != ProcessDocumentRequestTypeEnum.Delete
				&& (document.RequestType != ProcessDocumentRequestTypeEnum.Review || document.ParentId == null))
				return;

			var currentId = document.Id!.Value;
			var parentId = document.ParentId;
			var numberLower = document.DocumentNumber?.ToLower();
			var stamp = $"- منسوخ شده در تاریخ {DateTime.Now.ToShamsiDateTime()}";

			var others = await unitOfWork.Repository<ProcessDocument>().TableNoTracking
				.Where(d =>
					d.Id != null
					&& d.Id != currentId
					&& (
						(parentId != null && d.Id == parentId)
						|| (numberLower != null && d.DocumentNumber != null && d.DocumentNumber.ToLower() == numberLower)
					))
				.Select(d => new { d.Id, d.Comment })
				.ToListAsync(ct);

			foreach (var row in others)
			{
				if (row.Id is null or 0)
					continue;

				await unitOfWork.Repository<ProcessDocument>().UpdateFieldsAsync(row.Id.Value, new Dictionary<string, object>
				{
					[nameof(ProcessDocument.IsDeprecate)] = true,
					[nameof(ProcessDocument.Comment)] = (row.Comment ?? "") + stamp
				}, ct);
			}
		}

		[EntityAction(typeof(ProcessDocumentStatusLog), EntityActionTrigger.AfterAdd,
			"NotifyAndMoveFilesOnStatus", "ایمیل/نوتیف گردش پس از ابلاغ", Priority = 4)]
		public async Task NotifyAndMoveFiles(ProcessDocumentStatusLog model, CancellationToken ct)
		{
			if (model.ProcessDocumentId <= 0)
				return;

			try
			{
				if (model.Status == ProcessDocumentStatusEnum.Notified)
				{
					var document = await unitOfWork.Repository<ProcessDocument>().TableNoTracking
						.Include(d => d.NotificationRecipients)
						.Include(d => d.ExecuterOrgUnits)
						.FirstOrDefaultAsync(d => d.Id == model.ProcessDocumentId, ct);
					if (document != null)
					{
						await ProcessDocumentRoleGrant.GrantPublishedListOnNotifyAsync(
							userService,
							onlineUserService: null,
							document,
							ct);
					}
				}

				var dispatcher = new ProcessDocumentNotificationDispatcher(
					unitOfWork, userService, notificationService, configuration);
				await dispatcher.DispatchAfterStatusLogAsync(model, ct);
			}
			catch
			{
				// شکست ایمیل/فایل نباید گردش را rollback کند (معادل Task.Run + catch در HTS).
			}
		}

		private async Task SyncLastStatusAsync(long processDocumentId, CancellationToken ct)
		{
			if (processDocumentId <= 0)
				return;

			var latest = await unitOfWork.Repository<ProcessDocumentStatusLog>().TableNoTracking
				.Where(c => c.ProcessDocumentId == processDocumentId)
				.OrderByDescending(c => c.Id)
				.Select(c => new { c.Status, c.CreatedById })
				.FirstOrDefaultAsync(ct);

			var fields = new Dictionary<string, object>();
			if (latest == null)
			{
				fields[nameof(ProcessDocument.LastStatus)] = null!;
				fields[nameof(ProcessDocument.LastStatusUserId)] = null!;
			}
			else
			{
				fields[nameof(ProcessDocument.LastStatus)] = latest.Status;
				if (latest.CreatedById != null)
					fields[nameof(ProcessDocument.LastStatusUserId)] = latest.CreatedById.Value;
				else
					fields[nameof(ProcessDocument.LastStatusUserId)] = null!;
			}

			await unitOfWork.Repository<ProcessDocument>().UpdateFieldsAsync(processDocumentId, fields, ct);
		}
	}
}
