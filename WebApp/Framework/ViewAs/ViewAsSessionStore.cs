using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Entities.Auth;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace WebApp.Framework.ViewAs;

public sealed class ViewAsSessionStore(IMemoryCache cache)
{
	public const string HeaderName = "X-Havayar-ViewAs";
	public const string QueryName = "viewAs";
	public const string ItemKey = "ViewAs";

	private static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);

	public ViewAsSession Create(long adminUserId, long targetUserId, string targetName, string token)
	{
		var session = new ViewAsSession
		{
			Id = Guid.NewGuid().ToString("N"),
			AdminUserId = adminUserId,
			TargetUserId = targetUserId,
			TargetName = targetName,
			Token = token,
			ExpiresUtc = DateTime.UtcNow.Add(Lifetime)
		};

		cache.Set(CacheKey(session.Id), session, session.ExpiresUtc);
		return session;
	}

	public ViewAsSession? Find(string? id)
	{
		if (string.IsNullOrWhiteSpace(id))
			return null;

		if (!cache.TryGetValue(CacheKey(id), out ViewAsSession? session) || session == null)
			return null;

		if (session.ExpiresUtc <= DateTime.UtcNow)
		{
			cache.Remove(CacheKey(id));
			return null;
		}

		return session;
	}

	public long? ReadCookieUserId(string? jwt)
	{
		if (string.IsNullOrWhiteSpace(jwt))
			return null;

		try
		{
			var handler = new JwtSecurityTokenHandler();
			var principal = handler.ValidateToken(jwt, new TokenValidationParameters
			{
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidateLifetime = true,
				ValidateIssuerSigningKey = true,
				ValidIssuer = AppSettings.Issuer,
				ValidAudience = AppSettings.Audience,
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AppSettings.PrivateKey))
			}, out _);

			var raw = principal.FindFirst("id")?.Value
				?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			return long.TryParse(raw, out var userId) && userId > 0 ? userId : null;
		}
		catch
		{
			return null;
		}
	}

	public static string KeepOnUrl(HttpContext http, string url)
	{
		if (http.Items[ItemKey] is not ViewAsSession session)
			return url;

		if (url.Contains(QueryName + "=", StringComparison.OrdinalIgnoreCase))
			return url;

		var separator = url.Contains('?') ? "&" : "?";
		return url + separator + QueryName + "=" + Uri.EscapeDataString(session.Id);
	}

	private static string CacheKey(string id) => "view-as:" + id;
}
