using Common.Utilities;
using Data.Contracts;
using Entities.App.Bpm;
using Entities.App.Bpm.Enums;
using Entities.App.Hcm;
using Entities.Auth;
using Entities.Base;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Services.Auth;
using Services.NotificationServices;
using System.Text;
using System.Text.Json;

namespace WebApp.Actions.Bpm
{
	/// <summary>
	/// معادل HTS SendNotificationEmail / SendEditOrUpdateEmail / SendAnnouncementNotificationEmail
	/// با صف Notification (EmailJob) و زنگ in-app.
	/// </summary>
	internal sealed class ProcessDocumentNotificationDispatcher(
		IUnitOfWork unitOfWork,
		IUserService userService,
		INotificationService notificationService,
		IConfiguration configuration)
	{
		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			PropertyNameCaseInsensitive = true
		};

		public async Task DispatchAfterStatusLogAsync(ProcessDocumentStatusLog log, CancellationToken ct)
		{
			if (log.ProcessDocumentId <= 0)
				return;

			var document = await LoadDocumentAsync(log.ProcessDocumentId, ct);
			if (document == null)
				return;
			if (IsSystemOwner(document))
				return;

			switch (log.Status)
			{
				case ProcessDocumentStatusEnum.Updated:
					await SendEditOrUpdateEmailAsync(document, "اصلاح", isBySupervisor: false, ct);
					return;
				case ProcessDocumentStatusEnum.BpmUpdated:
					await SendEditOrUpdateEmailAsync(document, "اصلاح", isBySupervisor: true, ct);
					return;
				case ProcessDocumentStatusEnum.CancelRequest:
					await SendEditOrUpdateEmailAsync(document, "لغو", isBySupervisor: false, ct);
					return;
				case ProcessDocumentStatusEnum.Notified:
					await SendAnnouncementAsync(document, log, ct);
					return;
				case ProcessDocumentStatusEnum.IgnoreProcessAndForceToNotify:
					return;
			}

			await SendStatusChangeEmailsAsync(document, log.Status, ct);
		}

		private async Task SendStatusChangeEmailsAsync(
			ProcessDocument document,
			ProcessDocumentStatusEnum status,
			CancellationToken ct)
		{
			if (status is ProcessDocumentStatusEnum.Issue or ProcessDocumentStatusEnum.Reject)
			{
				await SendOneStatusEmailAsync(document, status, isSendForCreator: true, ct);
				return;
			}

			if (status == ProcessDocumentStatusEnum.InProgress)
			{
				await SendOneStatusEmailAsync(document, status, isSendForCreator: true, ct);
				await SendOneStatusEmailAsync(document, status, isSendForCreator: false, ct);
				return;
			}

			if (status is ProcessDocumentStatusEnum.BpmExpertApproved
				or ProcessDocumentStatusEnum.BpmApproved
				or ProcessDocumentStatusEnum.ProcessOwnerApproved
				or ProcessDocumentStatusEnum.ProcessOwnerCommented
				or ProcessDocumentStatusEnum.ApproverApproved
				or ProcessDocumentStatusEnum.ApproverCommented
				or ProcessDocumentStatusEnum.FinalApproverApproved
				or ProcessDocumentStatusEnum.FinalApproverCommented
				or ProcessDocumentStatusEnum.BpmCancelRequest
				or ProcessDocumentStatusEnum.BpmAcceptComments
				or ProcessDocumentStatusEnum.BpmReject)
			{
				await SendOneStatusEmailAsync(document, status, isSendForCreator: false, ct);
			}
		}

