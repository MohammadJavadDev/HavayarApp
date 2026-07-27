using Common.Utilities;
using Data.Repositories;
using WebFramework.Services;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Services.AccessServices;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using System.Security.Cryptography;

public class ProcessedViewResult : ViewResult
{
	public ProcessedViewResult() {}

	public ProcessedViewResult(string viewName, object model, ViewDataDictionary viewData, ITempDataDictionary tempData)
	{
		ViewName = viewName;
		ViewData = viewData;
		TempData = tempData;
		if (model != null) ViewData.Model = model;
	}

	public override async Task ExecuteResultAsync(ActionContext context)
	{
		if (context == null)
			throw new ArgumentNullException(nameof(context));

		var headers = context.HttpContext.Request.Headers;
		bool isPartialRequest = (headers.ContainsKey("X-Partial-Request") && headers["X-Partial-Request"] == "true") ||
						    (headers.ContainsKey("X-Requested-With") && headers["X-Requested-With"] == "XMLHttpRequest");

		// کنترل اتوماتیک Layout
		ViewData["RenderLayout"] = !isPartialRequest;
		var isMainPage = !isPartialRequest;

		var viewEngine = ViewEngine ??
					  (IViewEngine)context.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();

		var viewRenderService = context.HttpContext.RequestServices.GetRequiredService<IPageBuilderViewRenderService>();

		var resolvedViewName = ResolveViewName(context);
		if (string.IsNullOrWhiteSpace(resolvedViewName))
			throw new InvalidOperationException("View name could not be determined for the current request.");

		var viewResult = await viewRenderService.ResolveViewAsync(
			context,
			resolvedViewName,
			isMainPage,
			context.HttpContext.RequestAborted);

		if (!viewResult.Success)
			throw new FileNotFoundException(
				$"View '{PageBuilderPathHelper.NormalizeViewPath(resolvedViewName)}' not found in Page Builder database or on disk.");

		using var sw = new StringWriter();
		var executingFilePath = viewResult.View?.Path;
		if (string.IsNullOrWhiteSpace(executingFilePath))
			executingFilePath = viewRenderService.GetApplicationRelativePath(resolvedViewName);

		var viewContext = new ViewContext(
		    context,
		    viewResult.View,
		    ViewData,
		    TempData,
		    sw,
		    new HtmlHelperOptions()
		);

		viewContext.ExecutingFilePath = executingFilePath;
		await viewResult.View.RenderAsync(viewContext);

		var fullHtml = sw.ToString();
		var scripts = new List<string>();

	

		string contentHtml = fullHtml;

		// 🔹 اگر Partial، فقط Container اصلی یا body
		if (isPartialRequest)
		{
			// 🔹 جداسازی Scriptها
			var scriptRegex = new Regex(@"<script>([\s\S]*?)<\/script>", RegexOptions.IgnoreCase);
			contentHtml = scriptRegex.Replace(fullHtml, match =>
			{
				var script = $"(function (page,$$,self) {{ {match.Groups[1].Value} }})(page,$$,self)";
				scripts.Add(script);
				return "";
			});

		
		}
		else
		{

			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(fullHtml);

			// انتخاب div با id = pagesContainer
			var node = doc.DocumentNode.SelectSingleNode("//div[@id='pagesContainer']");
			if (node != null)
			{
				// همه فرزندان داخل div رو پاک می‌کنیم
				//node.RemoveAllChildren();
			}

			fullHtml = doc.DocumentNode.OuterHtml;
 

		}

			var response = context.HttpContext.Response;
		

		if (isPartialRequest)
		{
			var _accessMemoryStorage = (IAccessMemoryStorage)context.HttpContext.RequestServices.GetService(typeof(IAccessMemoryStorage));
			var requestPath = context.HttpContext.Request.Path.Value;
			var action = _accessMemoryStorage?.GetAccessAction(requestPath);
			response.ContentType = "application/json; charset=utf-8";
			var viewTitle = (action?.AccessController?.DisplayName + " - " + action?.DisplayName) ?? "تب جدید";
			if (requestPath == "/Authenticate/Forbidden")
			{
				viewTitle = "عدم دسترسی";
			}

			// Obfuscate payload to avoid obvious wire format inspection (not security).
			var salt = new byte[] { 13, 71, 99, 201, 54, 11, 222, 39 };
			byte keyByte;
			using (var rng = RandomNumberGenerator.Create())
			{
				var keyBuf = new byte[1];
				rng.GetBytes(keyBuf);
				keyByte = keyBuf[0];
			}

			var payloadObj = new
			{
				h = contentHtml ?? string.Empty,                 // html
				s = scripts ?? new List<string>(),               // scripts
				p = new { t = viewTitle }                        // page info
			};

			var payloadJson = payloadObj.JsonSerialize();
			var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

			for (int i = 0; i < payloadBytes.Length; i++)
			{
				payloadBytes[i] = (byte)((payloadBytes[i] ^ keyByte) ^ salt[i % salt.Length]);
			}

			var blob = Convert.ToBase64String(payloadBytes);

			var json = new
			{
				v = 1,
				p = blob,
				q = (byte)(keyByte ^ 0xA5)
			};

			await response.WriteAsync(json.JsonSerialize());
		}
		else
		{
			response.ContentType = "text/html; charset=utf-8";
			await response.WriteAsync(fullHtml);
		}
	}

	private string ResolveViewName(ActionContext context)
	{
		if (!string.IsNullOrWhiteSpace(ViewName))
			return ViewName!;

		var panelViewPath = ResolvePanelViewPathFromRequest(context.HttpContext.Request.Path);
		if (!string.IsNullOrWhiteSpace(panelViewPath))
			return panelViewPath;

		var actionName = context.RouteData.Values["action"]?.ToString();
		if (!string.IsNullOrWhiteSpace(actionName))
			return actionName;

		throw new InvalidOperationException("View name could not be determined for the current request.");
	}

	private static string? ResolvePanelViewPathFromRequest(PathString requestPath)
	{
		if (!requestPath.HasValue)
			return null;

		var segments = requestPath.Value!.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (segments.Length < 4)
			return null;

		if (!segments[0].Equals("Panel", StringComparison.OrdinalIgnoreCase))
			return null;

		var action = segments[^1];
		var folder = string.Join("/", segments[..^1]);
		return PageBuilderPathHelper.ToFileProviderSubpath($"Views/{folder}/{action}.cshtml");
	}
}
