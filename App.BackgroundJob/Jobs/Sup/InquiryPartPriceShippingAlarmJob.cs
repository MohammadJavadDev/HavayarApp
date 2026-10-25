using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Sup;
using Entities.App.Sup.Enums;
using Entities.Base;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Globalization;
using System.Net;
using System.Text;

namespace App.BackgroundJob.Jobs.Sup
{
	/// <summary>
	/// جایگزین HTS <c>SentShippingActualPriceChangeAlarm</c> (ساعت ۱۱:۵۰ و ۱۵:۵۰).
	/// آخرین قیمت یورویی با وضعیت خرید را می‌بیند؛ اگر هزینه حمل بیش از ۱۵ درصد قیمت کالا باشد
	/// یک <c>system.Notification</c> ایمیل می‌سازد. ارسال با EmailJob است.
	///
	/// تکرار نمی‌فرستد:
	///  1) ردیف‌هایی که HTS با TypeId برابر 3002 قبلاً ایمیل کرده (کد کالا + قیمت + حمل + تاریخ)؛
	///  2) ردیف‌هایی که در همین سیستم Notification با نشانگر <see cref="AlarmMarker"/> دارند.
	/// اگر HTS در دسترس نباشد جاب خطا می‌دهد تا ایمیل قدیمی دوباره ساخته نشود.
	/// </summary>
	public class InquiryPartPriceShippingAlarmJob(ApplicationDbContext db, HtsDbContext htsDb, IUnitOfWork unitOfWork)
	{
		public const string ReceiverRoleName = "دریافت کنندگان ایمیل هشدار اختلاف قیمت خرید";
		public const string Subject = "هشدار قیمت حمل و نقل کالا نسبت به خرید";
		public const string AlarmMarker = "shippingPriceAlarm";

		/// <summary>معادل Inquiry_PartPrice_ID &gt; 377381 در HTS (همان ردیف در 2025-01-12 16:21 ساخته شده).</summary>
		private static readonly DateTime LegacyCutoff = new(2025, 1, 12, 16, 21, 0);

		private const long EuroPriceUnitId = 4;
		private const decimal ThresholdPercent = 15m;

		[JobHandler(
			"هشدار قیمت حمل نسبت به قیمت کالا — ۱۱:۵۰",
			"هر روز ساعت ۱۱:۵۰. آخرین قیمت یورویی خرید که هزینه حملش بیش از ۱۵ درصد قیمت کالاست.")]
		public Task SendMorningAlarm(IJobLogger? jobLogger = null, CancellationToken cn = default)
			=> SendAsync(jobLogger, cn);

		[JobHandler(
			"هشدار قیمت حمل نسبت به قیمت کالا — ۱۵:۵۰",
			"هر روز ساعت ۱۵:۵۰. آخرین قیمت یورویی خرید که هزینه حملش بیش از ۱۵ درصد قیمت کالاست.")]
		public Task SendAfternoonAlarm(IJobLogger? jobLogger = null, CancellationToken cn = default)
			=> SendAsync(jobLogger, cn);

