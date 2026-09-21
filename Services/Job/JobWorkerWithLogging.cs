using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.Base.Job;
using Entities.Base.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;


namespace Services.Job
{
	public class JobWorkerWithLogging : BackgroundService
	{
		private readonly IServiceProvider _services;
		private readonly ILogger<JobWorkerWithLogging> _logger;
		private readonly IJobRealtimeNotifier _realtimeNotifier;

		public JobWorkerWithLogging(
			IServiceProvider services,
			ILogger<JobWorkerWithLogging> logger,
			IJobRealtimeNotifier realtimeNotifier)
		{
			_services = services;
			_logger = logger;
			_realtimeNotifier = realtimeNotifier;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					await ProcessDueJobs(stoppingToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error in JobWorkerWithLogging loop");
				}

				// فاصله زمانی بین هر چک (مثلا ۵ ثانیه)
				await Task.Delay(5000, stoppingToken);
			}
		}

		private async Task ProcessDueJobs(CancellationToken stoppingToken)
		{
			using var scope = _services.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			// فقط Idle و Waiting اجرا می‌شوند. Error تا تغییر دستی وضعیت (اجرا فوری) تکرار نمی‌شود.
			var dueJobs = await db.JobSchedules
			    .Where(s => s.IsActive
			        && s.NextRunTime <= DateTime.Now
			        && (s.LastStatus == JobStatus.Idle || s.LastStatus == JobStatus.Waiting))
			    .ToListAsync(stoppingToken);

			foreach (var schedule in dueJobs)
			{
				// تغییر وضعیت به Running برای جلوگیری از اجرای همزمان تکراری
				schedule.LastStatus = JobStatus.Running;
				schedule.LastRunTime = DateTime.Now;

				// ایجاد رکورد لاگ
				var history = new JobHistory
				{
					ScheduleId = schedule.Id,
					StartTime = DateTime.Now,
					LogOutput = "Job started"
				};
				db.JobHistories.Add(history);
				await db.SaveChangesAsync(stoppingToken);

				await _realtimeNotifier.NotifyStatusChangedAsync(
					schedule.Id,
					JobStatus.Running.ToString(),
					schedule.LastRunTime,
					schedule.NextRunTime,
					stoppingToken);

				_ = Task.Run(async () =>
				{
					try
					{
						await ExecuteJobAsync(schedule.Id, history.Id, stoppingToken);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Unhandled error while executing schedule {ScheduleId}", schedule.Id);
					}
				}, stoppingToken);
			}
		}

		private async Task ExecuteJobAsync(int scheduleId, long historyId, CancellationToken stoppingToken)
		{
			using var scope = _services.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var _unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
			var _jobLogger = scope.ServiceProvider.GetRequiredService<IJobLogger>();
			var realtimeNotifier = scope.ServiceProvider.GetRequiredService<IJobRealtimeNotifier>();

			var schedule = await db.JobSchedules.FindAsync(new object[] { scheduleId }, stoppingToken);
			var history = await db.JobHistories.FindAsync(new object[] { historyId }, stoppingToken);
			if (schedule == null || history == null)
				return;

			var jobDef = await db.JobDefinitions.FirstOrDefaultAsync(c => c.Id == schedule.JobId, stoppingToken);

			try
			{
				if (jobDef == null)
					throw new InvalidOperationException($"Job definition not found for schedule {scheduleId}");

				// Set current history ID for job logging context
				_jobLogger.SetCurrentHistoryId(historyId, scheduleId);

				// Log job start
				await _jobLogger.LogInfoAsync($"Job '{jobDef.DisplayName}' started execution", historyId, stoppingToken);

				// 1. لود کردن تایپ کلاس
				Type type = Type.GetType($"{jobDef.ClassType}, {jobDef.AssemblyName}");

				// 2. دریافت اینستنس کلاس از DI (چون الان اتوماتیک رجیستر شده‌اند)
				// اگر کلاس رجیستر نشده باشد اینجا خطا می‌دهد که ایمن‌تر است
				var instance = scope.ServiceProvider.GetRequiredService(type);

				// 3. پیدا کردن و اجرای متد
				var method = type.GetMethod(jobDef.MethodName);

				// Check if the method accepts IJobLogger parameter
				var parameters = method.GetParameters();
				bool hasLoggerParameter = parameters.Any(p => p.ParameterType == typeof(IJobLogger));

				object result;

				if (hasLoggerParameter)
				{
					// If method accepts logger, pass it (it will automatically use the current history ID)
					result = method.Invoke(instance, new object[] { _jobLogger, stoppingToken });
				}
				else
				{
					// Original behavior for methods without logger parameter
					result = method.Invoke(instance, new object[] { stoppingToken });
				}

				if (result is Task task)
				{
					await task;
				}

				history.IsSuccess = true;
				await _jobLogger.LogInfoAsync($"Job '{jobDef.DisplayName}' completed successfully", historyId, stoppingToken);
				schedule.LastStatus = JobStatus.Idle;
			}
			catch (Exception ex)
			{
				var actual = UnwrapInvocationException(ex);
				history.IsSuccess = false;
				history.ErrorMessage = Truncate(actual.Message ?? "خطای نا مشخص", 2000);
				schedule.LastStatus = JobStatus.Error;
				history.EndTime = DateTime.Now;

				// وضعیت Error باید قبل از هر کار جانبی در دیتابیس بنشیند تا ورکر دوباره انتخابش نکند.
				await db.SaveChangesAsync(stoppingToken);

				try
				{
					await _jobLogger.LogExceptionAsync(actual, historyId, stoppingToken);
				}
				catch (Exception logEx)
				{
					_logger.LogError(logEx, "Failed to write job exception log for schedule {ScheduleId}", scheduleId);
				}

				try
				{
					var displayName = jobDef?.DisplayName ?? $"Schedule {scheduleId}";
					var methodName = jobDef?.MethodName ?? "";
					var query = @"INSERT INTO [system].[Notification]
(Title, Body, IsRead, OwnerId,ModifiedDateShamsiDateTime,ModifiedDateMiladiDateTime,CreatedOnShamsiDateTime,CreatedOnMiladiDateTime,IsActive)
VALUES (@Title, @Body, @IsRead, @OwnerId,@ModifiedDateShamsiDateTime,@ModifiedDateMiladiDateTime,@CreatedOnShamsiDateTime,@CreatedOnMiladiDateTime,@IsActive);";

					var paramsSql = new
					{
						Body = $"خطا در اجرای سرویس : {displayName} {methodName} <br/> {actual.Message}",
						Title = "خطا در سرویس های درحال اجرا",
						OwnerId = 1,
						IsRead = false,
						ModifiedDateShamsiDateTime = DateTime.Now.ToShamsiDateTime(),
						ModifiedDateMiladiDateTime = DateTime.Now,
						CreatedOnShamsiDateTime = DateTime.Now.ToShamsiDateTime(),
						CreatedOnMiladiDateTime = DateTime.Now,
						IsActive = 1
					};

					await _unitOfWork.Repository<Notification>()
						.ExecuteCommandAsync(query, paramsSql, stoppingToken);
				}
				catch (Exception notifyEx)
				{
					_logger.LogError(notifyEx, "Failed to insert failure notification for schedule {ScheduleId}", scheduleId);
				}
			}
			finally
			{
				if (history.EndTime == null)
					history.EndTime = DateTime.Now;

				// جاب خطا خورده نباید زمان اجرای بعدی بگیرد؛ تا تغییر دستی وضعیت دوباره انتخاب نمی‌شود.
				if (schedule.LastStatus != JobStatus.Error)
					schedule.NextRunTime = CalculateNextRunTime(schedule);

				await db.SaveChangesAsync(stoppingToken);

				var finalStatus = schedule.LastStatus.ToString();
				await realtimeNotifier.NotifyStatusChangedAsync(
					schedule.Id,
					finalStatus,
					schedule.LastRunTime,
					schedule.NextRunTime,
					stoppingToken);

				await realtimeNotifier.NotifyHistoryCompletedAsync(
					historyId,
					scheduleId,
					history.IsSuccess,
					history.EndTime.Value,
					stoppingToken);
			}
		}

