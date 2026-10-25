using App.BackgroundJob.Jobs;
using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Sale;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using Services.Job;
using System.Net;

namespace App.BackgroundJob.Jobs.Sls
{
	/// <summary>
	/// جایگزین کار SQL Agent «HTS_SaleRequirmentAdvertise_NewComment» و تریگر SYS3.SaleRequirmentAdvertiseComments در راهکاران
	/// (که ردیف نوع 2707 را در Gnr_EmailBucketList سیستم HTS می‌ساخت).
	///
	/// هر چند دقیقه یادداشت‌های جدید «اعلام نیازمندی» (SYS3.Note) که کاربر <see cref="CommentUserName"/> ثبت کرده را از راهکاران می‌خواند
	/// و برای سازندهٔ درخواست یک system.Notification از نوع ایمیل می‌سازد؛ ارسال با EmailJob انجام می‌شود.
	///
	/// جلوگیری از ارسال تکراری (ایمیل‌های قبلی دوباره نمی‌روند):
	///  1) فقط یادداشت‌های با NoteID بزرگ‌تر از <see cref="LegacyLastNoteId"/> (آخرین یادداشتی که کار قدیمی ایمیل کرده) پردازش می‌شوند؛
	///  2) هر NoteID که در Gnr_EmailBucketList قدیمی (HTS) هست رد می‌شود (برای فاصلهٔ بین خاموش‌کردن کار قدیمی و استقرار این کار)؛
	///  3) هر NoteID که قبلاً Notification گرفته (نشانگر rahkaranNoteId در ViewPath) رد می‌شود.
	/// </summary>
	public class RequirmentAdvertiseCommentEmailJob(
		RahkaranDbContext rahkaranDb,
		HtsDbContext htsDb,
		IUnitOfWork unitOfWork,
		IUserService userService)
	{
		/// <summary>فقط کامنت‌های این کاربر راهکاران ایمیل می‌شود (مثل کار قدیمی).</summary>
		private const string CommentUserName = "lalouni.m";

		private const string CcEmail = "lalouni.m@havayar.com";

		/// <summary>آخرین SYS3.Note که کار قدیمی برایش ایمیل ساخته (max EntityIdRef سیستم Sale_RequirmentAdvertise در HTS).</summary>
		private const long LegacyLastNoteId = 10069;

		private const string NoteMarker = "rahkaranNoteId=";

		private const string RahkaranSystemName = "Sale_RequirmentAdvertise";

		[JobHandler(
			"ایمیل کامنت اعلام نیازمندی",
			"یادداشت‌های جدید اعلام نیازمندی (راهکاران) را می‌خواند و برای سازنده درخواست ایمیل در صف system.Notification می‌سازد (جایگزین کار SQL راهکاران HTS_SaleRequirmentAdvertise_NewComment).")]
		public async Task QueueCommentEmailsAsync(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				var notes = await rahkaranDb.Database.SqlQueryRaw<CommentRow>(
					@"SELECT n.NoteID AS NoteId,
					         s.Sale_RequirmentAdvertiseID AS RequestId,
					         CAST(s.RequestNumber AS BIGINT) AS RequestNumber,
					         p.FullName AS PartyName,
					         CAST(n.[Description] AS NVARCHAR(MAX)) AS CommentText,
					         u2.DomainUserName AS CreatorUserName
					  FROM SYS3.Note n
					  INNER JOIN USR3.Sale_RequirmentAdvertise s ON n.EntityRef = s.Sale_RequirmentAdvertiseID
					  INNER JOIN SYS3.[User] u  ON u.UserID  = n.Creator
					  INNER JOIN SYS3.[User] u2 ON u2.UserID = s.Creator
					  LEFT  JOIN GNR3.Party p   ON p.PartyID = s.PartyIdRef
					  WHERE n.EntityName = 'Sale_RequirmentAdvertise'
					    AND u.DomainUserName = @commentUser
					    AND u2.DomainUserName IS NOT NULL
					    AND n.NoteID > @afterNoteId
					  ORDER BY n.NoteID",
					new SqlParameter("@commentUser", CommentUserName),
					new SqlParameter("@afterNoteId", LegacyLastNoteId))
					.ToListAsync(cn);

				if (notes.Count == 0)
					return;

				// ایمیل‌هایی که کار قدیمی (HTS) ساخته؛ اگر HTS در دسترس نباشد اجرا خطا می‌دهد و ۵ دقیقه بعد دوباره تلاش می‌شود
				// (ترجیح با خطا است تا ایمیل قدیمی دوباره ارسال نشود). بعد از جمع‌شدن HTS این بخش قابل حذف است.
				var legacyNoteIds = (await htsDb.Database.SqlQueryRaw<long>(
					@"SELECT EntityIdRef AS Value
					  FROM dbo.Gnr_EmailBucketList
					  WHERE [System] = @system AND EntityIdRef IS NOT NULL AND EntityIdRef > @afterNoteId",
					new SqlParameter("@system", RahkaranSystemName),
					new SqlParameter("@afterNoteId", LegacyLastNoteId))
					.ToListAsync(cn)).ToHashSet();

				var pending = notes.Where(n => !legacyNoteIds.Contains(n.NoteId)).ToList();
				if (pending.Count == 0)
					return;

				var rahkaranRequestIds = pending.Select(n => n.RequestId).Distinct().ToList();
				var requestMap = (await unitOfWork.Repository<RequirmentAdvertise>().TableNoTracking
					.Where(x => x.HamkaranId != null && rahkaranRequestIds.Contains(x.HamkaranId.Value))
					.Select(x => new { x.Id, x.HamkaranId })
					.ToListAsync(cn))
					.Where(x => x.Id is > 0)
					.GroupBy(x => x.HamkaranId!.Value)
					.ToDictionary(g => g.Key, g => g.First().Id!.Value);

				var appRequestIds = requestMap.Values.Distinct().ToList();
				var alreadyQueuedNoteIds = new HashSet<long>();
				if (appRequestIds.Count > 0)
				{
					var queuedPaths = await unitOfWork.Repository<Notification>().TableNoTracking
						.Where(x => x.EntityId != null && appRequestIds.Contains(x.EntityId.Value)
							&& x.ViewPath != null && x.ViewPath.Contains(NoteMarker))
						.Select(x => x.ViewPath!)
						.ToListAsync(cn);
					foreach (var path in queuedPaths)
					{
						var noteId = ExtractNoteId(path);
						if (noteId != null)
							alreadyQueuedNoteIds.Add(noteId.Value);
					}
				}

				var userNames = pending.Select(n => n.CreatorUserName!).Distinct().ToList();
				var users = (await userService.TableNoTracking
					.Where(u => u.Id != null && userNames.Contains(u.Username))
					.Select(u => new { Id = u.Id!.Value, u.Username, u.Email })
					.ToListAsync(cn))
					.GroupBy(u => u.Username, StringComparer.OrdinalIgnoreCase)
					.ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

				var queued = 0;
				var missingRequest = new HashSet<long>();
				foreach (var note in pending)
				{
					cn.ThrowIfCancellationRequested();

					if (alreadyQueuedNoteIds.Contains(note.NoteId))
						continue;

					// تا همگام‌سازی RequirmentAdvertiseJob درخواست را نیاورد، ایمیل ساخته نمی‌شود و اجرای بعدی دوباره تلاش می‌کند
					if (!requestMap.TryGetValue(note.RequestId, out var appRequestId))
					{
						missingRequest.Add(note.RequestId);
						continue;
					}

					users.TryGetValue(note.CreatorUserName!, out var owner);
					var toEmail = owner != null && !string.IsNullOrWhiteSpace(owner.Email)
						? owner.Email!.Trim()
						: $"{note.CreatorUserName!.Trim()}@havayar.com";

					await unitOfWork.Repository<Notification>().AddAsync(new Notification
					{
						Type = NotificationType.Email,
						Title = $"کامنت اعلام نیازمندی به شماره درخواست {note.RequestNumber} - {note.PartyName}",
						Body = BuildBody(note.CommentText),
						EntityId = appRequestId,
						OwnerId = owner?.Id ?? 1,
						ViewPath = $"/Panel/RequirmentAdvertise/Edit?id={appRequestId}&{NoteMarker}{note.NoteId}",
						IsRead = false,
						IsSend = false,
						ToEmails = [toEmail],
						CcEmails = [CcEmail]
					}, cn);
					queued++;
				}

				if (queued > 0)
					await (jobLogger?.LogInfoAsync($"ایمیل کامنت اعلام نیازمندی در صف قرار گرفت: {queued}", cn) ?? Task.CompletedTask);

				if (missingRequest.Count > 0)
					await (jobLogger?.LogWarningAsync(
						$"برای {missingRequest.Count} درخواست راهکاران هنوز اعلام نیازمندی در برنامه نیست؛ اجرای بعدی دوباره بررسی می‌شود (شناسه‌های راهکاران: {JobLookup.SummarizeIds(missingRequest)})",
						0, cn) ?? Task.CompletedTask);
			}
			catch (Exception ex)
			{
				await (jobLogger?.LogExceptionAsync(ex, cn) ?? Task.CompletedTask);
				throw;
			}
		}

		private static string BuildBody(string? commentText)
		{
			var text = WebUtility.HtmlEncode(commentText ?? string.Empty)
				.Replace("\r", string.Empty)
				.Replace("\n", "<br/>");
			return $"<div style='direction:rtl;text-align:right'>{text}</div>";
		}

		private static long? ExtractNoteId(string viewPath)
		{
			var index = viewPath.IndexOf(NoteMarker, StringComparison.Ordinal);
			if (index < 0)
				return null;

			var digits = new string(viewPath[(index + NoteMarker.Length)..].TakeWhile(char.IsDigit).ToArray());
			return long.TryParse(digits, out var id) ? id : null;
		}

		public class CommentRow
		{
			public long NoteId { get; set; }
			public long RequestId { get; set; }
			public long? RequestNumber { get; set; }
			public string? PartyName { get; set; }
			public string? CommentText { get; set; }
			public string? CreatorUserName { get; set; }
		}
	}
}
