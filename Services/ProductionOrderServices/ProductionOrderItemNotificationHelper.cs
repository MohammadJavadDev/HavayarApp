using Common.Utilities;
using Data;
using Data.Contracts;
using Entities.Auth;
using Entities.Base;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Services.ProductionOrderServices
{
	/// <summary>
	/// کمک‌کننده مشترک اعلان‌های سفارش ساخت / اقلام سفارش ساخت برای WebApp و App.BackgroundJob.
	/// جایگزین لیست‌های ایمیل سخت‌کد HTS (GetIndustrialStaticReceivers، گروه‌های کاربری 540/538/567/380 …) با
	/// گروه‌های اعلان قابل مدیریت در پنل (system.NotificationGroup) و نقش‌های واقعی (system.Role).
	/// فقط به IUnitOfWork وابسته است تا در هر دو Host بدون ثبت DI اضافی قابل استفاده باشد.
	/// </summary>
	public static class ProductionOrderItemNotificationHelper
	{
		#region Group codes (معادل گروه‌های کاربری/لیست‌های ثابت HTS)

		/// <summary>معادل HTS GetIndustrialStaticReceivers + گیرندگان «isSendToPlanningUnitMode» — گیرندگان اعلان‌های ارجاع به صنایع</summary>
		public const string GroupIndustrial = "Sale.ProductionOrderItem.Industrial";

		/// <summary>معادل HTS GetChangeStatusStaticReceivers — CC ثابت اعلان تغییر وضعیت تولید قلم</summary>
		public const string GroupProductionStatusChange = "Sale.ProductionOrderItem.ProductionStatusChange";

		/// <summary>معادل HTS گروه کاربری 540 — اعلان شروع تست (۲۲۰۹) محصولی که مدیر پروژه دارد (IT Support / خدمات پس از فروش)</summary>
		public const string GroupTestStart = "Sale.ProductionOrderItem.TestStart";

		/// <summary>معادل HTS گروه کاربری 538 — اعلان تغییر سریال قلمی که بازرسی محصول دارد (کنترل کیفیت)</summary>
		public const string GroupSerialChangeQc = "Sale.ProductionOrderItem.SerialChangeQc";

		/// <summary>معادل HTS گروه کاربری 567 — دریافت‌کنندگان هشدار کالاهای نزدیک به موعد تحویل</summary>
		public const string GroupNearDeliveryDate = "Sale.ProductionOrderItem.NearDeliveryDate";

		/// <summary>معادل HTS گروه کاربری 380 — کارشناسان رئیس کمیته تامین (CC/To اعلان‌های استعلام)</summary>
		public const string GroupSupplyCommitteeExperts = "Sale.ProductionOrderItem.SupplyCommitteeExperts";

		public const string GroupBomIndustrial = "Sale.ProductionOrderItemBom.Industrial";
		public const string GroupBomEngineering = "Sale.ProductionOrderItemBom.Engineering";

		/// <summary>
		/// فهرست کامل گروه‌ها با عنوان/توضیح برای ایجاد خودکار در صورت نبود (seed SQL هم همین‌ها را می‌سازد).
		/// </summary>
		public static readonly (string Code, string DisplayName, string Description)[] KnownGroups =
		{
			(GroupIndustrial, "سفارش ساخت - اقلام - واحد صنایع", "گیرندگان اعلان ارجاع قلم به کارتابل صنایع (معادل لیست ثابت صنایع در HTS)"),
			(GroupProductionStatusChange, "سفارش ساخت - اقلام - تغییر وضعیت تولید", "CC ثابت اعلان تغییر وضعیت تولید قلم (معادل GetChangeStatusStaticReceivers در HTS)"),
			(GroupTestStart, "سفارش ساخت - اقلام - شروع تست (IT Support)", "اعلان شروع تست تولید (۲۲۰۹) برای سفارش‌های دارای مدیر پروژه (معادل گروه 540 در HTS)"),
			(GroupSerialChangeQc, "سفارش ساخت - اقلام - تغییر سریال (کنترل کیفیت)", "اعلان تغییر سریال قلمی که بازرسی حین ساخت دارد (معادل گروه 538 در HTS)"),
			(GroupNearDeliveryDate, "سفارش ساخت - اقلام - نزدیک به موعد تحویل", "دریافت‌کنندگان هشدار کالاهای نزدیک به موعد تحویل (معادل گروه 567 در HTS)"),
			(GroupSupplyCommitteeExperts, "سفارش ساخت - اقلام - کارشناسان کمیته تامین", "گیرندگان اعلان‌های استعلام/کمیته تامین (معادل گروه 380 در HTS)"),
			(GroupBomIndustrial, "سفارش ساخت - BOM - واحد صنایع", "گیرندگان اصلی اعلان افزودن/ویرایش BOM (معادل لیست صنایع در HTS)"),
			(GroupBomEngineering, "سفارش ساخت - BOM - واحد مهندسی", "گیرندگان CC اعلان BOM (معادل لیست مهندسی در HTS)"),
		};

		#endregion

		/// <summary>
		/// ایمیل اعضای فعال یک گروه اعلان. اگر گروه وجود نداشته باشد با عنوان/توضیح پیش‌فرض ساخته می‌شود (بدون عضو).
		/// </summary>
		public static async Task<List<string>> GetGroupEmailsAsync(IUnitOfWork unitOfWork, string groupCode, CancellationToken ct)
		{
			await EnsureGroupExistsAsync(unitOfWork, groupCode, ct);

			var members = await unitOfWork.Repository<NotificationGroupMember>()
				.TableNoTracking
				.Where(m => m.NotificationGroup.Code == groupCode
					&& m.NotificationGroup.IsActive == IsActiveEnum.Active
					&& m.IsActive == IsActiveEnum.Active)
				.Select(m => new { m.Email, UserEmail = m.User.Email })
				.ToListAsync(ct);

			return members
				.Select(m => m.Email.HasValue() ? m.Email : m.UserEmail)
				.Where(e => e.HasValue())
				.Select(e => e!)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		public static async Task EnsureGroupExistsAsync(IUnitOfWork unitOfWork, string groupCode, CancellationToken ct)
		{
			var exists = await unitOfWork.Repository<NotificationGroup>()
				.TableNoTracking
				.AnyAsync(g => g.Code == groupCode, ct);
			if (exists)
				return;

			var known = KnownGroups.FirstOrDefault(g => g.Code == groupCode);
			var displayName = known.DisplayName ?? groupCode;

			await unitOfWork.Repository<NotificationGroup>().AddAsync(new NotificationGroup
			{
				Code = groupCode,
				DisplayName = displayName,
				Description = known.Description
			}, ct, saveAudit: false, saveNow: true, InvokeAction: false);
		}

		/// <summary>
		/// کاربران فعال دارای یک نقش (system.User.Roles / RoleIds) — برای نقش‌های گردش‌کار مثل رئیس کمیته تامین.
		/// فیلتر نقش سمت کلاینت انجام می‌شود چون Roles/RoleIds به‌صورت JSON ذخیره شده‌اند.
		/// </summary>
		public static async Task<List<User>> GetUsersByRoleAsync(ApplicationDbContext db, string roleName, CancellationToken ct)
		{
			var role = await db.Set<Role>()
				.AsNoTracking()
				.FirstOrDefaultAsync(r => r.Name == roleName, ct);

			var users = await db.Users
				.AsNoTracking()
				.Where(u => u.IsActive == IsActiveEnum.Active)
				.ToListAsync(ct);

			return users
				.Where(u =>
					(u.Roles != null && u.Roles.Contains(roleName, StringComparer.OrdinalIgnoreCase)) ||
					(role?.Id != null && u.RoleIds != null && u.RoleIds.Contains(role.Id.Value)))
				.ToList();
		}

		public static async Task<List<string>> GetRoleEmailsAsync(ApplicationDbContext db, string roleName, CancellationToken ct)
		{
			var users = await GetUsersByRoleAsync(db, roleName, ct);
			return users
				.Where(u => u.Email.HasValue())
				.Select(u => u.Email!)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		public static async Task<List<string>> GetUserEmailsAsync(ApplicationDbContext db, IEnumerable<long> userIds, CancellationToken ct)
		{
			var ids = userIds.Where(id => id > 0).Distinct().ToList();
			if (!ids.Any())
				return new List<string>();

			var emails = await db.Users
				.AsNoTracking()
				.Where(u => u.Id != null && ids.Contains(u.Id.Value) && u.Email != null && u.Email != "")
				.Select(u => u.Email!)
				.ToListAsync(ct);

			return emails.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}

		/// <summary>
		/// صف کردن یک اعلان ایمیلی در system.Notification (ارسال واقعی توسط EmailJob).
		/// اگر To خالی و CC پر باشد، CC به To منتقل می‌شود؛ اگر هر دو خالی باشند چیزی صف نمی‌شود.
		/// </summary>
		public static async Task<bool> QueueEmailAsync(
			IUnitOfWork unitOfWork,
			string title,
			string bodyHtml,
			IEnumerable<string?> to,
			IEnumerable<string?>? cc,
			long? entityId,
			long ownerId,
			string? viewPath,
			CancellationToken ct,
			bool saveNow = true)
		{
			var toList = Normalize(to);
			var ccList = Normalize(cc ?? Array.Empty<string?>())
				.Where(e => !toList.Contains(e, StringComparer.OrdinalIgnoreCase))
				.ToList();

			if (!toList.Any() && !ccList.Any())
				return false;

			if (!toList.Any())
			{
				toList = ccList;
				ccList = new List<string>();
			}

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = title,
				Body = bodyHtml,
				EntityId = entityId,
				OwnerId = ownerId <= 0 ? 1 : ownerId,
				ViewPath = viewPath,
				IsRead = false,
				IsSend = false,
				ToEmails = toList,
				CcEmails = ccList
			}, ct, saveAudit: false, saveNow: saveNow, InvokeAction: false);

			return true;
		}

		private static List<string> Normalize(IEnumerable<string?> emails) =>
			emails
				.Where(e => e.HasValue())
				.Select(e => e!.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

		/// <summary>
		/// قالب استاندارد ایمیل‌های HTS (جدول با سربرگ «گروه صنعتی هوایار»).
		/// </summary>
		public static string BuildEmailShell(string innerHtml)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<div style='text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right;direction:rtl' width='100%'>");
			sb.AppendLine("<tr style='background:#000aa0'><td><div style='font-size:14pt;font-family:Zar;color:#FFFFFF;text-align:center;direction:rtl'>گروه صنعتی هوایار</div></td></tr>");
			sb.AppendLine("<tr><td><div style='font-size:14pt;font-family:Zar;color:#1F497D;text-align:right;direction:rtl'>");
			sb.AppendLine("با سلام و احترام <br />");
			sb.AppendLine(innerHtml);
			sb.AppendLine("</div></td></tr></table></div>");
			return sb.ToString();
		}

		public static string Li(string label, object? value)
		{
			var text = value?.ToString();
			return $"<li>{label} : <strong>{(text.HasValue() ? text : "-")}</strong></li>";
		}
	}
}
