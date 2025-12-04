using System.Text.RegularExpressions;
using Common.Utilities;
using Microsoft.AspNetCore.Http;
using NUglify;

namespace WebFramework.Middlewares
{
    public class ScriptExtractionMiddleware
    {
        private readonly RequestDelegate _next;

        public ScriptExtractionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // دریافت پاسخ اصلی
            var originalBodyStream = context.Response.Body;
            using (var memoryStream = new MemoryStream())
            {
                context.Response.Body = memoryStream;

                // ادامه پردازش درخواست
                await _next(context);

                // بررسی نوع محتوای پاسخ
                if (context.Response.ContentType != null &&
                    context.Response.ContentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
                {
                    // بازگشت به ابتدای استریم
                    memoryStream.Position = 0;
                    var responseBody = new StreamReader(memoryStream).ReadToEnd();

                    // استخراج تگ‌های <script>
                    var scriptTags = ExtractAndMinifyScriptTags(responseBody);
                    if(!scriptTags.HasValue(true))
                    {
                        memoryStream.Position = 0;
                        await memoryStream.CopyToAsync(originalBodyStream);
                    }
                    else
                    {
                        var scriptFileName = $"{context.Request.Path.Value.Replace("/","")}.js";
                        // ایجاد فایل جاوااسکریپت
                        var scriptsFilePath = Path.Combine("wwwroot", "scripts", scriptFileName);
                        if (!Directory.Exists(Path.Combine("wwwroot", "scripts")))
                        {
                            Directory.CreateDirectory(Path.Combine("wwwroot", "scripts"));
                        }

                        await System.IO.File.WriteAllTextAsync(scriptsFilePath, scriptTags);

                        // حذف تگ‌های <script> از محتوای HTML
                        var modifiedHtml = RemoveScriptTags(responseBody);

                        // ارسال لینک به فایل اسکریپت جدید در HTML
                        modifiedHtml = AddScriptFileLink(modifiedHtml, $"/scripts/{scriptFileName}");

                        // بازگرداندن محتوای تغییر یافته به پاسخ HTTP
                        context.Response.Body = originalBodyStream;
                        await context.Response.WriteAsync(modifiedHtml);
                    }
              
                }
                else
                {
                    // اگر نوع محتوا HTML نیست، مستقیماً محتوا را بازگردانید
                    memoryStream.Position = 0;
                    await memoryStream.CopyToAsync(originalBodyStream);
                }
            }
        }

        private string ExtractAndMinifyScriptTags(string htmlContent)
        {
            var scriptRegex = new Regex(@"<script>\s*(.*?)\s*</script>", RegexOptions.Singleline);
            var matches = scriptRegex.Matches(htmlContent);

       
            var allScripts = string.Join(Environment.NewLine, matches.Select(m => m.Groups[1].Value));
            var minifiedResult = Uglify.Js(allScripts);

             if (minifiedResult.HasErrors)
            {
                 return allScripts;
            }

            return minifiedResult.Code;
        }



        private string RemoveScriptTags(string htmlContent)
        {
            var scriptRegex = new Regex(@"<script>\s*(.*?)\s*</script>", RegexOptions.Singleline);
            return scriptRegex.Replace(htmlContent, "");
        }

        private string AddScriptFileLink(string htmlContent, string scriptFilePath)
        {
            return htmlContent.Replace("</body>", $"<script src=\"{scriptFilePath}\"></script></body>");
        }
    }


}
