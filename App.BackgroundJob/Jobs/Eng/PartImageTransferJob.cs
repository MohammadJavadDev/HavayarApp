using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Eng;
using Entities.App.Inv;
using Entities.Base;
using Entities.Hts.Inv;
using Microsoft.EntityFrameworkCore;
using Services.FileServices;
using Services.Job;

namespace App.BackgroundJob.Jobs.Eng;

/// <summary>
/// یک‌باره: کپی تصویرهای پارت‌لیست (UNC) و دفترچه قطعات (blob در TotalSystem)
/// به ذخیره‌سازی استاندارد <see cref="IFileService"/> و ست کردن PictureId.
/// <para>
/// ثبت خودکار: با استارت App.BackgroundJob، <c>JobDiscoveryService</c> تعریف جاب را با شناسه
/// <c>App.BackgroundJob.Jobs.Eng.PartImageTransferJob.TransferImages</c>
/// در <c>JobDefinitions</c> می‌سازد (زمان‌بندی خودکار ندارد).
/// </para>
/// <para>
/// اجرا روی سرور اپ (دستی) — جایی که <c>Storage:UploadsPath</c> همان مسیر واقعی فایل‌سرور جدید است:
/// 1) سرویس/اپ <c>App.BackgroundJob</c> را روی سرور اپ استارت کنید.
/// 2) در UI جاب‌ها جاب «انتقال تصاویر پارت‌لیست و دفترچه قطعات» را پیدا کنید.
/// 3) «اجرا فوری» را بزنید.
/// </para>
/// PicturePath پارت‌لیست به‌عنوان مرجع منبع UNC دست‌نخورده می‌ماند.
/// </summary>
public class PartImageTransferJob(
	IUnitOfWork unitOfWork,
	IFileService fileService,
	HtsDbContext htsDb)
{
	private const int PageSize = 100;

	[JobHandler(
		"انتقال تصاویر پارت‌لیست و دفترچه قطعات",
		"تصاویر Eng.PartListProductSection را از UNC و تصاویر Inv.PartBookSection را از PictureContent در TotalSystem از طریق IFileService به ذخیره‌سازی جدید منتقل می‌کند و PictureId را ست می‌کند. یک‌باره/دستی؛ زمان‌بندی خودکار ندارد.")]
	public async Task TransferImages(IJobLogger? jobLogger = null, CancellationToken cn = default)
	{
		try
		{
			await jobLogger?.LogInfoAsync(
				"شروع انتقال تصاویر پارت‌لیست (UNC) و دفترچه قطعات (TotalSystem blob) از طریق IFileService",
				cn);

			var partList = await TransferPartListImagesAsync(jobLogger, cn);
			var partBook = await TransferPartBookImagesAsync(jobLogger, cn);

			await jobLogger?.LogInfoAsync(
				$"پایان انتقال تصاویر. " +
				$"پارت‌لیست — کپی: {partList.Copied}، رد شده: {partList.Skipped}، ناموجود: {partList.Missing}، خطا: {partList.Failed}؛ " +
				$"دفترچه قطعات — کپی: {partBook.Copied}، رد شده: {partBook.Skipped}، ناموجود: {partBook.Missing}، خطا: {partBook.Failed}",
				cn);
		}
		catch (Exception ex)
		{
			await jobLogger?.LogExceptionAsync(ex, cn);
			throw;
		}
	}

	private async Task<TransferCounters> TransferPartListImagesAsync(IJobLogger? jobLogger, CancellationToken cn)
	{
		await jobLogger?.LogInfoAsync("شروع انتقال تصاویر پارت‌لیست از UNC", cn);

		var counters = new TransferCounters();
		var entityTypeName = typeof(PartListProductSection).FullName!;
		long lastId = 0;
		var processed = 0;

		while (true)
		{
			var batch = await unitOfWork.Repository<PartListProductSection>().Table
				.Where(s => s.Id != null && s.Id > lastId)
				.OrderBy(s => s.Id)
				.Take(PageSize)
				.ToListAsync(cn);

			if (batch.Count == 0)
				break;

			lastId = batch[^1].Id!.Value;

			var pictureIds = batch
				.Where(s => s.PictureId.HasValue)
				.Select(s => s.PictureId!.Value)
				.Distinct()
				.ToList();
			var fileById = await LoadFileEntitiesAsync(pictureIds, cn);

			foreach (var section in batch)
			{
				processed++;
				cn.ThrowIfCancellationRequested();

				try
				{
					if (section.PictureId.HasValue
						&& fileById.TryGetValue(section.PictureId.Value, out var existing)
						&& IsPresentInNewStorage(existing))
					{
						counters.Skipped++;
						continue;
					}

					var sourcePath = section.PicturePath?.Trim();
					if (string.IsNullOrWhiteSpace(sourcePath))
					{
						counters.Missing++;
						await jobLogger?.LogWarningAsync(
							$"مسیر تصویر پارت‌لیست خالی است. SectionId={section.Id}, HtsId={section.HtsId}, PictureId={section.PictureId}",
							0,
							cn);
						continue;
					}

					if (!File.Exists(sourcePath))
					{
						counters.Missing++;
						await jobLogger?.LogWarningAsync(
							$"فایل تصویر پارت‌لیست یافت نشد. SectionId={section.Id}, HtsId={section.HtsId}, Path={sourcePath}",
							0,
							cn);
						continue;
					}

					var bytes = await File.ReadAllBytesAsync(sourcePath, cn);
					if (bytes.Length == 0)
					{
						counters.Missing++;
						await jobLogger?.LogWarningAsync(
							$"فایل تصویر پارت‌لیست خالی است. SectionId={section.Id}, Path={sourcePath}",
							0,
							cn);
						continue;
					}

					var fileName = Path.GetFileName(sourcePath);
					if (string.IsNullOrWhiteSpace(fileName))
						fileName = $"PartListSection_{section.HtsId}.png";

					var uploaded = await fileService.UploadAsync(
						bytes,
						fileName,
						null,
						entityTypeName,
						nameof(PartListProductSection.Picture),
						section.Id,
						cn);

					if (uploaded.Id is null or 0)
					{
						counters.Failed++;
						await jobLogger?.LogErrorAsync(
							$"آپلود تصویر پارت‌لیست شناسه برنگرداند. SectionId={section.Id}, Path={sourcePath}",
							0,
							cn);
						continue;
					}

					// PicturePath عمداً دست‌نخورده می‌ماند — مرجع منبع UNC.
					section.PictureId = uploaded.Id;
					counters.Copied++;
				}
				catch (Exception ex)
				{
					counters.Failed++;
					await jobLogger?.LogErrorAsync(
						$"خطا در انتقال تصویر پارت‌لیست SectionId={section.Id}: {ex.Message}",
						0,
						cn);
				}
			}

			await unitOfWork.SaveChangesAsync(cn);

			if (processed % 500 == 0 || batch.Count < PageSize)
			{
				await jobLogger?.LogInfoAsync(
					$"پیشرفت پارت‌لیست — پردازش: {processed}، کپی: {counters.Copied}، رد شده: {counters.Skipped}، ناموجود: {counters.Missing}، خطا: {counters.Failed}",
					cn);
			}
		}

		await jobLogger?.LogInfoAsync(
			$"پایان پارت‌لیست. پردازش: {processed}، کپی: {counters.Copied}، رد شده: {counters.Skipped}، ناموجود: {counters.Missing}، خطا: {counters.Failed}",
			cn);

		return counters;
	}

	private async Task<TransferCounters> TransferPartBookImagesAsync(IJobLogger? jobLogger, CancellationToken cn)
	{
		await jobLogger?.LogInfoAsync("شروع انتقال تصاویر دفترچه قطعات از TotalSystem.PictureContent", cn);

		var counters = new TransferCounters();
		var entityTypeName = typeof(PartBookSection).FullName!;
		long lastId = 0;
		var processed = 0;

		while (true)
		{
			var batch = await unitOfWork.Repository<PartBookSection>().Table
				.Where(s => s.Id != null && s.Id > lastId && s.HtsId != 0)
				.OrderBy(s => s.Id)
				.Take(PageSize)
				.ToListAsync(cn);

			if (batch.Count == 0)
				break;

			lastId = batch[^1].Id!.Value;

			var pictureIds = batch
				.Where(s => s.PictureId.HasValue)
				.Select(s => s.PictureId!.Value)
				.Distinct()
				.ToList();
			var fileById = await LoadFileEntitiesAsync(pictureIds, cn);

			var htsIds = batch.Select(s => (int)s.HtsId).Distinct().ToList();
			var htsRows = await htsDb.Hts_Inv_PartBookSections.AsNoTracking()
				.Where(h => htsIds.Contains(h.Id))
				.Select(h => new { h.Id, h.PictureTitle, h.PictureContent })
				.ToListAsync(cn);
			var htsById = htsRows.ToDictionary(h => h.Id);

			foreach (var section in batch)
			{
				processed++;
				cn.ThrowIfCancellationRequested();

				try
				{
					if (section.PictureId.HasValue
						&& fileById.TryGetValue(section.PictureId.Value, out var existing)
						&& IsPresentInNewStorage(existing))
					{
						counters.Skipped++;
						continue;
					}

					if (!htsById.TryGetValue((int)section.HtsId, out var hts)
						|| hts.PictureContent == null
						|| hts.PictureContent.Length == 0)
					{
						counters.Missing++;
						await jobLogger?.LogWarningAsync(
							$"blob تصویر دفترچه قطعات در TotalSystem یافت نشد. SectionId={section.Id}, HtsId={section.HtsId}",
							0,
							cn);
						continue;
					}

					var fileName = !string.IsNullOrWhiteSpace(hts.PictureTitle)
						? hts.PictureTitle.Trim()
						: !string.IsNullOrWhiteSpace(section.PictureTitle)
							? section.PictureTitle.Trim()
							: $"PartBookSection_{section.HtsId}.jpg";

					var uploaded = await fileService.UploadAsync(
						hts.PictureContent,
						fileName,
						null,
						entityTypeName,
						nameof(PartBookSection.Picture),
						section.Id,
						cn);

					if (uploaded.Id is null or 0)
					{
						counters.Failed++;
						await jobLogger?.LogErrorAsync(
							$"آپلود تصویر دفترچه قطعات شناسه برنگرداند. SectionId={section.Id}, HtsId={section.HtsId}",
							0,
							cn);
						continue;
					}

					section.PictureId = uploaded.Id;
					if (string.IsNullOrWhiteSpace(section.PictureTitle) && !string.IsNullOrWhiteSpace(hts.PictureTitle))
						section.PictureTitle = hts.PictureTitle.Trim();

					counters.Copied++;
				}
				catch (Exception ex)
				{
					counters.Failed++;
					await jobLogger?.LogErrorAsync(
						$"خطا در انتقال تصویر دفترچه قطعات SectionId={section.Id}, HtsId={section.HtsId}: {ex.Message}",
						0,
						cn);
				}
			}

			await unitOfWork.SaveChangesAsync(cn);

			if (processed % 200 == 0 || batch.Count < PageSize)
			{
				await jobLogger?.LogInfoAsync(
					$"پیشرفت دفترچه قطعات — پردازش: {processed}، کپی: {counters.Copied}، رد شده: {counters.Skipped}، ناموجود: {counters.Missing}، خطا: {counters.Failed}",
					cn);
			}
		}

		await jobLogger?.LogInfoAsync(
			$"پایان دفترچه قطعات. پردازش: {processed}، کپی: {counters.Copied}، رد شده: {counters.Skipped}، ناموجود: {counters.Missing}، خطا: {counters.Failed}",
			cn);

		return counters;
	}

	private async Task<Dictionary<long, FileEntity>> LoadFileEntitiesAsync(List<long> fileIds, CancellationToken cn)
	{
		var result = new Dictionary<long, FileEntity>();
		if (fileIds.Count == 0)
			return result;

		var rows = await unitOfWork.Repository<FileEntity>().TableNoTracking
			.Where(f => f.Id.HasValue && fileIds.Contains(f.Id.Value))
			.ToListAsync(cn);

		foreach (var row in rows)
		{
			if (row.Id.HasValue)
				result[row.Id.Value] = row;
		}

		return result;
	}

	/// <summary>
	/// فقط مسیر نسبی قالب جدید (yyyyMMdd/guid.ext) که روی دیسک ذخیره‌سازی جدید موجود باشد قبول می‌شود.
	/// مسیر UNC/absolute حتی اگر فایل قدیمی در دسترس باشد «حاضر در ذخیره‌سازی جدید» محسوب نمی‌شود.
	/// </summary>
	private bool IsPresentInNewStorage(FileEntity file)
	{
		if (string.IsNullOrWhiteSpace(file.PhysicalPath))
			return false;

		var normalized = file.PhysicalPath.Replace('/', Path.DirectorySeparatorChar);
		if (Path.IsPathRooted(normalized))
			return false;

		var fullPath = fileService.GetFullPhysicalPath(file.PhysicalPath);
		return File.Exists(fullPath);
	}

	private sealed class TransferCounters
	{
		public int Copied { get; set; }
		public int Skipped { get; set; }
		public int Missing { get; set; }
		public int Failed { get; set; }
	}
}
