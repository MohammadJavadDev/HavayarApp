using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Base.Job
{
	public class JobDefinition:BaseEntity
	{
		public long Id { get; set; }
		public string JobId { get; set; }  
		public string DisplayName { get; set; }
		public string Description { get; set; }
		public string AssemblyName { get; set; }
		public string ClassType { get; set; }  
		public string MethodName { get; set; }
	}
	public class JobSchedule
	{
		public int Id { get; set; }
		public string JobId { get; set; } // FK به JobDefinition
		public bool IsActive { get; set; }

		// تنظیمات زمان‌بندی (مثلاً اینتروال ساده یا کرون)
		public int IntervalSeconds { get; set; }
		public DateTime? NextRunTime { get; set; }
		public DateTime? LastRunTime { get; set; }

		public JobStatus LastStatus { get; set; } // Running, Idle, Failed

		// تنظیمات جدید برای زمان‌بندی پیشرفته
		public ScheduleType ScheduleType { get; set; } = ScheduleType.Interval;
		public int DailyIntervalDays { get; set; } = 1; // برای زمان‌بندی روزانه با فاصله (مثلا هر 2 روز یکبار)
		public string WeeklyDays { get; set; } = ""; // برای زمان‌بندی هفتگی - روزهای هفته به صورت CSV (مثلا "Saturday,Sunday")
		public TimeSpan? DailyTime { get; set; } // زمان اجرا برای زمان‌بندی روزانه
		public int HourlyMinute { get; set; } = 0; // دقیقه برای زمان‌بندی ساعتی
	}

	public enum ScheduleType
	{
		Interval,    // زمان‌بندی بر اساس فاصله زمانی ساده
		Daily,       // زمان‌بندی روزانه با فاصله قابل تنظیم
		Weekly,      // زمان‌بندی هفتگی با انتخاب روزهای هفته
		Hourly       // زمان‌بندی ساعتی
	}
	public class JobHistory
	{
		public long Id { get; set; }
		public int ScheduleId { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime? EndTime { get; set; }
		public bool IsSuccess { get; set; }
		public string LogOutput { get; set; } // ذخیره لاگ‌های متنی
		public string ErrorMessage { get; set; } = "";
	}
	public enum JobStatus { Idle, Running, Waiting , Error }
}
