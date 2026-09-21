using Data.Contracts;
using Data.Contracts.Actions;
using Data.SystemAuth;
using Entities.App.Sale;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Services.Auth;

namespace WebApp.Actions.Sale
{
	/// <summary>
	/// معادل ایمیل‌های DoOperation مانیتورینگ فروش HTS: خروج از کارخانه و راه‌اندازی.
	/// </summary>
	public class OrderDetailSerialAction(
		IUnitOfWork unitOfWork,
		IUserService userService,
		ISdk sdk,
		IConfiguration configuration)
	{
		public const string RoleFactoryExitNotify = "Sale.AfterSales.FactoryExitNotify";
		public const string RoleFactoryExitCustomerCall = "Sale.AfterSales.FactoryExitCustomerCall";
		public const string LaunchNotifyEmail = "Ahadi.s@havayar.com";

		private DateTime? _previousFinalExit;
		private string? _previousLaunch;

		[EntityAction(typeof(OrderDetailSerial), EntityActionTrigger.BeforeUpdate,
			"OrderDetailSerialCaptureMonitoringDates", "ثبت تاریخ قبلی خروج و راه‌اندازی برای ایمیل مانیتورینگ", Priority = 1)]
		public async Task CapturePreviousDates(OrderDetailSerial entity, CancellationToken ct)
		{
			if (entity.Id == null || entity.Id == 0)
				return;

			var existing = await unitOfWork.Repository<OrderDetailSerial>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == entity.Id, ct);
			_previousFinalExit = existing?.FinalExitMiladiDate;
			_previousLaunch = existing?.LaunchShamsiDate;
		}

		[EntityAction(typeof(OrderDetailSerial), EntityActionTrigger.AfterUpdate,
			"OrderDetailSerialMonitoringEmails", "ایمیل خروج از کارخانه و راه‌اندازی کالا", Priority = 1)]
		public async Task AfterUpdateEmails(OrderDetailSerial entity, CancellationToken ct)
		{
			if (entity.Id == null || entity.Id == 0)
				return;

			var finalExitChanged = entity.FinalExitMiladiDate.HasValue
				&& entity.FinalExitMiladiDate != _previousFinalExit;
			var launchChanged = !string.IsNullOrWhiteSpace(entity.LaunchShamsiDate)
				&& !string.Equals(entity.LaunchShamsiDate, _previousLaunch, StringComparison.Ordinal);

			if (!finalExitChanged && !launchChanged)
				return;

			var model = await unitOfWork.Repository<OrderDetailSerial>().TableNoTracking
				.Include(s => s.OrderDetail).ThenInclude(od => od.Part)
				.Include(s => s.OrderDetail).ThenInclude(od => od.Sale_Order).ThenInclude(o => o.Customer).ThenInclude(c => c.Party)
				.FirstOrDefaultAsync(c => c.Id == entity.Id, ct);
			if (model == null)
				return;

			var partCode = model.OrderDetail?.Part?.Code ?? "";
			var partName = model.OrderDetail?.Part?.Name ?? "";
			var customerTitle = model.OrderDetail?.Sale_Order?.Customer?.Party?.FullName ?? "";
			var customerCode = model.OrderDetail?.Sale_Order?.Customer?.Code?.ToString() ?? "";
			var userName = sdk.CurrentUser?.FullName ?? sdk.CurrentUser?.FullNameFn ?? "";
			var userEmail = ToHavayarEmail(sdk.CurrentUser?.Email ?? sdk.CurrentUser?.Username);

			if (finalExitChanged && (IsScrewCompressor(partCode) || IsPackageWithCompressor(partName)))
			{
				await SendExitEmailAsync(model, partCode, partName, customerTitle, customerCode, userName, userEmail, false, ct);
				await SendExitEmailAsync(model, partCode, partName, customerTitle, customerCode, userName, userEmail, true, ct);
			}

			if (launchChanged)
				await SendLaunchEmailAsync(model, partCode, partName, customerTitle, customerCode, userName, userEmail, ct);
		}

		private async Task SendExitEmailAsync(
			OrderDetailSerial entity,
			string partCode,
			string partName,
			string customerTitle,
			string customerCode,
			string userName,
			string? userEmail,
			bool forCustomerCall,
			CancellationToken ct)
		{
			var title = forCustomerCall
				? "اعلان خروج کالا از کارخانه - جهت برقراری تماس با مشتری"
				: "اعلان خروج کالا از کارخانه";
			var body = BuildExitBody(entity, partCode, partName, customerTitle, customerCode, userName);
			var roleName = forCustomerCall ? RoleFactoryExitCustomerCall : RoleFactoryExitNotify;
			var htsGroupId = forCustomerCall ? 570 : 542;
			var receivers = await ResolveRecipientsAsync(roleName, htsGroupId, ct);
			await AddNotificationsAsync(title, body, entity.Id, receivers, userEmail, ct);
		}

		private async Task SendLaunchEmailAsync(
			OrderDetailSerial entity,
			string partCode,
			string partName,
			string customerTitle,
			string customerCode,
			string userName,
			string? userEmail,
			CancellationToken ct)
		{
			var body = BuildLaunchBody(entity, partCode, partName, customerTitle, customerCode, userName);
			await AddNotificationsAsync(
				"اعلان راه اندازی کالا",
				body,
				entity.Id,
				new List<string> { LaunchNotifyEmail },
				userEmail,
				ct);
		}

