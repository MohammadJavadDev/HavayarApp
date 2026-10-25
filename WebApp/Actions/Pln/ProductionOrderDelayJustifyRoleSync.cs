using Data.Contracts;
using Data.SystemAuth;
using Entities.App.Pln;
using Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Services.Auth;

namespace WebApp.Actions.Pln;

/// <summary>
/// نقش کارتابل توجیه را با فهرست کاربران هر عامل توقف همگام می‌کند.
/// </summary>
public static class ProductionOrderDelayJustifyRoleSync
{
	public const string RoleName = "Pln.ProductionOrderDelay.Justify";

	public static async Task SyncAsync(
		IUnitOfWork unitOfWork,
		IUserService userService,
		IOnlineUserService? onlineUserService,
		CancellationToken cn)
	{
		var role = await userService.Context.Set<Role>()
			.AsNoTracking()
			.FirstOrDefaultAsync(r => r.Name == RoleName, cn);
		if (role?.Id is null or 0)
			return;

		var roleId = role.Id.Value;
		var rows = await unitOfWork.Repository<ProductionOrderDelayResponsibleUser>()
			.TableNoTracking
			.ToListAsync(cn);

		var desired = rows
			.SelectMany(r => r.UserIds ?? [])
			.Where(id => id > 0)
			.ToHashSet();

		var currentHolders = await userService.GetUsersByRoleId(roleId) ?? [];
		var currentIds = currentHolders
			.Where(u => u.Id is > 0)
			.Select(u => u.Id!.Value)
			.ToHashSet();

		foreach (var userId in desired)
		{
			if (currentIds.Contains(userId))
				continue;
			if (await SetRoleAsync(userService, onlineUserService, userId, roleId, role.Name, grant: true, cn))
				currentIds.Add(userId);
		}

		foreach (var userId in currentIds.ToArray())
		{
			if (desired.Contains(userId))
				continue;
			await SetRoleAsync(userService, onlineUserService, userId, roleId, role.Name, grant: false, cn);
		}
	}

	private static async Task<bool> SetRoleAsync(
		IUserService userService,
		IOnlineUserService? onlineUserService,
		long userId,
		long roleId,
		string roleName,
		bool grant,
		CancellationToken cn)
	{
		var user = await userService.GetById(userId);
		if (user?.Id is null or 0)
			return false;

		var roleIds = user.RoleIds?.ToList() ?? [];
		var roles = user.Roles?.ToList() ?? [];
		var hasRole = roleIds.Contains(roleId)
			|| roles.Contains(roleName, StringComparer.OrdinalIgnoreCase);

		if (grant && hasRole)
			return true;
		if (!grant && !hasRole)
			return true;

		if (grant)
		{
			if (!roleIds.Contains(roleId))
				roleIds.Add(roleId);
			if (!roles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
				roles.Add(roleName);
		}
		else
		{
			roleIds.Remove(roleId);
			roles = roles
				.Where(r => !string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase))
				.ToList();
		}

		await userService.AddAndUpdateUserAsync(new User
		{
			Id = user.Id,
			Name = user.Name,
			Username = user.Username,
			Password = null,
			Email = user.Email,
			NameFa = user.NameFa,
			PartyId = user.PartyId,
			ProfileUrl = user.ProfileUrl,
			AuthorizationType = user.AuthorizationType,
			Roles = roles,
			RoleIds = roleIds
		}, cn);

		if (onlineUserService == null)
			return true;

		try
		{
			await onlineUserService.RefreshUserRoleAccessesAsync(userId, cn);
		}
		catch (Exception)
		{
			// نقش در دیتابیس ذخیره شده؛ کش آنلاین ممکن است در دسترس نباشد.
		}

		return true;
	}
}
