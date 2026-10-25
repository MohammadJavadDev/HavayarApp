using App.BackgroundJob.Jobs;
using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Gnr;
using Entities.App.Sale;
using Entities.App.SLS;
using Entities.Base;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Data;
using System.Linq;

namespace App.BackgroundJob.Jobs.Sls
{
	public class CustomerJob(RahkaranDbContext Rdb, IUnitOfWork unitOfWork, ApplicationDbContext db)
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

				var partyMap = await JobLookup.ToUniqueValueMapAsync(
					appParties,
					x => x.HamkaranId!.Value,
					x => x.Id,
					jobLogger,
					"Gnr.Party.HamkaranId",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active);

				await jobLogger?.LogInfoAsync($"تعداد {partyMap.Count} Party با HamkaranId در پایگاه داده برنامه موجود است", cn);

				// بارگذاری Customer های موجود
				var appCustomers = await unitOfWork.Repository<Customer>()
					.Table
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {appCustomers.Count} مشتری در پایگاه داده برنامه موجود است", cn);

				var appCustomersDict = await JobLookup.ToUniqueMapAsync(
					appCustomers,
					x => x.HamkaranId,
					jobLogger,
					"Sls.Customer.HamkaranId",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active);

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

				// نوع صنعت مشتری (از HTS) جدا و کم‌دفعه‌تر در SyncCustomerIndustryFromHts به‌روز می‌شود،
				// نه در هر اجرای این جاب؛ چون هر بار روی linked server [TMS] سنگین است.
				await jobLogger?.LogInfoAsync("همگام‌سازی مشتریان با موفقیت انجام شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		/// <summary>
		/// مثل چاپ قدیمی: نوع صنعت از Gnr_ManCompany.IndustryHdr شرکت مشتری (Party.HamkaranId = Hamkaran_ManCompany_FK).
		/// زمان‌بندی پیش‌فرض: ساعتی (JobDiscoveryService).
		/// </summary>
		[JobHandler(
			"همگام‌سازی نوع صنعت مشتری از HTS",
			"نوع صنعت مشتریان را از Gnr_ManCompany شرکت HTS (linked server TMS) می‌خواند و در SLS.Customer.IndustryId می‌نویسد.")]
		public async Task SyncCustomerIndustryFromHts(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی نوع صنعت مشتریان از HTS", cn);

				// UPDATE شامل خواندن linked server است؛ زمان بیشتر از پیش‌فرض ۳۰ ثانیه
				db.Database.SetCommandTimeout(300);

				var updatedRows = new SqlParameter("@updatedRows", SqlDbType.Int) { Direction = ParameterDirection.Output };
				await db.Database.ExecuteSqlRawAsync(SyncIndustryFromHtsSql, new object[] { updatedRows }, cn);

				var updated = updatedRows.Value is int count ? count : 0;
				await jobLogger?.LogInfoAsync($"صنعت مشتری از شرکت HTS به‌روز شد: {updated}", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		/// <summary>
		/// قبلاً برای هر مشتری یک OUTER APPLY روی [TMS] اجرا می‌شد (هزاران رفت‌وبرگشت راه دور در هر اجرا).
		/// حالا یک بار کل نگاشت «شرکت راهکاران → کد صنعت» (اولین ردیف هر شرکت با کمترین ManCompany_ID)
		/// در #HtsIndustry خوانده می‌شود و UPDATE فقط روی جدول‌های محلی join می‌شود.
		/// </summary>
		private const string SyncIndustryFromHtsSql = """
			SET NOCOUNT ON;

			IF OBJECT_ID('tempdb..#HtsIndustry') IS NOT NULL DROP TABLE #HtsIndustry;

			SELECT x.HamkaranCompanyId, x.IndustryCode
			INTO #HtsIndustry
			FROM (
				SELECT CAST(mc.Hamkaran_ManCompany_FK AS BIGINT) AS HamkaranCompanyId,
				       CAST(mc.IndustryHdr_FK AS BIGINT) AS IndustryCode,
				       ROW_NUMBER() OVER (
				           PARTITION BY CAST(mc.Hamkaran_ManCompany_FK AS BIGINT)
				           ORDER BY mc.ManCompany_ID) AS rn
				FROM [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] mc
				WHERE mc.Hamkaran_ManCompany_FK IS NOT NULL
				  AND mc.Hamkaran_ManCompany_FK <> 0
				  AND mc.IndustryHdr_FK IS NOT NULL
			) x
			WHERE x.rn = 1;

			CREATE CLUSTERED INDEX IX_HtsIndustry ON #HtsIndustry (HamkaranCompanyId);

			UPDATE c
			SET c.IndustryId = ind.Id
			FROM SLS.Customer c
			INNER JOIN Gnr.Party p ON p.Id = c.PartyId
			INNER JOIN #HtsIndustry h ON h.HamkaranCompanyId = p.HamkaranId
			INNER JOIN Gnr.PartyIndustry ind ON ind.Code = h.IndustryCode
			WHERE c.IndustryId IS NULL OR c.IndustryId <> ind.Id;

			SET @updatedRows = @@ROWCOUNT;

			DROP TABLE #HtsIndustry;
			""";

		private class AddCustomerDto
		{
			public int Number { get; set; }
			public long PartyRef { get; set; }
			public long CustomerID { get; set; }
		}
	}
}
