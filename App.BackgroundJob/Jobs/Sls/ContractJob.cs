using Common.Attributes;
using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.App.FIN;
using Entities.App.Sale;
using Entities.App.SLS;
using Entities.App.SLS.Enums;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Globalization;

namespace App.BackgroundJob.Jobs.Sls
{
	public class ContractJob(RahkaranDbContext Rdb, IUnitOfWork unitOfWork, ApplicationDbContext appContext)
	{
		private static readonly PersianCalendar PersianCalendar = new();
		private const long DefaultBranchId = 10;

		[JobHandler("افزودن قرارداد از راهکاران")]
		public async Task AddContractFromRahkaran(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی قراردادها از راهکاران", cn);

				const string sql = @"
SELECT
	RahkaranContractHeader.ContractID,
	RahkaranContractHeader.Number,
	RahkaranContractHeader.Date,
	RahkaranContractHeader.Title,
	RahkaranContractHeader.ContractNumber,
	RahkaranContractHeader.NetPrice,
	RahkaranContractHeader.Description,
	RahkaranContractHeader.State,
	RahkaranContractHeader.SalesTypeRef,
	RahkaranContractHeader.CustomerRef,
	RahkaranContractHeader.FiscalYearRef,
	RahkaranContractHeader.Creator,
	RahkaranContractHeader.CreationDate,
	CAST(RahkaranContractHeader.Version AS BIGINT) AS RahkaranVersion,
	ISNULL(TRY_CAST(SaleOffice.Code AS BIGINT), 10) AS BranchId,
	Dl.DLID AS DlHamkaranId,
	Confirmer.ChangingUserID AS ConfirmerUserId
FROM SLS3.Contract AS RahkaranContractHeader
LEFT JOIN SLS3.SalesOffice AS SaleOffice ON SaleOffice.SalesOfficeID = RahkaranContractHeader.SalesOfficeRef
LEFT JOIN SLS3.Customer AS RahkaranCustomer ON RahkaranCustomer.CustomerID = RahkaranContractHeader.CustomerRef
LEFT JOIN FIN3.DL AS Dl ON Dl.ReferenceID = RahkaranCustomer.PartyRef
	AND Dl.EntityCode = 242
	AND Dl.DLTypeRef IN (1, 2)
LEFT JOIN (
	SELECT RecordID, ChangingUserID
	FROM (
		SELECT
			RahkaranStateHistory.RecordID,
			RahkaranStateHistory.ChangingUserID,
			ROW_NUMBER() OVER (
				PARTITION BY RahkaranStateHistory.RecordID
				ORDER BY RahkaranStateHistory.StateHistoryID DESC
			) AS RowNum
		FROM SYS3.StateHistory AS RahkaranStateHistory
		WHERE RahkaranStateHistory.EntityCode = 1467
			AND RahkaranStateHistory.SourceState < RahkaranStateHistory.TargetState
			AND RahkaranStateHistory.TargetState > 4
	) AS RankedConfirmer
	WHERE RankedConfirmer.RowNum = 1
) AS Confirmer ON Confirmer.RecordID = RahkaranContractHeader.ContractID
";

				await jobLogger?.LogInfoAsync("در حال بارگذاری داده‌های قرارداد از راهکاران...", cn);
				var rahkaranContracts = await Rdb.Database
					.SqlQueryRaw<RahkaranContractDto>(sql)
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {rahkaranContracts.Count} رکورد قرارداد در راهکاران یافت شد", cn);

				await jobLogger?.LogInfoAsync("در حال بارگذاری entity های مرتبط...", cn);

				var customerMap = (await unitOfWork.Repository<Customer>()
						.TableNoTracking
						.ToListAsync(cn))
					.ToDictionary(x => x.HamkaranId, x => x.Id);

				var dlMap = (await unitOfWork.Repository<DL>()
						.TableNoTracking
						.Where(x => x.HamkaranId.HasValue)
						.ToListAsync(cn))
					.ToDictionary(x => x.HamkaranId!.Value, x => x.Id);

				var branchIds = (await unitOfWork.Repository<Branch>()
						.TableNoTracking
						.Select(x => x.Id!.Value)
						.ToListAsync(cn))
					.ToHashSet();

				var users = await appContext.Users
					.AsNoTracking()
					.Where(x => x.HamkaranId.HasValue && x.Id.HasValue)
					.Select(x => new { x.Id, x.HamkaranId, x.NameFa, x.Name })
					.ToListAsync(cn);

				var userMap = users.ToDictionary(x => x.HamkaranId!.Value, x => x.Id!.Value);
				var userNameMap = users.ToDictionary(
					x => x.HamkaranId!.Value,
					x => !string.IsNullOrWhiteSpace(x.NameFa) ? x.NameFa! : (x.Name ?? string.Empty));

				await jobLogger?.LogInfoAsync(
					$"Entity های مرتبط بارگذاری شدند: {customerMap.Count} مشتری، {dlMap.Count} تفصیل، {branchIds.Count} شعبه، {userMap.Count} کاربر",
					cn);

				var appContracts = await unitOfWork.Repository<Contract>()
					.Table
					.ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {appContracts.Count} رکورد قرارداد در پایگاه داده برنامه موجود است", cn);

				var appContractsDict = appContracts
					.Where(x => x.HamkaranId.HasValue)
					.ToDictionary(x => x.HamkaranId!.Value);

				var newContracts = new List<Contract>();
				var updatedContractsCount = 0;
				var skippedCount = 0;

				foreach (var row in rahkaranContracts)
				{
					if (!row.CustomerRef.HasValue || !customerMap.TryGetValue(row.CustomerRef.Value, out var customerId))
					{
						await jobLogger?.LogWarningAsync(
							$"مشتری با HamkaranId {row.CustomerRef} برای قرارداد {row.ContractID} یافت نشد",
							0,
							cn);
						skippedCount++;
						continue;
					}

					long? detailId = null;
					if (row.DlHamkaranId.HasValue && dlMap.TryGetValue(row.DlHamkaranId.Value, out var foundDetailId))
					{
						detailId = foundDetailId;
					}

					var branchId = ResolveBranchId(row.BranchId, branchIds);

					long? creatorId = null;
					string? creatorName = null;
					if (row.Creator.HasValue && userMap.TryGetValue(row.Creator.Value, out var foundCreatorId))
					{
						creatorId = foundCreatorId;
						userNameMap.TryGetValue(row.Creator.Value, out creatorName);
					}

					long? confirmerId = null;
					if (row.ConfirmerUserId.HasValue && userMap.TryGetValue(row.ConfirmerUserId.Value, out var foundConfirmerId))
					{
						confirmerId = foundConfirmerId;
					}

					var status = row.State.HasValue ? (ContractStatusEnum?)row.State.Value : null;
					var salesType = row.SalesTypeRef.HasValue ? (ContractSalesTypeEnum?)row.SalesTypeRef.Value : null;
					var dateShamsi = row.Date.HasValue ? row.Date.Value.ToShamsiDate() : null;
					var month = row.Date.HasValue ? (short?)PersianCalendar.GetMonth(row.Date.Value) : null;

					if (!appContractsDict.TryGetValue(row.ContractID, out var existContract))
					{
						var now = DateTime.Now;
						newContracts.Add(new Contract
						{
							HamkaranId = row.ContractID,
							Number = row.Number,
							DateMiladi = row.Date,
							DateShamsi = dateShamsi,
							CustomerId = customerId,
							DetailId = detailId,
							FiscalYearRef = row.FiscalYearRef,
							Month = month,
							CreatedById = creatorId,
							CreatedByName = creatorName,
							ConfirmerId = confirmerId,
							Status = status,
							Title = row.Title,
							ContractNumber = row.ContractNumber,
							SalesType = salesType,
							NetPrice = row.NetPrice,
							BranchId = branchId,
							Description = Truncate(row.Description, 1024),
							RahkaranVersion = row.RahkaranVersion,
							CreatedOnMiladiDateTime = now,
							CreatedOnShamsiDateTime = now.ToShamsiDateTime(),
							IsActive = IsActiveEnum.Active
						});
					}
					else
					{
						// بروزرسانی وقتی نسخه تغییر کرده یا هنوز همگام نشده (RahkaranVersion خالی)
						if (existContract.RahkaranVersion.HasValue
							&& existContract.RahkaranVersion == row.RahkaranVersion)
						{
							continue;
						}

						SetIfChanged(existContract.BranchId, branchId, v => existContract.BranchId = v);
						SetIfChanged(existContract.Number, row.Number, v => existContract.Number = v);
						SetIfChanged(existContract.DateMiladi, row.Date, v => existContract.DateMiladi = v);
						SetIfChanged(existContract.DateShamsi, dateShamsi, v => existContract.DateShamsi = v);
						SetIfChanged(existContract.CustomerId, customerId, v => existContract.CustomerId = v);
						SetIfChanged(existContract.DetailId, detailId, v => existContract.DetailId = v);
						SetIfChanged(existContract.FiscalYearRef, row.FiscalYearRef, v => existContract.FiscalYearRef = v);
						SetIfChanged(existContract.Month, month, v => existContract.Month = v);
						SetIfChanged(existContract.CreatedById, creatorId, v => existContract.CreatedById = v);
						SetIfChanged(existContract.CreatedByName, creatorName, v => existContract.CreatedByName = v);
						SetIfChanged(existContract.ConfirmerId, confirmerId, v => existContract.ConfirmerId = v);
						SetIfChanged(existContract.Status, status, v => existContract.Status = v);
						SetIfChanged(existContract.Title, row.Title, v => existContract.Title = v);
						SetIfChanged(existContract.ContractNumber, row.ContractNumber, v => existContract.ContractNumber = v);
						SetIfChanged(existContract.SalesType, salesType, v => existContract.SalesType = v);
						SetIfChanged(existContract.NetPrice, row.NetPrice, v => existContract.NetPrice = v);
						SetIfChanged(existContract.Description, Truncate(row.Description, 1024), v => existContract.Description = v);

						if (row.CreationDate.HasValue)
						{
							existContract.CreatedOnMiladiDateTime = row.CreationDate;
							existContract.CreatedOnShamsiDateTime = row.CreationDate.Value.ToShamsiDateTime();
						}

						existContract.RahkaranVersion = row.RahkaranVersion;
						updatedContractsCount++;
					}
				}

				if (skippedCount > 0)
				{
					await jobLogger?.LogWarningAsync(
						$"تعداد {skippedCount} رکورد به دلیل عدم وجود مشتری مرتبط رد شد",
						0,
						cn);
				}

				if (newContracts.Count > 0)
				{
					await jobLogger?.LogInfoAsync($"افزودن {newContracts.Count} قرارداد جدید به پایگاه داده", cn);
					await unitOfWork.Repository<Contract>().AddRangeAsync(newContracts, cn, false);
				}

				if (updatedContractsCount > 0)
				{
					await jobLogger?.LogInfoAsync($"به‌روزرسانی {updatedContractsCount} قرارداد موجود", cn);
				}

				await unitOfWork.SaveChangesAsync(cn);

				await SyncProductionOrderBranches(jobLogger, cn);
				await DeleteRemovedContracts(rahkaranContracts.Select(x => x.ContractID).ToHashSet(), jobLogger, cn);

				await jobLogger?.LogInfoAsync("همگام‌سازی قراردادها با موفقیت انجام شد", cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}

		/// <summary>
		/// بروزرسانی مرکز فروش سفارشات ساخت مطابق Job سیستم HTS
		/// </summary>
		private async Task SyncProductionOrderBranches(IJobLogger? jobLogger, CancellationToken cn)
		{
			var productionOrders = await unitOfWork.Repository<ProductionOrder>()
				.Table
				.Where(x => x.ContractId.HasValue)
				.ToListAsync(cn);

			if (productionOrders.Count == 0)
			{
				return;
			}

			var contractBranchMap = (await unitOfWork.Repository<Contract>()
					.TableNoTracking
					.Where(x => x.BranchId.HasValue)
					.Select(x => new { x.Id, x.BranchId })
					.ToListAsync(cn))
				.ToDictionary(x => x.Id!.Value, x => x.BranchId);

			var updatedCount = 0;
			foreach (var productionOrder in productionOrders)
			{
				if (!productionOrder.ContractId.HasValue
					|| !contractBranchMap.TryGetValue(productionOrder.ContractId.Value, out var branchId)
					|| !branchId.HasValue
					|| productionOrder.BranchId == branchId)
				{
					continue;
				}

				productionOrder.BranchId = branchId;
				updatedCount++;
			}

			if (updatedCount > 0)
			{
				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync($"به‌روزرسانی مرکز فروش {updatedCount} سفارش ساخت", cn);
			}
		}

		/// <summary>
		/// حذف قراردادهایی که در راهکاران وجود ندارند و به سفارش ساخت وصل نیستند
		/// </summary>
		private async Task DeleteRemovedContracts(
			HashSet<long> rahkaranContractIds,
			IJobLogger? jobLogger,
			CancellationToken cn)
		{
			var usedContractIds = (await unitOfWork.Repository<ProductionOrder>()
					.TableNoTracking
					.Where(x => x.ContractId.HasValue)
					.Select(x => x.ContractId!.Value)
					.Distinct()
					.ToListAsync(cn))
				.ToHashSet();

			var candidates = await unitOfWork.Repository<Contract>()
				.Table
				.Where(x => x.HamkaranId.HasValue && x.RahkaranVersion.HasValue)
				.ToListAsync(cn);

			var toDelete = candidates
				.Where(x =>
					!rahkaranContractIds.Contains(x.HamkaranId!.Value)
					&& x.Id.HasValue
					&& !usedContractIds.Contains(x.Id.Value))
				.ToList();

			if (toDelete.Count == 0)
			{
				return;
			}

			foreach (var contract in toDelete)
			{
				await unitOfWork.Repository<Contract>().DeleteAsync(contract, cn, false);
			}

			await unitOfWork.SaveChangesAsync(cn);
			await jobLogger?.LogInfoAsync($"حذف {toDelete.Count} قرارداد که در راهکاران وجود نداشتند", cn);
		}

		private static long? ResolveBranchId(long? branchId, HashSet<long> branchIds)
		{
			if (branchId.HasValue && branchIds.Contains(branchId.Value))
			{
				return branchId.Value;
			}

			if (branchIds.Contains(DefaultBranchId))
			{
				return DefaultBranchId;
			}

			return branchId;
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

		private sealed class RahkaranContractDto
		{
			public long ContractID { get; set; }
			public string? Number { get; set; }
			public DateTime? Date { get; set; }
			public string? Title { get; set; }
			public string? ContractNumber { get; set; }
			public decimal? NetPrice { get; set; }
			public string? Description { get; set; }
			public int? State { get; set; }
			public long? SalesTypeRef { get; set; }
			public long? CustomerRef { get; set; }
			public long? FiscalYearRef { get; set; }
			public long? Creator { get; set; }
			public DateTime? CreationDate { get; set; }
			public long? RahkaranVersion { get; set; }
			public long? BranchId { get; set; }
			public long? DlHamkaranId { get; set; }
			public long? ConfirmerUserId { get; set; }
		}
	}
}
