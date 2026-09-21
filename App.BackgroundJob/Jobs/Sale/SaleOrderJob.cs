using Common.Attributes;
using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.App.Inv;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.App.SLS;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Services.Job;

namespace App.BackgroundJob.Jobs.Sale
{
	public class SaleOrderJob(RahkaranDbContext Rdb, IUnitOfWork unitOfWork, ApplicationDbContext appContext)
	{
		private const long DefaultBranchId = 10;
		private const long MinFiscalYearRef = 102;
		private const int BatchSize = 500;

		[JobHandler("افزودن و به‌روزرسانی سفارش فروش از راهکاران")]
		public async Task SyncSaleOrdersFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی سفارشات فروش از راهکاران", cn);
				Rdb.Database.SetCommandTimeout(0);
				appContext.Database.SetCommandTimeout(0);

				await SyncHeadersAsync(jobLogger, cn);
				await SyncDetailsAsync(jobLogger, cn);
				await MarkDeletedOrdersAsync(jobLogger, cn);

				try
				{
					await SyncSaleOrdersFromHts(jobLogger, cn);
				}
				catch (Exception htsEx)
				{
					await jobLogger?.LogWarningAsync(
						$"همگام‌سازی HTS بعد از راهکاران ناموفق بود: {htsEx.Message}",
						0,
						cn);
				}

