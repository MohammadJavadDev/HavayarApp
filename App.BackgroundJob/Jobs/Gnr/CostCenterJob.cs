using Common.Attributes;
using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.App.FIN;
using Entities.App.Gnr;
using Entities.App.Gnr.Enums;
using Entities.App.SLS;
using Entities.Base;
using Entities.Rahkaran.USR3;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Linq;

namespace App.BackgroundJob.Jobs.Gnr
{
	public class CostCenterJob(RahkaranDbContext Rdb, IUnitOfWork unitOfWork)
	{

		[JobHandler("افزودن مرکز هزینه از راهکاران")]
		public async Task AddCostCenterFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی مراکز هزینه از راهکاران", cn);

				var sqlRow = @"SELECT CostCenterID,
                                        Number AS CCCode,
                                        [Name] AS Title,
                                        CASE WHEN CostCenter.Type = 2 THEN 0 WHEN CostCenter.Type = 3 THEN 1 WHEN CostCenter.Type = 1 THEN 2 END  AS CenterType,
                                        DL.DLID AS DLRef,
                                        CASE WHEN CostCenter.[State] = 1 THEN 1 ELSE  0 END AS active
                                FROM GNR3.CostCenter AS CostCenter
                                JOIN FIN3.DL AS Dl ON Dl.ReferenceID = CostCenter.CostCenterID AND Dl.DLTypeRef = 4 AND Dl.EntityCode = 251";

				await jobLogger?.LogInfoAsync("در حال اجرای کوئری برای دریافت داده‌های مراکز هزینه از راهکاران...", cn);

				var rahakarnData = await Rdb.Database.SqlQueryRaw<AddCostCenterDto>(sqlRow).ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {rahakarnData.Count} رکورد مرکز هزینه از راهکاران دریافت شد", cn);

				// بارگذاری CostCenter های موجود
				var appCostCenters = await unitOfWork.Repository<CostCenter>()
					.Table
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {appCostCenters.Count} مرکز هزینه در پایگاه داده برنامه موجود است", cn);


			 var appDLs = await unitOfWork.Repository<DL>()
				.TableNoTracking
				.ToListAsync(cn);

				// ایجاد Dictionary برای نگاشت Code به  CostCenters
				var appSalesOfficesDict = appCostCenters
					.Where(x => x.HamkaranId != null)
					.ToDictionary(x => x.HamkaranId!);

				var appappDLsDic = appDLs.Where(c=>c.HamkaranId != null)
					.ToDictionary(x=>x.HamkaranId!);

				var newCostCenters = new List<CostCenter>();
				int updatedCostCentersCount = 0;

				foreach (var rahkaranData in rahakarnData)
				{

					if(!appappDLsDic.TryGetValue(rahkaranData.DLRef , out var exitDl))
					{
						await jobLogger?.LogInfoAsync($" Dl با شناسه{rahkaranData.DLRef}  در اپلیکیشن یافت نشد", cn);
					}
					 

					if (!appSalesOfficesDict.TryGetValue(rahkaranData.CostCenterID, out var existSalesOffice))
					{
						// ایجاد رکورد جدید
						newCostCenters.Add(new CostCenter
						{
							Number = rahkaranData.CCCode.ToLong(),
							Title = rahkaranData.Title,
							DlId = exitDl.Id,
							IsActive = (IsActiveEnum) rahkaranData.active,
							Type = (CostCenterTypeEnum) rahkaranData.CenterType


						});
					}
					else
					{
						// به‌روزرسانی رکورد موجود
						bool isModified = false;

						if (existSalesOffice.Title != rahkaranData.Title)
						{
							existSalesOffice.Title = rahkaranData.Title;
							isModified = true;
						}

						if (existSalesOffice.Number != rahkaranData.CCCode.ToLong())
						{
							existSalesOffice.Number = rahkaranData.CCCode.ToLong();
							isModified = true;
						}

						if ((int)existSalesOffice.Type != rahkaranData.CenterType)
						{
							existSalesOffice.Type = (CostCenterTypeEnum)rahkaranData.CenterType;
							isModified = true;
						}

						if ((int)existSalesOffice.IsActive != rahkaranData.active)
						{
							existSalesOffice.IsActive = (IsActiveEnum)rahkaranData.active;
							isModified = true;
						}

						if (isModified)
						{
							updatedCostCentersCount++;
						}
					}
				}

				if (newCostCenters.Any())
				{
					await jobLogger?.LogInfoAsync($"افزودن {newCostCenters.Count} مرکز هزینه جدید به پایگاه داده", cn);
					await unitOfWork.Repository<CostCenter>().AddRangeAsync(newCostCenters, cn, false);
				}

				if (updatedCostCentersCount > 0)
				{
					await jobLogger?.LogInfoAsync($"به‌روزرسانی {updatedCostCentersCount} مرکز هزینه موجود", cn);
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync("همگام‌سازی مراکز هزینه با موفقیت انجام شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}
		private class AddCostCenterDto
		{
			public string CCCode { get; set; }

			public string Title { get; set; }

			public int CenterType { get; set; }

			public long DLRef { get; set; }
			public long CostCenterID { get; set; }
			public int active { get; set; }
		}
	}
}
