using Common.Attributes;
using Data.Contracts;
using Entities.App.Qc;
using Microsoft.EntityFrameworkCore;
using Services.FileServices;
using Services.Job;
using Services.Qc;

namespace App.BackgroundJob.Jobs.Qc
{
	public class ProductInspectionJob(IUnitOfWork unitOfWork, IFileService fileService)
	{
		[JobHandler("کپی خرابی کامنت قلم سفارش ساخت روی بازرسی محصول در هوایار", "معادل HTS RunQualitySystemTasks — هر ۲۰ دقیقه بین ۶ تا ۲۱")]
		public async Task AddFailureTypesFromProductionOrderItems(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			var now = DateTime.Now;
			if (now.Hour < 6 || now.Hour > 21)
			{
				if (jobLogger != null)
					await jobLogger.LogInfoAsync("خارج از بازه ۶ تا ۲۱ — اجرا نشد (مطابق HTS)", cn);
				return;
			}

			if (jobLogger != null)
				await jobLogger.LogInfoAsync("شروع کپی خرابی‌های کامنت قلم روی بازرسی محصول", cn);

			var count = await ProductInspectionFailureSync.AddFromProductionOrderItemCommentsAsync(unitOfWork, cn);

			if (jobLogger != null)
				await jobLogger.LogInfoAsync("ردیف خرابی اضافه‌شده: " + count, cn);
		}

		[JobHandler("کپی فایل پیوست بازرسی محصول در هوایار از مسیر HTS")]
		public async Task CopyProductInspectionAttachmentFiles(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			var pending = await unitOfWork.Repository<ProductInspectionAttachment>().Table
				.Where(a => a.FileId == null && a.SourceFilePath != null && a.SourceFilePath != "")
				.ToListAsync(cn);

			if (jobLogger != null)
				await jobLogger.LogInfoAsync("پیوست بدون فایل: " + pending.Count, cn);

			var copied = 0;
			var missing = 0;
			foreach (var attachment in pending)
			{
				try
				{
					var path = attachment.SourceFilePath!;
					if (!File.Exists(path))
					{
						missing++;
						if (jobLogger != null)
							await jobLogger.LogWarningAsync("فایل یافت نشد. AttachmentId=" + attachment.Id + " Path=" + path, 0, cn);
						continue;
					}

					var bytes = await File.ReadAllBytesAsync(path, cn);
					if (bytes.Length == 0)
					{
						missing++;
						continue;
					}

					var name = string.IsNullOrWhiteSpace(attachment.OriginalFileName)
						? Path.GetFileName(path)
						: attachment.OriginalFileName;
					var uploaded = await fileService.UploadAsync(
						bytes,
						name,
						null,
						typeof(ProductInspectionAttachment).FullName,
						nameof(ProductInspectionAttachment.File),
						attachment.Id,
						cn);

					attachment.FileId = uploaded.Id;
					attachment.OriginalFileName = name;
					copied++;
				}
				catch (Exception ex)
				{
					missing++;
					if (jobLogger != null)
						await jobLogger.LogWarningAsync("خطا در پیوست " + attachment.Id + ": " + ex.Message, 0, cn);
				}
			}

			if (copied > 0)
				await unitOfWork.SaveChangesAsync(cn);

			if (jobLogger != null)
				await jobLogger.LogInfoAsync("کپی شد: " + copied + " — ناموجود یا خطا: " + missing, cn);
		}
	}
}
