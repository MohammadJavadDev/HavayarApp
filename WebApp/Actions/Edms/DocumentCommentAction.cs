using Common.Utilities;
using Data.Contracts;
using Data.Contracts.Actions;
using Data.Services.Sup;
using Data.SystemAuth;
using Entities.App.Edms;
using Entities.App.Edms.Enums;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.EntityFrameworkCore;
using Services.NotificationServices;

namespace WebApp.Actions.Edms
{
	public class DocumentCommentAction(IUnitOfWork unitOfWork, ISdk sdk, INotificationService notificationService)
	{
		private const long EngineeringManagerRoleId = 200072;
		private const string EngineeringManagerRoleName = "Edms.Document.EngineeringManager";

		private bool IsEngineeringManager() =>
			sdk.HasRole(EngineeringManagerRoleId) || sdk.HasRole(EngineeringManagerRoleName);

		// اعتبارسنجی قبل از ذخیره: اگر در AfterAdd بود، ردیف کامنت ذخیره می‌شد و بعد خطا می‌آمد.
		// متد عمدا async است تا پیام خطا بدون پیشوند «خطا در اکشن» نمایش داده شود (مثل رفتار قبلی).
		[EntityAction(typeof(DocumentComment), EntityActionTrigger.BeforeAdd, "DocumentCommentValidateStatus", "اعتبارسنجی وضعیت کامنت مدرک", Priority = 1)]
		public async Task ValidateStatus(DocumentComment model, CancellationToken ct)
		{
			await Task.CompletedTask;
			if (model.Status == null)
				throw new Exception("وضعیت مدرک انتخاب نشده است");
		}

		[EntityAction(typeof(DocumentComment), EntityActionTrigger.AfterAdd, "ChangeDocumentStatus", "تغییر وضعیت مدرک", Priority = 1)]
		public async Task ChangeDocumentStatus(DocumentComment model, CancellationToken ct)
		{

			if (model.Status == null)
				return;

			var now = DateTime.Now;
			var document = await unitOfWork.Repository<Document>().
				Table
				.Include(c => c.DocumentVpis)
				.FirstAsync(c => c.Id == model.DocumentId);

			var previousStatus = document.Status;
			var isVendor = false;
			if (document.ProjectId is > 0)
			{
				isVendor = await unitOfWork.Repository<Project>().TableNoTracking
					.Where(p => p.Id == document.ProjectId.Value)
					.Select(p => p.ProjectIsVendoriType)
					.FirstOrDefaultAsync(ct);
			}

			if (document.ApproverId == sdk.CurrentUser.Id)
			{
				if (model.Status == DocumentStatusEnums.ApproveByApprover)
				{
					document.ApprovedShamsiDateTime = now.ToShamsiDateTime();
					document.ApprovedMiladiDateTime = now;
				}

			}

			if (document.ReviewerId == sdk.CurrentUser.Id)
			{
				if (model.Status == DocumentStatusEnums.ApproveByReviewer)
				{
					document.RevieweShamsiDateTime = now.ToShamsiDateTime();
					document.RevieweMiladiDateTime = now;
				}

			}

			// برگه پاسخ: اگر تاییدکننده همان بررسی‌کننده باشد، تایید او هر دو تاریخ را می‌زند.
			if (previousStatus == DocumentStatusEnums.ReplaySheet
				&& model.Status == DocumentStatusEnums.ApproveByApprover
				&& document.ReviewerId == sdk.CurrentUser.Id)
			{
				document.RevieweShamsiDateTime = now.ToShamsiDateTime();
				document.RevieweMiladiDateTime = now;
			}

			if (model.Status == DocumentStatusEnums.ApproveByApprover && IsEngineeringManager())
			{
				document.ApprovedShamsiDateTime = now.ToShamsiDateTime();
				document.ApprovedMiladiDateTime = now;
				if (document.RevieweMiladiDateTime == null)
				{
					document.RevieweShamsiDateTime = now.ToShamsiDateTime();
					document.RevieweMiladiDateTime = now;
				}
			}

			// معادل HTS DCC: تایید هر دو تاریخ را می‌زند؛ رد هر دو را پاک می‌کند
			if (model.Status == DocumentStatusEnums.ApprovedByDcc)
			{
				document.ApprovedShamsiDateTime = now.ToShamsiDateTime();
				document.ApprovedMiladiDateTime = now;
			}
			else if (model.Status == DocumentStatusEnums.RejectByDcc)
			{
				document.ApprovedShamsiDateTime = null;
				document.ApprovedMiladiDateTime = null;
				document.RevieweShamsiDateTime = null;
				document.RevieweMiladiDateTime = null;
			}
			else if (model.Status == DocumentStatusEnums.Archived
				&& document.ApproverId == sdk.CurrentUser.Id)
			{
				document.ApprovedShamsiDateTime = now.ToShamsiDateTime();
				document.ApprovedMiladiDateTime = now;
				if (document.RevieweMiladiDateTime == null && IsDocumentReviewer(document, sdk.CurrentUser.Id))
				{
					document.RevieweShamsiDateTime = now.ToShamsiDateTime();
					document.RevieweMiladiDateTime = now;
				}
			}

			// مسیر وندور: تاییدکننده بعد از ApproveByClient فقط تاریخ تایید می‌زند؛ وضعیت ۷ (DCC) نمی‌شود
			var keepApproveByClient = isVendor
				&& previousStatus == DocumentStatusEnums.ApproveByClient
				&& model.Status == DocumentStatusEnums.ApproveByApprover;

			if (keepApproveByClient)
			{
				document.Status = DocumentStatusEnums.ApproveByClient;
				document.ApprovedShamsiDateTime = now.ToShamsiDateTime();
				document.ApprovedMiladiDateTime = now;
			}
			else
			{
				document.Status = model.Status;
			}

			document.ModifiedDateMiladiDateTime = DateTime.Now;
			document.ModifiedDateShamsiDateTime = DateTime.Now.ToShamsiDateTime();
			document.ModifiedById = sdk.CurrentUser.Id;
			document.ModifiedByName = sdk.CurrentUser.FullName;

			await unitOfWork.SaveChangesAsync(ct);

			var title = document.DocumentVpis?.Title;
			var shouldUpdateOpenOrderLinks =
				(model.Status == DocumentStatusEnums.ApprovedByDcc
					&& !string.IsNullOrEmpty(title)
					&& title.Contains('*'))
				|| model.Status == DocumentStatusEnums.NotReview;

			if (shouldUpdateOpenOrderLinks)
				await OpenOrderRequestVpisRevisionHelper.ApplyRevisionChangeForDocumentAsync(unitOfWork, document, ct);

			// اعلان نباید ذخیره گردش را خراب کند
			try
			{
				await NotifyDocumentStatusChangedAsync(document, model.Status.Value, ct);
			}
			catch
			{
			}
		}