		private async Task SendOneStatusEmailAsync(
			ProcessDocument document,
			ProcessDocumentStatusEnum status,
			bool isSendForCreator,
			CancellationToken ct)
		{
			var settings = await LoadSettingsAsync(ct);
			var to = new List<string>();
			var cc = new List<string>();
			var roleNames = new List<string>();
			var inAppUserIds = new HashSet<long>();
			string title;
			string innerHtml;

			if (isSendForCreator)
			{
				title = status == ProcessDocumentStatusEnum.Issue
					? "اعلان درخواست ایجاد/تغییر سند"
					: "اعلان تایید درخواست ایجاد/تغییر سند";
				roleNames.Add(ProcessDocumentNotificationConstants.RoleSupervisor);
				innerHtml = BuildCreatorEmailContent(document, status);
				AddUserEmail(to, document.RequesterUser, inAppUserIds);
			}
			else
			{
				switch (status)
				{
					case ProcessDocumentStatusEnum.BpmCancelRequest:
					case ProcessDocumentStatusEnum.BpmReject:
						title = "اعلان رد درخواست";
						roleNames.Add(ProcessDocumentNotificationConstants.RoleSupervisor);
						roleNames.Add(ProcessDocumentNotificationConstants.RoleExpert);
						innerHtml = BuildCreatorEmailContent(document, status);
						AddUserEmail(to, document.RequesterUser, inAppUserIds);
						break;

					case ProcessDocumentStatusEnum.InProgress:
					case ProcessDocumentStatusEnum.BpmAcceptComments:
						title = "اعلان اسناد در انتظار اقدام کارشناس";
						roleNames.Add(ProcessDocumentNotificationConstants.RoleSupervisor);
						roleNames.Add(ProcessDocumentNotificationConstants.RoleExpert);
						innerHtml = BuildExpertEmailContent(document);
						break;

					case ProcessDocumentStatusEnum.BpmApproved:
						title = "اعلان اسناد در انتظار اقدام";
						roleNames.Add(ProcessDocumentNotificationConstants.RoleSupervisor);
						innerHtml = BuildSupervisorEmailContent(document);
						AddUserEmail(to, document.ProcessOwner, inAppUserIds);
						break;

					case ProcessDocumentStatusEnum.BpmExpertApproved:
						title = "اعلان اسناد در انتظار اقدام سیستم ها وروش ها";
						roleNames.Add(ProcessDocumentNotificationConstants.RoleSupervisor);
						roleNames.Add(ProcessDocumentNotificationConstants.RoleExpert);
						innerHtml = BuildBpmEmailContent(document);
						break;

					case ProcessDocumentStatusEnum.ProcessOwnerApproved:
						title = "اعلان تایید/کامنت مالک مجموعه فرآیندی";
						roleNames.Add(ProcessDocumentNotificationConstants.RoleSupervisor);
						innerHtml = BuildProcessOwnerEmailContent(document, isCommentMode: false);
						AddUserEmail(to, document.Approver, inAppUserIds);
						AddUserEmail(cc, document.ProcessOwner, inAppUserIds);
						break;

					case ProcessDocumentStatusEnum.ProcessOwnerCommented:
						title = "اعلان تایید/کامنت مالک مجموعه فرآیندی";
						roleNames.Add(ProcessDocumentNotificationConstants.RoleSupervisor);
						innerHtml = BuildProcessOwnerEmailContent(document, isCommentMode: true);
						AddUserEmail(to, document.ProcessOwner, inAppUserIds);
						break;

					case ProcessDocumentStatusEnum.ApproverApproved:
						title = "اعلان تایید/کامنت تاییدکننده فرآیند";
						roleNames.Add(ProcessDocumentNotificationConstants.RoleSupervisor);
						innerHtml = BuildApproverEmailContent(document, isCommentMode: false);
						AddUserEmail(to, document.FinalApprover, inAppUserIds);
						break;

					case ProcessDocumentStatusEnum.ApproverCommented:
						title = "اعلان تایید/کامنت تاییدکننده فرآیند";
						roleNames.Add(ProcessDocumentNotificationConstants.RoleSupervisor);
						innerHtml = BuildApproverEmailContent(document, isCommentMode: true);
						break;

					case ProcessDocumentStatusEnum.FinalApproverApproved:
						title = "اعلان تایید/کامنت تصویب کننده فرآیند";
						roleNames.Add(ProcessDocumentNotificationConstants.RoleSupervisor);
						innerHtml = BuildFinalApproverEmailContent(document, isCommentMode: false);
						break;

					case ProcessDocumentStatusEnum.FinalApproverCommented:
						title = "اعلان تایید/کامنت  تصویب کننده فرآیند";
						roleNames.Add(ProcessDocumentNotificationConstants.RoleSupervisor);
						innerHtml = BuildFinalApproverEmailContent(document, isCommentMode: true);
						break;

					default:
						return;
				}
			}

			await AddRoleEmailsAsync(to, inAppUserIds, roleNames, ct);
			await ApplyConditionalCcAsync(to, cc, settings, ct);

			var body = WrapStatusEmail(innerHtml);
			await EnqueueAsync(
				document,
				title,
				body,
				to,
				cc,
				inAppUserIds,
				ProcessDocumentNotificationConstants.ManageEditViewPath,
				ct);
		}

