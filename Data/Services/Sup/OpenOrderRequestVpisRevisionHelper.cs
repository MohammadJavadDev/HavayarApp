using Common.Utilities;
using Data.Contracts;
using Entities.App.Edms;
using Entities.App.Sup;
using Entities.Auth;
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

			await unitOfWork.SaveChangesAsync(cancellationToken);
		}

		await AddRevisionNotificationsAsync(unitOfWork, changed, latestByVpis, cancellationToken);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return changed.Count;
	}

	/// <summary>
	/// HTS: یک ایمیل برای همهٔ درخواست‌های همان VPIS.
	/// گیرنده: کارشناس فروش، مدیر فروش، مدیر پروژه. رونوشت: متولی خرید.
	/// </summary>
	private static async Task AddRevisionNotificationsAsync(
		IUnitOfWork unitOfWork,
		List<OpenOrderRequestVpis> changed,
		Dictionary<long, Document> latestByVpis,
		CancellationToken cancellationToken)
	{
		var requestIds = changed.Select(v => v.OpenOrderRequestId).Distinct().ToList();
		var requests = await unitOfWork.Repository<OpenOrderRequest>().TableNoTracking
			.Include(o => o.SalesUnitSalesExpert)
			.Include(o => o.SalesUnitSalesManager)
			.Include(o => o.SalesUnitProjectManager)
			.Include(o => o.Part)
				.ThenInclude(p => p.BuyCategory)
					.ThenInclude(b => b!.PurchaseResponsible)
			.Where(o => requestIds.Contains(o.Id!.Value))
			.ToListAsync(cancellationToken);
		var requestById = requests.ToDictionary(r => r.Id!.Value);

		var vpisIds = changed.Select(v => v.ProjectVpisId).Distinct().ToList();
		var vpisRows = await unitOfWork.Repository<ProjectVpis>().TableNoTracking
			.Where(v => vpisIds.Contains(v.Id!.Value))
			.Select(v => new { v.Id, v.Code, v.ProjectNameId })
			.ToListAsync(cancellationToken);
		var vpisById = vpisRows.ToDictionary(v => v.Id!.Value);

		var projectIds = changed.Select(v => v.ProjectId).Distinct().ToList();
		var projects = await unitOfWork.Repository<Project>().TableNoTracking
			.Where(p => projectIds.Contains(p.Id!.Value))
			.Select(p => new { p.Id, p.Code, p.ProjectName })
			.ToListAsync(cancellationToken);
		var projectById = projects.ToDictionary(p => p.Id!.Value);

		foreach (var vpisGroup in changed.GroupBy(v => v.ProjectVpisId))
		{
			var links = vpisGroup.ToList();
			var related = links
				.Select(v => requestById.GetValueOrDefault(v.OpenOrderRequestId))
				.Where(r => r != null)
				.ToList();
			if (related.Count == 0)
				continue;

			var to = new List<string>();
			foreach (var request in related)
			{
				AddUserEmail(to, request!.SalesUnitSalesExpert);
				AddUserEmail(to, request.SalesUnitSalesManager);
				AddUserEmail(to, request.SalesUnitProjectManager);
			}

			var cc = new List<string>();
			foreach (var request in related)
			{
				var buyer = request!.Part?.BuyCategory?.PurchaseResponsible;
				AddEmail(cc, buyer?.Email.HasValue() == true ? buyer.Email : buyer?.Username);
			}
			cc.RemoveAll(c => to.Any(t => string.Equals(t, c, StringComparison.OrdinalIgnoreCase)));

			if (to.Count == 0)
				continue;

			projectById.TryGetValue(links[0].ProjectId, out var project);
			vpisById.TryGetValue(vpisGroup.Key, out var vpis);
			latestByVpis.TryGetValue(vpisGroup.Key, out var document);

			var requestNumbers = string.Join(" | ", related.Select(r => r!.PurchaseRequestNumber.ToString()).Distinct());
			var orderNumbers = string.Join(" | ", related.Select(r => r!.OrderNo?.ToString()).Where(n => n.HasValue()).Distinct());
			var documentNumber = vpis?.Code ?? document?.Id?.ToString() ?? "-";

			var body = $@"
<div style='text-align:center;direction:rtl'>
<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl' width='100%'>
<tr style='background: #000aa0'><td colspan='2'><div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center'>گروه صنعتی هوایار</div></td></tr>
<tr><td colspan='2'><div style='font-size:14.0pt;font-family:Zar;color:#1F497D;text-align:right;direction:rtl'>
با سلام و احترام <br />
بدینوسیله اعلام میگردد رویژن جدیدی از مدرکی با مشخصات ذیل در سامانه بارگذاری گردید، لذا خواهمشند است نسبت به بررسی درخواست های باز آن مساعدت فرمایید
<br /><br />
<ul>
<li>کد پروژه : <strong>{project?.Code}</strong></li>
<li>عنوان پروژه : <strong>{project?.ProjectName}</strong></li>
<li>شماره مدرک : <strong>{documentNumber}</strong></li>
<li>شماره درخواست های خرید : <strong>{requestNumbers}</strong></li>
<li>شماره سفارش های خرید : <strong>{orderNumbers}</strong></li>
</ul>
</div></td></tr>
</table>
</div>";

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = "اعلان افزایش رویژن مدرک مهندسی",
				Body = body,
				EntityId = related[0]!.Id,
				OwnerId = related[0]!.SalesUnitSalesExpertId ?? 1,
				ViewPath = $"/Panel/Sup/OpenOrderRequest/Edit?id={related[0]!.Id}",
				IsRead = false,
				IsSend = false,
				ToEmails = to,
				CcEmails = cc.Count == 0 ? null : cc
			}, cancellationToken, saveAudit: false, saveNow: false, InvokeAction: false);
		}
	}

	private static void AddUserEmail(List<string> target, User? user)
	{
		if (user == null)
			return;
		AddEmail(target, user.Email.HasValue() ? user.Email : user.Username);
	}

	private static void AddEmail(List<string> target, string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return;
		var email = value.Trim();
		if (!email.Contains('@'))
			email += "@havayar.com";
		if (target.Any(e => string.Equals(e, email, StringComparison.OrdinalIgnoreCase)))
			return;
		target.Add(email);
	}
}