		private static bool IsDocumentReviewer(Document document, long userId)
		{
			if (document.ReviewerId == userId)
				return true;

			var raw = document.DocumentVpis?.ReviewersId;
			if (string.IsNullOrWhiteSpace(raw))
				return false;

			foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				if (long.TryParse(part, out var id) && id == userId)
					return true;
			}

			return false;
		}

		private async Task NotifyDocumentStatusChangedAsync(
			Document document,
			DocumentStatusEnums newStatus,
			CancellationToken ct)
		{
			var vpis = document.DocumentVpis;
			if (vpis == null && document.DocumentVpisId is > 0)
			{
				vpis = await unitOfWork.Repository<ProjectVpis>().TableNoTracking
					.FirstOrDefaultAsync(v => v.Id == document.DocumentVpisId.Value, ct);
			}

			if (vpis == null)
				return;

			var actorId = sdk.CurrentUser?.Id;
			var recipients = new HashSet<long>();

			if (vpis.ProducerId is > 0 && vpis.ProducerId != actorId)
				recipients.Add(vpis.ProducerId.Value);

			if (vpis.ApproverId is > 0 && vpis.ApproverId != actorId)
				recipients.Add(vpis.ApproverId.Value);

			if (!string.IsNullOrWhiteSpace(vpis.ReviewersId))
			{
				foreach (var part in vpis.ReviewersId.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				{
					if (long.TryParse(part, out var id) && id > 0 && id != actorId)
						recipients.Add(id);
				}
			}

			if (recipients.Count == 0)
				return;

			var docTitle = string.IsNullOrWhiteSpace(vpis.Title) ? (vpis.Code ?? "مدرک") : vpis.Title;
			var statusText = newStatus.ToDisplay();
			var notifyTitle = "تغییر وضعیت مدرک";
			var notifyBody = $"وضعیت مدرک «{docTitle}» به «{statusText}» تغییر کرد.";
			var viewPath = $"/Panel/Edms/Document/EditAsGroup?id={document.Id}";

			foreach (var userId in recipients)
			{
				try
				{
					await notificationService.CreateAndSendAsync(new Notification
					{
						Type = NotificationType.Appliaction,
						Title = notifyTitle,
						Body = notifyBody,
						EntityId = document.Id,
						OwnerId = userId,
						ViewPath = viewPath,
						IsRead = false
					});
				}
				catch
				{
					try
					{
						await unitOfWork.Repository<Notification>().AddAsync(new Notification
						{
							Type = NotificationType.Appliaction,
							Title = notifyTitle,
							Body = notifyBody,
							EntityId = document.Id,
							OwnerId = userId,
							ViewPath = viewPath,
							IsRead = false
						}, ct, true);
					}
					catch
					{
						// ignore — ذخیره مدرک/کامنت نباید به‌خاطر اعلان fail شود
					}
				}
			}
		}
	}
}
