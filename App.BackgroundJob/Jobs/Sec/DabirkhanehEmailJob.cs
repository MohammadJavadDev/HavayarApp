using Common.Attributes;
using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.App.Hcm;
using Entities.App.Sec;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace App.BackgroundJob.Jobs.Sec
{
	/// <summary>
	/// معادل HTS <c>DabirkhanehTask</c> / <c>DabirkhanehEmails.SendBirthEmailNew</c> و <c>SendEmploymentEmailNew</c>.
	/// زمان‌بندی: هر ۳۰ دقیقه، ساعت ۲ تا ۱۷.
	/// </summary>
	public class DabirkhanehEmailJob(
		IUnitOfWork unitOfWork,
		RahkaranDbContext rahkaranDb,
		IConfiguration configuration)
	{
		private const long OfficeReciversGroupId = 1;
		private const long BirthdayEmailTypeId = 1;
		private const long AnniversaryEmailTypeId = 2;

		[JobHandler("ارسال ایمیل تولد و سالروز حضور دبیرخانه")]
		public async Task SendBirthAndEmploymentEmails(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			var now = DateTime.Now;
			if (now.Hour < 2 || now.Hour > 17)
			{
				await jobLogger?.LogInfoAsync($"خارج از بازه ساعتی ۲–۱۷ (ساعت فعلی: {now.Hour}) — اجرا نشد.", cn);
				return;
			}

			try
			{
				await CleanupOldLogsAsync(cn);
				await SendBirthEmailNewAsync(jobLogger, cn);
				await SendEmploymentEmailNewAsync(jobLogger, cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		/// <summary>معادل DELETE در GetBirthDataTable / GetEmploymentDataTable — لاگ‌هایی که ماه-روزشان قبل از امروز است.</summary>
		private async Task CleanupOldLogsAsync(CancellationToken cn)
		{
			var today = DateTime.Today;
			var logs = await unitOfWork.Repository<EmailsLog>().Table.ToListAsync(cn);
			var idsToDelete = logs
				.Where(l =>
				{
					var sendMd = l.SendDate.Month * 100 + l.SendDate.Day;
					var todayMd = today.Month * 100 + today.Day;
					return todayMd > sendMd;
				})
				.Select(l => l.Id!.Value)
				.ToList();

			if (idsToDelete.Count == 0)
				return;

			await unitOfWork.Repository<EmailsLog>().DeleteWhereAsync(l => idsToDelete.Contains(l.Id!.Value), cn);
		}

		private async Task SendBirthEmailNewAsync(IJobLogger? jobLogger, CancellationToken cn)
		{
			var currentShamsi = DateTime.Now.ToShamsiDate();
			var currentDay = int.Parse(currentShamsi.Substring(8, 2));
			var currentMonth = int.Parse(currentShamsi.Substring(5, 2));
			var currentYear = DateTime.Now.Year;

			var candidates = await LoadBirthdayCandidatesAsync(currentMonth, currentDay, currentYear, cn);
			await jobLogger?.LogInfoAsync($"نامزدهای ایمیل تولد: {candidates.Count}", cn);

			var postTitles = await LoadPostTitlesAsync(candidates.Select(c => c.HamkaranId).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList(), cn);
			var groups = await unitOfWork.Repository<ReciversGroup>().TableNoTracking.ToListAsync(cn);
			var settings = ReadEmailSettings(configuration);

			foreach (var item in candidates)
			{
				try
				{
					if (item.Picture == null || item.Picture.Length == 0)
						continue;

					var birthDate = item.BirthDate ?? "";
					var personelBirthDay = birthDate.Length >= 10 ? int.Parse(birthDate.Substring(8, 2)) : 0;
					var personelBirthMonth = birthDate.Length >= 7 ? int.Parse(birthDate.Substring(5, 2)) : 0;
					if (currentDay != personelBirthDay || currentMonth != personelBirthMonth)
						continue;

					var gender = item.GenderCode == 1 ? "جناب آقای " : "سرکار خانم ";
					var orgPost = postTitles.GetValueOrDefault(item.HamkaranId ?? 0) ?? "";

					var contentId = Guid.NewGuid().ToString();
					var content = new StringBuilder();
					content.Append("<html><body><div>");
					content.Append("<table style=\"width: 100%;height: 100%;text-align: center;font-family:'B Zar'\">");
					content.Append("<tr><td>");
					content.Append($@"<img src=""cid:{contentId}"" style='max-width: 700px; max-height: 600px; min-width: 400px; min-height: 300px;'/>");
					content.Append("</td></tr></table></div></body></html>");

					var subject = "سالروز تولد " + gender + item.NameDisplay + " " + item.FamilyDisplay;
					var receivers = ResolveBirthdayReceivers(item, orgPost, groups);
					var bccReceiver = item.BranchCode == 2 ? null : "mirlohi.m@havayar.com";

					var logged = await TryLogEmailAsync(item.SecPersonnelId, BirthdayEmailTypeId, OfficeReciversGroupId, cn);
					if (logged)
						await SendHtmlWithInlineImageAsync(settings, receivers, subject, content.ToString(), item.Picture, contentId, bccReceiver, cn);
				}
				catch (Exception ex)
				{
					await jobLogger?.LogWarningAsync("Logger For SendBirthDateEmail Method -> " + ex.Message, 0, cn);
				}
			}
		}

		private async Task SendEmploymentEmailNewAsync(IJobLogger? jobLogger, CancellationToken cn)
		{
			var currentShamsi = DateTime.Now.ToShamsiDate();
			var currentDay = int.Parse(currentShamsi.Substring(8, 2));
			var currentMonth = int.Parse(currentShamsi.Substring(5, 2));
			var currentYear = DateTime.Now.Year;

			var candidates = await LoadAnniversaryCandidatesAsync(currentMonth, currentDay, currentYear, cn);
			await jobLogger?.LogInfoAsync($"نامزدهای ایمیل سالروز حضور: {candidates.Count}", cn);

			var postTitles = await LoadPostTitlesAsync(candidates.Select(c => c.HamkaranId).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList(), cn);
			var groups = await unitOfWork.Repository<ReciversGroup>().TableNoTracking.ToListAsync(cn);
			var settings = ReadEmailSettings(configuration);

			foreach (var item in candidates)
			{
				try
				{
					if (item.Picture == null || item.Picture.Length == 0)
						continue;

					LinkedResource? linkedResource = null;
					var userImageByte = item.Picture;
					var streamBitmap = new MemoryStream(userImageByte);
					var contentId = Guid.NewGuid().ToString();
					linkedResource = new LinkedResource(streamBitmap)
					{
						ContentId = contentId,
						ContentType =
						{
							Name = contentId,
							MediaType = MediaTypeNames.Image.Jpeg
						},
						TransferEncoding = TransferEncoding.Base64,
						ContentLink = new Uri("cid:" + contentId)
					};

					var presenceYear = PresenceYearFromEmploymentDate(item.EmploymentDate, currentShamsi);
					// باگ عمدی سیستم قدیم: return کل متد، نه continue
					if (presenceYear == 1)
						return;

					if (linkedResource != null)
					{
						var gender = item.GenderCode == 1 ? "جناب آقای " : "سرکار خانم ";
						var orgPost = postTitles.GetValueOrDefault(item.HamkaranId ?? 0) ?? "";

						var content = new StringBuilder();
						content.Append("<html><body><div>");
						content.Append("<table style=\"width: 100%;height: 100%;text-align: center;font-family:'B Zar'\">");
						content.Append("<tr><td>");
						content.Append($@"<img src=""cid:{linkedResource.ContentId}"" style='max-width: 700px; max-height: 600px min-width: 400px; min-height: 300px;'/>");
						content.Append("</td></tr></table></div></body></html>");

						var alternateView = AlternateView.CreateAlternateViewFromString(content.ToString(), Encoding.UTF8, MediaTypeNames.Text.Html);
						alternateView.LinkedResources.Add(linkedResource);

						var subject = "سالروز حضور در گروه صنعتی هوایار-" + gender + item.NameDisplay + " " + item.FamilyDisplay;
						var receivers = ResolveAnniversaryReceivers(item, orgPost, presenceYear, groups);
						var bccReceiver = item.BranchCode == 2 ? null : "mirlohi.m@havayar.com";

						var logged = await TryLogEmailAsync(item.SecPersonnelId, AnniversaryEmailTypeId, OfficeReciversGroupId, cn);
						if (logged)
							await SendWithAlternateViewAsync(settings, receivers, subject, alternateView, bccReceiver, cn);
					}
					else
					{
						// باگ عمدی سیستم قدیم: return کل متد وقتی تصویر سالروز نباشد
						return;
					}
				}
				catch (Exception ex)
				{
					await jobLogger?.LogWarningAsync("Logger For SendAnniversaryEmail Method -> " + ex.Message, 0, cn);
				}
			}
		}

		private static string? ResolveBirthdayReceivers(SecEmailCandidate item, string orgPost, List<ReciversGroup> groups)
		{
			if (item.ReciversGroupId.HasValue)
			{
				var g = groups.FirstOrDefault(x => x.Id == item.ReciversGroupId);
				if (g?.ReciversEmails.HasValue() == true)
					return g.ReciversEmails!.Trim();
			}

			if (IsManager(orgPost))
				return "everyone@havayar.com";
			if (item.BranchCode == 1 || item.BranchCode == 18)
				return "co@havayar.com";
			if (item.BranchCode == 2)
				return "factory@havayar.com";
			return null;
		}

		private static string? ResolveAnniversaryReceivers(SecEmailCandidate item, string orgPost, int presenceYear, List<ReciversGroup> groups)
		{
			if (item.ReciversGroupId.HasValue)
			{
				var g = groups.FirstOrDefault(x => x.Id == item.ReciversGroupId);
				if (g?.ReciversEmails.HasValue() == true)
					return g.ReciversEmails!.Trim();
			}

			if (IsManager(orgPost) || presenceYear >= 13)
				return "everyone@havayar.com";
			if (item.BranchCode == 1)
				return "co@havayar.com";
			if (item.BranchCode == 2)
				return "factory@havayar.com";
			return null;
		}

		private static bool IsManager(string orgPost)
		{
			return !string.IsNullOrEmpty(orgPost)
				&& ((
				orgPost.StartsWith("مدیر")
				|| orgPost.StartsWith("معاون")
				|| orgPost.StartsWith("رییس")
				|| orgPost.StartsWith("ریاست")
				|| orgPost == "قائم مقام مدیر عامل"
				|| orgPost.StartsWith("جانشین مدیر")
				|| orgPost.StartsWith("ریاست هئیت مدیره"))
				&& !(orgPost.Contains("پروژه")));
		}

		private static int PresenceYearFromEmploymentDate(string? employmentDate, string currentShamsi)
		{
			if (string.IsNullOrWhiteSpace(employmentDate) || employmentDate.Length < 4)
				return 0;
			if (!int.TryParse(employmentDate.AsSpan(0, 4), out var empYear))
				return 0;
			if (!int.TryParse(currentShamsi.AsSpan(0, 4), out var curYear))
				return 0;
			return curYear - empYear;
		}

		private async Task<List<SecEmailCandidate>> LoadBirthdayCandidatesAsync(int month, int day, int gregorianYear, CancellationToken cn)
		{
			var monthDay = $"{month:D2}/{day:D2}";
			var exceptionHamkaranIds = new long[] { 534, 15024, 15299 };

			var query =
				from sp in unitOfWork.Repository<Personnel>().TableNoTracking
				join p in unitOfWork.Repository<Personel>().TableNoTracking on sp.PersonelId equals p.Id
				where sp.BirthDate != null
					&& sp.BirthDate.Length >= 10
					&& sp.BirthDate.Substring(5, 5) == monthDay
					&& (sp.IsActive == IsActiveEnum.Active
						|| (p.HamkaranId.HasValue && exceptionHamkaranIds.Contains(p.HamkaranId.Value)))
					&& sp.Picture != null
				select new { sp, p };

			var rows = await query.ToListAsync(cn);
			var alreadySentIds = await unitOfWork.Repository<EmailsLog>().TableNoTracking
				.Where(l => l.EmailTypeId == BirthdayEmailTypeId && l.SendDate.Year == gregorianYear)
				.Select(l => l.PersonnelId)
				.ToListAsync(cn);

			return rows
				.Where(r => !alreadySentIds.Contains(r.sp.Id!.Value))
				.Select(r => new SecEmailCandidate
				{
					SecPersonnelId = r.sp.Id!.Value,
					NameDisplay = r.sp.NameDisplay,
					FamilyDisplay = r.sp.FamilyDisplay,
					BirthDate = r.sp.BirthDate,
					EmploymentDate = r.sp.EmploymentDate,
					Picture = r.sp.Picture,
					GenderCode = r.p.GenderCode,
					BranchCode = r.p.BranchCode,
					HamkaranId = r.p.HamkaranId,
					ReciversGroupId = r.sp.ReciversGroupId,
					PrsCode = r.p.Code
				})
				.ToList();
		}

		private async Task<List<SecEmailCandidate>> LoadAnniversaryCandidatesAsync(int month, int day, int gregorianYear, CancellationToken cn)
		{
			var monthDay = $"{month:D2}/{day:D2}";
			var exceptionHamkaranIds = new long[] { 534, 15024, 15299 };

			var query =
				from sp in unitOfWork.Repository<Personnel>().TableNoTracking
				join p in unitOfWork.Repository<Personel>().TableNoTracking on sp.PersonelId equals p.Id
				where sp.EmploymentDate != null
					&& sp.EmploymentDate.Length >= 10
					&& sp.EmploymentDate.Substring(5, 5) == monthDay
					&& (sp.IsActive == IsActiveEnum.Active
						|| (p.HamkaranId.HasValue && exceptionHamkaranIds.Contains(p.HamkaranId.Value)))
				select new { sp, p };

			var rows = await query.ToListAsync(cn);
			var alreadySentIds = await unitOfWork.Repository<EmailsLog>().TableNoTracking
				.Where(l => l.EmailTypeId == AnniversaryEmailTypeId && l.SendDate.Year == gregorianYear)
				.Select(l => l.PersonnelId)
				.ToListAsync(cn);

			return rows
				.Where(r => !alreadySentIds.Contains(r.sp.Id!.Value))
				.Select(r => new SecEmailCandidate
				{
					SecPersonnelId = r.sp.Id!.Value,
					NameDisplay = r.sp.NameDisplay,
					FamilyDisplay = r.sp.FamilyDisplay,
					BirthDate = r.sp.BirthDate,
					EmploymentDate = r.sp.EmploymentDate,
					Picture = r.sp.SecondPicture,
					GenderCode = r.p.GenderCode,
					BranchCode = r.p.BranchCode,
					HamkaranId = r.p.HamkaranId,
					ReciversGroupId = r.sp.ReciversGroupId,
					PrsCode = r.p.Code
				})
				.ToList();
		}

		private async Task<Dictionary<long, string>> LoadPostTitlesAsync(List<long> employeeIds, CancellationToken cn)
		{
			if (employeeIds.Count == 0)
				return new Dictionary<long, string>();

			var ids = string.Join(",", employeeIds);
			var sql = $@"
SELECT e.EmployeeID, ps.Title AS PostTitle
FROM HCM3.Employee e
INNER JOIN HCM3.EmployeeStatute es ON es.EmployeeRef = e.EmployeeID
INNER JOIN HCM3.Post ps ON ps.PostID = es.PostRef
WHERE e.EmployeeID IN ({ids})
AND es.EmployeeStatuteID = (
	SELECT TOP 1 es2.EmployeeStatuteID FROM HCM3.EmployeeStatute es2
	WHERE es2.EmployeeRef = e.EmployeeID ORDER BY es2.ApplyDate DESC)";

			try
			{
				var rows = await rahkaranDb.Database.SqlQueryRaw<PostTitleRow>(sql).ToListAsync(cn);
				return rows
					.GroupBy(r => r.EmployeeID)
					.ToDictionary(g => g.Key, g => g.First().PostTitle ?? "");
			}
			catch
			{
				return new Dictionary<long, string>();
			}
		}

		private async Task<bool> TryLogEmailAsync(long personnelId, long emailTypeId, long reciversGroupId, CancellationToken cn)
		{
			try
			{
				var now = DateTime.Now;
				await unitOfWork.Repository<EmailsLog>().AddAsync(new EmailsLog
				{
					PersonnelId = personnelId,
					EmailTypeId = emailTypeId,
					ReciversGroupId = reciversGroupId,
					SendDate = now.Date,
					SendTime = now.TimeOfDay,
					IsActive = IsActiveEnum.Active
				}, cn, false);
				await unitOfWork.SaveChangesAsync(cn);
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static async Task SendHtmlWithInlineImageAsync(
			EmailSettings settings,
			string? to,
			string subject,
			string html,
			byte[] imageBytes,
			string contentId,
			string? bcc,
			CancellationToken cn)
		{
			if (string.IsNullOrWhiteSpace(to) || !settings.SmtpHost.HasValue())
				return;

			using var stream = new MemoryStream(imageBytes);
			var linked = new LinkedResource(stream)
			{
				ContentId = contentId,
				ContentType = { Name = contentId, MediaType = MediaTypeNames.Image.Jpeg },
				TransferEncoding = TransferEncoding.Base64,
				ContentLink = new Uri("cid:" + contentId)
			};
			var view = AlternateView.CreateAlternateViewFromString(html, Encoding.UTF8, MediaTypeNames.Text.Html);
			view.LinkedResources.Add(linked);
			await SendWithAlternateViewAsync(settings, to, subject, view, bcc, cn);
		}

		private static async Task SendWithAlternateViewAsync(
			EmailSettings settings,
			string? to,
			string subject,
			AlternateView alternateView,
			string? bcc,
			CancellationToken cn)
		{
			if (string.IsNullOrWhiteSpace(to) || !settings.SmtpHost.HasValue())
				return;

			using var message = new MailMessage
			{
				From = new MailAddress(settings.FromAddress, settings.FromDisplayName),
				Subject = subject,
				IsBodyHtml = true
			};
			message.To.Add(to);
			if (!string.IsNullOrWhiteSpace(bcc))
				message.Bcc.Add(bcc);
			message.AlternateViews.Add(alternateView);

			using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
			{
				EnableSsl = settings.EnableSsl,
				DeliveryMethod = SmtpDeliveryMethod.Network
			};
			if (settings.Username.HasValue())
				client.Credentials = new NetworkCredential(settings.Username, settings.Password ?? string.Empty);

			await client.SendMailAsync(message, cn);
		}

		private static EmailSettings ReadEmailSettings(IConfiguration configuration) => new()
		{
			SmtpHost = configuration["Email:SmtpHost"] ?? "mail2.havayar.com",
			SmtpPort = int.TryParse(configuration["Email:SmtpPort"], out var port) ? port : 25,
			EnableSsl = bool.TryParse(configuration["Email:EnableSsl"], out var ssl) && ssl,
			Username = configuration["Email:Username"],
			Password = configuration["Email:Password"],
			FromAddress = configuration["Email:FromAddress"] ?? "noreply@havayar.com",
			FromDisplayName = configuration["Email:FromDisplayName"] ?? "سامانه هوایار"
		};

		private sealed class SecEmailCandidate
		{
			public long SecPersonnelId { get; set; }
			public string? NameDisplay { get; set; }
			public string? FamilyDisplay { get; set; }
			public string? BirthDate { get; set; }
			public string? EmploymentDate { get; set; }
			public byte[]? Picture { get; set; }
			public int? GenderCode { get; set; }
			public int? BranchCode { get; set; }
			public long? HamkaranId { get; set; }
			public long? ReciversGroupId { get; set; }
			public string? PrsCode { get; set; }
		}

		private sealed class PostTitleRow
		{
			public long EmployeeID { get; set; }
			public string? PostTitle { get; set; }
		}

		private sealed class EmailSettings
		{
			public string SmtpHost { get; set; } = "";
			public int SmtpPort { get; set; }
			public bool EnableSsl { get; set; }
			public string? Username { get; set; }
			public string? Password { get; set; }
			public string FromAddress { get; set; } = "";
			public string FromDisplayName { get; set; } = "";
		}
	}
}
