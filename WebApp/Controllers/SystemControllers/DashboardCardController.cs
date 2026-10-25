using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Data.Services.QueryBuilderServices;
using Data.SystemAuth;
using Entities.Auth;
using Entities.Base;
using Entities.Base.Menu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Services.AccessServices;
using System.Text.Json;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.SystemControllers;

[Route("Panel/System/DashboardCard")]
[ApiController]
[ApiResultFilter]
[Authorize("AuthenticatedUser")]
[ControllerInfo("کارت داشبورد")]
public class DashboardCardController(
	IUnitOfWork unitOfWork,
	IDistributedCache cache,
	IAccessMemoryStorage accessMemoryStorage,
	IQueryService queryService,
	IMenuBuilderService menuBuilderService) : BaseController
{
	private static readonly TimeSpan CardsCacheTtl = TimeSpan.FromHours(1);
	private static readonly TimeSpan CountCacheTtl = TimeSpan.FromMinutes(3);

	private static readonly HashSet<string> AllowedIcons = new(StringComparer.OrdinalIgnoreCase)
	{
		"fas fa-inbox",
		"fas fa-tasks",
		"fas fa-clipboard-list",
		"fas fa-users",
		"fas fa-file-alt",
		"fas fa-shopping-cart",
		"fas fa-wrench",
		"fas fa-chart-bar",
		"fas fa-bell",
		"fas fa-folder-open",
		"fas fa-calendar-alt",
		"fas fa-truck",
		"fas fa-cogs",
		"fas fa-user-check",
		"fas fa-list-ul",
		"fas fa-briefcase"
	};

	private static readonly HashSet<string> AllowedColors = new(StringComparer.OrdinalIgnoreCase)
	{
		"primary",
		"success",
		"danger",
		"warning",
		"info",
		"dark"
	};

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	[HttpGet("cards")]
	public async Task<IActionResult> GetCards(CancellationToken ct)
	{
		var userId = CurrentUserId;
		if (userId == null)
			return Unauthorized();

		var cards = await LoadAccessibleCardsAsync(userId.Value, ct);
		return Ok(cards);
	}

	[HttpGet("lists")]
	public IActionResult GetLists()
	{
		if (CurrentUserId == null)
			return Unauthorized();

		var profileEntities = GetEntitiesWithAccessibleProfiles();
		var menuPlacement = BuildMenuPlacement();

		var entries = GetAccessibleLists()
			.Where(l => !string.IsNullOrWhiteSpace(l.EntityName)
			            && (profileEntities == null || profileEntities.Contains(l.EntityName!)))
			.GroupBy(l => l.Path, StringComparer.OrdinalIgnoreCase)
			.Select(g => g.First())
			.Select(l =>
			{
				var fallbackTitle = l.ControllerDisplayName ?? l.ControllerName ?? l.Path;
				if (menuPlacement.TryGetValue(l.Path, out var place))
					return new { Info = l, Group = place.Group, Title = place.Title, Order = place.Order };
				return new { Info = l, Group = OtherGroupTitle, Title = fallbackTitle, Order = int.MaxValue };
			})
			.ToList();

		var lists = entries
			.GroupBy(e => e.Group, StringComparer.OrdinalIgnoreCase)
			.OrderBy(g => g.Key == OtherGroupTitle ? 1 : 0)
			.ThenBy(g => g.Min(e => e.Order))
			.Select(g => new
			{
				groupTitle = g.Key,
				items = g.OrderBy(e => e.Order)
					.ThenBy(e => e.Title, StringComparer.CurrentCulture)
					.Select(e => new
					{
						path = e.Info.Path,
						entityName = e.Info.EntityName,
						title = e.Title,
						controllerName = e.Info.ControllerName
					}).ToList()
			})
			.ToList();

		return Ok(lists);
	}

	private const string OtherGroupTitle = "سایر صفحات";

	/// <summary>
	/// Entities for which the current user has at least one data profile (null = no restriction, e.g. administrator).
	/// </summary>
	private HashSet<string>? GetEntitiesWithAccessibleProfiles()
	{
		if (IsAdministrator)
			return null;

		return (sdk.CurrentUser?.RoleAccess ?? [])
			.Where(c => c.ActionAccessType == ActionAccessType.DataProfile
				&& !string.IsNullOrWhiteSpace(c.EntityName)
				&& c.RowId != null)
			.Select(c => c.EntityName!)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Maps a normalized page path to the group/title it has in the user's menu(s),
	/// so the add-card dialog is grouped exactly like the sidebar menu.
	/// The first menu (the one shown by default) wins when a page appears in several menus.
	/// </summary>
	private Dictionary<string, MenuPlacement> BuildMenuPlacement()
	{
		var result = new Dictionary<string, MenuPlacement>(StringComparer.OrdinalIgnoreCase);
		var order = 0;

		List<SystemMenu> menus;
		try
		{
			menus = menuBuilderService.GetMenuByRole(sdk) ?? [];
		}
		catch
		{
			return result;
		}

		foreach (var menu in menus)
		{
			if (menu.Id == null)
				continue;

			List<SystemMenuItem> items;
			try
			{
				items = menuBuilderService.GetMenuItems(menu.Id, sdk) ?? [];
			}
			catch
			{
				continue;
			}

			Walk(items, [], menu.Title?.Trim() ?? string.Empty);
		}

		return result;

		void Walk(List<SystemMenuItem>? nodes, List<string> ancestors, string menuTitle)
		{
			if (nodes == null)
				return;

			foreach (var node in nodes)
			{
				if (node == null)
					continue;

				var text = (node.Text ?? string.Empty).Trim();
				if (node.Children is { Count: > 0 })
				{
					var next = new List<string>(ancestors);
					if (text.Length > 0)
						next.Add(text);
					Walk(node.Children, next, menuTitle);
					continue;
				}

				if (string.IsNullOrWhiteSpace(node.Path) || node.Path == "#" || text.Length == 0)
					continue;

				var path = NormalizePath(node.Path);
				var q = path.IndexOf('?');
				if (q >= 0)
					path = path[..q];
				if (path.Length > 1 && path.EndsWith('/'))
					path = path[..^1];

				if (result.ContainsKey(path))
					continue;

				var group = ancestors.Count > 0
					? string.Join(" › ", ancestors)
					: (menuTitle.Length > 0 ? menuTitle : OtherGroupTitle);

				result[path] = new MenuPlacement(group, text, order++);
			}
		}
	}

	private sealed record MenuPlacement(string Group, string Title, int Order);

	[HttpGet("profiles")]
	public IActionResult GetProfiles([FromQuery] string entityName)
	{
		if (CurrentUserId == null)
			return Unauthorized();

		if (string.IsNullOrWhiteSpace(entityName))
			return BadRequest("نام موجودیت الزامی است");

		var profiles = GetAccessibleProfiles(entityName.Trim())
			.Select(p => new { id = p.Id, title = p.Title, name = p.Name })
			.ToList();

		return Ok(profiles);
	}

	[HttpPost("save")]
	public async Task<IActionResult> Save([FromBody] DashboardCardSaveRequest request, CancellationToken ct)
	{
		var userId = CurrentUserId;
		if (userId == null)
			return Unauthorized();

		if (request == null)
			return BadRequest("داده نامعتبر است");

		if (request.Slot is < 1 or > 4)
			return BadRequest("جایگاه باید بین ۱ تا ۴ باشد");

		var title = (request.Title ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
			return BadRequest("عنوان کارت نامعتبر است");

		var iconClass = (request.IconClass ?? string.Empty).Trim();
		if (!AllowedIcons.Contains(iconClass))
			return BadRequest("آیکن مجاز نیست");

		var colorClass = (request.ColorClass ?? string.Empty).Trim();
		if (!AllowedColors.Contains(colorClass))
			return BadRequest("رنگ مجاز نیست");

		var listPath = NormalizePath(request.ListPath);
		if (string.IsNullOrWhiteSpace(listPath))
			return BadRequest("مسیر لیست الزامی است");

		var accessibleList = GetAccessibleLists()
			.FirstOrDefault(l => string.Equals(l.Path, listPath, StringComparison.OrdinalIgnoreCase));
		if (accessibleList == null)
			return BadRequest("دسترسی به این لیست وجود ندارد");

		if (string.IsNullOrWhiteSpace(accessibleList.EntityName))
			return BadRequest("موجودیت این لیست مشخص نیست");

		var profile = GetAccessibleProfiles(accessibleList.EntityName)
			.FirstOrDefault(p => p.Id == request.SavedQueryId);
		if (profile == null || profile.Id == null)
			return BadRequest("دسترسی به این نمایه وجود ندارد");

		var profileId = profile.Id.Value;
		var repo = unitOfWork.Repository<UserDashboardCard>();
		var existing = await repo.Table
			.FirstOrDefaultAsync(c => c.UserId == userId.Value && c.Slot == request.Slot, ct);

		if (existing == null)
		{
			existing = new UserDashboardCard
			{
				UserId = userId.Value,
				Slot = request.Slot,
				SavedQueryId = profileId,
				Title = title,
				IconClass = iconClass,
				ColorClass = colorClass,
				ListPath = accessibleList.Path,
				IsActive = IsActiveEnum.Active
			};
			await repo.AddAsync(existing, ct);
		}
		else
		{
			existing.SavedQueryId = profileId;
			existing.Title = title;
			existing.IconClass = iconClass;
			existing.ColorClass = colorClass;
			existing.ListPath = accessibleList.Path;
			existing.IsActive = IsActiveEnum.Active;
			await repo.UpdateAsync(existing, ct);
		}

		await InvalidateCardsCacheAsync(userId.Value, ct);
		await InvalidateCountCacheAsync(userId.Value, profileId, ct);

		return Ok(new
		{
			id = existing.Id,
			slot = existing.Slot,
			savedQueryId = existing.SavedQueryId,
			title = existing.Title,
			iconClass = existing.IconClass,
			colorClass = existing.ColorClass,
			listPath = existing.ListPath
		});
	}

	[HttpPost("delete")]
	public async Task<IActionResult> Delete([FromBody] DashboardCardDeleteRequest request, CancellationToken ct)
	{
		var userId = CurrentUserId;
		if (userId == null)
			return Unauthorized();

		if (request == null || request.Slot is < 1 or > 4)
			return BadRequest("جایگاه نامعتبر است");

		var repo = unitOfWork.Repository<UserDashboardCard>();
		var existing = await repo.Table
			.FirstOrDefaultAsync(c => c.UserId == userId.Value && c.Slot == request.Slot, ct);

		if (existing != null)
		{
			var profileId = existing.SavedQueryId;
			await repo.DeleteAsync(existing, ct);
			await InvalidateCardsCacheAsync(userId.Value, ct);
			await InvalidateCountCacheAsync(userId.Value, profileId, ct);
		}
		else
		{
			await InvalidateCardsCacheAsync(userId.Value, ct);
		}

		return Ok(new { slot = request.Slot });
	}

	[HttpPost("counts")]
	public async Task<IActionResult> GetCounts([FromBody] DashboardCardCountsRequest request, CancellationToken ct)
	{
		var userId = CurrentUserId;
		if (userId == null)
			return Unauthorized();

		var refresh = request?.Refresh == true;
		var cards = await LoadAccessibleCardsAsync(userId.Value, ct);
		var allowedIds = cards.Select(c => c.SavedQueryId).ToHashSet();

		var requestedIds = (request?.ProfileIds ?? [])
			.Where(id => allowedIds.Contains(id))
			.Distinct()
			.ToList();

		if (requestedIds.Count == 0)
			requestedIds = allowedIds.ToList();

		var userIdStr = userId.Value.ToString();
		var username = CurrentUserName ?? string.Empty;
		var result = new Dictionary<long, int>();

		foreach (var profileId in requestedIds)
		{
			var countKey = CountCacheKey(userId.Value, profileId);
			if (!refresh)
			{
				var cached = await cache.GetStringAsync(countKey, ct);
				if (int.TryParse(cached, out var cachedCount))
				{
					result[profileId] = cachedCount;
					continue;
				}
			}

			try
			{
				var count = await queryService.ExecuteReportCountAsync(profileId, userIdStr, username);
				result[profileId] = count;
				await cache.SetStringAsync(
					countKey,
					count.ToString(),
					new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CountCacheTtl },
					ct);
			}
			catch
			{
				result[profileId] = -1;
			}
		}

		return Ok(result);
	}

	private async Task<List<DashboardCardDto>> LoadAccessibleCardsAsync(long userId, CancellationToken ct)
	{
		var cacheKey = CardsCacheKey(userId);
		var cached = await cache.GetStringAsync(cacheKey, ct);
		List<DashboardCardDto>? cards = null;
		if (!string.IsNullOrEmpty(cached))
		{
			try
			{
				cards = JsonSerializer.Deserialize<List<DashboardCardDto>>(cached, JsonOptions);
			}
			catch
			{
				cards = null;
			}
		}

		if (cards == null)
		{
			var rows = await unitOfWork.Repository<UserDashboardCard>().TableNoTracking
				.Where(c => c.UserId == userId && c.IsActive == IsActiveEnum.Active)
				.OrderBy(c => c.Slot)
				.Take(4)
				.ToListAsync(ct);

			cards = rows.Select(c => new DashboardCardDto
			{
				Id = c.Id ?? 0,
				Slot = c.Slot,
				SavedQueryId = c.SavedQueryId,
				Title = c.Title,
				IconClass = c.IconClass,
				ColorClass = c.ColorClass,
				ListPath = NormalizePath(c.ListPath)
			}).ToList();

			await cache.SetStringAsync(
				cacheKey,
				JsonSerializer.Serialize(cards, JsonOptions),
				new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CardsCacheTtl },
				ct);
		}

		var accessibleLists = GetAccessibleLists();
		var listByPath = accessibleLists
			.GroupBy(l => l.Path, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

		var filtered = new List<DashboardCardDto>();
		foreach (var card in cards.Where(c => c.Slot is >= 1 and <= 4).OrderBy(c => c.Slot))
		{
			if (!listByPath.TryGetValue(NormalizePath(card.ListPath), out var list)
			    || string.IsNullOrWhiteSpace(list.EntityName))
				continue;

			var profileOk = GetAccessibleProfiles(list.EntityName)
				.Any(p => p.Id == card.SavedQueryId);
			if (!profileOk)
				continue;

			card.ListPath = list.Path;
			filtered.Add(card);
		}

		return filtered;
	}

	private List<AccessibleListInfo> GetAccessibleLists()
	{
		var controllers = accessMemoryStorage.GetAllAccessControllers() ?? [];
		var items = new List<AccessibleListInfo>();

		foreach (var ctrl in controllers)
		{
			if (ctrl.Actions == null)
				continue;

			foreach (var action in ctrl.Actions)
			{
				if (action.ActionAccessType != ActionAccessType.View)
					continue;
				if (action.ActionAccessItemType != ActionAccessItemType.List)
					continue;
				if (!string.Equals(action.DisplayName, "لیست اطلاعات", StringComparison.Ordinal))
					continue;

				var path = ResolveControllerToken(action.Path, ctrl.Name);
				path = NormalizePath(path);
				if (string.IsNullOrWhiteSpace(path))
					continue;

				items.Add(new AccessibleListInfo
				{
					Path = path,
					ControllerName = ctrl.Name,
					ControllerDisplayName = ctrl.DisplayName,
					EntityName = ctrl.EntityType?.FullName
				});
			}
		}

		if (IsAdministrator)
			return items;

		var roleAccess = sdk.CurrentUser?.RoleAccess ?? [];
		return items.Where(item =>
			roleAccess.Any(ra =>
				ra.ActionAccessType == ActionAccessType.View
				&& ra.ActionAccessItemType == ActionAccessItemType.List
				&& PathsMatch(item.Path, ra.Path, item.ControllerName)))
			.ToList();
	}

	private List<SavedQuery> GetAccessibleProfiles(string entityName)
	{
		if (IsAdministrator)
			return queryService.GetDataTableProfileListByEntityName(entityName) ?? [];

		var profileIds = (sdk.CurrentUser?.RoleAccess ?? [])
			.Where(c => c.ActionAccessType == ActionAccessType.DataProfile
				&& c.EntityName != null
				&& c.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase)
				&& c.RowId != null)
			.Select(c => c.RowId)
			.ToList();

		return queryService.GetDataTableProfileById(profileIds, entityName) ?? [];
	}

	private static bool PathsMatch(string resolvedPath, string? rolePath, string? controllerName)
	{
		if (string.IsNullOrWhiteSpace(rolePath))
			return false;

		var resolvedRolePath = NormalizePath(ResolveControllerToken(rolePath, controllerName));
		if (string.Equals(resolvedPath, resolvedRolePath, StringComparison.OrdinalIgnoreCase))
			return true;

		return RoutePatternHelper.IsMatch(resolvedPath, NormalizePath(rolePath));
	}

	private static string ResolveControllerToken(string? path, string? controllerName)
	{
		if (string.IsNullOrWhiteSpace(path))
			return string.Empty;

		var resolved = path;
		if (resolved.Contains("[controller]", StringComparison.OrdinalIgnoreCase)
		    && !string.IsNullOrWhiteSpace(controllerName))
		{
			var name = controllerName;
			if (name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
				name = name[..^"Controller".Length];
			resolved = resolved.Replace("[controller]", name.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
		}

		return resolved;
	}

	private static string NormalizePath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return string.Empty;

		var p = path.Trim().Replace('\\', '/');
		if (!p.StartsWith('/'))
			p = "/" + p;
		while (p.Contains("//", StringComparison.Ordinal))
			p = p.Replace("//", "/", StringComparison.Ordinal);
		return p.ToLowerInvariant();
	}

	private static string CardsCacheKey(long userId) => $"dashboard:cards:{userId}";
	private static string CountCacheKey(long userId, long profileId) => $"dashboard:count:{userId}:{profileId}";

	private Task InvalidateCardsCacheAsync(long userId, CancellationToken ct)
		=> cache.RemoveAsync(CardsCacheKey(userId), ct);

	private Task InvalidateCountCacheAsync(long userId, long profileId, CancellationToken ct)
		=> cache.RemoveAsync(CountCacheKey(userId, profileId), ct);

	private sealed class AccessibleListInfo
	{
		public string Path { get; set; } = string.Empty;
		public string? ControllerName { get; set; }
		public string? ControllerDisplayName { get; set; }
		public string? EntityName { get; set; }
	}
}

public class DashboardCardDto
{
	public long Id { get; set; }
	public int Slot { get; set; }
	public long SavedQueryId { get; set; }
	public string Title { get; set; } = string.Empty;
	public string IconClass { get; set; } = string.Empty;
	public string ColorClass { get; set; } = string.Empty;
	public string ListPath { get; set; } = string.Empty;
}

public class DashboardCardSaveRequest
{
	public int Slot { get; set; }
	public long SavedQueryId { get; set; }
	public string? Title { get; set; }
	public string? IconClass { get; set; }
	public string? ColorClass { get; set; }
	public string? ListPath { get; set; }
}

public class DashboardCardDeleteRequest
{
	public int Slot { get; set; }
}

public class DashboardCardCountsRequest
{
	public List<long>? ProfileIds { get; set; }
	public bool Refresh { get; set; }
}
