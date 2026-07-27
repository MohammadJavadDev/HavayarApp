using App.BackgroundJob.Models;
using Common.Attributes;
using Data;
using Data.Contracts;
using Data.Repositories;
using Entities.App.Inv;
using Entities.App.Inv.Enums;
using Microsoft.EntityFrameworkCore;
using Services.FileServices;


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

			var htsParts = await Hdb.Hts_Inv_Parts.AsNoTracking()
				.Where(hp => hp.Hamkaran_Part_FK != null )
				.ToListAsync(cn);

			var appPartsByHamkaran = await unitOfWork.Repository<Part>().Table
				.Where(p => p.HamkaranId != null  )
				.ToDictionaryAsync(x => x.HamkaranId!.Value, x => x, cn);

			foreach (var hts in htsParts)
			{
				if (hts.Hamkaran_Part_FK == null || !appPartsByHamkaran.TryGetValue(hts.Hamkaran_Part_FK.Value, out var appPart))
					continue;

				if (hts.DataSheetTypeId != null)
					appPart.DataSheet = (PartDataSheetEnum)hts.DataSheetTypeId;
				appPart.DataSheetUsage = hts.IsActiveForDataSheet ?? false;
				appPart.BrandInDataSheet = hts.DataSheetBrand;
				appPart.Brand = hts.Brand;
				appPart.Dimensions = hts.Dimensions;
				appPart.Foreign = hts?.IsExternal ?? false;
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
		public async Task AddExteraInfoToPartFromHtnk(CancellationToken cancellationToken)
		{

			var newParts = await unitOfWork.Repository<Part>().Table
				.ToListAsync(cancellationToken);
			var newPartsDict = newParts.Where(c => c.HamkaranId != null).ToDictionary(c => c.HamkaranId!.Value);

			var existOldParts = await Hdb.Hts_Inv_Parts.AsNoTracking().ToListAsync(cancellationToken);
			var existOldPartsDict = existOldParts.ToDictionary(c => c.Part_ID);

			var partAttachments = await Hdb.Hts_Inv_Part_Attachments.AsNoTracking().ToListAsync(cancellationToken);

			var partAttachmentPermissions = await Hdb.Hts_Inv_Part_Attachment_Permissions.AsNoTracking().ToListAsync(cancellationToken);
			var permissionsDict = partAttachmentPermissions.GroupBy(c => c.Part_Attachment_FK).ToDictionary(g => g.Key, g => g.ToList());

			var existingPartDocs = await unitOfWork.Repository<PartDocument>().Table
				.Where(c => c.HtsId != 0)
				.ToListAsync(cancellationToken);
			var existingPartDocsDict = existingPartDocs.ToDictionary(c => c.HtsId);

			foreach (var pa in partAttachments)
			{
				if (!existOldPartsDict.TryGetValue(pa.Part_FK, out var existOldPart)) { continue; }

				if (existOldPart.Hamkaran_Part_FK == null || !newPartsDict.TryGetValue(existOldPart.Hamkaran_Part_FK.Value, out var newExistPart)) { continue; }

				if (!File.Exists(pa.AttachmentFilePath)) { continue; }

				byte[] existFIle;
				long attachmentId = 0;

				existFIle = await File.ReadAllBytesAsync(pa.AttachmentFilePath, cancellationToken);

				var partAttachmentPermission = permissionsDict.ContainsKey(pa.Part_Attachment_ID)
					? permissionsDict[pa.Part_Attachment_ID].Select(c => c.OrgUnit_FK)
					: Enumerable.Empty<short>(); // Use appropriate type for OrgUnit_FK

				var partAttachmentPermissionIds = string.Join(',', partAttachmentPermission);

				if (existingPartDocsDict.TryGetValue(pa.Part_Attachment_ID, out var existingDoc))
				{
					// Update existing
					if (existingDoc.AttachmentId > 0)
					{
						await fileService.DeleteAsync(existingDoc.AttachmentId, cancellationToken);
					}

					var attachment = await fileService
						.UploadAsync(existFIle, (pa?.Attachment_FileName ?? Guid.NewGuid().ToString().Substring(0, 6)), null, "Entities.App.Inv.PartDocument", "Attachment", null, cancellationToken);
					attachmentId = (long)attachment.Id;

					existingDoc.AttachmentId = attachmentId;
					existingDoc.Comment = pa.Comment;
					existingDoc.Main = pa.IsMain;
					existingDoc.OrganizationUnitIds = partAttachmentPermissionIds;
					existingDoc.PartId = (long)newExistPart.Id;
					existingDoc.Type = (PartDocumentTypeEnum)(pa?.Project_Document_Type_FK ?? 1);

					// Update explicitly if needed, though tracking handles it
					// unitOfWork.Repository<PartDocument>().Update(existingDoc); 
				}
				else
				{
					// Insert new
					var attachment = await fileService
						.UploadAsync(existFIle, (pa?.Attachment_FileName ?? Guid.NewGuid().ToString().Substring(0, 6)), null, "Entities.App.Inv.PartDocument", "Attachment", null, cancellationToken);
					attachmentId = (long)attachment.Id;

					var newPartDocument = new PartDocument()
					{
						AttachmentId = attachmentId,
						Comment = pa.Comment,
						Main = pa.IsMain,
						OrganizationUnitIds = partAttachmentPermissionIds,
						PartId = (long)newExistPart.Id,
						Type = (PartDocumentTypeEnum)(pa?.Project_Document_Type_FK ?? 1),
						HtsId = pa.Part_Attachment_ID
					};

					await unitOfWork.Repository<PartDocument>().AddAsync(newPartDocument, cancellationToken);
					existingPartDocsDict[pa.Part_Attachment_ID] = newPartDocument;
				}

				if (existOldPart.DataSheetTypeId != null)
				{
					newExistPart.DataSheet = (PartDataSheetEnum)existOldPart.DataSheetTypeId;
				}

				newExistPart.DataSheetUsage = existOldPart?.IsActiveForDataSheet ?? false;
				newExistPart.BrandInDataSheet = existOldPart?.DataSheetBrand;
				newExistPart.Brand = existOldPart.Brand;
				newExistPart.Dimensions = existOldPart?.Dimensions;
			}

			await unitOfWork.SaveChangesAsync(cancellationToken);
		}
	}
}
