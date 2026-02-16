using Data.SystemAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace WebFramework.Page;
public class BaseController : Controller
{
	protected ISdk? _sdk;
	protected ISdk sdk => _sdk ??= HttpContext.RequestServices.GetRequiredService<ISdk>();

	#region Convenience Properties
	protected long? CurrentUserId => sdk?.CurrentUser?.Id;
	protected string? CurrentUserName => sdk?.CurrentUser?.Username;
	protected string? CurrentUserFullName => sdk?.CurrentUser?.FullName;
	protected long? CurrentOrganizationUnitId => sdk?.CurrentUser?.OrganizationUnitId;
	protected string? CurrentUserFullNameFn => sdk?.CurrentUser?.FullNameFn;
	protected bool IsAuthenticated => sdk?.Authenticated ?? false;
	protected bool IsAdministrator => sdk?.IsAdministrator ?? false;
	protected string? CurrentUserEmail => sdk?.CurrentUser.Email;
	#endregion

	#region Role Checking Methods - Delegating to SDK
	protected bool HasRole(string roleName) => sdk?.HasRole(roleName) ?? false;
	protected bool HasRole(long roleId) => sdk?.HasRole(roleId) ?? false;
	protected bool HasAnyRole(params string[] roleNames) => sdk?.HasAnyRole(roleNames) ?? false;
	protected bool HasAllRoles(params string[] roleNames) => sdk?.HasAllRoles(roleNames) ?? false;
	#endregion

	#region View Methods
	protected new IActionResult View(string viewName, object model = null)
	{
		return new ProcessedViewResult(viewName, model, ViewData, TempData);
	}

	protected IActionResult View(object model)
	{
		return new ProcessedViewResult(null, model, ViewData, TempData);
	}

	protected new IActionResult View()
	{
		return new ProcessedViewResult(null, null, ViewData, TempData);
	}
	#endregion
}