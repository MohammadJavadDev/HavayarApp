using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Inv;
using Entities.App.Sup;
using Entities.App.Sup.Enums;
using Entities.Rahkaran.USR3;
using Microsoft.EntityFrameworkCore;
using Services.Job;

namespace App.BackgroundJob.Jobs.Sup
{
	public class BuyCategoryJob(ApplicationDbContext dbContext, RahkaranDbContext rdb, IUnitOfWork unitOfWork)
	{
		[JobHandler("هماهنگ کردن اطلاعات دسته بندی های خرید از راهکاران")]
		public async Task SyncBuyCategoryJobFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("Starting SyncBuyCategoryJobFromRahkaran...", cn);

				// 1. Load Rahkaran Data
				var rahkaranBuyCategories = await rdb.RahkaranSup_BuyCategorys
					.ToListAsync(cn);
				var rahkaranBuyCategoryItems = await rdb.RahkaranSup_BuyCategoryItemss
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"Fetched {rahkaranBuyCategories.Count} categories and {rahkaranBuyCategoryItems.Count} items from Rahkaran", cn);

				// 2. Load Local Data
				var localBuyCategories = await unitOfWork.Repository<BuyCategory>()
					.Table
					.Include(x => x.Items)
					.ToListAsync(cn);

				// 3. Create lookup dictionaries
				var partsDict = await unitOfWork.Repository<Part>().TableNoTracking
					.Where(p => !string.IsNullOrEmpty(p.Code))
					.Select(p => new { Id = p.Id!.Value, Code = p.Code! })
					.ToDictionaryAsync(x => x.Code, x => x.Id, cn);

				var usersDict = await dbContext.Users
					.Include(c=>c.Party)
					.Where(u => u.Party != null && u.Party.HamkaranId != null)
					.Select(u => new { Id = u.Id!.Value, HamkaranId = u.Party.HamkaranId!.Value })
					.ToDictionaryAsync(x => x.HamkaranId, x => x.Id, cn);

				// 4. Sync BuyCategories
				var categoryIdMap = new Dictionary<long, long>(); // Rahkaran ID => App ID

				foreach (var rahkaranCategory in rahkaranBuyCategories)
				{
					var localCategory = localBuyCategories.FirstOrDefault(x => x.HamkaranId == rahkaranCategory.Sup_BuyCategoryID);

					long? purchaseResponsibleId = null;
					if (rahkaranCategory.PurchaseResponsibleRef.HasValue &&
						usersDict.TryGetValue(rahkaranCategory.PurchaseResponsibleRef.Value, out var userId))
					{
						purchaseResponsibleId = userId;
					}

					if (localCategory == null)
					{
						// INSERT
						var newCategory = new BuyCategory
						{
							HamkaranId = rahkaranCategory.Sup_BuyCategoryID,
							Title = rahkaranCategory.Title,
							PurchaseResponsibleId = purchaseResponsibleId,
							RoutineLeadTimeInDay = rahkaranCategory.RoutineLeadTimeInDay.HasValue
								? (int?)Convert.ToInt32(rahkaranCategory.RoutineLeadTimeInDay.Value)
								: null,
							NonRoutineLeadTimeInDay = rahkaranCategory.NonRoutineLeadTimeInDay.HasValue
								? (int?)Convert.ToInt32(rahkaranCategory.NonRoutineLeadTimeInDay.Value)
								: null,
							Type = rahkaranCategory.TypeRef.HasValue
								? (BuyCategoryTypeEnum?)rahkaranCategory.TypeRef.Value
								: null
						};

						await unitOfWork.Repository<BuyCategory>().AddAsync(newCategory, cn);
						localBuyCategories.Add(newCategory);
						await unitOfWork.SaveChangesAsync(cn); // Save to get ID
						categoryIdMap[rahkaranCategory.Sup_BuyCategoryID] = newCategory.Id!.Value;

						 
					}
					else
					{
						// UPDATE
						bool updated = false;

						if (localCategory.Title != rahkaranCategory.Title)
						{
							localCategory.Title = rahkaranCategory.Title;
							updated = true;
						}

						if (localCategory.PurchaseResponsibleId != purchaseResponsibleId)
						{
							localCategory.PurchaseResponsibleId = purchaseResponsibleId;
							updated = true;
						}

						var routineLeadTime = rahkaranCategory.RoutineLeadTimeInDay.HasValue
							? (int?)Convert.ToInt32(rahkaranCategory.RoutineLeadTimeInDay.Value)
							: null;
						if (localCategory.RoutineLeadTimeInDay != routineLeadTime)
						{
							localCategory.RoutineLeadTimeInDay = routineLeadTime;
							updated = true;
						}

						var nonRoutineLeadTime = rahkaranCategory.NonRoutineLeadTimeInDay.HasValue
							? (int?)Convert.ToInt32(rahkaranCategory.NonRoutineLeadTimeInDay.Value)
							: null;
						if (localCategory.NonRoutineLeadTimeInDay != nonRoutineLeadTime)
						{
							localCategory.NonRoutineLeadTimeInDay = nonRoutineLeadTime;
							updated = true;
						}

						var typeValue = rahkaranCategory.TypeRef.HasValue
							? (BuyCategoryTypeEnum?)rahkaranCategory.TypeRef.Value
							: null;
						if (localCategory.Type != typeValue)
						{
							localCategory.Type = typeValue;
							updated = true;
						}

						 

						categoryIdMap[rahkaranCategory.Sup_BuyCategoryID] = localCategory.Id!.Value;
					}
				}

