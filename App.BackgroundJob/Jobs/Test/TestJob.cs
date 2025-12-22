using Common.Attributes;
using Services.Job;

namespace App.BackgroundJob.Jobs.Test
{
	public class TestJob
	{
		[JobHandler("سرویس تستی - هر دقیقه یکبار")]
		public async Task RunTestJob()
		{
			Console.WriteLine("Test job is running...");
			Console.WriteLine("Current time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			await Task.Delay(1000); // شبیه‌سازی کار
		}

		[JobHandler("سرویس تستی روزانه")]
		public async Task RunDailyTestJob()
		{
			Console.WriteLine("Daily test job is running...");
			Console.WriteLine("This should run once per day");
			await Task.Delay(2000);
		}

		[JobHandler("سرویس تستی ساعتی")]
		public async Task RunHourlyTestJob()
		{
			Console.WriteLine("Hourly test job is running...");
			Console.WriteLine("This should run once per hour");
			await Task.Delay(1500);
		}
	}
}