		private async Task AddNotificationsAsync(
			string title,
			string body,
			long? entityId,
			List<string> toEmails,
			string? ccEmail,
			CancellationToken ct)
		{
			var unique = toEmails
				.Where(e => !string.IsNullOrWhiteSpace(e))
				.Select(e => e.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
			if (unique.Count == 0)
				return;

			var ownerId = sdk.CurrentUser?.Id ?? 1;
			var cc = string.IsNullOrWhiteSpace(ccEmail) ? null : new List<string> { ccEmail };
			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = title,
				Body = body,
				EntityId = entityId,
				OwnerId = ownerId,
				ViewPath = "/Panel/Sale/OrderDetailSerial/MonitoringEdit?id=" + entityId,
				IsRead = false,
				IsSend = false,
				ToEmails = unique,
				CcEmails = cc
			}, ct);
			await unitOfWork.SaveChangesAsync(ct);
		}

		private async Task<List<string>> ResolveRecipientsAsync(string roleName, int htsGroupId, CancellationToken ct)
		{
			var emails = new List<string>();
			var roleUsers = await userService.GetUsersByRoleName(roleName);
			if (roleUsers != null)
			{
				foreach (var user in roleUsers)
				{
					var mail = ToHavayarEmail(user.Email ?? user.Username);
					if (!string.IsNullOrWhiteSpace(mail))
						emails.Add(mail);
				}
			}

			if (emails.Count > 0)
				return emails;

			emails.AddRange(await QueryHtsGroupEmailsAsync(htsGroupId, ct));
			return emails;
		}

		private async Task<List<string>> QueryHtsGroupEmailsAsync(int groupId, CancellationToken ct)
		{
			var cs = configuration.GetConnectionString("Hts");
			if (string.IsNullOrWhiteSpace(cs))
				return [];

			const string sql = @"
SELECT DISTINCT NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N'')
FROM dbo.Gnr_UserGroupMember m
INNER JOIN dbo.Gnr_User u ON u.User_ID = m.User_FK
WHERE m.UserGroup_FK = @groupId
  AND NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N'') IS NOT NULL";

			try
			{
				await using var conn = new SqlConnection(cs);
				await conn.OpenAsync(ct);
				await using var cmd = new SqlCommand(sql, conn);
				cmd.Parameters.Add(new SqlParameter("@groupId", groupId));
				await using var reader = await cmd.ExecuteReaderAsync(ct);
				var list = new List<string>();
				while (await reader.ReadAsync(ct))
				{
					var mail = ToHavayarEmail(reader.GetString(0));
					if (!string.IsNullOrWhiteSpace(mail))
						list.Add(mail);
				}
				return list;
			}
			catch
			{
				return [];
			}
		}

		private static string BuildExitBody(
			OrderDetailSerial entity,
			string partCode,
			string partName,
			string customerTitle,
			string customerCode,
			string userName)
		{
			return "<div style='direction:rtl;text-align:right'>"
				+ $"به استحضار می رساند، تاریخ خروج نهایی کالا مربوط به مشتری {customerTitle} "
				+ $"با کد مشتری {customerCode} "
				+ "با مشخصات زیر "
				+ $"بواسطه {userName} در سیستم ثبت گردید"
				+ "<br/><br/><ul>"
				+ $"<li>عنوان کالا : {partName}</li>"
				+ $"<li>کد کالا : {partCode}</li>"
				+ $"<li style='color:red'>سریال : {entity.Serial}</li>"
				+ $"<li>تاریخ خروج نهایی : {entity.FinalExitShamsiDate}</li>"
				+ $"<li>ساعت خروج نهایی : {entity.ExitTimeFinal}</li>"
				+ "</ul></div>";
		}

		private static string BuildLaunchBody(
			OrderDetailSerial entity,
			string partCode,
			string partName,
			string customerTitle,
			string customerCode,
			string userName)
		{
			return "<div style='direction:rtl;text-align:right'>"
				+ $"به استحضار می رساند، تاریخ راه اندازی کالا مربوط به مشتری {customerTitle} "
				+ $"با کد مشتری {customerCode} "
				+ "با مشخصات زیر "
				+ $"بواسطه {userName} در سیستم ثبت گردید"
				+ "<br/><br/><ul>"
				+ $"<li>عنوان کالا : {partName}</li>"
				+ $"<li>کد کالا : {partCode}</li>"
				+ $"<li>سریال : {entity.Serial}</li>"
				+ $"<li>تاریخ خروج نهایی : {entity.FinalExitShamsiDate}</li>"
				+ $"<li>ساعت خروج نهایی : {entity.ExitTimeFinal}</li>"
				+ $"<li>تاریخ راه اندازی : {entity.LaunchShamsiDate}</li>"
				+ "</ul></div>";
		}

		private static string? ToHavayarEmail(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return null;
			var trimmed = value.Trim();
			return trimmed.Contains('@') ? trimmed : trimmed + "@havayar.com";
		}

		private static bool IsScrewCompressor(string partCode)
		{
			if (string.IsNullOrWhiteSpace(partCode) || !partCode.StartsWith("1801", StringComparison.Ordinal))
				return false;
			string[] excluded =
			[
				"180110", "180114", "180116", "180117", "180118", "180120", "180121",
				"180125", "180127", "1801281", "180135", "180140", "180141", "180142",
				"180143", "180144", "180145"
			];
			return excluded.All(prefix => !partCode.StartsWith(prefix, StringComparison.Ordinal));
		}

		private static bool IsPackageWithCompressor(string partName)
		{
			if (string.IsNullOrWhiteSpace(partName))
				return false;
			var hasPackage = partName.Contains("پکیج", StringComparison.Ordinal)
				|| partName.Contains("پكيج", StringComparison.Ordinal);
			return hasPackage && partName.Contains("hy", StringComparison.OrdinalIgnoreCase);
		}
	}
}
