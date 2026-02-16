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

	public class PartJob(RahkaranDbContext Rdb, IUnitOfWork unitOfWork, HtsDbContext Hdb,
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
				{
					eunit.Title = unit.Name;
				}
				else
				{
					newUnits.Add(new PartUnit
					{
						Title = unit.Name,
						HamkaranId = unit.UnitID
					});
				}
			}

			if (newUnits.Any())
			{
				await unitOfWork.Repository<PartUnit>().AddRangeAsync(newUnits, cn, false); // اگر متد دارید
			}

			await unitOfWork.SaveChangesAsync(cn);

			var rahkaranParts = await Rdb.RahkaranParts.AsNoTracking().ToListAsync(cn);
			var appParts = await unitOfWork.Repository<Part>().Table.ToListAsync(cn);

			// Dictionary برای جستجوی سریع
			var appPartsDict = appParts.Where(c => c.HamkaranId != null).ToDictionary(x => x.HamkaranId);

			var newParts = new List<Part>();

			foreach (var part in rahkaranParts)
			{
				if (!appPartsDict.TryGetValue(part.PartID, out var existPart))
				{
					// Insert
					newParts.Add(new Part
					{
						HamkaranId = part.PartID,
						Code = part.Code,
						Name = part.Name,
						LatinTitle = part.LatinName,
						Type = (PartTypeEnum)part?.PartType,
						Description = part.PropertiesComment,
						Number = part.TechnicalSpecification,
						UnitId = existUnitDict.ContainsKey(part.MajorUnitRef) ? existUnitDict[part.MajorUnitRef].Id : (long?)null
					});
				}
				else
				{

					bool isModified = false;

					if (existPart.Code != part.Code)
					{
						existPart.Code = part.Code;
						isModified = true;
					}

					if (existPart.Name != part.Name)
					{
						existPart.Name = part.Name;
						isModified = true;
					}

					if (existPart.LatinTitle != part.LatinName)
					{
						existPart.LatinTitle = part.LatinName;
						isModified = true;
					}

					if (existPart.Type != (PartTypeEnum)part?.PartType)
					{
						existPart.Type = (PartTypeEnum)part.PartType;
						isModified = true;
					}

					if (existPart.Description != part.PropertiesComment)
					{
						existPart.Description = part.PropertiesComment;
						isModified = true;
					}

					if (existPart.Number != part.TechnicalSpecification)
					{
						existPart.Number = part.TechnicalSpecification;
						isModified = true;
					}

					var newUnitId = existUnitDict.ContainsKey(part.MajorUnitRef) ? existUnitDict[part.MajorUnitRef].Id : (long?)null;
					if (existPart.UnitId != newUnitId)
					{
						existPart.UnitId = newUnitId;
						isModified = true;
					}


				}
			}
			if (newParts.Any())
			{
				await unitOfWork.Repository<Part>().AddRangeAsync(newParts, cn, false);
			}


			await unitOfWork.SaveChangesAsync(cn);


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