				await unitOfWork.SaveChangesAsync(cn);

				// 5. Sync BuyCategoryItems
				var allLocalItems = localBuyCategories.SelectMany(x => x.Items).ToList();

				foreach (var rahkaranItem in rahkaranBuyCategoryItems)
				{
					if (!categoryIdMap.TryGetValue(rahkaranItem._MasterRef, out var categoryId))
					{
						await jobLogger?.LogWarningAsync($"Skipping item {rahkaranItem.Sup_BuyCategoryItemsID}: Category not found", 0, cn);
						continue;
					}

					long? partId = null;
					if (!string.IsNullOrEmpty(rahkaranItem.PartCode) &&
						partsDict.TryGetValue(rahkaranItem.PartCode, out var pId))
					{
						partId = pId;
					}

					var localItem = allLocalItems.FirstOrDefault(x =>
						x.HamkaranId == rahkaranItem.Sup_BuyCategoryItemsID);

					if (localItem == null)
					{
						// INSERT
						var newItem = new BuyCategoryItem
						{
							HamkaranId = rahkaranItem.Sup_BuyCategoryItemsID,
							BuyCategoryId = categoryId,
							PartId = partId,
							TimeInWay = rahkaranItem.TimeInWay.HasValue
								? (int?)Convert.ToInt32(rahkaranItem.TimeInWay.Value)
								: null,
							Supplier = rahkaranItem.Supplier.HasValue
								? (BuyCategoryItemSupplierEnum?)rahkaranItem.Supplier.Value
								: null,
							Comments = rahkaranItem.Comments
						};

						await unitOfWork.Repository<BuyCategoryItem>().AddAsync(newItem, cn);
						allLocalItems.Add(newItem);

						 
					}
					else
					{
						// UPDATE
						bool updated = false;

						if (localItem.BuyCategoryId != categoryId)
						{
							localItem.BuyCategoryId = categoryId;
							updated = true;
						}

						if (localItem.PartId != partId)
						{
							localItem.PartId = partId;
							updated = true;
						}

						var timeInWay = rahkaranItem.TimeInWay.HasValue
							? (int?)Convert.ToInt32(rahkaranItem.TimeInWay.Value)
							: null;
						if (localItem.TimeInWay != timeInWay)
						{
							localItem.TimeInWay = timeInWay;
							updated = true;
						}

						var supplierValue = rahkaranItem.Supplier.HasValue
							? (BuyCategoryItemSupplierEnum?)rahkaranItem.Supplier.Value
							: null;
						if (localItem.Supplier != supplierValue)
						{
							localItem.Supplier = supplierValue;
							updated = true;
						}

						if (localItem.Comments != rahkaranItem.Comments)
						{
							localItem.Comments = rahkaranItem.Comments;
							updated = true;
						}

						if (updated)
						{
							 
						}
					}
				}

				await unitOfWork.SaveChangesAsync(cn);

				// 6. Update Part.BuyCategoryId based on BuyCategoryItems
				await jobLogger?.LogInfoAsync("Updating Part.BuyCategoryId fields...", cn);

				var itemsWithParts = allLocalItems.Where(x => x.PartId.HasValue).ToList();
				var partIds = itemsWithParts.Select(x => x.PartId!.Value).Distinct().ToList();
				var parts = await unitOfWork.Repository<Part>()
					.Table
					.Where(p => partIds.Contains(p.Id!.Value))
					.ToListAsync(cn);

				int updatedPartsCount = 0;
				foreach (var part in parts)
				{
					var item = itemsWithParts.FirstOrDefault(x => x.PartId == part.Id);
					if (item != null && part.BuyCategoryId != item.BuyCategoryId)
					{
						part.BuyCategoryId = item.BuyCategoryId;
						updatedPartsCount++;
					}
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync($"Updated {updatedPartsCount} Part records with BuyCategoryId", cn);

				await jobLogger?.LogInfoAsync("SyncBuyCategoryJobFromRahkaran completed successfully", cn);
			}
			catch (Exception ex)
			{
				if (jobLogger != null)
				{
					await jobLogger.LogInfoAsync(ex.Message, cn);
				}
				throw;
			}
		}
	}
}
