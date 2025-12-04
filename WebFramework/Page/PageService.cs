using Common.Utilities;
using Data.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NUglify.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebFramework.Page
{
//	public class PageService(
//	IUnitOfWork _service,
//	IRazorViewEngine _viewEngine,
//	ITempDataProvider _tempDataProvider,
//	IServiceProvider _serviceProvider,
//	IPageMemoryStorage _pageMemoryStorage,
//     IJsFileMemoryStorage _jsFileMemoryStorage,
//	ICssFileMemoryStorage _cssFileMemoryStorage
//	,IWebHostEnvironment _webHostEnvironment,
//	ICssFileMemoryStorage cssFileMemoryStorage,
//	RazorLightEngine _engine
//	) : IPageService
//	{





//		public async Task<string> RenderPageRouting(string? address, params object[]? param)
//		{
//			var page = new Page();

//			if (!address.HasValue())
//			{
//				page = _pageMemoryStorage.GetByAddress("");


//				//_service.Repository<Page>().TableNoTracking
//				//                    .Include(c=>c.SaveContent)
//				//                    .Include(c=>c.PublishContent)

//				//                    .FirstOrDefault(c => c.Address.Contains(""));

//				return await RenderWithLayout(page, param);
//			}

//			page = _pageMemoryStorage.GetByAddress(address);

//			//_service.Repository<Page>().TableNoTracking
//			//             .Include(c => c.SaveContent)
//			//             .Include(c => c.PublishContent)
//			//	.FirstOrDefault(c => c.Address.Contains(address.ToLower())  );

//			if (page == null)
//			{
//				throw new Exception("Not Found");
//			}

//			if (page.RenderAll || page.TypePage == TypePage.Layout)
//			{
//				return await RenderPageFull(page, param);

//			}

//			return await RenderWithLayout(page, param);


//		}

//		public async Task<string> RenderPageFull(Page page, params object[] param)
//		{

//			try
//			{
//				var template = new StringBuilder();
//				var cssList = "";
//				var jsList = "";

//				foreach (var s in page?.PublishContent?.JavaScriptLinks)
//				{

//					jsList += $"<script src='{s}-system-file.js'></script> \n";
//				}


//				if (page.TypePage == TypePage.Layout)
//				{
//					template.Append(page.PublishContent.Html);
//				}
//				else
//				{
//					if (page.LayoutId != null)
//					{


//						var layoutPage = _pageMemoryStorage.GetPageById(page.LayoutId);

//						if (layoutPage == null)
//						{
//							throw new Exception("صفحه چیدمان یافت نشد .");
//						}

//						var renderLyout = await RenderDemoWithLayout(null, layoutPage);

//						template.Clear();
//						template.Append(ConvertStringToHtmlDocumentBodyAndCss(renderLyout, page?.PublishContent?.Html, page?.PublishContent?.Css, layoutPage.LoadSectionSelector));




//					}
//					else
//					{
//						template.Append(@$"
//								<html direction=""rtl"" dir=""rtl"" style=""direction: rtl"" >
 
//								<head>
//									<base href=""""/>
//									<title>حصین حاسب</title>
//									<meta charset=""utf-8"" />
// 									<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />

				 
//									 {cssList}
//								  <style> {page?.PublishContent?.Css} </style>
//								</head>
 
//									<body>
		
//									{page?.PublishContent?.Html}

//									</body>

				
//								    {jsList}
			
//							</html>
//							");
//					}


//				}







//				string appendScript = "";

//				if (page.TypePage == TypePage.Layout || page.LayoutId == null)
//				{

//					if (template.Length == 0)
//					{
//						var returnToPathScritp = "<script> setTimeout(()=>{window.location.pathname = \"/\"},  10000) </script>";
//						var tempErrorHtml = @$"

//						<html direction=""rtl"" dir=""rtl"" style=""direction: rtl"" >
 
//							<head>
//								<base href=""""/>
//								<title>حصین حاسب</title>
//								<meta charset=""utf-8"" />
// 								<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
								
//							</head>
 
//							<body >
		
//							<h1>متاسفانه صفحه مورد نظر شما یافت نشد .</h1>
//								برگشت به صفحه اصلی 10 ثانیه دیگر
						
//							</body>
					
//							{returnToPathScritp}
//						</html>
//					";


//						return tempErrorHtml;
//					}

//					template.Insert(0, @" 
//						@using System.Dynamic;
//						@using Common.Utilities;
//							@{
//									dynamic JSData = new ExpandoObject();
//							}
 
//                             " +
//								   page?.PublishContent?.Razor);


//					appendScript = @$"

//				<script>
//			 		let data = @(ObjectExtensions.JsonSerialize(JSData));
//                         const appController = new AppController(
//	                         null,
//	                         null,
//                              $(""#{page.LoadSectionSelector}"")
//                         );

//                         appController._init();

//				</script>";

//				}
//				else
//				{





//					template.Insert((template?.ToString()?.IndexOf("}") ?? 0) + 1, page?.PublishContent?.Razor ?? "");

//				}





//				string scrip = page.PublishContent.JavaScript;



//				if (scrip.HasValue())
//				{
//					appendScript += "<script>";
//					appendScript += scrip;
//					appendScript += "</script>";
//				}



//				var cssSystemFiles = new ListSystemStaticFiles(TypeSystemStaticFiles.Css)
//					.Files.OrderByDescending(c => c.Order).Select(c => c.Address).ToArray();

//				var jsSystemFiles = new ListSystemStaticFiles(TypeSystemStaticFiles.Js)
//					.Files.OrderByDescending(c => c.Order).Select(c => c.Address).ToArray();

//				if (page?.PublishContent?.CssLinks.Any() ?? false)
//				{
//					var cssListIds = page?.PublishContent?.CssLinks.Select(Guid.Parse).ToList();

//					var cssListVersions = cssFileMemoryStorage.GetCssFiles()
//						.Select(c => new FileWithModifiedOn { Id = c.Id, ModifiedOn = c.ModifiedDateMiladiDateTime })
//						.Where(c => cssListIds.Contains((Guid)c.Id))
//						.ToArray();

//					if (cssListVersions.Any())
//						ConvertStringToHtmlDocumentCssList(ref template, cssListVersions, "-system-file.css");
//				}


//				if (page.TypePage == TypePage.Layout)
//				{
//					//template = ConvertStringToHtmlDocumentCssList(template, cssSystemFiles, "");
//					ConvertStringToHtmlDocumentJs(ref template, jsSystemFiles, "");

//					if (page?.PublishContent?.Css.HasValue() ?? false)
//						ConvertStringToHtmlDocumentCss(ref template, page?.PublishContent?.Css);
//				}




//				if (appendScript.HasValue())
//					ConvertStringToHtmlDocumentJs(ref template, appendScript);




//				if (page?.PublishContent?.Head?.HasValue() ?? false)
//					ConvertStringToHtmlDocumentHead(ref template, page?.PublishContent?.Head);




//				if (page?.PublishContent?.JavaScriptLinks.Any() ?? false)
//				{
//					var jsListIds = page?.PublishContent?.JavaScriptLinks.Select(Guid.Parse).ToList();

//					var jsListVersions = _jsFileMemoryStorage.GetJsFiles()
//						.Select(c => new FileWithModifiedOn { Id = c.Id, ModifiedOn = c.ModifiedDateMiladiDateTime })
//						.Where(c => jsListIds.Contains((Guid)c.Id))
//						.ToArray();

//					ConvertStringToHtmlDocumentJs(ref template, jsListVersions, "-system-file.js");
//				}




//				List<string> usingDirectives = ExtractDirectives(template, "@using");
//				List<string> injectDirectives = ExtractDirectives(template, "@inject");


//				string updatedTemplate = AddDirectivesToTemplate(ref template, usingDirectives, injectDirectives);



//				var _tempFolderPath = Path.Combine(_webHostEnvironment.ContentRootPath, "TempRazorPages");

//				if (!Directory.Exists(_tempFolderPath))
//				{
//					Directory.CreateDirectory(_tempFolderPath);
//				}





//				var addresstemp = page.Id.ToString() + "Publish" + page.ModifiedDateMiladiDateTime.Value.ToString("yy-MM-dd-HH-mm-ss");




//				string result = "";



//				var prevTemplate = _engine.Options.DynamicTemplates.FirstOrDefault(c => c.Key.Contains(page.Id.ToString() + "Publish"));
//				if (prevTemplate.Key != null)
//				{
//					_engine.Options.DynamicTemplates.Remove(prevTemplate.Key);
//					_engine.Handler.Cache.Remove(prevTemplate.Key);

//				}


//				result = await _engine.CompileRenderStringAsync(addresstemp, updatedTemplate, new { });




//				return result;


//			}
//			catch (Exception ex)
//			{


//				if (page.TypePage == TypePage.Layout)
//				{

//					var returnToPathScritp = "<script> setTimeout(()=>{window.location.pathname = \"/\"},  10000) </script>";
//					var tempErrorHtml = @$"

				 
//					<html direction=""rtl"" dir=""rtl"" style=""direction: rtl"" >
 
//						<head>
//							<base href=""""/>
//							<title>حصین حاسب</title>
//							<meta charset=""utf-8"" />
// 							<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
								
//						</head>
 
//						<body >
		
//						{ex.Message}
//							برگشت به صفحه اصلی 10 ثانیه دیگر
						
//						</body>
					
//						{returnToPathScritp}
//					</html>
//					";


//					return tempErrorHtml;
//				}


//				return $"<span>{ex.Message}</span>";

//			}
//		}

//		#region RenderPageAlone
//		public async Task<RenderPageViewModel> RenderPageDemoAsync(string address, Guid? id)
//		{
//			var page = _pageMemoryStorage.GetByAddress(address);

//			var template = new StringBuilder();

//			if (page == null)
//			{
//				page = _pageMemoryStorage.GetByAddress("404");
//			}


//			var pageid = Guid.NewGuid().ToString().Substring(0, 5);
//			template.Append(@" 
//						@using System.Dynamic;
//						@using Common.Utilities;
					
//							@{
//									dynamic JSData = new ExpandoObject();
//							}
 
//                             " +
//						page.SaveContent.Razor
//						+ "  "
//						+
//						$"<style> {page.SaveContent.Css} </style>"

//						 +
//						$"<div {pageid}>"

//							+
//							page.SaveContent.Html
//						    +
//						   "</div>");

//			string scrip = page.SaveContent.JavaScript;
//			string jsLinks = "";



//			string appendScript = @"
//<script type='data-object'>
//let data = @ObjectExtensions.JsonSerialize(JSData) //data
//</script>
//<script>
//";
//			appendScript += scrip;
//			appendScript += "</script>";


//			template.AppendLine(appendScript);

//			List<string> usingDirectives = ExtractDirectives(template, "@using");
//			List<string> injectDirectives = ExtractDirectives(template, "@inject");

//			string updatedTemplate = AddDirectivesToTemplate(ref template, usingDirectives, injectDirectives);


//			var addresstemp = page.Id + "DemoRenderPage" + page.ModifiedDateMiladiDateTime.Value.ToString("yy-MM-dd-HH-mm-ss");


//			string result = "";



//			var prevTemplate = _engine.Options.DynamicTemplates.FirstOrDefault(c => c.Key.Contains(page.Id.ToString() + "Demo"));
//			if (prevTemplate.Key != null)
//			{
//				_engine.Options.DynamicTemplates.Remove(prevTemplate.Key);
//				_engine.Handler.Cache.Remove(prevTemplate.Key);

//			}


//			result = await _engine.CompileRenderStringAsync(addresstemp, updatedTemplate, new { });

//			var scripts = ExtractScriptTags(result);


//			result = RemoveScriptTags(result);


//			string script = "";


//			var JSData = "";

//			foreach (var s in scripts)
//			{

//				if (s.Contains("<script type='data-object'>"))
//				{
//					JSData = s.Split("//data")[0].Replace("let data =", "").Replace("<script type='data-object'>", "");

//				}

//				else if (s.Contains("<script>"))
//				{
//					script += s.Replace("<script>", "").Replace("</script>", "") + Environment.NewLine;

//				}

//				else if (s.Contains("src") || s.Contains("text/x-jquery-tmpl"))
//				{

//					result = result.Insert(0, s + Environment.NewLine);

//				}

//			}

//			//var listScriptsData = _service.Get<JsFile>(c => page.Data.PublishContent.JavaScriptLinks.Contains(c.Id)).Select(c=>new {Content = c.Data.Content  , Title = c.Data.Title}).ToList();

//			// foreach(var sl in listScriptsData)
//			// {
//			//     var linkScirpt = $" /*  {sl.Title}  Section Start  */ \n \n";

//			//     linkScirpt += sl.Content + Environment.NewLine + Environment.NewLine;

//			//     linkScirpt += $" /*  {sl.Title}  Section End  */ \n \n";

//			//     script = script.Insert(0, linkScirpt) + Environment.NewLine;

//			// }

//			dynamic? documentData = null;

//			if (id != null && id != Guid.Empty)
//			{
//				documentData = _service.Repository<Page>().TableNoTracking.Include(c => c.SaveContent).Include(c => c.PublishContent).FirstOrDefault(c => c.Id == id);
//			}
//			if (string.IsNullOrWhiteSpace(JSData))
//			{
//				JSData = "{}";
//			}
//			var pageInfo = new
//			PageInfo
//			{
//				Title = page.Name,
//				Id = pageid,
//				JSData = JSData,
//				Address = address.ToLower(),
//				documentData = documentData
//			};



//			return new RenderPageViewModel { html = result, script = script, pageInfo = pageInfo, listJsLink = page.SaveContent.JavaScriptLinks.Select(c => c += "-system-file.js").ToList(), listCssLink = page.SaveContent.CssLinks.Select(c => c += "-system-file.css").ToList(), Css = page?.SaveContent?.Css };
//		}

//		public async Task<RenderPageViewModel> RenderPageAsync(string address, Guid? id)
//		{
//			var page = _pageMemoryStorage.GetByAddress(address);

//			StringBuilder template = new StringBuilder();


//			if (page == null)
//			{
//				page = _pageMemoryStorage.GetByAddress("404");
//			}



//			template.Append(@" 
//						@using System.Dynamic;
//						@using Common.Utilities;
					
//							@{
//									dynamic JSData = new ExpandoObject();
//							}
 
//                             " +
//						page?.PublishContent?.Razor
//						+ "  "
//						+
//						$"<style> {page?.PublishContent?.Css} </style>"

//						 +
//						$"<div>"

//							+
//							page?.PublishContent?.Html
//						    +
//						   "</div>");

//			string scrip = page?.PublishContent?.JavaScript;
//			string jsLinks = "";



//			string appendScript = @"
//<script type='data-object'>
//let data = @ObjectExtensions.JsonSerialize(JSData) //data
//</script>
//<script>
//";
//			appendScript += scrip;
//			appendScript += "</script>";


//			template.AppendLine(appendScript);

//			List<string> usingDirectives = ExtractDirectives(template, "@using");
//			List<string> injectDirectives = ExtractDirectives(template, "@inject");

//			string updatedTemplate = AddDirectivesToTemplate(ref template, usingDirectives, injectDirectives);





//			var addresstemp = page.Id + "PublishRenderPage" + page.ModifiedDateMiladiDateTime.Value.ToString("yy-MM-dd-HH-mm-ss");



//			string result = "";



//			var cacheResult = _engine.Handler.Cache.RetrieveTemplate(addresstemp);
//			if (cacheResult.Success)
//			{


//				var templatePage = cacheResult.Template.TemplatePageFactory();
//				result = await _engine.RenderTemplateAsync(templatePage, new { });


//			}

//			else
//			{
//				var prevTemplate = _engine.Options.DynamicTemplates.FirstOrDefault(c => c.Key.Contains(page.Id.ToString() + "Demo"));
//				if (prevTemplate.Key != null)
//				{
//					_engine.Options.DynamicTemplates.Remove(prevTemplate.Key);
//					_engine.Handler.Cache.Remove(prevTemplate.Key);

//				}


//				result = await _engine.CompileRenderStringAsync(addresstemp, updatedTemplate, new { });


//			}


//			var scripts = ExtractScriptTags(result);


//			result = RemoveScriptTags(result);


//			string script = "";


//			var JSData = "";

//			foreach (var s in scripts)
//			{

//				if (s.Contains("<script type='data-object'>"))
//				{
//					JSData = s.Split("//data")[0].Replace("let data =", "").Replace("<script type='data-object'>", "");

//				}

//				else if (s.Contains("<script>"))
//				{
//					script += s.Replace("<script>", "").Replace("</script>", "") + Environment.NewLine;

//				}

//				else if (s.Contains("src") || s.Contains("text/x-jquery-tmpl"))
//				{

//					result = result.Insert(0, s + Environment.NewLine);

//				}

//			}


//			dynamic? documentData = null;

//			if (id != null && id != Guid.Empty)
//			{
//				documentData = _service.Repository<Page>().TableNoTracking.Include(c => c.SaveContent).Include(c => c.PublishContent).FirstOrDefault(c => c.Id == id);
//			}
//			if (string.IsNullOrWhiteSpace(JSData))
//			{
//				JSData = "{}";
//			}
//			var pageInfo = new
//			PageInfo
//			{
//				Title = page.Name,
//				Id = "1",
//				JSData = JSData,
//				Address = address.ToLower(),
//				documentData = documentData
//			};



//			return new RenderPageViewModel { html = result, script = script, pageInfo = pageInfo, listJsLink = page?.PublishContent?.JavaScriptLinks.Select(c => c += "-system-file.js").ToList() ?? [], listCssLink = page?.PublishContent?.CssLinks.Select(c => c += "-system-file.css").ToList() ?? [], Css = page?.SaveContent?.Css ?? "" };
//		}
//		#endregion
//		#region RenderWithLayout
//		public async Task<string> RenderWithLayout(Page page, params object[] param)
//		{

//			try
//			{
//				var template = new StringBuilder();
//				var cssList = "";
//				var jsList = "";

//				foreach (var s in page?.PublishContent?.JavaScriptLinks)
//				{

//					jsList += $"<script src='{s}-system-file.js'></script> \n";
//				}


//				if (page.TypePage == TypePage.Layout)
//				{
//					template.Append(page.PublishContent.Html);
//				}
//				else
//				{
//					if (page.LayoutId != null)
//					{


//						var layoutPage = _pageMemoryStorage.GetPageById(page.LayoutId);

//						if (layoutPage == null)
//						{
//							throw new Exception("صفحه چیدمان یافت نشد .");
//						}

//						var renderLyout = await RenderWithLayout(layoutPage);

//						template.Clear();
//						template.Append(ConvertStringToHtmlDocumentBodyAndCss(renderLyout, page?.PublishContent?.Html, page?.PublishContent?.Css, layoutPage.LoadSectionSelector));




//					}
//					else
//					{
//						template.Append(@$"
//								<html direction=""rtl"" dir=""rtl"" style=""direction: rtl"" >
 
//								<head>
//									<base href=""""/>
//									<title>حصین حاسب</title>
//									<meta charset=""utf-8"" />
// 									<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />

				 
//									 {cssList}
//								  <style> {page?.PublishContent?.Css} </style>
//								</head>
 
//									<body>
		
//									{page?.PublishContent?.Html}

//									</body>

				
//								    {jsList}
			
//							</html>
//							");
//					}


//				}







//				string appendScript = "";

//				if (page.TypePage == TypePage.Layout || page.LayoutId == null)
//				{

//					if (template.Length == 0)
//					{
//						var returnToPathScritp = "<script> setTimeout(()=>{window.location.pathname = \"/\"},  10000) </script>";
//						var tempErrorHtml = @$"

//						<html direction=""rtl"" dir=""rtl"" style=""direction: rtl"" >
 
//							<head>
//								<base href=""""/>
//								<title>حصین حاسب</title>
//								<meta charset=""utf-8"" />
// 								<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
								
//							</head>
 
//							<body >
		
//							<h1>متاسفانه صفحه مورد نظر شما یافت نشد .</h1>
//								برگشت به صفحه اصلی 10 ثانیه دیگر
						
//							</body>
					
//							{returnToPathScritp}
//						</html>
//					";


//						return tempErrorHtml;
//					}

//					template.Insert(0, @" 
//						@using System.Dynamic;
//						@using Common.Utilities;
//							@{
//									dynamic JSData = new ExpandoObject();
//							}
 
//                             " +
//								   page?.PublishContent?.Razor);


//					appendScript = @$"

//				<script>
//			 		let data = @(ObjectExtensions.JsonSerialize(JSData));
//                         const appController = new AppController(
//	                         null,
//	                         null,
//                              $(""#{page.LoadSectionSelector}"")
//                         );

//                         appController._init();

//				</script>";

//				}
//				else
//				{





//					template = template.Insert((template?.ToString()?.IndexOf("}") ?? 0) + 1, page?.PublishContent?.Razor ?? "");

//				}





//				string scrip = page.PublishContent.JavaScript;



//				if (scrip.HasValue())
//				{
//					appendScript += "<script>";
//					appendScript += scrip;
//					appendScript += "</script>";
//				}



//				var cssSystemFiles = new ListSystemStaticFiles(TypeSystemStaticFiles.Css)
//					.Files.OrderByDescending(c => c.Order).Select(c => c.Address).ToArray();

//				var jsSystemFiles = new ListSystemStaticFiles(TypeSystemStaticFiles.Js)
//					.Files.OrderByDescending(c => c.Order).Select(c => c.Address).ToArray();

//				if (page?.PublishContent?.CssLinks.Any() ?? false)
//				{
//					var cssListIds = page?.PublishContent?.CssLinks.Select(Guid.Parse).ToList();

//					var cssListVersions = cssFileMemoryStorage.GetCssFiles()
//						.Select(c => new FileWithModifiedOn { Id = c.Id, ModifiedOn = c.ModifiedDateMiladiDateTime })
//						.Where(c => cssListIds.Contains((Guid)c.Id))
//						.ToArray();

//					if (cssListVersions.Any())
//						ConvertStringToHtmlDocumentCssList(ref template, cssListVersions, "-system-file.css");
//				}


//				if (page.TypePage == TypePage.Layout)
//				{
//					//template = ConvertStringToHtmlDocumentCssList(template, cssSystemFiles, "");
//					ConvertStringToHtmlDocumentJs(ref template, jsSystemFiles, "");

//					if (page?.PublishContent?.Css.HasValue() ?? false)
//						ConvertStringToHtmlDocumentCss(ref template, page?.PublishContent?.Css);
//				}




//				if (appendScript.HasValue())
//					ConvertStringToHtmlDocumentJs(ref template, appendScript);




//				if (page?.PublishContent?.Head?.HasValue() ?? false)
//					ConvertStringToHtmlDocumentHead(ref template, page?.PublishContent?.Head);




//				if (page?.PublishContent?.JavaScriptLinks.Any() ?? false)
//				{
//					var jsListIds = page?.PublishContent?.JavaScriptLinks.Select(Guid.Parse).ToList();

//					var jsListVersions = _jsFileMemoryStorage.GetJsFiles()
//						.Select(c => new FileWithModifiedOn { Id = c.Id, ModifiedOn = c.ModifiedDateMiladiDateTime })
//						.Where(c => jsListIds.Contains((Guid)c.Id))
//						.ToArray();

//					ConvertStringToHtmlDocumentJs(ref template, jsListVersions, "-system-file.js");
//				}




//				List<string> usingDirectives = ExtractDirectives(template, "@using");
//				List<string> injectDirectives = ExtractDirectives(template, "@inject");


//				string updatedTemplate = AddDirectivesToTemplate(ref template, usingDirectives, injectDirectives);



//				var _tempFolderPath = Path.Combine(_webHostEnvironment.ContentRootPath, "TempRazorPages");

//				if (!Directory.Exists(_tempFolderPath))
//				{
//					Directory.CreateDirectory(_tempFolderPath);
//				}






//				if (page.TypePage == TypePage.Layout)
//				{
//					return updatedTemplate;

//				}




//				var addresstemp = page.Id.ToString() + "Publish" + page.ModifiedDateMiladiDateTime.Value.ToString("yy-MM-dd-HH-mm-ss");




//				string result = "";



//				var cacheResult = _engine.Handler.Cache.RetrieveTemplate(addresstemp);
//				if (cacheResult.Success)
//				{


//					var templatePage = cacheResult.Template.TemplatePageFactory();
//					result = await _engine.RenderTemplateAsync(templatePage, new { });


//				}

//				else
//				{
//					var prevTemplate = _engine.Options.DynamicTemplates.FirstOrDefault(c => c.Key.Contains(page.Id.ToString() + "Demo"));
//					if (prevTemplate.Key != null)
//					{
//						_engine.Options.DynamicTemplates.Remove(prevTemplate.Key);
//						_engine.Handler.Cache.Remove(prevTemplate.Key);

//					}


//					result = await _engine.CompileRenderStringAsync(addresstemp, updatedTemplate, new { });


//				}



//				return result;


//			}
//			catch (Exception ex)
//			{


//				if (page.TypePage == TypePage.Layout)
//				{

//					var returnToPathScritp = "<script> setTimeout(()=>{window.location.pathname = \"/\"},  10000) </script>";
//					var tempErrorHtml = @$"

				 
//					<html direction=""rtl"" dir=""rtl"" style=""direction: rtl"" >
 
//						<head>
//							<base href=""""/>
//							<title>حصین حاسب</title>
//							<meta charset=""utf-8"" />
// 							<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
								
//						</head>
 
//						<body >
		
//						{ex.Message}
//							برگشت به صفحه اصلی 10 ثانیه دیگر
						
//						</body>
					
//						{returnToPathScritp}
//					</html>
//					";


//					return tempErrorHtml;
//				}


//				return $"<span>{ex.Message}</span>";

//			}
//		}
//		public async Task<string> RenderDemoWithLayout(string? address, Page? page, params object[] param)
//		{
//			if (page == null)
//				page = _pageMemoryStorage.GetByAddress(address);

//			try
//			{
//				var template = new StringBuilder();
//				var cssList = "";
//				var jsList = "";

//				foreach (var s in page?.SaveContent?.JavaScriptLinks)
//				{

//					jsList += $"<script src='{s}-system-file.js'></script> \n";
//				}


//				if (page.TypePage == TypePage.Layout)
//				{
//					template.Append(page.SaveContent.Html);
//				}
//				else
//				{
//					if (page.LayoutId != null)
//					{


//						var layoutPage = _pageMemoryStorage.GetPageById(page.LayoutId);

//						if (layoutPage == null)
//						{
//							throw new Exception("صفحه چیدمان یافت نشد .");
//						}

//						var renderLyout = await RenderDemoWithLayout(null, layoutPage);

//						template.Clear();
//						template.Append(ConvertStringToHtmlDocumentBodyAndCss(renderLyout, page?.SaveContent?.Html, page?.SaveContent?.Css, layoutPage.LoadSectionSelector));




//					}
//					else
//					{
//						template.Append(@$"
//								<html direction=""rtl"" dir=""rtl"" style=""direction: rtl"" >
 
//								<head>
//									<base href=""""/>
//									<title>حصین حاسب</title>
//									<meta charset=""utf-8"" />
// 									<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />

				 
//									 {cssList}
//								  <style> {page?.SaveContent?.Css} </style>
//								</head>
 
//									<body>
		
//									{page?.SaveContent?.Html}

//									</body>

				
//								    {jsList}
			
//							</html>
//							");
//					}


//				}







//				string appendScript = "";

//				if (page.TypePage == TypePage.Layout || page.LayoutId == null)
//				{

//					if (template.Length == 0)
//					{
//						var returnToPathScritp = "<script> setTimeout(()=>{window.location.pathname = \"/\"},  10000) </script>";
//						var tempErrorHtml = @$"

//						<html direction=""rtl"" dir=""rtl"" style=""direction: rtl"" >
 
//							<head>
//								<base href=""""/>
//								<title>حصین حاسب</title>
//								<meta charset=""utf-8"" />
// 								<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
								
//							</head>
 
//							<body >
		
//							<h1>متاسفانه صفحه مورد نظر شما یافت نشد .</h1>
//								برگشت به صفحه اصلی 10 ثانیه دیگر
						
//							</body>
					
//							{returnToPathScritp}
//						</html>
//					";


//						return tempErrorHtml;
//					}

//					template.Insert(0, @" 
//						@using System.Dynamic;
//						@using Common.Utilities;
//							@{
//									dynamic JSData = new ExpandoObject();
//							}
 
//                             " +
//								   page?.SaveContent?.Razor);


//					appendScript = @$"

//				<script>
//			 		let data = @(ObjectExtensions.JsonSerialize(JSData));
//                         const appController = new AppController(
//	                         null,
//	                         null,
//                              $(""#{page.LoadSectionSelector}"")
//                         );

//                         appController._init();

//				</script>";

//				}
//				else
//				{





//					template.Insert((template?.ToString()?.IndexOf("}") ?? 0) + 1, page?.SaveContent?.Razor ?? "");

//				}





//				string scrip = page.SaveContent.JavaScript;



//				if (scrip.HasValue())
//				{
//					appendScript += "<script>";
//					appendScript += scrip;
//					appendScript += "</script>";
//				}



//				var cssSystemFiles = new ListSystemStaticFiles(TypeSystemStaticFiles.Css)
//					.Files.OrderByDescending(c => c.Order).Select(c => c.Address).ToArray();

//				var jsSystemFiles = new ListSystemStaticFiles(TypeSystemStaticFiles.Js)
//					.Files.OrderByDescending(c => c.Order).Select(c => c.Address).ToArray();

//				if (page?.SaveContent?.CssLinks.Any() ?? false)
//				{
//					var cssListIds = page?.SaveContent?.CssLinks.Select(Guid.Parse).ToList();

//					var cssListVersions = cssFileMemoryStorage.GetCssFiles()
//						.Select(c => new FileWithModifiedOn { Id = c.Id, ModifiedOn = c.ModifiedDateMiladiDateTime })
//						.Where(c => cssListIds.Contains((Guid)c.Id))
//						.ToArray();

//					if (cssListVersions.Any())
//						ConvertStringToHtmlDocumentCssList(ref template, cssListVersions, "-system-file.css");
//				}


//				if (page.TypePage == TypePage.Layout)
//				{
//					//template = ConvertStringToHtmlDocumentCssList(template, cssSystemFiles, "");
//					ConvertStringToHtmlDocumentJs(ref template, jsSystemFiles, "");

//					if (page?.SaveContent?.Css.HasValue() ?? false)
//						ConvertStringToHtmlDocumentCss(ref template, page?.SaveContent?.Css);
//				}




//				if (appendScript.HasValue())
//					ConvertStringToHtmlDocumentJs(ref template, appendScript);




//				if (page?.SaveContent?.Head?.HasValue() ?? false)
//					ConvertStringToHtmlDocumentHead(ref template, page?.SaveContent?.Head);




//				if (page?.SaveContent?.JavaScriptLinks.Any() ?? false)
//				{
//					var jsListIds = page?.SaveContent?.JavaScriptLinks.Select(Guid.Parse).ToList();

//					var jsListVersions = _jsFileMemoryStorage.GetJsFiles()
//						.Select(c => new FileWithModifiedOn { Id = c.Id, ModifiedOn = c.ModifiedDateMiladiDateTime })
//						.Where(c => jsListIds.Contains((Guid)c.Id))
//						.ToArray();

//					ConvertStringToHtmlDocumentJs(ref template, jsListVersions, "-system-file.js");
//				}




//				List<string> usingDirectives = ExtractDirectives(template, "@using");
//				List<string> injectDirectives = ExtractDirectives(template, "@inject");


//				string updatedTemplate = AddDirectivesToTemplate(ref template, usingDirectives, injectDirectives);



//				var _tempFolderPath = Path.Combine(_webHostEnvironment.ContentRootPath, "TempRazorPages");

//				if (!Directory.Exists(_tempFolderPath))
//				{
//					Directory.CreateDirectory(_tempFolderPath);
//				}






//				if (page.TypePage == TypePage.Layout)
//				{
//					return updatedTemplate;

//				}




//				var addresstemp = page.Id.ToString() + "Demo" + page.ModifiedDateMiladiDateTime.Value.ToString("yy-MM-dd-HH-mm-ss");




//				string result = "";



//				var prevTemplate = _engine.Options.DynamicTemplates.FirstOrDefault(c => c.Key.Contains(page.Id.ToString() + "Demo"));
//				if (prevTemplate.Key != null)
//				{
//					_engine.Options.DynamicTemplates.Remove(prevTemplate.Key);
//					_engine.Handler.Cache.Remove(prevTemplate.Key);

//				}


//				result = await _engine.CompileRenderStringAsync(addresstemp, updatedTemplate, new { });




//				return result;


//			}
//			catch (Exception ex)
//			{


//				if (page.TypePage == TypePage.Layout)
//				{

//					var returnToPathScritp = "<script> setTimeout(()=>{window.location.pathname = \"/\"},  10000) </script>";
//					var tempErrorHtml = @$"

				 
//					<html direction=""rtl"" dir=""rtl"" style=""direction: rtl"" >
 
//						<head>
//							<base href=""""/>
//							<title>حصین حاسب</title>
//							<meta charset=""utf-8"" />
// 							<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
								
//						</head>
 
//						<body >
		
//						{ex.Message}
//							برگشت به صفحه اصلی 10 ثانیه دیگر
						
//						</body>
					
//						{returnToPathScritp}
//					</html>
//					";


//					return tempErrorHtml;
//				}


//				return $"<span>{ex.Message}</span>";

//			}
//		}

//		#endregion
//		#region CreateAndRenderDemoForEditPage
//		public string CreateDemoPage(Page page, bool rtnHtml = false)
//		{
//			throw new NotImplementedException();
//			//var cssList = "";
//			//var jsList = "";



//			//foreach (var s in page?.SaveContent?.JavaScriptLinks)
//			//{

//			//	jsList += $"<script src='{s}-system-file.js'></script> \n";
//			//}
//			//var html = "";

//			//if (page.TypePage == TypePage.Layout)
//			//{
//			//	html = page.SaveContent.Html;
//			//}
//			//else
//			//{
//			//	if (page.LayoutId != null)
//			//	{

//			//		var layoutPage = _service.Repository<Page>()
//			//			.TableNoTracking
//			//			.Include(c => c.SaveContent)
//			//			.Include(c => c.PublishContent)
//			//			.FirstOrDefault(c => c.Id == page.LayoutId);

//			//		if (layoutPage == null)
//			//		{
//			//			throw new Exception("صفحه چیدمان یافت نشد .");
//			//		}

//			//		var renderLyout = CreateDemoPage(layoutPage, true);

//			//		html = ConvertStringToHtmlDocumentBodyAndCss(renderLyout, page?.SaveContent?.Html, page?.SaveContent?.Css, layoutPage.LoadSectionSelector);

//			//	}
//			//	else
//			//	{
//			//		html = @$"


//			//	<html direction=""rtl"" dir=""rtl"" style=""direction: rtl"" >

//			//	<head>
//			//		<base href=""""/>
//			//		<title>حصین حاسب</title>
//			//		<meta charset=""utf-8"" />
//			//			<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />


//			//		 {cssList}
//			//                   <style> {page.SaveContent.Css} </style>
//			//	</head>

//			//		<body>

//			//		{page.SaveContent.Html}

//			//		</body>


//			//	    {jsList}

//			//</html>
//			//";
//			//	}


//			//}




//			//string template = "";

//			//string appendScript = "";

//			//if (page.TypePage == TypePage.Layout || page.LayoutId == null)
//			//{
//			//	template = @" 
//			//			@using System.Dynamic;
//			//			@using Common.Utilities;
//			//				@{
//			//						dynamic JSData = new ExpandoObject();
//			//				}

//			//                          " +
//			//			 page.SaveContent.Razor +
//			//			 " " +
//			//			 html;


//			//	appendScript = @"

//			//	<script>
//			//	let data = @Html.Raw(ObjectExtensions.JsonSerialize(JSData));
//			//	</script>";

//			//}
//			//else
//			//{

//			//	template = @$"{html}";

//			//	var indexRazor = html.IndexOf('}');

//			//	template = template.Insert(indexRazor + 1, page?.SaveContent?.Razor ?? "");

//			//}





//			//string scrip = page.SaveContent.JavaScript;
//			//string jsLinks = "";



//			//appendScript += "<script>";
//			//appendScript += scrip;
//			//appendScript += "</script>";


//			//template += appendScript;

//			//var systemFiles = new ListSystemStaticFiles(TypeSystemStaticFiles.Css)
//			//	.Files.OrderByDescending(c => c.Order).Select(c => c.Address).ToArray();


//			//var cssListIds = page?.PublishContent?.CssLinks.Select(Guid.Parse).ToList();

//			//var cssListVersions = _service.Repository<CssFile>()
//			//	.TableNoTracking
//			//	.Select(c => new FileWithModifiedOn { Id = c.Id, ModifiedOn = c.ModifiedDateMiladiDateTime })
//			//	.Where(c => cssListIds.Contains((Guid)c.Id))
//			//	.ToArray();



//			//template = ConvertStringToHtmlDocumentCssList(template, cssListVersions, "-system-file.css");


//			//template = ConvertStringToHtmlDocumentCssList(template, systemFiles, "");

//			//if (page?.SaveContent?.Head.HasValue() ?? false)
//			//	template = ConvertStringToHtmlDocumentHead(template, page?.SaveContent?.Head);


//			//template = ConvertStringToHtmlDocumentJs(template, page?.SaveContent?.JavaScriptLinks, "-system-file.js");


//			//List<string> usingDirectives = ExtractDirectives(template, "@using");
//			//List<string> injectDirectives = ExtractDirectives(template, "@inject");


//			//string updatedTemplate = AddDirectivesToTemplate(template, usingDirectives, injectDirectives);


//			//var _tempFolderPath = Path.Combine(_webHostEnvironment.ContentRootPath, "TempRazorPages");

//			//if (!Directory.Exists(_tempFolderPath))
//			//{
//			//	Directory.CreateDirectory(_tempFolderPath);
//			//}

//			//if (rtnHtml == true)
//			//{
//			//	return updatedTemplate;

//			//}

//			//var tempId = Guid.NewGuid().ToString().Replace("-", "");
//			//var addresstemp = page.Address + tempId + tempId;
//			//var filePath = Path.Combine(_tempFolderPath, $"{addresstemp}.cshtml");

//			//File.WriteAllText(filePath, updatedTemplate);

//			//return addresstemp;
//		}

//		public async Task<string> RenderDemoPage(string address)
//		{
//			throw new NotImplementedException();
//			//			var actionContext = new ActionContext(new DefaultHttpContext { RequestServices = _serviceProvider }, new RouteData(), new ActionDescriptor());

//			//			var result = "";
//			//			var _tempFolderPath = Path.Combine(_webHostEnvironment.ContentRootPath, "TempRazorPages");
//			//			var filePath = Path.Combine(_tempFolderPath, $"{address}.cshtml");

//			//			try
//			//			{
//			//				using (var sw = new StringWriter())
//			//				{
//			//					var viewResult = _viewEngine.GetView(null, $"/TempRazorPages/{address}.cshtml", false);


//			//					if (!viewResult.Success)
//			//					{
//			//						throw new InvalidOperationException($"Couldn't find view '{address}'");
//			//					}

//			//					var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
//			//					{
//			//						Model = new { }
//			//					};

//			//					var viewContext = new ViewContext(
//			//					    actionContext,
//			//					    viewResult.View,
//			//					    viewDictionary,
//			//					    new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
//			//					    sw,
//			//					    new HtmlHelperOptions()
//			//					);

//			//					await viewResult.View.RenderAsync(viewContext);
//			//					result = sw.ToString();
//			//				}

//			//			}
//			//			catch (Exception e)
//			//			{

//			//				throw new Exception(e.Message);
//			//			}
//			//			finally
//			//			{
//			//				File.Delete(filePath);
//			//			}

//			//			var scripts = ExtractScriptTags(result);

//			//			result = RemoveScriptTags(result);



//			//			var indexOfBody = result.IndexOf("</body>") + 8;



//			//			var scriptLayout = @"



//			//(function () {
//			//               let elemets = $('[id]').toArray();


//			//            let $$ = function (selector) {
//			//                if(selector.length > 0)
//			//                    return $('html').find(selector)
//			//                else
//			//                    return $('html')
//			//            }

//			//            $$.dataBind = $().dataBind;
//			//            elemets.forEach(c => {
//			//                $$[$(c).attr('id')] = $(c);
//			//            })
//			//";


//			//			foreach (var s in scripts)
//			//			{

//			//				if (s.Contains("let data ="))
//			//				{
//			//					scriptLayout = scriptLayout.Insert(0, s.Replace("<script>", "").Replace("</script>", "") + Environment.NewLine);
//			//				}

//			//				else if (s.Contains("<script>"))
//			//				{
//			//					scriptLayout += s.Replace("<script>", "").Replace("</script>", "") + Environment.NewLine;

//			//				}

//			//				else if (s.Contains("src") || s.Contains("text/x-jquery-tmpl"))
//			//				{

//			//					result = result.Insert(indexOfBody, s + Environment.NewLine);

//			//				}

//			//			}

//			//			foreach (var s in new ListSystemStaticFiles(TypeSystemStaticFiles.Js).Files.OrderByDescending(c => c.Order))
//			//			{

//			//				result = result.Insert(indexOfBody, $"<script src='{s.Address}'></script> \n");
//			//			}


//			//			scriptLayout += "  $('[data-realTimeScritp]').remove(); })() ;   </script>";

//			//			scriptLayout = scriptLayout.Insert(0, "<script data-realTimeScritp='true'>" + Environment.NewLine);


//			//			result = result.Insert(result.IndexOf("</html>"), Environment.NewLine + scriptLayout);


//			//			return result;
//		}

//		#endregion
//		#region SystemPages

//		public async Task<string> RenderPageSystemHtmlAsync(string address, Guid? id, ControllerContext controller)
//		{

//			dynamic docData = new { };

//			if (id != null && id != Guid.Empty)
//			{
//				switch (address.Split("-")[0])
//				{
//					case "pagebuilder":
//						docData = _service.Repository<Page>().TableNoTracking
//							.Include(c => c.PublishContent)
//							.Include(c => c.SaveContent)
//							.FirstOrDefault(c => c.Id == id);
//						break;
//					default:
//						break;
//				}
//			}

//			var view = await RenderViewToStringAsync("/Views/SystemPage/" + address.Replace("-", "/") + ".cshtml", docData, controller);

//			var scriptSplited = view.Split("<script>");
//			string script = "";

//			if (scriptSplited.Length > 1)
//			{
//				script = scriptSplited[1].Replace("</script>", "");

//			}
//			var pageid = Guid.NewGuid().ToString().Substring(0, 5);




//			return view;
//		}
//		public async Task<RenderPageViewModel> RenderPageSystemAsync(string address, Guid? id, ControllerContext controller)
//		{

//			var view = await RenderViewToStringAsync("/Views/SystemPage/" + address.Replace("-", "/") + ".cshtml", new { }, controller);

//			var scriptSplited = view.Split("<script>");
//			string script = "";

//			if (scriptSplited.Length > 1)
//			{
//				script = scriptSplited[1].Replace("</script>", "");

//			}
//			var pageid = Guid.NewGuid().ToString().Substring(0, 5);
//			dynamic docData = new { };

//			if (id != null && id != Guid.Empty)
//			{
//				switch (address.Split("-")[0])
//				{
//					case "pagebuilder":
//						docData = _service.Repository<Page>().TableNoTracking
//								 .Include(c => c.PublishContent)
//								 .Include(c => c.SaveContent)
//								  .FirstOrDefault(c => c.Id == id);
//						break;
//					default:
//						break;
//				}
//			}

//			var pageInfo = new
//			PageInfo
//			{
//				Title = "صفحه ساز",
//				Id = pageid,
//				Address = controller.HttpContext.Request.Path + controller.HttpContext.Request.QueryString,
//				documentData = docData
//			};

//			return new RenderPageViewModel { html = scriptSplited[0], script = script, pageInfo = pageInfo };
//		}
//		private static List<string> ExtractDirectives(StringBuilder content, string directiveType)
//		{
//			var directives = new List<string>();
//			var regexPattern = $@"{directiveType}\s+([a-zA-Z0-9\.]+(?:\s+[a-zA-Z0-9_]+)?);?";
//			var regex = new Regex(regexPattern);

//			var matches = regex.Matches(content.ToString());
//			foreach (Match match in matches)
//			{
//				if (match.Groups.Count > 1)
//				{
//					directives.Add(match.Value.Trim());
//				}
//			}

//			return directives;
//		}

//		private static string AddDirectivesToTemplate(ref StringBuilder template, List<string> usingDirectives, List<string> injectDirectives)
//		{
//			usingDirectives = EnsureSemicolon(usingDirectives);
//			injectDirectives = EnsureSemicolon(injectDirectives);

//			var usingsText = string.Join(Environment.NewLine, usingDirectives);
//			var injectsText = string.Join(Environment.NewLine, injectDirectives);
//			var directivesText = usingsText + Environment.NewLine + injectsText;

//			var templateWithoutDirectives = Regex.Replace(template.ToString(), @"(@using\s+[a-zA-Z0-9\.]+\s*;?\s*)+|(@inject\s+[a-zA-Z0-9\.]+\s+[a-zA-Z0-9_]+\s*;?\s*)+", string.Empty);

//			template.Clear();
//			template.Append(directivesText + Environment.NewLine + templateWithoutDirectives);

//			return template.ToString();
//		}

//		private static List<string> EnsureSemicolon(List<string> directives)
//		{
//			for (int i = 0; i < directives.Count; i++)
//			{
//				if (!directives[i].TrimEnd().EndsWith(";"))
//				{
//					directives[i] = directives[i].TrimEnd() + ";";
//				}
//			}
//			return directives;
//		}

//		static string[] ExtractScriptTags(string input)
//		{
//			Regex regex = new Regex(@"<script\b[^>]*>(.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
//			var matches = regex.Matches(input);

//			string[] scriptTags = new string[matches.Count];
//			for (int i = 0; i < matches.Count; i++)
//			{
//				scriptTags[i] = matches[i].Value;
//			}

//			return scriptTags;
//		}

//		static string RemoveScriptTags(string input)
//		{
//			return Regex.Replace(input, @"<script\b[^>]*>(.*?)</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
//		}
//		private static async Task<string> RenderViewToStringAsync(
//		    string viewName, object model,
//		    ControllerContext controllerContext,
//		    bool isPartial = false)
//		{
//			var actionContext = controllerContext as ActionContext;

//			var serviceProvider = controllerContext.HttpContext.RequestServices;
//			var razorViewEngine = serviceProvider.GetService(typeof(IRazorViewEngine)) as IRazorViewEngine;
//			var tempDataProvider = serviceProvider.GetService(typeof(ITempDataProvider)) as ITempDataProvider;


//			var metadataProvider = serviceProvider.GetService(typeof(IModelMetadataProvider)) as IModelMetadataProvider;
//			using (var sw = new StringWriter())
//			{

//				var viewResult = razorViewEngine.GetView("~/", viewName, false);

//				if (viewResult?.View == null)
//					throw new Exception($"{viewName} does not match any available view");

//				var viewDictionary = new ViewDataDictionary(
//					metadataProvider ?? new EmptyModelMetadataProvider(),
//					new ModelStateDictionary())
//				{
//					Model = model
//				};

//				var viewContext = new ViewContext(
//				    actionContext,
//				    viewResult.View,
//				    viewDictionary,
//				    new TempDataDictionary(actionContext.HttpContext, tempDataProvider),
//				    sw,
//				    new HtmlHelperOptions()
//				);

//				await viewResult.View.RenderAsync(viewContext);
//				return sw.ToString();
//			}

//		}

//		//private void ConvertStringToHtmlDocumentCssList(ref StringBuilder input, FileWithModifiedOn[] cssLinks, string suffixes)
//		//{
//		//	var htmlDocument = new HtmlDocument();
//		//	htmlDocument.LoadHtml(input.ToString());

//		//	var headNode = htmlDocument.DocumentNode.SelectSingleNode("//head");
//		//	if (headNode != null)
//		//	{
//		//		foreach (var cssLink in cssLinks)
//		//		{
//		//			if (!cssLink.ModifiedOn.HasValue)
//		//			{
//		//				var jsNode = HtmlNode.CreateNode($"<script src='{cssLink.Id}{suffixes}'></script>");
//		//				headNode.AppendChild(jsNode);
//		//			}
//		//			else
//		//			{
//		//				var version = cssLink.ModifiedOn.Value.ToString("yy.MM.dd.HH.mm.ss");
//		//				var cssNode = HtmlNode.CreateNode($"<link rel='stylesheet' href='{cssLink.Id}{suffixes}?v={version}'>");
//		//				headNode.AppendChild(cssNode);
//		//			}
//		//		}
//		//	}

//		//	input.Clear();
//		//	input.Append(htmlDocument.DocumentNode.OuterHtml);
//		//}

//		private void ConvertStringToHtmlDocumentCssList(ref StringBuilder input, FileWithModifiedOn[] cssLinks, string suffixes)
//		{
//			var htmlDocument = new HtmlDocument();
//			htmlDocument.LoadHtml(input.ToString());

//			var headNode = htmlDocument.DocumentNode.SelectSingleNode("//head");
//			if (headNode != null)
//			{
//				foreach (var cssLink in cssLinks)
//				{
//					if (!cssLink.ModifiedOn.HasValue)
//					{
//						var jsNode = HtmlNode.CreateNode($"<script src='{cssLink.Id}{suffixes}'></script>");
//						headNode.AppendChild(jsNode);
//					}
//					else
//					{
//						var version = cssLink.ModifiedOn.Value.ToString("yy.MM.dd.HH.mm.ss");
//						var cssNode = HtmlNode.CreateNode($"<link rel='stylesheet' href='{cssLink.Id}{suffixes}?v={version}'>");
//						headNode.AppendChild(cssNode);
//					}
//				}
//			}

//			input.Clear();
//			input.Append(htmlDocument.DocumentNode.OuterHtml);

//		}

//		private void ConvertStringToHtmlDocumentHead(ref StringBuilder input, string headInput)
//		{
//			var htmlDocument = new HtmlDocument();
//			htmlDocument.LoadHtml(input.ToString());

//			var headNode = htmlDocument.DocumentNode.SelectSingleNode("//head");
//			if (headNode == null)
//			{
//				headNode = htmlDocument.CreateElement("head");
//				htmlDocument.DocumentNode.PrependChild(headNode);
//			}

//			var tempHeadDocument = new HtmlDocument();
//			tempHeadDocument.LoadHtml(headInput);
//			foreach (var node in tempHeadDocument.DocumentNode.ChildNodes)
//			{
//				headNode.AppendChild(node.Clone());
//			}

//			input.Clear();
//			input.Append(htmlDocument.DocumentNode.OuterHtml);
//		}

//		private void ConvertStringToHtmlDocumentCss(ref StringBuilder input, string cssInput, string addNode = "//head")
//		{
//			var htmlDocument = new HtmlDocument();
//			htmlDocument.LoadHtml(input.ToString());

//			var headNode = htmlDocument.DocumentNode.SelectSingleNode(addNode);
//			if (headNode == null)
//			{
//				headNode = htmlDocument.CreateElement("head");
//				htmlDocument.DocumentNode.PrependChild(headNode);
//			}

//			var styleNode = HtmlNode.CreateNode($"<style>{cssInput}</style>");
//			headNode.AppendChild(styleNode);

//			input.Clear();
//			input.Append(htmlDocument.DocumentNode.OuterHtml);
//		}

//		private void ConvertStringToHtmlDocumentJs(ref StringBuilder input, string jsInput, string addNode = "//body")
//		{
//			var htmlDocument = new HtmlDocument();
//			htmlDocument.LoadHtml(input.ToString());

//			var bodyNode = htmlDocument.DocumentNode.SelectSingleNode(addNode);
//			if (bodyNode == null)
//			{
//				bodyNode = htmlDocument.CreateElement("head");
//				htmlDocument.DocumentNode.PrependChild(bodyNode);
//			}

//			var styleNode = HtmlNode.CreateNode(jsInput);
//			bodyNode.AppendChild(styleNode);

//			input.Clear();
//			input.Append(htmlDocument.DocumentNode.OuterHtml);
//		}

//		private class FileWithModifiedOn
//		{
//			public Guid? Id { get; set; }
//			public DateTime? ModifiedOn { get; set; }
//		}
//		private void ConvertStringToHtmlDocumentJs(ref StringBuilder input, FileWithModifiedOn[] jsLinks, string suffixes)
//		{
//			var htmlDocument = new HtmlDocument();
//			htmlDocument.LoadHtml(input.ToString());

//			var bodyNode = htmlDocument.DocumentNode.SelectSingleNode("//body");
//			if (bodyNode != null)
//			{
//				foreach (var jsLink in jsLinks)
//				{
//					if (!jsLink.ModifiedOn.HasValue)
//					{
//						var jsNode = HtmlNode.CreateNode($"<script src='{jsLink.Id}{suffixes}'></script>");
//						bodyNode.AppendChild(jsNode);
//					}
//					else
//					{
//						var version = jsLink.ModifiedOn.Value.ToString("yy.MM.dd.HH.mm.ss");
//						var jsNode = HtmlNode.CreateNode($"<script src='{jsLink.Id}{suffixes}?v={version}'></script>");
//						bodyNode.AppendChild(jsNode);
//					}
//				}
//			}

//			input.Clear();
//			input.Append(htmlDocument.DocumentNode.OuterHtml);
//		}

//		private void ConvertStringToHtmlDocumentJs(ref StringBuilder input, string[] jsLinks, string suffixes)
//		{
//			var htmlDocument = new HtmlDocument();
//			htmlDocument.LoadHtml(input.ToString());

//			var bodyNode = htmlDocument.DocumentNode.SelectSingleNode("//body");
//			if (bodyNode != null)
//			{
//				foreach (var jsLink in jsLinks)
//				{
//					var scripts = $"<script src='{jsLink}{suffixes}'></script>";
//					var jsNode = HtmlNode.CreateNode(scripts);
//					bodyNode.AppendChild(jsNode);
//				}
//			}

//			input.Clear();
//			input.Append(htmlDocument.DocumentNode.OuterHtml);
//		}

//		private string ConvertStringToHtmlDocumentBodyAndCss(string parent, string? html, string? css, string selector)
//		{
//			var htmlDocument = new HtmlDocument();
//			htmlDocument.LoadHtml(parent);
//			var bodyNode = htmlDocument.DocumentNode.SelectSingleNode($"//*[@id='{selector}']");

//			if (css.HasValue())
//			{
//				var styleNode = HtmlNode.CreateNode($"<style>{css}</style>");
//				bodyNode.AppendChild(styleNode);
//			}

//			if (bodyNode != null)
//			{
//				var jsNode = HtmlNode.CreateNode($"<div>{html}</div>");
//				bodyNode.AppendChild(jsNode);
//			}

//			return htmlDocument.DocumentNode.OuterHtml;
//		}

//		public class MediaResult : ContentResult
//		{
//			public ContentResult JavaScriptResult(string script)
//			{
//				this.Content = script;
//				this.ContentType = "application/javascript";
//				return this;
//			}

//			public ContentResult CSSResult(string script)
//			{
//				this.Content = script;
//				this.ContentType = "text/css";
//				return this;
//			}
//		}

//		static string AddIdToSelector(string css, string idValue)
//		{
//			return css;
//			if (string.IsNullOrWhiteSpace(css))
//			{
//				return "";
//			}
//			// Regular expression pattern to match selectors
//			var selectorPattern = @"(?<selector>[^{]+)\s*{";

//			// Replace each selector with the modified version
//			var modifiedCss = Regex.Replace(css, selectorPattern, match =>
//			{
//				var selector = match.Groups["selector"].Value.Trim();
//				var modifiedSelector = $"[{idValue}] {selector}";
//				return $"{modifiedSelector} {{";
//			});

//			return modifiedCss;
//		}

//		public string AddAttributeToElements(string htmlString, string attributeName, string attributeValue)
//		{
//			var htmlDoc = new HtmlDocument();
//			htmlDoc.LoadHtml(htmlString);


//			foreach (var node in htmlDoc.DocumentNode.Descendants())
//			{
//				if (node.Name != "#text")
//				{

//					node.SetAttributeValue(attributeName, attributeValue);
//				}
//			}


//			return htmlDoc.DocumentNode.OuterHtml;
//		}

//		#endregion

//		#region StaticFilesSystem
//		public string LoadJsFile(string address)
//		{
//			return _jsFileMemoryStorage.GetJsFileById(address)?.Content ?? "";
//		}

//		public JsFile GetJsFileById(Guid id)
//		{
//			return _jsFileMemoryStorage.GetJsFileById(id);
//		}

//		public CssFile GetCssFileById(Guid id)
//		{
//			return _cssFileMemoryStorage.GetCssFileById(id);
//		}

//		public CssFile[] GetCssFiles()
//		{
//			return _cssFileMemoryStorage.GetCssFiles();
//		}

//		public JsFile[] GetJsFiles()
//		{
//			return _jsFileMemoryStorage.GetJsFiles();
//		}

//		public string LoadCssFiles(string address)
//		{
//			return _cssFileMemoryStorage.GetCssFileById(address)?.Content ?? "";
//		}

//		public async Task<JsFile> SaveJsFile(JsFile newFile, CancellationToken cn)
//		{

//			newFile = await _service.Repository<JsFile>().
//				    SaveAsync(newFile, cn);

//			_jsFileMemoryStorage.UpdateJsFile((Guid)newFile.Id, newFile);

//			return newFile;
//		}

//		public async Task<CssFile> SaveCssFile(CssFile newFile, CancellationToken cn)
//		{
//			newFile = await _service.Repository<CssFile>().
//				SaveAsync(newFile, cn);

//			_cssFileMemoryStorage.UpdateCssFile((Guid)newFile.Id, newFile);

//			return newFile;
//		}

//		public async Task<Page> PublishPage(Page page, CancellationToken cn)
//		{
//			var existAddress = false;

//			foreach (var a in page.Address)
//			{
//				existAddress = await _service.Repository<Page>().TableNoTracking
//					.AnyAsync(c => c.Address.Contains(a) && c.Id != page.Id, cn);


//				if (existAddress == true)
//					break;
//			}
//			if (existAddress == true)
//				throw new Exception("خطا در ذخیره سازی یکی از آدرس های وارد شده تکراری میباشد");


//			await _service.Repository<Page>().SaveAsync(page, cn);

//			_pageMemoryStorage.UpdatePageContent((Guid)page.Id, page);

//			return page;
//		}

//		public async Task<Page> SavePage(Page page, CancellationToken cn)
//		{
//			var existAddress = false;

//			foreach (var a in page.Address)
//			{
//				existAddress = await _service.Repository<Page>().TableNoTracking
//					.AnyAsync(c => c.Address.Contains(a) && c.Id != page.Id, cn);

//				if (existAddress == true)
//					break;
//			}
//			if (existAddress == true)
//				throw new Exception("خطا در ذخیره سازی یکی از آدرس های وارد شده تکراری میباشد");

//			page = await _service.Repository<Page>().SaveAsync(page, cn);

//			_pageMemoryStorage.UpdatePageContent((Guid)page.Id, page);

//			return page;
//		}

//		public async Task<Page> SavePageExtension(Page page, CancellationToken cn)
//		{
//			var existAddres = false;

//			foreach (var a in page.Address)
//			{
//				existAddres = await _service.Repository<Page>().TableNoTracking
//					.AnyAsync(c => c.Address.Contains(a) && c.Id != page.Id, cn);

//				if (existAddres == true)
//					break;
//			}
//			if (existAddres == true)
//				throw new Exception("خطا در ذخیره سازی یکی از آدرس های وارد شده تکراری میباشد");


//			var existPage = await _service.Repository<Page>().
//				TableNoTracking
//				.Include(c => c.PublishContent)
//				.FirstOrDefaultAsync(c => c.Id == page.Id, cn);

//			if (existPage != null)
//			{
//				page.PublishContent = existPage.PublishContent;
//			}

//			page = await _service.Repository<Page>().SaveAsync(page, cn);

//			_pageMemoryStorage.UpdatePageContent((Guid)page.Id, page);

//			return page;

//		}

//		#endregion


//	}




//	public class RenderPageViewModel
//	{
//		public string html { get; set; } = "";
//		public string script { get; set; } = "";
//		public string Css { get; set; } = "";
//		public PageInfo pageInfo { get; set; } = new();

//		public List<string> listJsLink { get; set; } = new();
//		public List<string> listCssLink { get; set; } = new();
//	}
//	public class PageInfo
//	{
//		public string Title { get; set; } = "";
//		public string? Id { get; set; }
//		public string JSData { get; set; } = "{}";

//		public string Address { get; set; } = "";
//		public object documentData { get; set; } = new { };



//	}
}
