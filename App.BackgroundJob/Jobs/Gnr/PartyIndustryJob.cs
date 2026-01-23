using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Gnr;
using Entities.Rahkaran.USR3;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Linq;

namespace App.BackgroundJob.Jobs.Gnr
{
	public class PartyIndustryJob(RahkaranDbContext Rdb, IUnitOfWork unitOfWork)
	{

		[JobHandler("افزودن صنعت شخص/شرکت از راهکاران")]
		public async Task AddPartsFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی صنعت شخص/شرکت از راهکاران", cn);

				// همگام‌سازی PartyIndustry (جدول والد)
				await jobLogger?.LogInfoAsync("در حال بارگذاری داده‌های صنعت از راهکاران...", cn);
				var rahkaranPartyIndustries = await Rdb.RahkaranGnr_PartyIndustries
					.AsNoTracking()
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {rahkaranPartyIndustries.Count} رکورد صنعت در راهکاران یافت شد", cn);

				var appPartyIndustries = await unitOfWork.Repository<PartyIndustry>()
					.Table
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {appPartyIndustries.Count} رکورد صنعت در پایگاه داده برنامه موجود است", cn);

				var appPartyIndustriesDict = appPartyIndustries
					.Where(x => x.HamkaranId.HasValue)
					.ToDictionary(x => x.HamkaranId!.Value);

				var newPartyIndustries = new List<PartyIndustry>();
				int updatedPartyIndustriesCount = 0;

				foreach (var rahkaranIndustry in rahkaranPartyIndustries)
				{
					if (!appPartyIndustriesDict.TryGetValue(rahkaranIndustry.Gnr_PartyIndustryID, out var existIndustry))
					{
						// ایجاد رکورد جدید
						newPartyIndustries.Add(new PartyIndustry
						{
							HamkaranId = rahkaranIndustry.Gnr_PartyIndustryID,
							Title = rahkaranIndustry.Title ?? string.Empty,
							Code = rahkaranIndustry.Code.HasValue ? (long)rahkaranIndustry.Code.Value : 0
						});

						await jobLogger?.LogDebugAsync($"افزودن صنعت جدید: {rahkaranIndustry.Title} (ID: {rahkaranIndustry.Gnr_PartyIndustryID})", 0, cn);
					}
					else
					{
						// به‌روزرسانی رکورد موجود
						bool isModified = false;

						if (existIndustry.Title != rahkaranIndustry.Title)
						{
							existIndustry.Title = rahkaranIndustry.Title ?? string.Empty;
							isModified = true;
						}

						var newCode = rahkaranIndustry.Code.HasValue ? (long)rahkaranIndustry.Code.Value : 0;
						if (existIndustry.Code != newCode)
						{
							existIndustry.Code = newCode;
							isModified = true;
						}

						if (isModified)
						{
							updatedPartyIndustriesCount++;
							await jobLogger?.LogDebugAsync($"به‌روزرسانی صنعت موجود: {rahkaranIndustry.Title} (ID: {rahkaranIndustry.Gnr_PartyIndustryID})", 0, cn);
						}
					}
				}

				if (newPartyIndustries.Any())
				{
					await jobLogger?.LogInfoAsync($"افزودن {newPartyIndustries.Count} صنعت جدید به پایگاه داده", cn);
					await unitOfWork.Repository<PartyIndustry>().AddRangeAsync(newPartyIndustries, cn, false);
				}

