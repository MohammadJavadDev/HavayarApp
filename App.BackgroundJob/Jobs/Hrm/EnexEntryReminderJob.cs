using Common.Attributes;
using Data;
using Entities.App.Hrm;
using Entities.Base;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Net;
using System.Text;

namespace App.BackgroundJob.Jobs.Hrm
{
	/// <summary>
	/// یادآوری HTS ساعت 10:25 برای کالای خروج اضطراری که برنگشته است.
	/// گیرنده: نقش «دریافت کنندگان ایمیل خروج اضطراری بازگشت نشده» (گروه 541).
	/// </summary>
	public class EnexEntryReminderJob(ApplicationDbContext db)
	{
		public const string ReceiverRoleName = "دریافت کنندگان ایمیل خروج اضطراری بازگشت نشده";
		private const short SecurityApproveStatus = 775;

		[JobHandler(
			"یادآوری بازگشت کالای خروج اضطراری — موعد امروز",
			"هر روز ساعت 10:25. فرم‌های خروج برگشت‌پذیر با موعد امروز که هنوز برنگشته‌اند.")]
		public Task SendTodayReminders(IJobLogger? jobLogger = null, CancellationToken cn = default)
			=> SendAsync(jobLogger, DateTime.Today, "یاد آوری سر رسید تاریخ ورود کالای خارج شده بصورت اضطراری با شماره فرم : ",
				formNumber => $"احتراماَ کالاهای مندرج در درخواست خروج اضطراری شماره : {formNumber}با موعد بازگشت امروز تا این لحظه بازگشت داده نشده لطفا پیگیری های لازم را انجام دهید.",
				cn);

		[JobHandler(
			"هشدار عدم بازگشت کالای خروج اضطراری",
			"هر روز ساعت 10:25. فرم‌های خروج برگشت‌پذیر که موعدشان دو روز پیش بوده و هنوز برنگشته‌اند.")]
		public Task SendExpiredReminders(IJobLogger? jobLogger = null, CancellationToken cn = default)
			=> SendAsync(jobLogger, DateTime.Today.AddDays(-2), "هشدار عدم بازگشت کالای خارج شده بصورت اضطراری ",
				formNumber => $"احتراماَ کالاهای مندرج در درخواست خروج اضطراری شماره : {formNumber} تا این لحظه بازگشت داده نشده لطفا پیگیری های لازم را انجام دهید.",
				cn);

		private async Task SendAsync(IJobLogger? jobLogger, DateTime claimedDate, string subject, Func<int, string> bodyText, CancellationToken cn)
		{
			var users = await db.Users.AsNoTracking()
				.Where(u => u.IsActive == IsActiveEnum.Active)
				.Select(u => new { u.Id, u.Username, u.Roles })
				.ToListAsync(cn);
			var receivers = users
				.Where(u => u.Roles.Contains(ReceiverRoleName) && !string.IsNullOrWhiteSpace(u.Username))
				.Select(u => u.Username!)
				.Distinct()
				.ToList();
			if (receivers.Count == 0)
			{
				await jobLogger?.LogWarningAsync($"گیرنده‌ای در نقش «{ReceiverRoleName}» نیست", 0, cn);
				return;
			}

			var forms = await db.Set<EnexExitForm>().AsNoTracking()
				.Where(x => x.IsReturnable && !x.IsReturned
					&& x.LastStatusId == SecurityApproveStatus
					&& x.ClaimedReturnMiladiDate == claimedDate)
				.Select(x => new { x.Id, x.FormNumber, x.CreatedById })
				.ToListAsync(cn);

			await jobLogger?.LogInfoAsync($"فرم‌های موعد {claimedDate:yyyy-MM-dd}: {forms.Count}", cn);
			var byId = users.Where(u => u.Id != null).ToDictionary(u => u.Id!.Value, u => u.Username);

			foreach (var form in forms)
			{
				string? creatorName = null;
				if (form.CreatedById != null)
					byId.TryGetValue(form.CreatedById.Value, out creatorName);
				var cc = ToEmail(creatorName);
				await db.Set<Notification>().AddAsync(new Notification
				{
					Type = NotificationType.Email,
					Title = subject,
					Body = BuildBody(bodyText(form.FormNumber)),
					EntityId = form.Id,
					OwnerId = form.CreatedById ?? 1,
					IsRead = false,
					IsSend = false,
					ToEmails = receivers.Select(ToEmail).Where(x => x != null).Cast<string>().ToList(),
					CcEmails = cc == null ? null : [cc]
				}, cn);
			}

			await db.SaveChangesAsync(cn);
		}

		private static string? ToEmail(string? username)
		{
			if (string.IsNullOrWhiteSpace(username)) return null;
			var value = username.Trim();
			return value.Contains('@') ? value : value + "@havayar.com";
		}

		private static string BuildBody(string text)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<div style='text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl'  width='100%' >");
			sb.AppendLine("<tr style='width: 7.0in; background: #000aa0'><td colspan='2'>");
			sb.AppendLine("<div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center;direction:rtl'>گروه صنعتی هوایار</div>");
			sb.AppendLine("</td></tr><tr><td colspan='2'>");
			sb.AppendLine("<div style='font-size:14.0pt;font-family:Zar;color:#1F497D;text-align:right;direction:rtl'>");
			sb.Append(WebUtility.HtmlEncode(text));
			sb.Append("<br /></div></td></tr></table></div>");
			return sb.ToString();
		}

	}
}
