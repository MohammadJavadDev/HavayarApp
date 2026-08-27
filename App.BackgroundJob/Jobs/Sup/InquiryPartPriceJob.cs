using Common.Attributes;
using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.App.Acc;
using Entities.App.Inv;
using Entities.App.Sup;
using Entities.App.Sup.Enums;
using Microsoft.EntityFrameworkCore;
using Services.Job;

namespace App.BackgroundJob.Jobs.Sup
{
	public class InquiryPartPriceJob(ApplicationDbContext dbContext, RahkaranDbContext rdb, IUnitOfWork unitOfWork)
	{
		[JobHandler("هماهنگ کردن اطلاعات استعلام قیمت کالا از راهکاران")]
		public async Task SyncInquiryPartPriceJobFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی استعلام قیمت کالا از راهکاران", cn);

				// مرحله ۱: بارگذاری داده‌های راهکاران
				var rahkaranSql = @"
SELECT 
	SupInquiryPartPrice.Sup_InquiryPartPriceID AS Id,
	SupInquiryPartPrice.InquiryTypeId,
	SupInquiryPartPrice.Title AS CompanyName,
	SupInquiryPartPrice.PartIdRef AS PartId,
	NULL AS BuyCount,
	Currency.Title AS CurrencyTitle,
	ISNULL(SupInquiryPartPrice.CurrencyPrice, 0) AS CurrencyPrice,
	ISNULL(SupInquiryPartPrice.TransportationCost, 0) AS TransportationCost,
	SupInquiryPartPrice.Price AS UnitPrice,
	ISNULL(SupInquiryPartPrice.IsFixedPrice, 0) AS IsFixedPrice,
	SupInquiryPartPrice.Comment,
	[USER].DomainUserName AS Creator,
	SupInquiryPartPrice.Creator AS CreatorUserID,
	SupInquiryPartPrice.CreationDate,
	CreationDate.SolarDate AS CreationDateInText,
	CONVERT(VARCHAR(5), SupInquiryPartPrice.CreationDate, 108) AS CreatedTime,
	SupInquiryPartPrice.ValidityDate,
	FORMAT(SupInquiryPartPrice.ValidityDate,'yyyy/MM/dd','fa-IR') AS ValidityDateInText,
	ISNULL(SupInquiryPartPrice.CurrencyStandardPrice, IIF(SupInquiryPartPrice.PriceStatus = 2831, NULL, SupInquiryPartPrice.CurrencyPrice)) AS CurrencyStandardPrice,
	ISNULL(SupInquiryPartPrice.CurrencyActualPrice, IIF(SupInquiryPartPrice.PriceStatus = 2831, SupInquiryPartPrice.CurrencyPrice, NULL)) AS CurrencyActualPrice,
	ISNULL(SupInquiryPartPrice.ShippingStandardPrice, IIF(SupInquiryPartPrice.PriceStatus = 2831, NULL, SupInquiryPartPrice.TransportationCost)) AS ShippingStandardPrice,
	ISNULL(SupInquiryPartPrice.ShippingActualPrice, IIF(SupInquiryPartPrice.PriceStatus = 2831, SupInquiryPartPrice.TransportationCost, NULL)) AS ShippingActualPrice,
	NULL AS TotalStandardPrice,
	NULL AS TotalActualPrice,
	ISNULL(SupInquiryPartPrice.IsSpecialCases, 0) AS IsSpecialCases,
	SupInquiryPartPrice.SpecialCasesDescription,
	SupInquiryPartPrice.PriceStatus,
	ISNULL(SupInquiryPartPrice.MarketBuyPrice, 0) AS MarketBuyPrice
FROM USR3.Sup_InquiryPartPrice AS SupInquiryPartPrice
JOIN LGS3.Part AS Part ON Part.PartID = SupInquiryPartPrice.PartIdRef
LEFT JOIN GNR3.Currency AS Currency ON Currency.CurrencyID = SupInquiryPartPrice.CurrencyUnitIdRef
LEFT JOIN SYS3.[User] AS [USER] ON [USER].UserID = SupInquiryPartPrice.Creator
LEFT JOIN USR3.Gnr_DimDate AS CreationDate ON CreationDate.Date = CAST(SupInquiryPartPrice.CreationDate AS DATE)
";