				if (updatedPartyIndustriesCount > 0)
				{
					await jobLogger?.LogInfoAsync($"به‌روزرسانی {updatedPartyIndustriesCount} صنعت موجود", cn);
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync("همگام‌سازی صنعت با موفقیت انجام شد", cn);

				// همگام‌سازی PartyIndustryItem (جدول فرزند)
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی زیر صنعت شخص/شرکت از راهکاران...", cn);
				var rahkaranPartyIndustryItems = await Rdb.RahkaranGnr_PartyIndustryItems
					.AsNoTracking()
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {rahkaranPartyIndustryItems.Count} رکورد زیر صنعت در راهکاران یافت شد", cn);

				// بارگذاری مجدد PartyIndustry ها برای نگاشت _MasterRef به PartyIndustryId
				var allAppPartyIndustries = await unitOfWork.Repository<PartyIndustry>()
					.Table
					.ToListAsync(cn);

				var partyIndustryMap = allAppPartyIndustries
					.Where(x => x.HamkaranId.HasValue)
					.ToDictionary(x => x.HamkaranId!.Value, x => x.Id);

				var appPartyIndustryItems = await unitOfWork.Repository<PartyIndustryItem>()
					.Table
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {appPartyIndustryItems.Count} رکورد زیر صنعت در پایگاه داده برنامه موجود است", cn);

				var appPartyIndustryItemsDict = appPartyIndustryItems
					.Where(x => x.HamkaranId.HasValue)
					.ToDictionary(x => x.HamkaranId!.Value);

				var newPartyIndustryItems = new List<PartyIndustryItem>();
				int updatedPartyIndustryItemsCount = 0;

				foreach (var rahkaranItem in rahkaranPartyIndustryItems)
				{
					// پیدا کردن PartyIndustryId بر اساس _MasterRef
					if (!partyIndustryMap.TryGetValue(rahkaranItem._MasterRef, out var partyIndustryId))
					{
						await jobLogger?.LogWarningAsync($"صنعت والد با ID {rahkaranItem._MasterRef} برای زیر صنعت {rahkaranItem.Title} یافت نشد", 0, cn);
						continue;
					}

					if (!appPartyIndustryItemsDict.TryGetValue(rahkaranItem.Gnr_PartyIndustryItemID, out var existItem))
					{
						// ایجاد رکورد جدید
						newPartyIndustryItems.Add(new PartyIndustryItem
						{
							HamkaranId = rahkaranItem.Gnr_PartyIndustryItemID,
							Title = rahkaranItem.Title ?? string.Empty,
							Code = rahkaranItem.Code.HasValue ? (long)rahkaranItem.Code.Value : 0,
							PartyIndustryId = (long)partyIndustryId
						});

						await jobLogger?.LogDebugAsync($"افزودن زیر صنعت جدید: {rahkaranItem.Title} (ID: {rahkaranItem.Gnr_PartyIndustryItemID})", 0, cn);
					}
					else
					{
						// به‌روزرسانی رکورد موجود
						bool isModified = false;

						if (existItem.Title != rahkaranItem.Title)
						{
							existItem.Title = rahkaranItem.Title ?? string.Empty;
							isModified = true;
						}

						var newCode = rahkaranItem.Code.HasValue ? (long)rahkaranItem.Code.Value : 0;
						if (existItem.Code != newCode)
						{
							existItem.Code = newCode;
							isModified = true;
						}

						if (existItem.PartyIndustryId != partyIndustryId)
						{
							existItem.PartyIndustryId = (long)partyIndustryId;
							isModified = true;
						}

						if (isModified)
						{
							updatedPartyIndustryItemsCount++;
							await jobLogger?.LogDebugAsync($"به‌روزرسانی زیر صنعت موجود: {rahkaranItem.Title} (ID: {rahkaranItem.Gnr_PartyIndustryItemID})", 0, cn);
						}
					}
				}

				if (newPartyIndustryItems.Any())
				{
					await jobLogger?.LogInfoAsync($"افزودن {newPartyIndustryItems.Count} زیر صنعت جدید به پایگاه داده", cn);
					await unitOfWork.Repository<PartyIndustryItem>().AddRangeAsync(newPartyIndustryItems, cn, false);
				}

				if (updatedPartyIndustryItemsCount > 0)
				{
					await jobLogger?.LogInfoAsync($"به‌روزرسانی {updatedPartyIndustryItemsCount} زیر صنعت موجود", cn);
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync("همگام‌سازی زیر صنعت با موفقیت انجام شد", cn);
				await jobLogger?.LogInfoAsync("همگام‌سازی کامل صنعت و زیر صنعت با موفقیت به پایان رسید", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}
	}
}
