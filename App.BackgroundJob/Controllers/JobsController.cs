using Data;
using Entities.Base.Job;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

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
			var schedule = allSchedules.FirstOrDefault(s => s.JobId == def.JobId);
			
			return new JobViewModel
			{
				ScheduleId = schedule?.Id ?? 0,
				JobId = def.JobId,
				DisplayName = def.DisplayName,
				Description = def.Description,
				IsActive = schedule?.IsActive ?? false,
				IntervalSeconds = schedule?.IntervalSeconds ?? 0,
				LastStatus = schedule?.LastStatus.ToString() ?? "NoSchedule",
				LastRunTime = schedule?.LastRunTime,
				NextRunTime = schedule?.NextRunTime,
				HasSchedule = schedule != null
			};
		}).ToList();

		// Debug information
		ViewBag.DebugInfo = $"Found {jobs.Count} job definitions. Schedules: {allSchedules.Count}, Definitions: {allDefinitions.Count}";

		return View(jobs);
	}

	// متد ایجاد زمان‌بندی برای جاب‌های بدون زمان‌بندی
	[HttpPost]
	public async Task<IActionResult> CreateSchedule(string jobId, bool isActive, int interval)
	{
		// بررسی وجود جاب تعریف
		var jobDefinition = await _db.JobDefinitions.FirstOrDefaultAsync(j => j.JobId == jobId);
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
			NextRunTime = isActive ? DateTime.Now.AddSeconds(interval) : (DateTime?)null,
			LastStatus = JobStatus.Idle
		};

		_db.JobSchedules.Add(newSchedule);
		await _db.SaveChangesAsync();

		return Ok(new { success = true, message = "زمان‌بندی با موفقیت ایجاد شد.", scheduleId = newSchedule.Id });
	}

	// متد ذخیره تغییرات (AJAX)
	[HttpPost]
	public async Task<IActionResult> UpdateSettings(int scheduleId, bool isActive, int interval)
	{
		var schedule = await _db.JobSchedules.FindAsync(scheduleId);
		if (schedule == null) return NotFound();

		schedule.IsActive = isActive;
		schedule.IntervalSeconds = interval;

		// اگر فعال شد، زمان اجرای بعدی را ست کن
		if (isActive && schedule.NextRunTime < DateTime.Now)
		{
			schedule.NextRunTime = DateTime.Now.AddSeconds(interval);
		}

		await _db.SaveChangesAsync();
		return Ok(new { success = true, message = "تنظیمات با موفقیت ذخیره شد." });
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
}