using Common.Attributes;
using Common.Utilities;
using Data.Contracts;
 using Data.SystemAuth;
using Entities.App.Bpm;
using Entities.App.Bpm.Enums;
using Entities.Auth;
using Entities.Base;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using Services.FileServices;
using Services.Job;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace App.BackgroundJob.Jobs.System
{
	public class EmailJob(IUnitOfWork unitOfWork,
	IUserService userService,
	IOnlineUserService onlineUserService,
	IFileService fileService,
	IConfiguration configuration)
	{
		[JobHandler("ارسال ایمیل های ارسال نشده")]
	 
		public async Task SendPendingEmailsAsync(
			IJobLogger? jobLogger = null,
			CancellationToken cancellationToken = default)
		{
			var settings = ReadSettings(configuration);
			var batchSize = settings.BatchSize > 0 ? settings.BatchSize : 200;

			if (!settings.SmtpHost.HasValue() || !settings.FromAddress.HasValue())
			{
				await jobLogger?.LogErrorAsync("تنظیمات SMTP (Email:SmtpHost / Email:FromAddress) در appsettings پیکربندی نشده است.", 0, cancellationToken);
				return;
			}

			var pendingNotifications = await unitOfWork.Repository<Notification>()
				.Table
				.Where(n => n.Type == NotificationType.Email && !n.IsSend && n.ErrorMessage == null)
				.OrderBy(n => n.Id)
				.Take(batchSize)
				.ToListAsync(cancellationToken);

			if (pendingNotifications.Count == 0)
			{
				await jobLogger?.LogInfoAsync("ایمیل ارسال‌نشده‌ای یافت نشد.", cancellationToken);
				return;
			}

			var ownerIds = pendingNotifications.Select(n => n.OwnerId).Distinct().ToArray();
			var ownerEmails = await userService.TableNoTracking
				.Where(u => u.Id != null && ownerIds.Contains(u.Id.Value))
				.ToDictionaryAsync(u => u.Id!.Value, u => u.Email, cancellationToken);

			var blockedEmails = await LoadBlockedEmailsAsync(cancellationToken);

			var result = new EmailSendBatchResult { Processed = pendingNotifications.Count };
			var prepared = new List<PreparedEmail>(pendingNotifications.Count);

			foreach (var notification in pendingNotifications)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var (toAddresses, ccAddresses) = ResolveRecipients(notification, ownerEmails);
				var removed = new List<string>();
				toAddresses = RemoveBlocked(toAddresses, blockedEmails, removed);
				ccAddresses = RemoveBlocked(ccAddresses, blockedEmails, removed);
				if (toAddresses.Count == 0)
				{
					notification.ErrorMessage = removed.Count > 0
						? "ارسال انجام نشد؛ همه گیرندگان در فهرست ایمیل‌های غیرفعال هستند."
						: "گیرنده‌ای برای ارسال ایمیل مشخص نشده است.";
					result.Failed++;
					var detail = removed.Count > 0
						? $"گیرنده‌ها در فهرست عدم ارسال بودند: {string.Join("، ", removed.Distinct(StringComparer.OrdinalIgnoreCase))}"
						: "گیرنده‌ای یافت نشد.";
					await jobLogger?.LogWarningAsync($"اعلان {notification.Id}: {detail}", 0, cancellationToken);
					continue;
				}

				if (removed.Count > 0)
				{
					await jobLogger?.LogInfoAsync(
						$"اعلان {notification.Id}: آدرس‌های فهرست عدم ارسال حذف شد: {string.Join("، ", removed.Distinct(StringComparer.OrdinalIgnoreCase))}",
						cancellationToken);
				}

				var (attachments, attachmentError) = await ResolveNotificationAttachmentsAsync(notification, cancellationToken);
				if (attachmentError != null)
				{
					notification.ErrorMessage = attachmentError;
					result.Failed++;
					await jobLogger?.LogWarningAsync($"اعلان {notification.Id}: {attachmentError}", 0, cancellationToken);
					continue;
				}

				prepared.Add(new PreparedEmail(notification, toAddresses, ccAddresses, attachments));
			}

			if (result.Failed > 0)
				await unitOfWork.SaveChangesAsync(cancellationToken);

			var units = BuildSendUnits(prepared);
			var concurrency = Math.Clamp(settings.MaxConcurrency <= 0 ? 8 : settings.MaxConcurrency, 1, 20);
			using var dbGate = new SemaphoreSlim(1, 1);

			await Parallel.ForEachAsync(units, new ParallelOptions
			{
				MaxDegreeOfParallelism = Math.Min(concurrency, Math.Max(1, units.Count)),
				CancellationToken = cancellationToken
			}, async (unit, token) =>
			{
				var sentIndividually = false;
				try
				{
					await SendEmailAsync(settings, unit.Subject, unit.Body, unit.To, unit.Cc, unit.Bcc, unit.Attachments, blockedEmails, token);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception ex) when (unit.Items.Count > 1)
				{
					await jobLogger?.LogWarningAsync(
						$"ارسال گروهی «{unit.Subject}» ناموفق بود؛ ارسال تک‌تک انجام می‌شود. {ex.Message}",
						0,
						token);

					foreach (var item in unit.Items)
					{
						token.ThrowIfCancellationRequested();
						try
						{
							await SendEmailAsync(settings, unit.Subject, unit.Body, item.To, item.Cc, [], item.Attachments, blockedEmails, token);
							await MarkSentAsync([item.Notification], token);
						}
						catch (OperationCanceledException)
						{
							throw;
						}
						catch (Exception itemEx)
						{
							await MarkFailedAsync(item.Notification, itemEx, token);
						}
					}

					sentIndividually = true;
				}
				catch (Exception ex)
				{
					await MarkFailedAsync(unit.Items[0].Notification, ex, token);
					return;
				}

				if (sentIndividually)
					return;

				try
				{
					await MarkSentAsync(unit.Notifications, token);
					await jobLogger?.LogInfoAsync($"ایمیل «{unit.Subject}» به {unit.RecipientCount} گیرنده ارسال شد.", token);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception ex)
				{
					await jobLogger?.LogWarningAsync(
						$"ایمیل «{unit.Subject}» ارسال شد ولی ذخیره وضعیت اعلان انجام نشد: {ex.Message}",
						0,
						token);
				}
			});

			await jobLogger?.LogInfoAsync(
				$"پایان ارسال ایمیل — پردازش: {result.Processed}، موفق: {result.Sent}، ناموفق: {result.Failed}، نامه SMTP: {units.Count}",
				cancellationToken);

			return;

			async Task MarkSentAsync(IReadOnlyList<Notification> notifications, CancellationToken token)
			{
				await dbGate.WaitAsync(token);
				try
				{
					var now = DateTime.Now;
					foreach (var notification in notifications)
					{
						notification.IsSend = true;
						notification.SendDateTime = now;
						notification.ErrorMessage = null;
					}

					await unitOfWork.SaveChangesAsync(token);
					result.Sent += notifications.Count;

					foreach (var notification in notifications)
					{
						if (!IsBpmAnnouncement(notification))
							continue;

						try
						{
							await AfterBpmAnnouncementSentAsync(notification, token);
						}
						catch (Exception hookEx)
						{
							await jobLogger?.LogWarningAsync(
								$"ایمیل ابلاغ ارسال شد ولی به‌روزرسانی شماره/نقش انجام نشد: {hookEx.Message}",
								0,
								token);
						}
					}
				}
				finally
				{
					dbGate.Release();
				}
			}

			async Task MarkFailedAsync(Notification notification, Exception ex, CancellationToken token)
			{
				var message = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
				await dbGate.WaitAsync(token);
				try
				{
					notification.ErrorMessage = message;
					result.Failed++;
					await unitOfWork.SaveChangesAsync(token);
				}
				finally
				{
					dbGate.Release();
				}

				await jobLogger?.LogExceptionAsync(ex, token);
			}
		}

		private static bool IsBpmAnnouncement(Notification notification)
		{
			if (!string.Equals(notification.Title, ProcessDocumentNotificationConstants.AnnouncementEmailTitle, StringComparison.Ordinal))
				return false;
			return notification.ViewPath != null
				&& notification.ViewPath.StartsWith(ProcessDocumentNotificationConstants.AnnouncementViewPath, StringComparison.OrdinalIgnoreCase);
		}

		private async Task<(List<EmailAttachment> Attachments, string? Error)> ResolveNotificationAttachmentsAsync(Notification notification, CancellationToken ct)
		{
			var requestedIds = notification.AttachmentFileIds?
				.Where(id => id > 0)
				.Distinct()
				.ToList() ?? [];

			if (requestedIds.Count > 0)
			{
				var files = await unitOfWork.Repository<FileEntity>().TableNoTracking
					.Where(f => f.Id != null && requestedIds.Contains(f.Id.Value))
					.ToListAsync(ct);
				var filesById = files
					.Where(f => f.Id != null)
					.GroupBy(f => f.Id!.Value)
					.ToDictionary(g => g.Key, g => g.First());

				var attachments = new List<EmailAttachment>(requestedIds.Count);
				var missingIds = new List<long>();
				foreach (var fileId in requestedIds)
				{
					if (!filesById.TryGetValue(fileId, out var file))
					{
						missingIds.Add(fileId);
						continue;
					}

					var attachment = TryCreateEmailAttachment(file);
					if (attachment == null)
						missingIds.Add(fileId);
					else
						attachments.Add(attachment);
				}

				if (missingIds.Count > 0)
					return ([], TruncateError($"پیوست ایمیل یافت نشد: {string.Join(", ", missingIds)}"));

				return (DisambiguateAttachmentNames(attachments), null);
			}

			var announcementAttachments = await ResolveBpmAnnouncementAttachmentsAsync(notification, ct);
			return (DisambiguateAttachmentNames(announcementAttachments), null);
		}

		private async Task<List<EmailAttachment>> ResolveBpmAnnouncementAttachmentsAsync(Notification notification, CancellationToken ct)
		{
			if (!IsBpmAnnouncement(notification) || notification.EntityId is null or 0)
				return [];

			var doc = await unitOfWork.Repository<ProcessDocument>().TableNoTracking
				.Where(d => d.Id == notification.EntityId)
				.Select(d => new { d.AccessLevel, d.DocumentType, d.FinalFileId, d.FinalPdfFileId })
				.FirstOrDefaultAsync(ct);
			if (doc == null)
				return [];
			if (doc.AccessLevel is ProcessDocumentAccessLevelEnum.Secret or ProcessDocumentAccessLevelEnum.Limit)
				return [];

			long? fileId = doc.DocumentType == ProcessDocumentTypeEnum.F
				? doc.FinalFileId
				: doc.FinalPdfFileId;
			if (fileId is null or 0)
				return [];

			var file = await unitOfWork.Repository<FileEntity>().TableNoTracking
				.FirstOrDefaultAsync(f => f.Id == fileId, ct);
			var attachment = TryCreateEmailAttachment(file);
			return attachment == null ? [] : [attachment];
		}

		private EmailAttachment? TryCreateEmailAttachment(FileEntity? file)
		{
			if (file == null || string.IsNullOrWhiteSpace(file.PhysicalPath))
				return null;

			var fullPath = fileService.GetFullPhysicalPath(file.PhysicalPath);
			if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
				return null;

			return new EmailAttachment(fullPath, SafeAttachmentName(file.OriginalName, fullPath), SafeContentType(file.ContentType));
		}

		private async Task AfterBpmAnnouncementSentAsync(Notification notification, CancellationToken ct)
		{
			var setting = await unitOfWork.Repository<ProcessDocumentModuleSetting>().TableNoTracking
				.OrderBy(s => s.Id)
				.Select(s => new { s.Id, s.LastAnnouncementNumber })
				.FirstOrDefaultAsync(ct);
			if (setting?.Id is > 0)
			{
				await unitOfWork.Repository<ProcessDocumentModuleSetting>().UpdateFieldsAsync(
					setting.Id.Value,
					new Dictionary<string, object>
					{
						[nameof(ProcessDocumentModuleSetting.LastAnnouncementNumber)] = setting.LastAnnouncementNumber + 1
					},
					ct);
			}

			if (notification.EntityId is null or 0)
				return;

			var document = await unitOfWork.Repository<ProcessDocument>().TableNoTracking
				.Include(d => d.NotificationRecipients)
				.Include(d => d.ExecuterOrgUnits)
				.FirstOrDefaultAsync(d => d.Id == notification.EntityId, ct);
			if (document == null)
				return;

			var userIds = document.NotificationRecipients.Select(x => x.UserId).Where(id => id > 0).ToList();
			if (document.AccessLevel == ProcessDocumentAccessLevelEnum.Limit)
			{
				var executerUnitIds = document.ExecuterOrgUnits.Select(x => x.OrgUnitId).Distinct().ToList();
				if (executerUnitIds.Count > 0)
				{
					var unitUsers = await userService.TableNoTracking
						.Where(u => u.Id != null
							&& u.OrgUnitId != null
							&& executerUnitIds.Contains(u.OrgUnitId.Value)
							&& u.IsActive == IsActiveEnum.Active)
						.Select(u => u.Id!.Value)
						.ToListAsync(ct);
					userIds.AddRange(unitUsers);
				}
			}

			var role = await unitOfWork.Repository<Role>().TableNoTracking
				.FirstOrDefaultAsync(r => r.Name == ProcessDocumentNotificationConstants.RolePublishedList, ct);
			if (role?.Id is null or 0)
				return;

			foreach (var userId in userIds.Where(id => id > 0).Distinct())
			{
				var user = await userService.GetById(userId);
				if (user == null)
					continue;

				user.RoleIds ??= [];
				user.Roles ??= [];
				if (user.RoleIds.Contains(role.Id.Value)
					|| user.Roles.Contains(ProcessDocumentNotificationConstants.RolePublishedList, StringComparer.OrdinalIgnoreCase))
					continue;

				user.RoleIds.Add(role.Id.Value);
				if (!user.Roles.Contains(ProcessDocumentNotificationConstants.RolePublishedList, StringComparer.OrdinalIgnoreCase))
					user.Roles.Add(ProcessDocumentNotificationConstants.RolePublishedList);

				await userService.AddAndUpdateUserAsync(user, ct);
				try
				{
					await onlineUserService.RefreshUserRoleAccessesAsync(userId, ct);
				}
				catch
				{
					// نقش در دیتابیس ذخیره شده.
				}
			}
		}

		private static (List<string> To, List<string> Cc) ResolveRecipients(
			Notification notification,
			Dictionary<long, string?> ownerEmails)
		{
			var to = NormalizeEmails(notification.ToEmails);
			var cc = NormalizeEmails(notification.CcEmails);

			if (to.Count == 0
			    && ownerEmails.TryGetValue(notification.OwnerId, out var ownerEmail)
			    && ownerEmail.HasValue())
			{
				to.Add(ownerEmail.Trim());
			}

			cc.RemoveAll(ccAddress => to.Contains(ccAddress, StringComparer.OrdinalIgnoreCase));

			return (to, cc);
		}

		private static List<string> NormalizeEmails(IEnumerable<string>? emails)
		{
			if (emails == null)
				return [];

			return emails
				.SelectMany(ExpandEmailAddresses)
				.Where(e => e.HasValue())
				.Select(e => e.Trim())
				.Where(IsValidEmail)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		/// <summary>
		/// فیلد ایمیل تامین‌کننده گاهی چند آدرس را با ; یا , در یک رشته نگه می‌دارد.
		/// MailAddress کل آن رشته را نامعتبر می‌داند و پیمانکار از To حذف می‌شد، در حالی که آدرس‌های داخلی معتبر می‌ماندند.
		/// یک آدرس معتبر، از جمله «Name &lt;a@b.com&gt;»، دست نمی‌خورد.
		/// </summary>
		private static IEnumerable<string> ExpandEmailAddresses(string? raw)
		{
			if (!raw.HasValue())
				yield break;

			foreach (var semi in raw!.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				var semiTrimmed = semi.Trim();
				if (IsValidEmail(semiTrimmed))
				{
					yield return semiTrimmed;
					continue;
				}

				var commaParts = semiTrimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (commaParts.Length <= 1)
				{
					yield return semiTrimmed;
					continue;
				}

				foreach (var comma in commaParts)
					yield return comma.Trim();
			}
		}

		private static bool IsValidEmail(string email)
		{
			try
			{
				_ = new MailAddress(email);
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static List<EmailSendUnit> BuildSendUnits(List<PreparedEmail> prepared)
		{
			var units = new List<EmailSendUnit>();
			var buckets = new Dictionary<string, List<PreparedEmail>>(StringComparer.Ordinal);

			foreach (var item in prepared)
			{
				if (item.To.Count != 1 || item.Cc.Count != 0)
				{
					units.Add(EmailSendUnit.FromSingle(item));
					continue;
				}

				var attachmentKey = string.Join('\u001f', item.Attachments.Select(attachment => attachment.FullPath).OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
				var key = string.Join('\u001e', item.Notification.Title ?? string.Empty, item.Notification.Body ?? string.Empty, attachmentKey);
				if (!buckets.TryGetValue(key, out var bucket))
				{
					bucket = [];
					buckets[key] = bucket;
				}

				bucket.Add(item);
			}

			foreach (var bucket in buckets.Values)
			{
				foreach (var chunk in bucket.Chunk(40))
				{
					var items = chunk.ToList();
					if (items.Count == 1)
					{
						units.Add(EmailSendUnit.FromSingle(items[0]));
						continue;
					}

					var recipients = items
						.Select(item => item.To[0])
						.Distinct(StringComparer.OrdinalIgnoreCase)
						.ToList();
					var first = items[0];
					units.Add(new EmailSendUnit
					{
						Items = items,
						Subject = first.Notification.Title,
						Body = first.Notification.Body ?? string.Empty,
						To = recipients.Count == 1 ? recipients : [],
						Cc = [],
						Bcc = recipients.Count == 1 ? [] : recipients,
						Attachments = first.Attachments
					});
				}
			}

			return units
				.OrderBy(unit => unit.Notifications.Min(notification => notification.Id ?? 0))
				.ToList();
		}

		private const string AlwaysCcAddress = "mirlohi.m@havayar.com";

		private async Task<HashSet<string>> LoadBlockedEmailsAsync(CancellationToken cancellationToken)
		{
			var rows = await unitOfWork.Repository<BlockedEmail>()
				.TableNoTracking
				.Where(x => x.IsActive == IsActiveEnum.Active && x.Email != "")
				.Select(x => x.Email)
				.ToListAsync(cancellationToken);

			var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var raw in rows)
			{
				if (TryGetMailbox(raw, out var mailbox))
					blocked.Add(mailbox);
			}

			return blocked;
		}

		private static List<string> RemoveBlocked(List<string> addresses, HashSet<string> blocked, List<string> removed)
		{
			if (blocked.Count == 0 || addresses.Count == 0)
				return addresses;

			var kept = new List<string>(addresses.Count);
			foreach (var address in addresses)
			{
				if (TryGetMailbox(address, out var mailbox) && blocked.Contains(mailbox))
				{
					removed.Add(mailbox);
					continue;
				}

				kept.Add(address);
			}

			return kept;
		}

		private static bool TryGetMailbox(string? raw, out string mailbox)
		{
			mailbox = string.Empty;
			if (!raw.HasValue())
				return false;

			try
			{
				mailbox = new MailAddress(raw!.Trim()).Address;
				return mailbox.HasValue();
			}
			catch
			{
				return false;
			}
		}

		private static async Task SendEmailAsync(
			EmailSettings settings,
			string subject,
			string body,
			IReadOnlyList<string> toAddresses,
			IReadOnlyList<string> ccAddresses,
			IReadOnlyList<string> bccAddresses,
			IReadOnlyList<EmailAttachment> attachments,
			HashSet<string> blockedEmails,
			CancellationToken cancellationToken)
		{
			using var message = new MailMessage
			{
				From = new MailAddress(settings.FromAddress, settings.FromDisplayName),
				Subject = subject,
				SubjectEncoding = Encoding.UTF8,
				Body = WrapHtmlBody(body),
				BodyEncoding = Encoding.UTF8,
				IsBodyHtml = true
			};

			foreach (var to in toAddresses)
				message.To.Add(to);

			foreach (var cc in ccAddresses)
				message.CC.Add(cc);

			if (!blockedEmails.Contains(AlwaysCcAddress))
				message.CC.Add(AlwaysCcAddress);

			foreach (var bcc in bccAddresses)
				message.Bcc.Add(bcc);

			foreach (var item in attachments)
				message.Attachments.Add(CreateMailAttachment(item));

			using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
			{
				EnableSsl = settings.EnableSsl,
				DeliveryMethod = SmtpDeliveryMethod.Network
			};

			if (settings.Username.HasValue())
				client.Credentials = new NetworkCredential(settings.Username, settings.Password ?? string.Empty);

			await client.SendMailAsync(message, cancellationToken);
		}

		/// <summary>
		/// بدنهٔ ایمیل‌های فارسی بدون <c>&lt;html&gt;</c>/<c>charset</c>/فونت پایه ارسال می‌شد؛ Outlook سلول‌ها را با Times New Roman
		/// و جهت چپ‌به‌راست نشان می‌داد و جدول به‌هم‌ریخته می‌شد. پوستهٔ RTL با فونت B Zar (فونت سازمانی؛ Zar تنها اغلب روی کلاینت پیدا نمی‌شود) دور بدنه می‌پیچد.
		/// </summary>
		private static string WrapHtmlBody(string body)
		{
			if (string.IsNullOrWhiteSpace(body))
				return body;
			if (body.Contains("<html", StringComparison.OrdinalIgnoreCase))
				return body;

			return "<!DOCTYPE html>\r\n"
				+ "<html dir=\"rtl\" lang=\"fa\">\r\n<head>\r\n"
				+ "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />\r\n"
				+ "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />\r\n"
				+ "<style type=\"text/css\">\r\n"
				+ "body, table, td, th, div, p, li, span { font-family: 'B Zar', Zar, Tahoma, Arial, sans-serif; direction: rtl; }\r\n"
				+ "body { margin: 0; padding: 8px; font-size: 11pt; text-align: right; }\r\n"
				+ "td, th { font-size: 11pt; }\r\n"
				+ "ul { margin: 0; padding-right: 20px; padding-left: 0; }\r\n"
				+ "</style>\r\n</head>\r\n"
				+ "<body dir=\"rtl\" style=\"font-family:'B Zar',Zar,Tahoma,Arial,sans-serif;direction:rtl;text-align:right;\">\r\n"
				+ body
				+ "\r\n</body>\r\n</html>";
		}

		private static EmailSettings ReadSettings(IConfiguration configuration)
		{
			return new EmailSettings
			{
				SmtpHost = configuration["Email:SmtpHost"] ?? string.Empty,
				SmtpPort = int.TryParse(configuration["Email:SmtpPort"], out var port) ? port : 587,
				EnableSsl = !bool.TryParse(configuration["Email:EnableSsl"], out var enableSsl) || enableSsl,
				Username = configuration["Email:Username"],
				Password = configuration["Email:Password"],
				FromAddress = configuration["Email:FromAddress"] ?? string.Empty,
				FromDisplayName = configuration["Email:FromDisplayName"] ?? "سامانه هوایار",
				BatchSize = int.TryParse(configuration["Email:BatchSize"], out var batchSize) ? batchSize : 200,
				MaxConcurrency = int.TryParse(configuration["Email:MaxConcurrency"], out var maxConcurrency) ? maxConcurrency : 8
			};
		}

		private sealed class PreparedEmail(
			Notification notification,
			List<string> to,
			List<string> cc,
			List<EmailAttachment> attachments)
		{
			public Notification Notification { get; } = notification;
			public List<string> To { get; } = to;
			public List<string> Cc { get; } = cc;
			public List<EmailAttachment> Attachments { get; } = attachments;
		}

		private sealed class EmailSendUnit
		{
			public List<PreparedEmail> Items { get; init; } = [];
			public string Subject { get; init; } = string.Empty;
			public string Body { get; init; } = string.Empty;
			public List<string> To { get; init; } = [];
			public List<string> Cc { get; init; } = [];
			public List<string> Bcc { get; init; } = [];
			public List<EmailAttachment> Attachments { get; init; } = [];

			public List<Notification> Notifications => Items.Select(item => item.Notification).ToList();

			public int RecipientCount => To.Count + Cc.Count + Bcc.Count;

			public static EmailSendUnit FromSingle(PreparedEmail item) => new()
			{
				Items = [item],
				Subject = item.Notification.Title,
				Body = item.Notification.Body ?? string.Empty,
				To = item.To,
				Cc = item.Cc,
				Bcc = [],
				Attachments = item.Attachments
			};
		}

		public class EmailSettings
		{
			public string SmtpHost { get; set; } = string.Empty;
			public int SmtpPort { get; set; } = 587;
			public bool EnableSsl { get; set; } = true;
			public string? Username { get; set; }
			public string? Password { get; set; }
			public string FromAddress { get; set; } = string.Empty;
			public string FromDisplayName { get; set; } = "سامانه هوایار";
			public int BatchSize { get; set; } = 200;
			public int MaxConcurrency { get; set; } = 8;
		}
		public class EmailSendBatchResult
		{
			public int Processed { get; set; }
			public int Sent { get; set; }
			public int Failed { get; set; }
		}

		private sealed record EmailAttachment(string FullPath, string FileName, string ContentType);

		private static Attachment CreateMailAttachment(EmailAttachment item)
		{
			var attachment = new Attachment(item.FullPath, item.ContentType);
			attachment.Name = item.FileName;
			attachment.NameEncoding = Encoding.UTF8;
			if (attachment.ContentDisposition != null)
			{
				attachment.ContentDisposition.FileName = item.FileName;
				attachment.ContentDisposition.DispositionType = DispositionTypeNames.Attachment;
			}

			return attachment;
		}

		private static List<EmailAttachment> DisambiguateAttachmentNames(List<EmailAttachment> attachments)
		{
			var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var result = new List<EmailAttachment>(attachments.Count);
			foreach (var item in attachments)
			{
				if (used.Add(item.FileName))
				{
					result.Add(item);
					continue;
				}

				var index = 2;
				string unique;
				do
				{
					unique = WithDuplicateSuffix(item.FileName, index);
					index++;
				}
				while (!used.Add(unique));

				result.Add(item with { FileName = unique });
			}

			return result;
		}

		private static string WithDuplicateSuffix(string fileName, int index)
		{
			var extension = Path.GetExtension(fileName);
			var stem = Path.GetFileNameWithoutExtension(fileName);
			return $"{stem} ({index}){extension}";
		}

		private static string SafeAttachmentName(string? originalName, string fullPath)
		{
			var diskName = SanitizeHeaderFileName(Path.GetFileName(fullPath));
			var name = string.IsNullOrWhiteSpace(originalName)
				? diskName
				: SanitizeHeaderFileName(Path.GetFileName(originalName.Trim()));
			if (string.IsNullOrWhiteSpace(name))
				name = diskName;
			if (string.IsNullOrWhiteSpace(name))
				name = "attachment";

			var diskExtension = Path.GetExtension(diskName);
			if (!string.IsNullOrEmpty(diskExtension) && string.IsNullOrEmpty(Path.GetExtension(name)))
				name += diskExtension;

			const int maxLength = 180;
			if (name.Length <= maxLength)
				return name;

			var extension = Path.GetExtension(name);
			var stem = Path.GetFileNameWithoutExtension(name);
			var stemLength = Math.Max(1, maxLength - extension.Length);
			return (stem.Length > stemLength ? stem[..stemLength] : stem) + extension;
		}

		private static string SanitizeHeaderFileName(string? name)
		{
			if (string.IsNullOrWhiteSpace(name))
				return string.Empty;

			var cleaned = new string(name.Where(c => c != '"' && c != '\r' && c != '\n' && !char.IsControl(c)).ToArray());
			return cleaned.Trim();
		}

		private static string SafeContentType(string? contentType)
		{
			var value = contentType?.Trim();
			if (string.IsNullOrEmpty(value))
				return MediaTypeNames.Application.Octet;

			try
			{
				_ = new ContentType(value);
				return value;
			}
			catch (FormatException)
			{
				return MediaTypeNames.Application.Octet;
			}
		}

		private static string TruncateError(string message)
		{
			return message.Length > 2000 ? message[..2000] : message;
		}

	}
}
