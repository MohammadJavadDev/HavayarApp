using Common.Attributes;
using Data.Contracts;
using Entities.App.Law;
using Microsoft.EntityFrameworkCore;
using Services.FileServices;
using Services.Job;
using System.Text.RegularExpressions;

namespace App.BackgroundJob.Jobs.Law
{
	/// <summary>
	/// یک‌باره / دستی: فایل‌های پیوست پرونده حقوقی HTS را از مسیر UNC قدیمی
	/// (<see cref="JudicialFileAttachment.HtsFilePath"/>، مثل \\172.20.40.27\Uploads\Juridical\1405\...) با
	/// <see cref="IFileService"/> آپلود می‌کند و <see cref="JudicialFileAttachment.FileId"/> را ست می‌کند.
	/// <para>
	/// ثبت خودکار: <c>JobDiscoveryService</c> این جاب را با شناسه
	/// <c>App.BackgroundJob.Jobs.Law.JudicialFileAttachmentImportJob.ImportFilesFromUnc</c> می‌سازد (زمان‌بندی خودکار ندارد).
	/// App.BackgroundJob باید روی سروری اجرا شود که به شِیر UNC دسترسی دارد؛ از «مدیریت سرویس‌های پس‌زمینه»
	/// ← «اجرا فوری» اجرا کنید. ردیف‌هایی که FileId دارند دوباره آپلود نمی‌شوند (قابل تکرار).
	/// </para>
	/// </summary>
	public class JudicialFileAttachmentImportJob(
		IUnitOfWork unitOfWork,
		IFileService fileService)
	{
		private const int PageSize = 100;

		// HTS نام فایل روی دیسک را با پیشوند زمان ذخیره می‌کرد: 14050726_050755843_نام اصلی.pdf
		private static readonly Regex HtsPrefix = new(@"^\d{8}_\d+_", RegexOptions.Compiled);

		[JobHandler(
			"انتقال پیوست‌های پرونده حقوقی از UNC",
			"فایل‌های پیوست پرونده حقوقی HTS را از HtsFilePath با IFileService آپلود می‌کند و FileId را ست می‌کند. یک‌باره/دستی؛ زمان‌بندی خودکار ندارد.")]
		public async Task ImportFilesFromUnc(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع انتقال پیوست‌های پرونده حقوقی از UNC", cn);

				var uploaded = 0;
				var missing = 0;
				var failed = 0;
				var processed = 0;
				long lastId = 0;
				var entityTypeName = typeof(JudicialFileAttachment).FullName!;

				while (true)
				{
					var batch = await unitOfWork.Repository<JudicialFileAttachment>().Table
						.Where(a => a.Id != null
							&& a.Id > lastId
							&& a.FileId == null
							&& a.HtsFilePath != null
							&& a.HtsFilePath != "")
						.OrderBy(a => a.Id)
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
							var sourcePath = row.HtsFilePath!.Trim();
							if (!File.Exists(sourcePath))
							{
								missing++;
								await jobLogger?.LogWarningAsync(
									$"فایل پیوست یافت نشد. AttachmentId={row.Id}, HtsId={row.HtsId}, Path={sourcePath}", 0, cn);
								continue;
							}

							var bytes = await File.ReadAllBytesAsync(sourcePath, cn);
							if (bytes.Length == 0)
							{
								missing++;
								await jobLogger?.LogWarningAsync(
									$"فایل پیوست خالی است. AttachmentId={row.Id}, HtsId={row.HtsId}, Path={sourcePath}", 0, cn);
								continue;
							}

							var fileName = HtsPrefix.Replace(Path.GetFileName(sourcePath), string.Empty);
							if (string.IsNullOrWhiteSpace(fileName))
								fileName = Path.GetFileName(sourcePath);

							var uploadedFile = await fileService.UploadAsync(
								bytes,
								fileName,
								null,
								entityTypeName,
								nameof(JudicialFileAttachment.File),
								row.Id,
								cn);

							if (uploadedFile.Id is null or 0)
							{
								failed++;
								await jobLogger?.LogErrorAsync(
									$"آپلود شناسه فایل برنگرداند. AttachmentId={row.Id}, Path={sourcePath}", 0, cn);
								continue;
							}

							row.FileId = uploadedFile.Id;
							uploaded++;
						}
						catch (Exception ex)
						{
							failed++;
							await jobLogger?.LogErrorAsync(
								$"خطا در انتقال پیوست AttachmentId={row.Id}: {ex.Message}", 0, cn);
						}
					}

					await unitOfWork.SaveChangesAsync(cn);

					await jobLogger?.LogInfoAsync(
						$"پیشرفت — پردازش: {processed}، آپلود: {uploaded}، ناموجود: {missing}، خطا: {failed}", cn);
				}

				await jobLogger?.LogInfoAsync(
					$"پایان انتقال پیوست‌های پرونده حقوقی. پردازش: {processed}، آپلود: {uploaded}، ناموجود/خالی: {missing}، خطا: {failed}", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}
	}
}
