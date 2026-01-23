using Data.Repositories;
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
	   IDataTableProfileService dataTableProfileService,
	   INotifitactionBuilderService _notifitactionBuilderService) : BaseController
	{
		[HttpGet("{action}/{id}")]
		public async Task<IActionResult> EntityInfoByProfile(long id)
		{
			var dataProfile = dataTableProfileService.GetDataTableProfileById(id);
			var entityName = dataProfile.EntityName;
			var data = _entityMetadataCache.Get(entityName);
			var existRules =await _notifitactionBuilderService.GetExist(entityName, (long)CurrentUserId);

			return Ok(new{entityData = data , existRules  });
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
