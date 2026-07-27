using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Entities.Base.DataTable;
using Entities.Base.Menu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.AccessServices;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebFramework.Controllers.SystemControllers
{
 
    [ControllerInfoAttribute("منو ساز")]
    [ApiController]
    [ApiResultFilter]
    [Route("System/[controller]")]
     
    public class MenuBuilderController(IUnitOfWork unitOfWork ,
         IMenuBuilderService menuBuilderService) : BaseController
    {
        [HttpGet("{action}")]
        [ActionDisplayName("لیست", ActionAccessType.View)]
        public IActionResult List()
        {
            return View("Views/Panel/System/MenuBuilder/List.cshtml");
        }

        [HttpGet("{action}")]
        [ActionDisplayName("ویرایش", ActionAccessType.View)]
        public IActionResult Edit(long? id)
        {
            if (id != null)
            {
                var entity = unitOfWork.Repository<SystemMenu>()
                    .TableNoTracking
                    .FirstOrDefault(c => c.Id == id);

                return View("Views/Panel/System/MenuBuilder/Edit.cshtml", entity);
            }
           
            return View("Views/Panel/System/MenuBuilder/Edit.cshtml" , new SystemMenu());
        }

        [HttpPost("{action}")]
        [ActionDisplayName("ذخیره", ActionAccessType.Api)]
        public IActionResult Save(SystemMenu entity)
        {
            if (entity.Id != null && entity.Id !=0)
            {
                unitOfWork.Repository<SystemMenu>()
                    .Update(entity);
            }
            else
            {
                unitOfWork.Repository<SystemMenu>()
                .Add(entity);
            }

            

            menuBuilderService.UpdateSystemMenu(entity);

            return Ok(entity);
        }

		[HttpPost("{action}")]
		[ActionDisplayName("دریافت منو با شناسه", ActionAccessType.Api)]
          [AllowAnonymous]
		public IActionResult GetMenuById(long? Id)
		{
		 
			var menus = menuBuilderService.GetMenuItems(Id, sdk);
               var menuHtml = menuBuilderService.GetHtmlItems(menus,null,sdk);

			return Ok(menuHtml);
		}


		[HttpPost("[action]")]
        [ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api)]
        public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
        {
	        return Ok(await unitOfWork.Repository<SystemMenu>().FetchDataAsync(request, cn));
        }
	}
}
