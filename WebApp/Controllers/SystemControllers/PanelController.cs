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
		   await   onlineUserService.RefreshUserRoleAccessesAsync(CurrentUserId , ct );

		  return View("Views/Panel/Index.cshtml");
		}
          

        [HttpGet("/Panel/NotFounded")]
        [AllowAnonymous]
        public IActionResult NotFounded()
        {
            return View("Views/Panel/NotFounded.cshtml");
        }


        [HttpGet("/panel/{action}")]
        [ActionDisplayName("فرم ساز", ActionAccessType.View)]
        public IActionResult FormBuilder()
        {
            HttpContext.Items["Name"] = "MohammadJavad";
            return View("Views/Panel/System/FormBuilder/Index.cshtml");
        }



		[HttpGet("/panel/{action}")]
		[ActionDisplayName("فرم ساز2", ActionAccessType.View)]
		public IActionResult FormBuilder2()
		{
			HttpContext.Items["Name"] = "MohammadJavad";
			return View("Views/Panel/System/FormBuilder/Edit.cshtml");
		}
	}
}
