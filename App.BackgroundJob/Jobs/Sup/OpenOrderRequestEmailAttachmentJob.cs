using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Edms;
using Entities.App.Inv;
using Entities.App.Sup;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Services.FileServices;
using Services.Job;

namespace App.BackgroundJob.Jobs.Sup
{
	/// <summary>
	/// کپی فایل‌های پیوست پیشینه ارسال به پیمانکار از HTS
	/// (<c>Attachment_FK</c> / <c>PartAttachmentId</c> / <c>EdmsDocumentAttachmentId</c>)
	/// به <see cref="FileEntity"/> روی <see cref="OpenOrderRequestEmailToSupplier"/>.
	/// جدا از <c>OpenOrderRequestJob</c> — الگوی آپلود عین <c>SyncOpenOrderRequestAttachmentsFromHts</c>.
	/// </summary>
	public class OpenOrderRequestEmailAttachmentJob(
		IUnitOfWork unitOfWork,
		HtsDbContext htsDb,
		IFileService fileService)
	{
		[JobHandler(
			"کپی پیوست‌های پیشینه ارسال به پیمانکار از HTS",
			"یک‌باره / تکراری امن: برای ردیف‌های EmailToSupplier که HtsId دارند و ستون پیوست خالی است، فایل را از HTS می‌خواند و UploadAsync می‌کند (یا از پیوست/مدرک/کالای از قبل مهاجرت‌شده لینک می‌گیرد).")]
		public async Task SyncEmailToSupplierAttachmentsFromHts(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				if (jobLogger != null)
					await jobLogger.LogInfoAsync("شروع کپی پیوست‌های EmailToSupplier از HTS...", cn);

				var htsRows = await htsDb.Hts_Sup_OpenOrderRequest_EmailToSuppliers.AsNoTracking()
					.Where(e => e.Attachment_FK != null || e.PartAttachmentId != null || e.EdmsDocumentAttachmentId != null)
					.Select(e => new
					{
						e.OpenOrderRequest_EmailToSupplier_ID,
						e.Attachment_FK,
						e.PartAttachmentId,
						e.EdmsDocumentAttachmentId
					})
					.ToListAsync(cn);

				if (jobLogger != null)
					await jobLogger.LogInfoAsync($"ردیف‌های HTS با حداقل یک پیوست: {htsRows.Count}", cn);

				if (htsRows.Count == 0)
					return;

				var htsIds = htsRows.Select(r => (long)r.OpenOrderRequest_EmailToSupplier_ID).ToList();
				var localEmails = await unitOfWork.Repository<OpenOrderRequestEmailToSupplier>().Table
					.Where(e => e.HtsId != null && htsIds.Contains(e.HtsId.Value))
					.ToListAsync(cn);
				var localByHtsId = localEmails
					.Where(e => e.HtsId.HasValue)
					.GroupBy(e => e.HtsId!.Value)
					.ToDictionary(g => g.Key, g => g.First());

				var oorAttByHts = await unitOfWork.Repository<OpenOrderRequestAttachment>().TableNoTracking
					.Where(a => a.HtsId != 0 && a.AttachmentId != null)
					.Select(a => new { a.HtsId, a.AttachmentId })
					.ToDictionaryAsync(a => a.HtsId, a => a.AttachmentId!.Value, cn);

				var partDocByHts = await unitOfWork.Repository<PartDocument>().TableNoTracking
					.Where(d => d.HtsId != 0 && d.AttachmentId > 0)
					.Select(d => new { d.HtsId, d.AttachmentId })
					.ToDictionaryAsync(d => (long)d.HtsId, d => d.AttachmentId, cn);

				var docMainByHts = await unitOfWork.Repository<Document>().TableNoTracking
					.Where(d => d.HtsId != 0 && d.MainFileId != null)
					.Select(d => new { d.HtsId, d.MainFileId })
					.ToDictionaryAsync(d => d.HtsId, d => d.MainFileId!.Value, cn);

				var linked = 0;
				var uploaded = 0;
				var skipped = 0;
				var failed = 0;
				var pendingSaves = 0;

				foreach (var hts in htsRows)
				{
					cn.ThrowIfCancellationRequested();
					try
					{
						if (!localByHtsId.TryGetValue(hts.OpenOrderRequest_EmailToSupplier_ID, out var local))
						{
							skipped++;
							continue;
						}

						var changed = false;

						if (hts.Attachment_FK is int oorAttHtsId && local.AttachmentId == null)
						{
							if (oorAttByHts.TryGetValue(oorAttHtsId, out var existingFileId))
							{
								local.AttachmentId = existingFileId;
								changed = true;
								linked++;
							}
							else
							{
								var fileId = await UploadOpenOrderAttachmentAsync(oorAttHtsId, cn);
								if (fileId != null)
								{
									local.AttachmentId = fileId;
									oorAttByHts[oorAttHtsId] = fileId.Value;
									changed = true;
									uploaded++;
								}
								else
									skipped++;
							}
						}

						if (hts.PartAttachmentId is int partAttHtsId && local.PartAttachmentId == null)
						{
							if (partDocByHts.TryGetValue(partAttHtsId, out var existingPartFileId))
							{
								local.PartAttachmentId = existingPartFileId;
								changed = true;
								linked++;
							}
							else
							{
								var fileId = await UploadPartAttachmentAsync(partAttHtsId, cn);
								if (fileId != null)
								{
									local.PartAttachmentId = fileId;
									partDocByHts[partAttHtsId] = fileId.Value;
									changed = true;
									uploaded++;
								}
								else
									skipped++;
							}
						}

						if (hts.EdmsDocumentAttachmentId is int docHtsId && local.EdmsDocumentAttachmentId == null)
						{
							if (docMainByHts.TryGetValue(docHtsId, out var existingDocFileId))
							{
								local.EdmsDocumentAttachmentId = existingDocFileId;
								changed = true;
								linked++;
							}
							else
							{
								var fileId = await UploadEdmsDocumentMainAsync(docHtsId, cn);
								if (fileId != null)
								{
									local.EdmsDocumentAttachmentId = fileId;
									docMainByHts[docHtsId] = fileId.Value;
									changed = true;
									uploaded++;
								}
								else
									skipped++;
							}
						}

						if (changed)
						{
							pendingSaves++;
							if (pendingSaves >= 50)
							{
								await unitOfWork.SaveChangesAsync(cn);
								pendingSaves = 0;
							}
						}
					}
					catch (Exception ex)
					{
						failed++;
						if (jobLogger != null)
							await jobLogger.LogErrorAsync(
								$"خطا در پیوست EmailToSupplier HtsId={hts.OpenOrderRequest_EmailToSupplier_ID}: {ex.Message}",
								0, cn);
					}
				}

				if (pendingSaves > 0)
					await unitOfWork.SaveChangesAsync(cn);

				if (jobLogger != null)
					await jobLogger.LogInfoAsync(
						$"پایان کپی پیوست EmailToSupplier. Linked={linked}, Uploaded={uploaded}, Skipped={skipped}, Failed={failed}",
						cn);
			}
			catch (Exception ex)
			{
				if (jobLogger != null)
					await jobLogger.LogErrorAsync($"خطای کلی SyncEmailToSupplierAttachmentsFromHts: {ex.Message}", 0, cn);
				throw;
			}
		}

