using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.SystemAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Services.AccessServices;

namespace WebFramework.Middlewares
{
    public class CustomAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly IRoleMemoryStorage _roleMemoryStorage;
        private readonly IAccessMemoryStorage _accessMemoryStorage;
        private readonly IOnlineUserService _onlineUserService;
        private readonly ILogger<CustomAuthorizationMiddleware> _logger;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(1);

       
        private static readonly HashSet<string> AdminRoles = new(StringComparer.OrdinalIgnoreCase) { "admin" };

        public CustomAuthorizationMiddleware(
            RequestDelegate next,
            IMemoryCache cache,
            IRoleMemoryStorage roleMemoryStorage,
            IAccessMemoryStorage accessMemoryStorage,
            IOnlineUserService onlineUserService,
            ILogger<CustomAuthorizationMiddleware> logger)
        {
            _next = next;
            _cache = cache;
            _roleMemoryStorage = roleMemoryStorage;
            _accessMemoryStorage = accessMemoryStorage;
            _onlineUserService = onlineUserService;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
    
            var endpoint = context.GetEndpoint();

            if (endpoint == null)
            {
                await _next(context);
                return;
            }

          
            if (IsAllowAnonymous(endpoint))
            {
                await _next(context);
                return;
            }

   
            if (!IsAuthorized(context, endpoint))
            {
               
                await context.ForbidAsync();
                return;
            }

            await _next(context);
        }

        private bool IsAuthorized(HttpContext context, Endpoint endpoint)
        {
       
            string? authorization = context.Request.Cookies["JwtToken"];

            if (string.IsNullOrEmpty(authorization))
            {
                authorization = context.Request.Headers.Authorization;
            }

            if (string.IsNullOrEmpty(authorization))
            {
                return false;
            }

             if (IsAuthenticatedUserPolicy(endpoint))
            {
                return context.User?.Identity?.IsAuthenticated == true;
            }

            var type = endpoint.Metadata?.GetMetadata<ActionDisplayNameAttribute>()?.Type ?? ActionAccessType.Other;
            var requestPath = context.Request.Path.Value?.ToLower();

            if (string.IsNullOrEmpty(requestPath))
            {
                return false;
            }

            try
            {
             
                if (context.User?.Identity == null || !context.User.Identity.IsAuthenticated)
                {
                    return false;
                }

                var userId = context.User.Identity.GetUserId();
                var roleNames = _onlineUserService.GetUserRole(userId);

                if (roleNames == null || roleNames.Count == 0)
                {
                    _logger.LogWarning("User {UserId} has no roles assigned", userId);
                    return false;
                }

              
                if (roleNames.Any(r => AdminRoles.Contains(r)))
                {
                    return true;
                }

                  if (!_accessMemoryStorage.ExistPath(requestPath))
                {
                    return true;  
                }


				var templatePath = _accessMemoryStorage.GetMatchingTemplate(requestPath) ?? requestPath;
				var cacheKey = BuildCacheKey(roleNames, templatePath);

				if (_cache.TryGetValue(cacheKey, out bool cachedAccess))
				{
					if (!cachedAccess && type == ActionAccessType.Api)
						ThrowForbiddenException(requestPath);
					return cachedAccess;
				}

				bool hasAccess = CheckRoleAccess(roleNames, requestPath);
				_cache.Set(cacheKey, hasAccess, _cacheDuration);

				if (!hasAccess && type == ActionAccessType.Api)
                {
                    ThrowForbiddenException(requestPath);
                }

                return hasAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authorization error for path: {Path}", requestPath);

                if (type == ActionAccessType.Api)
                {
                    ThrowForbiddenException(requestPath);
                }

                return false;
            }
        }

		private bool CheckRoleAccess(List<string> roleNames, string requestPath)
		{
			foreach (var roleName in roleNames)
			{
				var role = _roleMemoryStorage.GetRoleByName(roleName);

				if (role?.RoleAccesses == null) continue;

				foreach (var access in role.RoleAccesses)
				{
					// ← تغییر: به‌جای Equals، از IsMatch استفاده می‌کنیم
					if (RoutePatternHelper.IsMatch(requestPath, access.Path))
						return true;
				}
			}

			return false;
		}

		private static string BuildCacheKey(List<string> roleNames, string requestPath)
        {
          
            if (roleNames.Count <= 3)
            {
                return $"{string.Join(",", roleNames)}:{requestPath}";
            }

           
            var capacity = roleNames.Sum(r => r.Count()) + roleNames.Count + requestPath.Length + 1;
            return string.Create(capacity, (roleNames, requestPath), (span, state) =>
            {
                int pos = 0;
                for (int i = 0; i < state.roleNames.Count; i++)
                {
                    if (i > 0)
                    {
                        span[pos++] = ',';
                    }
                    state.roleNames[i].AsSpan().CopyTo(span[pos..]);
                    pos += state.roleNames[i].Length;
                }
                span[pos++] = ':';
                state.requestPath.AsSpan().CopyTo(span[pos..]);
            });
        }

        private void ThrowForbiddenException(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new UnauthorizedAccessException("خطای عدم دسترسی");
            }

            var action = _accessMemoryStorage.GetAccessAction(path);
            if (action != null)
            {
                _logger.LogWarning("Access denied to path: {Path}, Controller: {Controller}, Action: {Action}",
                    path, action.AccessController?.DisplayName, action.DisplayName);

                throw new UnauthorizedAccessException(
                    $"خطای عدم دسترسی - آدرس: {action.AccessController?.DisplayName} - عملیات: {action.DisplayName}");
            }

            _logger.LogWarning("Access denied to unregistered path: {Path}", path);
            throw new UnauthorizedAccessException($"خطای عدم دسترسی - آدرس: {path}");
        }

        private static bool IsAllowAnonymous(Endpoint endpoint)
        {
            return endpoint.Metadata?.GetMetadata<IAllowAnonymous>() != null;
        }

        private static bool IsAuthenticatedUserPolicy(Endpoint endpoint)
        {
            var authorizeAttributes = endpoint.Metadata?.GetOrderedMetadata<AuthorizeAttribute>();

            if (authorizeAttributes == null)
            {
                return false;
            }

            foreach (var attr in authorizeAttributes)
            {
                if (string.Equals(attr.Policy, "AuthenticatedUser", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
