using Common.Attributes;
using Data.Contracts;
using Entities.App.Hrm;
using Microsoft.EntityFrameworkCore;
using Services.FileServices;
using Services.Job;

namespace App.BackgroundJob.Jobs.Hrm
{
	/// <summary>
	/// یک‌باره: انتقال تصویر ورود پرسنل/پیمانکار از مسیر قدیمی (HtsEntryFilePath)
	/// از طریق <see cref="IFileService"/> و ست کردن <see cref="GuardPersonIo.EntryFileId"/>.
	/// زمان‌بندی خودکار ندارد؛ از UI جاب‌ها با «اجرا فوری» اجرا می‌شود.
	/// </summary>
	public class GuardPersonIoFileImportJob(
		IUnitOfWork unitOfWork,
		IFileService fileService)
	{
		private const int PageSize = 200;

		[JobHandler(
			"انتقال تصویر ورود پرسنل/پیمانکار",
			"فایل HtsEntryFilePath را از طریق IFileService آپلود می‌کند و EntryFileId را ست می‌کند. یک‌باره/دستی؛ زمان‌بندی خودکار ندارد.")]
		public async Task ImportFilesFromHtsPath(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync(
					"شروع انتقال تصویر ورود پرسنل/پیمانکار از مسیر قدیمی از طریق IFileService",
					cn);

				var uploaded = 0;
				var missing = 0;
				var failed = 0;
				var skippedEmpty = 0;
				var processed = 0;
				long lastId = 0;
				var entityTypeName = typeof(GuardPersonIo).FullName!;

				while (true)
				{
					var batch = await unitOfWork.Repository<GuardPersonIo>().Table
						.Where(w => w.Id != null
							&& w.Id > lastId
							&& w.EntryFileId == null
							&& w.HtsEntryFilePath != null
							&& w.HtsEntryFilePath != "")
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
							var sourcePath = row.HtsEntryFilePath!.Trim();
							if (string.IsNullOrWhiteSpace(sourcePath))
							{
								skippedEmpty++;
								continue;
							}

							if (!File.Exists(sourcePath))
							{
								missing++;
								await jobLogger?.LogWarningAsync(
									$"پیوست یافت نشد. GuardPersonIoId={row.Id}, HtsId={row.HtsId}, Path={sourcePath}",
									0,
									cn);
								continue;
							}

							var bytes = await File.ReadAllBytesAsync(sourcePath, cn);
							if (bytes.Length == 0)
							{
								missing++;
								await jobLogger?.LogWarningAsync(
									$"پیوست خالی است. GuardPersonIoId={row.Id}, HtsId={row.HtsId}, Path={sourcePath}",
									0,
									cn);
								continue;
							}

							var fileName = Path.GetFileName(sourcePath);
							if (string.IsNullOrWhiteSpace(fileName))
								fileName = $"GuardPersonIo_{row.HtsId}{Path.GetExtension(sourcePath)}";

							var uploadedFile = await fileService.UploadAsync(
								bytes,
								fileName,
								null,
								entityTypeName,
								nameof(GuardPersonIo.EntryFile),
								row.Id,
								cn);

							if (uploadedFile.Id is null or 0)
							{
								failed++;
								await jobLogger?.LogErrorAsync(
									$"آپلود پیوست شناسه برنگرداند. GuardPersonIoId={row.Id}, Path={sourcePath}",
									0,
									cn);
								continue;
							}

							row.EntryFileId = uploadedFile.Id;
							uploaded++;
						}
						catch (Exception ex)
						{
							failed++;
							await jobLogger?.LogErrorAsync(
								$"خطا در انتقال پیوست GuardPersonIoId={row.Id}: {ex.Message}",
								0,
								cn);
						}
					}

					await unitOfWork.SaveChangesAsync(cn);
				}

				await jobLogger?.LogInfoAsync(
					$"پایان انتقال تصویر ورود پرسنل/پیمانکار. پردازش: {processed}، آپلود: {uploaded}، ناموجود: {missing}، خالی/رد شده: {skippedEmpty}، خطا: {failed}",
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
