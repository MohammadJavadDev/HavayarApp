using Common.Entities;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace WebApp.Controllers.SystemControllers
{
	[Route("api/entity-selector")]
	[ApiController]
	public class EntitySelectorController : ControllerBase
	{
		private readonly IEntitySelectorService _service;

		public EntitySelectorController(IEntitySelectorService service)
		{
			_service = service;
		}

		[HttpGet("{entityName}")]
		public async Task<IActionResult> Get(
		   string entityName,
		   [FromQuery] string q,
		   [FromQuery] int page = 1,
		   [FromQuery] int pageSize = 20,
		   [FromQuery] string queryId = "",       
		   [FromQuery] string defaultParams = "",  
		   [FromQuery] long? lastId = null)        
		{
			var request = new EntitySelectorRequest
			{
				Q = q,
				Page = page,
				PageSize = pageSize,
				LastId = lastId,
				Extra = new Dictionary<string, string>
			 {
				{ "queryId", queryId },
				{ "defaultParams", defaultParams }
			 }
			};

			return Ok(await _service.QueryAsync(entityName, request));
		}

		[HttpGet("get-ids")]
		public async Task<IActionResult> GetByIds(
		  [FromQuery] string ids, // Comma separated IDs
		  [FromQuery] string queryId,
		  [FromQuery] string defaultParams,
		  [FromQuery] string displayTemplate)
		{
			if (string.IsNullOrEmpty(ids)) return Ok(new List<object>());

			// idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

			//var items = await _service.GetItemsByIdsAsync(idList, queryId, defaultParams, displayTemplate);

			return Ok(new { });
		}
	}
}
