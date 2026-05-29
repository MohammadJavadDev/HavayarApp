 
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
 

namespace WebApp.Controllers
{
	public class HomeController(IWebHostEnvironment _webHostEnvironment) : ControllerBase
	{
	 

		//private string GetContentType(string path)
		//{
		//	var provider = new FileExtensionContentTypeProvider();
		//	if (!provider.TryGetContentType(path, out var contentType))
		//	{
		//		contentType = "application/octet-stream";
		//	}
		//	return contentType;
		//}

		//public class MediaResult : ContentResult
		//{
		//	public ContentResult JavaScriptResult(string script)
		//	{
		//		this.Content = MinifyScriptsJs(script);
		//		this.ContentType = "application/javascript";
		//		return this;
		//	}

		//	public ContentResult CssResult(string script)
		//	{
		//		this.Content = MinifyScriptsCss(script);
		//		this.ContentType = "text/css";
		//		return this;
		//	}

		//	private static string MinifyScriptsJs(string content)
		//	{
		//		var minifiedResult = Uglify.Js(content, new CodeSettings()
		//		{

		//			// فعال‌سازی مینیمایز (فشرده‌سازی کامل)
		//			MinifyCode = true,

		//			// حذف دستورات دیباگ مانند debugger و console
		//			StripDebugStatements = true,

		//			// حذف کدهای بلااستفاده (مانند توابع بدون فراخوانی)
		//			RemoveUnneededCode = true,

		//			// تغییر نام تمامی متغیرهای محلی به نام‌های کوتاه
		//			LocalRenaming = LocalRenaming.CrunchAll,

		//			// ارزیابی عبارات لغوی (تبدیل عبارات ثابت به مقدار نهایی)
		//			EvalLiteralExpressions = true,

		//			// جلوگیری از تغییر نام برخی از شناسه‌های سراسری (در صورت نیاز)
		//			NoAutoRenameList = "",

		//			// مرتب‌سازی مجدد اعلان‌های متغیرها بهینه (برای کاهش اندازه خروجی)
		//			ReorderScopeDeclarations = true,

		//			// تغییرات لازم برای جلوگیری از ایجاد مشکل در داخل رشته‌های متنی
		//			InlineSafeStrings = true,

		//			// حذف کامنت‌های غیر ضروری (حفظ کامنت‌های مهم را خاموش می‌کند)
		//			PreserveImportantComments = false,

		//			// جلوگیری از اضافه شدن کاراکترهای غیر ASCII به صورت کد Unicode (اختیاری)
		//			AlwaysEscapeNonAscii = false,

		//			// تنظیماتی برای حفظ سازگاری در موارد eval؛ در اکثر موارد استفاده از Ignore کافی است
		//			EvalTreatment = EvalTreatment.Ignore,

		//			// عدم تغییر نقل قول‌ها در اشیاء (در صورتی که نیازی به تغییر نباشد)
		//			QuoteObjectLiteralProperties = false
		//		});

		//		if (minifiedResult.HasErrors)
		//		{
		//			return content;
		//		}

		//		return minifiedResult.Code;
		//	}

		//	private static string MinifyScriptsCss(string content)
		//	{
		//		var minifiedResult = Uglify.Css(content);



		//		if (minifiedResult.HasErrors)
		//		{
		//			return content;
		//		}

		//		return minifiedResult.Code;
		//	}
		//}

		//[HttpGet("")]
		//[HttpGet("/system-page/{address}")]
		//[HttpGet("/{address?}")]
		//[HttpGet("/{address?}/{*param}")]


		//public async Task<IActionResult> Index(string? address, string? param)
		//{

		//	if (!string.IsNullOrWhiteSpace(address) && address == "demo")
		//	{
		//		return Content("", "text/html");
		//	}


		//	if (Request.Path.Value?.EndsWith(".map") ?? false)
		//	{
		//		return Content("", "text/css");
		//	}


		//	if ((Request.Path.Value?.EndsWith(".js") ?? false) || (Request.Path.Value?.EndsWith(".css") ?? false))
		//	{
		//		address = Request.Path.Value.Split("/").Last();
		//	}




