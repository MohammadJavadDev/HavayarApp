using Data;
using Entities.Base.Job;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace App.BackgroundJob.Services
{
    public class TestJobSeeder : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public TestJobSeeder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // Check if test jobs already exist
                var existingTestJobs = await dbContext.JobDefinitions
                    .Where(j => j.DisplayName.Contains("سرویس تستی"))
                    .ToListAsync(cancellationToken);
                
                if (existingTestJobs.Any())
                {
                    Console.WriteLine("Test jobs already exist, skipping seeder.");
                    return;
                }

                // Add test job definitions
                var testJobs = new[]
                {
                    new JobDefinition
                    {
                        JobId = "TestJob-RunTestJob",
                        DisplayName = "سرویس تستی - هر دقیقه یکبار",
                        Description = "یک سرویس تستی که هر دقیقه یکبار اجرا می‌شود",
                        AssemblyName = "App.BackgroundJob",
                        ClassType = "App.BackgroundJob.Jobs.Test.TestJob",
                        MethodName = "RunTestJob"
                    },
                    new JobDefinition
                    {
                        JobId = "TestJob-RunDailyTestJob",
                        DisplayName = "سرویس تستی روزانه",
                        Description = "یک سرویس تستی که روزانه اجرا می‌شود",
                        AssemblyName = "App.BackgroundJob",
                        ClassType = "App.BackgroundJob.Jobs.Test.TestJob",
                        MethodName = "RunDailyTestJob"
                    },
                    new JobDefinition
                    {
                        JobId = "TestJob-RunHourlyTestJob",
                        DisplayName = "سرویس تستی ساعتی",
                        Description = "یک سرویس تستی که هر ساعت اجرا می‌شود",
                        AssemblyName = "App.BackgroundJob",
                        ClassType = "App.BackgroundJob.Jobs.Test.TestJob",
                        MethodName = "RunHourlyTestJob"
                    }
                };

                await dbContext.JobDefinitions.AddRangeAsync(testJobs, cancellationToken);

                // Add corresponding schedules
                var testSchedules = new[]
                {
                    new JobSchedule
                    {
                        JobId = "TestJob-RunTestJob",
                        IsActive = true,
                        IntervalSeconds = 60, // Every minute
                        NextRunTime = DateTime.Now.AddMinutes(1),
                        LastStatus = JobStatus.Idle
                    },
                    new JobSchedule
                    {
                        JobId = "TestJob-RunDailyTestJob",
                        IsActive = true,
                        IntervalSeconds = 86400, // Daily (24 hours)
                        NextRunTime = DateTime.Now.AddDays(1),
                        LastStatus = JobStatus.Idle
                    },
                    new JobSchedule
                    {
                        JobId = "TestJob-RunHourlyTestJob",
                        IsActive = true,
                        IntervalSeconds = 3600, // Hourly
                        NextRunTime = DateTime.Now.AddHours(1),
                        LastStatus = JobStatus.Idle
                    }
                };

                await dbContext.JobSchedules.AddRangeAsync(testSchedules, cancellationToken);

                await dbContext.SaveChangesAsync(cancellationToken);
                
                Console.WriteLine("Test jobs seeded successfully!");
                Console.WriteLine($"Added {testJobs.Length} job definitions and {testSchedules.Length} schedules.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}