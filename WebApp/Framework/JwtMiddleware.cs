using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Common.Utilities;
using Entities.Auth;
using Microsoft.IdentityModel.Tokens;
using Services.Auth;

namespace WebApp.Framework
{
	public class JwtMiddleware
	{
		private readonly RequestDelegate _next;
 

		public JwtMiddleware(RequestDelegate next )
		{
			_next = next;
 
		}

		public async Task Invoke(HttpContext context, IUserService userService)
		{
			var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

			if (token != null)
				await attachUserToContext(context, userService, token);

			await _next(context);
		}

		private async Task attachUserToContext(HttpContext context, IUserService userService, string token)
		{
			try
			{
				var tokenHandler = new JwtSecurityTokenHandler();
				var key = Encoding.ASCII.GetBytes(AppSettings.PrivateKey);
				tokenHandler.ValidateToken(token, new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(key),
					ValidateIssuer = false,
					ValidateAudience = false,
			 
					ClockSkew = TimeSpan.Zero
				}, out SecurityToken validatedToken);

				var jwtToken = (JwtSecurityToken)validatedToken;
				var userId = jwtToken.Claims.First(x => x.Type == "id").Value.ToLong();

				//Attach user to context on successful JWT validation
				context.Items["User"] = await userService.GetById(userId);
			}
			catch
			{
				//Do nothing if JWT validation fails
				// user is not attached to context so the request won't have access to secure routes
			}
		}
	}
}
