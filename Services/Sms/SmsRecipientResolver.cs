using Data;
using Entities.App.Hcm;
using Entities.App.Sale;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Services.Settings;

namespace Services.Sms
{
	/// <summary>
	/// پیدا کردن شماره موبایل گیرندگان پیامک. خروجی همه متدها رشته‌ای با جداکننده «;» است که مستقیم به
	/// <see cref="ISmsService.EnqueueAsync(string, string?, string, string?, long?, DateTime?, CancellationToken)"/> داده می‌شود
	/// (شماره‌های تکراری و نامعتبر همان‌جا حذف/علامت‌گذاری می‌شوند).
	/// </summary>
	public interface ISmsRecipientResolver
	{
		/// <summary>موبایل پرسنل (Personel.Mobile)؛ خالی اگر ثبت نشده باشد.</summary>
		Task<string> GetPersonelMobileAsync(long? personelId, CancellationToken ct = default);

		/// <summary>
		/// موبایل اعضای فعال یک گروه اعلان (NotificationGroup) از مسیر User.PartyId به Personel.PartyId.
		/// اگر <paramref name="branchCode"/> داده شود فقط پرسنل همان شعبه (Personel.BranchCode) می‌آیند.
		/// </summary>
		Task<string> GetGroupMobilesAsync(string groupCode, int? branchCode = null, CancellationToken ct = default);

		/// <summary>شماره‌های ذخیره‌شده در GeneralSetting (جدا شده با ;). اگر مقدار نداشته باشد خالی برمی‌گردد.</summary>
		Task<string> GetSettingNumbersAsync(string settingKey, CancellationToken ct = default);

		/// <summary>موبایل پرسنلی که کاربر جاری/داده‌شده با او به یک Party وصل است (برای تشخیص شعبه سازنده).</summary>
		Task<Personel?> GetPersonelByUserIdAsync(long? userId, CancellationToken ct = default);

		/// <summary>
		/// شماره موبایل «نماینده مشتری» یک سفارش فروش (معادل prm.CustomerAgentPhone در HTS):
		/// سفارش ← قرارداد ← سفارش ساخت ← اعلام نیازمندی (با شماره سفارش ساخت) ← RepresentativeContactNumber.
		/// فقط شماره‌های موبایل معتبر برمی‌گردند (تلفن ثابت/متن نامعتبر حذف می‌شود)؛ اگر چیزی پیدا نشد خالی است
		/// و فراخواننده به موبایل شرکت مشتری برمی‌گردد.
		/// </summary>
		Task<string> GetCustomerAgentMobileAsync(long? orderId, CancellationToken ct = default);
	}

	public class SmsRecipientResolver(ApplicationDbContext db, IGeneralSettingService settings) : ISmsRecipientResolver
	{
		public async Task<string> GetPersonelMobileAsync(long? personelId, CancellationToken ct = default)
		{
			if (personelId is null or 0)
				return string.Empty;

			var mobile = await db.Set<Personel>().AsNoTracking()
				.Where(p => p.Id == personelId)
				.Select(p => p.Mobile)
				.FirstOrDefaultAsync(ct);
			return mobile?.Trim() ?? string.Empty;
		}

		public async Task<string> GetGroupMobilesAsync(string groupCode, int? branchCode = null, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(groupCode))
				return string.Empty;

			var code = groupCode.Trim();

			var query =
				from m in db.NotificationGroupMembers.AsNoTracking()
				where m.NotificationGroup.Code == code
					&& m.NotificationGroup.IsActive == IsActiveEnum.Active
					&& m.IsActive == IsActiveEnum.Active
				join u in db.Users.AsNoTracking() on (long?)m.UserId equals u.Id
				join p in db.Set<Personel>().AsNoTracking() on u.PartyId equals p.PartyId
				where p.Mobile != null && p.Mobile != ""
				select new { p.Mobile, p.BranchCode };

			if (branchCode.HasValue)
				query = query.Where(x => x.BranchCode == branchCode);

