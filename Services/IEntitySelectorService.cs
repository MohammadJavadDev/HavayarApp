using Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
	public interface IEntitySelectorService
	{
		Task<EntitySelectorResult> QueryAsync(string entityName, EntitySelectorRequest request);
		Task<List<EntitySelectorItem>> GetItemsByIdsAsync(List<string> ids, string queryId, string encryptedParams, string displayTemplate, string colMapStr);

		List<EntitySelectorItem> GetInitialItemsSync(List<string> ids, string cleanSql, Dictionary<string, string> sqlParams, string displayTemplate, string[] colMap);
	}
}
