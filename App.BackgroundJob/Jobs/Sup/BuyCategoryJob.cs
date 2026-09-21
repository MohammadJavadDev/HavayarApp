using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Inv;
using Entities.App.Sup;
using Entities.App.Sup.Enums;
using Entities.Base;
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
				// نکته (D12): ToDictionaryAsync روی کلیدهای غیر یکتا (مثلاً HamkaranId مشترک بین دو کاربر) کل جاب را
				// با «An item with the same key has already been added» متوقف می‌کرد. به‌جای آن GroupBy و انتخاب
				// قطعی برنده: ابتدا رکورد فعال، سپس کوچک‌ترین Id. تکراری‌ها فقط لاگ (warning) می‌شوند.
				var partRows = await unitOfWork.Repository<Part>().TableNoTracking
					.Where(p => !string.IsNullOrEmpty(p.Code))
					.Select(p => new { Id = p.Id!.Value, Code = p.Code!, p.IsActive })
					.ToListAsync(cn);

				var duplicatePartCodes = partRows
					.GroupBy(x => x.Code)
					.Where(g => g.Count() > 1)
					.Select(g => g.Key)
					.ToList();
				if (duplicatePartCodes.Count > 0)
				{
					await jobLogger?.LogWarningAsync(
						$"{duplicatePartCodes.Count} کد کالای تکراری در Inv.Part (کالای فعال / کم‌ترین Id انتخاب شد): {string.Join(", ", duplicatePartCodes.Take(50))}{(duplicatePartCodes.Count > 50 ? " ..." : string.Empty)}",
						0, cn);
				}

				var partsDict = partRows
					.GroupBy(x => x.Code)
					.ToDictionary(
						g => g.Key,
						g => g.OrderByDescending(x => x.IsActive == IsActiveEnum.Active).ThenBy(x => x.Id).First().Id);

				var userRows = await dbContext.Users
					.Where(u => u.Party != null && u.Party.HamkaranId != null)
					.Select(u => new { Id = u.Id!.Value, HamkaranId = u.Party!.HamkaranId!.Value, u.IsActive })
					.ToListAsync(cn);

				var duplicateUserHamkaranIds = userRows
					.GroupBy(x => x.HamkaranId)
					.Where(g => g.Count() > 1)
					.Select(g => g.Key)
					.ToList();
				if (duplicateUserHamkaranIds.Count > 0)
				{
					await jobLogger?.LogWarningAsync(
						$"{duplicateUserHamkaranIds.Count} HamkaranId مشترک بین چند کاربر (system.User → Gnr.Party.HamkaranId) — کاربر فعال / کم‌ترین Id به‌عنوان متولی خرید انتخاب شد: {string.Join(", ", duplicateUserHamkaranIds)}. گزارش: Data/Scripts/Report_Party_DuplicateHamkaranId.sql",
						0, cn);
				}

				var usersDict = userRows
					.GroupBy(x => x.HamkaranId)
					.ToDictionary(
						g => g.Key,
						g => g.OrderByDescending(x => x.IsActive == IsActiveEnum.Active).ThenBy(x => x.Id).First().Id);

				// 4. Sync BuyCategories
				var categoryIdMap = new Dictionary<long, long>(); // Rahkaran ID => App ID
				var failedCategoryCount = 0;

				foreach (var rahkaranCategory in rahkaranBuyCategories)
				{
					if (cn.IsCancellationRequested) break;

					BuyCategory? newCategory = null;
					try
					{
						var localCategory = localBuyCategories.FirstOrDefault(x => x.HamkaranId == rahkaranCategory.Sup_BuyCategoryID);

						long? purchaseResponsibleId = null;
						if (rahkaranCategory.PurchaseResponsibleRef.HasValue &&
							usersDict.TryGetValue(rahkaranCategory.PurchaseResponsibleRef.Value, out var userId))
						{
							purchaseResponsibleId = userId;
						}

						var routineLeadTime = rahkaranCategory.RoutineLeadTimeInDay.HasValue
							? (int?)Convert.ToInt32(rahkaranCategory.RoutineLeadTimeInDay.Value)
							: null;
						var nonRoutineLeadTime = rahkaranCategory.NonRoutineLeadTimeInDay.HasValue
							? (int?)Convert.ToInt32(rahkaranCategory.NonRoutineLeadTimeInDay.Value)
							: null;
						var typeValue = rahkaranCategory.TypeRef.HasValue
							? (BuyCategoryTypeEnum?)rahkaranCategory.TypeRef.Value
							: null;

						if (localCategory == null)
						{
							// INSERT
							newCategory = new BuyCategory
							{
								HamkaranId = rahkaranCategory.Sup_BuyCategoryID,
								Title = rahkaranCategory.Title,
								PurchaseResponsibleId = purchaseResponsibleId,
								RoutineLeadTimeInDay = routineLeadTime,
								NonRoutineLeadTimeInDay = nonRoutineLeadTime,
								Type = typeValue
							};

							await unitOfWork.Repository<BuyCategory>().AddAsync(newCategory, cn);
							localBuyCategories.Add(newCategory);
							await unitOfWork.SaveChangesAsync(cn); // Save to get ID
							categoryIdMap[rahkaranCategory.Sup_BuyCategoryID] = newCategory.Id!.Value;
						}
						else
						{
							// UPDATE
							if (localCategory.Title != rahkaranCategory.Title)
								localCategory.Title = rahkaranCategory.Title;

							// D37: مقدار NULL / نگاشت‌نشده در راهکاران به معنی «اطلاعی ندارد» است، نه «پاک کن».
							// متولی خرید و lead time روتین/غیرروتین فقط وقتی راهکاران مقدار دارد بازنویسی می‌شوند؛
							// در غیر این صورت مقدار محلی (backfill از HTS یا ویرایش در پنل) حفظ می‌شود.
							if (purchaseResponsibleId.HasValue && localCategory.PurchaseResponsibleId != purchaseResponsibleId)
								localCategory.PurchaseResponsibleId = purchaseResponsibleId;

							if (routineLeadTime.HasValue && localCategory.RoutineLeadTimeInDay != routineLeadTime)
								localCategory.RoutineLeadTimeInDay = routineLeadTime;

							if (nonRoutineLeadTime.HasValue && localCategory.NonRoutineLeadTimeInDay != nonRoutineLeadTime)
								localCategory.NonRoutineLeadTimeInDay = nonRoutineLeadTime;

							if (typeValue.HasValue && localCategory.Type != typeValue)
								localCategory.Type = typeValue;

							categoryIdMap[rahkaranCategory.Sup_BuyCategoryID] = localCategory.Id!.Value;
						}
					}
					catch (Exception ex) when (ex is not OperationCanceledException)
					{
						failedCategoryCount++;

						// رکورد ناموفق را از change tracker خارج کن تا SaveChanges بعدی دوباره شکست نخورد
						if (newCategory != null)
						{
							localBuyCategories.Remove(newCategory);
							dbContext.Entry(newCategory).State = EntityState.Detached;
						}

						await jobLogger?.LogErrorAsync(
							$"خطا در همگام‌سازی دسته خرید Rahkaran Id={rahkaranCategory.Sup_BuyCategoryID} «{rahkaranCategory.Title}»: {ex.Message}",
							0, cn);
					}
				}

				await unitOfWork.SaveChangesAsync(cn);

				if (failedCategoryCount > 0)
					await jobLogger?.LogWarningAsync($"{failedCategoryCount} دسته خرید به دلیل خطا رد شد (جزئیات در لاگ‌های بالا)", 0, cn);

				// 5. Sync BuyCategoryItems
				var allLocalItems = localBuyCategories.SelectMany(x => x.Items).ToList();
				var failedItemCount = 0;

				foreach (var rahkaranItem in rahkaranBuyCategoryItems)
				{
					if (cn.IsCancellationRequested) break;

					if (!categoryIdMap.TryGetValue(rahkaranItem._MasterRef, out var categoryId))
					{
						await jobLogger?.LogWarningAsync($"Skipping item {rahkaranItem.Sup_BuyCategoryItemsID}: Category not found", 0, cn);
						continue;
					}

					BuyCategoryItem? newItem = null;
					try
					{
						long? partId = null;
						if (!string.IsNullOrEmpty(rahkaranItem.PartCode) &&
							partsDict.TryGetValue(rahkaranItem.PartCode, out var pId))
						{
							partId = pId;
						}

						var localItem = allLocalItems.FirstOrDefault(x =>
							x.HamkaranId == rahkaranItem.Sup_BuyCategoryItemsID);

						var timeInWay = rahkaranItem.TimeInWay.HasValue
							? (int?)Convert.ToInt32(rahkaranItem.TimeInWay.Value)
							: null;
						var supplierValue = rahkaranItem.Supplier.HasValue
							? (BuyCategoryItemSupplierEnum?)rahkaranItem.Supplier.Value
							: null;

						if (localItem == null)
						{
							// INSERT
							newItem = new BuyCategoryItem
							{
								HamkaranId = rahkaranItem.Sup_BuyCategoryItemsID,
								BuyCategoryId = categoryId,
								PartId = partId,
								TimeInWay = timeInWay,
								Supplier = supplierValue,
								Comments = rahkaranItem.Comments
							};

							await unitOfWork.Repository<BuyCategoryItem>().AddAsync(newItem, cn);
							allLocalItems.Add(newItem);
						}
						else
						{
							// UPDATE
							if (localItem.BuyCategoryId != categoryId)
								localItem.BuyCategoryId = categoryId;

							if (localItem.PartId != partId)
								localItem.PartId = partId;

							if (localItem.TimeInWay != timeInWay)
								localItem.TimeInWay = timeInWay;

							if (localItem.Supplier != supplierValue)
								localItem.Supplier = supplierValue;

							if (localItem.Comments != rahkaranItem.Comments)
								localItem.Comments = rahkaranItem.Comments;
						}
					}
					catch (Exception ex) when (ex is not OperationCanceledException)
					{
						failedItemCount++;

						if (newItem != null)
						{
							allLocalItems.Remove(newItem);
							dbContext.Entry(newItem).State = EntityState.Detached;
						}

						await jobLogger?.LogErrorAsync(
							$"خطا در همگام‌سازی قلم دسته خرید Rahkaran Id={rahkaranItem.Sup_BuyCategoryItemsID} (PartCode={rahkaranItem.PartCode}): {ex.Message}",
							0, cn);
					}
				}

				await unitOfWork.SaveChangesAsync(cn);

				if (failedItemCount > 0)
					await jobLogger?.LogWarningAsync($"{failedItemCount} قلم دسته خرید به دلیل خطا رد شد (جزئیات در لاگ‌های بالا)", 0, cn);

				// 6. Update Part.BuyCategoryId based on BuyCategoryItems
				await jobLogger?.LogInfoAsync("Updating Part.BuyCategoryId fields...", cn);

				var itemsWithParts = allLocalItems.Where(x => x.PartId.HasValue).ToList();
				var partIds = itemsWithParts.Select(x => x.PartId!.Value).Distinct().ToList();
				var parts = await unitOfWork.Repository<Part>()
					.Table
					.Where(p => partIds.Contains(p.Id!.Value))
					.ToListAsync(cn);

				// یک کالا ممکن است در چند دسته باشد (D34) — به‌صورت قطعی کم‌ترین Id قلم انتخاب می‌شود
				var itemByPartId = itemsWithParts
					.GroupBy(x => x.PartId!.Value)
					.ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id ?? long.MaxValue).First());

				int updatedPartsCount = 0;
				foreach (var part in parts)
				{
					if (part.Id.HasValue
						&& itemByPartId.TryGetValue(part.Id.Value, out var item)
						&& part.BuyCategoryId != item.BuyCategoryId)
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
