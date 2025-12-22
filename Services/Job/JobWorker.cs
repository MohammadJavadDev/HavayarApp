using Data;
using Entities.Base.Job;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Job
{
	public class JobWorker : BackgroundService
	{
		private readonly IServiceProvider _services;
		private readonly ILogger<JobWorker> _logger;

		public JobWorker(IServiceProvider services, ILogger<JobWorker> logger)
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
					await ProcessDueJobs();
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error in JobWorker loop");
				}

				// فاصله زمانی بین هر چک (مثلا ۵ ثانیه)
				await Task.Delay(5000, stoppingToken);
			}
		}

		private async Task ProcessDueJobs()
		{
			using var scope = _services.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			// 1. پیدا کردن جاب‌هایی که زمان اجرایشان رسیده است
			var dueJobs = db.JobSchedules
			    .Where(s => s.IsActive && s.NextRunTime <= DateTime.Now && s.LastStatus != JobStatus.Running)
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
					LogOutput = "logOutPut"
				};
				db.JobHistories.Add(history);
				await db.SaveChangesAsync();

				// اجرای واقعی جاب در یک Thread جداگانه تا Loop اصلی مسدود نشود
				_ = Task.Run(async () =>
				{
					await ExecuteJobAsync(schedule.Id, history.Id);
				});
			}
		}

		private async Task ExecuteJobAsync(int scheduleId, long historyId)
		{
			using var scope = _services.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			var schedule = await db.JobSchedules.FindAsync(scheduleId);
			var history = await db.JobHistories.FindAsync(historyId);
			var jobDef = await db.JobDefinitions.FirstOrDefaultAsync(c=>c.JobId== schedule.JobId);

			var logBuilder = new System.Text.StringBuilder();

			try
			{
				// 1. لود کردن تایپ کلاس
				Type type = Type.GetType($"{jobDef.ClassType}, {jobDef.AssemblyName}");

				// 2. دریافت اینستنس کلاس از DI (چون الان اتوماتیک رجیستر شده‌اند)
				// اگر کلاس رجیستر نشده باشد اینجا خطا می‌دهد که ایمن‌تر است
				var instance = scope.ServiceProvider.GetRequiredService(type);

				// 3. پیدا کردن و اجرای متد
				var method = type.GetMethod(jobDef.MethodName);

				var result = method.Invoke(instance, null);
				if (result is Task task)
				{
					await task;
				}

				history.IsSuccess = true;
				logBuilder.AppendLine("Job executed successfully.");
			}
			catch (Exception ex)
			{
				history.IsSuccess = false;
				history.ErrorMessage = ex.Message ?? "خطای نا مشخص";
				logBuilder.AppendLine($"Error: {ex}");
			}
			finally
			{
				// 4. پایان کار و محاسبه زمان بعدی
				history.EndTime = DateTime.Now;
				history.LogOutput = logBuilder.ToString();

				schedule.LastStatus = JobStatus.Idle;
				// محاسبه زمان بعدی (مثلا ۱۰ دقیقه دیگر)
				schedule.NextRunTime = DateTime.Now.AddSeconds(schedule.IntervalSeconds);

				await db.SaveChangesAsync();
			}
		}
	}
}
