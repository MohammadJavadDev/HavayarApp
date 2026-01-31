using Entities.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Auth;
using System;
using WebFramework.Page;

namespace App.BackgroundJob.Controllers
{
	[Authorize(Roles = "admin")]
	public class AuthenticateController : Controller
	{
		private readonly IUserService _userService;

		public AuthenticateController(IUserService userService)
		{
			_userService = userService;
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult Login()
		{
			return View();
		}

		[HttpPost]
		[AllowAnonymous]
		[Route("Authenticate/Login")]
		public async Task<IActionResult> Login([FromBody] AuthenticateRequest model, CancellationToken ct)
		{
			if (!string.IsNullOrEmpty(model.FName))
			{
				return BadRequest(new { message = "اطلاعات وارد شده اشتباه میباشد." });
			}

			var response = await _userService.Authenticate(model, ct);

			if (response == null)
				return BadRequest(new { message = "نام کاربری یا رمزعبور اشتباه میباشد." });

			// بررسی اینکه کاربر admin با شناسه 1 باشد یا نقش admin داشته باشد
			var user = await _userService.GetById(response.Id);
			if (user == null)
			{
				return BadRequest(new { message = "کاربر یافت نشد." });
			}

			// بررسی اینکه کاربر شناسه 1 داشته باشد یا نقش admin داشته باشد
			bool isAdmin = user.Id == 1 || (user.RoleIds != null && user.RoleIds.Contains(1)) ||
						   (user.Roles != null && user.Roles.Contains("admin", StringComparer.OrdinalIgnoreCase));

			if (!isAdmin)
			{
				return BadRequest(new { message = "فقط کاربران با دسترسی admin می‌توانند وارد شوند." });
			}

			// Create and configure the cookie
			var cookieOptions = new CookieOptions
			{
				SameSite = SameSiteMode.Strict,
				Expires = DateTime.UtcNow.AddDays(150),
				HttpOnly = true,
				Secure = true,
			};

			Response.Cookies.Append("JwtToken", response.Token, cookieOptions);

			return Ok(response);
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult Logout()
		{
			Response.Cookies.Delete("JwtToken");
			return RedirectToAction("Login");
		}
	}
}
