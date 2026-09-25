using Common.Attributes;
using Data.Contracts;
using Entities.App.Cng;
using Microsoft.EntityFrameworkCore;
using Services.FileServices;
using Services.Job;

namespace App.BackgroundJob.Jobs.Cng
{
	/// <summary>
	/// یک‌باره: آپلود فایل‌های گزارش کار CNG از مسیر UNC قدیمی (WorkReportFileAddress)
	/// از طریق <see cref="IFileService"/> و ست کردن <see cref="WorkReport.FileId"/>.
	/// <para>
	/// ثبت خودکار: با استارت App.BackgroundJob، <c>JobDiscoveryService</c> تعریف جاب را با شناسه
	/// <c>App.BackgroundJob.Jobs.Cng.CngWorkReportFileImportJob.ImportFilesFromUnc</c>
	/// در <c>JobDefinitions</c> می‌سازد (زمان‌بندی خودکار ندارد).
	/// </para>
	/// <para>
	/// اجرا روی سرور (دستی):
	/// 1) سرویس/اپ <c>App.BackgroundJob</c> را روی سروری که به شِیر UNC دسترسی دارد استارت کنید.
	/// 2) در UI جاب‌ها (<c>/Jobs</c> → «مدیریت سرویس‌های پس‌زمینه») جاب
	///    «انتقال فایل‌های گزارش کار CNG از UNC» را پیدا کنید.
	/// 3) اگر زمان‌بندی ندارد، با «ایجاد زمان‌بندی» یک schedule بسازید (می‌تواند غیرفعال بماند).
	/// 4) «اجرا فوری» / <c>POST /Jobs/RunNow</c> با <c>scheduleId</c> همان جاب را بزنید.
	/// </para>
	/// بعد از اجرا، دانلود پنل از FileId سرو می‌شود؛ WorkReportFileAddress دست‌نخورده می‌ماند.
	/// </summary>
	public class CngWorkReportFileImportJob(
		IUnitOfWork unitOfWork,
		IFileService fileService)
	{
		private const int PageSize = 200;

		[JobHandler(
			"انتقال فایل‌های گزارش کار CNG از UNC",
			"فایل‌های WorkReportFileAddress را از طریق IFileService آپلود می‌کند و FileId را ست می‌کند. یک‌باره/دستی؛ زمان‌بندی خودکار ندارد.")]
		public async Task ImportFilesFromUnc(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync(
					"شروع انتقال فایل‌های گزارش کار CNG از UNC از طریق IFileService",
					cn);

				var uploaded = 0;
				var missing = 0;
				var failed = 0;
				var skippedEmpty = 0;
				var processed = 0;
				long lastId = 0;
				var entityTypeName = typeof(WorkReport).FullName!;

				while (true)
				{
					var batch = await unitOfWork.Repository<WorkReport>().Table
						.Where(w => w.Id != null
							&& w.Id > lastId
							&& w.FileId == null
							&& w.WorkReportFileAddress != null
							&& w.WorkReportFileAddress != "")
						.OrderBy(w => w.Id)
						.Take(PageSize)
						.ToListAsync(cn);

					if (batch.Count == 0)
						break;

					lastId = batch[^1].Id!.Value;

					foreach (var row in batch)
					{
						processed++;
						cn.ThrowIfCancellationRequested();

						try
						{
							var sourcePath = row.WorkReportFileAddress!.Trim();
							if (string.IsNullOrWhiteSpace(sourcePath))
							{
								skippedEmpty++;
								continue;
							}

							if (!File.Exists(sourcePath))
							{
								missing++;
								await jobLogger?.LogWarningAsync(
									$"فایل گزارش کار یافت نشد. WorkReportId={row.Id}, HtsId={row.HtsId}, Path={sourcePath}",
									0,
									cn);
								continue;
							}

							var bytes = await File.ReadAllBytesAsync(sourcePath, cn);
							if (bytes.Length == 0)
							{
								missing++;
								await jobLogger?.LogWarningAsync(
									$"فایل گزارش کار خالی است. WorkReportId={row.Id}, HtsId={row.HtsId}, Path={sourcePath}",
									0,
									cn);
								continue;
							}

							var fileName = Path.GetFileName(sourcePath);
							if (string.IsNullOrWhiteSpace(fileName))
								fileName = $"WorkReport_{row.HtsId}{row.WorkReportFileType ?? Path.GetExtension(sourcePath)}";

							var uploadedFile = await fileService.UploadAsync(
								bytes,
								fileName,
								null,
								entityTypeName,
								nameof(WorkReport.File),
								row.Id,
								cn);

							if (uploadedFile.Id is null or 0)
							{
								failed++;
								await jobLogger?.LogErrorAsync(
									$"آپلود فایل گزارش کار شناسه برنگرداند. WorkReportId={row.Id}, Path={sourcePath}",
									0,
									cn);
								continue;
							}

							row.FileId = uploadedFile.Id;
							// WorkReportFileAddress عمداً پاک/بازنویسی نمی‌شود — مرجع منبع UNC می‌ماند.
							uploaded++;
						}
						catch (Exception ex)
						{
							failed++;
							await jobLogger?.LogErrorAsync(
								$"خطا در انتقال فایل گزارش کار WorkReportId={row.Id}: {ex.Message}",
								0,
								cn);
						}
					}

					await unitOfWork.SaveChangesAsync(cn);

					if (processed % 1000 == 0 || batch.Count < PageSize)
					{
						await jobLogger?.LogInfoAsync(
							$"پیشرفت انتقال فایل گزارش کار — پردازش: {processed}، آپلود: {uploaded}، ناموجود: {missing}، خطا: {failed}",
							cn);
					}
				}

				await jobLogger?.LogInfoAsync(
					$"پایان انتقال فایل‌های گزارش کار CNG. پردازش: {processed}، آپلود: {uploaded}، ناموجود: {missing}، خالی/رد شده: {skippedEmpty}، خطا: {failed}",
					cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}
	}
}
