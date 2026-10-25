using Common.Utilities;
using Data;
using Data.Contracts;
using Services.Settings;
using Entities.Base;
using Entities.Base.Sms;
using Entities.Base.Sms.Enums;
using Microsoft.EntityFrameworkCore;

namespace Services.Sms
{
	public class SmsEnqueueRequest
	{
		/// <summary>یکی از کلیدهای <see cref="SmsEventKeys"/>.</summary>
		public string EventKey { get; set; } = string.Empty;

		/// <summary>یک یا چند شماره (جداکننده ; , ، یا خط جدید). برای هر شماره یک ردیف ساخته می‌شود.</summary>
		public string? Receivers { get; set; }

		public string Body { get; set; } = string.Empty;

		public string? RecipientName { get; set; }
		public string? SourceEntityType { get; set; }
		public long? SourceEntityId { get; set; }

		/// <summary>اگر مقدار داشته باشد، قبل از این زمان ارسال نمی‌شود (یادآوری‌های زمان‌بندی‌شده).</summary>
		public DateTime? ScheduledOn { get; set; }

		/// <summary>اگر null باشد از <see cref="SmsEventKeys"/> گرفته می‌شود.</summary>
		public SmsCategoryEnum? Category { get; set; }

		/// <summary>اگر null باشد از <see cref="SmsEventKeys"/> گرفته می‌شود.</summary>
		public bool? IsSensitive { get; set; }

		/// <summary>کاربر ایجادکننده (برای ارسال دستی)؛ در پیامک‌های خودکار خالی می‌ماند.</summary>
		public long? CreatedById { get; set; }
		public string? CreatedByName { get; set; }
	}

	/// <summary>
	/// تنها نقطه ثبت پیامک. پیامک مستقیم به پنل فرستاده نمی‌شود؛
	/// اینجا فقط در صف (<c>system.SmsMessage</c>) ثبت می‌شود و job پس‌زمینه آن را می‌فرستد.
	/// بعد از ذخیره موفق عملیات اصلی (نه قبل از آن) فراخوانی شود. خطای این سرویس نباید عملیات کاربر را خراب کند.
	/// </summary>
	public interface ISmsService
	{
		Task<IReadOnlyList<SmsMessage>> EnqueueAsync(SmsEnqueueRequest request, CancellationToken ct = default);

		Task<IReadOnlyList<SmsMessage>> EnqueueAsync(
			string eventKey,
			string? receivers,
			string body,
			string? sourceEntityType = null,
			long? sourceEntityId = null,
			DateTime? scheduledOn = null,
			CancellationToken ct = default);

		/// <summary>مثل EnqueueAsync ولی هرگز خطا نمی‌دهد (خطا نباید عملیات اصلی کاربر را خراب کند). فقط بعد از ذخیره موفق عملیات اصلی صدا زده شود.</summary>
		Task<bool> TryEnqueueAsync(string eventKey, string? receivers, string body, string? sourceEntityType = null, long? sourceEntityId = null, DateTime? scheduledOn = null, CancellationToken ct = default);

		/// <summary>آیا برای این رویداد و منبع قبلاً پیامکی (در هر وضعیتی جز لغو) ثبت شده؟ برای جلوگیری از ارسال تکراری.</summary>
		Task<bool> ExistsAsync(string eventKey, string sourceEntityType, long sourceEntityId, CancellationToken ct = default);

		/// <summary>لغو پیامک‌هایی که هنوز در صف‌اند (مثلاً حذف یادآوری زمان‌بندی‌شده). تعداد لغوشده را برمی‌گرداند.</summary>
		Task<int> CancelPendingAsync(string eventKey, string sourceEntityType, long sourceEntityId, CancellationToken ct = default);
	}

	public class SmsService(IUnitOfWork unitOfWork, IGeneralSettingService settings, ApplicationDbContext db) : ISmsService
	{
		public Task<IReadOnlyList<SmsMessage>> EnqueueAsync(
			string eventKey,
			string? receivers,
			string body,
			string? sourceEntityType = null,
			long? sourceEntityId = null,
			DateTime? scheduledOn = null,
			CancellationToken ct = default)
			=> EnqueueAsync(new SmsEnqueueRequest
			{
				EventKey = eventKey,
				Receivers = receivers,
				Body = body,
				SourceEntityType = sourceEntityType,
				SourceEntityId = sourceEntityId,
				ScheduledOn = scheduledOn
			}, ct);

		public async Task<IReadOnlyList<SmsMessage>> EnqueueAsync(SmsEnqueueRequest request, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(request.EventKey))
				throw new ArgumentException("EventKey پیامک مشخص نشده است.", nameof(request));

			var receivers = SmsHelpers.SplitReceivers(request.Receivers);
			if (receivers.Count == 0)
				return [];

