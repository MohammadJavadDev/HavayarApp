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
		private const string CustomerAddressAgencyDlJobId =
			"App.BackgroundJob.Jobs.Sls.CustomerAddressJob.FillEmptyAgencyDlFromRahkaranRegion";
		private const string CustomerAddressFromRahkaranJobId =
			"App.BackgroundJob.Jobs.Sls.CustomerAddressJob.AddCustomerAddressFromRahkaran";
		private const string AgencyPartCardexJobId =
			"App.BackgroundJob.Jobs.Sale.AgencyPartCardexJob.AddNewItemsByCheckSaleOrders";
		private const string InvSystemTasksJobId =
			"App.BackgroundJob.Jobs.Inv.PartCardexJob.RunInvSystemTasks";
		private const string CngRepairsUsedPartImportJobId =
			"App.BackgroundJob.Jobs.Cng.CngRepairsUsedPartImportJob.ImportFromRahkaran";
		private const string RepairRequestPartImportJobId =
			"App.BackgroundJob.Jobs.Rpr.RepairRequestPartImportJob.ImportFromRahkaran";
		private const string CngMaintenanceContractExpireJobId =
			"App.BackgroundJob.Jobs.Cng.CngMaintenanceContractExpireJob.CheckExpiredItems";
		private const string DabirkhanehEmailJobId =
			"App.BackgroundJob.Jobs.Sec.DabirkhanehEmailJob.SendBirthAndEmploymentEmails";
		private const string JobLogCleanupJobId =
			"App.BackgroundJob.Jobs.System.JobLogCleanupJob.PurgeOldJobLogs";
		private const string DollarExchangeRateJobId =
			"App.BackgroundJob.Jobs.Acc.DollarExchangeRateJob.UpdateDollarExchangeRate";
		private const string SmsJobId =
			"App.BackgroundJob.Jobs.System.SmsJob.SendPendingSmsAsync";
		private const string CentrifugeDocsSmsJobId =
			"App.BackgroundJob.Jobs.Sale.CentrifugeDocsSmsJob.SendCentrifugeDocsSmsAsync";
		private const string ExitRemittanceSmsJobId =
			"App.BackgroundJob.Jobs.Sale.ExitRemittanceSmsJob.SendExitRemittanceSmsAsync";
		private const string SmsDeliveryJobId =
			"App.BackgroundJob.Jobs.System.SmsJob.CheckSmsDeliveryAsync";
		private const string ProductInspectionFailureSyncJobId =
			"App.BackgroundJob.Jobs.Qc.ProductInspectionJob.AddFailureTypesFromProductionOrderItems";
		private const string CustomerIndustryFromHtsJobId =
			"App.BackgroundJob.Jobs.Sls.CustomerJob.SyncCustomerIndustryFromHts";
		private const string EnexTodayReminderJobId =
			"App.BackgroundJob.Jobs.Hrm.EnexEntryReminderJob.SendTodayReminders";
		private const string EnexExpiredReminderJobId =
			"App.BackgroundJob.Jobs.Hrm.EnexEntryReminderJob.SendExpiredReminders";
		private const string RequirmentAdvertiseCommentEmailJobId =
			"App.BackgroundJob.Jobs.Sls.RequirmentAdvertiseCommentEmailJob.QueueCommentEmailsAsync";
		private const string ShippingPriceAlarmMorningJobId =
			"App.BackgroundJob.Jobs.Sup.InquiryPartPriceShippingAlarmJob.SendMorningAlarm";
		private const string ShippingPriceAlarmAfternoonJobId =
			"App.BackgroundJob.Jobs.Sup.InquiryPartPriceShippingAlarmJob.SendAfternoonAlarm";

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
					existingJob.DisplayName = attr.DisplayName;
					existingJob.Description = attr?.Description ?? "بدون مقدار";
				}
			}

			// فقط جاب‌هایی که وسط اجرا با کرش/ری‌استارت گیر کرده‌اند آزاد می‌شوند.
			// وضعیت Error باید بماند. اجرای بعدی فقط با «اجرا فوری» یا گزینه ContinueOnError انجام می‌شود.
			var stuckRunning = await dbContext.JobSchedules
				.Where(s => s.LastStatus == JobStatus.Running)
				.ToListAsync(cancellationToken);
			foreach (var stuck in stuckRunning)
				stuck.LastStatus = JobStatus.Idle;

			await dbContext.SaveChangesAsync(cancellationToken);

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

			// HTS Update_Sales_CustomerAddressAgencyDls: هر ۲۰ دقیقه بین ۶ تا ۲۱
			var agencyDlJob = await dbContext.JobDefinitions
				.FirstOrDefaultAsync(x => x.JobId == CustomerAddressAgencyDlJobId, cancellationToken);
			if (agencyDlJob != null
				&& !await dbContext.JobSchedules.AnyAsync(x => x.JobId == agencyDlJob.Id, cancellationToken))
			{
				dbContext.JobSchedules.Add(new JobSchedule
				{
					JobId = agencyDlJob.Id,
					IsActive = true,
					ScheduleType = ScheduleType.Interval,
					IntervalSeconds = 1200,
					NextRunTime = DateTime.Now.AddMinutes(1),
					LastStatus = JobStatus.Idle
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			// HTS BaseHamkaranTask → Import_AllCustomer_Address: هر ۱۵ دقیقه
			var customerAddressFromRahkaranJob = await dbContext.JobDefinitions
				.FirstOrDefaultAsync(x => x.JobId == CustomerAddressFromRahkaranJobId, cancellationToken);
			if (customerAddressFromRahkaranJob != null
				&& !await dbContext.JobSchedules.AnyAsync(x => x.JobId == customerAddressFromRahkaranJob.Id, cancellationToken))
			{
				dbContext.JobSchedules.Add(new JobSchedule
				{
					JobId = customerAddressFromRahkaranJob.Id,
					IsActive = true,
					ScheduleType = ScheduleType.Interval,
					IntervalSeconds = 900,
					NextRunTime = DateTime.Now.AddMinutes(2),
					LastStatus = JobStatus.Idle
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			// HTS RunQualitySystemTasks: هر ۲۰ دقیقه بین ۶ تا ۲۱
			var productInspectionJob = await dbContext.JobDefinitions
				.FirstOrDefaultAsync(x => x.JobId == ProductInspectionFailureSyncJobId, cancellationToken);
			if (productInspectionJob != null
				&& !await dbContext.JobSchedules.AnyAsync(x => x.JobId == productInspectionJob.Id, cancellationToken))
			{
				dbContext.JobSchedules.Add(new JobSchedule
				{
					JobId = productInspectionJob.Id,
					IsActive = true,
					ScheduleType = ScheduleType.Interval,
					IntervalSeconds = 1200,
					NextRunTime = DateTime.Now.AddMinutes(1),
					LastStatus = JobStatus.Idle
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			// HTS AfterSalesAndRepairSystemTask: هر ۲۰ دقیقه بین ۶ تا ۲۱
			var agencyPartCardexJob = await dbContext.JobDefinitions
				.FirstOrDefaultAsync(x => x.JobId == AgencyPartCardexJobId, cancellationToken);
			if (agencyPartCardexJob != null
				&& !await dbContext.JobSchedules.AnyAsync(x => x.JobId == agencyPartCardexJob.Id, cancellationToken))
			{
				dbContext.JobSchedules.Add(new JobSchedule
				{
					JobId = agencyPartCardexJob.Id,
					IsActive = true,
					ScheduleType = ScheduleType.Interval,
					IntervalSeconds = 1200,
					NextRunTime = DateTime.Now.AddMinutes(1),
					LastStatus = JobStatus.Idle
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			// HTS InvSystemTask (کاردکس کالا، هدر اسناد انبار، اعلان سند جدید، نقطه سفارش): هر ۱۵ دقیقه بین ۶ تا ۲۱
			var invSystemTasksJob = await dbContext.JobDefinitions
				.FirstOrDefaultAsync(x => x.JobId == InvSystemTasksJobId, cancellationToken);
			if (invSystemTasksJob != null
				&& !await dbContext.JobSchedules.AnyAsync(x => x.JobId == invSystemTasksJob.Id, cancellationToken))
			{
				dbContext.JobSchedules.Add(new JobSchedule
				{
					JobId = invSystemTasksJob.Id,
					IsActive = true,
					ScheduleType = ScheduleType.Interval,
					IntervalSeconds = 900,
					NextRunTime = DateTime.Now.AddMinutes(1),
					LastStatus = JobStatus.Idle
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			// HTS AfterSalesAndRepairSystemTask → Import_Rpr_RepairRequest_Part: هر ۲۰ دقیقه بین ۶ تا ۲۱
			var repairRequestPartJob = await dbContext.JobDefinitions
				.FirstOrDefaultAsync(x => x.JobId == RepairRequestPartImportJobId, cancellationToken);
			if (repairRequestPartJob != null
				&& !await dbContext.JobSchedules.AnyAsync(x => x.JobId == repairRequestPartJob.Id, cancellationToken))
			{
				dbContext.JobSchedules.Add(new JobSchedule
				{
					JobId = repairRequestPartJob.Id,
					IsActive = true,
					ScheduleType = ScheduleType.Interval,
					IntervalSeconds = 1200,
					NextRunTime = DateTime.Now.AddMinutes(1),
					LastStatus = JobStatus.Idle
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			// HTS Import_Cng_Repairing_UsedPart: هر ۱۵ دقیقه
			var cngRepairsUsedPartJob = await dbContext.JobDefinitions
				.FirstOrDefaultAsync(x => x.JobId == CngRepairsUsedPartImportJobId, cancellationToken);
			if (cngRepairsUsedPartJob != null
				&& !await dbContext.JobSchedules.AnyAsync(x => x.JobId == cngRepairsUsedPartJob.Id, cancellationToken))
			{
				dbContext.JobSchedules.Add(new JobSchedule
				{
					JobId = cngRepairsUsedPartJob.Id,
					IsActive = true,
					ScheduleType = ScheduleType.Interval,
					IntervalSeconds = 900,
					NextRunTime = DateTime.Now.AddMinutes(1),
					LastStatus = JobStatus.Idle
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			// HTS CngSystemTask CheckExpiredItems: هر ۲۰ دقیقه
			var cngMaintenanceExpireJob = await dbContext.JobDefinitions
				.FirstOrDefaultAsync(x => x.JobId == CngMaintenanceContractExpireJobId, cancellationToken);
			if (cngMaintenanceExpireJob != null
				&& !await dbContext.JobSchedules.AnyAsync(x => x.JobId == cngMaintenanceExpireJob.Id, cancellationToken))
			{
				dbContext.JobSchedules.Add(new JobSchedule
				{
					JobId = cngMaintenanceExpireJob.Id,
					IsActive = true,
					ScheduleType = ScheduleType.Interval,
					IntervalSeconds = 1200,
					NextRunTime = DateTime.Now.AddMinutes(1),
					LastStatus = JobStatus.Idle
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			// HTS DabirkhanehTask: هر ۳۰ دقیقه، ساعت ۲ تا ۱۷ (فیلتر ساعت داخل خود جاب)
			var dabirkhanehJob = await dbContext.JobDefinitions
				.FirstOrDefaultAsync(x => x.JobId == DabirkhanehEmailJobId, cancellationToken);
			if (dabirkhanehJob != null
				&& !await dbContext.JobSchedules.AnyAsync(x => x.JobId == dabirkhanehJob.Id, cancellationToken))
			{
				dbContext.JobSchedules.Add(new JobSchedule
				{
					JobId = dabirkhanehJob.Id,
					IsActive = true,
					ScheduleType = ScheduleType.Interval,
					IntervalSeconds = 1800,
					NextRunTime = DateTime.Now.AddMinutes(1),
					LastStatus = JobStatus.Idle
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			// لاگ جاب‌ها بعد از ۳ روز حذف می‌شوند. اجرای اول کمی بعد از استقرار است، بعد هر روز ساعت ۳.
			var jobLogCleanupJob = await dbContext.JobDefinitions
				.FirstOrDefaultAsync(x => x.JobId == JobLogCleanupJobId, cancellationToken);
			if (jobLogCleanupJob != null
				&& !await dbContext.JobSchedules.AnyAsync(x => x.JobId == jobLogCleanupJob.Id, cancellationToken))
			{
				dbContext.JobSchedules.Add(new JobSchedule
				{
					JobId = jobLogCleanupJob.Id,
					IsActive = true,
					ScheduleType = ScheduleType.Daily,
					DailyIntervalDays = 1,
					DailyTime = TimeSpan.FromHours(3),
					IntervalSeconds = 86400,
					NextRunTime = DateTime.Now.AddMinutes(1),
					LastStatus = JobStatus.Idle
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			// صف پیامک (system.SmsMessage): هر ۶۰ ثانیه
			var smsJob = await dbContext.JobDefinitions
				.FirstOrDefaultAsync(x => x.JobId == SmsJobId, cancellationToken);
			if (smsJob != null
				&& !await dbContext.JobSchedules.AnyAsync(x => x.JobId == smsJob.Id, cancellationToken))
			{
				dbContext.JobSchedules.Add(new JobSchedule
				{
					JobId = smsJob.Id,
					IsActive = true,
					ScheduleType = ScheduleType.Interval,
					IntervalSeconds = 60,
					NextRunTime = DateTime.Now.AddMinutes(1),
					LastStatus = JobStatus.Idle
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			// پیامک لینک مدارک سانتریفیوژ (جایگزین کار SQL راهکاران): هر ۳۰ دقیقه؛ تا تنظیم Sms:CentrifugeDocs:Enabled=true چیزی نمی‌فرستد.
			var centrifugeDocsJob = await dbContext.JobDefinitions
				.FirstOrDefaultAsync(x => x.JobId == CentrifugeDocsSmsJobId, cancellationToken);
			if (centrifugeDocsJob != null
				&& !await dbContext.JobSchedules.AnyAsync(x => x.JobId == centrifugeDocsJob.Id, cancellationToken))
			{
				dbContext.JobSchedules.Add(new JobSchedule
				{
					JobId = centrifugeDocsJob.Id,
					IsActive = true,
					ScheduleType = ScheduleType.Interval,
					IntervalSeconds = 1800,
					NextRunTime = DateTime.Now.AddMinutes(5),
					LastStatus = JobStatus.Idle
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			// پیامک خروج از کارخانه (جایگزین کار SQL راهکاران نوع 2746): هر روز ساعت ۰۸:۰۰ مثل کار قدیمی.
			var exitRemittanceJob = await dbContext.JobDefinitions
				.FirstOrDefaultAsync(x => x.JobId == ExitRemittanceSmsJobId, cancellationToken);
			if (exitRemittanceJob != null
				&& !await dbContext.JobSchedules.AnyAsync(x => x.JobId == exitRemittanceJob.Id, cancellationToken))
			{
				var exitDailyTime = new TimeSpan(8, 0, 0);
				var exitNextRun = DateTime.Now.Date.Add(exitDailyTime);
				if (exitNextRun <= DateTime.Now)
					exitNextRun = exitNextRun.AddDays(1);

				dbContext.JobSchedules.Add(new JobSchedule
				{
					JobId = exitRemittanceJob.Id,
					IsActive = true,
					ScheduleType = ScheduleType.Daily,
					DailyIntervalDays = 1,
					DailyTime = exitDailyTime,
					IntervalSeconds = 86400,
					NextRunTime = exitNextRun,
					LastStatus = JobStatus.Idle
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			// استعلام دلیوری پیامک‌ها: هر ۵ دقیقه
			var smsDeliveryJob = await dbContext.JobDefinitions
				.FirstOrDefaultAsync(x => x.JobId == SmsDeliveryJobId, cancellationToken);
			if (smsDeliveryJob != null
				&& !await dbContext.JobSchedules.AnyAsync(x => x.JobId == smsDeliveryJob.Id, cancellationToken))
			{
				dbContext.JobSchedules.Add(new JobSchedule
				{
					JobId = smsDeliveryJob.Id,
					IsActive = true,
					ScheduleType = ScheduleType.Interval,
					IntervalSeconds = 300,
					NextRunTime = DateTime.Now.AddMinutes(2),
					LastStatus = JobStatus.Idle
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			// HTS UpdateExchangeRateArchive: هر روز ساعت ۱۱:۱۵
			var dollarRateJob = await dbContext.JobDefinitions
				.FirstOrDefaultAsync(x => x.JobId == DollarExchangeRateJobId, cancellationToken);
			if (dollarRateJob != null
				&& !await dbContext.JobSchedules.AnyAsync(x => x.JobId == dollarRateJob.Id, cancellationToken))
			{
				var dailyTime = new TimeSpan(11, 15, 0);
				var nextRun = DateTime.Now.Date.Add(dailyTime);
				if (nextRun <= DateTime.Now)
					nextRun = nextRun.AddDays(1);

				dbContext.JobSchedules.Add(new JobSchedule
				{
					JobId = dollarRateJob.Id,
					IsActive = true,
					ScheduleType = ScheduleType.Daily,
					DailyIntervalDays = 1,
					DailyTime = dailyTime,
					IntervalSeconds = 86400,
					NextRunTime = nextRun,
					LastStatus = JobStatus.Idle
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			// نوع صنعت مشتری از HTS (linked server TMS): جدا از جاب دقیقه‌ای مشتری، هر ساعت
			var customerIndustryJob = await dbContext.JobDefinitions
				.FirstOrDefaultAsync(x => x.JobId == CustomerIndustryFromHtsJobId, cancellationToken);
			if (customerIndustryJob != null
				&& !await dbContext.JobSchedules.AnyAsync(x => x.JobId == customerIndustryJob.Id, cancellationToken))
			{
				dbContext.JobSchedules.Add(new JobSchedule
				{
					JobId = customerIndustryJob.Id,
					IsActive = true,
					ScheduleType = ScheduleType.Interval,
					IntervalSeconds = 3600,
					NextRunTime = DateTime.Now.AddMinutes(2),
					LastStatus = JobStatus.Idle
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}

			// HTS SendTodayEnexEntryNotifications و SendExpiredEnexEntryNotifications: هر روز ساعت ۱۰:۲۵
			await EnsureDailyAsync(dbContext, EnexTodayReminderJobId, new TimeSpan(10, 25, 0), cancellationToken);
			await EnsureDailyAsync(dbContext, EnexExpiredReminderJobId, new TimeSpan(10, 25, 0), cancellationToken);

			// HTS SentShippingActualPriceChangeAlarm: هر روز ۱۱:۵۰ و ۱۵:۵۰
			await EnsureDailyAsync(dbContext, ShippingPriceAlarmMorningJobId, new TimeSpan(11, 50, 0), cancellationToken);
			await EnsureDailyAsync(dbContext, ShippingPriceAlarmAfternoonJobId, new TimeSpan(15, 50, 0), cancellationToken);

			// جایگزین کار SQL راهکاران HTS_SaleRequirmentAdvertise_NewComment و تریگر روی SYS3.Note: هر ۵ دقیقه
			var requirmentAdvertiseCommentEmailJob = await dbContext.JobDefinitions
				.FirstOrDefaultAsync(x => x.JobId == RequirmentAdvertiseCommentEmailJobId, cancellationToken);
			if (requirmentAdvertiseCommentEmailJob != null
				&& !await dbContext.JobSchedules.AnyAsync(x => x.JobId == requirmentAdvertiseCommentEmailJob.Id, cancellationToken))
			{
				dbContext.JobSchedules.Add(new JobSchedule
				{
					JobId = requirmentAdvertiseCommentEmailJob.Id,
					IsActive = true,
					ScheduleType = ScheduleType.Interval,
					IntervalSeconds = 300,
					NextRunTime = DateTime.Now.AddMinutes(2),
					LastStatus = JobStatus.Idle
				});
				await dbContext.SaveChangesAsync(cancellationToken);
			}
		}

		private static async Task EnsureDailyAsync(ApplicationDbContext dbContext, string jobId, TimeSpan dailyTime, CancellationToken cancellationToken)
		{
			var job = await dbContext.JobDefinitions.FirstOrDefaultAsync(x => x.JobId == jobId, cancellationToken);
			if (job == null || await dbContext.JobSchedules.AnyAsync(x => x.JobId == job.Id, cancellationToken))
				return;

			var nextRun = DateTime.Now.Date.Add(dailyTime);
			if (nextRun <= DateTime.Now)
				nextRun = nextRun.AddDays(1);

			dbContext.JobSchedules.Add(new JobSchedule
			{
				JobId = job.Id,
				IsActive = true,
				ScheduleType = ScheduleType.Daily,
				DailyIntervalDays = 1,
				DailyTime = dailyTime,
				IntervalSeconds = 86400,
				NextRunTime = nextRun,
				LastStatus = JobStatus.Idle
			});
			await dbContext.SaveChangesAsync(cancellationToken);
		}

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
