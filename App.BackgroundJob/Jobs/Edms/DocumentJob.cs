using Common.Attributes;
using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.App.Edms;
using Entities.App.Edms.Enums;
using Entities.App.Hcm;
using Entities.Base;
using Entities.Hts.Edms;
using Microsoft.EntityFrameworkCore;
using Services.FileServices;
using Services.Job;

namespace App.BackgroundJob.Jobs.Edms
{
	public class DocumentJob(
		HtsDbContext htsDb,
		IUnitOfWork unitOfWork,
		ApplicationDbContext appContext,
		IFileService fileService)
	{
		private const string PortalEdmsRoot = @"D:\PortalData\EDMS";
		private const string EdmsPathMarker = @"\EDMS\";

		private static string? ResolvePortalEdmsPath(string? htsPath)
		{
			if (string.IsNullOrWhiteSpace(htsPath))
				return null;

			var normalized = htsPath.Trim().Replace('/', '\\');
			var markerIndex = normalized.IndexOf(EdmsPathMarker, StringComparison.OrdinalIgnoreCase);
			if (markerIndex < 0)
				return normalized;

			var relative = normalized[(markerIndex + EdmsPathMarker.Length)..].TrimStart('\\');
			return Path.Combine(PortalEdmsRoot, relative);
		}

		private static string? LocatePortalFile(string? htsPath)
		{
			var expected = ResolvePortalEdmsPath(htsPath);
			if (string.IsNullOrWhiteSpace(expected))
				return null;

			if (SafeFileExists(expected))
				return expected;

			var relative = GetEdmsRelativePath(htsPath);
			if (string.IsNullOrWhiteSpace(relative) || !SafeDirectoryExists(PortalEdmsRoot))
				return null;

			var current = PortalEdmsRoot;
			var parts = relative.Split('\\', StringSplitOptions.RemoveEmptyEntries);
			for (var i = 0; i < parts.Length; i++)
			{
				var found = FindChild(current, parts[i].Trim(), file: i == parts.Length - 1);
				if (found == null)
					return null;
				current = found;
			}

			return current;
		}

		private static string? GetEdmsRelativePath(string? htsPath)
		{
			if (string.IsNullOrWhiteSpace(htsPath))
				return null;

			var normalized = htsPath.Trim().Replace('/', '\\');
			var markerIndex = normalized.IndexOf(EdmsPathMarker, StringComparison.OrdinalIgnoreCase);
			if (markerIndex < 0)
				return null;

			return normalized[(markerIndex + EdmsPathMarker.Length)..].TrimStart('\\');
		}

		private static string? FindChild(string directory, string name, bool file)
		{
			if (string.IsNullOrWhiteSpace(name))
				return null;

			var exact = Path.Combine(directory, name);
			if (file ? SafeFileExists(exact) : SafeDirectoryExists(exact))
				return exact;

			IEnumerable<string> children;
			try
			{
				var root = ToLongPath(directory);
				children = file ? Directory.EnumerateFiles(root) : Directory.EnumerateDirectories(root);
			}
			catch
			{
				return null;
			}

			foreach (var child in children)
			{
				var childPath = StripLongPathPrefix(child);
				var childName = Path.GetFileName(childPath);
				if (string.Equals(childName.Trim(), name, StringComparison.OrdinalIgnoreCase))
					return childPath;
			}

			return null;
		}

		private static bool SafeFileExists(string path)
		{
			try
			{
				return File.Exists(path) || File.Exists(ToLongPath(path));
			}
			catch
			{
				return false;
			}
		}

		private static bool SafeDirectoryExists(string path)
		{
			try
			{
				return Directory.Exists(path) || Directory.Exists(ToLongPath(path));
			}
			catch
			{
				return false;
			}
		}

		private static string ToLongPath(string path)
		{
			var full = Path.GetFullPath(path);
			if (full.StartsWith(@"\\?\", StringComparison.Ordinal))
				return full;
			if (full.StartsWith(@"\\", StringComparison.Ordinal))
				return @"\\?\UNC\" + full[2..];
			return @"\\?\" + full;
		}

		private static string StripLongPathPrefix(string path)
		{
			if (path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
				return @"\\" + path[@"\\?\UNC\".Length..];
			if (path.StartsWith(@"\\?\", StringComparison.Ordinal))
				return path[4..];
			return path;
		}
		private const int PageSize = 200;
		private const int SaveBatchSize = 500;
		private const int HoldCauseLookupOffset = 409;
		private const int CommentMaxLength = 2000;

		[JobHandler("هماهنگ کردن مدارک مهندسی از HTS")]
		public async Task SyncDocumentsFromHts(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی مدارک مهندسی از HTS", cn);

				var vpisByHtsId = (await unitOfWork.Repository<ProjectVpis>().TableNoTracking
						.Where(v => v.HtsId != 0 && v.Id.HasValue)
						.Select(v => new { v.HtsId, Id = v.Id!.Value, v.ProjectNameId })
						.ToListAsync(cn))
					.GroupBy(v => v.HtsId)
					.ToDictionary(g => g.Key, g => g.First());

				if (vpisByHtsId.Count == 0)
				{
					await jobLogger?.LogWarningAsync(
						"هیچ VPIS همگام‌شده‌ای با HtsId یافت نشد. ابتدا جاب VPIS پروژه‌ها را اجرا کنید",
						0,
						cn);
					return;
				}

				var userMaps = await BuildUserMapsAsync(jobLogger, cn);

				var insertedCount = 0;
				var updatedCount = 0;
				var skippedCount = 0;
				var failedCount = 0;
				var skippedVpisIds = new HashSet<long>();
				var unmatchedUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				var pendingChanges = 0;
				var lastId = 0;
				var processed = 0;

				while (true)
				{
					var batch = await htsDb.Hts_Edms_Documents.AsNoTracking()
						.Where(d => d.Document_ID > lastId)
						.OrderBy(d => d.Document_ID)
						.Take(PageSize)
						.ToListAsync(cn);

					if (batch.Count == 0)
						break;

					lastId = batch[^1].Document_ID;
					processed += batch.Count;

					var htsIds = batch.Select(d => (long)d.Document_ID).ToList();
					var existing = await unitOfWork.Repository<Document>().Table
						.Where(d => htsIds.Contains(d.HtsId))
						.ToListAsync(cn);
					var byHtsId = existing
						.GroupBy(d => d.HtsId)
						.ToDictionary(g => g.Key, g => g.First());

					foreach (var hts in batch)
					{
						try
						{
							if (!vpisByHtsId.TryGetValue(hts.Project_Vpis_FK, out var vpis))
							{
								skippedVpisIds.Add(hts.Project_Vpis_FK);
								skippedCount++;
								continue;
							}

							var mapped = MapDocument(hts, vpis.Id, vpis.ProjectNameId, userMaps, unmatchedUsers);

							if (byHtsId.TryGetValue(hts.Document_ID, out var current))
							{
								ApplyDocumentFields(current, mapped);
								updatedCount++;
							}
							else
							{
								await unitOfWork.Repository<Document>().AddAsync(
									mapped,
									cn,
									saveAudit: false,
									saveNow: false,
									InvokeAction: false);
								byHtsId[hts.Document_ID] = mapped;
								insertedCount++;
							}

							pendingChanges++;
							if (pendingChanges >= SaveBatchSize)
							{
								await unitOfWork.SaveChangesAsync(cn);
								pendingChanges = 0;
							}
						}
						catch (Exception ex)
						{
							failedCount++;
							await jobLogger?.LogErrorAsync(
								$"خطا در همگام‌سازی مدرک HTS Id={hts.Document_ID}: {ex.Message}",
								0,
								cn);
						}
					}

					if (pendingChanges > 0)
					{
						await unitOfWork.SaveChangesAsync(cn);
						pendingChanges = 0;
					}
				}

				await jobLogger?.LogInfoAsync(
					$"مدارک ذخیره شد. پردازش: {processed}، جدید: {insertedCount}، به‌روزرسانی: {updatedCount}، رد شده به‌خاطر VPIS: {skippedCount}، خطا: {failedCount}",
					cn);

				if (skippedVpisIds.Count > 0)
				{
					await jobLogger?.LogWarningAsync(
						$"تعداد {skippedVpisIds.Count} شناسه VPIS برای مدرک در App یافت نشد. نمونه: {string.Join(", ", skippedVpisIds.Take(20))}",
						0,
						cn);
				}

				if (unmatchedUsers.Count > 0)
				{
					await jobLogger?.LogWarningAsync(
						$"تعداد {unmatchedUsers.Count} شناسه کاربر HTS در App یافت نشد. نمونه: {string.Join(", ", unmatchedUsers.Take(20))}",
						0,
						cn);
				}

				await jobLogger?.LogInfoAsync("همگام‌سازی مدارک مهندسی با موفقیت انجام شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		[JobHandler("هماهنگ کردن کامنت مدارک مهندسی از HTS")]
		public async Task SyncDocumentCommentsFromHts(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی کامنت مدارک مهندسی از HTS", cn);

				var documentByHtsId = (await unitOfWork.Repository<Document>().TableNoTracking
						.Where(d => d.HtsId != 0 && d.Id.HasValue)
						.Select(d => new { d.HtsId, Id = d.Id!.Value })
						.ToListAsync(cn))
					.GroupBy(d => d.HtsId)
					.ToDictionary(g => g.Key, g => g.First().Id);

				if (documentByHtsId.Count == 0)
				{
					await jobLogger?.LogWarningAsync(
						"هیچ مدرک همگام‌شده‌ای با HtsId یافت نشد. ابتدا جاب مدارک را اجرا کنید",
						0,
						cn);
					return;
				}

				var userMaps = await BuildUserMapsAsync(jobLogger, cn);

				var insertedCount = 0;
				var updatedCount = 0;
				var skippedCount = 0;
				var failedCount = 0;
				var attachedCount = 0;
				var missingFileCount = 0;
				var unmatchedUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				var pendingChanges = 0;
				var lastId = 0;
				var processed = 0;

				while (true)
				{
					var batch = await htsDb.Hts_Edms_Document_Comments.AsNoTracking()
						.Where(c => c.Document_Comment_ID > lastId)
						.OrderBy(c => c.Document_Comment_ID)
						.Take(PageSize)
						.ToListAsync(cn);

					if (batch.Count == 0)
						break;

					lastId = batch[^1].Document_Comment_ID;
					processed += batch.Count;

					var htsIds = batch.Select(c => (long)c.Document_Comment_ID).ToList();
					var existing = await unitOfWork.Repository<DocumentComment>().Table
						.Where(c => htsIds.Contains(c.HtsId))
						.ToListAsync(cn);
					var byHtsId = existing
						.GroupBy(c => c.HtsId)
						.ToDictionary(g => g.Key, g => g.First());

					var documentFks = batch.Select(c => c.Document_FK).Distinct().ToList();
					var documentRoles = (await htsDb.Hts_Edms_Documents.AsNoTracking()
							.Where(d => documentFks.Contains(d.Document_ID))
							.Select(d => new { d.Document_ID, d.CheckedUser_FK, d.ApprovedUser_FK })
							.ToListAsync(cn))
						.GroupBy(d => d.Document_ID)
						.ToDictionary(
							g => g.Key,
							g => (CheckedUserId: g.First().CheckedUser_FK, ApprovedUserId: g.First().ApprovedUser_FK));

					foreach (var hts in batch)
					{
						try
						{
							if (!documentByHtsId.TryGetValue(hts.Document_FK, out var documentId))
							{
								skippedCount++;
								continue;
							}

							documentRoles.TryGetValue(hts.Document_FK, out var roles);
							var mapped = MapComment(
								hts,
								documentId,
								userMaps,
								unmatchedUsers,
								roles.CheckedUserId,
								roles.ApprovedUserId);

							if (byHtsId.TryGetValue(hts.Document_Comment_ID, out var current))
							{
								ApplyCommentFields(current, mapped);
								//if (!current.AttachmentId.HasValue)
								//{
								//	var uploadedId = await TryUploadCommentAttachmentAsync(
								//		hts,
								//		current.Id,
								//		jobLogger,
								//		cn);
								//	if (uploadedId.HasValue)
								//	{
								//		current.AttachmentId = uploadedId;
								//		attachedCount++;
								//	}
								//	else if (HasCommentAttachment(hts))
								//	{
								//		missingFileCount++;
								//	}
								//}

								updatedCount++;
							}
							else
							{
								//var uploadedId = await TryUploadCommentAttachmentAsync(hts, null, jobLogger, cn);
								//if (uploadedId.HasValue)
								//{
								//	mapped.AttachmentId = uploadedId;
								//	attachedCount++;
								//}
								//else if (HasCommentAttachment(hts))
								//{
								//	missingFileCount++;
								//}

								await unitOfWork.Repository<DocumentComment>().AddAsync(
									mapped,
									cn,
									saveAudit: false,
									saveNow: false,
									InvokeAction: false);
								byHtsId[hts.Document_Comment_ID] = mapped;
								insertedCount++;
							}

							pendingChanges++;
							if (pendingChanges >= SaveBatchSize)
							{
								await unitOfWork.SaveChangesAsync(cn);
								pendingChanges = 0;
							}
						}
						catch (Exception ex)
						{
							failedCount++;
							await jobLogger?.LogErrorAsync(
								$"خطا در همگام‌سازی کامنت HTS Id={hts.Document_Comment_ID}: {ex.Message}",
								0,
								cn);
						}
					}

					if (pendingChanges > 0)
					{
						await unitOfWork.SaveChangesAsync(cn);
						pendingChanges = 0;
					}
				}

				await jobLogger?.LogInfoAsync(
					$"کامنت‌ها ذخیره شد. پردازش: {processed}، جدید: {insertedCount}، به‌روزرسانی: {updatedCount}، رد شده به‌خاطر مدرک: {skippedCount}، پیوست: {attachedCount}، فایل ناموجود: {missingFileCount}، خطا: {failedCount}",
					cn);

				if (unmatchedUsers.Count > 0)
				{
					await jobLogger?.LogWarningAsync(
						$"تعداد {unmatchedUsers.Count} شناسه کاربر HTS در App یافت نشد. نمونه: {string.Join(", ", unmatchedUsers.Take(20))}",
						0,
						cn);
				}

				await jobLogger?.LogInfoAsync("همگام‌سازی کامنت مدارک با موفقیت انجام شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		[JobHandler("انتقال فایل‌های مدارک مهندسی از HTS")]
		public async Task SyncDocumentFilesFromHts(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync($"شروع انتقال فایل‌های مدارک از {PortalEdmsRoot}", cn);

				var counters = new FileSyncCounters();
				var skippedCount = 0;
				var failedCount = 0;
				var lastId = 0;
				var processed = 0;
				var candidatesExamined = 0;

				while (true)
				{
					// فقط مدارکی که در HTS حداقل یک مسیر/نام فایل دارند — بقیه کار ندارند
					var batch = await htsDb.Hts_Edms_Documents.AsNoTracking()
						.Where(d => d.Document_ID > lastId && (
							(d.Document_FilePath != null && d.Document_FilePath != "")
							|| (d.Document_FileName != null && d.Document_FileName != "")
							|| (d.Document_NativeFilePath != null && d.Document_NativeFilePath != "")
							|| (d.Document_NativeFileName != null && d.Document_NativeFileName != "")
							|| (d.Document_SecondaryFilePath != null && d.Document_SecondaryFilePath != "")
							|| (d.Document_SecondaryFileName != null && d.Document_SecondaryFileName != "")
							|| (d.Document_ReplySheetFilePath != null && d.Document_ReplySheetFilePath != "")
							|| (d.Document_ReplySheetFileName != null && d.Document_ReplySheetFileName != "")
							|| (d.Document_SecondReplySheetFilePath != null && d.Document_SecondReplySheetFilePath != "")
							|| (d.Document_SecondReplySheetFileName != null && d.Document_SecondReplySheetFileName != "")))
						.OrderBy(d => d.Document_ID)
						.Take(PageSize)
						.ToListAsync(cn);

					if (batch.Count == 0)
						break;

					lastId = batch[^1].Document_ID;
					candidatesExamined += batch.Count;

					var htsIds = batch.Select(d => (long)d.Document_ID).ToList();
					var appDocuments = await unitOfWork.Repository<Document>().Table
						.Where(d => htsIds.Contains(d.HtsId))
						.ToListAsync(cn);
					var byHtsId = appDocuments
						.GroupBy(d => d.HtsId)
						.ToDictionary(g => g.Key, g => g.First());

					var batchChanged = false;

					foreach (var hts in batch)
					{
						try
						{
							if (!byHtsId.TryGetValue(hts.Document_ID, out var document) || !document.Id.HasValue)
							{
								skippedCount++;
								continue;
							}

							// اسلات‌های لینک‌شده را دوباره بررسی/ثبت نکن — فقط شکاف‌ها
							if (!DocumentNeedsFileSync(document, hts))
							{
								counters.Unchanged += CountLinkedDocumentSlots(document, hts);
								continue;
							}

							processed++;

							document.MainFileId = await SyncFileSlotAsync(
								document.MainFileId,
								hts.Document_FilePath,
								hts.Document_FileName,
								typeof(Document).FullName!,
								nameof(Document.MainFile),
								document.Id,
								jobLogger,
								hts.Document_ID,
								"Main",
								counters,
								cn);

							document.MotherFileId = await SyncFileSlotAsync(
								document.MotherFileId,
								hts.Document_NativeFilePath,
								hts.Document_NativeFileName,
								typeof(Document).FullName!,
								nameof(Document.MotherFile),
								document.Id,
								jobLogger,
								hts.Document_ID,
								"Mother",
								counters,
								cn);

							document.SecondaryFileId = await SyncFileSlotAsync(
								document.SecondaryFileId,
								hts.Document_SecondaryFilePath,
								hts.Document_SecondaryFileName,
								typeof(Document).FullName!,
								nameof(Document.SecondaryFile),
								document.Id,
								jobLogger,
								hts.Document_ID,
								"Secondary",
								counters,
								cn);

							document.ReplySheetId = await SyncFileSlotAsync(
								document.ReplySheetId,
								hts.Document_ReplySheetFilePath,
								hts.Document_ReplySheetFileName,
								typeof(Document).FullName!,
								nameof(Document.ReplySheet),
								document.Id,
								jobLogger,
								hts.Document_ID,
								"ReplySheet",
								counters,
								cn);

							document.SecondReplySheetId = await SyncFileSlotAsync(
								document.SecondReplySheetId,
								hts.Document_SecondReplySheetFilePath,
								hts.Document_SecondReplySheetFileName,
								typeof(Document).FullName!,
								nameof(Document.SecondReplySheet),
								document.Id,
								jobLogger,
								hts.Document_ID,
								"SecondReplySheet",
								counters,
								cn);

							batchChanged = true;
						}
						catch (Exception ex)
						{
							failedCount++;
							await jobLogger?.LogErrorAsync(
								$"خطا در انتقال فایل‌های مدرک HTS Id={hts.Document_ID}: {ex.Message}",
								0,
								cn);
						}
					}

					if (batchChanged)
						await unitOfWork.SaveChangesAsync(cn);
				}

				await ImportCommentAttachmentsFromPortalAsync(jobLogger, counters, cn);

				await jobLogger?.LogInfoAsync(
					$"پایان انتقال فایل‌ها از {PortalEdmsRoot}. بررسی‌شده: {candidatesExamined}، نیازمند انتقال: {processed}، آپلود شده: {counters.Uploaded}، بدون تغییر: {counters.Unchanged}، فایل ناموجود: {counters.Missing}، رد شده به‌خاطر مدرک: {skippedCount}، خطا: {failedCount}",
					cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		private async Task ImportCommentAttachmentsFromPortalAsync(
			IJobLogger? jobLogger,
			FileSyncCounters counters,
			CancellationToken cn)
		{
			await jobLogger?.LogInfoAsync("شروع انتقال پیوست کامنت‌ها از مسیر EDMS", cn);
			var lastId = 0;
			var attached = 0;
			var candidatesExamined = 0;
			var alreadyLinked = 0;

			while (true)
			{
				// فقط کامنت‌های HTS که واقعاً پیوست دارند — نه همهٔ کامنت‌های بدون AttachmentId
				var htsBatch = await htsDb.Hts_Edms_Document_Comments.AsNoTracking()
					.Where(c => c.Document_Comment_ID > lastId && (
						(c.Attachment_FilePath != null && c.Attachment_FilePath != "")
						|| (c.Attachment_FileName != null && c.Attachment_FileName != "")))
					.OrderBy(c => c.Document_Comment_ID)
					.Take(PageSize)
					.ToListAsync(cn);

				if (htsBatch.Count == 0)
					break;

				lastId = htsBatch[^1].Document_Comment_ID;
				candidatesExamined += htsBatch.Count;

				var htsIds = htsBatch.Select(c => (long)c.Document_Comment_ID).ToList();
				var appComments = await unitOfWork.Repository<DocumentComment>().Table
					.Where(c => htsIds.Contains(c.HtsId))
					.ToListAsync(cn);
				var byHtsId = appComments
					.GroupBy(c => c.HtsId)
					.ToDictionary(g => g.Key, g => g.First());

				var batchChanged = false;

				foreach (var hts in htsBatch)
				{
					if (!byHtsId.TryGetValue(hts.Document_Comment_ID, out var comment) || !comment.Id.HasValue)
						continue;

					if (comment.AttachmentId.HasValue)
					{
						alreadyLinked++;
						counters.Unchanged++;
						continue;
					}

					var uploadedId = await TryUploadCommentAttachmentAsync(hts, comment.Id, jobLogger, cn);
					if (uploadedId.HasValue)
					{
						comment.AttachmentId = uploadedId;
						attached++;
						counters.Uploaded++;
						batchChanged = true;
					}
					else
					{
						counters.Missing++;
					}
				}

				if (batchChanged)
					await unitOfWork.SaveChangesAsync(cn);
			}

			await jobLogger?.LogInfoAsync(
				$"پیوست کامنت منتقل شد: {attached} (بررسی HTS با پیوست: {candidatesExamined}، قبلاً لینک‌شده: {alreadyLinked})",
				cn);
		}

		private static Document MapDocument(
			Hts_Edms_Document hts,
			long documentVpisId,
			long? projectId,
			UserMaps users,
			HashSet<string> unmatchedUsers)
		{
			var createdOn = CombineDateAndTime(hts.CreatedDate, hts.CreatedTime);
			var reviewedOn = NormalizeDate(hts.CheckedDate);
			var approvedOn = NormalizeDate(hts.ApprovedDate);
			var publication = NormalizeDate(hts.Due_Date);

			return new Document
			{
				HtsId = hts.Document_ID,
				ProjectId = projectId,
				DocumentVpisId = documentVpisId,
				GoalOfProduction = MapGoalOfProduction(hts.DocPoi_FK),
				HourPrePerson = hts.ConsumedManHours,
				PublicationMiladiDate = publication,
				PublicationShamsiDate = Truncate(hts.Due_Date_Shamsi, 30),
				ReviewerId = ResolveHtsUser(hts.CheckedUser_FK, users, unmatchedUsers),
				RevieweMiladiDateTime = reviewedOn,
				RevieweShamsiDateTime = FormatShamsiDateTime(reviewedOn, hts.CheckedDate_Shamsi),
				ApproverId = ResolveHtsUser(hts.ApprovedUser_FK, users, unmatchedUsers),
				ApprovedMiladiDateTime = approvedOn,
				ApprovedShamsiDateTime = FormatShamsiDateTime(approvedOn, hts.ApprovedDate_Shamsi),
				Status = MapDocumentStatus(
					hts.LastStatusId,
					createdUserId: hts.LastStatusUserId,
					checkedUserId: hts.CheckedUser_FK,
					approvedUserId: hts.ApprovedUser_FK),
				Revision = hts.Revision ?? 0,
				IsLatest = hts.IsLatest,
				CreatedById = ResolveHtsUser(hts.CreatedUser_FK, users, unmatchedUsers),
				CreatedByName = Truncate(ResolveCreatedByName(hts.CreatedUser_FK, users), 150),
				CreatedOnMiladiDateTime = createdOn,
				CreatedOnShamsiDateTime = FormatShamsiDateTime(createdOn, hts.CreatedDate_Shamsi),
				IsActive = IsActiveEnum.Active
			};
		}

		private static void ApplyDocumentFields(Document target, Document source)
		{
			target.HtsId = source.HtsId;
			target.ProjectId = source.ProjectId;
			target.DocumentVpisId = source.DocumentVpisId;
			target.GoalOfProduction = source.GoalOfProduction;
			target.HourPrePerson = source.HourPrePerson;
			target.PublicationMiladiDate = source.PublicationMiladiDate;
			target.PublicationShamsiDate = source.PublicationShamsiDate;
			target.ReviewerId = source.ReviewerId;
			target.RevieweMiladiDateTime = source.RevieweMiladiDateTime;
			target.RevieweShamsiDateTime = source.RevieweShamsiDateTime;
			target.ApproverId = source.ApproverId;
			target.ApprovedMiladiDateTime = source.ApprovedMiladiDateTime;
			target.ApprovedShamsiDateTime = source.ApprovedShamsiDateTime;
			target.Status = source.Status;
			target.Revision = source.Revision;
			target.IsLatest = source.IsLatest;
			target.CreatedById = source.CreatedById;
			target.CreatedByName = source.CreatedByName;
			target.CreatedOnMiladiDateTime = source.CreatedOnMiladiDateTime;
			target.CreatedOnShamsiDateTime = source.CreatedOnShamsiDateTime;
		}

		private static DocumentComment MapComment(
			Hts_Edms_Document_Comment hts,
			long documentId,
			UserMaps users,
			HashSet<string> unmatchedUsers,
			short? checkedUserId,
			short? approvedUserId)
		{
			var status = MapDocumentStatus(
				hts.Document_Status_FK,
				hts.IsForDcc,
				hts.CreatedUser_FK,
				checkedUserId,
				approvedUserId);
			var comment = Truncate(hts.Comment, CommentMaxLength);
			var createdOn = hts.CreationDateTime ?? CombineDateAndTime(hts.CreatedDate, hts.CreatedTime);

			return new DocumentComment
			{
				HtsId = hts.Document_Comment_ID,
				DocumentId = documentId,
				Comment = comment,
				Status = status,
				LegalHolder = MapLegalHolder(hts.HoldCause_FK),
				HOLDOwnerId = null,
				HoldDetails = status is DocumentStatusEnums.Hold or DocumentStatusEnums.HoldByProject
					? comment
					: null,
				CreatedById = ResolveHtsUser(hts.CreatedUser_FK, users, unmatchedUsers),
				CreatedByName = Truncate(ResolveCreatedByName(hts.CreatedUser_FK, users), 150),
				CreatedOnMiladiDateTime = createdOn,
				CreatedOnShamsiDateTime = FormatShamsiDateTime(createdOn, hts.CreatedDate_Shamsi)
					?? Truncate(hts.CreationDateTimeInText, 30),
				IsActive = IsActiveEnum.Active
			};
		}

		private static void ApplyCommentFields(DocumentComment target, DocumentComment source)
		{
			target.HtsId = source.HtsId;
			target.DocumentId = source.DocumentId;
			target.Comment = source.Comment;
			target.Status = source.Status;
			target.LegalHolder = source.LegalHolder;
			target.HOLDOwnerId = source.HOLDOwnerId;
			target.HoldDetails = source.HoldDetails;
			target.CreatedById = source.CreatedById;
			target.CreatedByName = source.CreatedByName;
			target.CreatedOnMiladiDateTime = source.CreatedOnMiladiDateTime;
			target.CreatedOnShamsiDateTime = source.CreatedOnShamsiDateTime;
		}

		private async Task<long?> TryUploadCommentAttachmentAsync(
			Hts_Edms_Document_Comment hts,
			long? entityId,
			IJobLogger? jobLogger,
			CancellationToken cn)
		{
			if (!HasCommentAttachment(hts))
				return null;

			if (string.IsNullOrWhiteSpace(hts.Attachment_FilePath))
				return null;

			var sourcePath = LocatePortalFile(hts.Attachment_FilePath);
			if (sourcePath == null)
			{
				await jobLogger?.LogWarningAsync(
					$"فایل پیوست کامنت HTS یافت نشد. CommentId={hts.Document_Comment_ID}, Path={ResolvePortalEdmsPath(hts.Attachment_FilePath) ?? hts.Attachment_FilePath}",
					0,
					cn);
				return null;
			}

			var info = new FileInfo(sourcePath);
			if (info.Length == 0)
			{
				await jobLogger?.LogWarningAsync(
					$"فایل پیوست کامنت HTS خالی است. CommentId={hts.Document_Comment_ID}, Path={sourcePath}",
					0,
					cn);
				return null;
			}

			var fileName = string.IsNullOrWhiteSpace(hts.Attachment_FileName)
				? $"comment_{hts.Document_Comment_ID}{Path.GetExtension(sourcePath)}"
				: hts.Attachment_FileName;

			var uploaded = await fileService.RegisterExistingAsync(
				sourcePath,
				fileName,
				typeof(DocumentComment).FullName,
				nameof(DocumentComment.Attachment),
				entityId,
				cn);

			return uploaded.Id;
		}

		private async Task<long?> SyncFileSlotAsync(
			long? currentFileId,
			string? htsPath,
			string? htsName,
			string entityType,
			string entityPropName,
			long? entityId,
			IJobLogger? jobLogger,
			int htsDocumentId,
			string slotName,
			FileSyncCounters counters,
			CancellationToken cn)
		{
			// قبلاً ثبت شده — دیسک را دوباره چک نکن و دوباره ثبت نکن
			if (currentFileId.HasValue)
			{
				counters.Unchanged++;
				return currentFileId;
			}

			if (string.IsNullOrWhiteSpace(htsPath) && string.IsNullOrWhiteSpace(htsName))
				return currentFileId;

			var sourcePath = LocatePortalFile(htsPath);

			if (string.IsNullOrWhiteSpace(sourcePath))
			{
				counters.Missing++;
				await jobLogger?.LogWarningAsync(
					$"فایل مدرک HTS یافت نشد. DocumentId={htsDocumentId}, Slot={slotName}, Path={ResolvePortalEdmsPath(htsPath) ?? htsPath}",
					0,
					cn);
				return currentFileId;
			}

			var fileInfo = new FileInfo(sourcePath);
			if (fileInfo.Length == 0)
			{
				counters.Missing++;
				await jobLogger?.LogWarningAsync(
					$"فایل مدرک HTS خالی است. DocumentId={htsDocumentId}, Slot={slotName}, Path={sourcePath}",
					0,
					cn);
				return currentFileId;
			}

			var fileName = string.IsNullOrWhiteSpace(htsName)
				? fileInfo.Name
				: htsName.Trim();
			if (string.IsNullOrWhiteSpace(fileName))
				fileName = $"document_{htsDocumentId}_{slotName}";

			var uploaded = await fileService.RegisterExistingAsync(
				sourcePath,
				fileName,
				entityType,
				entityPropName,
				entityId,
				cn);

			counters.Uploaded++;
			return uploaded.Id;
		}

		private static bool DocumentNeedsFileSync(Document document, Hts_Edms_Document hts) =>
			SlotNeedsSync(document.MainFileId, hts.Document_FilePath, hts.Document_FileName)
			|| SlotNeedsSync(document.MotherFileId, hts.Document_NativeFilePath, hts.Document_NativeFileName)
			|| SlotNeedsSync(document.SecondaryFileId, hts.Document_SecondaryFilePath, hts.Document_SecondaryFileName)
			|| SlotNeedsSync(document.ReplySheetId, hts.Document_ReplySheetFilePath, hts.Document_ReplySheetFileName)
			|| SlotNeedsSync(document.SecondReplySheetId, hts.Document_SecondReplySheetFilePath, hts.Document_SecondReplySheetFileName);

		private static bool SlotNeedsSync(long? currentFileId, string? htsPath, string? htsName) =>
			!currentFileId.HasValue
			&& (!string.IsNullOrWhiteSpace(htsPath) || !string.IsNullOrWhiteSpace(htsName));

		private static int CountLinkedDocumentSlots(Document document, Hts_Edms_Document hts)
		{
			var count = 0;
			if (HasHtsFileSlot(hts.Document_FilePath, hts.Document_FileName) && document.MainFileId.HasValue)
				count++;
			if (HasHtsFileSlot(hts.Document_NativeFilePath, hts.Document_NativeFileName) && document.MotherFileId.HasValue)
				count++;
			if (HasHtsFileSlot(hts.Document_SecondaryFilePath, hts.Document_SecondaryFileName) && document.SecondaryFileId.HasValue)
				count++;
			if (HasHtsFileSlot(hts.Document_ReplySheetFilePath, hts.Document_ReplySheetFileName) && document.ReplySheetId.HasValue)
				count++;
			if (HasHtsFileSlot(hts.Document_SecondReplySheetFilePath, hts.Document_SecondReplySheetFileName) && document.SecondReplySheetId.HasValue)
				count++;
			return count;
		}

		private static bool HasHtsFileSlot(string? htsPath, string? htsName) =>
			!string.IsNullOrWhiteSpace(htsPath) || !string.IsNullOrWhiteSpace(htsName);

		private static bool HasCommentAttachment(Hts_Edms_Document_Comment hts) =>
			!string.IsNullOrWhiteSpace(hts.Attachment_FilePath) || !string.IsNullOrWhiteSpace(hts.Attachment_FileName);

		private static DocumentGoalOfProductionEnum? MapGoalOfProduction(byte docPoiFk)
		{
			var value = docPoiFk - 1;
			return Enum.IsDefined(typeof(DocumentGoalOfProductionEnum), value)
				? (DocumentGoalOfProductionEnum)value
				: null;
		}

		private static DocumentStatusEnums? MapDocumentStatus(
			byte? statusFk,
			bool isForDcc = false,
			short? createdUserId = null,
			short? checkedUserId = null,
			short? approvedUserId = null)
		{
			if (!statusFk.HasValue)
				return null;

			if (statusFk.Value == 2)
				return MapHtsApproveStatus(createdUserId, checkedUserId, approvedUserId);

			return statusFk.Value switch
			{
				1 => DocumentStatusEnums.RejectByDcc,
				3 => isForDcc
					? DocumentStatusEnums.CommentedByDcc
					: DocumentStatusEnums.CommentedByApprover,
				4 => MapCommentedStatus(isForDcc, createdUserId, checkedUserId, approvedUserId),
				5 => DocumentStatusEnums.Issue,
				6 => DocumentStatusEnums.ApprovedByDcc,
				7 => DocumentStatusEnums.RejectByClient,
				8 => DocumentStatusEnums.ApproveByClient,
				9 => DocumentStatusEnums.ApprovedAsNoteByClient,
				10 => DocumentStatusEnums.CommentedByClient,
				11 => DocumentStatusEnums.Hold,
				12 => DocumentStatusEnums.UnHold,
				13 => DocumentStatusEnums.NotReview,
				14 => DocumentStatusEnums.UnHold,
				15 => DocumentStatusEnums.SendEmailToClient,
				16 => DocumentStatusEnums.ReIssued,
				17 => DocumentStatusEnums.Commited,
				18 => DocumentStatusEnums.UnCommit,
				19 => DocumentStatusEnums.ApprovedBySale,
				20 => DocumentStatusEnums.CommentedBySale,
				21 => DocumentStatusEnums.AsBuild,
				22 => DocumentStatusEnums.ReplaySheet,
				23 => DocumentStatusEnums.ReplaySheetFromClient,
				24 => DocumentStatusEnums.IssueForClient,
				25 => DocumentStatusEnums.RejectedBySale,
				26 => DocumentStatusEnums.Archived,
				27 => DocumentStatusEnums.HoldByProject,
				28 => DocumentStatusEnums.UnHold,
				29 => DocumentStatusEnums.ConvertToGeneralPackage,
				_ => null
			};
		}

		private static DocumentStatusEnums MapHtsApproveStatus(
			short? createdUserId,
			short? checkedUserId,
			short? approvedUserId)
		{
			if (!createdUserId.HasValue || createdUserId.Value == 0)
				return DocumentStatusEnums.ApproveByApprover;

			var createdByReviewer = checkedUserId is > 0 && createdUserId.Value == checkedUserId.Value;
			var createdByApprover = approvedUserId is > 0 && createdUserId.Value == approvedUserId.Value;

			if (createdByReviewer && !createdByApprover)
				return DocumentStatusEnums.ApproveByReviewer;

			return DocumentStatusEnums.ApproveByApprover;
		}

		private static DocumentStatusEnums MapCommentedStatus(
			bool isForDcc,
			short? createdUserId,
			short? checkedUserId,
			short? approvedUserId)
		{
			if (isForDcc)
				return DocumentStatusEnums.CommentedByDcc;

			var createdByApprover = approvedUserId is > 0 && createdUserId == approvedUserId;
			var createdByReviewer = checkedUserId is > 0 && createdUserId == checkedUserId;

			if (createdByApprover && !createdByReviewer)
				return DocumentStatusEnums.CommentedByApprover;

			if (createdByReviewer && !createdByApprover)
				return DocumentStatusEnums.CommentedByReviewer;

			if (createdByApprover)
				return DocumentStatusEnums.CommentedByApprover;

			return DocumentStatusEnums.CommentedByReviewer;
		}

		private static DocumentLegalHolderEnum? MapLegalHolder(short? holdCauseFk)
		{
			if (!holdCauseFk.HasValue)
				return null;

			var value = holdCauseFk.Value - HoldCauseLookupOffset;
			return Enum.IsDefined(typeof(DocumentLegalHolderEnum), value)
				? (DocumentLegalHolderEnum)value
				: null;
		}

		private async Task<UserMaps> BuildUserMapsAsync(IJobLogger? jobLogger, CancellationToken cn)
		{
			var appUsers = await appContext.Users.AsNoTracking()
				.Where(u => u.Id.HasValue)
				.Select(u => new { u.Id, u.Username, u.PartyId, u.Name, u.NameFa })
				.ToListAsync(cn);

			var usersByUsername = new Dictionary<string, AppUserInfo>(StringComparer.OrdinalIgnoreCase);
			var usersByPartyId = new Dictionary<long, AppUserInfo>();
			var usersById = new Dictionary<long, AppUserInfo>();

			foreach (var user in appUsers)
			{
				var info = new AppUserInfo(user.Id!.Value, user.NameFa ?? user.Name ?? user.Username);
				usersById[info.Id] = info;
				if (!string.IsNullOrWhiteSpace(user.Username))
					usersByUsername[user.Username.Trim()] = info;
				if (user.PartyId.HasValue)
					usersByPartyId[user.PartyId.Value] = info;
			}

			var personels = await unitOfWork.Repository<Personel>().TableNoTracking
				.Where(p => p.HamkaranId.HasValue && p.PartyId.HasValue)
				.Select(p => new { p.HamkaranId, p.PartyId })
				.ToListAsync(cn);

			var hamkaranToUser = new Dictionary<long, long>();
			foreach (var personel in personels)
			{
				if (usersByPartyId.TryGetValue(personel.PartyId!.Value, out var user))
					hamkaranToUser[personel.HamkaranId!.Value] = user.Id;
			}

			var htsPersonels = await htsDb.Hts_HRM_Personels.AsNoTracking().ToListAsync(cn);
			var htsUsers = await htsDb.Hts_Gnr_Users.AsNoTracking().ToListAsync(cn);

			var personelToUser = new Dictionary<short, long>();
			var htsUserToAppUser = new Dictionary<short, long>();
			var htsUserNames = new Dictionary<short, string>();

			foreach (var personel in htsPersonels)
			{
				if (personel.Hamkaran_Personel_FK.HasValue
					&& hamkaranToUser.TryGetValue(personel.Hamkaran_Personel_FK.Value, out var userId))
				{
					personelToUser[personel.Personel_ID] = userId;
				}
			}

			foreach (var htsUser in htsUsers)
			{
				if (!string.IsNullOrWhiteSpace(htsUser.FullName))
					htsUserNames[htsUser.User_ID] = htsUser.FullName.Trim();

				if (!string.IsNullOrWhiteSpace(htsUser.Username)
					&& usersByUsername.TryGetValue(htsUser.Username.Trim(), out var byName))
				{
					htsUserToAppUser[htsUser.User_ID] = byName.Id;
					if (htsUser.Personel_FK.HasValue && !personelToUser.ContainsKey(htsUser.Personel_FK.Value))
						personelToUser[htsUser.Personel_FK.Value] = byName.Id;
				}
				else if (htsUser.Personel_FK.HasValue
					&& personelToUser.TryGetValue(htsUser.Personel_FK.Value, out var fromPersonel))
				{
					htsUserToAppUser[htsUser.User_ID] = fromPersonel;
				}
			}

			await jobLogger?.LogInfoAsync(
				$"نگاشت کاربر: {personelToUser.Count} پرسنل و {htsUserToAppUser.Count} کاربر HTS",
				cn);

			return new UserMaps(personelToUser, htsUserToAppUser, usersById, htsUserNames);
		}

		private static long? ResolveHtsUser(short? htsUserId, UserMaps users, HashSet<string> unmatched)
		{
			if (!htsUserId.HasValue || htsUserId.Value == 0)
				return null;

			if (users.HtsUserToAppUser.TryGetValue(htsUserId.Value, out var userId))
				return userId;

			unmatched.Add($"U:{htsUserId.Value}");
			return null;
		}

		private static string? ResolveCreatedByName(short htsUserId, UserMaps users)
		{
			if (htsUserId == 0)
				return null;

			if (users.HtsUserToAppUser.TryGetValue(htsUserId, out var appUserId)
				&& users.UsersById.TryGetValue(appUserId, out var appUser)
				&& !string.IsNullOrWhiteSpace(appUser.DisplayName))
			{
				return appUser.DisplayName;
			}

			if (users.HtsUserNames.TryGetValue(htsUserId, out var htsName) && !string.IsNullOrWhiteSpace(htsName))
				return htsName.Trim();

			return null;
		}

		private static DateTime? CombineDateAndTime(DateTime? date, string? time)
		{
			var normalized = NormalizeDate(date);
			if (!normalized.HasValue)
				return null;

			if (string.IsNullOrWhiteSpace(time) || !TimeSpan.TryParse(time.Trim(), out var timeOfDay))
				return normalized;

			return normalized.Value.Date.Add(timeOfDay);
		}

		private static DateTime? NormalizeDate(DateTime? date)
		{
			if (!date.HasValue || date.Value == default || date.Value.Year <= 1900)
				return null;

			return date.Value;
		}

		private static string? FormatShamsiDateTime(DateTime? miladi, string? fallbackShamsi)
		{
			if (miladi.HasValue)
				return Truncate(miladi.Value.ToShamsiDateTime(), 30);

			return Truncate(fallbackShamsi, 30);
		}

		private static string? Truncate(string? value, int maxLength)
		{
			if (string.IsNullOrEmpty(value))
				return value;

			var trimmed = value.Trim();
			return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
		}

		private sealed record UserMaps(
			Dictionary<short, long> PersonelToUser,
			Dictionary<short, long> HtsUserToAppUser,
			Dictionary<long, AppUserInfo> UsersById,
			Dictionary<short, string> HtsUserNames);

		private sealed record AppUserInfo(long Id, string? DisplayName);

		private sealed class FileSyncCounters
		{
			public int Uploaded { get; set; }
			public int Unchanged { get; set; }
			public int Missing { get; set; }
		}
	}
}