		private async Task<long?> UploadOpenOrderAttachmentAsync(int htsAttachmentId, CancellationToken cn)
		{
			var meta = await htsDb.Hts_Sup_OpenOrderRequest_Attachments.AsNoTracking()
				.Where(a => a.OpenOrderRequest_Attachment_ID == htsAttachmentId)
				.Select(a => new
				{
					a.Attachment_FileName,
					a.AttachmentFilePath,
					HasDbContent = a.Attachment_FileContent != null
				})
				.FirstOrDefaultAsync(cn);

			if (meta == null)
				return null;

			var bytes = await ReadBytesAsync(meta.AttachmentFilePath, async () =>
			{
				if (!meta.HasDbContent) return null;
				return await htsDb.Hts_Sup_OpenOrderRequest_Attachments.AsNoTracking()
					.Where(a => a.OpenOrderRequest_Attachment_ID == htsAttachmentId)
					.Select(a => a.Attachment_FileContent)
					.FirstOrDefaultAsync(cn);
			}, cn);

			if (bytes == null || bytes.Length == 0)
				return null;

			var fileName = string.IsNullOrWhiteSpace(meta.Attachment_FileName)
				? $"oor-email-att-{htsAttachmentId}"
				: meta.Attachment_FileName;

			var uploaded = await fileService.UploadAsync(
				bytes,
				fileName,
				null,
				typeof(OpenOrderRequestEmailToSupplier).FullName,
				nameof(OpenOrderRequestEmailToSupplier.Attachment),
				null,
				cn);

			return uploaded.Id;
		}

