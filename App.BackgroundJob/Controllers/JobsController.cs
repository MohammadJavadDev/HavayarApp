using Data;
using Entities.Base.Job;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace App.BackgroundJob.Controllers
{
	[Authorize(Roles = "admin")]
	public class JobsController : Controller
	{
		private readonly ApplicationDbContext _db;

		public JobsController(ApplicationDbContext db)
		{
			_db = db;
		}

		// نمایش لیست جاب‌ها
		public async Task<IActionResult> Index()
		{
			// دریافت همه تعریف‌های جاب
			var allDefinitions = await _db.JobDefinitions.ToListAsync();

			// دریافت همه زمان‌بندی‌های جاب
			var allSchedules = await _db.JobSchedules.ToListAsync();

			// ایجاد لیست کامل جاب‌ها (حتی آنهایی که زمان‌بندی ندارند)
			var jobs = allDefinitions.Select(def =>
			{
				var schedule = allSchedules.FirstOrDefault(s => s.JobId == def.Id);

				return new JobViewModel
				{
					Id = def.Id,
					ScheduleId = schedule?.Id ?? 0,
					JobId = def.JobId,
					DisplayName = def.DisplayName,
					Description = def.Description,
					IsActive = schedule?.IsActive ?? false,
					IntervalSeconds = schedule?.IntervalSeconds ?? 0,
					LastStatus = schedule?.LastStatus.ToString() ?? "NoSchedule",
					LastRunTime = schedule?.LastRunTime,
					NextRunTime = schedule?.NextRunTime,
					HasSchedule = schedule != null,
					ScheduleType = schedule?.ScheduleType ?? ScheduleType.Interval,
					DailyIntervalDays = schedule?.DailyIntervalDays ?? 1,
					WeeklyDays = schedule?.WeeklyDays ?? "",
					DailyTime = schedule?.DailyTime ?? TimeSpan.MinValue,
					HourlyMinute = schedule?.HourlyMinute ?? 0
				};
			}).ToList();

			// Debug information
			ViewBag.DebugInfo = $"Found {jobs.Count} job definitions. Schedules: {allSchedules.Count}, Definitions: {allDefinitions.Count}";

			return View(jobs);
		}

		// متد ایجاد زمان‌بندی برای جاب‌های بدون زمان‌بندی
		[HttpPost]
		public async Task<IActionResult> CreateSchedule(long jobId, bool isActive, int interval, ScheduleType scheduleType = ScheduleType.Interval, int dailyIntervalDays = 1, string weeklyDays = "", TimeSpan? dailyTime = null, int hourlyMinute = 0)
		{
			// بررسی وجود جاب تعریف
			var jobDefinition = await _db.JobDefinitions.FirstOrDefaultAsync(j => j.Id == jobId);
			if (jobDefinition == null) return NotFound("Job definition not found");

			// بررسی وجود زمان‌بندی
			var existingSchedule = await _db.JobSchedules.FirstOrDefaultAsync(s => s.JobId == jobId);
			if (existingSchedule != null) return BadRequest("Schedule already exists");

			// ایجاد زمان‌بندی جدید
			var newSchedule = new JobSchedule
			{
				JobId = jobId,
				IsActive = isActive,
				IntervalSeconds = interval,
				ScheduleType = scheduleType,
				DailyIntervalDays = dailyIntervalDays,
				WeeklyDays = weeklyDays,
				DailyTime = dailyTime,
				HourlyMinute = hourlyMinute,
				NextRunTime = isActive ? CalculateNextRunTimeForNewSchedule(scheduleType, interval, dailyIntervalDays, weeklyDays, dailyTime, hourlyMinute) : (DateTime?)null,
				LastStatus = JobStatus.Idle
			};

			_db.JobSchedules.Add(newSchedule);
			await _db.SaveChangesAsync();

			return Ok(new { success = true, message = "زمان‌بندی با موفقیت ایجاد شد.", scheduleId = newSchedule.Id });
		}

		// متد ذخیره تغییرات (AJAX)
		[HttpPost]
		public async Task<IActionResult> UpdateSettings(int scheduleId, bool isActive, int interval, ScheduleType scheduleType, int dailyIntervalDays, string weeklyDays, TimeSpan? dailyTime, int hourlyMinute)
		{
			var schedule = await _db.JobSchedules.FindAsync(scheduleId);
			if (schedule == null) return NotFound();

			schedule.IsActive = isActive;
			schedule.IntervalSeconds = interval;
			schedule.ScheduleType = scheduleType;
			schedule.DailyIntervalDays = dailyIntervalDays;
			schedule.WeeklyDays = weeklyDays;
			schedule.DailyTime = dailyTime;
			schedule.HourlyMinute = hourlyMinute;

			// اگر فعال شد، زمان اجرای بعدی را ست کن
			if (isActive)
			{
				schedule.NextRunTime = CalculateNextRunTime(schedule);
			}

			await _db.SaveChangesAsync();
			return Ok(new { success = true, message = "تنظیمات با موفقیت ذخیره شد." });
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

		// متد اجرا فوری سرویس (AJAX)
		[HttpPost]
		public async Task<IActionResult> RunNow(int scheduleId)
		{
			var schedule = await _db.JobSchedules.FindAsync(scheduleId);
			if (schedule == null) return NotFound();

			// تنظیم زمان اجرای بعدی برای فوراً اجرا شدن
			schedule.NextRunTime = DateTime.Now.AddSeconds(5); // اجرا در 5 ثانیه آینده
			schedule.LastStatus = JobStatus.Waiting;
			await _db.SaveChangesAsync();
			return Ok(new { success = true, message = "سرویس با موفقیت برای اجرا فوری برنامه‌ریزی شد." });
		}

		// دریافت تاریخچه لاگ‌ها (AJAX برای Modal)
		[HttpGet]
		public async Task<IActionResult> GetHistory(int scheduleId)
		{
			var logs = await _db.JobHistories
			    .Where(h => h.ScheduleId == scheduleId)
			    .OrderByDescending(h => h.StartTime)
			    .Take(20) // گرفتن ۲۰ لاگ آخر
			    .Select(h => new JobHistoryViewModel
			    {
				    StartTime = h.StartTime,
				    EndTime = h.EndTime,
				    IsSuccess = h.IsSuccess,
				    Message = h.IsSuccess ? "Success" : h.ErrorMessage ?? h.LogOutput
			    })
			    .ToListAsync();

			return Json(logs);
		}

		// متد کمکی برای محاسبه زمان اجرای بعدی برای زمان‌بندی جدید
		private DateTime CalculateNextRunTimeForNewSchedule(ScheduleType scheduleType, int intervalSeconds, int dailyIntervalDays, string weeklyDays, TimeSpan? dailyTime, int hourlyMinute)
		{
			var now = DateTime.Now;

			switch (scheduleType)
			{
				case ScheduleType.Interval:
					return now.AddSeconds(intervalSeconds);

				case ScheduleType.Daily:
					var nextDailyTime = dailyTime ?? TimeSpan.Zero;
					var nextDate = now.Date.AddDays(dailyIntervalDays);

					if (now.TimeOfDay >= nextDailyTime)
					{
						nextDate = nextDate.AddDays(dailyIntervalDays);
					}

					return nextDate.Add(nextDailyTime);

				case ScheduleType.Weekly:
					if (string.IsNullOrEmpty(weeklyDays))
						return now.AddDays(1);

					var days = weeklyDays.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					var nextWeeklyDate = now.Date;

					for (int i = 1; i <= 7; i++)
					{
						var testDate = nextWeeklyDate.AddDays(i);
						var dayName = testDate.DayOfWeek.ToString();

						if (days.Contains(dayName))
						{
							var time = dailyTime ?? TimeSpan.Zero;
							return testDate.Add(time);
						}
					}

					return now.AddDays(7);

				case ScheduleType.Hourly:
					var nextHour = now.AddHours(1);
					return new DateTime(nextHour.Year, nextHour.Month, nextHour.Day, nextHour.Hour, hourlyMinute, 0);

				default:
					return now.AddSeconds(intervalSeconds);
			}
		}
	}
}