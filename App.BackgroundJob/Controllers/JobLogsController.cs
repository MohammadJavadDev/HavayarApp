 
using Data;
using Entities.Base.Job;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace App.BackgroundJob.Controllers
{
    public class JobLogsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IJobLogger _jobLogger;
        private readonly ILogger<JobLogsController> _logger;

        public JobLogsController(ApplicationDbContext db, IJobLogger jobLogger, ILogger<JobLogsController> logger)
        {
            _db = db;
            _jobLogger = jobLogger;
            _logger = logger;
        }

        // نمایش لیست تاریخچه جاب‌ها
        public async Task<IActionResult> Index(int? scheduleId)
        {
            try
            {
                var schedules = await _db.JobSchedules
                    .Include(s => s.JobDefinition)
                    .ToListAsync();

                var selectedSchedule = schedules.FirstOrDefault(s => s.Id == scheduleId) ?? schedules.FirstOrDefault();

                ViewBag.Schedules = schedules;
                ViewBag.SelectedScheduleId = selectedSchedule?.Id ?? 0;

                if (selectedSchedule != null)
                {
                    var histories = await _db.JobHistories
                        .Where(h => h.ScheduleId == selectedSchedule.Id)
                        .OrderByDescending(h => h.StartTime)
                        .ToListAsync();

                    return View(histories);
                }

                return View(new List<JobHistory>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobLogs Index action");
                return View(new List<JobHistory>());
            }
        }

        // نمایش جزئیات لاگ‌های یک تاریخچه خاص
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                var history = await _db.JobHistories
                    .Include(h => h.JobSchedule)
                    .ThenInclude(s => s.JobDefinition)
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (history == null)
                {
                    return NotFound();
                }

                // Ensure JobSchedule is loaded if not already
                if (history.JobSchedule == null && history.ScheduleId > 0)
                {
                    history.JobSchedule = await _db.JobSchedules
                        .Include(s => s.JobDefinition)
                        .FirstOrDefaultAsync(s => s.Id == history.ScheduleId);
                }

                // دریافت لاگ‌های جزئی از طریق JobLogger
                var logs = await _jobLogger.GetLogsByJobHistoryIdAsync(id);

                ViewBag.JobHistory = history;
                return View(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobLogs Details action");
                return RedirectToAction("Index");
            }
        }

        // دریافت لاگ‌های جزئی به صورت AJAX
        [HttpGet]
        public async Task<IActionResult> GetDetailedLogs(long historyId)
        {
            try
            {
                var logs = await _jobLogger.GetLogsByJobHistoryIdAsync(historyId);
                return Json(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDetailedLogs action");
                return Json(new { error = "Failed to retrieve logs" });
            }
        }

        // نمایش لاگ‌های یک جاب خاص
        public async Task<IActionResult> JobLogs(int scheduleId)
        {
            try
            {
                var schedule = await _db.JobSchedules
                    .Include(s => s.JobDefinition)
                    .FirstOrDefaultAsync(s => s.Id == scheduleId);

                if (schedule == null)
                {
                    return NotFound();
                }

                var logs = await _jobLogger.GetLogsByJobScheduleIdAsync(scheduleId);

                ViewBag.Schedule = schedule;
                return View(logs); // Use the dedicated JobLogs view
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobLogs action");
                return RedirectToAction("Index");
            }
        }

        // جستجوی لاگ‌ها
        [HttpGet]
        public async Task<IActionResult> Search(string query, int? scheduleId, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var schedules = await _db.JobSchedules
                    .Include(s => s.JobDefinition)
                    .ToListAsync();

                ViewBag.Schedules = schedules;
                ViewData["query"] = query;
                ViewData["scheduleId"] = scheduleId;
                ViewData["fromDate"] = fromDate;
                ViewData["toDate"] = toDate;

                var baseQuery = _db.JobLogs.AsQueryable();

                if (scheduleId.HasValue)
                {
                    baseQuery = baseQuery.Where(l => l.JobHistory.ScheduleId == scheduleId.Value);
                }

                if (fromDate.HasValue)
                {
                    baseQuery = baseQuery.Where(l => l.Timestamp >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    baseQuery = baseQuery.Where(l => l.Timestamp <= toDate.Value);
                }

                if (!string.IsNullOrEmpty(query))
                {
                    var searchTerm = query.ToLower();
                    baseQuery = baseQuery.Where(l => 
                        (l.Message != null && l.Message.Contains(searchTerm)) ||
                        (l.Details != null && l.Details.Contains(searchTerm)) ||
                        (l.ExceptionType != null && l.ExceptionType.Contains(searchTerm)) ||
                        (l.StackTrace != null && l.StackTrace.Contains(searchTerm)));
                }

                var results = await baseQuery
                    .Include(l => l.JobHistory)
                        .ThenInclude(h => h.JobSchedule)
                            .ThenInclude(s => s.JobDefinition)
                    .OrderByDescending(l => l.Timestamp)
                    .Take(100)
                    .ToListAsync();

                return View("Search", results);
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error in JobLogs Search action");
                return View("Search", new List<Entities.Base.Job.JobLog>());
            }
        }
    }
}