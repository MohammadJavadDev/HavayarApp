using Common.Attributes;
using Common.Utilities;
using Data;
using Entities.Base.Job;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Data;

namespace App.BackgroundJob.Jobs.System;

/// <summary>
/// لاگ خط‌به‌خط در system.JobLog و متن اجرای هر نوبت در system.JobHistory.LogOutput است.
/// هر اجرایی که بیش از سه روز از شروعش گذشته حذف می‌شود، حتی اگر وضعیتش روی «در حال اجرا» گیر کرده باشد.
/// زمان‌بندی همان اجرا از حالت Running به Idle برمی‌گردد تا جاب دوباره انتخاب شود.
/// </summary>
public sealed class JobLogCleanupJob(ApplicationDbContext db)
{
	public const int RetentionDays = 3;
	private const int HistoryChunkSize = 200;
	private const int LogDeleteBatchSize = 8000;

	[JobHandler(
		"پاک‌سازی لاگ جاب‌های قدیمی",
		"لاگ و تاریخچه اجرای جاب‌هایی که بیش از ۳ روز از شروع‌شان گذشته را حذف می‌کند، شامل اجراهایی که در وضعیت در حال اجرا گیر کرده‌اند.")]
	public async Task PurgeOldJobLogs(IJobLogger? jobLogger = null, CancellationToken cancellationToken = default)
	{
		var cutoff = DateTime.Now.AddDays(-RetentionDays);
		db.Database.SetCommandTimeout(300);

		await LogAsync(
			jobLogger,
			$"شروع پاک‌سازی لاگ جاب‌ها؛ نگهداری {RetentionDays} روز (قبل از {cutoff.ToShamsiDateTime()})",
			cancellationToken);

		var oldHistoryIds = await db.JobHistories
			.AsNoTracking()
			.Where(h => h.StartTime < cutoff)
			.Select(h => h.Id)
			.ToListAsync(cancellationToken);

		long deletedLogs = 0;
		long deletedHistories = 0;

		if (oldHistoryIds.Count == 0)
		{
			await LogAsync(jobLogger, "لاگ قدیمی برای حذف پیدا نشد.", cancellationToken);
		}
		else
		{
			await LogAsync(
				jobLogger,
				$"{oldHistoryIds.Count:N0} اجرای قدیمی برای حذف انتخاب شد.",
				cancellationToken);

			var nextProgressLog = 200_000L;

			foreach (var chunk in oldHistoryIds.Chunk(HistoryChunkSize))
			{
				cancellationToken.ThrowIfCancellationRequested();
				var ids = chunk.ToArray();

				while (true)
				{
					cancellationToken.ThrowIfCancellationRequested();
					var deleted = await DeleteBatchAsync(
						$"DELETE TOP ({LogDeleteBatchSize}) FROM [system].[JobLog] WHERE [JobHistoryId] IN ({{0}}); SET @deleted = @@ROWCOUNT;",
						ids,
						cancellationToken);
					if (deleted <= 0)
						break;

					deletedLogs += deleted;
					if (deletedLogs >= nextProgressLog)
					{
						await LogAsync(jobLogger, $"تا اینجا {deletedLogs:N0} ردیف لاگ حذف شد.", cancellationToken);
						nextProgressLog += 200_000L;
					}
				}

				deletedHistories += await DeleteBatchAsync(
					"DELETE FROM [system].[JobHistory] WHERE [Id] IN ({0}); SET @deleted = @@ROWCOUNT;",
					ids,
					cancellationToken);
			}

			await LogAsync(
				jobLogger,
				$"پاک‌سازی تمام شد. لاگ حذف‌شده: {deletedLogs:N0}، تاریخچه حذف‌شده: {deletedHistories:N0}.",
				cancellationToken);
		}

		var releasedSchedules = await ReleaseStuckRunningSchedulesAsync(cutoff, cancellationToken);
		if (releasedSchedules > 0)
		{
			await LogAsync(
				jobLogger,
				$"{releasedSchedules:N0} زمان‌بندی که بیش از {RetentionDays} روز در وضعیت اجرا مانده بود به حالت آماده برگشت.",
				cancellationToken);
		}
	}

	private async Task<int> ReleaseStuckRunningSchedulesAsync(DateTime cutoff, CancellationToken cancellationToken)
	{
		var stuckSchedules = await db.JobSchedules
			.Where(s => s.LastStatus == JobStatus.Running && s.LastRunTime != null && s.LastRunTime < cutoff)
			.ToListAsync(cancellationToken);

		if (stuckSchedules.Count == 0)
			return 0;

		foreach (var schedule in stuckSchedules)
			schedule.LastStatus = JobStatus.Idle;

		await db.SaveChangesAsync(cancellationToken);
		return stuckSchedules.Count;
	}

	private async Task<int> DeleteBatchAsync(string sqlTemplate, long[] ids, CancellationToken cancellationToken)
	{
		var parameters = new object[ids.Length + 1];
		var names = new string[ids.Length];
		for (var i = 0; i < ids.Length; i++)
		{
			names[i] = "@p" + i;
			parameters[i] = new SqlParameter(names[i], SqlDbType.BigInt) { Value = ids[i] };
		}

		var deleted = new SqlParameter("@deleted", SqlDbType.Int) { Direction = ParameterDirection.Output };
		parameters[ids.Length] = deleted;

		var sql = string.Format(sqlTemplate, string.Join(", ", names));
		var affected = await db.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);

		if (deleted.Value is int count)
			return count;

		return affected > 0 ? affected : 0;
	}

	private static Task LogAsync(IJobLogger? jobLogger, string message, CancellationToken cancellationToken)
	{
		if (jobLogger == null)
			return Task.CompletedTask;

		return jobLogger.LogInfoAsync(message, cancellationToken);
	}
}
