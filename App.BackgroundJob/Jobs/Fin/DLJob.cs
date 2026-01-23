using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.FIN;
using Entities.Rahkaran.FIN3;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Linq;

namespace App.BackgroundJob.Jobs.Fin
{
    public class DLJob(RahkaranDbContext Rdb, IUnitOfWork unitOfWork)
    {
        [JobHandler("افزودن حساب تفصیلی از راهکاران")]
        public async Task AddDLFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
        {
            try
            {
                await jobLogger?.LogInfoAsync("شروع همگام‌سازی حساب تفصیلی از راهکاران", cn);

                // همگام‌سازی DLType (جدول والد)
                await jobLogger?.LogInfoAsync("در حال بارگذاری داده‌های نوع تفصیل از راهکاران...", cn);
                var rahkaranDLTypes = await Rdb.RahkaranDLType
                    .AsNoTracking()
                    .ToListAsync(cn);

                await jobLogger?.LogInfoAsync($"تعداد {rahkaranDLTypes.Count} رکورد نوع تفصیل در راهکاران یافت شد", cn);

                var appDLTypes = await unitOfWork.Repository<DLType>()
                    .Table
                    .ToListAsync(cn);

                await jobLogger?.LogInfoAsync($"تعداد {appDLTypes.Count} رکورد نوع تفصیل در پایگاه داده برنامه موجود است", cn);

                var appDLTypesDict = appDLTypes
                    .Where(x => x.HamkaranId.HasValue)
                    .ToDictionary(x => x.HamkaranId!.Value);

                var newDLTypes = new List<DLType>();
                int updatedDLTypesCount = 0;

                foreach (var rahkaranDLType in rahkaranDLTypes)
                {
                    if (!appDLTypesDict.TryGetValue(rahkaranDLType.DLTypeID, out var existDLType))
                    {
                        // ایجاد رکورد جدید
                        newDLTypes.Add(new DLType
                        {
                            HamkaranId = rahkaranDLType.DLTypeID,
                            Title = rahkaranDLType.Title ?? string.Empty,
                            TitleInEnglish = rahkaranDLType.Title_En
                        });
                    }
                    else
                    {
                        // به‌روزرسانی رکورد موجود
                        bool isModified = false;

                        if (existDLType.Title != rahkaranDLType.Title)
                        {
                            existDLType.Title = rahkaranDLType.Title ?? string.Empty;
                            isModified = true;
                        }

                        if (existDLType.TitleInEnglish != rahkaranDLType.Title_En)
                        {
                            existDLType.TitleInEnglish = rahkaranDLType.Title_En;
                            isModified = true;
                        }

                        if (isModified)
                        {
                            updatedDLTypesCount++;
                        }
                    }
                }

                if (newDLTypes.Any())
                {
                    await jobLogger?.LogInfoAsync($"افزودن {newDLTypes.Count} نوع تفصیل جدید به پایگاه داده", cn);
                    await unitOfWork.Repository<DLType>().AddRangeAsync(newDLTypes, cn, false);
                }

                if (updatedDLTypesCount > 0)
                {
                    await jobLogger?.LogInfoAsync($"به‌روزرسانی {updatedDLTypesCount} نوع تفصیل موجود", cn);
                }

                await unitOfWork.SaveChangesAsync(cn);
                await jobLogger?.LogInfoAsync("همگام‌سازی نوع تفصیل با موفقیت انجام شد", cn);

                // همگام‌سازی DL (جدول فرزند)
                await jobLogger?.LogInfoAsync("شروع همگام‌سازی حساب تفصیلی از راهکاران...", cn);
                var rahkaranDLs = await Rdb.RahkaranDL
                    .AsNoTracking()
                    .ToListAsync(cn);

                await jobLogger?.LogInfoAsync($"تعداد {rahkaranDLs.Count} رکورد حساب تفصیلی در راهکاران یافت شد", cn);

                // بارگذاری مجدد DLType ها برای نگاشت DLTypeRef
                var allAppDLTypes = await unitOfWork.Repository<DLType>()
                    .Table
                    .ToListAsync(cn);

                var dlTypeMap = allAppDLTypes
                    .Where(x => x.HamkaranId.HasValue)
                    .ToDictionary(x => x.HamkaranId!.Value, x => x.Id);

                var appDLs = await unitOfWork.Repository<DL>()
                    .Table
                    .ToListAsync(cn);

                await jobLogger?.LogInfoAsync($"تعداد {appDLs.Count} رکورد حساب تفصیلی در پایگاه داده برنامه موجود است", cn);

                var appDLsDict = appDLs
                    .Where(x => x.HamkaranId.HasValue)
                    .ToDictionary(x => x.HamkaranId!.Value);

                var newDLs = new List<DL>();
                int updatedDLsCount = 0;
                int skippedDLsCount = 0;

                foreach (var rahkaranDL in rahkaranDLs)
                {
                    // پیدا کردن TypeId بر اساس DLTypeRef
                    if (!dlTypeMap.TryGetValue(rahkaranDL.DLTypeRef, out var typeId))
                    {
                        await jobLogger?.LogWarningAsync($"نوع تفصیل با HamkaranId {rahkaranDL.DLTypeRef} برای حساب تفصیلی یافت نشد", 0, cn);
                        skippedDLsCount++;
                        continue;
                    }

                    if (!appDLsDict.TryGetValue(rahkaranDL.DLID, out var existDL))
                    {
                        // ایجاد رکورد جدید
                        newDLs.Add(new DL
                        {
                            HamkaranId = rahkaranDL.DLID,
                            Title = rahkaranDL.Title ?? string.Empty,
                            Code = rahkaranDL.Code ?? string.Empty,
                            TitleEnglish = rahkaranDL.Title_En,
                            TypeId = (long)typeId
                        });
                    }
                    else
                    {
                        // به‌روزرسانی رکورد موجود
                        bool isModified = false;

                        if (existDL.Title != rahkaranDL.Title)
                        {
                            existDL.Title = rahkaranDL.Title ?? string.Empty;
                            isModified = true;
                        }

                        if (existDL.Code != rahkaranDL.Code)
                        {
                            existDL.Code = rahkaranDL.Code ?? string.Empty;
                            isModified = true;
                        }

                        if (existDL.TitleEnglish != rahkaranDL.Title_En)
                        {
                            existDL.TitleEnglish = rahkaranDL.Title_En;
                            isModified = true;
                        }

                        if (existDL.TypeId != typeId)
                        {
                            existDL.TypeId = (long)typeId;
                            isModified = true;
                        }

                        if (isModified)
                        {
                            updatedDLsCount++;
                        }
                    }
                }

                if (skippedDLsCount > 0)
                {
                    await jobLogger?.LogWarningAsync($"تعداد {skippedDLsCount} رکورد به دلیل عدم وجود نوع تفصیل مربوطه رد شد", 0, cn);
                }

                if (newDLs.Any())
                {
                    await jobLogger?.LogInfoAsync($"افزودن {newDLs.Count} حساب تفصیلی جدید به پایگاه داده", cn);
                    await unitOfWork.Repository<DL>().AddRangeAsync(newDLs, cn, false);
                }

                if (updatedDLsCount > 0)
                {
                    await jobLogger?.LogInfoAsync($"به‌روزرسانی {updatedDLsCount} حساب تفصیلی موجود", cn);
                }

                await unitOfWork.SaveChangesAsync(cn);
                await jobLogger?.LogInfoAsync("همگام‌سازی حساب تفصیلی با موفقیت انجام شد", cn);
                await jobLogger?.LogInfoAsync("همگام‌سازی کامل نوع تفصیل و حساب تفصیلی با موفقیت به پایان رسید", cn);
            }
            catch (Exception ex)
            {
                await jobLogger?.LogExceptionAsync(ex, cn);
                throw;
            }
        }
    }
}
