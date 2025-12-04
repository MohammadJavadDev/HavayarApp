using Common.Utilities;
using Data.Repositories;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Services.AccessServices;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
					  (IViewEngine)context.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine));

		

		var viewResult = viewEngine.FindView(context, ViewName, isMainPage: isMainPage);

		// 🔹 اگر مسیر کامل دادی، GetView
		if (!viewResult.Success)
		{
			viewResult = viewEngine.GetView(null, ViewName, isMainPage);
		}
		


		if (!viewResult.Success)
			throw new FileNotFoundException($"View '{ViewName}' not found.");

		using var sw = new StringWriter();
		var viewContext = new ViewContext(
		    context,
		    viewResult.View,
		    ViewData,
		    TempData,
		    sw,
		    new HtmlHelperOptions()
		);

		viewContext.ExecutingFilePath = null;
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
				node.RemoveAllChildren();
			}

			fullHtml = doc.DocumentNode.OuterHtml;
 

		}

			var response = context.HttpContext.Response;
		

		if (isPartialRequest)
		{
			var _accessMemoryStorage = (IAccessMemoryStorage)context.HttpContext.RequestServices.GetService(typeof(IAccessMemoryStorage));
			var viewName = context.HttpContext.Request.Path.Value;
			var action = _accessMemoryStorage?.GetAccessAction(viewName);
			response.ContentType = "application/json; charset=utf-8";
			var json = new
			{
				html = contentHtml,
				scripts = scripts,
				pageInfo = new
				{
					title =(action?.AccessController?.DisplayName + " - " + action?.DisplayName) ?? "تب جدید"
				}
			};
			await response.WriteAsync(json.JsonSerialize());
		}
		else
		{
			response.ContentType = "text/html; charset=utf-8";
			await response.WriteAsync(fullHtml);
		}
	}
}
