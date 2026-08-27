using Common.Attributes;
using Data;
using Entities.Base.Job;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Job
{
	public class JobDiscoveryService : IHostedService
	{
		private const string ProductPriceSnapshotJobId =
			"App.BackgroundJob.Jobs.Bom.ProductPriceSnapshotJob.CaptureDailyProductPriceSnapshot";

		private readonly IServiceProvider _serviceProvider;

		public JobDiscoveryService(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			using var scope = _serviceProvider.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

			// پیدا کردن تمام کلاس‌هایی که متدهای Job دارند
			var methods = AppDomain.CurrentDomain.GetAssemblies()
			    .SelectMany(a => a.GetTypes())
			    .SelectMany(t => t.GetMethods())
			    .Where(m => m.GetCustomAttributes(typeof(JobHandlerAttribute), false).Length > 0)
			    .ToArray();

			var methodsId = methods.Select(c=> $"{c.DeclaringType.FullName}.{c.Name}").ToList();

			var existJobs = await dbContext.JobDefinitions.ToArrayAsync(cancellationToken);

			var needDeleteJobs = existJobs.Where(c => !methodsId.Contains(c.JobId));

			foreach (var job in needDeleteJobs)
			{
				var js = dbContext.JobSchedules.Where(c => c.JobId == job.Id).ToList();

				dbContext.JobSchedules.RemoveRange(js);
				dbContext.JobDefinitions.Remove(job);

			}
			await dbContext.SaveChangesAsync();

			foreach (var method in methods)
			{
				var attr = (JobHandlerAttribute)method.GetCustomAttributes(typeof(JobHandlerAttribute), false)[0];
				var jobId = $"{method.DeclaringType.FullName}.{method.Name}";

				var existingJob = await dbContext.JobDefinitions.FirstOrDefaultAsync(c=>c.JobId == jobId);
				if (existingJob == null)
				{
					dbContext.JobDefinitions.Add(new JobDefinition
					{
						JobId = jobId,
						DisplayName = attr.DisplayName,
						Description = attr.Description ?? "بدون مقدار",
						AssemblyName = method.DeclaringType.Assembly.FullName,
						ClassType = method.DeclaringType.FullName,
						MethodName = method.Name
					});
				}
				else
				{
					// آپدیت کردن نام و توضیحات در صورت تغییر در کد
					existingJob.DisplayName = attr.DisplayName;
					existingJob.Description = attr?.Description ?? "بدون مقدار";
					var js = dbContext.JobSchedules.Where(c => c.JobId == existingJob.Id).ToList();
					foreach (var j in js) {
						j.LastStatus = JobStatus.Idle;
					}

					dbContext.JobSchedules.UpdateRange(js);

				}
			}
			await dbContext.SaveChangesAsync();

			// این جاب ماهیت زیرساختی دارد و باید از اولین استقرار، روزانه فعال باشد.
			// اجرای اول چند ثانیه بعد انجام می‌شود تا اولین snapshot بدون انتظار تا روز بعد ساخته شود.
			var snapshotJob = await dbContext.JobDefinitions
				.FirstOrDefaultAsync(x => x.JobId == ProductPriceSnapshotJobId, cancellationToken);

			if (snapshotJob != null
				&& !await dbContext.JobSchedules.AnyAsync(x => x.JobId == snapshotJob.Id, cancellationToken))
			{
				dbContext.JobSchedules.Add(new JobSchedule
				{
					JobId = snapshotJob.Id,
					IsActive = true,
					ScheduleType = ScheduleType.Daily,
					DailyIntervalDays = 1,
					DailyTime = TimeSpan.FromHours(1),
					IntervalSeconds = 86400,
					NextRunTime = DateTime.Now.AddSeconds(10),
					LastStatus = JobStatus.Idle
				});

				await dbContext.SaveChangesAsync(cancellationToken);
			}
		}

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
