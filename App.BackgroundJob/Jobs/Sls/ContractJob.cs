using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.FIN;
using Entities.App.SLS;
using Entities.App.SLS.Enums;
using Entities.Rahkaran.SLS3;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Linq;

namespace App.BackgroundJob.Jobs.Sls
{
	public class ContractJob(RahkaranDbContext Rdb, IUnitOfWork unitOfWork)
	{

		[JobHandler("افزودن قرارداد از راهکاران")]
		public async Task AddContractFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی قراردادها از راهکاران", cn);

				await jobLogger?.LogInfoAsync("در حال بارگذاری داده‌های قرارداد از راهکاران...", cn);
				var rahkaranContracts = await Rdb.RahkaranContract
					.AsNoTracking()
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {rahkaranContracts.Count} رکورد قرارداد در راهکاران یافت شد", cn);

				// بارگذاری entity های مرتبط برای نگاشت
				await jobLogger?.LogInfoAsync("در حال بارگذاری entity های مرتبط...", cn);
				var appCustomers = await unitOfWork.Repository<Customer>()
					.Table
					.ToListAsync(cn);
				var customerMap = appCustomers.ToDictionary(x => x.HamkaranId, x => x.Id);

				var appDLs = await unitOfWork.Repository<DL>()
					.Table
					.Where(x => x.HamkaranId.HasValue)
					.ToListAsync(cn);
				var dlMap = appDLs.ToDictionary(x => x.HamkaranId!.Value, x => x.Id);

				await jobLogger?.LogInfoAsync($"تعداد {customerMap.Count} مشتری و {dlMap.Count} حساب تفصیلی در پایگاه داده برنامه موجود است", cn);

				// بارگذاری Contract های موجود
				var appContracts = await unitOfWork.Repository<Contract>()
					.Table
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {appContracts.Count} رکورد قرارداد در پایگاه داده برنامه موجود است", cn);

				var appContractsDict = appContracts
					.Where(x => x.HamkaranId.HasValue)
					.ToDictionary(x => x.HamkaranId!.Value);

				var newContracts = new List<Contract>();
				int updatedContractsCount = 0;
				int skippedCount = 0;

				foreach (var rahkaranContract in rahkaranContracts)
				{
					// پیدا کردن CustomerId بر اساس CustomerRef
					long? customerId = null;
					if (rahkaranContract.CustomerRef.HasValue && customerMap.TryGetValue(rahkaranContract.CustomerRef.Value, out var foundCustomerId))
					{
						customerId = foundCustomerId;
					}
					else if (rahkaranContract.CustomerRef.HasValue)
					{
						await jobLogger?.LogWarningAsync($"مشتری با HamkaranId {rahkaranContract.CustomerRef} برای قرارداد یافت نشد", 0, cn);
					}

					 
					 

					if (!appContractsDict.TryGetValue(rahkaranContract.ContractID, out var existContract))
					{
						// ایجاد رکورد جدید
						newContracts.Add(new Contract
						{
							HamkaranId = rahkaranContract.ContractID,
							Title = rahkaranContract.Title,
							Number = rahkaranContract.Number,
							ContractNumber = rahkaranContract.ContractNumber,
							DateMiladi = rahkaranContract.Date,
							CustomerId = customerId,
						 
							Status = rahkaranContract.State.HasValue ? (ContractStatusEnum?)rahkaranContract.State.Value : null,
							SalesType = rahkaranContract.SalesTypeRef.HasValue ? (ContractSalesTypeEnum?)rahkaranContract.SalesTypeRef.Value : null,
							NetPrice = rahkaranContract.NetPrice,
							Description = rahkaranContract.Description
						});
					}
					else
					{
						// به‌روزرسانی رکورد موجود
						bool isModified = false;

						if (existContract.Title != rahkaranContract.Title)
						{
							existContract.Title = rahkaranContract.Title;
							isModified = true;
						}

						if (existContract.Number != rahkaranContract.Number)
						{
							existContract.Number = rahkaranContract.Number;
							isModified = true;
						}

						if (existContract.ContractNumber != rahkaranContract.ContractNumber)
						{
							existContract.ContractNumber = rahkaranContract.ContractNumber;
							isModified = true;
						}

						if (existContract.DateMiladi != rahkaranContract.Date)
						{
							existContract.DateMiladi = rahkaranContract.Date;
							isModified = true;
						}

						if (existContract.CustomerId != customerId)
						{
							existContract.CustomerId = customerId;
							isModified = true;
						}

						 

						var newStatus = rahkaranContract.State.HasValue ? (ContractStatusEnum?)rahkaranContract.State.Value : null;
						if (existContract.Status != newStatus)
						{
							existContract.Status = newStatus;
							isModified = true;
						}

						var newSalesType = rahkaranContract.SalesTypeRef.HasValue ? (ContractSalesTypeEnum?)rahkaranContract.SalesTypeRef.Value : null;
						if (existContract.SalesType != newSalesType)
						{
							existContract.SalesType = newSalesType;
							isModified = true;
						}

						if (existContract.NetPrice != rahkaranContract.NetPrice)
						{
							existContract.NetPrice = rahkaranContract.NetPrice;
							isModified = true;
						}

						if (existContract.Description != rahkaranContract.Description)
						{
							existContract.Description = rahkaranContract.Description;
							isModified = true;
						}

						if (isModified)
						{
							updatedContractsCount++;
						}
					}
				}

				if (skippedCount > 0)
				{
					await jobLogger?.LogWarningAsync($"تعداد {skippedCount} رکورد به دلیل عدم وجود entity های مرتبط رد شد", 0, cn);
				}

				if (newContracts.Any())
				{
					await jobLogger?.LogInfoAsync($"افزودن {newContracts.Count} قرارداد جدید به پایگاه داده", cn);
					await unitOfWork.Repository<Contract>().AddRangeAsync(newContracts, cn, false);
				}

				if (updatedContractsCount > 0)
				{
					await jobLogger?.LogInfoAsync($"به‌روزرسانی {updatedContractsCount} قرارداد موجود", cn);
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync("همگام‌سازی قراردادها با موفقیت انجام شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}
	}
}