			var body = (request.Body ?? string.Empty).Trim();
			var info = SmsEventKeys.Find(request.EventKey);
			var category = request.Category ?? info?.Category ?? SmsCategoryEnum.Internal;
			var isSensitive = request.IsSensitive ?? info?.IsSensitive ?? false;
			var now = DateTime.Now;
			var segments = SmsHelpers.CountSegments(body);

			// حالت آزمایشی: همه پیامک‌ها به یک شماره هدایت می‌شوند و شماره اصلی در «نام گیرنده» می‌ماند.
			var redirectRaw = await settings.GetAsync(GeneralSettingKeys.SmsTestRedirectMobile, ct);
			string? redirect = null;
			if (!string.IsNullOrWhiteSpace(redirectRaw))
			{
				var normalized = SmsHelpers.Normalize(redirectRaw, out var redirectValid);
				if (redirectValid)
					redirect = normalized;
			}

			var items = new List<SmsMessage>(receivers.Count);
			foreach (var (originalMobile, originalValid) in receivers)
			{
				var mobile = redirect ?? originalMobile;
				var isValid = redirect != null || originalValid;
				var recipientName = redirect != null
					? ("[تست] " + originalMobile + (string.IsNullOrWhiteSpace(request.RecipientName) ? "" : " - " + request.RecipientName))
					: request.RecipientName;
				if (recipientName is { Length: > 200 })
					recipientName = recipientName[..200];

				var message = new SmsMessage
				{
					EventKey = request.EventKey.Trim(),
					Category = category,
					MobileNumber = mobile.Length > 20 ? mobile[..20] : mobile,
					RecipientName = recipientName,
					Body = body.Length > 2000 ? body[..2000] : body,
					IsSensitive = isSensitive,
					SegmentCount = segments,
					SourceEntityType = request.SourceEntityType,
					SourceEntityId = request.SourceEntityId,
					Status = SmsStatusEnum.Pending,
					IsActive = IsActiveEnum.Active,
					CreatedById = request.CreatedById,
					CreatedByName = request.CreatedByName,
					CreatedOnMiladiDateTime = now,
					CreatedOnShamsiDateTime = now.ToShamsiDateTime()
				};

				if (request.ScheduledOn.HasValue)
				{
					message.ScheduledOnMiladiDateTime = request.ScheduledOn;
					message.ScheduledOnShamsiDateTime = request.ScheduledOn.Value.ToShamsiDateTime();
				}

				if (string.IsNullOrWhiteSpace(body))
				{
					message.Status = SmsStatusEnum.Failed;
					message.ErrorMessage = "متن پیامک خالی است.";
				}
				else if (!isValid)
				{
					message.Status = SmsStatusEnum.InvalidMobile;
					message.ErrorMessage = "شماره موبایل معتبر نیست.";
				}

				items.Add(message);
			}

			unitOfWork.Repository<SmsMessage>().Entities.AddRange(items);
			await unitOfWork.SaveChangesAsync(ct);
			return items;
		}

		public async Task<bool> TryEnqueueAsync(
			string eventKey,
			string? receivers,
			string body,
			string? sourceEntityType = null,
			long? sourceEntityId = null,
			DateTime? scheduledOn = null,
			CancellationToken ct = default)
		{
			try
			{
				var items = await EnqueueAsync(eventKey, receivers, body, sourceEntityType, sourceEntityId, scheduledOn, ct);
				return items.Count > 0;
			}
			catch (Exception)
			{
				// اگر ذخیره پیامک شکست خورد، ردیف‌های ناقص را از context جدا می‌کنیم تا ذخیره‌های بعدی همان درخواست خراب نشوند.
				foreach (var entry in db.ChangeTracker.Entries<SmsMessage>().Where(x => x.State == EntityState.Added).ToList())
					entry.State = EntityState.Detached;
				return false;
			}
		}

		public Task<bool> ExistsAsync(string eventKey, string sourceEntityType, long sourceEntityId, CancellationToken ct = default)
			=> unitOfWork.Repository<SmsMessage>().TableNoTracking.AnyAsync(
				x => x.EventKey == eventKey
					&& x.SourceEntityType == sourceEntityType
					&& x.SourceEntityId == sourceEntityId
					&& x.Status != SmsStatusEnum.Cancelled,
				ct);

		public async Task<int> CancelPendingAsync(string eventKey, string sourceEntityType, long sourceEntityId, CancellationToken ct = default)
		{
			var pending = await unitOfWork.Repository<SmsMessage>().Table
				.Where(x => x.EventKey == eventKey
					&& x.SourceEntityType == sourceEntityType
					&& x.SourceEntityId == sourceEntityId
					&& x.Status == SmsStatusEnum.Pending)
				.ToListAsync(ct);

			foreach (var message in pending)
			{
				message.Status = SmsStatusEnum.Cancelled;
				message.ModifiedDateMiladiDateTime = DateTime.Now;
				message.ModifiedDateShamsiDateTime = DateTime.Now.ToShamsiDateTime();
			}

			if (pending.Count > 0)
				await unitOfWork.SaveChangesAsync(ct);

			return pending.Count;
		}
	}
}