		private async Task SendEditOrUpdateEmailAsync(
			ProcessDocument document,
			string operationTypeTitle,
			bool isBySupervisor,
			CancellationToken ct)
		{
			var settings = await LoadSettingsAsync(ct);
			var sb = new StringBuilder();
			sb.AppendLine("<tr>");
			sb.AppendLine("<td>");
			sb.AppendLine("با سلام");
			sb.AppendLine("<br />");
			sb.AppendLine(isBySupervisor
				? $@"درخواست ایجاد/ تغییر سند یا سیستم با شماره درخواست {document.Id} توسط سرپرست {operationTypeTitle} گردید"
				: $@"درخواست ایجاد/ تغییر سند یا سیستم با شماره درخواست {document.Id} توسط درخواست دهنده {operationTypeTitle} گردید");
			sb.AppendLine("<br />");
			sb.AppendLine("<br />");

			if (isBySupervisor)
			{
				sb.AppendLine("<ul>");
				AppendLi(sb, "شماره درخواست", document.Id);
				AppendLi(sb, "نام سند / سیستم", document.DocumentTitle);
				AppendLi(sb, "نوع سند", document.DocumentType?.ToDisplay());
				AppendLi(sb, "کد سند", document.DocumentNumber);
				AppendLi(sb, "مجموعه فرآیندی", FormatProcessSet(document));
				AppendLi(sb, "مالک مجموعه فرآیندی", UserDisplay(document.ProcessOwner));
				AppendLi(sb, "تایید کننده", UserDisplay(document.Approver));
				AppendLi(sb, "تصویب کننده", UserDisplay(document.FinalApprover));
				AppendLi(sb, "تاریخ پیش بینی انجام درخواست", document.CompletionForecastShamsiDate);
				sb.AppendLine("</ul>");
			}

			sb.AppendLine("</td>");
			sb.AppendLine("</tr>");

			var to = new List<string>();
			var cc = new List<string>();
			var inAppUserIds = new HashSet<long>();
			var roles = new List<string> { ProcessDocumentNotificationConstants.RoleSupervisor };
			if (isBySupervisor)
				roles.Add(ProcessDocumentNotificationConstants.RoleExpert);

			await AddRoleEmailsAsync(to, inAppUserIds, roles, ct);
			await ApplyConditionalCcAsync(to, cc, settings, ct);

			await EnqueueAsync(
				document,
				"اعلان اصلاح/حذف سند",
				WrapStatusEmail(sb.ToString()),
				to,
				cc,
				inAppUserIds,
				isBySupervisor
					? ProcessDocumentNotificationConstants.ManageEditViewPath
					: ProcessDocumentNotificationConstants.RequestEditViewPath,
				ct);
		}

		private async Task SendAnnouncementAsync(ProcessDocument document, ProcessDocumentStatusLog log, CancellationToken ct)
		{
			var settings = await LoadSettingsAsync(ct);
			var lastAnnouncement = settings?.LastAnnouncementNumber ?? 0;
			var announcementNumber = lastAnnouncement + 1;
			var now = DateTime.Now;
			var isPrivate = document.AccessLevel is ProcessDocumentAccessLevelEnum.Secret
				or ProcessDocumentAccessLevelEnum.Limit;

			var beneficiaryIds = document.BeneficiaryOrgUnits.Select(x => x.OrgUnitId).Distinct().ToList();
			var executerIds = document.ExecuterOrgUnits.Select(x => x.OrgUnitId).Distinct().ToList();
			var recipientUserIds = document.NotificationRecipients.Select(x => x.UserId).Where(id => id > 0).Distinct().ToList();
			var editingOrgUnitIds = document.EditingOrgUnits.Select(x => x.OrgUnitId).Distinct().ToList();

			if (beneficiaryIds.Count == 0
				&& executerIds.Count == 0
				&& recipientUserIds.Count == 0
				&& editingOrgUnitIds.Count == 0)
				return;

			var body = BuildAnnouncementHtml(document, log, announcementNumber, now, isPrivate);

			var unitIds = new List<long>();
			unitIds.AddRange(beneficiaryIds);
			if (document.AccessLevel != ProcessDocumentAccessLevelEnum.Secret)
				unitIds.AddRange(executerIds);

			var to = new List<string>();
			var cc = new List<string>();
			var inAppUserIds = new HashSet<long>();

			await AddOrgUnitEmailsAsync(to, inAppUserIds, unitIds, ct);
			await AddUserIdEmailsAsync(to, inAppUserIds, recipientUserIds, ct);
			await AddOrgUnitEmailsAsync(to, inAppUserIds, editingOrgUnitIds, ct);

			AddSettingEmails(cc, settings?.AnnouncementAlwaysCc);

			if (to.Count == 0)
				to.AddRange(cc);

			await AddRoleEmailsAsync(cc, inAppUserIds, [
				ProcessDocumentNotificationConstants.RoleSupervisor,
				ProcessDocumentNotificationConstants.RoleExpert
			], ct);

			await ApplyConditionalCcAsync(to, cc, settings, ct);

			if (to.Count == 0)
				return;

			await EnqueueAsync(
				document,
				ProcessDocumentNotificationConstants.AnnouncementEmailTitle,
				body,
				to,
				cc,
				inAppUserIds,
				ProcessDocumentNotificationConstants.AnnouncementViewPath,
				ct);
		}

