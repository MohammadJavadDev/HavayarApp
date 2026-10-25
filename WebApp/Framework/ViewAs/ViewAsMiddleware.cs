namespace WebApp.Framework.ViewAs;

public sealed class ViewAsMiddleware(RequestDelegate next)
{
	public async Task Invoke(HttpContext context, ViewAsSessionStore store)
	{
		var sessionId = context.Request.Headers[ViewAsSessionStore.HeaderName].FirstOrDefault();
		if (string.IsNullOrWhiteSpace(sessionId))
			sessionId = context.Request.Query[ViewAsSessionStore.QueryName].FirstOrDefault();

		if (!string.IsNullOrWhiteSpace(sessionId))
		{
			context.Request.Cookies.TryGetValue("JwtToken", out var cookieToken);
			var adminUserId = store.ReadCookieUserId(cookieToken);
			var session = store.Find(sessionId);

			if (adminUserId == null || session == null || session.AdminUserId != adminUserId)
			{
				context.Response.StatusCode = StatusCodes.Status403Forbidden;
				context.Response.ContentType = "text/html; charset=utf-8";
				await context.Response.WriteAsync("""
					<!DOCTYPE html>
					<html lang="fa" dir="rtl">
					<head><meta charset="utf-8"><title>بررسی دسترسی</title></head>
					<body style="font-family:Tahoma,sans-serif;padding:32px;">
					<p>این پنجره بررسی دسترسی دیگر معتبر نیست. نشست خودتان تغییر نکرده است.</p>
					<p><button type="button" onclick="window.close()">بستن پنجره</button></p>
					</body>
					</html>
					""");
				return;
			}

			context.Items[ViewAsSessionStore.ItemKey] = session;
			context.Request.Headers.Authorization = "Bearer " + session.Token;
			context.Response.Headers.CacheControl = "no-store";
			await next(context);
			return;
		}

		if (context.Request.Cookies.TryGetValue("JwtToken", out var token))
			context.Request.Headers.Authorization = "Bearer " + token;

		await next(context);
	}
}