		private async Task<long?> UploadPartAttachmentAsync(int htsPartAttachmentId, CancellationToken cn)
		{
			var meta = await htsDb.Hts_Inv_Part_Attachments.AsNoTracking()
				.Where(a => a.Part_Attachment_ID == htsPartAttachmentId)
				.Select(a => new
				{
					a.Attachment_FileName,
					a.AttachmentFilePath,
					HasDbContent = a.Attachment_FileContent != null
				})
				.FirstOrDefaultAsync(cn);

			if (meta == null)
				return null;

			var bytes = await ReadBytesAsync(meta.AttachmentFilePath, async () =>
			{
				if (!meta.HasDbContent) return null;
				return await htsDb.Hts_Inv_Part_Attachments.AsNoTracking()
					.Where(a => a.Part_Attachment_ID == htsPartAttachmentId)
					.Select(a => a.Attachment_FileContent)
					.FirstOrDefaultAsync(cn);
			}, cn);

			if (bytes == null || bytes.Length == 0)
				return null;

			var fileName = string.IsNullOrWhiteSpace(meta.Attachment_FileName)
				? $"part-email-att-{htsPartAttachmentId}"
				: meta.Attachment_FileName;

			var uploaded = await fileService.UploadAsync(
				bytes,
				fileName,
				null,
				typeof(OpenOrderRequestEmailToSupplier).FullName,
				nameof(OpenOrderRequestEmailToSupplier.PartAttachment),
				null,
				cn);

			return uploaded.Id;
		}

		private async Task<long?> UploadEdmsDocumentMainAsync(int htsDocumentId, CancellationToken cn)
		{
			var meta = await htsDb.Hts_Edms_Documents.AsNoTracking()
				.Where(d => d.Document_ID == htsDocumentId)
				.Select(d => new
				{
					d.Document_FileName,
					d.Document_FilePath
				})
				.FirstOrDefaultAsync(cn);

			if (meta == null)
				return null;

			if (string.IsNullOrWhiteSpace(meta.Document_FilePath) || !File.Exists(meta.Document_FilePath))
				return null;

			var bytes = await File.ReadAllBytesAsync(meta.Document_FilePath, cn);
			if (bytes.Length == 0)
				return null;

			var fileName = string.IsNullOrWhiteSpace(meta.Document_FileName)
				? $"edms-email-att-{htsDocumentId}"
				: meta.Document_FileName;

			var uploaded = await fileService.UploadAsync(
				bytes,
				fileName,
				null,
				typeof(OpenOrderRequestEmailToSupplier).FullName,
				nameof(OpenOrderRequestEmailToSupplier.EdmsDocumentAttachment),
				null,
				cn);

			return uploaded.Id;
		}

		private static async Task<byte[]?> ReadBytesAsync(
			string? filePath,
			Func<Task<byte[]?>> loadFromDb,
			CancellationToken cn)
		{
			if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
				return await File.ReadAllBytesAsync(filePath, cn);

			return await loadFromDb();
		}
	}
}
