using Azure.Core;
using Common.System;
using Data.SystemAuth;
using Entities.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace WebFramework.Page
{
	public class BaseController() : Controller
	{

		// Property برای دسترسی سریع به ISdk
		protected ISdk sdk =>
		    HttpContext.RequestServices.GetRequiredService<ISdk>();

		// Convenience Properties
		protected long CurrentUserId => sdk.CurrentUser.Id;
		protected string? CurrentUserName => sdk.CurrentUser.Username;
		protected string? CurrentUserFullName => sdk.CurrentUser.FullName;
		protected IEnumerable<string> CurrentUserRoles => sdk.CurrentUser.Roles;
		protected bool IsAuthenticated => sdk.Authenticated;
		protected new IActionResult View(string viewName, object model = null)
		{
			return new ProcessedViewResult(viewName, model, ViewData, TempData);
		}
	}
}
