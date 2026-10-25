using Data.Services.QueryBuilderServices;
using Entities.Base.NotifitactionBuilder;
using Entities.Services;
using Microsoft.AspNetCore.Mvc;
using Services.NotifitactionBuilderServices;
using System.Threading.Tasks;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebFramework.Controllers.SystemControllers
{
	[ApiController]
	[ApiResultFilter]
	[Route("[controller]")]
	public class NotifictionBuilderController(IEntityMetadataCache _entityMetadataCache,
	   IQueryService queryService,
	   INotifitactionBuilderService _notifitactionBuilderService) : BaseController
	{
		[HttpGet("{action}/{id}")]
		public async Task<IActionResult> EntityInfoByProfile(long id)
		{
			var profile = await queryService.GetReportAsync(id);
			if (profile == null)
				return BadRequest("نمایه داده یافت نشد.");

			var entityName = profile.EntityFullName;
			if (string.IsNullOrWhiteSpace(entityName))
				return BadRequest("امکان ایجاد نوتیفیکشن برای این موجودیت نیست");

			var data = _entityMetadataCache.Get(entityName);
			if (data == null)
				return BadRequest("امکان ایجاد نوتیفیکشن برای این موجودیت نیست");

			if (CurrentUserId is not > 0)
				return BadRequest("کاربر جاری مشخص نیست.");

			var existRules = await _notifitactionBuilderService.GetExist(data.EntityFullName, CurrentUserId.Value);

			return Ok(new { entityData = data, existRules });
		}

		[HttpGet("{action}/{fullName}")]
		public IActionResult EntityInfoBy(string fullName)
		{

			var data = _entityMetadataCache.Get(fullName);
	
			return Ok(data);
		}

		[HttpPost("{action}")]
		public async Task<IActionResult> Save(SaveNotificationBuidler entity , CancellationToken tn)
		{
			
		   await	_notifitactionBuilderService.Save(entity , tn);
			return Ok();
		}
	}
}
