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
	 
	public class PartJob(RahkaranDbContext Rdb , IUnitOfWork unitOfWork,HtsDbContext Hdb,
		IFileService fileService)
	{
		[JobHandler("افزودن اطلاعات کالا از راهکاران")]
		public async Task AddPartsFromRahkaran(CancellationToken cn  =default)
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
				await unitOfWork.Repository<PartUnit>().AddRangeAsync(newUnits, cn,false); // اگر متد دارید
			}

			await unitOfWork.SaveChangesAsync(cn);

			var rahkaranParts = await Rdb.RahkaranParts.AsNoTracking().ToListAsync(cn);
			var appParts = await unitOfWork.Repository<Part>().Table.ToListAsync(cn);

			// Dictionary برای جستجوی سریع
			var appPartsDict = appParts.ToDictionary(x => x.HamkaranId);

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

			var newParts = await unitOfWork.Repository<Part>().TableNoTracking
				.ToArrayAsync();
		     var existOldParts = await Hdb.Hts_Inv_Parts.AsNoTracking().ToListAsync();

			var partAttachments = await Hdb.Hts_Inv_Part_Attachments.AsNoTracking().ToListAsync();

			var partAttachmentPermissions = await Hdb.Hts_Inv_Part_Attachment_Permissions.AsNoTracking().ToListAsync();

			  
			foreach(var pa in partAttachments)
			{
				var existOldPart = existOldParts.FirstOrDefault(c => c.Part_ID == pa.Part_FK);

				if(existOldPart is null) { continue; }

				var newExistPart = newParts.FirstOrDefault(c => c.HamkaranId == existOldPart.Hamkaran_Part_FK);

				if (newExistPart is null) { continue; }

				if(!File.Exists(pa.AttachmentFilePath)) { continue; }

				byte[]? existFIle;
				long attachmentId = 0;

				 
				existFIle = File.ReadAllBytes(pa.AttachmentFilePath);
				var attachment = await fileService
					.UploadAsync(existFIle, (pa?.Attachment_FileName ?? Guid.NewGuid().ToString().Substring(0, 6)), null, "Entities.App.Inv.PartDocument", "Attachment", null, cancellationToken);
				attachmentId = (long)attachment.Id;
				 

				var partAttachmentPermission = partAttachmentPermissions
					.Where(c => c.Part_Attachment_FK == pa.Part_Attachment_ID)
					.Select(c => c.OrgUnit_FK);

				var partAttachmentPermissionIds = string.Join(',', partAttachmentPermission);


				var newPartDocument = new PartDocument()
				{
					AttachmentId = attachmentId,
					Comment = pa.Comment,
					Main = pa.IsMain,
					OrganizationUnitIds = partAttachmentPermissionIds,
					PartId = (long)newExistPart.Id,
					Type = (PartDocumentTypeEnum)(pa?.Project_Document_Type_FK ?? 1) 				
				};

			var res =	await unitOfWork.Repository<PartDocument>().
					AddAsync(newPartDocument,cancellationToken);

				if(existOldPart.DataSheetTypeId != null)
				{
					newExistPart.DataSheet = (PartDataSheetEnum)existOldPart.DataSheetTypeId;
				}

				newExistPart.DataSheetUsage = existOldPart?.IsActiveForDataSheet ?? false;
				newExistPart.BrandInDataSheet = existOldPart?.DataSheetBrand;
				newExistPart.Brand = existOldPart.Brand;
				newExistPart.Dimensions = existOldPart?.Dimensions;
			 
				 

			}
			 
		}
	}
}
