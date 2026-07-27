using Common;
using Common.Utilities;
using Data.Services;
using Entities.Base;
using Entities.Base.System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.DependencyInjection;
using WebFramework.Abstractions;

namespace WebFramework.Services;

public interface IPageBuilderViewRenderService
{
	Task<ViewEngineResult> ResolveViewAsync(
		ActionContext context,
		string viewPath,
		bool isMainPage,
		CancellationToken cancellationToken = default);

	string GetApplicationRelativePath(string viewPath);
}

public sealed class PageBuilderViewRenderService(
	IPageBuilderService pageBuilderService,
	IPageBuilderCache pageBuilderCache,
	IEnvironmentService environmentService) : IPageBuilderViewRenderService, IScopedDependency
{
	public async Task<ViewEngineResult> ResolveViewAsync(
		ActionContext context,
		string viewPath,
		bool isMainPage,
		CancellationToken cancellationToken = default)
	{
		var normalized = PageBuilderPathHelper.NormalizeViewPath(viewPath);
		if (string.IsNullOrWhiteSpace(normalized))
			return ViewEngineResult.NotFound(normalized, []);

		var razorEngine = context.HttpContext.RequestServices.GetRequiredService<IRazorViewEngine>();

	

		// 2) Registered Razor file providers fallback (physical app views and Razor class libraries)
		var providerResult = TryResolveExistingView(razorEngine, normalized, isMainPage);
		if (providerResult.Success)
			return providerResult;

		// 3) Physical files fallback for direct disk paths that are not registered as app-relative views
		var physicalPath = PageBuilderPathHelper.ToPhysicalViewPath(environmentService.ContentRootPath, normalized);
		if (File.Exists(physicalPath))
		{
			var physicalResult = TryResolveExistingView(razorEngine, normalized, isMainPage);
			if (physicalResult.Success)
				return physicalResult;
		}

		// 4) Conventional view name (e.g. "List")
		if (!normalized.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
		{
			var actionName = Path.GetFileNameWithoutExtension(normalized);
			if (!string.IsNullOrWhiteSpace(actionName))
			{
				var findResult = SafeFindView(razorEngine, context, actionName, isMainPage);
				if (findResult.Success)
					return findResult;
			}
		}

		// 1) Database first
		var page = await pageBuilderService.GetByPathAsync(normalized, cancellationToken);
		if (page != null && page.IsActive == IsActiveEnum.Active && !string.IsNullOrWhiteSpace(page.Content))
		{
			WarmCache(normalized, page);
			var databaseResult = TryResolveExistingView(razorEngine, normalized, isMainPage);
			if (databaseResult.Success)
				return databaseResult;
		}

		return ViewEngineResult.NotFound(normalized, PageBuilderPathHelper.GetViewLookupPaths(normalized).ToArray());
	}

	public string GetApplicationRelativePath(string viewPath)
	{
		var normalized = PageBuilderPathHelper.NormalizeViewPath(viewPath);
		return string.IsNullOrWhiteSpace(normalized) ? string.Empty : "~/" + normalized;
	}

	private void WarmCache(string normalizedPath, PageDefinition page)
	{
		var lastModified = page.ModifiedDateMiladiDateTime ?? page.CreatedOnMiladiDateTime ?? DateTime.Now;
		pageBuilderCache.Set(normalizedPath, page.Content, new DateTimeOffset(lastModified));
	}

	private ViewEngineResult TryResolveExistingView(IRazorViewEngine razorEngine, string normalizedPath, bool isMainPage)
	{
		foreach (var lookupPath in PageBuilderPathHelper.GetViewLookupPaths(normalizedPath))
		{
			if (string.IsNullOrWhiteSpace(lookupPath))
				continue;

			var result = SafeGetView(razorEngine, lookupPath, isMainPage);
			if (result.Success)
				return result;
		}

		return ViewEngineResult.NotFound(normalizedPath, PageBuilderPathHelper.GetViewLookupPaths(normalizedPath).ToArray());
	}

	private static ViewEngineResult SafeGetView(IRazorViewEngine razorEngine, string viewPath, bool isMainPage)
	{
		if (string.IsNullOrWhiteSpace(viewPath))
			return ViewEngineResult.NotFound(viewPath, []);

		try
		{
			return razorEngine.GetView(null, viewPath, isMainPage);
		}
		catch (ArgumentException)
		{
			return ViewEngineResult.NotFound(viewPath, []);
		}
	}

	private static ViewEngineResult SafeFindView(
		IRazorViewEngine razorEngine,
		ActionContext context,
		string viewName,
		bool isMainPage)
	{
		if (string.IsNullOrWhiteSpace(viewName))
			return ViewEngineResult.NotFound(viewName, []);

		try
		{
			return razorEngine.FindView(context, viewName, isMainPage);
		}
		catch (ArgumentException)
		{
			return ViewEngineResult.NotFound(viewName, []);
		}
	}
}
