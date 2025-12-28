using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.Base.Job;
using Entities.Base.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
 

namespace Services.Job
{
	public class JobWorkerWithLogging : BackgroundService
	{
		private readonly IServiceProvider _services;
		private readonly ILogger<JobWorkerWithLogging> _logger;

		public JobWorkerWithLogging(IServiceProvider services, ILogger<JobWorkerWithLogging> logger)
		{
			_services = services;
			_logger = logger;
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

			// 1. پیدا کردن جاب‌هایی که زمان اجرایشان رسیده است
			var dueJobs = db.JobSchedules
			    .Where(s => s.IsActive && s.NextRunTime <= DateTime.Now
			    &&
			    s.LastStatus != JobStatus.Running && s.LastStatus != JobStatus.Error)
			    .ToList();

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
				await db.SaveChangesAsync();

				_ = Task.Run(async () =>
				{
					await ExecuteJobAsync(schedule.Id, history.Id, stoppingToken);
				});
			}
		}

		private async Task ExecuteJobAsync(int scheduleId, long historyId, CancellationToken stoppingToken)
		{
			using var scope = _services.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var _unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
			var _jobLogger = scope.ServiceProvider.GetRequiredService<IJobLogger>();
			
			var schedule = await db.JobSchedules.FindAsync(scheduleId);
			var history = await db.JobHistories.FindAsync(historyId);
			var jobDef = await db.JobDefinitions.FirstOrDefaultAsync(c => c.Id == schedule.JobId);

			try
			{
				// Set current history ID for job logging context
				_jobLogger.SetCurrentHistoryId(historyId);

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
				history.IsSuccess = false;
				history.ErrorMessage = ex.Message ?? "خطای نا مشخص";
				await _jobLogger.LogExceptionAsync(ex, historyId, stoppingToken);

				schedule.LastStatus = JobStatus.Error;

				var query = @"INSERT INTO [system].[Notification]
(Title, Body, IsRead, OwnerId,ModifiedDateShamsiDateTime,ModifiedDateMiladiDateTime,CreatedOnShamsiDateTime,CreatedOnMiladiDateTime,IsActive)
VALUES (@Title, @Body, @IsRead, @OwnerId,@ModifiedDateShamsiDateTime,@ModifiedDateMiladiDateTime,@CreatedOnShamsiDateTime,@CreatedOnMiladiDateTime,@IsActive);";

				var paramsSql = new
				{
					Body = $"خطا در اجرای سرویس : {jobDef.DisplayName} {jobDef.MethodName} <br/> {ex.Message}",
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

				var queryJobSchedule = @"update [system].[JobSchedule] set LastStatus = 3 where Id = @Id";

				var paramsJobScheduleSql = new
				{
					Id = schedule.Id
				};

				await _unitOfWork.Repository<Notification>()
				.ExecuteCommandAsync(queryJobSchedule, paramsJobScheduleSql, stoppingToken);
			}
			finally
			{
				// 4. پایان کار و محاسبه زمان بعدی
				history.EndTime = DateTime.Now;
				
				// محاسبه زمان بعدی بر اساس نوع زمان‌بندی
				schedule.NextRunTime = CalculateNextRunTime(schedule);

				await db.SaveChangesAsync();
			}
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
					// محاسبه زمان بعدی برای زمان‌بندی روزانه با فاصله
					var nextDailyTime = schedule.DailyTime ?? TimeSpan.Zero;
					var nextDate = now.Date.AddDays(schedule.DailyIntervalDays);
					
					// اگر زمان مشخص شده امروز گذشته است، به روز بعد برو
					if (now.TimeOfDay >= nextDailyTime)
					{
						nextDate = nextDate.AddDays(schedule.DailyIntervalDays);
					}
					
					return nextDate.Add(nextDailyTime);

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