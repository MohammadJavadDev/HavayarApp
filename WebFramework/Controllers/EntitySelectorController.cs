using Common.Entities;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace WebFramework.Controllers.SystemControllers
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
		   [FromQuery] string searchColumnsInfo = "",   
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
					{ "defaultParams", defaultParams },
					{ "searchColumnsInfo", searchColumnsInfo }   
				}
			};

			return Ok(await _service.QueryAsync(entityName, request));
		}

		[HttpGet("get-ids")]
		public async Task<IActionResult> GetByIds(
		  [FromQuery] string ids,
		  [FromQuery] string queryId,
		  [FromQuery] string defaultParams,
		  [FromQuery] string displayTemplate)
		{
			if (string.IsNullOrEmpty(ids))
				return Ok(new List<object>());

			var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
						   .Select(x => x.Trim())
						   .Where(x => !string.IsNullOrEmpty(x))
						   .ToList();

			if (idList.Count == 0)
				return Ok(new List<object>());

			try
			{
				var items = await _service.GetItemsByIdsAsync(
					idList,
					queryId,
					defaultParams,
					displayTemplate,
					colMapStr: null
				);

				return Ok(items);
			}
			catch (Exception ex)
			{
				return BadRequest(new { error = ex.Message });
			}
		}
	}
}