		//	try
		//	{
		//		if (address is not null && address.EndsWith(".js"))
		//		{
		//			var jsContent = System.IO.File.ReadAllText(
		//			Path.Combine(_webHostEnvironment.WebRootPath, address)
		//			);

		//			return new MediaResult().JavaScriptResult(jsContent);
		//		}
		//	}
		//	catch (Exception e)
		//	{
		//		return NotFound(e.Message);
		//	}


		//	try
		//	{
		//		if (address is not null && address.EndsWith(".css"))
		//		{
		//			var cssContent = System.IO.File.ReadAllText(
		//			Path.Combine(_webHostEnvironment.WebRootPath, address)
		//			);

		//			return new MediaResult().CssResult(cssContent);
		//		}
		//	}
		//	catch (Exception e)
		//	{
		//		return NotFound(e.Message);
		//	}


		//	// Check if the request is for a static file
		//	var staticFileExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg", ".css", ".js", ".woff", ".woff2", ".ttf", ".eot" };
		//	if (staticFileExtensions.Any(ext => Request.Path.Value?.EndsWith(ext) ?? false))
		//	{
		//		var filePath = Path.Combine(_webHostEnvironment.WebRootPath, Request.Path.Value.TrimStart('/'));
		//		if (System.IO.File.Exists(filePath))
		//		{
		//			var contentType = GetContentType(filePath);
		//			var fileContent = await System.IO.File.ReadAllBytesAsync(filePath);
		//			return File(fileContent, contentType);
		//		}
		//		return NotFound();
		//	}


		//	if (Request.Path.Value?.StartsWith("/system-page") ?? false)
		//	{

		//		var queryParam = Request.Query.FirstOrDefault(c => c.Key.ToLower() == "id").Value;
		//		if (queryParam.Count > 0)
		//		{

		//			return await PageBuilder(address, Guid.Parse(queryParam));

		//		}

		//		return await PageBuilder(address, null);
		//	}

		//	var content = Uglify.Html(await _pageService.RenderPageRouting(address, param)).Code;
		//	return Content(content, "text/html");

		//}

		//[HttpPost("[action]/{address?}/{id?}")]
		//public async Task<object> Page(string? address, Guid? id)
		//{
		//	return await _pageService.RenderPageAsync(address, id);
		//}

		//[HttpPost("[action]/{address?}/{id?}")]
		//public async Task<object> PageDemo(string? address, Guid? id)
		//{
		//	return await _pageService.RenderPageDemoAsync(address, id);
		//}

		//public async Task<IActionResult> PageBuilder(string address, Guid? id)
		//{

		//	var content = await _pageService.RenderPageSystemHtmlAsync(address, id, ControllerContext);

		//	return Content(content, "text/html");

		//}

		//public class RequestData
		//{
		//	public string? id { get; set; }
		//}

		//[HttpPost("/system-page/{address}")]
		//public async Task<object> SystemPages(string address, [FromQuery] Guid? id)
		//{
		//	return await _pageService.RenderPageSystemAsync(address, id, ControllerContext);
		//}



		//[ApiResultFilterAttribute]
		//[HttpPost("[action]")]
		//public async Task<IActionResult> SaveJsFile(JsFile model, CancellationToken cn)
		//{
		//	var file = await _pageService.SaveJsFile(model, cn);
		//	return Ok(file);
		//}
		//[ApiResultFilterAttribute]
		//[HttpPost("[action]")]
		//public async Task<IActionResult> SaveCssFile(CssFile model, CancellationToken cn)
		//{
		//	var file = await _pageService.SaveCssFile(model, cn);
		//	return Ok(file);
		//}
		//[ApiResultFilterAttribute]
		//[HttpGet("[action]/{id}")]
		//public IActionResult GetCssFileBy(Guid id)
		//{
		//	var file = _pageService.GetCssFileById(id);
		//	return Ok(file);
		//}
		//[ApiResultFilterAttribute]
		//[HttpGet("[action]/{id}")]
		//public IActionResult GetJsFileBy(Guid id)
		//{
		//	var file = _pageService.GetJsFileById(id);