				await jobLogger?.LogInfoAsync("در حال اجرای کوئری برای دریافت داده‌های استعلام قیمت از راهکاران...", cn);
				var rahkaranData = await rdb.Database.SqlQueryRaw<RahkaranInquiryPartPriceDto>(rahkaranSql).ToListAsync(cn);
				await jobLogger?.LogInfoAsync($"تعداد {rahkaranData.Count} رکورد استعلام قیمت از راهکاران دریافت شد", cn);

				// مرحله ۲: بارگذاری داده‌های پایه از Application
				await jobLogger?.LogInfoAsync("در حال بارگذاری Part، PriceUnit و User از پایگاه داده برنامه...", cn);

				var partsDict = await unitOfWork.Repository<Part>().TableNoTracking
					.Where(p => p.HamkaranId != null)
					.ToDictionaryAsync(x => x.HamkaranId!.Value, x => x.Id!.Value, cn);

				var priceUnits = await unitOfWork.Repository<PriceUnit>().TableNoTracking
					.Where(p => !string.IsNullOrEmpty(p.Title))
					.ToListAsync(cn);
				var priceUnitDict = priceUnits.ToDictionary(x => x.Title!.Trim(), StringComparer.OrdinalIgnoreCase);

				var usersDict = await dbContext.Users
					.Where(u => u.HamkaranId != null)
					.ToDictionaryAsync(x => x.HamkaranId!.Value, x => x.Id!.Value, cn);

				var existingInquiryPartPrices = await unitOfWork.Repository<InquiryPartPrice>()
					.Table
					.Where(x => x.RahkaranId != null)
					.ToListAsync(cn);
				var existingRahkaranIds = existingInquiryPartPrices.Select(x => x.RahkaranId!.Value).ToHashSet();
				var existingByRahkaranId = existingInquiryPartPrices.ToDictionary(x => x.RahkaranId!.Value);

				await jobLogger?.LogInfoAsync($"تعداد {partsDict.Count} کالا، {priceUnitDict.Count} واحد ارز و {usersDict.Count} کاربر در برنامه موجود است", cn);

				// مرحله ۳: فیلتر رکوردهای جدید و پردازش
				var newRecords = new List<InquiryPartPrice>();
				var updatedCount = 0;
				var skippedCount = 0;

