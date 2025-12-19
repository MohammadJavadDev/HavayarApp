using Common.Attributes;
using Common.Auth.Enums;
using Data.SystemAuth;
using Entities.App.Edms;
 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Auth;
using WebApp.Framework.Generator;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.SystemControllers
{
     [Route("[controller]")]
     [ApiController]
     [ApiResultFilter]
	[Authorize("AuthenticatedUser")]
	[ControllerInfoAttribute("سیستم")]
	public class PanelController(IOnlineUserService onlineUserService,
		  IUserService service  ) : BaseController 
	{
  
        [HttpGet("/")]
        [HttpGet("/Panel")]
        public async Task<IActionResult> Index(CancellationToken ct)
		{
                //new ControllerGenerator().GenerateEntity(typeof(Document));
		   await   onlineUserService.RefreshUserRoleAccessesAsync((long)CurrentUserId , ct );

		  return View("Views/Panel/Index.cshtml");
		}
          

        [HttpGet("/Panel/NotFounded")]
        [AllowAnonymous]
        public IActionResult NotFounded()
        {
            return View("Views/Panel/NotFounded.cshtml");
        }


     
	}
}
