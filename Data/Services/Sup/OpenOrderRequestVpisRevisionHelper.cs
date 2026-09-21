using Common.Utilities;
using Data.Contracts;
using Entities.App.Edms;
using Entities.App.Sup;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.EntityFrameworkCore;

namespace Data.Services.Sup;

/// <summary>
/// به‌روزرسانی لینک VPIS درخواست باز بعد از رویژن جدید — معادل
/// HTS CheckOpenOrderRequestVpisRevisionAndSendEmailNotification.
/// </summary>
public static class OpenOrderRequestVpisRevisionHelper
{
	public static async Task<int> ApplyRevisionChangeForDocumentAsync(
		IUnitOfWork unitOfWork,
		Document document,
		CancellationToken cancellationToken)
	{
		if (document.Id is null or 0 || document.DocumentVpisId is null)
			return 0;

		return await ApplyRevisionChangesAsync(unitOfWork, [document], cancellationToken);
	}

	public static async Task<int> ApplyRevisionChangesAsync(
		IUnitOfWork unitOfWork,
		IReadOnlyList<Document> latestDocuments,
		CancellationToken cancellationToken)
	{
		var latestByVpis = latestDocuments
			.Where(d => d.Id.HasValue && d.DocumentVpisId.HasValue)
			.GroupBy(d => d.DocumentVpisId!.Value)
			.ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.Id).First());

		if (latestByVpis.Count == 0)
			return 0;

		var vpisIds = latestByVpis.Keys.ToList();
		var links = await unitOfWork.Repository<OpenOrderRequestVpis>().Table
			.Include(v => v.OpenOrderRequest)
			.Include(v => v.Project)
			.Where(v => v.IsLatest
				&& vpisIds.Contains(v.ProjectVpisId)
				&& !v.OpenOrderRequest.IsDeleted
				&& !v.OpenOrderRequest.IsForceDeletedByUser
				&& v.OpenOrderRequest.SalesUnitSalesExpertId != null)
			.ToListAsync(cancellationToken);

		var changed = links
			.Where(v =>
			{
				if (!latestByVpis.TryGetValue(v.ProjectVpisId, out var doc))
					return false;
				return v.DocumentId != doc.Id || v.RevisionNumber != doc.Revision;
			})
			.ToList();

		if (changed.Count == 0)
			return 0;

		foreach (var group in changed.GroupBy(v => v.OpenOrderRequestId))
		{
			var request = group.First().OpenOrderRequest;
			request.HasSalesUnitConfirmation = false;

			foreach (var link in group)
				link.IsLatest = false;

			await unitOfWork.SaveChangesAsync(cancellationToken);

			var now = DateTime.Now;
			foreach (var link in group)
			{
				var doc = latestByVpis[link.ProjectVpisId];
				await unitOfWork.Repository<OpenOrderRequestVpis>().AddAsync(new OpenOrderRequestVpis
				{
					OpenOrderRequestId = link.OpenOrderRequestId,
					ProjectId = link.ProjectId,
					ProjectVpisId = link.ProjectVpisId,
					DocumentId = doc.Id,
					RevisionNumber = doc.Revision,
					IsLatest = true,
					Comment = $"بروزرسانی خودکار Revision از {link.RevisionNumber} به {doc.Revision}"
				}, cancellationToken, saveAudit: false, saveNow: true, InvokeAction: false);
			}

			await unitOfWork.Repository<OpenOrderRequestComment>().AddAsync(new OpenOrderRequestComment
			{
				OpenOrderRequestId = group.Key,
				MiladiDate = now,
				ShamsiDate = now.ToShamsiDate(),
				HasSalesUnitConfirmation = false,
				SalesUnitConfirmationComment = "منتظر تایید پروژه/فروش بعلت تغییر در رویژن مدرک مهندسی",
				CommentValue = "تغییر Revision مدارک VPIS - نیاز به تایید مجدد واحد فروش/پروژه"
			}, cancellationToken, saveAudit: false, saveNow: true, InvokeAction: false);

			await AddRevisionNotificationsAsync(unitOfWork, request, group.ToList(), cancellationToken);
			await unitOfWork.SaveChangesAsync(cancellationToken);
		}

		return changed.Count;
	}

	private static async Task AddRevisionNotificationsAsync(
		IUnitOfWork unitOfWork,
		OpenOrderRequest openOrderRequest,
		List<OpenOrderRequestVpis> changedVpis,
		CancellationToken cancellationToken)
	{
		var recipientUserIds = new List<long>();
		if (openOrderRequest.SalesUnitSalesExpertId.HasValue)
			recipientUserIds.Add(openOrderRequest.SalesUnitSalesExpertId.Value);
		if (openOrderRequest.SalesUnitSalesManagerId.HasValue)
			recipientUserIds.Add(openOrderRequest.SalesUnitSalesManagerId.Value);
		if (openOrderRequest.SalesUnitProjectManagerId.HasValue)
			recipientUserIds.Add(openOrderRequest.SalesUnitProjectManagerId.Value);

		if (recipientUserIds.Count == 0)
			return;

		var firstVpis = changedVpis.FirstOrDefault();
		var project = firstVpis?.Project;
		var projectName = project?.ProjectName ?? "نامشخص";
		var projectCode = project?.Code ?? "-";

		const string title = "تغییر Revision مدرک مهندسی - نیاز به تایید مجدد";
		var body = $@"
                    <div style='text-align:center;direction:rtl'>
                        <table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl' width='100%'>
                            <tr style='background: #000aa0'>
                                <td><div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center'>گروه صنعتی هوایار</div></td>
                            </tr>
                            <tr><td>
                                <div style='font-size:12pt;text-align:right;direction:rtl'>
                                    <p>با سلام و احترام</p>
                                    <p>رویژن جدیدی از مدارک پروژه در سامانه بارگذاری گردید. لذا خواهشمند است نسبت به بررسی مجدد درخواست های باز اقدام فرمایید.</p>
                                    <ul>
                                        <li>کد پروژه: <strong>{projectCode}</strong></li>
                                        <li>عنوان پروژه: <strong>{projectName}</strong></li>
                                        <li>شماره درخواست خرید: <strong>{openOrderRequest.PurchaseRequestNumber}</strong></li>
                                        <li>شماره سفارش خرید: <strong>{openOrderRequest.OrderNo}</strong></li>
                                    </ul>
                                </div>
                            </td></tr>
                        </table>
                    </div>
                ";

		foreach (var userId in recipientUserIds.Distinct())
		{
			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = title,
				Body = body,
				EntityId = openOrderRequest.Id,
				OwnerId = userId,
				ViewPath = $"Panel/Sup/OpenOrderRequest/Edit/{openOrderRequest.Id}",
				IsRead = false
			}, cancellationToken, saveAudit: false, saveNow: false, InvokeAction: false);
		}
	}
}
