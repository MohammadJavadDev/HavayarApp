using Common.Attributes;
using Common.Utilities;
using Data;
using Entities.App.SLS;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Services.Job;

namespace App.BackgroundJob.Jobs.Sls
{
	/// <summary>
	/// معادل HTS WriteData.Update_Sales_CustomerAddressAgencyDls
	/// (HtsTaskService.AfterSalesAndRepairSystemTask هر ۲۰ دقیقه، ساعت ۶–۲۱).
	/// فقط ردیف‌هایی که AgencyDlId خالی است از آدرس پیش‌فرض ERPS Type=2 پر می‌شوند.
	/// </summary>
	public class CustomerAddressJob(ApplicationDbContext db, RahkaranDbContext rdb, HtsDbContext htsDb)
	{
		private const string ImportUserName = "job-customeraddress-import";
		[JobHandler("تخصیص تفصیل نمایندگی سایت مشتری از منطقه راهکاران")]
		public async Task FillEmptyAgencyDlFromRahkaranRegion(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			var now = DateTime.Now;
			if (now.Hour < 6 || now.Hour > 21)
			{
				await jobLogger?.LogInfoAsync("خارج از بازه ۶ تا ۲۱ — اجرا نشد (مطابق HTS)", cn);
				return;
			}

			await jobLogger?.LogInfoAsync("شروع تخصیص تفصیل نمایندگی برای سایت‌های بدون DL", cn);

			// JOIN همزمان TMS و ERPS از HavayarApp داده را ردیف‌به‌ردیف می‌کشد و روی timeout پیش‌فرض ۳۰ ثانیه EF قطع می‌شود.
			// هر سمت جدا با OPENQUERY خوانده می‌شود (مثل HTS که ERPS را از خود TotalSystem می‌خواند) و نگاشت محلی است.
			db.Database.SetCommandTimeout(180);

			var sql = """
				SET NOCOUNT ON;

				SELECT *
				INTO #Sites
				FROM OPENQUERY(TMS, N'
				SELECT a.Customer_Address_ID, mc.Hamkaran_ManCompany_FK, a.Address_Title
				FROM dbo.Crm_Customer_Address AS a
				INNER JOIN dbo.Crm_Customer AS c ON c.Customer_ID = a.Customer_FK
				INNER JOIN dbo.Gnr_ManCompany AS mc ON mc.ManCompany_ID = c.ManCompany_FK
				WHERE a.AgencyDlId IS NULL
				');

				SELECT PartyID, RegionalDivisionRef, ParentRef
				INTO #Regions
				FROM
				(
					SELECT
						PartyID,
						RegionalDivisionRef,
						ParentRef,
						ROW_NUMBER() OVER (PARTITION BY PartyID ORDER BY RegionalDivisionRef, ParentRef) AS rn
					FROM OPENQUERY(ERPS, N'
				SELECT cu.PartyRef AS PartyID, addr.RegionalDivisionRef, rd.ParentRef
				FROM Erps.SLS3.Customer AS cu
				INNER JOIN Erps.SLS3.CustomerAddress AS ca
					ON ca.CustomerRef = cu.CustomerID
				   AND ca.IsDefault = 1
				   AND ca.Type = 2
				INNER JOIN Erps.GNR3.Address AS addr ON addr.AddressID = ca.AddressRef
				INNER JOIN Erps.GNR3.RegionalDivision AS rd ON rd.RegionalDivisionID = addr.RegionalDivisionRef
				')
				) AS d
				WHERE rn = 1;

				SELECT *
				INTO #Agency
				FROM OPENQUERY(TMS, N'
				SELECT
					ad.Acc_DL_ID,
					ad.Hamkaran_Acc_DL_FK,
					MIN(mc.Hamkaran_ManCompany_FK) AS Hamkaran_ManCompany_FK
				FROM dbo.Acc_DL AS ad
				LEFT JOIN dbo.Gnr_ManCompany AS mc
					ON LTRIM(RTRIM(ISNULL(mc.DlRef, N''''))) IN (
						LTRIM(RTRIM(CAST(ad.Hamkaran_Acc_DL_FK AS NVARCHAR(20)))),
						RIGHT(N''00000'' + LTRIM(RTRIM(CAST(ad.Hamkaran_Acc_DL_FK AS NVARCHAR(20)))), 5)
					)
				WHERE ad.Acc_DL_ID IN (26118, 21480, 391729, 389616, 387620, 162365, 383882, 14197, 378723, 380341)
				GROUP BY ad.Acc_DL_ID, ad.Hamkaran_Acc_DL_FK
				');

				SET NOCOUNT OFF;

				;WITH Mapped AS
				(
					SELECT
						s.Customer_Address_ID AS HtsAddressId,
						CASE
							WHEN r.RegionalDivisionRef IN (121000103, 300000047, 58, 59, 52, 89, 300000100, 37, 44)
							  OR r.ParentRef IN (121000103, 300000047, 58, 59, 52, 89, 300000100, 377)
								THEN 26118
							WHEN r.RegionalDivisionRef IN (62, 121000052, 72, 9)
							  OR r.ParentRef IN (62, 121000052, 72, 9)
								THEN 21480
							WHEN r.RegionalDivisionRef IN (12, 33, 34, 11, 13, 30, 49)
							  OR r.ParentRef IN (12, 33, 34, 11, 13, 30, 49)
								THEN 391729
							WHEN r.RegionalDivisionRef IN (15, 300000032, 300000096)
							  OR r.ParentRef IN (15, 300000032, 300000096)
								THEN 389616
							WHEN r.RegionalDivisionRef IN (26)
								THEN 387620
							WHEN r.RegionalDivisionRef IN (31, 66, 56, 98, 32, 50, 47, 73, 94)
								THEN 162365
							WHEN r.RegionalDivisionRef IN (8, 121000179, 121000002)
								THEN 383882
							WHEN r.RegionalDivisionRef IN (17, 18)
								THEN 14197
							WHEN r.RegionalDivisionRef IN (6, 19, 71, 121000114)
								THEN 387620
							WHEN r.RegionalDivisionRef IN (42, 43, 40, 45, 54, 121000049)
								THEN 378723
							WHEN s.Address_Title LIKE N'%تهران غرب%'
								THEN 380341
						END AS HtsAgencyDlId
					FROM #Sites AS s
					LEFT JOIN #Regions AS r ON r.PartyID = s.Hamkaran_ManCompany_FK
				)
				UPDATE ca
				SET
					ca.AgencyDlId = dl.Id,
					ca.AgencyPartyId = p.Id,
					ca.ModifiedById = 1,
					ca.ModifiedByName = N'job-customeraddress-agencydl',
					ca.ModifiedDateMiladiDateTime = SYSDATETIME(),
					ca.ModifiedDateShamsiDateTime = CONVERT(NVARCHAR(30), SYSDATETIME(), 120)
				FROM SLS.CustomerAddress AS ca
				INNER JOIN Mapped AS m ON m.HtsAddressId = ca.HtsId
				INNER JOIN #Agency AS ag ON ag.Acc_DL_ID = m.HtsAgencyDlId
				INNER JOIN FIN.DL AS dl ON dl.HamkaranId = ag.Hamkaran_Acc_DL_FK
				LEFT JOIN Gnr.Party AS p ON p.HamkaranId = ag.Hamkaran_ManCompany_FK
				WHERE ca.AgencyDlId IS NULL
				  AND m.HtsAgencyDlId IS NOT NULL;
				""";

			var affected = await db.Database.ExecuteSqlRawAsync(sql, cn);
			await jobLogger?.LogInfoAsync($"تعداد ردیف به‌روزشده: {affected}", cn);
		}

