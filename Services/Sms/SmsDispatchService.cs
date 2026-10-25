using Common.Utilities;
using Data.Contracts;
using Entities.Base;
using Entities.Base.Sms;
using Entities.Base.Sms.Enums;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using Services.Settings;

namespace Services.Sms
{
	public class SmsDispatchResult
	{
		public int Processed { get; set; }
		public int Sent { get; set; }
		public int Retrying { get; set; }
		public int Failed { get; set; }
		public int Recovered { get; set; }
		public string? ConfigurationError { get; set; }
		public bool Disabled { get; set; }
	}

	/// <summary>
	/// پردازش صف <c>system.SmsMessage</c>: ارسال از طریق <see cref="ISmsProvider"/>، ثبت هر تلاش در
	/// <c>SmsMessageAttempt</c> و به‌روزرسانی وضعیت. توسط <c>SmsJob</c> در App.BackgroundJob صدا زده می‌شود.
	/// </summary>
	public interface ISmsDispatchService
	{
		Task<SmsDispatchResult> ProcessPendingAsync(IJobLogger? jobLogger = null, CancellationToken ct = default);

		/// <summary>استعلام وضعیت دلیوری پیامک‌های ارسال‌شده و به‌روزرسانی «تحویل به گیرنده / تحویل نشد».</summary>
		Task<SmsDeliveryCheckResult> CheckDeliveryAsync(IJobLogger? jobLogger = null, CancellationToken ct = default);
	}

	public class SmsDeliveryCheckResult
	{
		public int Checked { get; set; }
		public int Delivered { get; set; }
		public int NotDelivered { get; set; }
		public int StillPending { get; set; }
		public int CallFailed { get; set; }
		public string? ConfigurationError { get; set; }
	}