		//	return Ok(file);
		//}


		//[ApiResultFilterAttribute]
		//[HttpGet("[action]")]
		//public IActionResult GetCssFiles()
		//{
		//	var file = _pageService.GetCssFiles();
		//	return Ok(file);
		//}

		//[ApiResultFilterAttribute]
		//[HttpGet("[action]")]
		//public IActionResult GetJsFiles()
		//{
		//	var file = _pageService.GetJsFiles();
		//	return Ok(file);
		//}


		//[ApiResultFilterAttribute]

		//[HttpPost("[action]")]
		//public async Task<IActionResult> Publish(Page model, CancellationToken cn)
		//{

		//	model = await _pageService.PublishPage(model, cn);
		//	return Ok(model);
		//}

		//[ApiResultFilterAttribute]
		//[HttpPost("[action]")]
		//public async Task<IActionResult> Save(Page model, CancellationToken cn)
		//{

		//	model = await _pageService.SavePage(model, cn);
		//	return Ok(model);
		//}


		//[HttpPost("[action]")]
		//public string CreateDemo(Page model)
		//{
		//	var content = _pageService.CreateDemoPage(model);

		//	return content;
		//}

		//[HttpGet("[action]/{address}")]
		//public async Task<IActionResult> RenderDemo(string address)
		//{

		//	try
		//	{
		//		if (address is not null && address.EndsWith("-system-file.js"))
		//		{
		//			var jsContent = _service.Repository<JsFile>().TableNoTracking.FirstOrDefault(c => c.Id.ToString() == address.Replace("-system-file.js", ""))?.Content ?? "";

		//			return new MediaResult().JavaScriptResult(jsContent);
		//		}
		//	}
		//	catch (Exception e)
		//	{
		//		return NotFound(e.Message);
		//	}

		//	try
		//	{
		//		if (address is not null && address.EndsWith("-system-file.css"))
		//		{
		//			var cssContent = _service.Repository<CssFile>().TableNoTracking.FirstOrDefault(c => c.Id.ToString() == address.Replace("-system-file.css", ""))?.Content ?? "";

		//			return new MediaResult().CssResult(cssContent);
		//		}
		//	}
		//	catch (Exception e)
		//	{
		//		return NotFound(e.Message);
		//	}

		//	try
		//	{
		//		if (address is not null && address.EndsWith(".js"))
		//		{
		//			var jsContent = System.IO.File.ReadAllText(
		//			Path.Combine(_webHostEnvironment.WebRootPath, address)
		//			);

		//			return new MediaResult().JavaScriptResult(jsContent);
		//		}
		//	}
		//	catch (Exception e)
		//	{
		//		return NotFound(e.Message);
		//	}


		//	try
		//	{
		//		if (address is not null && address.EndsWith(".css"))
		//		{
		//			var cssContent = System.IO.File.ReadAllText(
		//			Path.Combine(_webHostEnvironment.WebRootPath, address)
		//			);

		//			return new MediaResult().CssResult(cssContent);
		//		}
		//	}
		//	catch (Exception e)
		//	{
		//		return NotFound(e.Message);
		//	}

		//	var content = await _pageService.RenderDemoPage(address);

		//	return Content(content, "text/html");
		//}


		//[HttpGet("/Demo/{address}/{*param}")]

		//public async Task<IActionResult> DemoSite(string? address, string? param)
		//{

		//	if (!string.IsNullOrWhiteSpace(address) && address == "demo")
		//	{
		//		return Content("", "text/html");
		//	}


		//	if (Request.Path.Value?.EndsWith(".map") ?? false)
		//	{
		//		return Content("", "text/css");
		//	}


		//	if ((Request.Path.Value?.EndsWith(".js") ?? false) || (Request.Path.Value?.EndsWith(".css") ?? false))
		//	{
		//		address = Request.Path.Value.Split("/").Last();
		//	}


		//	try
		//	{
		//		if (address is not null && address.EndsWith("-system-file.js"))
		//		{
		//			var jsContent =