		private async Task SendAsync(IJobLogger? jobLogger, CancellationToken cn)
		{
			try
			{
				var rows = await unitOfWork.Repository<InquiryPartPrice>().TableNoTracking
					.Where(x => x.Id != null
						&& x.PriceUnitId == EuroPriceUnitId
						&& x.PriceStatus == InquiryPartPricePriceStatusEnum.Purchase
						&& x.CurrencyActualPrice != null && x.CurrencyActualPrice != 0
						&& x.ShippingActualPrice != null && x.ShippingActualPrice != 0
						&& x.CreatedOnMiladiDateTime != null
						&& x.CreatedOnMiladiDateTime > LegacyCutoff)
					.Select(x => new PriceRow
					{
						Id = x.Id ?? 0,
						PartId = x.PartId,
						PartCode = x.Part.Code,
						PartName = x.Part.Name,
						CurrencyActualPrice = x.CurrencyActualPrice ?? 0,
						ShippingActualPrice = x.ShippingActualPrice ?? 0,
						CreatedOn = x.CreatedOnMiladiDateTime ?? LegacyCutoff,
						CreatedById = x.CreatedById
					})
					.ToListAsync(cn);

				var latest = rows
					.GroupBy(x => x.PartId)
					.Select(g => g
						.OrderByDescending(x => x.CreatedOn)
						.ThenByDescending(x => x.Id)
						.First())
					.Where(x => (x.ShippingActualPrice / x.CurrencyActualPrice) * 100m > ThresholdPercent)
					.ToList();

				await (jobLogger?.LogInfoAsync($"آخرین قیمت یورویی بالای آستانه حمل: {latest.Count}", cn) ?? Task.CompletedTask);
				if (latest.Count == 0)
					return;

				var legacyKeys = await LoadLegacySentKeysAsync(cn);

				var priceIds = latest.Select(x => x.Id).ToList();
				var alreadyQueued = (await unitOfWork.Repository<Notification>().TableNoTracking
					.Where(n => n.EntityId != null
						&& priceIds.Contains(n.EntityId.Value)
						&& n.ViewPath == AlarmMarker)
					.Select(n => n.EntityId!.Value)
					.ToListAsync(cn)).ToHashSet();

				var activeUsers = await db.Users.AsNoTracking()
					.Where(u => u.IsActive == IsActiveEnum.Active && u.Id != null)
					.Select(u => new UserRow
					{
						Id = u.Id!.Value,
						Username = u.Username,
						Email = u.Email,
						NameFa = u.NameFa,
						Name = u.Name,
						Roles = u.Roles
					})
					.ToListAsync(cn);

				var ccEmails = activeUsers
					.Where(u => u.Roles != null && u.Roles.Contains(ReceiverRoleName))
					.Select(u => ToEmail(u.Email, u.Username))
					.Where(x => x != null)
					.Cast<string>()
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToList();

				if (ccEmails.Count == 0)
				{
					await (jobLogger?.LogWarningAsync($"گیرنده‌ای در نقش «{ReceiverRoleName}» نیست", 0, cn) ?? Task.CompletedTask);
				}

				var usersById = activeUsers.ToDictionary(u => u.Id);
				var missingCreatorIds = latest
					.Where(x => x.CreatedById != null && !usersById.ContainsKey(x.CreatedById.Value))
					.Select(x => x.CreatedById!.Value)
					.Distinct()
					.ToList();
				if (missingCreatorIds.Count > 0)
				{
					var extras = await db.Users.AsNoTracking()
						.Where(u => u.Id != null && missingCreatorIds.Contains(u.Id.Value))
						.Select(u => new UserRow
						{
							Id = u.Id!.Value,
							Username = u.Username,
							Email = u.Email,
							NameFa = u.NameFa,
							Name = u.Name,
							Roles = u.Roles
						})
						.ToListAsync(cn);
					foreach (var extra in extras)
						usersById[extra.Id] = extra;
				}

				var queued = 0;
				var skippedLegacy = 0;
				var skippedQueued = 0;
				var skippedNoRecipient = 0;

				foreach (var price in latest)
				{
					cn.ThrowIfCancellationRequested();

					if (alreadyQueued.Contains(price.Id))
					{
						skippedQueued++;
						continue;
					}

					if (legacyKeys.Contains(SentKey.From(price)))
					{
						skippedLegacy++;
						continue;
					}

					usersById.TryGetValue(price.CreatedById ?? 0, out var creator);
					var to = ToEmail(creator?.Email, creator?.Username);
					if (to == null)
					{
						skippedNoRecipient++;
						continue;
					}

					var creatorName = FirstNonEmpty(creator?.NameFa, creator?.Name, creator?.Username) ?? "";
					await unitOfWork.Repository<Notification>().AddAsync(new Notification
					{
						Type = NotificationType.Email,
						Title = Subject,
						Body = BuildBody(price, creatorName),
						EntityId = price.Id,
						OwnerId = price.CreatedById ?? 1,
						ViewPath = AlarmMarker,
						IsRead = false,
						IsSend = false,
						ToEmails = [to],
						CcEmails = ccEmails.Count == 0 ? null : ccEmails.ToList()
					}, cn, saveNow: false, InvokeAction: false);
					queued++;
				}

				if (queued > 0)
					await unitOfWork.SaveChangesAsync(cn);

				await (jobLogger?.LogInfoAsync(
					$"صف ایمیل حمل: {queued}، رد به‌خاطر HTS: {skippedLegacy}، رد به‌خاطر صف قبلی: {skippedQueued}، بدون گیرنده: {skippedNoRecipient}",
					cn) ?? Task.CompletedTask);
			}
			catch (Exception ex)
			{
				await (jobLogger?.LogExceptionAsync(ex, cn) ?? Task.CompletedTask);
				throw;
			}
		}