				foreach (var row in rahkaranData)
				{
					// نگاشت Part
					if (!partsDict.TryGetValue(row.PartId, out var partId))
					{
						await jobLogger?.LogWarningAsync($"کالا با HamkaranId {row.PartId} برای استعلام قیمت یافت نشد", 0, cn);
						skippedCount++;
						continue;
					}

					// نگاشت PriceUnit
					long? priceUnitId = null;
					if (!string.IsNullOrEmpty(row.CurrencyTitle) &&
						priceUnitDict.TryGetValue(row.CurrencyTitle.Trim(), out var pu))
					{
						priceUnitId = pu.Id;
					}

					// نگاشت Creator
					long? createdById = null;
					if (row.CreatorUserID.HasValue && usersDict.TryGetValue(row.CreatorUserID.Value, out var uid))
					{
						createdById = uid;
					}

					// نگاشت Type و PriceStatus از کد مستقیم (معادل Lookup Code)
					InquiryPartPriceTypeEnum? type = row.InquiryTypeId.HasValue && Enum.IsDefined(typeof(InquiryPartPriceTypeEnum), row.InquiryTypeId.Value)
						? (InquiryPartPriceTypeEnum)row.InquiryTypeId.Value
						: null;

					InquiryPartPricePriceStatusEnum? priceStatus = row.PriceStatus.HasValue && Enum.IsDefined(typeof(InquiryPartPricePriceStatusEnum), row.PriceStatus.Value)
						? (InquiryPartPricePriceStatusEnum)row.PriceStatus.Value
						: null;

					if (existingByRahkaranId.TryGetValue(row.Id, out var existing))
					{
						// به‌روزرسانی رکورد موجود
						var modified = false;
						if (existing.CompanyName != row.CompanyName) { existing.CompanyName = row.CompanyName; modified = true; }
						if (existing.PartId != partId) { existing.PartId = partId; modified = true; }
						var buyCount = row.BuyCount.HasValue ? (int?)Convert.ToInt32(row.BuyCount.Value) : null;
						if (existing.BuyCount != buyCount) { existing.BuyCount = buyCount; modified = true; }
						if (existing.PriceUnitValue != row.CurrencyPrice) { existing.PriceUnitValue = row.CurrencyPrice; modified = true; }
						if (existing.TransportationCost != row.TransportationCost) { existing.TransportationCost = row.TransportationCost; modified = true; }
						if (existing.UnitPrice != row.UnitPrice) { existing.UnitPrice = row.UnitPrice; modified = true; }
						if (existing.IsFixedPrice != row.IsFixedPrice) { existing.IsFixedPrice = row.IsFixedPrice; modified = true; }
						if (existing.Comment != row.Comment) { existing.Comment = row.Comment; modified = true; }
						if (existing.ValidityDate != row.ValidityDate) { existing.ValidityDate = row.ValidityDate; modified = true; }
						if (existing.ValidShamsiDate != row.ValidityDateInText) { existing.ValidShamsiDate = row.ValidityDateInText; modified = true; }
						if (existing.CurrencyStandardPrice != row.CurrencyStandardPrice) { existing.CurrencyStandardPrice = row.CurrencyStandardPrice; modified = true; }
						if (existing.CurrencyActualPrice != row.CurrencyActualPrice) { existing.CurrencyActualPrice = row.CurrencyActualPrice; modified = true; }
						if (existing.ShippingStandardPrice != row.ShippingStandardPrice) { existing.ShippingStandardPrice = row.ShippingStandardPrice; modified = true; }
						if (existing.ShippingActualPrice != row.ShippingActualPrice) { existing.ShippingActualPrice = row.ShippingActualPrice; modified = true; }
						if (existing.IsSpecialCases != row.IsSpecialCases) { existing.IsSpecialCases = row.IsSpecialCases; modified = true; }
						if (existing.SpecialCasesDescription != row.SpecialCasesDescription) { existing.SpecialCasesDescription = row.SpecialCasesDescription; modified = true; }
						if (existing.PriceStatus != priceStatus) { existing.PriceStatus = priceStatus; modified = true; }
						if (existing.NewFeature != row.MarketBuyPrice) { existing.NewFeature = row.MarketBuyPrice; modified = true; }
						if (existing.PriceUnitId != priceUnitId) { existing.PriceUnitId = priceUnitId; modified = true; }
						if (existing.CreatedById != createdById) { existing.CreatedById = createdById; modified = true; }

						if (modified)
							updatedCount++;
					}
					else
					{
						// درج رکورد جدید
						newRecords.Add(new InquiryPartPrice
						{
							RahkaranId = row.Id,
							Type = type,
							CompanyName = row.CompanyName,
							PartId = partId,
							BuyCount = row.BuyCount.HasValue ? (int?)Convert.ToInt32(row.BuyCount.Value) : null,
							PriceUnitValue = row.CurrencyPrice,
							TransportationCost = row.TransportationCost,
							UnitPrice = row.UnitPrice,
							IsFixedPrice = row.IsFixedPrice,
							Comment = row.Comment,
							ValidityDate = row.ValidityDate,
							ValidShamsiDate = row.ValidityDateInText,
							CurrencyStandardPrice = row.CurrencyStandardPrice,
							CurrencyActualPrice = row.CurrencyActualPrice,
							ShippingStandardPrice = row.ShippingStandardPrice,
							ShippingActualPrice = row.ShippingActualPrice,
							TotalStandardPrice = row.TotalStandardPrice,
							TotalActualPrice = row.TotalActualPrice,
							IsSpecialCases = row.IsSpecialCases,
							SpecialCasesDescription = row.SpecialCasesDescription,
							PriceStatus = priceStatus,
							NewFeature = row.MarketBuyPrice,
							PriceUnitId = priceUnitId,
							CreatedById = createdById,
							CreatedOnMiladiDateTime = row.CreationDate,
							CreatedOnShamsiDateTime = row.CreationDateInText + " " + row.CreatedTime,
							ModifiedDateMiladiDateTime = row.CreationDate,
							ModifiedDateShamsiDateTime = row.CreationDateInText + " " + row.CreatedTime,
						});
					}
				}