		//				_pageService.LoadJsFile(address.Replace("-system-file.js", ""));


		//			Response.Headers["Cache-Control"] = "public, max-age=31536000";


		//			return new MediaResult().JavaScriptResult(jsContent);
		//		}
		//	}
		//	catch (Exception e)
		//	{
		//		return NotFound(e.Message);
		//	}

		//	try
		//	{
		//		if (address is not null && address.EndsWith("-system-file.css"))
		//		{
		//			var cssContent =
		//				_pageService.LoadCssFiles(address.Replace("-system-file.css", ""));
		//			Response.Headers["Cache-Control"] = "public, max-age=31536000";
		//			return new MediaResult().CssResult(cssContent);
		//		}
		//	}
		//	catch (Exception e)
		//	{
		//		return NotFound(e.Message);
		//	}
		//	try
		//	{
		//		if (address is not null && address.EndsWith(".js"))
		//		{
		//			var jsContent = System.IO.File.ReadAllText(
		//			Path.Combine(_webHostEnvironment.WebRootPath, address)
		//			);

		//			return new MediaResult().JavaScriptResult(jsContent);
		//		}
		//	}
		//	catch (Exception e)
		//	{
		//		return NotFound(e.Message);
		//	}


		//	try
		//	{
		//		if (address is not null && address.EndsWith(".css"))
		//		{
		//			var cssContent = System.IO.File.ReadAllText(
		//			Path.Combine(_webHostEnvironment.WebRootPath, address)
		//			);

		//			return new MediaResult().CssResult(cssContent);
		//		}
		//	}
		//	catch (Exception e)
		//	{
		//		return NotFound(e.Message);
		//	}


		//	// Check if the request is for a static file
		//	var staticFileExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg", ".css", ".js", ".woff", ".woff2", ".ttf", ".eot" };
		//	if (staticFileExtensions.Any(ext => Request.Path.Value?.EndsWith(ext) ?? false))
		//	{
		//		var filePath = Path.Combine(_webHostEnvironment.WebRootPath, Request.Path.Value.TrimStart('/'));
		//		if (System.IO.File.Exists(filePath))
		//		{
		//			var contentType = GetContentType(filePath);
		//			var fileContent = await System.IO.File.ReadAllBytesAsync(filePath);
		//			return File(fileContent, contentType);
		//		}
		//		return NotFound();
		//	}



		//	var content = Uglify.Html(await _pageService.RenderDemoWithLayout(address, null, param)).Code;

		//	return Content(content, "text/html");

		//}

		//[ApiResultFilterAttribute]
		//[HttpGet("[action]/{id}")]
		//public async Task<IActionResult> PageInformation(Guid id)
		//{
		//	try
		//	{
		//		var page = await _service.Repository<Page>().TableNoTracking
		//			.Include(c => c.SaveContent)
		//			.FirstOrDefaultAsync(c => c.Id == id);

		//		var cssFiles = await _service.Repository<CssFile>()
		//			.TableNoTracking
		//			.Select(c => new { c.Title, c.Id })
		//			.ToArrayAsync();

		//		var jsFiles = await _service.Repository<JsFile>()
		//			.TableNoTracking
		//			.Select(c => new { c.Title, c.Id })
		//			.ToArrayAsync();


		//		return Ok(new { page, cssFiles, jsFiles });
		//	}
		//	catch (Exception ex)
		//	{
		//		return StatusCode(500, $"Internal server error: {ex.Message}");
		//	}
		//}
		//[ApiResultFilterAttribute]
		//[HttpPost("[action]")]
		//public async Task<IActionResult> SavePageExtension(Page model, CancellationToken cn)
		//{

		//	return Ok(await _pageService.SavePageExtension(model, cn));
		//}


		//[ApiResultFilterAttribute]
		//[HttpGet("[action]")]
		//public IActionResult ListPage()
		//{
		//	var pages = _service.Repository<Page>().TableNoTracking
		//		.Select(c => new { c.Address, c.Id, c.Name })
		//		.ToArray();

		//	return Ok(pages);
		//}
	}
}
