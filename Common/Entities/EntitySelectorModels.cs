using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Entities
{
	public class EntitySelectorResult
	{
		public int Page { get; set; }
		public int PageSize { get; set; }
		public int Total { get; set; }
		public List<EntitySelectorItem> Items { get; set; } = new List<EntitySelectorItem>();
	}

	public class EntitySelectorItem
	{
		public object Id { get; set; }
		public string Display { get; set; }
		public Dictionary<string, object> Row { get; set; }
	}

	public class SearchColumnInfo
	{
		public string ColumnName { get; set; }  // مسیر C# (مثل "ProductionOrder.ProductionOrderNumber")
		public string SqlAlias { get; set; }    // نام alias در CTE (مثل "ProductionOrderNumber")
		public string TypeName { get; set; }     // نوع (مثل "Int32")
		public bool IsString { get; set; }       // آیا string است؟
	}
	public class SelectorDefinition
	{
		public string Sql { get; set; }
		public string[] ColMap { get; set; }
		public string DisplayTemplate { get; set; }
		public string SearchCols { get; set; }
		public List<SearchColumnInfo> SearchColumnInfos { get; set; }
	}

	public class EntitySelectorRequest
	{
		public string? Q { get; set; }
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 20;
		public long? LastId { get; set; }
		public string Cols { get; set; }
		// NEW: The filter string passed from TagHelper
		public string Filter { get; set; }
		public Dictionary<string, string> Extra { get; set; }
	}
}
