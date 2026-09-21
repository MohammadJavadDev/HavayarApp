using Data;
using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.FIN;
using Entities.App.Gnr;
using Entities.App.SLS;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Actions.SLS
{
	/// <summary>
	/// معادل HTS SaleCustomerAddressController.DoOperation:
	/// تفصیل نمایندگی الزامی است؛ شرکت نمایندگی از
	/// Acc_DL.Hamkaran_Acc_DL_FK (پد ۵رقمی) → Gnr_ManCompany.DlRef گرفته می‌شود.
	/// AgencyDlId روی ذخیره نوشته می‌شود (باگ HTS که FK تفصیل را در Update جا می‌انداخت کپی نمی‌شود).
	/// </summary>
	public class CustomerAddressAction(IUnitOfWork unitOfWork, ApplicationDbContext db)
	{
		[EntityAction(typeof(CustomerAddress), EntityActionTrigger.BeforeSave,
			"ResolveAgencyFromDl", "استخراج نمایندگی از تفصیل (HTS)", Priority = 1)]
		public async Task ResolveAgencyFromDl(CustomerAddress entity, CancellationToken ct)
		{
			if (entity.AgencyDlId == null || entity.AgencyDlId == 0)
				throw new InvalidOperationException("خطا در تفصیل نمایندگی انتخاب شده ");

			var dl = await unitOfWork.Repository<DL>().TableNoTracking
				.FirstOrDefaultAsync(d => d.Id == entity.AgencyDlId, ct);
			if (dl?.HamkaranId == null || dl.HamkaranId == 0)
				throw new InvalidOperationException("خطا در تفصیل نمایندگی انتخاب شده ");

			var hamkaran = dl.HamkaranId.Value;
			var padded = hamkaran.ToString();
			if (padded.Length < 5)
				padded = padded.PadLeft(5, '0');

			long? partyId = await unitOfWork.Repository<Supplier>().TableNoTracking
				.Where(s => s.DlCode == padded || s.DlCode == hamkaran.ToString() || s.DlCode == dl.Code)
				.Select(s => (long?)s.PartyId)
				.FirstOrDefaultAsync(ct);

			if (partyId == null)
			{
				var ids = await db.Database.SqlQueryRaw<long>(
					"""
					SELECT TOP 1 CAST(p.Id AS bigint) AS [Value]
					FROM [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] AS mc
					INNER JOIN [Gnr].[Party] AS p ON p.HamkaranId = mc.Hamkaran_ManCompany_FK
					WHERE LTRIM(RTRIM(ISNULL(mc.DlRef, N''))) IN ({0}, {1})
					""",
					padded, hamkaran.ToString()).ToListAsync(ct);
				partyId = ids.FirstOrDefault();
				if (partyId == 0)
					partyId = null;
			}

			if (partyId == null)
				throw new InvalidOperationException("خطا در تفصیل نمایندگی انتخاب شده ");

			entity.AgencyPartyId = partyId;
			entity.AgencyParty = null;
			entity.AgencyDl = null;
			entity.Customer = null;
			entity.Zone = null;
			entity.Province = null;
		}
	}
}
