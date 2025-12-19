using Common.Attributes;
using Common.Auth.Enums;
using Entities.Auth;
using Entities.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Auth;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.SystemControllers
{
    [ApiController]
    [ApiResultFilter]
    [Route("[controller]")]
    [Authorize(Roles = "admin")]
    [Authorize("AuthenticatedUser")]
    public class AuthenticateController(IUserService _userService) : BaseController
	{
        [HttpGet("[action]")]
		[AllowAnonymous]
		public IActionResult Login()
        {
            return View("Views/Authenticate/Login.cshtml");
        }

        [HttpGet("[action]")]
        [AllowAnonymous]
		[ActionDisplayName("عدم دسترسی", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult Forbidden()
           {
            return View("Views/Forbidden.cshtml");
		 }


	   [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login(AuthenticateRequest model , CancellationToken ct)
        {

            if (!string.IsNullOrEmpty(model.FName))
            {
                return BadRequest(new { message = "اطلاعات وارد شده اشتباه میباشد." });
            }
            var response = await _userService.Authenticate(model , ct);

            if (response == null)
                return BadRequest(new { message = "نام کاربری یا رمزعبور اشتباه میباشد ." });



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

        [HttpPost("[action]")]
        public async Task<IActionResult> Register(CreateUserViewModel model, CancellationToken cn)
        {
            if (!string.IsNullOrEmpty(model.FName))
            {
                return BadRequest(new { message = "اطلاعات وارد شده اشتباه میباشد." });
            }
          
            var response = await _userService.CreateUserAsync(model, cn);

            return Ok(response);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model, CancellationToken cn)
        {
            if (!string.IsNullOrEmpty(model.FName))
            {
                return BadRequest(new { message = "اطلاعات وارد شده اشتباه میباشد." });
            }
            var response = await _userService.CreateUserAsync(model, cn);

            return Ok(response);
        }

        [HttpGet("[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            Response.Cookies.Delete("JwtToken");
			return Redirect("/");
        }
		[HttpPost("[action]")]
		[Authorize("AuthenticatedUser")]
		public async Task<IActionResult> GetUserByName([FromForm]string name)
		{
		  return Ok( await _userService.SearchByName(name));

		}

	}
}
 