		private static Exception UnwrapInvocationException(Exception ex)
		{
			while (ex is TargetInvocationException { InnerException: { } inner })
				ex = inner;

			if (ex is AggregateException { InnerException: { } aggregateInner })
				return aggregateInner;

			return ex;
		}

		private static string Truncate(string value, int maxLength)
		{
			if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
				return value;

			return value[..maxLength];
		}

		// متد کمکی برای محاسبه زمان اجرای بعدی بر اساس نوع زمان‌بندی
		private DateTime CalculateNextRunTime(JobSchedule schedule)
		{
			var now = DateTime.Now;

			switch (schedule.ScheduleType)
			{
				case ScheduleType.Interval:
					return now.AddSeconds(schedule.IntervalSeconds);

				case ScheduleType.Daily:
					var nextDailyTime = schedule.DailyTime ?? TimeSpan.Zero;
					var dailyIntervalDays = Math.Max(1, schedule.DailyIntervalDays);
					var nextDailyRun = now.Date.Add(nextDailyTime);

					if (nextDailyRun <= now)
						nextDailyRun = nextDailyRun.AddDays(dailyIntervalDays);

					return nextDailyRun;

				case ScheduleType.Weekly:
					// محاسبه زمان بعدی برای زمان‌بندی هفتگی
					if (string.IsNullOrEmpty(schedule.WeeklyDays))
						return now.AddDays(1); // اگر روزی انتخاب نشده، فردا اجرا کن

					var weeklyDays = schedule.WeeklyDays.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					var nextWeeklyDate = now.Date;

					// پیدا کردن نزدیک‌ترین روز هفته که بعد از امروز باشد
					for (int i = 1; i <= 7; i++)
					{
						var testDate = nextWeeklyDate.AddDays(i);
						var dayName = testDate.DayOfWeek.ToString();

						if (weeklyDays.Contains(dayName))
						{
							var dailyTime = schedule.DailyTime ?? TimeSpan.Zero;
							return testDate.Add(dailyTime);
						}
					}

					// اگر هیچ روزی پیدا نشد، 7 روز بعد اجرا کن
					return now.AddDays(7);

				case ScheduleType.Hourly:
					// محاسبه زمان بعدی برای زمان‌بندی ساعتی
					var nextHour = now.AddHours(1);
					return new DateTime(nextHour.Year, nextHour.Month, nextHour.Day, nextHour.Hour, schedule.HourlyMinute, 0);

				default:
					return now.AddSeconds(schedule.IntervalSeconds);
			}
		}
	}
}