		private async Task<HashSet<SentKey>> LoadLegacySentKeysAsync(CancellationToken cn)
		{
			var legacy = await htsDb.Database.SqlQueryRaw<LegacySentPrice>(
				@"SELECT LTRIM(RTRIM(p.Part_Code)) AS PartCode,
				         h.CurrencyActualPrice AS CurrencyActualPrice,
				         h.ShippingActualPrice AS ShippingActualPrice,
				         CAST(h.CreatedDateLatin AS datetime2) AS CreatedDate
				  FROM dbo.Gnr_EmailBucketList e
				  INNER JOIN dbo.Sup_Inquiry_PartPrice h ON h.Inquiry_PartPrice_ID = e.EntityIdRef
				  INNER JOIN dbo.Inv_Part p ON p.Part_ID = h.Part_FK
				  WHERE e.TypeId = 3002
				    AND e.EntityIdRef IS NOT NULL
				    AND h.CurrencyActualPrice IS NOT NULL
				    AND h.ShippingActualPrice IS NOT NULL
				    AND p.Part_Code IS NOT NULL")
				.ToListAsync(cn);

			return legacy
				.Select(x => new SentKey(x.PartCode.Trim(), x.CurrencyActualPrice, x.ShippingActualPrice, DateOnly.FromDateTime(x.CreatedDate)))
				.ToHashSet();
		}

		private static string BuildBody(PriceRow price, string creatorName)
		{
			var percent = ((price.ShippingActualPrice / price.CurrencyActualPrice) * 100m)
				.ToString("F1", CultureInfo.InvariantCulture);
			var productPrice = price.CurrencyActualPrice.ToString("0.00", CultureInfo.InvariantCulture);
			var shippingPrice = price.ShippingActualPrice.ToString("0.00", CultureInfo.InvariantCulture);

			var sb = new StringBuilder();
			sb.AppendLine("<div style='text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl' width='672'>");
			sb.AppendLine("<tr style='width: 7.0in; background: #000aa0'><td colspan='2'>");
			sb.AppendLine("<div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center;direction:rtl'>");
			sb.AppendLine("گروه صنعتی هوایار");
			sb.Append("<br/>").Append(WebUtility.HtmlEncode(Subject));
			sb.AppendLine("</div></td></tr><tr><td colspan='2'>");
			sb.AppendLine("<div style='font-size:14.0pt;font-family:Zar;color:#1F497D;text-align:right;direction:rtl'>");
			sb.AppendLine("با سلام و احترام <br />");
			sb.AppendLine("بدینوسیله اعلام میگردد کالا با مشخصات زیر<br />");
			sb.Append("محصول با کد : ").Append(WebUtility.HtmlEncode(price.PartCode ?? "")).Append("<br />");
			sb.Append("محصول با نام : ").Append(WebUtility.HtmlEncode(price.PartName ?? "")).Append("<br />");
			sb.Append(" با قیمت محصول : ").Append(productPrice).Append("<br />");
			sb.Append(" با قیمت حمل و نقل : ").Append(shippingPrice).Append("<br />");
			sb.Append(" با اختلاف درصدی : ").Append(percent).Append("<br />");
			sb.Append(" توسط : ").Append(WebUtility.HtmlEncode(creatorName)).Append("<br />");
			sb.AppendLine("حمل شده است<br />");
			sb.AppendLine("لذا خواهشمند است نسبت به بررسی و انجام اقدامات لازم مساعدت فرمایید");
			sb.AppendLine("</div></td></tr></table></div>");
			return sb.ToString();
		}

		private static string? ToEmail(string? email, string? username)
		{
			var value = FirstNonEmpty(email, username);
			if (value == null)
				return null;
			return value.Contains('@') ? value : value + "@havayar.com";
		}

		private static string? FirstNonEmpty(params string?[] values)
		{
			foreach (var value in values)
			{
				if (!string.IsNullOrWhiteSpace(value))
					return value.Trim();
			}
			return null;
		}

		private sealed class PriceRow
		{
			public long Id { get; set; }
			public long PartId { get; set; }
			public string? PartCode { get; set; }
			public string? PartName { get; set; }
			public decimal CurrencyActualPrice { get; set; }
			public decimal ShippingActualPrice { get; set; }
			public DateTime CreatedOn { get; set; }
			public long? CreatedById { get; set; }
		}

		private sealed class UserRow
		{
			public long Id { get; set; }
			public string? Username { get; set; }
			public string? Email { get; set; }
			public string? NameFa { get; set; }
			public string? Name { get; set; }
			public List<string> Roles { get; set; } = [];
		}

		public class LegacySentPrice
		{
			public string PartCode { get; set; } = "";
			public decimal CurrencyActualPrice { get; set; }
			public decimal ShippingActualPrice { get; set; }
			public DateTime CreatedDate { get; set; }
		}

		private readonly record struct SentKey(string PartCode, decimal Currency, decimal Shipping, DateOnly Day)
		{
			public static SentKey From(PriceRow price)
				=> new((price.PartCode ?? "").Trim(), price.CurrencyActualPrice, price.ShippingActualPrice, DateOnly.FromDateTime(price.CreatedOn));
		}
	}
}
