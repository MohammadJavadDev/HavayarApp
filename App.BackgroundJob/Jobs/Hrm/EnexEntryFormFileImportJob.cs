using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Hrm;
using Microsoft.EntityFrameworkCore;
using Services.FileServices;
using Services.Job;
using System.Data;
using System.Data.Common;

namespace App.BackgroundJob.Jobs.Hrm
{
	/// <summary>
	/// یک‌باره: پیوست فرم خروج از مسیر HTS و پیوست قلم از محتوای باینری TotalSystem.
	/// زمان‌بندی خودکار ندارد؛ از صفحه جاب‌ها اجرا می‌شود.
	/// </summary>
	public class EnexEntryFormFileImportJob(IUnitOfWork unitOfWork, IFileService fileService, HtsDbContext htsDb)
	{
		private const int PageSize = 100;

		[JobHandler(
			"انتقال پیوست‌های ورود اضطراری",
			"فایل فرم خروج را از مسیر HTS و فایل قلم را از TotalSystem آپلود می‌کند. یک‌باره؛ زمان‌بندی خودکار ندارد.")]
		public async Task ImportFiles(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			await jobLogger?.LogInfoAsync("شروع انتقال پیوست‌های ورود اضطراری", cn);
			var exit = await ImportExitFilesAsync(jobLogger, cn);
			var parts = await ImportPartFilesAsync(jobLogger, cn);
			await jobLogger?.LogInfoAsync(
				$"پایان انتقال پیوست ورود اضطراری. خروج: آپلود {exit.uploaded}، ناموجود {exit.missing}، خطا {exit.failed}. قلم: آپلود {parts.uploaded}، بدون محتوا {parts.missing}، خطا {parts.failed}",
				cn);
		}

		private async Task<(int uploaded, int missing, int failed)> ImportExitFilesAsync(IJobLogger? jobLogger, CancellationToken cn)
		{
			var uploaded = 0;
			var missing = 0;
			var failed = 0;
			long lastId = 0;
			var entityTypeName = typeof(EnexExitForm).FullName!;

			while (true)
			{
				var batch = await unitOfWork.Repository<EnexExitForm>().Table
					.Where(w => w.Id != null && w.Id > lastId && w.FileId == null
						&& w.HtsAttachmentPath != null && w.HtsAttachmentPath != "")
					.OrderBy(w => w.Id)
					.Take(PageSize)
					.ToListAsync(cn);
				if (batch.Count == 0) break;
				lastId = batch[^1].Id!.Value;

				foreach (var row in batch)
				{
					cn.ThrowIfCancellationRequested();
					try
					{
						var sourcePath = row.HtsAttachmentPath!.Trim();
						if (!File.Exists(sourcePath))
						{
							missing++;
							await jobLogger?.LogWarningAsync($"پیوست خروج یافت نشد. Id={row.Id}, Path={sourcePath}", 0, cn);
							continue;
						}

						var bytes = await File.ReadAllBytesAsync(sourcePath, cn);
						if (bytes.Length == 0)
						{
							missing++;
							continue;
						}

						var fileName = row.AttachmentFileName;
						if (string.IsNullOrWhiteSpace(fileName))
							fileName = Path.GetFileName(sourcePath);
						if (string.IsNullOrWhiteSpace(fileName))
							fileName = $"EnexExit_{row.HtsId}";

						var uploadedFile = await fileService.UploadAsync(bytes, fileName, null, entityTypeName, nameof(EnexExitForm.File), row.Id, cn);
						if (uploadedFile.Id is null or 0)
						{
							failed++;
							continue;
						}

						row.FileId = uploadedFile.Id;
						uploaded++;
					}
					catch (Exception ex)
					{
						failed++;
						await jobLogger?.LogErrorAsync($"خطا در پیوست خروج Id={row.Id}: {ex.Message}", 0, cn);
					}
				}

				await unitOfWork.SaveChangesAsync(cn);
			}

			return (uploaded, missing, failed);
		}

		private async Task<(int uploaded, int missing, int failed)> ImportPartFilesAsync(IJobLogger? jobLogger, CancellationToken cn)
		{
			var uploaded = 0;
			var missing = 0;
			var failed = 0;
			long lastId = 0;
			var entityTypeName = typeof(EnexFormPart).FullName!;
			var conn = htsDb.Database.GetDbConnection();
			if (conn.State != ConnectionState.Open)
				await conn.OpenAsync(cn);

			try
			{
				while (true)
				{
					var batch = await unitOfWork.Repository<EnexFormPart>().Table
						.Where(w => w.Id != null && w.Id > lastId && w.FileId == null
							&& w.HtsId > 0
							&& w.HtsAttachmentFileName != null && w.HtsAttachmentFileName != "")
						.OrderBy(w => w.Id)
						.Take(PageSize)
						.ToListAsync(cn);
					if (batch.Count == 0) break;
					lastId = batch[^1].Id!.Value;

					foreach (var row in batch)
					{
						cn.ThrowIfCancellationRequested();
						try
						{
							var (name, bytes) = await ReadHtsPartFileAsync(conn, row.HtsId, cn);
							if (bytes == null || bytes.Length == 0)
							{
								missing++;
								continue;
							}

							var fileName = string.IsNullOrWhiteSpace(name) ? row.HtsAttachmentFileName! : name;
							var uploadedFile = await fileService.UploadAsync(bytes, fileName, null, entityTypeName, nameof(EnexFormPart.File), row.Id, cn);
							if (uploadedFile.Id is null or 0)
							{
								failed++;
								continue;
							}

							row.FileId = uploadedFile.Id;
							uploaded++;
						}
						catch (Exception ex)
						{
							failed++;
							await jobLogger?.LogErrorAsync($"خطا در پیوست قلم Id={row.Id}: {ex.Message}", 0, cn);
						}
					}

					await unitOfWork.SaveChangesAsync(cn);
				}
			}
			finally
			{
				if (conn.State == ConnectionState.Open)
					await conn.CloseAsync();
			}

			return (uploaded, missing, failed);
		}

		private static async Task<(string? name, byte[]? bytes)> ReadHtsPartFileAsync(DbConnection conn, long htsId, CancellationToken cn)
		{
			await using var cmd = conn.CreateCommand();
			cmd.CommandText = "SELECT AttachmentFileName, AttachmentFileContent FROM dbo.Enex_FormPart WHERE Id = @id";
			var p = cmd.CreateParameter();
			p.ParameterName = "@id";
			p.Value = (int)htsId;
			cmd.Parameters.Add(p);
			await using var reader = await cmd.ExecuteReaderAsync(cn);
			if (!await reader.ReadAsync(cn)) return (null, null);
			var name = reader.IsDBNull(0) ? null : reader.GetString(0);
			byte[]? bytes = reader.IsDBNull(1) ? null : (byte[])reader.GetValue(1);
			return (name, bytes);
		}
	}
}
