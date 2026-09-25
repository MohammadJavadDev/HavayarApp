using App.BackgroundJob.Models;
using Common.Attributes;
using Data;
using Data.Contracts;
using Data.Repositories;
using Entities.App.Hrm;
using Entities.App.Inv;
using Entities.App.Inv.Enums;
using Entities.Base;
using Entities.Hts.Inv;
using Microsoft.EntityFrameworkCore;
using Services.FileServices;
using Services.Job;


namespace App.BackgroundJob.Jobs.Inv
{

	public class PartJob(ApplicationDbContext dbContext, RahkaranDbContext Rdb, IUnitOfWork unitOfWork, HtsDbContext Hdb,
		IFileService fileService)
	{
		[JobHandler("افزودن اطلاعات کالا از راهکاران")]
		public async Task AddPartsFromRahkaran(CancellationToken cn = default)
		{
		 
			var unitData = await Rdb.RahkaranUnits.AsNoTracking().ToListAsync(cn);
			var existUnitData = await unitOfWork.Repository<PartUnit>().Table.ToListAsync(cn);
			var existUnitDict = existUnitData.ToDictionary(x => x.HamkaranId);

			var newUnits = new List<PartUnit>();
			foreach (var unit in unitData)
			{
				if (existUnitDict.TryGetValue(unit.UnitID, out var eunit))
					eunit.Title = unit.Name;
				else
					newUnits.Add(new PartUnit { Title = unit.Name, HamkaranId = unit.UnitID });
			}
			if (newUnits.Any())
				await unitOfWork.Repository<PartUnit>().AddRangeAsync(newUnits, cn, false);
			await unitOfWork.SaveChangesAsync(cn);

	 
			var rahkaranPartsSql = @"
SELECT * FROM OPENQUERY(ERPS, '
    SELECT 
        PartID,
        Code,
        Name,
        LatinName,
        PropertiesComment,
        TechnicalSpecification,
        PartType,
        MajorUnitRef
    FROM Erps.LGS3.Part
') AS r
WHERE NOT EXISTS (SELECT 1 FROM Inv.Part p WHERE p.HamkaranId = r.PartID)
   OR EXISTS (
        SELECT 1 FROM Inv.Part p 
        WHERE p.HamkaranId = r.PartID 
        AND (
            ISNULL(p.Code,'''') COLLATE SQL_Latin1_General_CP1_CI_AS <> ISNULL(r.Code,'''')
            OR ISNULL(p.Name,'''') COLLATE SQL_Latin1_General_CP1_CI_AS <> ISNULL(r.Name,'''')
            OR ISNULL(p.LatinTitle,'''') COLLATE SQL_Latin1_General_CP1_CI_AS <> ISNULL(r.LatinName,'''')
            OR ISNULL(p.Description,'''') COLLATE SQL_Latin1_General_CP1_CI_AS <> ISNULL(r.PropertiesComment,'''')
            OR ISNULL(p.Number,'''') COLLATE SQL_Latin1_General_CP1_CI_AS <> ISNULL(r.TechnicalSpecification,'''')
            OR ISNULL(CAST(p.Type AS INT),-1) <> ISNULL(r.PartType,-1)
        )
   )
OPTION (RECOMPILE);
";
			dbContext.Database.SetCommandTimeout(0);
			var rahkaranPartsData = await dbContext.Database.SqlQueryRaw<RahkaranPartDto>(rahkaranPartsSql).ToListAsync(cn);


			var existParts = await unitOfWork.Repository<Part>().Table
				.ToArrayAsync(cn);


			var appPartsDict = existParts
				.Where(c => c.HamkaranId != null)
				.ToDictionary(x => x.HamkaranId!.Value, x => x );

			var newParts = new List<Part>();
			foreach (var r in rahkaranPartsData)
			{
				if (!appPartsDict.TryGetValue(r.PartID, out var existPart))
				{
					var existByCode = existParts.FirstOrDefault(c => c.Code == r.Code);
					if (existByCode is null)
					{
						newParts.Add(new Part
						{
							HamkaranId = r.PartID,
							Code = r.Code,
							Name = r.Name ?? "",
							LatinTitle = r.LatinName,
							Type = r.PartType.HasValue ? (PartTypeEnum)r.PartType.Value : null,
							Description = r.PropertiesComment,
							Number = r.TechnicalSpecification,
							UnitId = existUnitDict.TryGetValue(r.MajorUnitRef ?? 0, out var u) ? u.Id : (long?)null
						});
					}
					else
					{
						existByCode.HamkaranId = r.PartID;
					}
				}
				else
				{
					existPart.Code = r.Code;
					existPart.Name = r.Name ?? existPart.Name;
					existPart.LatinTitle = r.LatinName;
					existPart.Type = r.PartType.HasValue ? (PartTypeEnum)r.PartType.Value : existPart.Type;
					existPart.Description = r.PropertiesComment;
					existPart.Number = r.TechnicalSpecification;
					existPart.UnitId = existUnitDict.TryGetValue(r.MajorUnitRef ?? 0, out var u) ? u.Id : (long?)null;
				}
			}

			if (newParts.Any())
				await unitOfWork.Repository<Part>().AddRangeAsync(newParts, cn, false);
			await unitOfWork.SaveChangesAsync(cn);

		     await SyncHtsSupplementaryInfoForParts(newParts.Select(p => p.HamkaranId!.Value).ToList(), cn);
		}

		/// <summary>
		/// همگام‌سازی اطلاعات تکمیلی از HTS Inv_Part برای کالاهای مشخص‌شده
		/// </summary>
		private async Task SyncHtsSupplementaryInfoForParts(List<long> hamkaranIds, CancellationToken cn)
		{
			if (hamkaranIds.Count == 0) return;

			var idSet = hamkaranIds.ToHashSet();
			var htsParts = await Hdb.Hts_Inv_Parts.AsNoTracking()
				.Where(hp => hp.Hamkaran_Part_FK != null && idSet.Contains(hp.Hamkaran_Part_FK.Value))
				.ToListAsync(cn);

			var appParts = await unitOfWork.Repository<Part>().Table
				.Where(p => p.HamkaranId != null && idSet.Contains(p.HamkaranId.Value))
				.ToListAsync(cn);
			var appPartsByHamkaran = appParts
				.GroupBy(p => p.HamkaranId!.Value)
				.ToDictionary(g => g.Key, g => g.First());
			var usedHtsIds = appParts.Where(p => p.HtsId != 0).Select(p => p.HtsId).ToHashSet();

			foreach (var hts in htsParts)
			{
				if (hts.Hamkaran_Part_FK == null || !appPartsByHamkaran.TryGetValue(hts.Hamkaran_Part_FK.Value, out var appPart))
					continue;

				ApplySupplementary(appPart, hts, usedHtsIds);
			}
			await unitOfWork.SaveChangesAsync(cn);
		}

		private sealed class RahkaranPartDto
		{
			public long PartID { get; set; }
			public string? Code { get; set; }
			public string? Name { get; set; }
			public string? LatinName { get; set; }
			public string? PropertiesComment { get; set; }
			public string? TechnicalSpecification { get; set; }
			public int? PartType { get; set; }
			public long? MajorUnitRef { get; set; }
		}

		[JobHandler("افزودن اطلاعات تکمیلی کالا از Hts")]
		public async Task AddExteraInfoToPartFromHtnk(IJobLogger? jobLogger = null, CancellationToken cancellationToken = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع انتقال مدارک کالا و اطلاعات تکمیلی از HTS", cancellationToken);

				var appParts = await unitOfWork.Repository<Part>().Table.ToListAsync(cancellationToken);
				var partsByHamkaran = appParts
					.Where(p => p.HamkaranId != null)
					.GroupBy(p => p.HamkaranId!.Value)
					.ToDictionary(g => g.Key, g => g.First());
				var usedPartHtsIds = appParts.Where(p => p.HtsId != 0).Select(p => p.HtsId).ToHashSet();

				var htsParts = await Hdb.Hts_Inv_Parts.AsNoTracking().ToListAsync(cancellationToken);
				var htsPartsById = htsParts.ToDictionary(p => p.Part_ID);

				var supplementaryCount = 0;
				foreach (var hts in htsParts)
				{
					if (hts.Hamkaran_Part_FK == null || !partsByHamkaran.TryGetValue(hts.Hamkaran_Part_FK.Value, out var appPart))
						continue;

					ApplySupplementary(appPart, hts, usedPartHtsIds);
					supplementaryCount++;
				}

				await unitOfWork.SaveChangesAsync(cancellationToken);
				await jobLogger?.LogInfoAsync($"اطلاعات تکمیلی کالا اعمال شد: {supplementaryCount}", cancellationToken);

				var orgMap = await BuildHtsOrgUnitMapAsync(cancellationToken);
				var permissions = await Hdb.Hts_Inv_Part_Attachment_Permissions.AsNoTracking().ToListAsync(cancellationToken);
				var permissionsByAttachment = permissions
					.GroupBy(p => p.Part_Attachment_FK)
					.ToDictionary(g => g.Key, g => g.Select(x => x.OrgUnit_FK).Distinct().ToList());

				var attachments = await Hdb.Hts_Inv_Part_Attachments.AsNoTracking()
					.Select(pa => new HtsPartAttachmentRow
					{
						Id = pa.Part_Attachment_ID,
						PartId = pa.Part_FK,
						FileName = pa.Attachment_FileName,
						FilePath = pa.AttachmentFilePath,
						DocumentTypeId = pa.Project_Document_Type_FK,
						IsMain = pa.IsMain,
						Comment = pa.Comment
					})
					.ToListAsync(cancellationToken);

				var existingDocs = await unitOfWork.Repository<PartDocument>().Table
					.Include(d => d.Attachment)
					.ToListAsync(cancellationToken);
				var docsByHtsId = existingDocs
					.Where(d => d.HtsId != 0)
					.GroupBy(d => d.HtsId)
					.ToDictionary(g => g.Key, g => g.First());
				var unmatchedByKey = new Dictionary<string, Queue<PartDocument>>(StringComparer.OrdinalIgnoreCase);
				foreach (var doc in existingDocs.Where(d => d.HtsId == 0))
				{
					var key = DocumentMatchKey(doc.PartId, doc.Type, doc.Attachment?.OriginalName);
					if (!unmatchedByKey.TryGetValue(key, out var queue))
					{
						queue = new Queue<PartDocument>();
						unmatchedByKey[key] = queue;
					}
					queue.Enqueue(doc);
				}

				var inserted = 0;
				var updated = 0;
				var linked = 0;
				var unchangedFiles = 0;
				var missingFiles = 0;
				var skippedNoPart = 0;
				var failed = 0;
				var unmappedOrgUnits = 0;
				var pendingSaves = 0;

				foreach (var pa in attachments)
				{
					cancellationToken.ThrowIfCancellationRequested();
					try
					{
						if (!htsPartsById.TryGetValue(pa.PartId, out var htsPart)
							|| htsPart.Hamkaran_Part_FK == null
							|| !partsByHamkaran.TryGetValue(htsPart.Hamkaran_Part_FK.Value, out var appPart)
							|| appPart.Id is not long appPartId)
						{
							skippedNoPart++;
							continue;
						}

						var docType = MapDocumentType(pa.DocumentTypeId);
						var fileName = string.IsNullOrWhiteSpace(pa.FileName)
							? $"hts-{pa.Id}"
							: pa.FileName.Trim();
						permissionsByAttachment.TryGetValue(pa.Id, out var orgIds);
						var (unitIds, unitTitles, unmapped) = MapOrgUnits(orgIds, orgMap);
						unmappedOrgUnits += unmapped;

						PartDocument? doc = null;
						var isNew = false;
						if (docsByHtsId.TryGetValue(pa.Id, out doc))
						{
							updated++;
						}
						else
						{
							var key = DocumentMatchKey(appPartId, docType, fileName);
							if (unmatchedByKey.TryGetValue(key, out var queue) && queue.Count > 0)
							{
								doc = queue.Dequeue();
								doc.HtsId = pa.Id;
								docsByHtsId[pa.Id] = doc;
								linked++;
							}
						}

						var fileReady = !string.IsNullOrWhiteSpace(pa.FilePath) && File.Exists(pa.FilePath);
						if (doc == null)
						{
							if (!fileReady)
							{
								missingFiles++;
								continue;
							}

							var uploaded = await UploadPartFileAsync(pa.FilePath!, fileName, cancellationToken);
							doc = new PartDocument
							{
								AttachmentId = uploaded.Id!.Value,
								PartId = appPartId,
								HtsId = pa.Id
							};
							await unitOfWork.Repository<PartDocument>().AddAsync(doc, cancellationToken, saveNow: false);
							docsByHtsId[pa.Id] = doc;
							isNew = true;
							inserted++;
						}
						else if (fileReady && !SameFile(doc.Attachment, pa.FilePath!, fileName))
						{
							var oldAttachmentId = doc.AttachmentId;
							var uploaded = await UploadPartFileAsync(pa.FilePath!, fileName, cancellationToken);
							doc.AttachmentId = uploaded.Id!.Value;
							doc.Attachment = uploaded;
							if (oldAttachmentId > 0 && oldAttachmentId != uploaded.Id)
								await TryDeleteFileAsync(oldAttachmentId, cancellationToken);
						}
						else
						{
							unchangedFiles++;
						}

						doc.Comment = pa.Comment;
						doc.Main = pa.IsMain;
						doc.PartId = appPartId;
						doc.Type = docType;
						doc.OrganizationUnitIds = unitIds;
						doc.OrganizationUnits = unitTitles;
						pendingSaves++;

						if (isNew || pendingSaves >= 200)
						{
							await unitOfWork.SaveChangesAsync(cancellationToken);
							pendingSaves = 0;
						}
					}
					catch (Exception ex)
					{
						failed++;
						await jobLogger?.LogWarningAsync($"مدرک HTS {pa.Id} منتقل نشد: {ex.Message}", 0, cancellationToken);
					}
				}

				if (pendingSaves > 0)
					await unitOfWork.SaveChangesAsync(cancellationToken);

				await jobLogger?.LogInfoAsync(
					$"پایان انتقال مدارک. جدید: {inserted}، به‌روزرسانی: {updated}، اتصال به مدرک قبلی: {linked}، فایل بدون تغییر: {unchangedFiles}، فایل ناموجود: {missingFiles}، کالا پیدا نشد: {skippedNoPart}، واحد بدون نگاشت: {unmappedOrgUnits}، خطا: {failed}",
					cancellationToken);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogErrorAsync($"انتقال مدارک کالا و اطلاعات تکمیلی از HTS ناموفق بود: {ex.Message}", 0, cancellationToken);
				throw;
			}
		}

		private static void ApplySupplementary(Part appPart, Hts_Inv_Part hts, HashSet<long> usedPartHtsIds)
		{
			if (hts.DataSheetTypeId is short dataSheetId && Enum.IsDefined(typeof(PartDataSheetEnum), (int)dataSheetId))
				appPart.DataSheet = (PartDataSheetEnum)dataSheetId;
			else if (hts.DataSheetTypeId == null)
				appPart.DataSheet = null;

			appPart.DataSheetUsage = hts.IsActiveForDataSheet ?? false;
			appPart.BrandInDataSheet = hts.DataSheetBrand;
			appPart.Brand = Truncate(hts.Brand, 500);
			appPart.Dimensions = Truncate(hts.Dimensions, 500);
			appPart.Foreign = hts.IsExternal ?? false;
			appPart.EngineeringRoutine = hts.IsRoutine ?? false;
			appPart.DocumentsNotRequired = hts.NotNeedToAttachDocuments ?? false;
			appPart.PreciseTool = hts.IsForInstrumentation ?? false;
			appPart.DesignTypeIsRoutine = hts.DesignTypeIsRoutine;
			if (hts.SaleRate.HasValue)
				appPart.SaleRate = hts.SaleRate.Value;

			if (appPart.HtsId == 0 && usedPartHtsIds.Add(hts.Part_ID))
				appPart.HtsId = hts.Part_ID;
		}

		private async Task<Dictionary<short, (long Id, string Title)>> BuildHtsOrgUnitMapAsync(CancellationToken cn)
		{
			var htsUnits = await Hdb.Hts_HRM_OrgUnits.AsNoTracking().ToListAsync(cn);
			var appUnits = await unitOfWork.Repository<OrgUnit>().TableNoTracking
				.Where(u => u.Id != null)
				.Select(u => new { Id = u.Id!.Value, u.HamkaranUnitId, u.Title })
				.ToListAsync(cn);

			var byHamkaran = appUnits
				.GroupBy(u => u.HamkaranUnitId)
				.ToDictionary(g => g.Key, g => g.First());
			var byTitle = appUnits
				.Where(u => !string.IsNullOrWhiteSpace(u.Title))
				.GroupBy(u => u.Title.Trim(), StringComparer.Ordinal)
				.ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

			var map = new Dictionary<short, (long Id, string Title)>();
			foreach (var hts in htsUnits)
			{
				if (hts.Hamkaran_Unit_FK is int hamkaran && hamkaran != 0
					&& byHamkaran.TryGetValue((long)hamkaran, out var byCode))
				{
					map[hts.OrgUnit_ID] = (byCode.Id, byCode.Title);
					continue;
				}

				if (!string.IsNullOrWhiteSpace(hts.OrgUnit_Title)
					&& byTitle.TryGetValue(hts.OrgUnit_Title.Trim(), out var byName))
					map[hts.OrgUnit_ID] = (byName.Id, byName.Title);
			}

			return map;
		}

		private static (string? Ids, string? Titles, int Unmapped) MapOrgUnits(
			IReadOnlyList<short>? htsOrgIds,
			IReadOnlyDictionary<short, (long Id, string Title)> orgMap)
		{
			if (htsOrgIds == null || htsOrgIds.Count == 0)
				return (null, null, 0);

			var ids = new List<long>();
			var titles = new List<string>();
			var unmapped = 0;
			foreach (var htsOrgId in htsOrgIds)
			{
				if (!orgMap.TryGetValue(htsOrgId, out var mapped) || ids.Contains(mapped.Id))
				{
					if (!orgMap.ContainsKey(htsOrgId))
						unmapped++;
					continue;
				}

				ids.Add(mapped.Id);
				titles.Add(mapped.Title);
			}

			if (ids.Count == 0)
				return (null, null, unmapped);

			return (string.Join(',', ids), string.Join(',', titles), unmapped);
		}

		private static PartDocumentTypeEnum MapDocumentType(short? htsTypeId)
		{
			if (htsTypeId is short typeId && Enum.IsDefined(typeof(PartDocumentTypeEnum), (int)typeId))
				return (PartDocumentTypeEnum)typeId;

			return PartDocumentTypeEnum.None;
		}

		private static string DocumentMatchKey(long partId, PartDocumentTypeEnum? type, string? fileName)
			=> $"{partId}|{(int?)type}|{(fileName ?? "").Trim()}";

		private static bool SameFile(FileEntity? file, string path, string fileName)
		{
			if (file == null)
				return false;

			var length = new FileInfo(path).Length;
			return file.Size == length
				&& string.Equals(file.OriginalName, fileName, StringComparison.OrdinalIgnoreCase);
		}

		private async Task<FileEntity> UploadPartFileAsync(string path, string fileName, CancellationToken ct)
		{
			var bytes = await File.ReadAllBytesAsync(path, ct);
			return await fileService.UploadAsync(
				bytes,
				fileName,
				null,
				"Entities.App.Inv.PartDocument",
				"Attachment",
				null,
				ct);
		}

		private async Task TryDeleteFileAsync(long fileId, CancellationToken ct)
		{
			try
			{
				await fileService.DeleteAsync(fileId, ct);
			}
			catch
			{
				// فایل قبلی اگر روی دیسک نباشد، مدرک جدید نباید از بین برود
			}
		}

		private static string? Truncate(string? value, int max)
		{
			if (string.IsNullOrEmpty(value) || value.Length <= max)
				return value;

			return value[..max];
		}

		private sealed class HtsPartAttachmentRow
		{
			public int Id { get; set; }
			public long PartId { get; set; }
			public string? FileName { get; set; }
			public string? FilePath { get; set; }
			public short? DocumentTypeId { get; set; }
			public bool? IsMain { get; set; }
			public string? Comment { get; set; }
		}
	}
}
