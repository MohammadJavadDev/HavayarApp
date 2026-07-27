using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Gnr;
using Entities.App.Sale;
using Entities.App.SLS;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Linq;

namespace App.BackgroundJob.Jobs.Sls
{
	public class CustomerJob(RahkaranDbContext Rdb, IUnitOfWork unitOfWork)
	{

		[JobHandler("افزودن مشتریان از راهکاران")]
		public async Task AddCustomerFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی مشتریان از راهکاران", cn);

				var sqlRow = @" SELECT CAST(A.Number as int) Number ,
				  A.PartyRef,
				  a.CustomerID
			    FROM SLS3.Customer AS A
			";

				await jobLogger?.LogInfoAsync("در حال اجرای کوئری برای دریافت داده‌های مشتریان از راهکاران...", cn);
				var rahakarnData = await Rdb.Database.SqlQueryRaw<AddCustomerDto>(sqlRow).ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {rahakarnData.Count} رکورد مشتری از راهکاران دریافت شد", cn);

				// بارگذاری Party ها برای نگاشت PartyRef به PartyId
				await jobLogger?.LogInfoAsync("در حال بارگذاری Party ها از پایگاه داده برنامه...", cn);
				var appParties = await unitOfWork.Repository<Party>()
					.Table
					.Where(x => x.HamkaranId.HasValue)
					.ToListAsync(cn);

				var partyMap = appParties
					.ToDictionary(x => x.HamkaranId!.Value, x => x.Id);

				await jobLogger?.LogInfoAsync($"تعداد {partyMap.Count} Party با HamkaranId در پایگاه داده برنامه موجود است", cn);

				// بارگذاری Customer های موجود
				var appCustomers = await unitOfWork.Repository<Customer>()
					.Table
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {appCustomers.Count} مشتری در پایگاه داده برنامه موجود است", cn);

				var appCustomersDict = appCustomers
					.ToDictionary(x => x.HamkaranId);

				var newCustomers = new List<Customer>();
				int updatedCustomersCount = 0;
				int skippedCount = 0;

				foreach (var rahkaranData in rahakarnData)
				{
					// پیدا کردن PartyId بر اساس HamkaranId (PartyRef)
					if (!partyMap.TryGetValue(rahkaranData.PartyRef, out var partyId))
					{
						await jobLogger?.LogWarningAsync($"Party با HamkaranId {rahkaranData.PartyRef} برای مشتری یافت نشد", 0, cn);
						skippedCount++;
						continue;
					}

					if (!appCustomersDict.TryGetValue((long)rahkaranData.CustomerID, out var existCustomer))
					{
						// ایجاد رکورد جدید
						newCustomers.Add(new Customer
						{
							PartyId = (long)partyId,
							Code = rahkaranData.Number ,
							HamkaranId = rahkaranData.CustomerID
						});
					}
					else
					{
						// به‌روزرسانی رکورد موجود
						bool isModified = false;

						var newCode = rahkaranData.Number ;
						if (existCustomer.Code != newCode)
						{
							existCustomer.Code = newCode;
							isModified = true;
						}

						if (existCustomer.HamkaranId != rahkaranData.CustomerID)
						{
							existCustomer.HamkaranId = rahkaranData.CustomerID;
							isModified = true;
						}

						if (isModified)
						{
							updatedCustomersCount++;
						}
					}
				}

				if (skippedCount > 0)
				{
					await jobLogger?.LogWarningAsync($"تعداد {skippedCount} رکورد به دلیل عدم وجود Party مربوطه رد شد", 0, cn);
				}

				if (newCustomers.Any())
				{
					await jobLogger?.LogInfoAsync($"افزودن {newCustomers.Count} مشتری جدید به پایگاه داده", cn);
					await unitOfWork.Repository<Customer>().AddRangeAsync(newCustomers, cn, false);
				}

				if (updatedCustomersCount > 0)
				{
					await jobLogger?.LogInfoAsync($"به‌روزرسانی {updatedCustomersCount} مشتری موجود", cn);
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync("همگام‌سازی مشتریان با موفقیت انجام شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		private class AddCustomerDto
		{
			public int Number { get; set; }
			public long PartyRef { get; set; }
			public long CustomerID { get; set; }
		}
	}
}