				if (skippedCount > 0)
					await jobLogger?.LogWarningAsync($"تعداد {skippedCount} رکورد به دلیل عدم وجود کالای مرتبط رد شد", 0, cn);

				if (newRecords.Any())
				{
					await jobLogger?.LogInfoAsync($"افزودن {newRecords.Count} استعلام قیمت جدید به پایگاه داده", cn);
					await unitOfWork.Repository<InquiryPartPrice>().AddRangeAsync(newRecords, cn, false);
				}

				if (updatedCount > 0)
					await jobLogger?.LogInfoAsync($"به‌روزرسانی {updatedCount} استعلام قیمت موجود", cn);

				await unitOfWork.SaveChangesAsync(cn);

				// مرحله ۴: به‌روزرسانی CurrencyActualPrice برای رکوردهای با واحد ریال
				await UpdateCurrencyActualPriceFromExchangeRate(jobLogger, cn);

				// مرحله ۵: حذف رکوردهایی که در راهکاران دیگر وجود ندارند
			//	await DeleteOrphanedRecords(jobLogger, cn);

				await jobLogger?.LogInfoAsync("همگام‌سازی استعلام قیمت کالا با موفقیت انجام شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		/// <summary>
		/// به‌روزرسانی CurrencyActualPrice و TotalActualPrice برای رکوردهای دلاری با CurrencyActualPrice خالی
		/// </summary>
		private async Task UpdateCurrencyActualPriceFromExchangeRate(IJobLogger? jobLogger, CancellationToken cn)
		{
			// طبق جدول Acc.PriceUnit شناسه 1 ریال، 2 دلار و 4 یورو است.
			const int dollarPriceUnitId = 2;
			const int exchangeRatePriceUnitId = 2;
			var minDate = new DateTime(2023, 3, 21); // 1402/01/01

			var toUpdate = await unitOfWork.Repository<InquiryPartPrice>()
				.Table
				.Where(x => x.PriceUnitId == dollarPriceUnitId
					&& x.CurrencyActualPrice == null
					&& x.CreatedOnMiladiDateTime >= minDate)
				.ToListAsync(cn);

			if (toUpdate.Count == 0) return;

			var exchangeRates = await unitOfWork.Repository<ExchangeRateArchive>()
				.TableNoTracking
				.Where(x => x.PriceUnitId == exchangeRatePriceUnitId && x.CreatedOnMiladiDateTime != null)
				.OrderByDescending(x => x.CreatedOnMiladiDateTime)
				.ToListAsync(cn);

			var updated = 0;
			foreach (var item in toUpdate)
			{
				var rate = exchangeRates
					.FirstOrDefault(r => r.CreatedOnMiladiDateTime <= item.CreatedOnMiladiDateTime);
				if (rate?.FinalPrice == null || rate.FinalPrice == 0) continue;

				var price = item.UnitPrice / (decimal)rate.FinalPrice;
				item.CurrencyActualPrice = price;
				item.TotalActualPrice = price;
				updated++;
			}

			if (updated > 0)
			{
				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync($"به‌روزرسانی CurrencyActualPrice برای {updated} رکورد", cn);
			}
		}

		/// <summary>
		/// حذف رکوردهای Application که در راهکاران دیگر وجود ندارند
		/// </summary>
		private async Task DeleteOrphanedRecords(IJobLogger? jobLogger, CancellationToken cn)
		{
			var appWithRahkaranId = await unitOfWork.Repository<InquiryPartPrice>()
				.Table
				.Where(x => x.RahkaranId != null)
				.Select(x => new { x.Id, x.RahkaranId })
				.ToListAsync(cn);

			if (appWithRahkaranId.Count == 0) return;

			var rahkaranIds = appWithRahkaranId.Select(x => x.RahkaranId!.Value).Distinct().ToList();
			if (rahkaranIds.Count == 0) return;

			var idList = string.Join(",", rahkaranIds);
			var existsSql = $@"
SELECT Sup_InquiryPartPriceID FROM USR3.Sup_InquiryPartPrice 
WHERE Sup_InquiryPartPriceID IN ({idList})";

			var existingInRahkaran = await rdb.Database.SqlQueryRaw<long>(existsSql).ToListAsync(cn);
			var existingSet = existingInRahkaran.ToHashSet();

			var toDelete = appWithRahkaranId.Where(x => !existingSet.Contains(x.RahkaranId!.Value)).ToList();
			if (toDelete.Count == 0) return;

			foreach (var item in toDelete)
			{
				var entity = await unitOfWork.Repository<InquiryPartPrice>().Table.FirstOrDefaultAsync(x => x.Id == item.Id, cn);
				if (entity != null)
				{
					await unitOfWork.Repository<InquiryPartPrice>().DeleteAsync(entity, cn, false);
				}
			}

			await unitOfWork.SaveChangesAsync(cn);
			await jobLogger?.LogInfoAsync($"حذف {toDelete.Count} رکورد که در راهکاران وجود نداشتند", cn);
		}

		private sealed class RahkaranInquiryPartPriceDto
		{
			public long Id { get; set; }
			public int? InquiryTypeId { get; set; }
			public string? CompanyName { get; set; }
			public long PartId { get; set; }
			public decimal? BuyCount { get; set; }
			public string? CurrencyTitle { get; set; }
			public decimal CurrencyPrice { get; set; }
			public decimal TransportationCost { get; set; }
			public decimal UnitPrice { get; set; }
			public bool IsFixedPrice { get; set; }
			public string? Comment { get; set; }
			public string? Creator { get; set; }
			public long? CreatorUserID { get; set; }
			public DateTime CreationDate { get; set; }
			public string? CreationDateInText { get; set; }
			public string? CreatedTime { get; set; }
			public DateTime? ValidityDate { get; set; }
			public string? ValidityDateInText { get; set; }
			public decimal? CurrencyStandardPrice { get; set; }
			public decimal? CurrencyActualPrice { get; set; }
			public decimal? ShippingStandardPrice { get; set; }
			public decimal? ShippingActualPrice { get; set; }
			public decimal? TotalStandardPrice { get; set; }
			public decimal? TotalActualPrice { get; set; }
			public bool IsSpecialCases { get; set; }
			public string? SpecialCasesDescription { get; set; }
			public int? PriceStatus { get; set; }
			public decimal MarketBuyPrice { get; set; }
		}

		/// <summary>
		/// همگام‌سازی استعلام قیمت از فاکتورهای خرید راهکاران.
		/// رکوردهای با نوع «فاکتور» از Vw_Prc3InvoiceDetails خوانده و به InquiryPartPrice اضافه می‌شوند.
		/// در توضیحات: شناسه فاکتور، شماره و عبارت «از فاکتور اضافه شده» ذخیره می‌شود.
		/// </summary>
		[JobHandler("افزودن استعلام قیمت از فاکتورهای خرید راهکاران")]
		public async Task SyncInquiryPartPriceFromPurchaseInvoice(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
		await jobLogger?.LogInfoAsync("شروع افزودن استعلام قیمت از فاکتورهای خرید راهکاران", cn);

				// کوئری خواندن از view فاکتورهای خرید راهکاران (Vw_Prc3InvoiceDetails)
				var invoiceSql = @"
 
DECLARE @LocalItems TABLE (InvoiceItemID INT PRIMARY KEY);

INSERT INTO @LocalItems
SELECT DISTINCT InvoiceItemId 
FROM Sup.InquiryPartPrice 
WHERE InvoiceItemId is NOT NULL;
 
 
IF OBJECT_ID('tempdb..#LocalItems') IS NOT NULL DROP TABLE #LocalItems;
CREATE TABLE #LocalItems (InvoiceItemID INT PRIMARY KEY);

INSERT INTO #LocalItems
SELECT DISTINCT InvoiceItemId 
FROM Sup.InquiryPartPrice WITH (NOLOCK)
WHERE InvoiceItemId is NOT NULL;;

 
SELECT * FROM OPENQUERY(ERPS, '
    SELECT 
        p.PartID,
        1 AS BuyCount,
       CAST( p.Price - ISNULL(p.Discount, 0) / p.Quantity AS DECIMAL)  AS UnitPrice,
        p.InvoiceItemID,
        p.InvoiceID,
        ISNULL(p.InvoiceNumber, '''') AS FactorNum,
        ISNULL(p.ItemDescription, '''') AS ItemDescription,
        ISNULL(p.RequesterDepartment, '''') AS RequesterDepartment,
        ISNULL(p.SupplierName, N''نامشخص'') AS CompanyName,
        p.CreationDate,
        p.DomainUserName
    FROM ERPS.dbo.Vw_Prc3InvoiceDetails p
    WHERE p.PurchasingDepartmentRef = 1 
        AND (p.PartNature IS NULL OR p.PartNature <> 4)
        AND p.PartID IS NOT NULL
') AS remote
WHERE NOT EXISTS (
    SELECT 1 
    FROM #LocalItems li 
    WHERE li.InvoiceItemID = remote.InvoiceItemID
)
OPTION (RECOMPILE);
--	متولی  تایید مهندسی در درخواست خریدهای باز وجود ندارد  
DROP TABLE #LocalItems;

";

				dbContext.Database.SetCommandTimeout(0);

				await jobLogger?.LogInfoAsync("در حال اجرای کوئری فاکتورهای خرید از راهکاران (Vw_Prc3InvoiceDetails)...", cn);
				var invoiceData = await dbContext.Database.SqlQueryRaw<PurchaseInvoiceItemDto>(invoiceSql).ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {invoiceData.Count} قلم فاکتور از راهکاران دریافت شد", cn);

				if (invoiceData.Count == 0)
				{
					await jobLogger?.LogInfoAsync("هیچ قلم فاکتوری برای افزودن یافت نشد", cn);
					return;
				}

				// بارگذاری Part و User از Application (نگاشت User با Username = DomainUserName)
				var partsDict = await unitOfWork.Repository<Part>().TableNoTracking
					.Where(p => p.HamkaranId != null)
					.ToDictionaryAsync(x => x.HamkaranId!.Value, x => x.Id!.Value, cn);

				var usersByUsername = await dbContext.Users
					.Where(u => !string.IsNullOrEmpty(u.Username))
					.ToDictionaryAsync(x => x.Username!, x => x.Id!.Value, StringComparer.OrdinalIgnoreCase);

				// رکوردهای موجود با InvoiceId+InvoiceItemId (برای جلوگیری از تکرار)
				var existingKeys = await unitOfWork.Repository<InquiryPartPrice>()
					.TableNoTracking
					.Where(x => x.InvoiceId != null && x.InvoiceItemId != null)
					.Select(x => new { x.InvoiceId, x.InvoiceItemId })
					.ToListAsync(cn);
				var existingSet = existingKeys.Select(x => (x.InvoiceId!.Value, x.InvoiceItemId!.Value)).ToHashSet();

				var newRecords = new List<InquiryPartPrice>();
				var skippedCount = 0;

				foreach (var row in invoiceData)
				{
					if (existingSet.Contains((row.InvoiceID, row.InvoiceItemID)))
						continue;

					if (!partsDict.TryGetValue(row.PartID, out var partId))
					{
						await jobLogger?.LogWarningAsync($"کالا با HamkaranId {row.PartID} برای قلم فاکتور {row.InvoiceItemID} یافت نشد", 0, cn);
						skippedCount++;
						continue;
					}

					long? createdById = null;
					if (!string.IsNullOrWhiteSpace(row.DomainUserName) && usersByUsername.TryGetValue(row.DomainUserName.Trim(), out var uid))
						createdById = uid;

					// توضیحات: شناسه فاکتور، شماره و «از فاکتور اضافه شده»
					var comment = $"شناسه فاکتور: {row.InvoiceID}، شماره: {row.FactorNum}. از فاکتور اضافه شده.";
					if (!string.IsNullOrWhiteSpace(row.ItemDescription))
						comment = $"{row.ItemDescription} | {comment}";

					if (comment.Length > 2800)
						comment = comment.Substring(0, 2797) + "...";

					newRecords.Add(new InquiryPartPrice
					{
						Type = InquiryPartPriceTypeEnum.Invoice,
						CompanyName = row.CompanyName,
						PartId = partId,
						BuyCount = row.BuyCount,
						PriceUnitValue = row?.UnitPrice ??0,
						PriceUnitId = 1, // فاکتورهای خرید راهکاران ریالی هستند
						TransportationCost = 0,
						UnitPrice = row?.UnitPrice ?? 0,
						IsFixedPrice = false,
						Comment = comment,
						PriceStatus = InquiryPartPricePriceStatusEnum.Purchase,
						InvoiceId = row.InvoiceID,
						InvoiceItemId = row.InvoiceItemID,
						InvoiceNumber = row.FactorNum,
						CreatedById = createdById,
						CreatedOnMiladiDateTime = row.CreationDate,
						CreatedOnShamsiDateTime = row.CreationDate.ToShamsiDateTime()
					});
				}

				if (skippedCount > 0)
					await jobLogger?.LogWarningAsync($"تعداد {skippedCount} قلم به دلیل عدم وجود کالا رد شد", 0, cn);

				if (newRecords.Any())
				{
					await unitOfWork.Repository<InquiryPartPrice>().AddRangeAsync(newRecords, cn, false);
					await unitOfWork.SaveChangesAsync(cn);
					await jobLogger?.LogInfoAsync($"تعداد {newRecords.Count} استعلام قیمت از فاکتور به پایگاه داده اضافه شد", cn);
				}
				else
					await jobLogger?.LogInfoAsync("همه قلم‌های فاکتور قبلاً در سیستم موجود بودند", cn);

				// به‌روزرسانی CurrencyActualPrice برای رکوردهای جدید با واحد دلار
				await UpdateCurrencyActualPriceFromExchangeRate(jobLogger, cn);

				await jobLogger?.LogInfoAsync("افزودن استعلام قیمت از فاکتورهای خرید با موفقیت انجام شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		private sealed class PurchaseInvoiceItemDto
		{
			public long PartID { get; set; }
			public int? BuyCount { get; set; }
			public decimal? UnitPrice { get; set; }
			public long InvoiceItemID { get; set; }
			public long InvoiceID { get; set; }
			public string? FactorNum { get; set; }
			public string? ItemDescription { get; set; }
			public string? RequesterDepartment { get; set; }
			public string? CompanyName { get; set; }
			public DateTime CreationDate { get; set; }
			public string? DomainUserName { get; set; }
		}
	}
}
