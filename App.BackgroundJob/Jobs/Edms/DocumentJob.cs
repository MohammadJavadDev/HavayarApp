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
		private const int PageSize = 500;
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

					foreach (var hts in batch)
					{
						try
						{
							if (!documentByHtsId.TryGetValue(hts.Document_FK, out var documentId))
							{
								skippedCount++;
								continue;
							}

							var mapped = MapComment(hts, documentId, userMaps, unmatchedUsers);

							if (byHtsId.TryGetValue(hts.Document_Comment_ID, out var current))
							{
								ApplyCommentFields(current, mapped);
								if (!current.AttachmentId.HasValue)
								{
									var uploadedId = await TryUploadCommentAttachmentAsync(
										hts,
										current.Id,
										jobLogger,
										cn);
									if (uploadedId.HasValue)
									{
										current.AttachmentId = uploadedId;
										attachedCount++;
									}
									else if (HasCommentAttachment(hts))
									{
										missingFileCount++;
									}
								}

								updatedCount++;
							}
							else
							{
								var uploadedId = await TryUploadCommentAttachmentAsync(hts, null, jobLogger, cn);
								if (uploadedId.HasValue)
								{
									mapped.AttachmentId = uploadedId;
									attachedCount++;
								}
								else if (HasCommentAttachment(hts))
								{
									missingFileCount++;
								}

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
				await jobLogger?.LogInfoAsync("شروع انتقال فایل‌های مدارک مهندسی از HTS", cn);

				var counters = new FileSyncCounters();
				var skippedCount = 0;
				var failedCount = 0;
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
					var appDocuments = await unitOfWork.Repository<Document>().Table
						.Where(d => htsIds.Contains(d.HtsId))
						.ToListAsync(cn);
					var byHtsId = appDocuments
						.GroupBy(d => d.HtsId)
						.ToDictionary(g => g.Key, g => g.First());

					var fileIds = appDocuments
						.SelectMany(d => new long?[]
						{
							d.MainFileId,
							d.MotherFileId,
							d.SecondaryFileId,
							d.ReplySheetId,
							d.SecondReplySheetId
						})
						.Where(id => id.HasValue)
						.Select(id => id!.Value)
						.Distinct()
						.ToList();

					var fileMeta = await LoadFileMetaAsync(fileIds, cn);

					foreach (var hts in batch)
					{
						try
						{
							if (!byHtsId.TryGetValue(hts.Document_ID, out var document) || !document.Id.HasValue)
							{
								skippedCount++;
								continue;
							}

							document.MainFileId = await SyncFileSlotAsync(
								document.MainFileId,
								hts.Document_FilePath,
								hts.Document_FileName,
								hts.Document_FileSize,
								fileMeta,
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
								hts.Document_NativeFileSize,
								fileMeta,
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
								hts.Document_SecondaryFileSize,
								fileMeta,
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
								hts.Document_ReplySheetFileSize,
								fileMeta,
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
								hts.Document_SecondReplySheetFileSize,
								fileMeta,
								typeof(Document).FullName!,
								nameof(Document.SecondReplySheet),
								document.Id,
								jobLogger,
								hts.Document_ID,
								"SecondReplySheet",
								counters,
								cn);
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

					await unitOfWork.SaveChangesAsync(cn);
				}

				await jobLogger?.LogInfoAsync(
					$"پایان انتقال فایل‌ها. مدارک: {processed}، آپلود شده: {counters.Uploaded}، بدون تغییر: {counters.Unchanged}، فایل ناموجود: {counters.Missing}، رد شده به‌خاطر مدرک: {skippedCount}، خطا: {failedCount}",
					cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
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
				Status = MapDocumentStatus(hts.LastStatusId),
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
			HashSet<string> unmatchedUsers)
		{
			var status = MapDocumentStatus(hts.Document_Status_FK, hts.IsForDcc);
			var comment = Truncate(hts.Comment, CommentMaxLength);
			var createdOn = hts.CreationDateTime ?? CombineDateAndTime(hts.CreatedDate, hts.CreatedTime);

			return new DocumentComment
			{
				HtsId = hts.Document_Comment_ID,
				DocumentId = documentId,
				Comment = comment,
				Status = status,
				LegalHolder = MapLegalHolder(hts.HoldCause_FK),
				HOLDOwnerId = ResolveHtsUser(hts.HoldResponsibleId, users, unmatchedUsers),
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

			if (string.IsNullOrWhiteSpace(hts.Attachment_FilePath) || !File.Exists(hts.Attachment_FilePath))
			{
				await jobLogger?.LogWarningAsync(
					$"فایل پیوست کامنت HTS یافت نشد. CommentId={hts.Document_Comment_ID}, Path={hts.Attachment_FilePath}",
					0,
					cn);
				return null;
			}

			var bytes = await File.ReadAllBytesAsync(hts.Attachment_FilePath, cn);
			if (bytes.Length == 0)
			{
				await jobLogger?.LogWarningAsync(
					$"فایل پیوست کامنت HTS خالی است. CommentId={hts.Document_Comment_ID}, Path={hts.Attachment_FilePath}",
					0,
					cn);
				return null;
			}

			var fileName = string.IsNullOrWhiteSpace(hts.Attachment_FileName)
				? $"comment_{hts.Document_Comment_ID}{Path.GetExtension(hts.Attachment_FilePath)}"
				: hts.Attachment_FileName;

			var uploaded = await fileService.UploadAsync(
				bytes,
				fileName,
				null,
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
			long? htsSize,
			Dictionary<long, FileMeta> fileMeta,
			string entityType,
			string entityPropName,
			long? entityId,
			IJobLogger? jobLogger,
			int htsDocumentId,
			string slotName,
			FileSyncCounters counters,
			CancellationToken cn)
		{
			if (string.IsNullOrWhiteSpace(htsPath) && string.IsNullOrWhiteSpace(htsName))
				return currentFileId;

			if (currentFileId.HasValue
				&& fileMeta.TryGetValue(currentFileId.Value, out var existing)
				&& FileMatches(existing, htsName, htsSize))
			{
				counters.Unchanged++;
				return currentFileId;
			}

			if (string.IsNullOrWhiteSpace(htsPath) || !File.Exists(htsPath))
			{
				counters.Missing++;
				await jobLogger?.LogWarningAsync(
					$"فایل مدرک HTS یافت نشد. DocumentId={htsDocumentId}, Slot={slotName}, Path={htsPath}",
					0,
					cn);
				return currentFileId;
			}

			var bytes = await File.ReadAllBytesAsync(htsPath, cn);
			if (bytes.Length == 0)
			{
				counters.Missing++;
				await jobLogger?.LogWarningAsync(
					$"فایل مدرک HTS خالی است. DocumentId={htsDocumentId}, Slot={slotName}, Path={htsPath}",
					0,
					cn);
				return currentFileId;
			}

			var fileName = string.IsNullOrWhiteSpace(htsName)
				? Path.GetFileName(htsPath)
				: htsName.Trim();
			if (string.IsNullOrWhiteSpace(fileName))
				fileName = $"document_{htsDocumentId}_{slotName}";

			var uploaded = await fileService.UploadAsync(
				bytes,
				fileName,
				null,
				entityType,
				entityPropName,
				entityId,
				cn);

			if (currentFileId.HasValue)
			{
				try
				{
					await fileService.DeleteAsync(currentFileId.Value, cn);
				}
				catch (Exception ex)
				{
					await jobLogger?.LogWarningAsync(
						$"حذف فایل قبلی مدرک HTS Id={htsDocumentId} Slot={slotName} ناموفق بود: {ex.Message}",
						0,
						cn);
				}
			}

			if (uploaded.Id.HasValue)
				fileMeta[uploaded.Id.Value] = new FileMeta(uploaded.OriginalName, uploaded.Size);

			counters.Uploaded++;
			return uploaded.Id;
		}

		private async Task<Dictionary<long, FileMeta>> LoadFileMetaAsync(List<long> fileIds, CancellationToken cn)
		{
			var result = new Dictionary<long, FileMeta>();
			if (fileIds.Count == 0)
				return result;

			const int chunkSize = 1000;
			for (var i = 0; i < fileIds.Count; i += chunkSize)
			{
				var chunk = fileIds.Skip(i).Take(chunkSize).ToList();
				var rows = await unitOfWork.Repository<FileEntity>().TableNoTracking
					.Where(f => f.Id.HasValue && chunk.Contains(f.Id.Value))
					.Select(f => new { Id = f.Id!.Value, f.OriginalName, f.Size })
					.ToListAsync(cn);

				foreach (var row in rows)
					result[row.Id] = new FileMeta(row.OriginalName, row.Size);
			}

			return result;
		}

		private static bool HasCommentAttachment(Hts_Edms_Document_Comment hts) =>
			!string.IsNullOrWhiteSpace(hts.Attachment_FilePath) || !string.IsNullOrWhiteSpace(hts.Attachment_FileName);

		private static bool FileMatches(FileMeta existing, string? htsName, long? htsSize)
		{
			if (!string.IsNullOrWhiteSpace(htsName)
				&& !string.Equals(existing.OriginalName, htsName.Trim(), StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			if (htsSize.HasValue && existing.Size != htsSize.Value)
				return false;

			return !string.IsNullOrWhiteSpace(existing.OriginalName) || existing.Size > 0;
		}

		private static DocumentGoalOfProductionEnum? MapGoalOfProduction(byte docPoiFk)
		{
			var value = docPoiFk - 1;
			return Enum.IsDefined(typeof(DocumentGoalOfProductionEnum), value)
				? (DocumentGoalOfProductionEnum)value
				: null;
		}

		private static DocumentStatusEnums? MapDocumentStatus(byte? statusFk, bool isForDcc = false)
		{
			if (!statusFk.HasValue)
				return null;

			return statusFk.Value switch
			{
				1 => DocumentStatusEnums.RejectByDcc,
				2 => DocumentStatusEnums.ApproveByApprover,
				3 => DocumentStatusEnums.CommentedByApprover,
				4 => isForDcc ? DocumentStatusEnums.CommentedByDcc : DocumentStatusEnums.CommentedByReviewer,
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

		private sealed record FileMeta(string? OriginalName, long Size);
	}
}
