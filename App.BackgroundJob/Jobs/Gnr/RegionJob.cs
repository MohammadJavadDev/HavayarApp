using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Gnr;
using Entities.App.Gnr.Enums;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Linq;

namespace App.BackgroundJob.Jobs.Gnr
{
	public class RegionJob(RahkaranDbContext Rdb, IUnitOfWork unitOfWork)
	{

		[JobHandler("افزودن مناطق جغرافیایی از راهکاران")]
		public async Task AddRegionFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی مناطق جغرافیایی از راهکاران", cn);

				// بارگذاری داده‌های راهکاران
				await jobLogger?.LogInfoAsync("در حال بارگذاری داده‌های مناطق از راهکاران...", cn);
				var rahkaranRegions = await Rdb.RahkaranRegionalDivision
					.AsNoTracking()
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {rahkaranRegions.Count} رکورد منطقه در راهکاران یافت شد", cn);

				// بارگذاری Region های موجود
				var appRegions = await unitOfWork.Repository<Region>()
					.Table
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {appRegions.Count} رکورد منطقه در پایگاه داده برنامه موجود است", cn);

				// ایجاد Dictionary برای نگاشت RahkaranId به Region
				var appRegionsByRahkaranId = appRegions
					.Where(x => x.RahkaranId.HasValue)
					.ToDictionary(x => x.RahkaranId!.Value);

				// ایجاد Dictionary برای نگاشت RahkaranId به RegionId (برای ParentId)
				var regionIdMap = appRegions
					.Where(x => x.RahkaranId.HasValue)
					.ToDictionary(x => x.RahkaranId!.Value, x => x.Id);

				var newRegions = new List<Region>();
				int updatedRegionsCount = 0;

				foreach (var rahkaranRegion in rahkaranRegions)
				{
					if (!appRegionsByRahkaranId.TryGetValue(rahkaranRegion.RegionalDivisionID, out var existRegion))
					{
						// ایجاد رکورد جدید
						long? parentId = null;
						if (rahkaranRegion.ParentRef.HasValue && regionIdMap.TryGetValue(rahkaranRegion.ParentRef.Value, out var parentRegionId))
						{
							parentId = parentRegionId;
						}
						else if (rahkaranRegion.ParentRef.HasValue)
						{
							await jobLogger?.LogWarningAsync($"منطقه والد با RahkaranId {rahkaranRegion.ParentRef} برای منطقه {rahkaranRegion.Name} یافت نشد", 0, cn);
						}

						newRegions.Add(new Region
						{
							RahkaranId = rahkaranRegion.RegionalDivisionID,
							Name = rahkaranRegion.Name ?? string.Empty,
							Title_EN = rahkaranRegion.Title_EN,
							ParentId = parentId,
							Type = rahkaranRegion.Type ?? RegionTypeEnum.City,
							Code = rahkaranRegion.Code,
							Abbreviation = rahkaranRegion.Abbreviation,
							LocalCode = rahkaranRegion.LocalCode,
							FinanceCode = rahkaranRegion.FinanceCode
						});

					 
					}
					else
					{
						// به‌روزرسانی رکورد موجود
						bool isModified = false;

						if (existRegion.Name != rahkaranRegion.Name)
						{
							existRegion.Name = rahkaranRegion.Name ?? string.Empty;
							isModified = true;
						}

						if (existRegion.Title_EN != rahkaranRegion.Title_EN)
						{
							existRegion.Title_EN = rahkaranRegion.Title_EN;
							isModified = true;
						}

						// به‌روزرسانی ParentId
						long? newParentId = null;
						if (rahkaranRegion.ParentRef.HasValue && regionIdMap.TryGetValue(rahkaranRegion.ParentRef.Value, out var newParentRegionId))
						{
							newParentId = newParentRegionId;
						}
						else if (rahkaranRegion.ParentRef.HasValue)
						{
							await jobLogger?.LogWarningAsync($"منطقه والد با RahkaranId {rahkaranRegion.ParentRef} برای منطقه {rahkaranRegion.Name} یافت نشد", 0, cn);
						}

						if (existRegion.ParentId != newParentId)
						{
							existRegion.ParentId = newParentId;
							isModified = true;
						}

						var newType = rahkaranRegion.Type ?? 0;
						if (existRegion.Type != newType)
						{
							existRegion.Type = newType;
							isModified = true;
						}

						if (existRegion.Code != rahkaranRegion.Code)
						{
							existRegion.Code = rahkaranRegion.Code;
							isModified = true;
						}

						if (existRegion.Abbreviation != rahkaranRegion.Abbreviation)
						{
							existRegion.Abbreviation = rahkaranRegion.Abbreviation;
							isModified = true;
						}

						if (existRegion.LocalCode != rahkaranRegion.LocalCode)
						{
							existRegion.LocalCode = rahkaranRegion.LocalCode;
							isModified = true;
						}

						if (existRegion.FinanceCode != rahkaranRegion.FinanceCode)
						{
							existRegion.FinanceCode = rahkaranRegion.FinanceCode;
							isModified = true;
						}

						if (isModified)
						{
							updatedRegionsCount++;
						 
						}
					}
				}

				if (newRegions.Any())
				{
					await jobLogger?.LogInfoAsync($"افزودن {newRegions.Count} منطقه جدید به پایگاه داده", cn);
					await unitOfWork.Repository<Region>().AddRangeAsync(newRegions, cn, false);
					await unitOfWork.SaveChangesAsync(cn);

					// به‌روزرسانی regionIdMap برای رکوردهای جدید
					var newRahkaranIds = newRegions.Where(nr => nr.RahkaranId.HasValue).Select(nr => nr.RahkaranId!.Value).ToList();
					var newlyAddedRegions = await unitOfWork.Repository<Region>()
						.Table
						.Where(x => x.RahkaranId.HasValue && newRahkaranIds.Contains(x.RahkaranId.Value))
						.ToListAsync(cn);

					foreach (var newRegion in newlyAddedRegions)
					{
						if (newRegion.RahkaranId.HasValue)
						{
							regionIdMap[newRegion.RahkaranId.Value] = newRegion.Id;
						}
					}

					// به‌روزرسانی ParentId برای رکوردهای جدید که Parent آنها در همان batch اضافه شده
					int parentUpdatedCount = 0;
					foreach (var newRegion in newlyAddedRegions)
					{
						if (newRegion.RahkaranId.HasValue)
						{
							var rahkaranRegion = rahkaranRegions.FirstOrDefault(r => r.RegionalDivisionID == newRegion.RahkaranId.Value);
							if (rahkaranRegion?.ParentRef.HasValue == true)
							{
								if (regionIdMap.TryGetValue(rahkaranRegion.ParentRef.Value, out var parentRegionId))
								{
									if (newRegion.ParentId != parentRegionId)
									{
										newRegion.ParentId = parentRegionId;
										parentUpdatedCount++;
									}
								}
							}
						}
					}

					if (parentUpdatedCount > 0)
					{
						await jobLogger?.LogInfoAsync($"به‌روزرسانی {parentUpdatedCount} ParentId برای رکوردهای جدید", cn);
						await unitOfWork.SaveChangesAsync(cn);
					}
				}

				if (updatedRegionsCount > 0)
				{
					await jobLogger?.LogInfoAsync($"به‌روزرسانی {updatedRegionsCount} منطقه موجود", cn);
				}

				if (!newRegions.Any())
				{
					await unitOfWork.SaveChangesAsync(cn);
				}

				await jobLogger?.LogInfoAsync("همگام‌سازی مناطق جغرافیایی با موفقیت انجام شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

	}
}
