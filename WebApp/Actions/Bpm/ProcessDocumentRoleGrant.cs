using Data.SystemAuth;
using Entities.App.Bpm;
using Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Services.Auth;

namespace WebApp.Actions.Bpm
{
	/// <summary>
	/// اعطای نقش BPM به کاربر (معادل HTS AddUsersPermission برای گروه‌های ۳۵۵ و ۴۱۸).
	/// کنترلر مدیریت و EntityAction ابلاغ از همین نقطه استفاده می‌کنند.
	/// </summary>
	internal static class ProcessDocumentRoleGrant
	{
		public static async Task GrantAsync(
			IUserService userService,
			IOnlineUserService? onlineUserService,
			string roleName,
			IEnumerable<long> userIds,
			CancellationToken cn)
		{
			var role = await userService.Context.Set<Role>()
				.AsNoTracking()
				.FirstOrDefaultAsync(r => r.Name == roleName, cn);
			if (role?.Id is null or 0)
				return;

			foreach (var userId in userIds.Where(id => id > 0).Distinct())
			{
				var user = await userService.GetById(userId);
				if (user == null)
					continue;

				user.RoleIds ??= [];
				user.Roles ??= [];
				if (user.RoleIds.Contains(role.Id.Value) || user.Roles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
					continue;

				user.RoleIds.Add(role.Id.Value);
				if (!user.Roles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
					user.Roles.Add(roleName);

				await userService.AddAndUpdateUserAsync(user, cn);
				if (onlineUserService == null)
					continue;

				try
				{
					await onlineUserService.RefreshUserRoleAccessesAsync(userId, cn);
				}
				catch (Exception)
				{
					// نقش در دیتابیس ذخیره شده؛ کش آنلاین ممکن است در دسترس نباشد.
				}
			}
		}

		public static async Task GrantPublishedListOnNotifyAsync(
			IUserService userService,
			IOnlineUserService? onlineUserService,
			ProcessDocument document,
			CancellationToken cn)
		{
			var userIds = document.NotificationRecipients
				.Select(x => x.UserId)
				.Where(id => id > 0)
				.ToList();

			if (document.AccessLevel == Entities.App.Bpm.Enums.ProcessDocumentAccessLevelEnum.Limit)
			{
				var orgIds = document.ExecuterOrgUnits.Select(x => x.OrgUnitId).Where(id => id > 0).Distinct().ToList();
				if (orgIds.Count > 0)
				{
					var extra = await userService.TableNoTracking
						.Where(u => u.OrgUnitId != null && orgIds.Contains(u.OrgUnitId.Value) && u.Id != null)
						.Select(u => u.Id!.Value)
						.ToListAsync(cn);
					userIds.AddRange(extra);
				}
			}

			await GrantAsync(
				userService,
				onlineUserService,
				ProcessDocumentNotificationConstants.RolePublishedList,
				userIds,
				cn);
		}
	}
}
