using Common.Attributes;
using Services.Job;
using Services.Sms;

namespace App.BackgroundJob.Jobs.System
{
	/// <summary>ارسال پیامک‌های صف‌شده (system.SmsMessage) و استعلام دلیوری، از طریق پنل تنظیم‌شده در GeneralSetting.</summary>
	public class SmsJob(ISmsDispatchService dispatchService)
	{
		[JobHandler(
			"ارسال پیامک‌های در صف",
			"پیامک‌های در صف (system.SmsMessage) را به پنل پیامکی می‌فرستد، هر تلاش را ثبت می‌کند و در صورت خطای موقت دوباره تلاش می‌کند.")]
		public async Task SendPendingSmsAsync(IJobLogger? jobLogger = null, CancellationToken cancellationToken = default)
		{
			// خطای تنظیمات (مثلاً خالی بودن ApiKey) در لاگ job ثبت می‌شود و پیامک‌ها در صف می‌مانند؛
			// job نباید خطا بدهد تا بعد از اصلاح تنظیم، خودش از اجرای بعدی ادامه دهد.
			await dispatchService.ProcessPendingAsync(jobLogger, cancellationToken);
		}

		[JobHandler(
			"استعلام وضعیت تحویل پیامک‌ها",
			"وضعیت دلیوری پیامک‌های ارسال‌شده را از پنل می‌پرسد و آن‌ها را «تحویل به گیرنده» یا «تحویل نشد» می‌کند.")]
		public async Task CheckSmsDeliveryAsync(IJobLogger? jobLogger = null, CancellationToken cancellationToken = default)
		{
			await dispatchService.CheckDeliveryAsync(jobLogger, cancellationToken);
		}
	}
}