		/// <summary>
		/// معادل HTS WriteData.Import_AllCustomer_Address (HtsTaskService.BaseHamkaranTask هر ۱۵ دقیقه):
		/// همان مجموعه‌ی آدرس‌های راهکاران (اولین آدرس هر Party مشتری) را بر اساس HamkaranAddId در SLS.CustomerAddress
		/// درج می‌کند و برای ردیف موجود فقط عنوان/آدرس را (اگر در راهکاران عوض شده) به‌روز می‌کند.
		/// مشتری مثل HTS با Customer.Code = SLS3.Customer.Number پیدا می‌شود؛ بدون مشتری رد می‌شود (ابتدا CustomerJob).
		/// منطقه / گرید / استان / تفصیل نمایندگی را دست نمی‌زند (تفصیل را FillEmptyAgencyDlFromRahkaranRegion پر می‌کند).
		/// HtsId (کلید ارتباط با سفارش/سریال/درخواست خدمات) اگر در HTS همان Hamkaran_Add_FK موجود باشد پر می‌شود.
		/// </summary>
		[JobHandler(
			"افزودن سایت مشتریان از راهکاران",
			"آدرس‌های راهکاران را بر اساس HamkaranAddId در SLS.CustomerAddress درج یا عنوان/آدرس را به‌روز می‌کند (معادل Import_AllCustomer_Address در HTS).")]
		public async Task AddCustomerAddressFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی سایت مشتریان از راهکاران", cn);

