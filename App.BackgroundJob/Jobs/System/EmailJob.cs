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
			var batchSize = settings.BatchSize > 0 ? settings.BatchSize : 50;

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

			var result = new EmailSendBatchResult { Processed = pendingNotifications.Count };

			foreach (var notification in pendingNotifications)
			{
				cancellationToken.ThrowIfCancellationRequested();

				try
				{
					var (toAddresses, ccAddresses) = ResolveRecipients(notification, ownerEmails);

					if (toAddresses.Count == 0)
					{
						notification.ErrorMessage = "گیرنده‌ای برای ارسال ایمیل مشخص نشده است.";
						result.Failed++;
						await jobLogger?.LogWarningAsync($"اعلان {notification.Id}: گیرنده‌ای یافت نشد.", 0, cancellationToken);
						continue;
					}

					var attachments = await ResolveNotificationAttachmentsAsync(notification, cancellationToken);

					await SendEmailAsync(settings, notification, toAddresses, ccAddresses, attachments, cancellationToken);

					notification.IsSend = true;
					notification.SendDateTime = DateTime.Now;
					notification.ErrorMessage = null;
					result.Sent++;

					if (IsBpmAnnouncement(notification))
					{
						try
						{
							await AfterBpmAnnouncementSentAsync(notification, cancellationToken);
						}
						catch (Exception hookEx)
						{
							await jobLogger?.LogWarningAsync(
								$"ایمیل ابلاغ ارسال شد ولی به‌روزرسانی شماره/نقش انجام نشد: {hookEx.Message}",
								0,
								cancellationToken);
						}
					}

					await jobLogger?.LogInfoAsync($"ایمیل «{notification.Title}» به {toAddresses.Count} گیرنده ارسال شد.", cancellationToken);
				}
				catch (Exception ex)
				{
					notification.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
					result.Failed++;
					await jobLogger?.LogExceptionAsync(ex, cancellationToken);
				}
			}

			await unitOfWork.SaveChangesAsync(cancellationToken);

			await jobLogger?.LogInfoAsync(
				$"پایان ارسال ایمیل — پردازش: {result.Processed}، موفق: {result.Sent}، ناموفق: {result.Failed}",
				cancellationToken);

			return;
		}

		private static bool IsBpmAnnouncement(Notification notification)
		{
			if (!string.Equals(notification.Title, ProcessDocumentNotificationConstants.AnnouncementEmailTitle, StringComparison.Ordinal))
				return false;
			return notification.ViewPath != null
				&& notification.ViewPath.StartsWith(ProcessDocumentNotificationConstants.AnnouncementViewPath, StringComparison.OrdinalIgnoreCase);
		}

		private async Task<List<string>> ResolveNotificationAttachmentsAsync(Notification notification, CancellationToken ct)
		{
			var paths = new List<string>();
			if (notification.AttachmentFileIds is { Count: > 0 })
			{
				var fileIds = notification.AttachmentFileIds.Where(id => id > 0).Distinct().ToList();
				var files = await unitOfWork.Repository<FileEntity>().TableNoTracking
					.Where(f => f.Id != null && fileIds.Contains(f.Id.Value))
					.ToListAsync(ct);
				foreach (var file in files)
				{
					if (string.IsNullOrWhiteSpace(file.PhysicalPath))
						continue;
					var fullPath = fileService.GetFullPhysicalPath(file.PhysicalPath);
					if (!string.IsNullOrWhiteSpace(fullPath) && File.Exists(fullPath))
						paths.Add(fullPath);
				}
			}

			if (paths.Count == 0)
				paths.AddRange(await ResolveBpmAnnouncementAttachmentsAsync(notification, ct));
			return paths;
		}

		private async Task<List<string>> ResolveBpmAnnouncementAttachmentsAsync(Notification notification, CancellationToken ct)
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
			if (file == null || string.IsNullOrWhiteSpace(file.PhysicalPath))
				return [];

			var fullPath = fileService.GetFullPhysicalPath(file.PhysicalPath);
			if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
				return [];

			return [fullPath];
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
				.Where(e => e.HasValue())
				.Select(e => e.Trim())
				.Where(IsValidEmail)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
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

		private static async Task SendEmailAsync(
			EmailSettings settings,
			Notification notification,
			IReadOnlyList<string> toAddresses,
			IReadOnlyList<string> ccAddresses,
			IReadOnlyList<string> attachmentPaths,
			CancellationToken cancellationToken)
		{
			using var message = new MailMessage
			{
				From = new MailAddress(settings.FromAddress, settings.FromDisplayName),
				Subject = notification.Title,
				Body = notification.Body ?? string.Empty,
				IsBodyHtml = true
			};

			foreach (var to in toAddresses)
				message.To.Add(to);

			foreach (var cc in ccAddresses)
				message.CC.Add(cc);

			foreach (var path in attachmentPaths.Where(File.Exists))
				message.Attachments.Add(new Attachment(path));

			using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
			{
				EnableSsl = settings.EnableSsl,
				DeliveryMethod = SmtpDeliveryMethod.Network
			};

			if (settings.Username.HasValue())
				client.Credentials = new NetworkCredential(settings.Username, settings.Password ?? string.Empty);

			await client.SendMailAsync(message, cancellationToken);
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
				BatchSize = int.TryParse(configuration["Email:BatchSize"], out var batchSize) ? batchSize : 50
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
			public int BatchSize { get; set; } = 50;
		}
		public class EmailSendBatchResult
		{
			public int Processed { get; set; }
			public int Sent { get; set; }
			public int Failed { get; set; }
		}

	}
}
