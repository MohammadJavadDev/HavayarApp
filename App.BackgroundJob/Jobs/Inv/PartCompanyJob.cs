using Common.Attributes;
using Data;
using Data.Contracts;
using Entities.App.Gnr;
using Entities.App.Inv;
using Microsoft.EntityFrameworkCore;
using Services.Job;

namespace App.BackgroundJob.Jobs.Inv
{
	public class PartCompanyJob(HtsDbContext htsDb, IUnitOfWork unitOfWork)
	{
		[JobHandler("هماهنگ کردن تامین‌کنندگان محصولات از HTS")]
		public async Task SyncPartCompanyFromHts(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			try
			{
				await jobLogger?.LogInfoAsync("شروع همگام‌سازی تامین‌کنندگان محصولات از HTS", cn);

				var htsLinks = await (
					from pc in htsDb.Hts_Inv_Part_Companies.AsNoTracking()
					join part in htsDb.Hts_Inv_Parts.AsNoTracking() on pc.PartId equals part.Part_ID
					join company in htsDb.Hts_Gnr_ManCompanies.AsNoTracking() on pc.CompanyId equals company.ManCompany_ID
					where part.Hamkaran_Part_FK != null && company.Hamkaran_ManCompany_FK > 0
					select new
					{
						HamkaranPartId = part.Hamkaran_Part_FK!.Value,
						HamkaranCompanyId = (long)company.Hamkaran_ManCompany_FK,
						CompanyName = company.CompanyName
					}
				).ToListAsync(cn);

				await jobLogger?.LogInfoAsync($"تعداد {htsLinks.Count} لینک کالا–تامین‌کننده از HTS دریافت شد", cn);

				var partsDict = await unitOfWork.Repository<Part>().TableNoTracking
					.Where(p => p.HamkaranId != null)
					.Select(p => new { PartId = p.Id!.Value, HamkaranId = p.HamkaranId!.Value })
					.ToDictionaryAsync(x => x.HamkaranId, x => x.PartId, cn);

				var partyHamkaranById = await unitOfWork.Repository<Party>().TableNoTracking
					.Where(p => p.HamkaranId != null)
					.Select(p => new { PartyId = p.Id!.Value, HamkaranId = p.HamkaranId!.Value })
					.ToDictionaryAsync(x => x.PartyId, x => x.HamkaranId, cn);

				var suppliers = await unitOfWork.Repository<Supplier>().TableNoTracking
					.Select(s => new { SupplierId = s.Id!.Value, s.PartyId })
					.ToListAsync(cn);

				var supplierDict = new Dictionary<long, long>();
				foreach (var supplier in suppliers)
				{
					if (!partyHamkaranById.TryGetValue(supplier.PartyId, out var hamkaranId))
						continue;

					supplierDict[hamkaranId] = supplier.SupplierId;
				}

				await jobLogger?.LogInfoAsync(
					$"نگاشت App: {partsDict.Count} کالا و {supplierDict.Count} تامین‌کننده با HamkaranId",
					cn);

				var desired = new HashSet<(long PartId, long SupplierId)>();
				var skippedPartCount = 0;
				var skippedSupplierCount = 0;
				var skippedPartLogged = new HashSet<long>();
				var skippedSupplierLogged = new HashSet<long>();

				foreach (var link in htsLinks)
				{
					if (!partsDict.TryGetValue(link.HamkaranPartId, out var partId))
					{
						skippedPartCount++;
						if (skippedPartLogged.Add(link.HamkaranPartId))
						{
							await jobLogger?.LogWarningAsync(
								$"کالا با HamkaranId {link.HamkaranPartId} در App یافت نشد",
								0,
								cn);
						}
						continue;
					}

					if (!supplierDict.TryGetValue(link.HamkaranCompanyId, out var supplierId))
					{
						skippedSupplierCount++;
						if (skippedSupplierLogged.Add(link.HamkaranCompanyId))
						{
							await jobLogger?.LogWarningAsync(
								$"تامین‌کننده با HamkaranId {link.HamkaranCompanyId} ({link.CompanyName}) در App یافت نشد",
								0,
								cn);
						}
						continue;
					}

					desired.Add((partId, supplierId));
				}

				await jobLogger?.LogInfoAsync(
					$"لینک‌های قابل نگاشت: {desired.Count} | رد شده به‌خاطر کالا: {skippedPartCount} | رد شده به‌خاطر تامین‌کننده: {skippedSupplierCount}",
					cn);

				var syncedPartIds = partsDict.Values.ToHashSet();
				var existing = await unitOfWork.Repository<PartCompany>().Table
					.Where(pc => syncedPartIds.Contains(pc.PartId))
					.ToListAsync(cn);

				var existingKeys = existing
					.Select(pc => (pc.PartId, pc.SupplierId))
					.ToHashSet();

				var toAdd = desired
					.Where(key => !existingKeys.Contains(key))
					.Select(key => new PartCompany
					{
						PartId = key.PartId,
						SupplierId = key.SupplierId
					})
					.ToList();

				var toDelete = existing
					.Where(pc => !desired.Contains((pc.PartId, pc.SupplierId)))
					.ToList();

				if (toAdd.Count > 0)
				{
					await jobLogger?.LogInfoAsync($"افزودن {toAdd.Count} لینک جدید", cn);
					await unitOfWork.Repository<PartCompany>().AddRangeAsync(toAdd, cn, false);
				}

				if (toDelete.Count > 0)
				{
					await jobLogger?.LogInfoAsync($"حذف {toDelete.Count} لینک اضافی نسبت به HTS", cn);
					await unitOfWork.Repository<PartCompany>().DeleteRangeAsync(toDelete, cn, false);
				}

				await unitOfWork.SaveChangesAsync(cn);
				await jobLogger?.LogInfoAsync(
					$"همگام‌سازی تامین‌کنندگان محصولات با موفقیت انجام شد. اضافه: {toAdd.Count}، حذف: {toDelete.Count}، بدون تغییر: {desired.Count - toAdd.Count}",
					cn);
			}
			catch (Exception ex)
			{
				await jobLogger?.LogExceptionAsync(ex, cn);
				throw;
			}
		}
	}
}