				rdb.Database.SetCommandTimeout(180);
				db.Database.SetCommandTimeout(180);
				htsDb.Database.SetCommandTimeout(180);

				var rahkaranRows = await rdb.Database.SqlQueryRaw<RahkaranAddressDto>(RahkaranAddressSql).ToListAsync(cn);
				await jobLogger?.LogInfoAsync($"تعداد {rahkaranRows.Count} آدرس از راهکاران دریافت شد", cn);

				// یک Party می‌تواند چند SLS3.Customer داشته باشد؛ برای هر آدرس یک ردیف (کمترین کد مشتری).
				var rahkaranAddresses = rahkaranRows
					.Where(r => r.Serial > 0 && r.Serial <= int.MaxValue)
					.GroupBy(r => r.Serial)
					.Select(g => g.OrderBy(x => x.CstmrCode).First())
					.ToList();

				var customers = await db.Set<Customer>()
					.AsNoTracking()
					.Where(c => c.Code != null)
					.Select(c => new CustomerRef
					{
						Id = c.Id.GetValueOrDefault(),
						Code = c.Code.GetValueOrDefault(),
						CreatedByName = c.CreatedByName,
						IsActive = c.IsActive
					})
					.ToListAsync(cn);

				// کد مشتری تکراری شناخته‌شده است (رکوردهای sync-*)؛ مثل Fix_CustomerAddress_SyncFromHts رکورد غیر sync و کم‌ترین Id.
				var customerByCode = await JobLookup.ToUniqueValueMapAsync(
					customers,
					x => x.Code,
					x => x.Id,
					null,
					"SLS.Customer.Code",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active && !(x.CreatedByName?.StartsWith("sync-", StringComparison.OrdinalIgnoreCase) ?? false));

				var existingAddresses = await db.Set<CustomerAddress>()
					.Where(a => a.HamkaranAddId != 0)
					.ToListAsync(cn);

				var existingByHamkaranAdd = await JobLookup.ToUniqueMapAsync(
					existingAddresses,
					x => x.HamkaranAddId,
					jobLogger,
					"SLS.CustomerAddress.HamkaranAddId",
					cn,
					x => x.Id,
					x => x.IsActive == IsActiveEnum.Active);

				var htsLinks = await LoadHtsAddressLinksAsync(jobLogger, cn);
				var usedHtsIds = (await db.Set<CustomerAddress>()
						.Where(a => a.HtsId != 0)
						.Select(a => a.HtsId)
						.ToListAsync(cn))
					.ToHashSet();

				var now = DateTime.Now;
				var nowShamsi = now.ToShamsiDateTime();

				var newAddresses = new List<CustomerAddress>();
				int updatedCount = 0, htsLinkedCount = 0, skippedNoCustomer = 0;

				foreach (var row in rahkaranAddresses)
				{
					var hamkaranAddId = (int)row.Serial;
					var title = NormalizeText(row.AddrTitle, 400);
					var address = NormalizeText(row.Address, 4000);

					if (existingByHamkaranAdd.TryGetValue(hamkaranAddId, out var existing))
					{
						var changed = false;

						if (title != null && existing.Title != title)
						{
							existing.Title = title;
							changed = true;
						}

						if (address != null && existing.Address != address)
						{
							existing.Address = address;
							changed = true;
						}

						if (existing.HtsId == 0 && TakeHtsId(htsLinks, usedHtsIds, hamkaranAddId) is { } linkedHtsId)
						{
							existing.HtsId = linkedHtsId;
							htsLinkedCount++;
							changed = true;
						}

						if (changed)
						{
							existing.ModifiedById = 1;
							existing.ModifiedByName = ImportUserName;
							existing.ModifiedDateMiladiDateTime = now;
							existing.ModifiedDateShamsiDateTime = nowShamsi;
							updatedCount++;
						}

						continue;
					}

					if (!customerByCode.TryGetValue(row.CstmrCode, out var customerId))
					{
						skippedNoCustomer++;
						continue;
					}

					var htsId = TakeHtsId(htsLinks, usedHtsIds, hamkaranAddId) ?? 0;
					if (htsId != 0)
						htsLinkedCount++;

					newAddresses.Add(new CustomerAddress
					{
						HtsId = htsId,
						CustomerId = customerId,
						Title = title,
						Address = address,
						HamkaranAddId = hamkaranAddId,
						CreatedById = 1,
						CreatedByName = ImportUserName,
						CreatedOnMiladiDateTime = now,
						CreatedOnShamsiDateTime = nowShamsi,
						ModifiedById = 1,
						ModifiedByName = ImportUserName,
						ModifiedDateMiladiDateTime = now,
						ModifiedDateShamsiDateTime = nowShamsi,
						IsActive = IsActiveEnum.Active
					});
				}