			var mobiles = await query.Select(x => x.Mobile!).ToListAsync(ct);
			return string.Join(";", mobiles.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct());
		}

		public async Task<string> GetSettingNumbersAsync(string settingKey, CancellationToken ct = default)
			=> await settings.GetAsync(settingKey, ct) ?? string.Empty;

		public async Task<string> GetCustomerAgentMobileAsync(long? orderId, CancellationToken ct = default)
		{
			if (orderId is null or 0)
				return string.Empty;

			var productionOrders = await (
				from o in db.Set<Order>().AsNoTracking()
				where o.Id == orderId && o.ContractId != null
				join po in db.Set<ProductionOrder>().AsNoTracking() on o.ContractId equals po.ContractId
				select new { po.RequirmentAdvertiseId, po.ProductionOrderNumber })
				.ToListAsync(ct);
			if (productionOrders.Count == 0)
				return string.Empty;

			// ۱) اتصال مستقیم سفارش ساخت ← اعلام نیازمندی (RequirementAdvertiseIdRef راهکاران)؛ دقیق‌ترین مسیر.
			var advertiseIds = productionOrders.Where(x => x.RequirmentAdvertiseId != null)
				.Select(x => x.RequirmentAdvertiseId!.Value).Distinct().ToList();
			if (advertiseIds.Count > 0)
			{
				var direct = await db.Set<RequirmentAdvertise>().AsNoTracking()
					.Where(x => x.Id != null && advertiseIds.Contains(x.Id.Value)
						&& x.RepresentativeContactNumber != null && x.RepresentativeContactNumber != "")
					.OrderByDescending(x => x.Id)
					.Select(x => x.RepresentativeContactNumber!)
					.ToListAsync(ct);
				var mobiles = FirstValidMobiles(direct);
				if (mobiles.Length > 0)
					return mobiles;
			}

			// ۲) پشتیبان: تطبیق با شماره سفارش ساخت (برای سفارش‌هایی که هنوز اتصال مستقیم ندارند).
			var productionNumbers = productionOrders.Where(x => x.ProductionOrderNumber != null)
				.Select(x => x.ProductionOrderNumber).Distinct().ToList();
			if (productionNumbers.Count == 0)
				return string.Empty;

			// چند اعلام نیازمندی می‌تواند یک شماره سفارش ساخت داشته باشد؛ جدیدترین‌ها اول بررسی می‌شوند.
			var phones = await db.Set<RequirmentAdvertise>().AsNoTracking()
				.Where(x => x.ProductionOrderNumber != null && productionNumbers.Contains(x.ProductionOrderNumber)
					&& x.RepresentativeContactNumber != null && x.RepresentativeContactNumber != "")
				.OrderByDescending(x => x.Id)
				.Select(x => x.RepresentativeContactNumber!)
				.ToListAsync(ct);

			return FirstValidMobiles(phones);
		}

		/// <summary>اولین رشته‌ای که حداقل یک موبایل معتبر دارد؛ خروجی «;» جدا (تلفن ثابت/متن نامعتبر حذف می‌شود).</summary>
		private static string FirstValidMobiles(IEnumerable<string> raw)
		{
			foreach (var phone in raw)
			{
				var valid = SmsHelpers.SplitReceivers(phone).Where(x => x.IsValid).Select(x => x.Mobile).ToList();
				if (valid.Count > 0)
					return string.Join(";", valid);
			}
			return string.Empty;
		}

		public async Task<Personel?> GetPersonelByUserIdAsync(long? userId, CancellationToken ct = default)
		{
			if (userId is null or 0)
				return null;

			var partyId = await db.Users.AsNoTracking()
				.Where(u => u.Id == userId)
				.Select(u => u.PartyId)
				.FirstOrDefaultAsync(ct);
			if (partyId == null)
				return null;

			return await db.Set<Personel>().AsNoTracking().FirstOrDefaultAsync(p => p.PartyId == partyId, ct);
		}
	}
}