				await jobLogger?.LogInfoAsync("همگام‌سازی سفارشات فروش با موفقیت انجام شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		[JobHandler("همگام‌سازی سفارش و سریال فروش از HTS")]
		public async Task SyncSaleOrdersFromHts(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی Order / OrderDetail / OrderDetailSerial از HTS", cn);
				appContext.Database.SetCommandTimeout(0);

				var orderRows = await appContext.Database.ExecuteSqlRawAsync(HtsStampOrderSql, cn);
				await jobLogger?.LogInfoAsync($"HtsId سفارش: {orderRows} ردیف به‌روز شد", cn);

				var detailRows = await appContext.Database.ExecuteSqlRawAsync(HtsStampAndInsertDetailSql, cn);
				await jobLogger?.LogInfoAsync($"HtsId / درج قلم: {detailRows} ردیف", cn);

				var serialRows = await appContext.Database.ExecuteSqlRawAsync(HtsMergeSerialSql, cn);
				await jobLogger?.LogInfoAsync($"سریال MERGE: {serialRows} ردیف", cn);

				await jobLogger?.LogInfoAsync("همگام‌سازی سفارش و سریال از HTS انجام شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		private async Task SyncHeadersAsync(IJobLogger? jobLogger, CancellationToken cn)
		{
			await jobLogger?.LogInfoAsync(
				$"در حال بارگذاری هدر سفارشات از راهکاران (FiscalYearRef >= {MinFiscalYearRef})...",
				cn);

			var headerSql = $@"
SELECT
	OrderHeader.OrderID AS OrderID,
	TRY_CAST(OrderHeader.Number AS BIGINT) AS OrderNumber,
	OrderHeader.FiscalYearRef AS FiscalYearRef,
	OrderHeader.[Date] AS VchDateMiladi,
	DimDate.SolarDate AS VchDateShamsi,
	OrderHeader.State AS RahkaranState,
	OrderHeader.SalesTypeRef AS SalesTypeRef,
	TRY_CAST(SalesOffice.Code AS BIGINT) AS BranchCode,
	OrderHeader.CustomerRef AS CustomerRef,
	OrderHeader.ContractRef AS ContractRef,
	OrderInvoice.InvoiceMiladiDate AS FactorMiladiDate
FROM SLS3.[Order] AS OrderHeader
INNER JOIN SLS3.SalesOffice AS SalesOffice ON SalesOffice.SalesOfficeID = OrderHeader.SalesOfficeRef
INNER JOIN GNR3.FiscalYear AS fy ON fy.FiscalYearID = OrderHeader.FiscalYearRef
INNER JOIN USR3.Gnr_DimDate AS DimDate ON DimDate.[Date] = OrderHeader.[Date]
INNER JOIN SLS3.Customer AS Customer ON Customer.CustomerID = OrderHeader.CustomerRef
LEFT JOIN SLS3.Broker AS [Broker] ON [Broker].BrokerID = OrderHeader.BrokerRef
LEFT JOIN (
	SELECT
		ord.OrderID,
		MAX(i.[Date]) AS InvoiceMiladiDate
	FROM SLS3.Invoice AS i
	INNER JOIN SLS3.InvoiceItem AS it ON i.InvoiceID = it.InvoiceRef
	LEFT JOIN SYS3.RecursiveEntityRelation AS RER ON RER.TargetRef = it.InvoiceItemID
		AND RER.TargetType = (
			SELECT code FROM SYS3.EntityLookup
			WHERE EntityName = N'SystemGroup.Sales.InvoiceManagement,InvoiceItem'
		)
		AND RER.SourceType = (
			SELECT code FROM SYS3.EntityLookup
			WHERE EntityName = N'SystemGroup.Sales.OrderManagement,OrderItem'
		)
	LEFT JOIN SLS3.OrderItem AS orderitem ON orderitem.OrderItemID = RER.SourceRef
	LEFT JOIN SLS3.[Order] AS ord ON ord.OrderID = orderitem.OrderRef
	WHERE ord.OrderID IS NOT NULL
	GROUP BY ord.OrderID
) AS OrderInvoice ON OrderInvoice.OrderID = OrderHeader.OrderID
WHERE OrderHeader.FiscalYearRef >= {MinFiscalYearRef}
	AND OrderHeader.CustomerRef IS NOT NULL
";

			var rahkaranOrders = await Rdb.Database
				.SqlQueryRaw<RahkaranSaleOrderHeaderDto>(headerSql)
				.ToListAsync(cn);

			await jobLogger?.LogInfoAsync($"تعداد {rahkaranOrders.Count} هدر سفارش در راهکاران یافت شد", cn);

			await jobLogger?.LogInfoAsync("در حال بارگذاری entity های مرتبط برای هدر...", cn);

			var customerMap = (await unitOfWork.Repository<Customer>()
					.TableNoTracking
					.ToListAsync(cn))
				.GroupBy(x => x.HamkaranId)
				.ToDictionary(g => g.Key, g => g.First().Id);

			var contractMap = (await unitOfWork.Repository<Contract>()
					.TableNoTracking
					.Where(x => x.HamkaranId.HasValue)
					.ToListAsync(cn))
				.GroupBy(x => x.HamkaranId!.Value)
				.ToDictionary(g => g.Key, g => g.First().Id);

			var branchIds = (await unitOfWork.Repository<Branch>()
					.TableNoTracking
					.Where(x => x.Id.HasValue)
					.Select(x => x.Id!.Value)
					.ToListAsync(cn))
				.ToHashSet();

			await jobLogger?.LogInfoAsync(
				$"Entity های مرتبط هدر بارگذاری شدند: {customerMap.Count} مشتری، {contractMap.Count} قرارداد، {branchIds.Count} شعبه",
				cn);

			var appOrders = await unitOfWork.Repository<Order>()
				.Table
				.Where(x => x.HamkaranId.HasValue)
				.ToListAsync(cn);

			await jobLogger?.LogInfoAsync($"تعداد {appOrders.Count} سفارش با HamkaranId در پایگاه داده برنامه موجود است", cn);

			var appOrdersDict = appOrders
				.GroupBy(x => x.HamkaranId!.Value)
				.ToDictionary(g => g.Key, g => g.First());

			var newOrders = new List<Order>();
			var updatedCount = 0;
			var skippedCount = 0;
			var pendingChanges = 0;

			foreach (var row in rahkaranOrders)
			{
				if (!row.CustomerRef.HasValue || !customerMap.TryGetValue(row.CustomerRef.Value, out var customerId) || !customerId.HasValue)
				{
					await jobLogger?.LogWarningAsync(
						$"مشتری با HamkaranId {row.CustomerRef} برای سفارش {row.OrderID} یافت نشد",
						0,
						cn);
					skippedCount++;
					continue;
				}

				var branchId = ResolveBranchId(row.BranchCode, branchIds);
				var status = MapStatus(row.RahkaranState);
				var voucherType = MapVoucherType(row.SalesTypeRef);
				long? contractId = null;
				if (row.ContractRef.HasValue && contractMap.TryGetValue(row.ContractRef.Value, out var mappedContractId))
				{
					contractId = mappedContractId;
				}

				var factorShamsi = row.FactorMiladiDate.HasValue
					? row.FactorMiladiDate.Value.ToShamsiDate()
					: null;
				var yearText = row.FiscalYearRef?.ToString();

				if (!appOrdersDict.TryGetValue(row.OrderID, out var existOrder))
				{
					var now = DateTime.Now;
					var newOrder = new Order
					{
						HamkaranId = row.OrderID,
						OrderNumber = row.OrderNumber,
						Year = yearText,
						FiscalYearRef = row.FiscalYearRef,
						VchDateMiladiDate = row.VchDateMiladi,
						VchDateShamsiDate = Truncate(row.VchDateShamsi, 30),
						CustomerId = customerId,
						StatusEnum = status,
						VoucherTypeStatus = voucherType,
						BranchId = branchId,
						ContractId = contractId,
						FactorMiladiDate = row.FactorMiladiDate,
						FactorShamsiDate = Truncate(factorShamsi, 30),
						CreatedOnMiladiDateTime = now,
						CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
						IsActive = IsActiveEnum.Active
					};
					newOrders.Add(newOrder);
					appOrdersDict[row.OrderID] = newOrder;
					pendingChanges++;
				}
				else
				{
					var changed = false;
					changed |= SetIfChanged(existOrder.OrderNumber, row.OrderNumber, v => existOrder.OrderNumber = v);
					changed |= SetIfChanged(existOrder.Year, yearText, v => existOrder.Year = v);
					changed |= SetIfChanged(existOrder.FiscalYearRef, row.FiscalYearRef, v => existOrder.FiscalYearRef = v);
					changed |= SetIfChanged(existOrder.VchDateMiladiDate, row.VchDateMiladi, v => existOrder.VchDateMiladiDate = v);
					changed |= SetIfChanged(existOrder.VchDateShamsiDate, Truncate(row.VchDateShamsi, 30), v => existOrder.VchDateShamsiDate = v);
					changed |= SetIfChanged(existOrder.CustomerId, customerId, v => existOrder.CustomerId = v);
					changed |= SetIfChanged(existOrder.StatusEnum, status, v => existOrder.StatusEnum = v);
					changed |= SetIfChanged(existOrder.VoucherTypeStatus, voucherType, v => existOrder.VoucherTypeStatus = v);
					changed |= SetIfChanged(existOrder.BranchId, branchId, v => existOrder.BranchId = v);
					changed |= SetIfChanged(existOrder.ContractId, contractId, v => existOrder.ContractId = v);
					changed |= SetIfChanged(existOrder.FactorMiladiDate, row.FactorMiladiDate, v => existOrder.FactorMiladiDate = v);
					changed |= SetIfChanged(existOrder.FactorShamsiDate, Truncate(factorShamsi, 30), v => existOrder.FactorShamsiDate = v);

					if (changed)
					{
						updatedCount++;
						pendingChanges++;
					}
				}

				if (newOrders.Count >= BatchSize)
				{
					await unitOfWork.Repository<Order>().AddRangeAsync(newOrders, cn, false);
					await unitOfWork.SaveChangesAsync(cn);
					await jobLogger?.LogInfoAsync($"ذخیره دسته‌ای هدر: {newOrders.Count} سفارش جدید افزوده شد", cn);
					newOrders.Clear();
					pendingChanges = 0;
				}
				else if (pendingChanges >= BatchSize)
				{
					await unitOfWork.SaveChangesAsync(cn);
					await jobLogger?.LogInfoAsync($"ذخیره دسته‌ای هدر: {BatchSize} تغییر اعمال شد (به‌روزرسانی)", cn);
					pendingChanges = 0;
				}
			}

			if (newOrders.Count > 0)
			{
				await unitOfWork.Repository<Order>().AddRangeAsync(newOrders, cn, false);
				await jobLogger?.LogInfoAsync($"افزودن {newOrders.Count} سفارش جدید (دسته پایانی)", cn);
			}

			if (pendingChanges > 0 || newOrders.Count > 0)
			{
				await unitOfWork.SaveChangesAsync(cn);
			}

			if (skippedCount > 0)
			{
				await jobLogger?.LogWarningAsync(
					$"تعداد {skippedCount} هدر سفارش به دلیل عدم وجود مشتری مرتبط رد شد",
					0,
					cn);
			}

			await jobLogger?.LogInfoAsync(
				$"همگام‌سازی هدر سفارشات تمام شد. به‌روزرسانی‌شده: {updatedCount}، ردشده: {skippedCount}",
				cn);
		}

		private async Task SyncDetailsAsync(IJobLogger? jobLogger, CancellationToken cn)
		{
			await jobLogger?.LogInfoAsync(
				$"در حال بارگذاری اقلام سفارش از راهکاران (FiscalYearRef >= {MinFiscalYearRef})...",
				cn);

			var detailSql = $@"
WITH FactorDateCTE AS (
	SELECT
		oi.OrderItemID,
		MAX(inv.[Date]) AS FactorDate
	FROM SLS3.InvoiceItem AS invItem
	INNER JOIN SLS3.Invoice AS inv ON invItem.InvoiceRef = inv.InvoiceID
	INNER JOIN SYS3.RecursiveEntityRelation AS r1 ON r1.TargetRef = invItem.InvoiceItemID
	INNER JOIN LGS3.InventoryVoucherItem AS invVItem ON r1.SourceRef = invVItem.InventoryVoucherItemID
	INNER JOIN LGS3.InventoryVoucher AS invV ON invVItem.InventoryVoucherRef = invV.InventoryVoucherID
	INNER JOIN SYS3.RecursiveEntityRelation AS r2 ON r2.TargetRef = invItem.InvoiceItemID
	INNER JOIN SLS3.OrderItemProductDetail AS oipd ON r2.SourceRef = oipd.OrderItemProductDetailID
	INNER JOIN SLS3.OrderItem AS oi ON oipd.OrderItemRef = oi.OrderItemID
	WHERE invV.InventoryVoucherSpecificationRef = 69
	GROUP BY oi.OrderItemID
)
SELECT
	OrderItem.OrderItemID AS OrderItemID,
	TRY_CAST(OrderItem.OrderNumber AS BIGINT) AS OrderNumber,
	TRY_CAST(Quotation.Number AS BIGINT) AS PrefactorVchNo,
	[Order].OrderID AS HamkaranOrderHdrId,
	OrderItem.RowNumber AS Seq,
	Part.PartID AS PartHamkaranId,
	OrderItem.ProductUnitRef AS UnitHamkaranId,
	OrderItem.Quantity AS Qty,
	OrderItem.ReferenceRef AS BaseVchItmRef,
	OrderItem.[Description] AS Description,
	OrderItem.PriceBaseUnitRef AS UnitPrice,
	CAST(OrderItem.Price AS BIGINT) AS Price,
	CAST(OrderItem.AdditionAmount AS BIGINT) AS IncPrice,
	CAST(OrderItem.ReductionAmount AS BIGINT) AS DecPrice,
	CASE
		WHEN PaymentAgreement.PaymentAgreementID = 2 THEN N'CRED'
		ELSE N'CASH'
	END AS PayMethodCode,
	CAST(OrderItem.Fee AS BIGINT) AS PriceTemp,
	OrderItem.InitialQuantity AS QtyTemp,
	FactorDateCTE.FactorDate AS FactorMiladiDate
FROM SLS3.OrderItem AS OrderItem
INNER JOIN SLS3.[Order] AS [Order] ON OrderItem.OrderRef = [Order].OrderID
LEFT JOIN SLS3.QuotationItem AS QuotationItem ON QuotationItem.QuotationItemID = OrderItem.ReferenceRef
LEFT JOIN SLS3.Quotation AS Quotation ON Quotation.QuotationID = QuotationItem.QuotationRef
LEFT JOIN SLS3.PaymentAgreement AS PaymentAgreement ON PaymentAgreement.PaymentAgreementID = Quotation.PaymentAgreementRef
INNER JOIN SLS3.Product AS Product ON Product.ProductID = OrderItem.ProductRef
INNER JOIN LGS3.Part AS Part ON Product.PartRef = Part.PartID
LEFT JOIN FactorDateCTE ON FactorDateCTE.OrderItemID = OrderItem.OrderItemID
WHERE [Order].FiscalYearRef >= {MinFiscalYearRef}
	AND [Order].CustomerRef IS NOT NULL
";

			var rahkaranDetails = await Rdb.Database
				.SqlQueryRaw<RahkaranSaleOrderDetailDto>(detailSql)
				.ToListAsync(cn);

			await jobLogger?.LogInfoAsync($"تعداد {rahkaranDetails.Count} قلم سفارش در راهکاران یافت شد", cn);

			await jobLogger?.LogInfoAsync("در حال بارگذاری entity های مرتبط برای اقلام...", cn);

			var orderIdByHamkaranId = (await unitOfWork.Repository<Order>()
					.TableNoTracking
					.Where(x => x.HamkaranId.HasValue && x.Id.HasValue)
					.Select(x => new { x.HamkaranId, x.Id })
					.ToListAsync(cn))
				.GroupBy(x => x.HamkaranId!.Value)
				.ToDictionary(g => g.Key, g => g.First().Id!.Value);

			var partMap = (await unitOfWork.Repository<Part>()
					.TableNoTracking
					.Where(x => x.HamkaranId.HasValue)
					.Select(x => new { x.HamkaranId, x.Id })
					.ToListAsync(cn))
				.GroupBy(x => x.HamkaranId!.Value)
				.ToDictionary(g => g.Key, g => g.First().Id);

			var unitMap = (await unitOfWork.Repository<PartUnit>()
					.TableNoTracking
					.Where(x => x.HamkaranId.HasValue)
					.Select(x => new { x.HamkaranId, x.Id })
					.ToListAsync(cn))
				.GroupBy(x => x.HamkaranId!.Value)
				.ToDictionary(g => g.Key, g => g.First().Id);

			await jobLogger?.LogInfoAsync(
				$"Entity های مرتبط اقلام بارگذاری شدند: {orderIdByHamkaranId.Count} سفارش، {partMap.Count} کالا، {unitMap.Count} واحد",
				cn);

			var appDetails = await unitOfWork.Repository<OrderDetail>()
				.Table
				.Where(x => x.HamkaranId.HasValue)
				.ToListAsync(cn);

			await jobLogger?.LogInfoAsync($"تعداد {appDetails.Count} قلم سفارش با HamkaranId در پایگاه داده برنامه موجود است", cn);

			var appDetailsDict = appDetails
				.GroupBy(x => x.HamkaranId!.Value)
				.ToDictionary(g => g.Key, g => g.First());

			var newDetails = new List<OrderDetail>();
			var updatedCount = 0;
			var skippedNoOrder = 0;
			var skippedNoPart = 0;
			var pendingChanges = 0;

			foreach (var row in rahkaranDetails)
			{
				if (!orderIdByHamkaranId.TryGetValue(row.HamkaranOrderHdrId, out var saleOrderId))
				{
					skippedNoOrder++;
					continue;
				}

				if (!row.PartHamkaranId.HasValue || !partMap.TryGetValue(row.PartHamkaranId.Value, out var partId) || !partId.HasValue)
				{
					skippedNoPart++;
					continue;
				}

				long? unitId = null;
				if (row.UnitHamkaranId.HasValue && unitMap.TryGetValue(row.UnitHamkaranId.Value, out var mappedUnitId))
				{
					unitId = mappedUnitId;
				}

				var payMethod = MapPayMethod(row.PayMethodCode);
				var factorShamsi = row.FactorMiladiDate.HasValue
					? row.FactorMiladiDate.Value.ToShamsiDate()
					: null;
				var qty = row.Qty ?? 0m;
				var qtyTemp = row.QtyTemp ?? 0m;
				var seq = row.Seq ?? 0;

				if (!appDetailsDict.TryGetValue(row.OrderItemID, out var existDetail))
				{
					var now = DateTime.Now;
					var newDetail = new OrderDetail
					{
						HamkaranId = row.OrderItemID,
						HamkaranOrderHdrId = row.HamkaranOrderHdrId,
						SaleOrderId = saleOrderId,
						Prefactor_VchNo = row.PrefactorVchNo,
						Seq = seq,
						PartId = partId,
						UnitId = unitId,
						Qty = qty,
						BaseVchItmRef = row.BaseVchItmRef,
						Description = Truncate(row.Description, 4000),
						UnitPrice = row.UnitPrice,
						Price = row.Price,
						IncPrice = row.IncPrice,
						DecPrice = row.DecPrice,
						PayMethod = payMethod,
						PriceTemp = row.PriceTemp,
						QtyTemp = qtyTemp,
						FactorMiladiDate = row.FactorMiladiDate,
						FactorShamsiDate = Truncate(factorShamsi, 30),
						CreatedOnMiladiDateTime = now,
						CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
						IsActive = IsActiveEnum.Active
					};
					newDetails.Add(newDetail);
					appDetailsDict[row.OrderItemID] = newDetail;
					pendingChanges++;
				}
				else
				{
					var changed = false;
					changed |= SetIfChanged(existDetail.HamkaranOrderHdrId, row.HamkaranOrderHdrId, v => existDetail.HamkaranOrderHdrId = v);
					changed |= SetIfChanged(existDetail.SaleOrderId, saleOrderId, v => existDetail.SaleOrderId = v);
					changed |= SetIfChanged(existDetail.Prefactor_VchNo, row.PrefactorVchNo, v => existDetail.Prefactor_VchNo = v);
					changed |= SetIfChanged(existDetail.Seq, seq, v => existDetail.Seq = v);
					changed |= SetIfChanged(existDetail.PartId, partId, v => existDetail.PartId = v);
					changed |= SetIfChanged(existDetail.UnitId, unitId, v => existDetail.UnitId = v);
					changed |= SetIfChanged(existDetail.Qty, qty, v => existDetail.Qty = v);
					changed |= SetIfChanged(existDetail.BaseVchItmRef, row.BaseVchItmRef, v => existDetail.BaseVchItmRef = v);
					changed |= SetIfChanged(existDetail.Description, Truncate(row.Description, 4000), v => existDetail.Description = v);
					changed |= SetIfChanged(existDetail.UnitPrice, row.UnitPrice, v => existDetail.UnitPrice = v);
					changed |= SetIfChanged(existDetail.Price, row.Price, v => existDetail.Price = v);
					changed |= SetIfChanged(existDetail.IncPrice, row.IncPrice, v => existDetail.IncPrice = v);
					changed |= SetIfChanged(existDetail.DecPrice, row.DecPrice, v => existDetail.DecPrice = v);
					changed |= SetIfChanged(existDetail.PayMethod, payMethod, v => existDetail.PayMethod = v);
					changed |= SetIfChanged(existDetail.PriceTemp, row.PriceTemp, v => existDetail.PriceTemp = v);
					changed |= SetIfChanged(existDetail.QtyTemp, qtyTemp, v => existDetail.QtyTemp = v);
					changed |= SetIfChanged(existDetail.FactorMiladiDate, row.FactorMiladiDate, v => existDetail.FactorMiladiDate = v);
					changed |= SetIfChanged(existDetail.FactorShamsiDate, Truncate(factorShamsi, 30), v => existDetail.FactorShamsiDate = v);

					if (changed)
					{
						updatedCount++;
						pendingChanges++;
					}
				}

				if (newDetails.Count >= BatchSize)
				{
					await unitOfWork.Repository<OrderDetail>().AddRangeAsync(newDetails, cn, false);
					await unitOfWork.SaveChangesAsync(cn);
					await jobLogger?.LogInfoAsync($"ذخیره دسته‌ای اقلام: {newDetails.Count} قلم جدید افزوده شد", cn);
					newDetails.Clear();
					pendingChanges = 0;
				}
				else if (pendingChanges >= BatchSize)
				{
					await unitOfWork.SaveChangesAsync(cn);
					await jobLogger?.LogInfoAsync($"ذخیره دسته‌ای اقلام: {BatchSize} تغییر اعمال شد (به‌روزرسانی)", cn);
					pendingChanges = 0;
				}
			}

			if (newDetails.Count > 0)
			{
				await unitOfWork.Repository<OrderDetail>().AddRangeAsync(newDetails, cn, false);
				await jobLogger?.LogInfoAsync($"افزودن {newDetails.Count} قلم سفارش جدید (دسته پایانی)", cn);
			}

			if (pendingChanges > 0 || newDetails.Count > 0)
			{
				await unitOfWork.SaveChangesAsync(cn);
			}

			if (skippedNoOrder > 0)
			{
				await jobLogger?.LogWarningAsync(
					$"تعداد {skippedNoOrder} قلم به دلیل نبود سفارش والد رد شد",
					0,
					cn);
			}

			if (skippedNoPart > 0)
			{
				await jobLogger?.LogWarningAsync(
					$"تعداد {skippedNoPart} قلم به دلیل نبود کالا مرتبط رد شد",
					0,
					cn);
			}

			await jobLogger?.LogInfoAsync(
				$"همگام‌سازی اقلام سفارش تمام شد. به‌روزرسانی‌شده: {updatedCount}",
				cn);
		}

		private async Task MarkDeletedOrdersAsync(IJobLogger? jobLogger, CancellationToken cn)
		{
			await jobLogger?.LogInfoAsync("در حال علامت‌گذاری سفارشات حذف‌شده/نامعتبر در راهکاران...", cn);

			var activeIdsSql = $@"
SELECT OrderHeader.OrderID AS Value
FROM SLS3.[Order] AS OrderHeader
WHERE OrderHeader.FiscalYearRef >= {MinFiscalYearRef}
	AND OrderHeader.CustomerRef IS NOT NULL
";

			var activeRahkaranIds = (await Rdb.Database
					.SqlQueryRaw<long>(activeIdsSql)
					.ToListAsync(cn))
				.ToHashSet();

			var candidates = await unitOfWork.Repository<Order>()
				.Table
				.Where(x => x.HamkaranId.HasValue)
				.ToListAsync(cn);

			var markedCount = 0;
			var pendingChanges = 0;

			foreach (var order in candidates)
			{
				if (!IsInSyncScope(order))
				{
					continue;
				}

				if (activeRahkaranIds.Contains(order.HamkaranId!.Value))
				{
					continue;
				}

				if (order.StatusEnum == SaleOrderStatusEnum.Deleted)
				{
					continue;
				}

				order.StatusEnum = SaleOrderStatusEnum.Deleted;
				markedCount++;
				pendingChanges++;

				if (pendingChanges >= BatchSize)
				{
					await unitOfWork.SaveChangesAsync(cn);
					await jobLogger?.LogInfoAsync($"علامت‌گذاری حذف: {BatchSize} سفارش ذخیره شد", cn);
					pendingChanges = 0;
				}
			}

			if (pendingChanges > 0)
			{
				await unitOfWork.SaveChangesAsync(cn);
			}

			await jobLogger?.LogInfoAsync(
				$"تعداد {markedCount} سفارش با وضعیت حذف‌شده ({(int)SaleOrderStatusEnum.Deleted}) علامت‌گذاری شد",
				cn);
		}

		private static bool IsInSyncScope(Order order)
		{
			if (order.FiscalYearRef.HasValue)
			{
				return order.FiscalYearRef.Value >= MinFiscalYearRef;
			}

			return long.TryParse(order.Year, out var year) && year >= MinFiscalYearRef;
		}

		private static SaleOrderStatusEnum? MapStatus(int? rahkaranState)
		{
			if (!rahkaranState.HasValue)
			{
				return null;
			}

			// مطابق CASE در ReadData_Hamkaran.GetAllSale_OrderHeader + LookupType 26
			return rahkaranState.Value switch
			{
				0 => SaleOrderStatusEnum.NoStatus,
				1 => SaleOrderStatusEnum.Initial,
				2 => SaleOrderStatusEnum.Approved,
				3 => SaleOrderStatusEnum.InUse,
				4 => SaleOrderStatusEnum.Approved,
				5 => SaleOrderStatusEnum.Suspended,
				6 => SaleOrderStatusEnum.Cancelled,
				_ => null
			};
		}

		private static SaleOrderTypeEnum? MapVoucherType(long? salesTypeRef)
		{
			if (!salesTypeRef.HasValue)
			{
				return null;
			}

			return salesTypeRef.Value switch
			{
				3 => SaleOrderTypeEnum.Cash,
				6 => SaleOrderTypeEnum.Gift,
				7 => SaleOrderTypeEnum.Discount,
				10 => SaleOrderTypeEnum.ReturnPreviousYear,
				13 => SaleOrderTypeEnum.Credit,
				14 => SaleOrderTypeEnum.Trust,
				_ => null
			};
		}

		private static SaleOrderPayMethodEnum MapPayMethod(string? payMethodCode)
		{
			if (string.Equals(payMethodCode, "CRED", StringComparison.OrdinalIgnoreCase))
			{
				return SaleOrderPayMethodEnum.PermanentCredit;
			}

			if (string.Equals(payMethodCode, "CREDT", StringComparison.OrdinalIgnoreCase))
			{
				return SaleOrderPayMethodEnum.TemporaryCredit;
			}

			return SaleOrderPayMethodEnum.Cash;
		}

		private static long? ResolveBranchId(long? branchCode, HashSet<long> branchIds)
		{
			if (branchCode.HasValue && branchIds.Contains(branchCode.Value))
			{
				return branchCode.Value;
			}

			if (branchIds.Contains(DefaultBranchId))
			{
				return DefaultBranchId;
			}

			return branchCode;
		}

		private static bool SetIfChanged<T>(T current, T incoming, Action<T> setter)
		{
			if (EqualityComparer<T>.Default.Equals(current, incoming))
			{
				return false;
			}

			setter(incoming);
			return true;
		}

		private static string? Truncate(string? value, int maxLength)
		{
			if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
			{
				return value;
			}

			return value[..maxLength];
		}

		private const string HtsStampOrderSql = """
			;WITH h AS (
				SELECT
					CAST(s.Order_ID AS BIGINT) AS HtsId,
					s.VchHdrId AS HamkaranId,
					CAST(s.VchNo AS BIGINT) AS HtsVchNo,
					CAST(s.Stock_FK AS BIGINT) AS HtsStockId,
					CAST(s.BaseVchType_FK AS BIGINT) AS HtsBaseVoucherTypeId,
					s.ContractVchHeaderId AS HtsContractVchHeaderId
				FROM OPENQUERY(TMS, N'SELECT Order_ID, VchHdrId, VchNo, Stock_FK, BaseVchType_FK, ContractVchHeaderId FROM TotalSystem.dbo.Sale_Order') s
			),
			cand AS (
				SELECT
					o.Id, h.HtsId, h.HtsVchNo, h.HtsStockId, h.HtsBaseVoucherTypeId, h.HtsContractVchHeaderId,
					ROW_NUMBER() OVER (PARTITION BY h.HtsId ORDER BY o.Id) AS RnHts,
					ROW_NUMBER() OVER (PARTITION BY o.Id ORDER BY h.HtsId) AS RnOrd
				FROM Sale.[Order] o
				INNER JOIN h ON o.HamkaranId = h.HamkaranId
				WHERE ISNULL(o.HtsId, 0) = 0
				  AND NOT EXISTS (SELECT 1 FROM Sale.[Order] x WHERE x.HtsId = h.HtsId AND x.HtsId <> 0)
			)
			UPDATE o SET
				o.HtsId = c.HtsId,
				o.HtsVchNo = c.HtsVchNo,
				o.HtsStockId = c.HtsStockId,
				o.HtsBaseVoucherTypeId = c.HtsBaseVoucherTypeId,
				o.HtsContractVchHeaderId = c.HtsContractVchHeaderId,
				o.ModifiedByName = N'sync-saleorder-hts',
				o.ModifiedDateMiladiDateTime = SYSUTCDATETIME()
			FROM Sale.[Order] o
			INNER JOIN cand c ON c.Id = o.Id AND c.RnHts = 1 AND c.RnOrd = 1;
			""";

		private const string HtsStampAndInsertDetailSql = """
			;WITH h AS (
				SELECT
					CAST(d.OrderDetail_ID AS BIGINT) AS HtsId,
					CAST(d.Order_FK AS BIGINT) AS OrderHtsId,
					d.Hamkaran_Vchitm_FK AS HamkaranId,
					d.Hamkaran_VchHdr_FK AS HamkaranOrderHdrId,
					d.Hamkaran_InvVchitm_FK AS HamkaranInvVchItmId,
					CAST(d.Prefactor_VchNo AS BIGINT) AS Prefactor_VchNo,
					CAST(d.Seq AS INT) AS Seq,
					CAST(d.Part_FK AS BIGINT) AS PartHtsId,
					d.Qty, d.BaseVchItmRef, d.Description, d.UnitPrice, d.Price, d.IncPrice, d.DecPrice,
					CAST(d.PayMethod_FK AS INT) AS PayMethod, d.PriceTemp, ISNULL(d.QtyTemp, 0) AS QtyTemp,
					d.IsPackage, d.NotComplete
				FROM OPENQUERY(TMS, N'SELECT OrderDetail_ID, Order_FK, Hamkaran_Vchitm_FK, Hamkaran_VchHdr_FK, Hamkaran_InvVchitm_FK, Prefactor_VchNo, Seq, Part_FK, Qty, BaseVchItmRef, Description, UnitPrice, Price, IncPrice, DecPrice, PayMethod_FK, PriceTemp, QtyTemp, IsPackage, NotComplete FROM TotalSystem.dbo.Sale_OrderDetail') d
			),
			cand AS (
				SELECT
					det.Id, h.HtsId, h.HamkaranInvVchItmId, h.IsPackage, h.NotComplete, o.Id AS SaleOrderId,
					ROW_NUMBER() OVER (PARTITION BY h.HtsId ORDER BY det.Id) AS RnHts,
					ROW_NUMBER() OVER (PARTITION BY det.Id ORDER BY h.HtsId) AS RnDet
				FROM Sale.OrderDetail det
				INNER JOIN h ON det.HamkaranId = h.HamkaranId
				LEFT JOIN Sale.[Order] o ON o.HtsId = h.OrderHtsId AND o.HtsId <> 0
				WHERE ISNULL(det.HtsId, 0) = 0
				  AND NOT EXISTS (SELECT 1 FROM Sale.OrderDetail x WHERE x.HtsId = h.HtsId AND x.HtsId <> 0)
			)
			UPDATE det SET
				det.HtsId = c.HtsId,
				det.HamkaranInvVchItmId = c.HamkaranInvVchItmId,
				det.IsPackage = c.IsPackage,
				det.NotComplete = c.NotComplete,
				det.Sale_OrderId = COALESCE(c.SaleOrderId, det.Sale_OrderId),
				det.ModifiedByName = N'sync-saleorder-hts',
				det.ModifiedDateMiladiDateTime = SYSUTCDATETIME()
			FROM Sale.OrderDetail det
			INNER JOIN cand c ON c.Id = det.Id AND c.RnHts = 1 AND c.RnDet = 1;

			INSERT INTO Sale.OrderDetail
				(HtsId, Sale_OrderId, HamkaranId, HamkaranOrderHdrId, HamkaranInvVchItmId, Prefactor_VchNo, Seq, PartId,
				 Qty, BaseVchItmRef, Description, UnitPrice, Price, IncPrice, DecPrice, PayMethod, PriceTemp, QtyTemp,
				 IsPackage, NotComplete, CustomerId,
				 CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
				 ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
			SELECT
				h.HtsId, o.Id, h.HamkaranId, h.HamkaranOrderHdrId, h.HamkaranInvVchItmId, h.Prefactor_VchNo, ISNULL(h.Seq, 0), p.Id,
				ISNULL(h.Qty, 0), h.BaseVchItmRef, h.Description, h.UnitPrice, h.Price, h.IncPrice, h.DecPrice,
				ISNULL(h.PayMethod, 0), h.PriceTemp, h.QtyTemp, h.IsPackage, h.NotComplete, o.CustomerId,
				1, N'sync-saleorder-hts', SYSUTCDATETIME(), CONVERT(NVARCHAR(30), GETDATE(), 120),
				1, N'sync-saleorder-hts', SYSUTCDATETIME(), CONVERT(NVARCHAR(30), GETDATE(), 120), 1
			FROM OPENQUERY(TMS, N'SELECT OrderDetail_ID, Order_FK, Hamkaran_Vchitm_FK, Hamkaran_VchHdr_FK, Hamkaran_InvVchitm_FK, Prefactor_VchNo, Seq, Part_FK, Qty, BaseVchItmRef, Description, UnitPrice, Price, IncPrice, DecPrice, PayMethod_FK, PriceTemp, QtyTemp, IsPackage, NotComplete FROM TotalSystem.dbo.Sale_OrderDetail') raw
			CROSS APPLY (
				SELECT
					CAST(raw.OrderDetail_ID AS BIGINT) AS HtsId,
					CAST(raw.Order_FK AS BIGINT) AS OrderHtsId,
					raw.Hamkaran_Vchitm_FK AS HamkaranId,
					raw.Hamkaran_VchHdr_FK AS HamkaranOrderHdrId,
					raw.Hamkaran_InvVchitm_FK AS HamkaranInvVchItmId,
					CAST(raw.Prefactor_VchNo AS BIGINT) AS Prefactor_VchNo,
					CAST(raw.Seq AS INT) AS Seq,
					CAST(raw.Part_FK AS BIGINT) AS PartHtsId,
					raw.Qty, raw.BaseVchItmRef, raw.Description, raw.UnitPrice, raw.Price, raw.IncPrice, raw.DecPrice,
					CAST(raw.PayMethod_FK AS INT) AS PayMethod, raw.PriceTemp, ISNULL(raw.QtyTemp, 0) AS QtyTemp,
					raw.IsPackage, raw.NotComplete
			) h
			INNER JOIN Sale.[Order] o ON o.HtsId = h.OrderHtsId AND o.HtsId <> 0
			LEFT JOIN Inv.Part p ON p.HtsId = h.PartHtsId AND p.HtsId <> 0
			WHERE NOT EXISTS (SELECT 1 FROM Sale.OrderDetail d WHERE d.HtsId = h.HtsId AND d.HtsId <> 0);
			""";

		private const string HtsMergeSerialSql = """
			MERGE Sale.OrderDetailSerial AS t
			USING (
				SELECT
					CAST(s.OrderDetail_Serial_ID AS BIGINT) AS HtsId,
					d.Id AS OrderDetailId,
					c.Id AS CustomerId,
					addr.Id AS CustomerAddressId,
					pr.Id AS PlaningProjectId,
					NULLIF(LTRIM(RTRIM(s.Serial)), N'') AS Serial,
					NULLIF(LTRIM(RTRIM(s.Model)), N'') AS Model,
					CAST(s.Hours_Daily AS NVARCHAR(20)) AS HoursDaily,
					s.Runtime, s.LifeTime, s.Guarantee_Time AS GuaranteeTime,
					CAST(s.Guarantee_SendDay AS INT) AS GuaranteeSendDay,
					CAST(s.Guarantee_LaunchDay AS INT) AS GuaranteeLaunchDay,
					s.HasInsurance,
					s.SendDate AS SendMiladiDate,
					NULLIF(LTRIM(RTRIM(s.SendDate_Shamsi)), N'') AS SendShamsiDate,
					NULLIF(LTRIM(RTRIM(s.SendTime)), N'') AS SendTime,
					s.LaunchDate AS LaunchMiladiDate,
					NULLIF(LTRIM(RTRIM(s.LaunchDate_Shamsi)), N'') AS LaunchShamsiDate,
					s.ExitFactory_Date AS ExitFactoryMiladiDate,
					NULLIF(LTRIM(RTRIM(s.ExitFactory_Date_Shamsi)), N'') AS ExitFactoryShamsiDate,
					NULLIF(LTRIM(RTRIM(s.ExitFactory_Time)), N'') AS ExitFactoryTime,
					CAST(s.ProductionOrder_Number AS NVARCHAR(50)) AS ProductionOrderNumber,
					s.IndustrialComment, s.InvComment, s.IndustrialConfirm,
					s.FinalExitDate AS FinalExitMiladiDate,
					NULLIF(LTRIM(RTRIM(s.FinalExitDate_Shamsi)), N'') AS FinalExitShamsiDate,
					NULLIF(LTRIM(RTRIM(s.ExitTimeFinal)), N'') AS ExitTimeFinal
				FROM OPENQUERY(TMS, N'SELECT OrderDetail_Serial_ID, OrderDetail_FK, NewCustomer_FK, Customer_Address_FK, Pln_Project_FK, Serial, Model, Hours_Daily, Runtime, LifeTime, Guarantee_Time, Guarantee_SendDay, Guarantee_LaunchDay, HasInsurance, SendDate, SendDate_Shamsi, SendTime, LaunchDate, LaunchDate_Shamsi, ExitFactory_Date, ExitFactory_Date_Shamsi, ExitFactory_Time, ProductionOrder_Number, IndustrialComment, InvComment, IndustrialConfirm, FinalExitDate, FinalExitDate_Shamsi, ExitTimeFinal FROM TotalSystem.dbo.Sale_OrderDetail_Serial') s
				INNER JOIN Sale.OrderDetail d ON d.HtsId = CAST(s.OrderDetail_FK AS BIGINT) AND d.HtsId <> 0
				LEFT JOIN SLS.Customer c ON c.HtsId = CAST(s.NewCustomer_FK AS BIGINT) AND c.HtsId <> 0
				LEFT JOIN SLS.CustomerAddress addr ON addr.HtsId = CAST(s.Customer_Address_FK AS BIGINT) AND addr.HtsId <> 0
				LEFT JOIN Pln.Project pr ON pr.HtsId = CAST(s.Pln_Project_FK AS BIGINT) AND pr.HtsId <> 0
			) AS s
			ON t.HtsId = s.HtsId
			WHEN MATCHED THEN UPDATE SET
				t.OrderDetailId = s.OrderDetailId, t.CustomerId = s.CustomerId, t.CustomerAddressId = s.CustomerAddressId,
				t.PlaningProjectId = s.PlaningProjectId,
				t.Serial = s.Serial, t.Model = s.Model, t.HoursDaily = s.HoursDaily, t.Runtime = s.Runtime, t.LifeTime = s.LifeTime,
				t.GuaranteeTime = s.GuaranteeTime, t.GuaranteeSendDay = s.GuaranteeSendDay, t.GuaranteeLaunchDay = s.GuaranteeLaunchDay,
				t.HasInsurance = s.HasInsurance, t.SendMiladiDate = s.SendMiladiDate, t.SendShamsiDate = s.SendShamsiDate, t.SendTime = s.SendTime,
				t.LaunchMiladiDate = s.LaunchMiladiDate, t.LaunchShamsiDate = s.LaunchShamsiDate,
				t.ExitFactoryMiladiDate = s.ExitFactoryMiladiDate, t.ExitFactoryShamsiDate = s.ExitFactoryShamsiDate, t.ExitFactoryTime = s.ExitFactoryTime,
				t.ProductionOrderNumber = s.ProductionOrderNumber, t.IndustrialComment = s.IndustrialComment, t.InvComment = s.InvComment,
				t.IndustrialConfirm = s.IndustrialConfirm, t.FinalExitMiladiDate = s.FinalExitMiladiDate,
				t.FinalExitShamsiDate = s.FinalExitShamsiDate, t.ExitTimeFinal = s.ExitTimeFinal,
				t.ModifiedByName = N'sync-saleorder-hts',
				t.ModifiedDateMiladiDateTime = SYSUTCDATETIME()
			WHEN NOT MATCHED THEN INSERT
				(HtsId, OrderDetailId, CustomerId, CustomerAddressId, PlaningProjectId, Serial, Model, HoursDaily, Runtime, LifeTime,
				 GuaranteeTime, GuaranteeSendDay, GuaranteeLaunchDay, HasInsurance, SendMiladiDate, SendShamsiDate, SendTime,
				 LaunchMiladiDate, LaunchShamsiDate, ExitFactoryMiladiDate, ExitFactoryShamsiDate, ExitFactoryTime,
				 ProductionOrderNumber, IndustrialComment, InvComment, IndustrialConfirm,
				 FinalExitMiladiDate, FinalExitShamsiDate, ExitTimeFinal,
				 CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
				 ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
			VALUES
				(s.HtsId, s.OrderDetailId, s.CustomerId, s.CustomerAddressId, s.PlaningProjectId, s.Serial, s.Model, s.HoursDaily, s.Runtime, s.LifeTime,
				 s.GuaranteeTime, s.GuaranteeSendDay, s.GuaranteeLaunchDay, s.HasInsurance, s.SendMiladiDate, s.SendShamsiDate, s.SendTime,
				 s.LaunchMiladiDate, s.LaunchShamsiDate, s.ExitFactoryMiladiDate, s.ExitFactoryShamsiDate, s.ExitFactoryTime,
				 s.ProductionOrderNumber, s.IndustrialComment, s.InvComment, s.IndustrialConfirm,
				 s.FinalExitMiladiDate, s.FinalExitShamsiDate, s.ExitTimeFinal,
				 1, N'sync-saleorder-hts', SYSUTCDATETIME(), CONVERT(NVARCHAR(30), GETDATE(), 120),
				 1, N'sync-saleorder-hts', SYSUTCDATETIME(), CONVERT(NVARCHAR(30), GETDATE(), 120), 1);
			""";

		private sealed class RahkaranSaleOrderHeaderDto
		{
			public long OrderID { get; set; }
			public long? OrderNumber { get; set; }
			public long? FiscalYearRef { get; set; }
			public DateTime? VchDateMiladi { get; set; }
			public string? VchDateShamsi { get; set; }
			public int? RahkaranState { get; set; }
			public long? SalesTypeRef { get; set; }
			public long? BranchCode { get; set; }
			public long? CustomerRef { get; set; }
			public long? ContractRef { get; set; }
			public DateTime? FactorMiladiDate { get; set; }
		}

		private sealed class RahkaranSaleOrderDetailDto
		{
			public long OrderItemID { get; set; }
			public long? OrderNumber { get; set; }
			public long? PrefactorVchNo { get; set; }
			public long HamkaranOrderHdrId { get; set; }
			public int? Seq { get; set; }
			public long? PartHamkaranId { get; set; }
			public long? UnitHamkaranId { get; set; }
			public decimal? Qty { get; set; }
			public long? BaseVchItmRef { get; set; }
			public string? Description { get; set; }
			public long? UnitPrice { get; set; }
			public long? Price { get; set; }
			public long? IncPrice { get; set; }
			public long? DecPrice { get; set; }
			public string? PayMethodCode { get; set; }
			public long? PriceTemp { get; set; }
			public decimal? QtyTemp { get; set; }
			public DateTime? FactorMiladiDate { get; set; }
		}
	}
}