				if (skippedNoCustomer > 0)
					await jobLogger?.LogWarningAsync($"تعداد {skippedNoCustomer} آدرس به دلیل نبودن مشتری (Customer.Code) رد شد؛ بعد از اجرای «افزودن مشتریان از راهکاران» دوباره بررسی می‌شود", 0, cn);

				if (newAddresses.Count > 0)
					db.Set<CustomerAddress>().AddRange(newAddresses);

				if (newAddresses.Count > 0 || updatedCount > 0)
					await db.SaveChangesAsync(cn);

				await jobLogger?.LogInfoAsync(
					$"سایت جدید: {newAddresses.Count} | به‌روزرسانی: {updatedCount} | HtsId پر شده: {htsLinkedCount} | بدون مشتری: {skippedNoCustomer}",
					cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		/// <summary>نگاشت Hamkaran_Add_FK → شناسه(های) Crm_Customer_Address در HTS. اگر HTS در دسترس نباشد جاب ادامه می‌دهد و HtsId=0 می‌ماند.</summary>
		private async Task<Dictionary<int, Queue<long>>> LoadHtsAddressLinksAsync(IJobLogger? jobLogger, CancellationToken cn)
		{
			var result = new Dictionary<int, Queue<long>>();
			try
			{
				var links = await htsDb.Database.SqlQueryRaw<HtsAddressLinkDto>(
					"""
					SELECT CAST(Customer_Address_ID AS bigint) AS HtsId,
					       CAST(Hamkaran_Add_FK AS int) AS HamkaranAddId
					FROM Crm_Customer_Address
					WHERE Hamkaran_Add_FK <> 0
					""").ToListAsync(cn);

				foreach (var group in links.GroupBy(l => l.HamkaranAddId))
					result[group.Key] = new Queue<long>(group.Select(l => l.HtsId).OrderBy(id => id));
			}
			catch (Exception ex)
			{
				await jobLogger?.LogWarningAsync($"خواندن شناسه آدرس‌ها از HTS ممکن نشد؛ HtsId پر نمی‌شود: {ex.Message}", 0, cn);
			}

			return result;
		}

		private static long? TakeHtsId(Dictionary<int, Queue<long>> htsLinks, HashSet<long> usedHtsIds, int hamkaranAddId)
		{
			if (!htsLinks.TryGetValue(hamkaranAddId, out var queue))
				return null;

			while (queue.Count > 0)
			{
				var candidate = queue.Dequeue();
				if (usedHtsIds.Add(candidate))
					return candidate;
			}

			return null;
		}

		private static string? NormalizeText(string? value, int maxLength)
		{
			var text = value?.Trim();
			if (string.IsNullOrEmpty(text))
				return null;
			return text.Length > maxLength ? text[..maxLength] : text;
		}

		// همان مجموعه‌ی HTS ReadDataHamkaran.GetAllCustomer_Address (حالت Rahkaran): اولین آدرس هر Party مشتری.
		// به‌جای CTE از زیرپرس‌وجو استفاده شده چون EF روی SqlQueryRaw آن را در SELECT دیگری می‌پیچد.
		private const string RahkaranAddressSql = """
			SELECT CAST(Adrs.AddressID AS bigint) AS Serial,
			       CAST(c.Number AS int) AS CstmrCode,
			       Adrs.Name AS AddrTitle,
			       Adrs.Details AS Address
			FROM GNR3.Address AS Adrs
			LEFT JOIN (
			    SELECT MIN(AddressRef) AS PartyAddressID, PartyRef
			    FROM GNR3.PartyAddress
			    GROUP BY PartyRef
			) AS pa ON pa.PartyAddressID = Adrs.AddressID
			LEFT JOIN SLS3.Customer AS c ON c.PartyRef = pa.PartyRef
			WHERE c.CustomerID IS NOT NULL
			""";

		private sealed class RahkaranAddressDto
		{
			public long Serial { get; set; }
			public int CstmrCode { get; set; }
			public string? AddrTitle { get; set; }
			public string? Address { get; set; }
		}

		private sealed class HtsAddressLinkDto
		{
			public long HtsId { get; set; }
			public int HamkaranAddId { get; set; }
		}

		private sealed class CustomerRef
		{
			public long Id { get; set; }
			public int Code { get; set; }
			public string? CreatedByName { get; set; }
			public IsActiveEnum? IsActive { get; set; }
		}
	}
}