		private async Task EnqueueAsync(
			ProcessDocument document,
			string title,
			string body,
			List<string> to,
			List<string> cc,
			HashSet<long> inAppUserIds,
			string viewPathPrefix,
			CancellationToken ct)
		{
			to = DistinctEmails(to);
			cc = DistinctEmails(cc).Where(x => !to.Contains(x, StringComparer.OrdinalIgnoreCase)).ToList();
			if (to.Count == 0 && cc.Count > 0)
			{
				to = cc;
				cc = [];
			}

			if (to.Count == 0)
				return;

			var viewPath = $"{viewPathPrefix}?id={document.Id}";
			var ownerId = inAppUserIds.FirstOrDefault();
			if (ownerId == 0)
				ownerId = document.RequesterUserId;

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = title,
				Body = body,
				EntityId = document.Id,
				OwnerId = ownerId,
				ViewPath = viewPath,
				IsRead = false,
				IsSend = false,
				ToEmails = to,
				CcEmails = cc
			}, ct, true);

			foreach (var userId in inAppUserIds.Where(id => id > 0).Distinct())
			{
				try
				{
					await notificationService.CreateAndSendAsync(new Notification
					{
						Type = NotificationType.Appliaction,
						Title = title,
						Body = StripToPlain(body),
						EntityId = document.Id,
						OwnerId = userId,
						ViewPath = viewPath,
						IsRead = false
					});
				}
				catch
				{
					await unitOfWork.Repository<Notification>().AddAsync(new Notification
					{
						Type = NotificationType.Appliaction,
						Title = title,
						Body = StripToPlain(body),
						EntityId = document.Id,
						OwnerId = userId,
						ViewPath = viewPath,
						IsRead = false
					}, ct, true);
				}
			}
		}

		private async Task AddRoleEmailsAsync(
			List<string> target,
			HashSet<long> inAppUserIds,
			IReadOnlyCollection<string> roleNames,
			CancellationToken ct)
		{
			foreach (var roleName in roleNames.Distinct(StringComparer.OrdinalIgnoreCase))
			{
				User[]? users;
				try
				{
					users = await userService.GetUsersByRoleName(roleName);
				}
				catch
				{
					continue;
				}

				if (users == null)
					continue;

				foreach (var user in users.Where(u => u.IsActive == IsActiveEnum.Active))
					AddUserEmail(target, user, inAppUserIds);
			}

			await Task.CompletedTask;
		}

		private async Task AddOrgUnitEmailsAsync(
			List<string> target,
			HashSet<long> inAppUserIds,
			IReadOnlyCollection<long> orgUnitIds,
			CancellationToken ct)
		{
			var ids = orgUnitIds.Where(id => id > 0).Distinct().ToList();
			if (ids.Count == 0)
				return;

			var personnelEmails = await unitOfWork.Repository<Personel>().TableNoTracking
				.Where(p => p.OrgUnitId != null
					&& ids.Contains(p.OrgUnitId.Value)
					&& p.IsActive == IsActiveEnum.Active
					&& p.Email != null
					&& p.Email != "")
				.Select(p => p.Email!)
				.ToListAsync(ct);
			target.AddRange(personnelEmails.Select(ToEmailAddress).Where(e => e.Length > 0));

			var users = await userService.TableNoTracking
				.Where(u => u.OrgUnitId != null
					&& ids.Contains(u.OrgUnitId.Value)
					&& u.IsActive == IsActiveEnum.Active)
				.ToListAsync(ct);
			foreach (var user in users)
				AddUserEmail(target, user, inAppUserIds);
		}

		private async Task AddUserIdEmailsAsync(
			List<string> target,
			HashSet<long> inAppUserIds,
			IReadOnlyCollection<long> userIds,
			CancellationToken ct)
		{
			var ids = userIds.Where(id => id > 0).Distinct().ToList();
			if (ids.Count == 0)
				return;

			var users = await userService.TableNoTracking
				.Where(u => u.Id != null && ids.Contains(u.Id.Value) && u.IsActive == IsActiveEnum.Active)
				.ToListAsync(ct);
			foreach (var user in users)
				AddUserEmail(target, user, inAppUserIds);
		}

		private async Task ApplyConditionalCcAsync(
			List<string> to,
			List<string> cc,
			ProcessDocumentModuleSetting? settings,
			CancellationToken ct)
		{
			if (settings == null || string.IsNullOrWhiteSpace(settings.AnnouncementConditionalCcJson))
				return;

			List<ProcessDocumentConditionalCcRule>? rules;
			try
			{
				rules = JsonSerializer.Deserialize<List<ProcessDocumentConditionalCcRule>>(
					settings.AnnouncementConditionalCcJson, JsonOptions);
			}
			catch
			{
				return;
			}

			if (rules == null || rules.Count == 0)
				return;

			foreach (var rule in rules)
			{
				if (string.IsNullOrWhiteSpace(rule.WhenToContains))
					continue;
				if (!to.Any(e => e.Contains(rule.WhenToContains, StringComparison.OrdinalIgnoreCase)))
					continue;

				if (rule.CcEmails != null)
				{
					foreach (var email in rule.CcEmails)
					{
						var normalized = ToEmailAddress(email);
						if (normalized.Length > 0)
							cc.Add(normalized);
					}
				}

				if (rule.CcOrgUnitId is > 0)
					await AddOrgUnitEmailsAsync(cc, [], [rule.CcOrgUnitId.Value], ct);
			}
		}

		private void AddUserEmail(List<string> target, User? user, HashSet<long> inAppUserIds)
		{
			if (user?.Id is > 0)
				inAppUserIds.Add(user.Id.Value);

			var email = ResolveUserEmail(user);
			if (email.Length > 0)
				target.Add(email);
		}

		private string ResolveUserEmail(User? user)
		{
			if (user == null)
				return "";
			if (!string.IsNullOrWhiteSpace(user.Email))
				return ToEmailAddress(user.Email);
			if (!string.IsNullOrWhiteSpace(user.Username))
				return ToEmailAddress(user.Username);
			return "";
		}

		private string ToEmailAddress(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return "";
			var trimmed = value.Trim();
			if (trimmed.Contains('@'))
				return trimmed;
			return trimmed + EmailDomain();
		}

		private string EmailDomain()
		{
			var domain = configuration[ProcessDocumentNotificationConstants.DefaultEmailSuffixKey];
			if (string.IsNullOrWhiteSpace(domain))
				return ProcessDocumentNotificationConstants.DefaultEmailDomain;
			return domain.StartsWith('@') ? domain : "@" + domain;
		}

		private static void AddSettingEmails(List<string> target, string? csv)
		{
			if (string.IsNullOrWhiteSpace(csv))
				return;
			foreach (var part in csv.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				if (part.Length > 0)
					target.Add(part.Contains('@') ? part : part + ProcessDocumentNotificationConstants.DefaultEmailDomain);
			}
		}

		private static List<string> DistinctEmails(IEnumerable<string> emails)
			=> emails
				.Where(e => !string.IsNullOrWhiteSpace(e))
				.Select(e => e.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

		private async Task<ProcessDocument?> LoadDocumentAsync(long id, CancellationToken ct)
		{
			return await unitOfWork.Repository<ProcessDocument>().TableNoTracking
				.Include(d => d.RequesterUser)
				.Include(d => d.CreatedOrganizationUnit)
				.Include(d => d.ProcessOwner)
				.Include(d => d.Approver)
				.Include(d => d.FinalApprover)
				.Include(d => d.StatusLogs)
				.Include(d => d.EditingOrgUnits)
				.Include(d => d.ExecuterOrgUnits)
				.Include(d => d.BeneficiaryOrgUnits)
				.Include(d => d.NotificationRecipients)
				.Include(d => d.ProcessExecuters).ThenInclude(x => x.User)
				.Include(d => d.MainFile)
				.Include(d => d.TempFile)
				.Include(d => d.FinalFile)
				.Include(d => d.FinalPdfFile)
				.Include(d => d.CommentFile)
				.FirstOrDefaultAsync(d => d.Id == id, ct);
		}

		private async Task<ProcessDocumentModuleSetting?> LoadSettingsAsync(CancellationToken ct)
		{
			return await unitOfWork.Repository<ProcessDocumentModuleSetting>().TableNoTracking
				.OrderBy(s => s.Id)
				.FirstOrDefaultAsync(ct);
		}

		private static bool IsSystemOwner(ProcessDocument document)
			=> string.Equals(document.Comment, ProcessDocumentNotificationConstants.SystemOwnerComment, StringComparison.OrdinalIgnoreCase);

		private static string WrapStatusEmail(string innerRowHtml)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<div style='width: 100%;text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl'  width='100%' >");
			sb.AppendLine("<tr style='background: #000aa0'>");
			sb.AppendLine("<td>");
			sb.AppendLine("<div  style='font-size:14.0pt;font-family:Tahoma;color:#FFFFFF;text-align:center;direction:rtl'>");
			sb.AppendLine("گروه صنعتی هوایار");
			sb.AppendLine("</div>");
			sb.AppendLine("</td>");
			sb.AppendLine("</tr>");
			sb.AppendLine(innerRowHtml);
			sb.AppendLine("</table>");
			sb.AppendLine("</div>");
			return sb.ToString();
		}

		private static string BuildCreatorEmailContent(ProcessDocument document, ProcessDocumentStatusEnum status)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<tr><td>");
			sb.AppendLine("با سلام");
			sb.AppendLine("<br />");

			var lastComment = LatestLog(document)?.Comment;
			switch (status)
			{
				case ProcessDocumentStatusEnum.Issue:
					sb.AppendLine("درخواست ایجاد/ تغییر سند یا سیستم با مشخصات زیر به کارتابل مدیر/ سرپرست برنامه ریزی و سیستم ها جهت بررسی ارسال گردیده است");
					sb.AppendLine("<br /><br /><ul>");
					AppendLi(sb, "شماره درخواست", document.Id);
					AppendLi(sb, "کد سند", document.DocumentNumber);
					AppendLi(sb, "عنوان سند", document.DocumentTitle);
					AppendLi(sb, "شرح درخواست", document.Comment);
					AppendLi(sb, "درخواست کننده", UserDisplay(document.RequesterUser));
					AppendLi(sb, "واحد درخواست کننده", document.CreatedOrganizationUnit?.Title);
					AppendLi(sb, "فوریت رسیدگی", document.Priority.ToDisplay());
					sb.AppendLine("</ul>");
					break;
				case ProcessDocumentStatusEnum.Reject:
					sb.AppendLine($@"درخواست شماره <span style='color:red'>{document.Id}</span> بعلت <span style='color:red'>{lastComment}</span> مورد تایید واحد برنامه ریزی و سیستم ها نمی باشد");
					break;
				case ProcessDocumentStatusEnum.BpmCancelRequest:
					sb.AppendLine($@"درخواست شماره <span style='color:red'>{document.Id}</span> بعلت <span style='color:red'>{lastComment}</span> توسط واحد برنامه ریزی و سیستم ها رد شد");
					break;
				case ProcessDocumentStatusEnum.BpmReject:
					sb.AppendLine($@"درخواست شماره <span style='color:red'>{document.Id}</span> بعلت <span style='color:red'>{lastComment}</span> توسط سرپرست برنامه ریزی و سیستم ها رد شد");
					break;
				case ProcessDocumentStatusEnum.InProgress:
					sb.AppendLine($@"درخواست شماره <span style='color:red'>{document.Id}</span> مورد تایید واحد برنامه ریزی و سیستم ها قرار گرفت و تاریخ پیش بینی انجام درخواست <span style='color:red'>{document.CompletionForecastShamsiDate}</span>  می باشد");
					break;
			}

			sb.AppendLine("<br /><br /></td></tr>");
			return sb.ToString();
		}

		private static string BuildExpertEmailContent(ProcessDocument document)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<tr><td>");
			sb.AppendLine("با سلام");
			sb.AppendLine("<br />");
			sb.AppendLine("درخواست با مشخصات زیر جهت اقدام به کارتابل کارشناس تضمین کیفیت ارسال شده است");
			sb.AppendLine("<br /><br /><ul>");
			AppendLi(sb, "شماره درخواست", document.Id);
			AppendLi(sb, "شرح درخواست", document.Comment);
			AppendLi(sb, "درخواست کننده", UserDisplay(document.RequesterUser));
			AppendLi(sb, "واحد درخواست کننده", document.CreatedOrganizationUnit?.Title);
			AppendLi(sb, "فوریت رسیدگی", document.Priority.ToDisplay());
			AppendLi(sb, "مهلت انجام", document.CompletionForecastShamsiDate);
			AppendLi(sb, "کامنت آخر", LatestLog(document, ProcessDocumentStatusEnum.BpmApproved)?.Comment);
			sb.AppendLine("</ul></td></tr>");
			return sb.ToString();
		}

		private static string BuildBpmEmailContent(ProcessDocument document)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<tr><td>");
			sb.AppendLine("با سلام");
			sb.AppendLine("<br />");
			sb.AppendLine("درخواست با مشخصات زیر جهت توسط کارشناس تضمین کیفیت تکمیل و جهت بررسی خدمتتان ارسال شده است");
			sb.AppendLine("<br /><br /><ul>");
			AppendLi(sb, "شماره درخواست", document.Id);
			AppendLi(sb, "شرح درخواست", document.Comment);
			AppendLi(sb, "درخواست کننده", UserDisplay(document.RequesterUser));
			AppendLi(sb, "واحد درخواست کننده", document.CreatedOrganizationUnit?.Title);
			AppendLi(sb, "فوریت رسیدگی", document.Priority.ToDisplay());
			var last = LatestLog(document, ProcessDocumentStatusEnum.BpmExpertApproved);
			if (last != null)
			{
				AppendLi(sb, "زمان دریافت", last.ReceiveShamsiDate);
				AppendLi(sb, "زمان ارسال", last.SendShamsiDate);
				AppendLi(sb, "کامنت", last.Comment);
			}
			sb.AppendLine("</ul></td></tr>");
			return sb.ToString();
		}

		private static string BuildSupervisorEmailContent(ProcessDocument document)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<tr><td>");
			sb.AppendLine("با سلام و احترام <br />");
			if (document.LastStatus == ProcessDocumentStatusEnum.BpmApproved)
				sb.AppendLine("درخواست، با مشخصات زیر توسط سرپرست برنامه ریزی و سیستم ها تایید و به کارتابل شما ارسال شده است");
			else if (document.LastStatus == ProcessDocumentStatusEnum.BpmAcceptComments)
				sb.AppendLine("کامنت های دریافتی از درخواست با مشخصات زیر توسط سرپرست برنامه ریزی و سیستم ها بررسی و مورد تایید قرار گرفته و به کارتابل شما ارسال شده است");
			sb.AppendLine("<br /><br /><ul>");
			AppendLi(sb, "شماره درخواست", document.Id);
			AppendLi(sb, "کامنت آخر", LatestLog(document, ProcessDocumentStatusEnum.BpmApproved)?.Comment);
			AppendLi(sb, "مالک مجموعه فرآیندی", UserDisplay(document.ProcessOwner));
			sb.AppendLine("</ul></td></tr>");
			return sb.ToString();
		}

		private static string BuildProcessOwnerEmailContent(ProcessDocument document, bool isCommentMode)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<tr><td>با سلام<br />");
			sb.AppendLine(isCommentMode
				? $"درخواست با مشخصات زیر توسط مالک فرآیند <span style='color:red'>{UserDisplay(document.ProcessOwner)}</span> کامنت و به کارتابل برنامه ریزی و سیستم ها ارسال گردید"
				: $"درخواست با مشخصات زیر توسط مالک فرآیند <span style='color:red'>{UserDisplay(document.ProcessOwner)}</span> تایید و به کارتابل تایید کننده ارسال گردید");
			sb.AppendLine("<br /><br /><ul>");
			AppendLi(sb, "شماره درخواست", document.Id);
			AppendLi(sb, "کد سند", document.DocumentNumber);
			AppendLi(sb, "عنوان سند", document.DocumentTitle);
			var last = LatestLog(document, ProcessDocumentStatusEnum.ProcessOwnerCommented, ProcessDocumentStatusEnum.ProcessOwnerApproved);
			if (last != null)
				AppendLi(sb, "کامنت آخر", last.Comment);
			sb.AppendLine("</ul></td></tr>");
			return sb.ToString();
		}

		private static string BuildApproverEmailContent(ProcessDocument document, bool isCommentMode)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<tr><td>با سلام<br />");
			sb.AppendLine(isCommentMode
				? $"درخواست با مشخصات زیر توسط تاییدکننده فرآیند <span style='color:red'>{UserDisplay(document.Approver)}</span> کامنت و به کارتابل برنامه ریزی و سیستم ها ارسال گردید"
				: $"درخواست با مشخصات زیر توسط تاییدکننده فرآیند <span style='color:red'>{UserDisplay(document.Approver)}</span> تایید و به کارتابل تصویب کننده ارسال گردید");
			sb.AppendLine("<br /><br /><ul>");
			AppendLi(sb, "شماره درخواست", document.Id);
			AppendLi(sb, "کد سند", document.DocumentNumber);
			AppendLi(sb, "عنوان سند", document.DocumentTitle);
			if (!isCommentMode)
			{
				var ownerRow = LatestLog(document, ProcessDocumentStatusEnum.ProcessOwnerApproved);
				AppendLi(sb, "مالک فرآیند / تاریخ تایید", $"{UserDisplay(document.ProcessOwner)} / {ownerRow?.CreatedOnShamsiDateTime}");
				AppendLi(sb, " تایید کننده / تاریخ تایید", $"{UserDisplay(document.Approver)} / {DateTime.Now.ToShamsiDate()}");
			}
			var last = LatestLog(document, ProcessDocumentStatusEnum.ApproverCommented, ProcessDocumentStatusEnum.ApproverApproved);
			if (last != null)
				AppendLi(sb, "کامنت آخر", last.Comment);
			sb.AppendLine("</ul></td></tr>");
			return sb.ToString();
		}

		private static string BuildFinalApproverEmailContent(ProcessDocument document, bool isCommentMode)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<tr><td>با سلام<br />");
			sb.AppendLine(isCommentMode
				? $"درخواست با مشخصات زیر توسط تصویب کننده فرآیند <span style='color:red'>{UserDisplay(document.FinalApprover)}</span> کامنت و به کارتابل برنامه ریزی و سیستم ها ارسال گردید"
				: $"درخواست با مشخصات زیر توسط تصویب کننده فرآیند <span style='color:red'>{UserDisplay(document.FinalApprover)}</span> تایید گردید و جهت ابلاغ برای واحد برنامه ریزی و سیستم ها ارسال گردید");
			sb.AppendLine("<br /><br /><ul>");
			AppendLi(sb, "شماره درخواست", document.Id);
			AppendLi(sb, "کد سند", document.DocumentNumber);
			AppendLi(sb, "عنوان سند", document.DocumentTitle);
			if (!isCommentMode)
			{
				var ownerRow = LatestLog(document, ProcessDocumentStatusEnum.ProcessOwnerApproved);
				var approverRow = LatestLog(document, ProcessDocumentStatusEnum.ApproverApproved);
				AppendLi(sb, "مالک فرآیند / تاریخ تایید", $"{UserDisplay(document.ProcessOwner)} / {ownerRow?.CreatedOnShamsiDateTime}");
				AppendLi(sb, "تایید کننده / تاریخ تایید", $"{UserDisplay(document.Approver)} / {approverRow?.CreatedOnShamsiDateTime}");
				AppendLi(sb, "تصویب کننده / تاریخ تصویب", $"{UserDisplay(document.FinalApprover)} / {DateTime.Now.ToShamsiDate()}");
			}
			var last = LatestLog(document, ProcessDocumentStatusEnum.FinalApproverApproved, ProcessDocumentStatusEnum.FinalApproverCommented);
			if (last != null)
				AppendLi(sb, "کامنت آخر", last.Comment);
			sb.AppendLine("</ul></td></tr>");
			return sb.ToString();
		}

		private static string BuildAnnouncementHtml(
			ProcessDocument document,
			ProcessDocumentStatusLog log,
			int announcementNumber,
			DateTime now,
			bool isPrivate)
		{
			var year = now.ToShamsiDate()[..4];
			var executerNames = string.Join("، ",
				document.ProcessExecuters
					.Select(x => UserDisplay(x.User))
					.Where(n => !string.IsNullOrWhiteSpace(n) && n != "-"));

			var sb = new StringBuilder();
			sb.AppendLine("<div style='text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl'  width='100%' >");
			sb.AppendLine("<tr style='background: #000aa0'><td colspan='2'>");
			sb.AppendLine("<div  style='font-size:14.0pt;font-family:Tahoma;color:#FFFFFF;text-align:center;direction:rtl'>گروه صنعتی هوایار</div>");
			sb.AppendLine("</td></tr>");
			sb.AppendLine("<tr>");
			sb.AppendLine("<td style='width:80%'><div  style='font-size:14.0pt;font-family:Tahoma;text-align:center;direction:rtl'>");
			sb.AppendLine("<strong>ابلاغیه</strong><br />");
			sb.AppendLine("<strong>مستندات سیستم مدیریت یکپارچه</strong><br />");
			sb.AppendLine("آخرین ویرایش مستندات زیر در سامانه جامع هوایار بارگذاری گردیده و از تاریخ ابلاغ لازم الاجرا می‏باشند.");
			sb.AppendLine("</div></td>");
			sb.AppendLine("<td style='width:20%'><div  style='font-size:13.0pt;font-family:Tahoma;text-align:right;direction:rtl'>");
			sb.AppendLine($"شماره ابلاغیه : {announcementNumber:000}-{year}<br />");
			sb.AppendLine($"تاریخ : {now.ToShamsiDate()}");
			sb.AppendLine("</div></td></tr>");
			sb.AppendLine("<tr><td colspan='2'>");
			sb.AppendLine("<table style ='font-family:Tahoma;font-size:14.0pt;width:100%; text-align:center' border='1'>");
			sb.AppendLine("<tr>");
			sb.AppendLine("<th>شرح اقدام</th><th>نام سند / سیستم</th><th>کد سند</th><th>مجموعه فرآیندی</th>");
			sb.AppendLine("<th>مالک مجموعه فرآیندی</th><th>واحدهای مجری/ همکار</th><th>توضیحات</th>");
			sb.AppendLine("</tr><tr>");
			sb.AppendLine($"<td>{document.RequestType.ToDisplay()}</td>");
			sb.AppendLine($"<td>{document.DocumentTitle}</td>");
			sb.AppendLine($"<td>{document.DocumentNumber}</td>");
			sb.AppendLine($"<td>{FormatProcessSet(document)}</td>");
			sb.AppendLine($"<td>{UserDisplay(document.ProcessOwner)}</td>");
			sb.AppendLine($"<td>{executerNames}</td>");
			sb.AppendLine($"<td>{log.Comment ?? LatestLog(document)?.Comment}</td>");
			sb.AppendLine("</tr></table>");

			if (isPrivate)
			{
				sb.AppendLine("<table style ='font-family:Tahoma;font-size:10pt;width:100%'><tr>");
				sb.AppendLine("<td style='text-align:right;font-size:8pt;color:#4c8fbd'>");
				sb.AppendLine($"جهت مشاهده و دریافت فایل به آدرس <strong>سامانه جامع هوایار / سیستم مدیریت اسناد / اسناد فرآیندی / لیست اسناد فرآیندی</strong> مراجعه نموده و فایل، با شناسه <strong>{document.Id}</strong> را دانلود نمایید");
				sb.AppendLine("</td></tr></table>");
			}

			sb.AppendLine("</td></tr></table></div>");
			return sb.ToString();
		}

		private static ProcessDocumentStatusLog? LatestLog(ProcessDocument document, params ProcessDocumentStatusEnum[] statuses)
		{
			var logs = document.StatusLogs ?? [];
			var query = logs.OrderByDescending(x => x.Id);
			if (statuses.Length == 0)
				return query.FirstOrDefault();
			return query.FirstOrDefault(x => statuses.Contains(x.Status));
		}

		private static string FormatProcessSet(ProcessDocument document)
		{
			if (document.ProcessSet == null)
				return "";
			var title = document.ProcessSet.Value.ToDisplay();
			var code = ProcessDocumentTypeCodes.GetLookupCode(document.ProcessSet.Value);
			return string.IsNullOrWhiteSpace(code) ? title : $"{title} {code}";
		}

		private static string UserDisplay(User? user)
		{
			if (user == null)
				return "-";
			if (!string.IsNullOrWhiteSpace(user.NameFa))
				return user.NameFa;
			if (!string.IsNullOrWhiteSpace(user.Name))
				return user.Name;
			return string.IsNullOrWhiteSpace(user.Username) ? "-" : user.Username;
		}

		private static void AppendLi(StringBuilder sb, string label, object? value)
		{
			sb.AppendLine("<li>");
			sb.AppendLine($"{label} : <strong>{value}</strong>");
			sb.AppendLine("</li>");
		}

		private static string StripToPlain(string html)
		{
			if (string.IsNullOrWhiteSpace(html))
				return html;
			var text = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", " ");
			return System.Net.WebUtility.HtmlDecode(text).Trim();
		}
	}

	internal sealed class ProcessDocumentConditionalCcRule
	{
		public string? WhenToContains { get; set; }
		public List<string>? CcEmails { get; set; }
		public long? CcOrgUnitId { get; set; }
	}
}
