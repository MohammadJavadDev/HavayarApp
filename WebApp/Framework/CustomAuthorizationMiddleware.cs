using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Common.Attributes;
using Common.Auth.Enums;
using Entities.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Services.AccessServices;

namespace WebFramework.Middlewares
{
    public class CustomAuthorizationMiddleware(RequestDelegate next, IMemoryCache _cache  
       , IRoleMemoryStorage _roleMemoryStorage ,IAccessMemoryStorage _accessMemoryStorage)
    {
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(1);
        public async Task InvokeAsync(HttpContext context)
        {

      
                if (!IsAllowAnonymous(context))
                {
                    var endpoint = context.GetEndpoint();
                    if (endpoint == null)
                    {
                        await next(context);
                        return;
                    }
                    if (!IsAuthorized(context))
                    {

                        await context.ForbidAsync();
                     
                    }
                }
            

                await next(context);
        }
        private bool IsStaticFileRequest(HttpContext context)
        {
            var path = context.Request.Path.Value;
            if (path is not null && path.EndsWith(".js"))
            {
                return true;
            }

            if (path is not null && path.EndsWith(".css"))
            {
                return true;
            }
            if (path is not null && path.EndsWith(".svg"))
            {
                return true;
            } 
            if (path is not null && path.EndsWith(".png"))
            {
                return true;
            }
            if (path is not null && path.EndsWith(".woff"))
            {
                return true;
            }  
            if (path is not null && path.EndsWith(".ico"))
            {
                return true;
            }
            if (path is not null && path.EndsWith(".jpg"))
            {
                return true;
            }
            if (path is not null && path.EndsWith(".jpge"))
            {
                return true;
            }

            return false;
        }

        private bool IsAuthorized(HttpContext context)
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

            var type = context.GetEndpoint()
                           ?.Metadata?
                           .GetMetadata<ActionDisplayNameAttribute>()?.Type ??
                       ActionAccessType.Other;

            var requestPath = context.Request.Path.Value?.ToLower();

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                var key = Encoding.ASCII.GetBytes(AppSettings.PrivateKey);

                var credentials = new SymmetricSecurityKey(key);


                ClaimsPrincipal principal = tokenHandler.ValidateToken(authorization, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    IssuerSigningKey = credentials,
                    ValidIssuer = AppSettings.Issuer,
                    ValidAudience = AppSettings.Audience,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;

                var roleNames = jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value.Split(",");


                context.User = principal;

                if (roleNames.Any(c => c == "admin"))
                {
                    return true;
                }

  

                if (!_accessMemoryStorage.ExistPath(requestPath))
                {
                    return true;
                }
                 

                var cacheKey = $"{string.Join(",", roleNames)}:{requestPath}";

                if (_cache.TryGetValue(cacheKey, out bool hasAccess))
                {
                    if (!hasAccess)
                    {
                        if (type == ActionAccessType.Api)
                            ForbiddenException(requestPath);

                        return false;
                    }
 
                    return true;
                }

                hasAccess = roleNames.Any(roleName =>
                {
                    var role = _roleMemoryStorage.GetRoleByName(roleName);
                    return role?.RoleAccesses.Any(c=>c.Path.Contains(requestPath)) == true;
                });

                _cache.Set(cacheKey, hasAccess, _cacheDuration);

                //foreach (var roleName in roleNames)
                //{
                //    // Fetch the role from memory
                //    var role = _roleMemoryStorage.GetRoleByName(roleName);
                //    if (role != null && role.AccessPath != null && role.AccessPath.Contains(requestPath))
                //    {
                //        hasAccess = true;
                //        break;
                //    }
                //}

                if (type == ActionAccessType.Api && hasAccess == false)
                    ForbiddenException(requestPath);

                return hasAccess;

            }
            catch (Exception)
            {
                if (type == ActionAccessType.Api )
                    ForbiddenException(requestPath);

                return false;
            }


        }
        private void ForbiddenException(string path)
        {
           var action = _accessMemoryStorage.GetAccessAction(path);
           if (action != null)
           {
                
               throw new Exception("خطای عدم دسترسی \n آدرس :" + action.AccessController.DisplayName + " عملیات : " +action.DisplayName);
           }
           else
           {
               throw new Exception("خطای عدم دسترسی آدرس :" + path);
           }
          
        }
        public static string Base64UrlDecode(string input)
        {
            input = input.Replace('-', '+').Replace('_', '/');
            switch (input.Length % 4)
            {
                case 2: input += "=="; break;
                case 3: input += "="; break;
            }
            var base64EncodedBytes = Convert.FromBase64String(input);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
        private bool IsAllowAnonymous(HttpContext context)
        {

            var endpoint = context.GetEndpoint();

            if (endpoint?.Metadata != null)
            {
                var allowAnonymousAttribute = endpoint.Metadata.GetMetadata<IAllowAnonymous>();
                return allowAnonymousAttribute != null;
            }

            return false;
        }

       
    }
}