	public class SmsDispatchService(
		IUnitOfWork unitOfWork,
		IGeneralSettingService settings,
		ISmsProvider provider) : ISmsDispatchService
	{
		private const string SensitiveBodyPlaceholder = "[متن حساس پس از ارسال حذف شد]";
		private static readonly TimeSpan StuckSendingAfter = TimeSpan.FromMinutes(10);

		public async Task<SmsDispatchResult> ProcessPendingAsync(IJobLogger? jobLogger = null, CancellationToken ct = default)
		{
			var result = new SmsDispatchResult();

			if (!await settings.GetBoolAsync(GeneralSettingKeys.SmsEnabled, true, ct))
			{
				result.Disabled = true;
				await Log(jobLogger, "ارسال پیامک غیرفعال است (Sms:Enabled=false)؛ پیامک‌ها در صف می‌مانند.", ct);
				return result;
			}

			var configError = await provider.CheckConfigurationAsync(ct);
			if (configError != null)
			{
				result.ConfigurationError = configError;
				if (jobLogger != null)
					await jobLogger.LogErrorAsync(configError, 0, ct);
				return result;
			}

			var repo = unitOfWork.Repository<SmsMessage>();

			// پیامک‌هایی که در وضعیت «در حال ارسال» گیر کرده‌اند (کرش وسط ارسال) دوباره به صف برمی‌گردند.
			var stuckBefore = DateTime.Now - StuckSendingAfter;
			var stuck = await repo.Table
				.Where(x => x.Status == SmsStatusEnum.Sending
					&& (x.LastAttemptOnMiladiDateTime == null || x.LastAttemptOnMiladiDateTime < stuckBefore))
				.ToListAsync(ct);
			foreach (var item in stuck)
			{
				item.Status = SmsStatusEnum.Pending;
				item.ErrorMessage = "ارسال قبلی نیمه‌کاره ماند؛ دوباره در صف قرار گرفت.";
				Touch(item);
			}
			if (stuck.Count > 0)
			{
				await unitOfWork.SaveChangesAsync(ct);
				result.Recovered = stuck.Count;
			}

			var batchSize = Math.Clamp(await settings.GetIntAsync(GeneralSettingKeys.SmsBatchSize, 100, ct), 1, 1000);
			var maxAttempts = Math.Max(1, await settings.GetIntAsync(GeneralSettingKeys.SmsMaxAttempts, 3, ct));
			var retryDelay = Math.Max(1, await settings.GetIntAsync(GeneralSettingKeys.SmsRetryDelayMinutes, 5, ct));
			var lineNumber = await provider.GetLineNumberAsync(ct);

			var now = DateTime.Now;
			var batch = await repo.Table
				.Where(x => x.Status == SmsStatusEnum.Pending
					&& (x.ScheduledOnMiladiDateTime == null || x.ScheduledOnMiladiDateTime <= now))
				.OrderBy(x => x.Id)
				.Take(batchSize)
				.ToListAsync(ct);

			if (batch.Count == 0)
			{
				await Log(jobLogger, "پیامکی برای ارسال در صف نیست.", ct);
				return result;
			}

			// قبل از ارسال، همه را «در حال ارسال» علامت می‌زنیم تا اجرای هم‌زمان دیگری آن‌ها را برندارد.
			foreach (var item in batch)
			{
				item.Status = SmsStatusEnum.Sending;
				item.LastAttemptOnMiladiDateTime = now;
				Touch(item);
			}
			await unitOfWork.SaveChangesAsync(ct);

			foreach (var message in batch)
			{
				ct.ThrowIfCancellationRequested();
				result.Processed++;

				SmsProviderResult send;
				try
				{
					send = await provider.SendAsync(message.MobileNumber, message.Body, ct);
				}
				catch (OperationCanceledException)
				{
					message.Status = SmsStatusEnum.Pending; // اجرای بعدی ادامه می‌دهد
					Touch(message);
					await unitOfWork.SaveChangesAsync(CancellationToken.None);
					throw;
				}
				catch (Exception ex)
				{
					send = new SmsProviderResult { Success = false, Error = ex.Message };
				}

				var attemptTime = DateTime.Now;
				message.AttemptCount++;
				message.LastAttemptOnMiladiDateTime = attemptTime;
				message.ProviderName = provider.Name;
				message.LineNumber = lineNumber;

				if (send.Success)
				{
					message.Status = SmsStatusEnum.Sent;
					message.SentOnMiladiDateTime = attemptTime;
					message.SentOnShamsiDateTime = attemptTime.ToShamsiDateTime();
					message.ProviderMessageId = send.ProviderMessageId;
					message.ErrorMessage = null;
					if (message.IsSensitive)
						message.Body = SensitiveBodyPlaceholder;
					result.Sent++;
				}
				else
				{
					message.ErrorMessage = send.Error;
					if (send.IsPermanentFailure || message.AttemptCount >= maxAttempts)
					{
						message.Status = SmsStatusEnum.Failed;
						result.Failed++;
					}
					else
					{
						var next = attemptTime.AddMinutes(retryDelay * message.AttemptCount);
						message.Status = SmsStatusEnum.Pending;
						message.ScheduledOnMiladiDateTime = next;
						message.ScheduledOnShamsiDateTime = next.ToShamsiDateTime();
						result.Retrying++;
					}
				}

				Touch(message);
				message.Attempts.Add(new SmsMessageAttempt
				{
					SmsMessageId = message.Id ?? 0,
					AttemptNumber = message.AttemptCount,
					ResultStatus = send.Success ? SmsStatusEnum.Sent : (message.Status == SmsStatusEnum.Failed ? SmsStatusEnum.Failed : SmsStatusEnum.Pending),
					ProviderMessageId = send.ProviderMessageId,
					ProviderStatusCode = send.StatusCode,
					ErrorMessage = send.Error,
					DurationMs = send.DurationMs,
					IsActive = IsActiveEnum.Active,
					CreatedOnMiladiDateTime = attemptTime,
					CreatedOnShamsiDateTime = attemptTime.ToShamsiDateTime()
				});

				// بعد از هر پیامک ذخیره می‌کنیم تا اگر وسط کار قطع شد، پیامک‌های ارسال‌شده دوباره ارسال نشوند.
				await unitOfWork.SaveChangesAsync(CancellationToken.None);
			}

			await Log(jobLogger,
				$"ارسال پیامک — پردازش: {result.Processed}، ارسال‌شده: {result.Sent}، تلاش مجدد: {result.Retrying}، ناموفق: {result.Failed}، بازیابی‌شده: {result.Recovered}",
				ct);

			return result;
		}

		public async Task<SmsDeliveryCheckResult> CheckDeliveryAsync(IJobLogger? jobLogger = null, CancellationToken ct = default)
		{
			var result = new SmsDeliveryCheckResult();

			var configError = await provider.CheckConfigurationAsync(ct);
			if (configError != null)
			{
				result.ConfigurationError = configError;
				if (jobLogger != null)
					await jobLogger.LogErrorAsync(configError, 0, ct);
				return result;
			}

			var batchSize = Math.Clamp(await settings.GetIntAsync(GeneralSettingKeys.SmsDeliveryBatchSize, 200, ct), 1, 2000);
			var checkHours = Math.Max(1, await settings.GetIntAsync(GeneralSettingKeys.SmsDeliveryCheckHours, 48, ct));

			var now = DateTime.Now;
			var since = now.AddHours(-checkHours);
			var minGap = now.AddMinutes(-3);

			// جدیدها هر چند دقیقه، قدیمی‌تر از یک ساعت هر ۳۰ دقیقه استعلام می‌شوند تا به API فشار نیاید.
			var candidates = await unitOfWork.Repository<SmsMessage>().Table
				.Where(x => x.Status == SmsStatusEnum.Sent
					&& x.ProviderMessageId != null
					&& x.SentOnMiladiDateTime != null && x.SentOnMiladiDateTime >= since
					&& (x.LastDeliveryCheckOnMiladiDateTime == null || x.LastDeliveryCheckOnMiladiDateTime < minGap))
				.OrderBy(x => x.LastDeliveryCheckOnMiladiDateTime)
				.ThenBy(x => x.Id)
				.Take(batchSize * 2)
				.ToListAsync(ct);

			var oldCutoff = now.AddHours(-1);
			var oldGap = now.AddMinutes(-30);
			var batch = candidates
				.Where(x => x.SentOnMiladiDateTime >= oldCutoff
					|| x.LastDeliveryCheckOnMiladiDateTime == null
					|| x.LastDeliveryCheckOnMiladiDateTime < oldGap)
				.Take(batchSize)
				.ToList();

			if (batch.Count == 0)
			{
				await Log(jobLogger, "پیامکی برای استعلام دلیوری نیست.", ct);
				return result;
			}

			foreach (var message in batch)
			{
				ct.ThrowIfCancellationRequested();
				result.Checked++;

				var delivery = await provider.GetDeliveryAsync(message.ProviderMessageId!, ct);
				var checkedAt = DateTime.Now;
				message.DeliveryCheckCount++;
				message.LastDeliveryCheckOnMiladiDateTime = checkedAt;

				if (!delivery.CallSucceeded)
				{
					result.CallFailed++;
					Touch(message);
					await unitOfWork.SaveChangesAsync(CancellationToken.None);
					continue;
				}

				message.DeliveryStateCode = delivery.StateCode;

				switch (delivery.Outcome)
				{
					case SmsDeliveryOutcome.Delivered:
						message.Status = SmsStatusEnum.Delivered;
						var deliveredOn = delivery.DeliveredOn ?? checkedAt;
						message.DeliveredOnMiladiDateTime = deliveredOn;
						message.DeliveredOnShamsiDateTime = deliveredOn.ToShamsiDateTime();
						message.ErrorMessage = null;
						result.Delivered++;
						break;

					case SmsDeliveryOutcome.NotDelivered:
						message.Status = SmsStatusEnum.NotDelivered;
						message.ErrorMessage = $"عدم تحویل طبق گزارش پنل: {delivery.StateText}";
						result.NotDelivered++;
						break;

					default:
						result.StillPending++;
						break;
				}

				Touch(message);
				await unitOfWork.SaveChangesAsync(CancellationToken.None);
			}

			await Log(jobLogger,
				$"استعلام دلیوری — بررسی‌شده: {result.Checked}، تحویل: {result.Delivered}، تحویل نشد: {result.NotDelivered}، در انتظار: {result.StillPending}، خطای استعلام: {result.CallFailed}",
				ct);

			return result;
		}

		private static void Touch(SmsMessage message)
		{
			var now = DateTime.Now;
			message.ModifiedDateMiladiDateTime = now;
			message.ModifiedDateShamsiDateTime = now.ToShamsiDateTime();
		}

		private static Task Log(IJobLogger? jobLogger, string message, CancellationToken ct)
			=> jobLogger?.LogInfoAsync(message, ct) ?? Task.CompletedTask;
	}
}
