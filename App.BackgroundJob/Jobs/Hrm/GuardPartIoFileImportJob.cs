using Common.Attributes;
using Data.Contracts;
using Entities.App.Hrm;
using Microsoft.EntityFrameworkCore;
using Services.FileServices;
using Services.Job;

namespace App.BackgroundJob.Jobs.Hrm
{
	/// <summary>
	/// یک‌باره: انتقال پیوست‌های اقلام ورود/خروج کالای حراست از مسیر قدیمی (HtsAttachmentPath)
	/// از طریق <see cref="IFileService"/> و ست کردن <see cref="GuardPartIoDetail.FileId"/>.
	/// <para>
	/// ثبت خودکار: با استارت App.BackgroundJob، <c>JobDiscoveryService</c> تعریف جاب را با شناسه
	/// <c>App.BackgroundJob.Jobs.Hrm.GuardPartIoFileImportJob.ImportFilesFromHtsPath</c>
	/// در <c>JobDefinitions</c> می‌سازد (زمان‌بندی خودکار ندارد).
	/// </para>
	/// <para>
	/// اجرا روی سرور (دستی):
	/// 1) سرویس/اپ <c>App.BackgroundJob</c> را روی سروری که به مسیر فایل‌های HTS دسترسی دارد استارت کنید.
	/// 2) در UI جاب‌ها (<c>/Jobs</c> → «مدیریت سرویس‌های پس‌زمینه») جاب
	///    «انتقال پیوست‌های ورود/خروج کالای حراست» را پیدا کنید.
	/// 3) اگر زمان‌بندی ندارد، با «ایجاد زمان‌بندی» یک schedule بسازید (می‌تواند غیرفعال بماند).
	/// 4) «اجرا فوری» / <c>POST /Jobs/RunNow</c> با <c>scheduleId</c> همان جاب را بزنید.
	/// </para>
	/// بعد از اجرا، دانلود پنل از FileId سرو می‌شود؛ HtsAttachmentPath دست‌نخورده می‌ماند.
	/// </summary>
	public class GuardPartIoFileImportJob(
		IUnitOfWork unitOfWork,
		IFileService fileService)
	{
		private const int PageSize = 200;

		[JobHandler(
			"انتقال پیوست‌های ورود/خروج کالای حراست",
			"فایل‌های HtsAttachmentPath را از طریق IFileService آپلود می‌کند و FileId را ست می‌کند. یک‌باره/دستی؛ زمان‌بندی خودکار ندارد.")]
		public async Task ImportFilesFromHtsPath(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync(
					"شروع انتقال پیوست‌های ورود/خروج کالای حراست از مسیر قدیمی از طریق IFileService",
					cn);

				var uploaded = 0;
				var missing = 0;
				var failed = 0;
				var skippedEmpty = 0;
				var processed = 0;
				long lastId = 0;
				var entityTypeName = typeof(GuardPartIoDetail).FullName!;

				while (true)
				{
					var batch = await unitOfWork.Repository<GuardPartIoDetail>().Table
						.Where(w => w.Id != null
							&& w.Id > lastId
							&& w.FileId == null
							&& w.HtsAttachmentPath != null
							&& w.HtsAttachmentPath != "")
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
							var sourcePath = row.HtsAttachmentPath!.Trim();
							if (string.IsNullOrWhiteSpace(sourcePath))
							{
								skippedEmpty++;
								continue;
							}

							if (!File.Exists(sourcePath))
							{
								missing++;
								await jobLogger?.LogWarningAsync(
									$"پیوست یافت نشد. GuardPartIoDetailId={row.Id}, HtsId={row.HtsId}, Path={sourcePath}",
									0,
									cn);
								continue;
							}

							var bytes = await File.ReadAllBytesAsync(sourcePath, cn);
							if (bytes.Length == 0)
							{
								missing++;
								await jobLogger?.LogWarningAsync(
									$"پیوست خالی است. GuardPartIoDetailId={row.Id}, HtsId={row.HtsId}, Path={sourcePath}",
									0,
									cn);
								continue;
							}

							var fileName = Path.GetFileName(sourcePath);
							if (string.IsNullOrWhiteSpace(fileName))
								fileName = $"GuardPartIo_{row.HtsId}{Path.GetExtension(sourcePath)}";

							var uploadedFile = await fileService.UploadAsync(
								bytes,
								fileName,
								null,
								entityTypeName,
								nameof(GuardPartIoDetail.File),
								row.Id,
								cn);

							if (uploadedFile.Id is null or 0)
							{
								failed++;
								await jobLogger?.LogErrorAsync(
									$"آپلود پیوست شناسه برنگرداند. GuardPartIoDetailId={row.Id}, Path={sourcePath}",
									0,
									cn);
								continue;
							}

							row.FileId = uploadedFile.Id;
							// HtsAttachmentPath عمداً پاک/بازنویسی نمی‌شود — مرجع منبع می‌ماند.
							uploaded++;
						}
						catch (Exception ex)
						{
							failed++;
							await jobLogger?.LogErrorAsync(
								$"خطا در انتقال پیوست GuardPartIoDetailId={row.Id}: {ex.Message}",
								0,
								cn);
						}
					}

					await unitOfWork.SaveChangesAsync(cn);

					if (processed % 1000 == 0 || batch.Count < PageSize)
					{
						await jobLogger?.LogInfoAsync(
							$"پیشرفت انتقال پیوست — پردازش: {processed}، آپلود: {uploaded}، ناموجود: {missing}، خطا: {failed}",
							cn);
					}
				}

				await jobLogger?.LogInfoAsync(
					$"پایان انتقال پیوست‌های ورود/خروج کالای حراست. پردازش: {processed}، آپلود: {uploaded}، ناموجود: {missing}، خالی/رد شده: {skippedEmpty}، خطا: {failed}",
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
